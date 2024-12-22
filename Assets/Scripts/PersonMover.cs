using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PersonMover : MonoBehaviour
{
    [SerializeField] GameObject[] lines; // Array of lines (including final lines)
    [SerializeField] GameObject waitingLine;
    [SerializeField] List<GameObject> finalLinePrefabs; // Prefabs for final lines
    [SerializeField] private string personTag; // Tag for persons
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float slideSpeed = 5f;
    [SerializeField] Transform finalLineTargetPosition; // Target position for final line
    [SerializeField] float slideOutOffset = 10f; // Distance to slide out to the right
    [SerializeField] float slideInOffset = -10f; // Distance to slide in from the left
    [SerializeField] private GameObject levelWonUI;
    [SerializeField] private GameObject levelLostUI;
    [SerializeField] private GameObject pausebtn;

    private int currentFinalLineIndex = 0;
    private bool isSliding = false;
    private GameObject currentFinalLine;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    void Start()
    {
        levelWonUI.SetActive(false);
        levelLostUI.SetActive(false);
        // Instantiate the first final line at the target position
        currentFinalLine = Instantiate(finalLinePrefabs[currentFinalLineIndex], finalLineTargetPosition.position, Quaternion.identity);
        currentFinalLine.name = finalLinePrefabs[currentFinalLineIndex].name; // Assign correct name
        lines[lines.Length - 1] = currentFinalLine; // Assign final line in the lines array

        // Start checking for automatic movement of persons to the final line
        StartCoroutine(AutoMoveMatchingPersonToFinalLine());
        StartCoroutine(AutoMoveFromWaitingLine());

    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag(personTag))
            {
                GameObject person = hit.collider.gameObject;
                StartCoroutine(MovePersonToNextBlock(person));
            }
        }
    }
    IEnumerator AutoMoveFromWaitingLine()
    {
        while (true)
        {
            Transform emptyBlock = GetEmptyBlock(lines[0]); // Check the first line for an empty block

            if (emptyBlock != null)
            {
                foreach (Transform block in waitingLine.transform)
                {
                    if (block.childCount > 0) // Check for persons in the waiting line
                    {
                        GameObject person = block.GetChild(0).gameObject;
                        Debug.Log($"Moving person '{person.name}' from waiting line to first line.");
                        yield return StartCoroutine(MovePersonToBlock(person, emptyBlock));
                        break; // Move one person at a time
                    }
                }
            }

            yield return new WaitForSeconds(0.5f); // Check periodically
        }
    }

    IEnumerator MovePersonToNextBlock(GameObject person)
{
    int currentLineIndex = GetCurrentLineIndex(person);

    while (currentLineIndex < lines.Length - 1)
    {
        int nextLineIndex = currentLineIndex + 1;
        Transform emptyBlock = GetEmptyBlock(lines[nextLineIndex]);

        if (emptyBlock != null)
        {
            // Check if we're moving to the final line
            if (nextLineIndex == lines.Length - 1)
            {
                string personColor = person.name.Replace("Person", "").Trim();
                string finalLineColor = currentFinalLine.name.Replace("FinalLine", "").Trim();

                if (personColor == finalLineColor)
                {
                    Debug.Log($"Person '{person.name}' color matched with final line.");
                    yield return StartCoroutine(MovePersonToBlock(person, emptyBlock));
                    currentLineIndex++;

                    // Enable the mesh of the second child when a person enters the final line
                    if (currentFinalLine.transform.childCount > 1)
                    {
                        MeshRenderer secondChildMesh = currentFinalLine.transform.GetChild(1).GetComponent<MeshRenderer>();
                        if (secondChildMesh != null)
                        {
                            secondChildMesh.enabled = true;
                        }
                    }
                }
                else
                {
                    Debug.Log($"Person '{person.name}' color did NOT match with final line. Stopping behind final line.");
                    yield break; // Stop moving if colors don't match
                }
            }
            else
            {
                // Move the person to the next line if it's not the final line
                yield return StartCoroutine(MovePersonToBlock(person, emptyBlock));
                currentLineIndex++;
            }
        }
        else
        {
            Debug.Log($"No empty block found in line {nextLineIndex}. Stopping person '{person.name}'.");
            break;
        }
    }

    // Check if the final line is full and trigger the sliding mechanism
    if (IsLineFull(lines[lines.Length - 1]) && !isSliding)
    {
        Debug.Log($"Final line '{currentFinalLine.name}' is full. Sliding out.");
        StartCoroutine(SlideOutFinalLine());
    }
}

    IEnumerator AutoMoveMatchingPersonToFinalLine()
{
    while (true)
    {
        GameObject lineBeforeFinalLine = lines[lines.Length - 2]; // The line before the final line
        bool allBlocksFilled = true; // Flag to check if all blocks are filled with persons
        bool anyPersonCanMove = false; // Flag to check if at least one person can be moved
        bool anyPersonInLine = false; // Flag to check if there is any person in the line to check for movement

        // Iterate over all blocks in the line before the final line to check if they are all filled
        foreach (Transform block in lineBeforeFinalLine.transform)
        {
            if (block.childCount == 0)
            {
                allBlocksFilled = false; // At least one block is empty, so the line is not fully filled
            }
            else
            {
                anyPersonInLine = true; // There is at least one person in the line
                GameObject person = block.GetChild(0).gameObject;
                string personColor = person.name.Replace("Person", "").Trim();
                string finalLineColor = currentFinalLine.name.Replace("FinalLine", "").Trim();

                // Check if the person's color matches the final line color
                if (personColor == finalLineColor)
                {
                    Transform emptyBlock = GetEmptyBlock(currentFinalLine);

                    if (emptyBlock != null)
                    {
                        Debug.Log($"Auto-moving person '{person.name}' to final line.");
                        yield return new WaitForSeconds(0.5f);
                        yield return StartCoroutine(MovePersonToBlock(person, emptyBlock));

                        // Enable the mesh of the second child when a person enters the final line
                        if (currentFinalLine.transform.childCount > 1)
                        {
                            MeshRenderer secondChildMesh = currentFinalLine.transform.GetChild(1).GetComponent<MeshRenderer>();
                            if (secondChildMesh != null)
                            {
                                secondChildMesh.enabled = true;
                            }
                        }

                        // Trigger sliding out if final line is full
                        if (IsLineFull(currentFinalLine) && !isSliding)
                        {
                            Debug.Log($"Final line '{currentFinalLine.name}' is now full after auto-move. Sliding out.");
                            StartCoroutine(SlideOutFinalLine());
                        }

                        anyPersonCanMove = true; // Mark that at least one person can move
                        break; // Exit the loop after moving one person
                    }
                }
            }
        }

        // If all blocks are filled, and no person can be moved, enable level lost UI
        if (allBlocksFilled && !anyPersonCanMove && anyPersonInLine)
        {
            Debug.Log("All blocks are filled, but no person could be moved to the final line. Level lost.");
            levelWonUI.SetActive(false);  // Hide the win UI
            yield return new WaitForSeconds(3f);
            levelLostUI.SetActive(true); // Show the lost UI
            pausebtn.SetActive(false);
            yield break; // Stop the coroutine
        }

        // If at least one person can move, we continue checking periodically
        yield return new WaitForSeconds(0.5f); // Check periodically
    }
}





    IEnumerator SlideOutFinalLine()
    {
        isSliding = true;
        Vector3 slideOutPosition = currentFinalLine.transform.position + Vector3.up * slideOutOffset;

        while (Vector3.Distance(currentFinalLine.transform.position, slideOutPosition) > 0.1f)
        {
            currentFinalLine.transform.position = Vector3.MoveTowards(currentFinalLine.transform.position, slideOutPosition, slideSpeed * Time.deltaTime);
            yield return null;
        }

        Destroy(currentFinalLine); // Destroy the current final line
        currentFinalLineIndex++; // Increment final line index

        if (currentFinalLineIndex < finalLinePrefabs.Count)
        {
            yield return StartCoroutine(SlideInNewFinalLine());
        }
        else
        {
            Debug.Log("All final lines completed.");
            levelWonUI.SetActive(true);
            pausebtn.SetActive(false);
            if (GameManager.levelToLoad < 11)
            {
                PlayerPrefs.SetInt("levelToLoad", ++GameManager.levelToLoad);
            }
            else
            {
                PlayerPrefs.SetInt("levelToLoad", GameManager.levelToLoad);
            }
            
            PlayerPrefs.Save();
        }

        isSliding = false;
    }

    IEnumerator SlideInNewFinalLine()
    {
        if (currentFinalLineIndex >= finalLinePrefabs.Count)
        {
            yield break;
        }

        Vector3 startPosition = finalLineTargetPosition.position + Vector3.left * slideInOffset;
        GameObject newFinalLine = Instantiate(finalLinePrefabs[currentFinalLineIndex], startPosition, Quaternion.identity);
        newFinalLine.name = finalLinePrefabs[currentFinalLineIndex].name;
        currentFinalLine = newFinalLine;

        // Disable the mesh of the second child
        if (newFinalLine.transform.childCount > 1)
        {
            MeshRenderer secondChildMesh = newFinalLine.transform.GetChild(1).GetComponent<MeshRenderer>();
            if (secondChildMesh != null)
            {
                secondChildMesh.enabled = false;
            }
        }

        while (Vector3.Distance(newFinalLine.transform.position, finalLineTargetPosition.position) > 0.1f)
        {
            newFinalLine.transform.position = Vector3.MoveTowards(newFinalLine.transform.position, finalLineTargetPosition.position, slideSpeed * Time.deltaTime);
            yield return null;
        }

        newFinalLine.transform.position = finalLineTargetPosition.position;
        lines[lines.Length - 1] = newFinalLine;

        // Restart auto-move coroutine
        Debug.Log("New final line slid in. Restarting auto-move.");
        StartCoroutine(AutoMoveMatchingPersonToFinalLine());
    }


    IEnumerator MovePersonToBlock(GameObject person, Transform targetBlock)
    {
        while (Vector3.Distance(person.transform.position, targetBlock.position) > 0.1f)
        {
            person.transform.position = Vector3.MoveTowards(person.transform.position, targetBlock.position, moveSpeed * Time.deltaTime);
            yield return null;
        }

        person.transform.SetParent(targetBlock);
    }

    Transform GetEmptyBlock(GameObject line)
    {
        foreach (Transform block in line.transform)
        {
            if (block.childCount == 0)
            {
                return block;
            }
        }
        return null;
    }

    int GetCurrentLineIndex(GameObject person)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (IsPersonInLine(person, lines[i]))
            {
                return i;
            }
        }
        return -1;
    }

    bool IsPersonInLine(GameObject person, GameObject line)
    {
        foreach (Transform block in line.transform)
        {
            if (block.childCount > 0 && block.GetChild(0).gameObject == person)
            {
                return true;
            }
        }
        return false;
    }

    bool IsLineFull(GameObject line)
    {
        // Get the first block of the line
        Transform firstBlock = line.transform.GetChild(0);

        // Check if the first block has any children
        if (firstBlock.childCount > 0)
        {
            return true; // Final line is considered full when the first block has a child
        }

        return false; // Otherwise, it's not full
    }

}
