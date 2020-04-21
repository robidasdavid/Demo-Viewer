using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Annotations : MonoBehaviour
{
    public GameObject particle;


    void Update()
    {
        float depth = .1f;
        Vector2 mousePos = Input.mousePosition;
        Vector2 wantedPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, depth));
        transform.localPosition = wantedPos;
    }
}
