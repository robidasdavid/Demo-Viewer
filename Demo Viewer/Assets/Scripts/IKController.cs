/**
 * Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Control IK of an individual player
 */

using Unity.Mathematics;
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
	public Vector3 bodyUp;
	public Vector3 bodyForward;

	public Transform neckTarget;
	public Transform bodyTarget;
	
	public Transform lHandTarget;
	public Transform rHandTarget;
	public Vector3 lHandPosition;
	public Vector3 rHandPosition;
	public Quaternion lHandRotation;
	public Quaternion rHandRotation;


	public Transform LeftThigh;
	public Transform RightThigh;
	
	
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
	
	private float BodyRotationValueForward = 171;
	private float BodyYRotation = 0;
	private float TargetForwardDotVelocityInLine;
	private float ForwardDotVelocityInLine;
	private float BodyXRotation0 = 0;
	private float BodyXRotation1 = 0;
	private float BodyXRotation2 = 0;
	private float BodyXRotation3 = 0;
	
	private float TargetBodyXRotation0 = 0;
	private float TargetBodyXRotation1 = 0;
	private float TargetBodyXRotation2 = 0;
	private float TargetBodyXRotation3 = 0;
	
	private float BodyYRotation0 = 0;
	private float BodyYRotation1 = 0;
	private float BodyYRotation2 = 0;
	private float BodyYRotation3 = 0;
	
	private float TargetBodyYRotation0 = 0;
	private float TargetBodyYRotation1 = 0;
	private float TargetBodyYRotation2 = 0;
	private float TargetBodyYRotation3 = 0;



	private float ThighXRotation = 0;
	private float CalfXRotation = 0;
	private float TargetlegsRestingMultiplier = 1;
	private float LegsRestingMultiplier = 1;
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
			//transform.rotation = bodyRotation;
		
			// set the headRotation
			
			//Debug.DrawRay(headPos,headForward*3);
			//Debug.DrawRay(headPos,headUp*3);
			head.rotation = Quaternion.LookRotation(headForward, headUp);
			
			bodyTarget.rotation = Quaternion.LookRotation(-bodyForward,-bodyUp);

			TargetForwardDotVelocityInLine = Vector3.Dot(bodyForward, playerVelocity) * 20;
			Vector3 bodyRight = Vector3.Cross(bodyForward.normalized, bodyUp.normalized);
			float TargetSideDotVelocityInLine = Vector3.Dot(bodyRight, playerVelocity) * 20;
			
			
			
			
			if (TargetForwardDotVelocityInLine > 0)
			{
				TargetBodyXRotation0 = TargetForwardDotVelocityInLine;
				TargetBodyXRotation1 = TargetForwardDotVelocityInLine - 20;
				TargetBodyXRotation2 = TargetForwardDotVelocityInLine - 40;
				TargetBodyXRotation3 = TargetForwardDotVelocityInLine - 40;
				
				TargetBodyXRotation0 = TargetBodyXRotation0 > 20 ? 20 : TargetBodyXRotation0 < 0? 0: TargetBodyXRotation0;
				TargetBodyXRotation1 = TargetBodyXRotation1 > 20 ? 20 : TargetBodyXRotation1 < 0? 0: TargetBodyXRotation1;
				TargetBodyXRotation2 = TargetBodyXRotation2 > 20 ? 20 : TargetBodyXRotation2 < 0? 0: TargetBodyXRotation2;
				TargetBodyXRotation3 = TargetBodyXRotation3 > 20 ? 20 : TargetBodyXRotation3 < 0? 0: TargetBodyXRotation3;
			}
			else
			{
				TargetBodyXRotation0 = TargetForwardDotVelocityInLine;
				TargetBodyXRotation1 = TargetForwardDotVelocityInLine + 15;
				TargetBodyXRotation2 = TargetForwardDotVelocityInLine + 30;
				TargetBodyXRotation3 = TargetForwardDotVelocityInLine + 45;
				
				TargetBodyXRotation0 = TargetBodyXRotation0 < -15 ? -15 : TargetBodyXRotation0 > 0? 0: TargetBodyXRotation0;
				TargetBodyXRotation1 = TargetBodyXRotation1 < -15 ? -15 : TargetBodyXRotation1 > 0? 0: TargetBodyXRotation1;
				TargetBodyXRotation2 = TargetBodyXRotation2 < -15 ? -15 : TargetBodyXRotation2 > 0? 0: TargetBodyXRotation2;
				TargetBodyXRotation3 = TargetBodyXRotation3 < -15 ? -15 : TargetBodyXRotation3 > 0? 0: TargetBodyXRotation3;
			}
			
			if (TargetSideDotVelocityInLine > 0)
			{
				TargetBodyYRotation0 = TargetSideDotVelocityInLine;
				TargetBodyYRotation1 = TargetSideDotVelocityInLine;
				TargetBodyYRotation2 = TargetSideDotVelocityInLine - 20;
				TargetBodyYRotation3 = TargetSideDotVelocityInLine - 40;
				
				TargetBodyYRotation0 = TargetBodyYRotation0 > 20 ? 20 : TargetBodyYRotation0 < 0 ? 0: TargetBodyYRotation0;
				TargetBodyYRotation1 = TargetBodyYRotation1 > 20 ? 20 : TargetBodyYRotation1 < 0 ? 0: TargetBodyYRotation1;
				TargetBodyYRotation2 = TargetBodyYRotation2 > 20 ? 20 : TargetBodyYRotation2 < 0 ? 0: TargetBodyYRotation2;
				TargetBodyYRotation3 = TargetBodyYRotation3 > 30 ? 30 : TargetBodyYRotation3 < 0 ? 0: TargetBodyYRotation3;
			}
			else
			{
				TargetBodyYRotation0 = TargetSideDotVelocityInLine;
				TargetBodyYRotation1 = TargetSideDotVelocityInLine;
				TargetBodyYRotation2 = TargetSideDotVelocityInLine + 20;
				TargetBodyYRotation3 = TargetSideDotVelocityInLine + 40;
				
				TargetBodyYRotation0 = TargetBodyYRotation0 < -20 ? -20 : TargetBodyYRotation0 > 0? 0: TargetBodyYRotation0;
				TargetBodyYRotation1 = TargetBodyYRotation1 < -20 ? -20 : TargetBodyYRotation1 > 0? 0: TargetBodyYRotation1;
				TargetBodyYRotation2 = TargetBodyYRotation2 < -20 ? -20 : TargetBodyYRotation2 > 0? 0: TargetBodyYRotation2;
				TargetBodyYRotation3 = TargetBodyYRotation3 < -30 ? -30 : TargetBodyYRotation3 > 0? 0: TargetBodyYRotation3;
			}
			
			BodyXRotation0 = math.lerp(BodyXRotation0, TargetBodyXRotation0, .05f);
			BodyXRotation1 = math.lerp(BodyXRotation1, TargetBodyXRotation1, .05f);
			BodyXRotation2 = math.lerp(BodyXRotation2, TargetBodyXRotation2, .05f);
			BodyXRotation3 = math.lerp(BodyXRotation3, TargetBodyXRotation3, .05f);

			BodyYRotation0 = math.lerp(BodyYRotation0, TargetBodyYRotation0, .05f);
			BodyYRotation1 = math.lerp(BodyYRotation1, TargetBodyYRotation1, .05f);
			BodyYRotation2 = math.lerp(BodyYRotation2, TargetBodyYRotation2, .05f);
			BodyYRotation3 = math.lerp(BodyYRotation3, TargetBodyYRotation3, .05f);

			ForwardDotVelocityInLine = math.lerp(ForwardDotVelocityInLine, TargetForwardDotVelocityInLine, .05f);
			Quaternion offsetBodyRotation = Quaternion.Euler(BodyXRotation0, 0, 0);
			
			Quaternion IdealBodyRotation = Quaternion.LookRotation(-bodyForward,-bodyUp);
			Quaternion IdealNeckRotation = Quaternion.LookRotation(bodyForward,bodyUp);
			float speed = math.abs(playerVelocity.magnitude);
			if (speed > 5)
			{
				speed = 5;
			}
			TargetlegsRestingMultiplier = (5 - speed)/5;
			
			neckTarget.rotation = IdealNeckRotation * offsetBodyRotation;
			bodyTarget.rotation = IdealBodyRotation * offsetBodyRotation;
			bodyTarget.GetChild(0).localRotation = Quaternion.Euler(BodyXRotation1+ (-10*TargetlegsRestingMultiplier), 0, -BodyYRotation1);
			bodyTarget.GetChild(0).GetChild(0).localRotation = Quaternion.Euler(BodyXRotation2+ (-10*TargetlegsRestingMultiplier), 0, -BodyYRotation2);
			bodyTarget.GetChild(0).GetChild(0).GetChild(0).localRotation = Quaternion.Euler(-BodyXRotation3+ (10*TargetlegsRestingMultiplier), 180, -(BodyYRotation3 ));

			
			
			
			
			
			//ThighRestingRotation = 32.247
			//CalfRestingRotation = -110
			
			
			LegsRestingMultiplier = math.lerp(LegsRestingMultiplier, TargetlegsRestingMultiplier, 0.05f);
			LeftThigh.localRotation = Quaternion.Euler( 32 * LegsRestingMultiplier, -5* LegsRestingMultiplier, -10* LegsRestingMultiplier);
			LeftThigh.GetChild(0).localRotation = Quaternion.Euler( -110 * LegsRestingMultiplier, 0, 0);
			
			RightThigh.localRotation = Quaternion.Euler( 32 * LegsRestingMultiplier, 5* LegsRestingMultiplier, 10* LegsRestingMultiplier);
			RightThigh.GetChild(0).localRotation = Quaternion.Euler( -110 * LegsRestingMultiplier, 0, 0);
			
			
			
			
			Debug.DrawRay(bodyPosition,bodyForward,Color.cyan);
			Debug.DrawRay(bodyPosition,playerVelocity,Color.green);
			
			Debug.DrawRay(bodyPosition, bodyForward*Vector3.Dot(bodyForward,playerVelocity),Color.red);
			
			Debug.DrawRay(bodyPosition,bodyRight,Color.yellow);
			
			Debug.DrawRay(bodyPosition, bodyRight*Vector3.Dot(bodyRight,playerVelocity),Color.magenta);
			//Debug.DrawRay(bodyPosition,bodyUp*3,Color.cyan);
			
			// float HeadYRotation = head.localRotation.eulerAngles.y;
			//
			// BodyYRotation = math.lerp(BodyYRotation, HeadYRotation, .05f);
			//
			// //bodyTarget.localRotation = quaternion.Euler(180,BodyXRotation,0);
			// bodyTarget.localRotation = Quaternion.Euler(180,BodyYRotation,0);
			//transform.rotation = quaternion.Euler(transform.eulerAngles.x,head.rotation.eulerAngles.y,transform.eulerAngles.z);
			playerStatsHover.rotation = head.rotation;
			//playerStatsHover.position = headPos;
				
			
			//psuedo-code
			//Given points A, B, P1
			 
			//get vector AB ~ Note the subtraction here
			 
			//get vector AP1 ~ Note the subtraction here
			Vector2 RelativeVelocityPlane = new Vector2(bodyTarget.forward.x,bodyTarget.forward.z);
			Vector2 GlobalVelocityPlane = new Vector2(playerVelocity.x,playerVelocity.z);

			//find the scalar projection of AP1 onto AB, thus distance
			float ForwardVelocity = Vector2.Dot(GlobalVelocityPlane, RelativeVelocityPlane) / GlobalVelocityPlane.magnitude;
			
			 // max forward = 267;
			 // min forward = 154
			 // defauly = 171
			if (ForwardVelocity > 0)
			{
				BodyRotationValueForward = 154;
			}
			else
			{
				BodyRotationValueForward = 267;

			}
			//bodyTarget.rotation = Quaternion.Euler(bodyRotation.eulerAngles.x+180,bodyRotation.eulerAngles.y,-bodyRotation.eulerAngles.z);
			//bodyTarget.position = bodyPosition;
			
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
