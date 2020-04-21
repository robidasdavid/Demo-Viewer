/**
 * Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Control IK of an individual player
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKController : MonoBehaviour
{

    public Vector3 rHandPosition;
    public Vector3 lHandPosition;
    public Vector3 headRotation;
    public Vector3 playerVelocity;

    public Transform rHandTarget;
    public Transform lHandTarget;

    public Transform rHandHint;
    public Transform lHandHint;

    public Transform Hip;
    public Transform Root;

    public Transform head;
    public Transform headPointer;

    //Just used to make wrist look good
    public Transform rForeArm;
    public Transform lForeArm;

    private Vector3 pos1;

    public float dot;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*dot = Vector3.Dot(headRotation, Vector3.up);
        //check for right side up
        if (Vector3.Dot(headRotation, Vector3.up) > -.5f)
        {
            transform.localScale = new Vector3(1,1,1);
        } else
        {
            transform.localScale = new Vector3(1, -1, 1);
        }*/

        // Get head angles with tangents *TODO: fix tan math so x and z work
        float headXAngle = Mathf.Atan2(headRotation.y, headRotation.z) * Mathf.Rad2Deg;
        float headYAngle = Mathf.Atan2(headRotation.x, headRotation.z) * Mathf.Rad2Deg;
        float headZAngle = Mathf.Atan2(headRotation.x, headRotation.y) * Mathf.Rad2Deg;

        float oppHeadYAngle = Mathf.Atan2(-headRotation.x, -headRotation.z) * Mathf.Rad2Deg;

        //get angle of the velocity on 2d plane
        float xzVelAng = Mathf.Atan2(playerVelocity.x, playerVelocity.z) * Mathf.Rad2Deg - 90;


        //hip
        //Determine the hip angle with a function (root x) so there are diminishing returns. Theres also a clamp at 70 degrees
        float zHipRot = Mathf.Clamp((Mathf.Sqrt(playerVelocity.magnitude) * 20), 0, 70);
        //Set the target hip rotation (local) based on the player's velocity.
        Vector3 targetHipRot = new Vector3(0,-180, zHipRot);
        //Set the rotation of the hip to a smooth interpolation between the current z position and the target
        Hip.localEulerAngles = new Vector3(0, -180, Mathf.Lerp(Hip.localEulerAngles.z, targetHipRot.z, Time.deltaTime * 1f));

        //body rotation
        //Set the target rotation to be at the angle of the velocity
        Vector3 bodyTargetRot = new Vector3(0, xzVelAng, 0);
        if (playerVelocity.magnitude > 0.5f)
        {
            //Smoothly rotate to the target rotation
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(bodyTargetRot), 2.5f);
        }

        //Set the hand positions
        rHandTarget.position = rHandPosition;
        lHandTarget.position = lHandPosition;
        //Sets hand rotations to continue off of the forearm
        rHandTarget.rotation = rForeArm.rotation;
        //this one was off and i couldnt be bothered to properly fix it but this is close enough
        lHandTarget.eulerAngles = lForeArm.eulerAngles + new Vector3(0, 60, 0);

        

        // Set angle to the absolute head angle (x only)
        head.eulerAngles = new Vector3(0, headYAngle-90, -90);
    }

}
