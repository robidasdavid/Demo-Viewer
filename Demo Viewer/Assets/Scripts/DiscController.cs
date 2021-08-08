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
		set {
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
		set {
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
	[Tooltip("blue, orange, default")]
	public Color[] lightColors = new Color[3];
	[Tooltip("blue, orange, default")]
	public Material[] materials = new Material[3];



	// Update is called once per frame
	void Update()
	{
		if (isGrabbed)
		{
			
		}
		else
		{
			
			if (discRotation != null)
			{
				transform.rotation = discRotation;
			}
			else
			{
				//Make it the right rotation (Mostly subjective)
				Vector3 upVector = new Vector3(19 - discVelocity.magnitude, discVelocity.magnitude, 0).normalized;
				transform.LookAt(discVelocity.normalized + transform.position, upVector);
			}
			//Make it spin based on speed
			child.Rotate(Vector3.up, 40 * discVelocity.magnitude * Time.deltaTime);
		}
		//set position
		transform.position = discPosition;
	}
}
