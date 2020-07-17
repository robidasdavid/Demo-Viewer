/**
 * David Robidas & Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Drive menu interactability
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
/*
 * Unity doesnt allow directly importing files in webgl for security.
 * TODO use an Application.ExternalCall or similar to call external js to open the file explorer.
 * Should make the file importing work with a web build.
 */
public class MenuScript : MonoBehaviour
{
    public GameObject loadingScreen;
    public InputField fileInput;
    static string fileDirector;


    void Start()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        loadingScreen.SetActive(false);
        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log("ARG " + i + ": " + args[i]);
            if (args[i].Contains(".json") || args[i].Contains(".echoreplay"))
            {
                fileInput.text = args[i];
                StartButtonClick();
                break;
            }
        }
    }

    public void StartButtonClick()
    {
        //Activate loading screen ux
        loadingScreen.SetActive(true);
        //Save input path to a static variable. 
        fileDirector = fileInput.text;
        PlayerPrefs.SetString("fileDirector", fileDirector);
        Debug.Log(fileDirector);
        //Change Scene to game scene (index 1)
        SceneManager.LoadScene(1);
    }
}
