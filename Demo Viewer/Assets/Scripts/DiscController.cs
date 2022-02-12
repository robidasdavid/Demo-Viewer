/**
 * Zzenith 2020
 * Date: 19 April 2020
 * Purpose: Control Disc position and rotation
 */

using System;
using System.Linq;
using EchoVRAPI;
using UnityEngine;
using Transform = UnityEngine.Transform;

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
				grabTime = Time.timeAsDouble;
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
	public Team.TeamColor TeamIndex
	{
		set
		{
			// if (value != teamIndex)
			{
				pointLight.color = lightColors[(int)teamIndex];
				discTrailRenderer.material.color = lightColors[(int)teamIndex];
				bubbleMaterial.SetColor(BubbleMaterialColor, bubbleColors[(int)teamIndex]);
			}

			teamIndex = value;
		}
		get => teamIndex;
	}

	private Team.TeamColor teamIndex = Team.TeamColor.spectator;

	private double grabTime;

	public Transform child;
	public Transform discFloating;
	public Transform discGrabbed;

	public Material bubbleMaterial;

	public Renderer discTrailRenderer;

	public Light pointLight;
	[Tooltip("blue, orange, default")] public Color[] lightColors = new Color[3];
	[Tooltip("blue, orange, default")] public Material[] materials = new Material[3];
	[Tooltip("blue, orange, default")] public Color[] bubbleColors = new Color[3];
	private static readonly int BubbleMaterialColor = Shader.PropertyToID("CurrentColor");


	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.instance.playhead == null) return;
		Frame frame = DemoStart.instance.playhead.GetFrame();
		if (frame?.disc == null) return;

		discVelocity = frame.disc.velocity.ToVector3();
		discPosition = frame.disc.Position;
		discRotation = frame.disc.Rotation;

		// blue team possession effects
		// Team.TeamColor teamPossession = frame.GetAllPlayers().FirstOrDefault(p => p.possession)?.team_color ?? Team.TeamColor.spectator;
		// TeamIndex = teamPossession;

		if (frame.teams[0].players?.FirstOrDefault(p => p.possession) != null)
		{
			TeamIndex = Team.TeamColor.blue;
		}
		else if (frame.teams[1].players?.FirstOrDefault(p => p.possession) != null)
		{
			TeamIndex = Team.TeamColor.orange;
		}
		else
		{
			TeamIndex = Team.TeamColor.spectator;
		}

		// if (frame.teams[0] != null && frame.teams[0].possession)
		// {
		// 	TeamIndex = Team.TeamColor.blue;
		// }
		// // orange team possession effects
		// else if (frame.teams[1] != null && frame.teams[1].possession)
		// {
		// 	TeamIndex = Team.TeamColor.orange;
		// }
		// // no team possession effects
		// else
		// {
		// 	TeamIndex = Team.TeamColor.spectator;
		// }


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