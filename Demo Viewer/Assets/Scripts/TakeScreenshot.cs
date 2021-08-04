using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TakeScreenshot : MonoBehaviour
{
    public int superSize = 4;
    
    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ScreenCapture.CaptureScreenshot($"screenshots/{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png", superSize);
        }
    }
}
