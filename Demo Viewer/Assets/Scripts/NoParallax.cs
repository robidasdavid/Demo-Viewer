﻿using UnityEngine;

public class NoParallax : MonoBehaviour
{
    public Transform cam;

    void LateUpdate()
    {
        transform.position = cam.transform.position;
    }
}