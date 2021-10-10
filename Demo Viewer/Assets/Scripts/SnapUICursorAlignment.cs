using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapUICursorAlignment : MonoBehaviour
{
    public Collider col;

    // Update is called once per frame
    private void Update()
    {
        if (col == null)
        {
            Debug.LogError("Snap UI cursor doesn't have collider assigned.");
            return;
        }
        
        //position setting
        Vector3 colliderPos = transform.parent.position;
        float distance = Vector3.Dot(colliderPos - col.transform.position, col.transform.up);
        transform.position = colliderPos - col.transform.up * distance;
        transform.forward = col.transform.up;
        transform.Rotate(0, 0, 45);
    }
}
