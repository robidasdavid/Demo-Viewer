/**
 * Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Control IK of an individual player
 */
using UnityEngine;

public class IKController : MonoBehaviour
{
    //justTesting
    public Transform testingSphere;

    public Vector3 rHandPosition;
    public Vector3 lHandPosition;
    public Vector3 headForward;
    public Vector3 headUp;
    public Vector3 playerVelocity;

    public Transform rHandTarget;
    public Transform lHandTarget;

    public Transform rHandHint;
    public Transform lHandHint;

    public Transform lLeg;
    public Transform rLeg;

    public Transform Root;
    public Transform standUp;

    public Transform head;

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
        dot = Vector3.Dot(headUp, Vector3.up);
        /*if(Vector3.Dot(headUp, Vector3.up) < 0f)
        {
            Debug.Log(Vector3.Dot(headUp, standUp.forward));
            standUp.RotateAround(head.transform.position, standUp.up, 180 * Time.deltaTime);
        }*/
        Vector3 rotAxis = Vector3.Cross(Vector3.up, headUp);
        if(Vector3.Dot(headUp, standUp.forward) > 0)
        {
            standUp.RotateAround(head.transform.position, standUp.up, 180);
        }

        //standUp.RotateAround(head.transform.position, standUp.up, 360 * Time.deltaTime);
        //check for right side up
        //transform.localScale = Vector3.Dot(headUp, Vector3.up) > 0f ? new Vector3(1, 1, 1) : new Vector3(1, -1, 1);

        //get angle of the velocity on 2d plane
        float xzVelAng = Mathf.Atan2(playerVelocity.x, playerVelocity.z) * Mathf.Rad2Deg - 90;


        //hip (both legs)
        //Determine the hip angle with a function (root x) so there are diminishing returns. Theres also a clamp at 60 degrees
        float zHipRot = Mathf.Clamp((Mathf.Sqrt(playerVelocity.magnitude) * 19), 0, 60);
        //Set the rotation of the hip to a smooth interpolation between the current z position and the target
        //Hip.localEulerAngles = new Vector3(0, -180, Mathf.Lerp(Hip.localEulerAngles.z, targetHipRot.z, Time.deltaTime * 1f));
        lLeg.localEulerAngles = new Vector3(Mathf.Lerp(lLeg.localEulerAngles.x, zHipRot, Time.deltaTime * 1f), 0, 175);
        rLeg.localEulerAngles = new Vector3(Mathf.Lerp(rLeg.localEulerAngles.x, zHipRot, Time.deltaTime * 1f), 0, 185);


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
        head.LookAt(headForward + transform.position, headUp);
    }

}
