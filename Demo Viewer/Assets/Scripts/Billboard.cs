/**
 * Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Orient nametags over player objects
 */

using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Rotate it (y axis only) after all other movement
    void LateUpdate()
    {
        transform.LookAt(new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z));
    }
}
