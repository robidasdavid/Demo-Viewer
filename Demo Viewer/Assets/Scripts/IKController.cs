/**
 * Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Control IK of an individual player
 */
using UnityEngine;

/// <summary>
/// 🕺
/// </summary>
public class IKController : MonoBehaviour
{
	public Transform head;
	public Vector3 headPos;
	public Vector3 headUp;
	public Vector3 headForward;
	public Vector3 playerVelocity;

	public Vector3 bodyPosition;
	public Quaternion bodyRotation;

	public Transform lHandTarget;
	public Transform rHandTarget;
	public Vector3 lHandPosition;
	public Vector3 rHandPosition;
	public Quaternion lHandRotation;
	public Quaternion rHandRotation;

	public Transform lHandHint;
	public Transform rHandHint;

	public Transform lLeg;
	public Transform rLeg;

	public Transform Root;
	public Transform standUp;

	//Just used to make wrist look good
	public Transform rForeArm;
	public Transform lForeArm;

	public Transform playerStatsHover;
	// Update is called once per frame
	void Update()
	{
		// old API compatible method
		if (bodyPosition == null)
		{
			float dot = Vector3.Dot(headUp, Vector3.up);
			/*if(Vector3.Dot(headUp, Vector3.up) < 0f)
			{
				Debug.Log(Vector3.Dot(headUp, standUp.forward));
				standUp.RotateAround(head.transform.position, standUp.up, 180 * Time.deltaTime);
			}*/
			Vector3 rotAxis = Vector3.Cross(Vector3.up, headUp);
			if (Vector3.Dot(headUp, standUp.forward) > 0)
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
			Vector3 headDirection2D = Vector3.ProjectOnPlane(headForward, Vector3.up);
			Vector3 headRotation2D = Quaternion.LookRotation(headDirection2D).eulerAngles;
			headRotation2D.y -= 90;
			Vector3 velRotation2D = new Vector3(0, xzVelAng, 0);

			Debug.DrawRay(head.transform.position, headDirection2D, Color.blue);
			Debug.DrawRay(head.transform.position, Quaternion.Euler(velRotation2D) * Vector3.forward, Color.red);

			// perform a weighted avg between head rotation and player velocity.
			// At low speeds, head rotation is used more; at speeds > 2m/s, the vel direction is used
			// I'm pretty sure this is worse than it was before
			Vector3 bodyTargetRot = Vector3.Slerp(headRotation2D, velRotation2D, Mathf.Clamp01(playerVelocity.magnitude / 2f));
			//Vector3 bodyTargetRot = velRotation2D;

			Debug.DrawRay(head.transform.position, Quaternion.Euler(bodyTargetRot) * Vector3.forward, Color.yellow);

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
		// use the new v28 API version with body position
		else
		{
			// set the whole player position based on the head
			transform.position = headPos;

			// set the body rotation
			transform.rotation = bodyRotation;

			// set the headRotation
			head.rotation = Quaternion.LookRotation(headForward, headUp);
			playerStatsHover.rotation = head.rotation;
			playerStatsHover.position = headPos;
			// TODO set the leg stuff based on velocity

			//Set the hand positions
			lHandTarget.position = lHandPosition;
			rHandTarget.position = rHandPosition;
			//Sets hand rotations
			lHandTarget.rotation = lHandRotation;
			rHandTarget.rotation = rHandRotation;
		}
	}

}
