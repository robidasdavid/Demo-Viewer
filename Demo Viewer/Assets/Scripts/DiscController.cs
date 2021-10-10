/**
 * Zzenith 2020
 * Date: 19 April 2020
 * Purpose: Control Disc position and rotation
 */

using System;
using UnityEngine;

public class DiscController : MonoBehaviour
{
	public Vector3 discPosition;
	public Quaternion discRotation;

	public Vector3 discVelocity;

	//public ArrayList isGrabbed = new ArrayList() { false, false, new int[2]{ -1, -1 } };
	//public GameObject playerHolding;
	public bool IsGrabbed
	{
		set
		{
			if (value != isGrabbed)
			{
				discFloating.gameObject.SetActive(!value);
				discGrabbed.gameObject.SetActive(value);
				grabTime = Time.time;
			}

			isGrabbed = value;
		}
		get => isGrabbed;
	}

	private bool isGrabbed;

	/// <summary>
	/// 0: blue
	/// 1: orange
	/// 2: spectator
	/// </summary>
	public TeamColor TeamIndex
	{
		set
		{
			if (value != teamIndex)
			{
				pointLight.color = lightColors[(int) teamIndex];
				discTrailRenderer.material.color = lightColors[(int) teamIndex];
			}

			teamIndex = value;
		}
		get => teamIndex;
	}

	private TeamColor teamIndex;

	private float grabTime;

	public Transform child;
	public Transform discFloating;
	public Transform discGrabbed;

	public Renderer discTrailRenderer;

	public Light pointLight;
	[Tooltip("blue, orange, default")] public Color[] lightColors = new Color[3];
	[Tooltip("blue, orange, default")] public Material[] materials = new Material[3];


	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.playhead == null) return;
		Frame frame = DemoStart.playhead.GetFrame();
		if (frame == null) return;

		discVelocity = frame.disc.velocity.ToVector3();
		discPosition = frame.disc.position.ToVector3();
		if (frame.disc.forward != null)
		{
			Debug.DrawRay(frame.disc.position.ToVector3(), transform.TransformVector(frame.disc.up.ToVector3()), Color.green);
			Debug.DrawRay(frame.disc.position.ToVector3(), transform.TransformVector(frame.disc.forward.ToVector3()), Color.blue);
			Debug.DrawRay(frame.disc.position.ToVector3(), transform.TransformVector(frame.disc.left.ToVector3()), Color.red);
			discRotation = Quaternion.LookRotation(frame.disc.left.ToVector3(), frame.disc.forward.ToVector3());
			// discRotation.y *= -1;
			// discRotation.z *= -1;
		}

		// blue team possession effects
		if (frame.teams[0] != null && frame.teams[0].possession)
		{
			TeamIndex = TeamColor.orange;
		}
		// orange team possession effects
		else if (frame.teams[1] != null && frame.teams[1].possession)
		{
			TeamIndex = TeamColor.blue;
		}
		// no team possession effects
		else
		{
			TeamIndex = TeamColor.spectator;
		}


		if (isGrabbed)
		{
		}
		else
		{
			if (discRotation != null)
			{
				child.localRotation = discRotation;
			}
			else
			{
				//Make it the right rotation (Mostly subjective)
				Vector3 upVector = new Vector3(19 - discVelocity.magnitude, discVelocity.magnitude, 0).normalized;
				transform.LookAt(discVelocity.normalized + transform.position, upVector);
			}

			// //Make it spin based on speed
			// child.Rotate(Vector3.up, 40 * discVelocity.magnitude * Time.deltaTime);
		}

		//set position
		transform.position = discPosition;
	}
}