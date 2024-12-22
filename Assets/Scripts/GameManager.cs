using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static int levelToLoad;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] GameObject pauseBtn;
    private void Start()
    {
        levelToLoad = PlayerPrefs.GetInt("levelToLoad", 1);
    }

    public void PlayBtn()
    {
        SceneManager.LoadScene(levelToLoad);
    }

    public void BackBtn()
    {
        SceneManager.LoadScene(0);
    }

    public void RetryBtn()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void PauseBtn()
    {
        pauseUI.SetActive(true);
        pauseBtn.SetActive(false);
        Time.timeScale = 0;
    }

    public void ResumeBtn()
    {
        pauseUI.SetActive(false);
        pauseBtn.SetActive(true);
        Time.timeScale = 1;
    }

    public void QuitBtn()
    {
        Application.Quit();
    }
}
