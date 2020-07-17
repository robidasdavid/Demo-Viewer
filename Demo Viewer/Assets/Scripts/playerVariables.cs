using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerVariables : MonoBehaviour
{
    public GameObject boost;
    public GameObject playerShield;

    public GameObject lftForearm;
    public GameObject rtForearm;

    public bool stunnedInitiated;

    void Start()
    {
        stunnedInitiated = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
