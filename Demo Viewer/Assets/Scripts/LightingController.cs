using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            RenderSettings.fog = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        
        if (other.CompareTag("MainCamera"))
        {
            RenderSettings.fog = false;
        }
            
    }
}
