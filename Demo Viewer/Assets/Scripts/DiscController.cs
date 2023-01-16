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
				discTrailRenderer.material.color = trailColors[(int)teamIndex] * emissionColorIntensity;
				bubbleMaterial.SetColor(BubbleMaterialColor, bubbleColors[(int)teamIndex] * emissionColorIntensity);
				discMaterial.SetColor("_BaseColor", discBaseColors[(int)TeamIndex]);
				discMaterial.SetColor("_EmissionColor", discEmissionColors[(int)TeamIndex] * emissionColorIntensity);
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

	public Renderer discBubbleRend;
	private Material bubbleMaterial;

	public Renderer discRend;
	private Material discMaterial;

	public Renderer discTrailRenderer;

	public Light pointLight;
	[Tooltip("blue, orange, default")] public Color[] lightColors = new Color[3];
	[Tooltip("blue, orange, default")] public Material[] materials = new Material[3];
	[Tooltip("blue, orange, default")] public Color[] bubbleColors = new Color[3];
	[Tooltip("blue, orange, default")] public Color[] discBaseColors = new Color[3];
	[Tooltip("blue, orange, default")] public Color[] discEmissionColors = new Color[3];
	[Tooltip("blue, orange, default")] public Color[] trailColors = new Color[3];
	public float emissionColorIntensity = 1;

	private static readonly int BubbleMaterialColor = Shader.PropertyToID("CurrentColor");

	private void Start()
	{
		bubbleMaterial = discBubbleRend.material;
		discMaterial = discRend.material;
	}

	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.instance.playhead == null) return;
		Frame frame = DemoStart.instance.playhead.GetFrame();
		if (frame?.disc == null) return;

		discVelocity = frame.disc.velocity.ToVector3();
		discPosition = frame.disc.Position;

		float[] discFwdRaw = frame.disc.Rotation.Forward().ToFloatArray();
		float[] discUpRaw = frame.disc.Rotation.Up().ToFloatArray();
		float[] discLeftRaw = frame.disc.Rotation.Left().ToFloatArray();

		Vector3 discFwd = new Vector3(discFwdRaw[0], discFwdRaw[1], discFwdRaw[2]);
		Vector3 discUp = new Vector3(discUpRaw[0], discUpRaw[1], discUpRaw[2]);
		Vector3 discLeft = new Vector3(discLeftRaw[0], discLeftRaw[1], discLeftRaw[2]);

		Quaternion finalDiscRot = Quaternion.LookRotation(discFwd, discUp);

		Quaternion upToUp = new Quaternion();
		//upToUp.SetFromToRotation(discUp, Vector3.up);
		Quaternion upRel = Quaternion.LookRotation(Vector3.forward, discUp);
		//finalDiscRot = Quaternion.Inverse(upRel) * finalDiscRot;

		discRotation = /*Quaternion.LookRotation(Vector3.left, Vector3.up) */ (finalDiscRot);

		Debug.DrawRay(discPosition, discRotation * Vector3.forward, Color.red);
		Debug.DrawRay(discPosition, discRotation * Vector3.left, Color.gray);
		Debug.DrawRay(discPosition, discRotation * Vector3.up, Color.green);


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
				transform.rotation = discRotation;
			}
			else
			{
				//Make it the right rotation (Mostly subjective)
				Vector3 upVector = new Vector3(19 - discVelocity.magnitude, discVelocity.magnitude, 0).normalized;
				//transform.LookAt(discVelocity.normalized + transform.position, upVector);
			}

			// //Make it spin based on speed
			// child.Rotate(Vector3.up, 40 * discVelocity.magnitude * Time.deltaTime);
		}

		//set position
		transform.position = discPosition;
	}
}