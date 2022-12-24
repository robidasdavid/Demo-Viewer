﻿using System;
using EchoVRAPI;
using UnityEngine;
using Transform = UnityEngine.Transform;

public class SimpleCameraController : MonoBehaviour
{
	private class CameraState
	{
		public float yaw;
		public float pitch;
		public float roll;
		public float x;
		public float y;
		public float z;
		public float orbitDistance = 3;

		public void SetPosition(Vector3 position)
		{
			x = position.x;
			y = position.y;
			z = position.z;
		}

		public void SetRotation(Quaternion rotation)
		{
			Vector3 eulerAngles = rotation.eulerAngles;
			pitch = eulerAngles.x;
			yaw = eulerAngles.y;
			roll = eulerAngles.z;
		}

		public void Translate(Vector3 translation)
		{
			Vector3 rotatedTranslation = Quaternion.Euler(0, yaw, roll) * translation;

			x += rotatedTranslation.x;
			y += rotatedTranslation.y;
			z += rotatedTranslation.z;
		}

		public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
		{
			yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
			pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
			roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

			x = Mathf.Lerp(x, target.x, positionLerpPct);
			y = Mathf.Lerp(y, target.y, positionLerpPct);
			z = Mathf.Lerp(z, target.z, positionLerpPct);
		}

		public void UpdateTransform(Transform t, Vector3 origin)
		{
			t.eulerAngles = new Vector3(pitch, yaw, roll);
			t.position = new Vector3(x, y, z) + origin;
		}
	}

	public enum CameraMode
	{
		free,
		pov,
		follow,
		followOrbit,
		auto,
		sideline,
		recorded,
		followSpectator,
	}

	[SerializeField] private CameraMode mode;

	public CameraMode Mode
	{
		get => mode;
		set
		{
			switch (value)
			{
				case CameraMode.free:
					cam.fieldOfView = defaultFov;
					targetCameraState.SetPosition(transform.position);
					interpolatingCameraState.SetPosition(transform.position);
					targetCameraState.SetRotation(Quaternion.LookRotation(transform.forward, Vector3.up));
					break;
				case CameraMode.pov:
					cam.fieldOfView = defaultFov;
					break;
				case CameraMode.follow:
					cam.fieldOfView = defaultFov;
					break;
				case CameraMode.followOrbit:
					cam.fieldOfView = defaultFov;
					targetCameraState.SetPosition(transform.position - playerTarget.position);
					interpolatingCameraState.SetPosition(transform.position - playerTarget.position);
					targetCameraState.SetRotation(Quaternion.LookRotation(transform.forward, Vector3.up));
					break;
				case CameraMode.auto:
					cam.fieldOfView = defaultFov;
					break;
				case CameraMode.sideline:
					break;
				case CameraMode.recorded:
					cam.fieldOfView = defaultFov;
					break;
				case CameraMode.followSpectator:
					//targetCameraState.SetPosition(spectatorCameraTarget.transform.position);
					//targetCameraState.SetRotation(spectatorCameraTarget.transform.rotation);
					cam.fieldOfView = defaultFov;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
			}

			mode = value;
		}
	}

	/// <summary>
	/// This is used when a player is clicked
	/// </summary>
	private CameraMode followCameraMode = CameraMode.followOrbit;

	public float defaultFov = 75;
	public Camera cam;

	/// <summary>
	/// The local position relative to the origin
	/// </summary>
	private readonly CameraState targetCameraState = new CameraState();

	private readonly CameraState interpolatingCameraState = new CameraState();
	private readonly CameraState directCameraState = new CameraState();

	public Transform playerTarget;
	public GameObject spectatorCameraTarget;

	public Vector3 povOffset = new Vector3(0, .6f, .3f);
	public Vector3 followCamOffset = new Vector3(0, .5f, -2f);
	public float orbitScrollSpeed = .1f;


	[Header("Sideline Camera")]
	public float discPositionSmoothness = 1f;
	public float discPositionSmoothnessDirection = 1f;
	private Vector3 smoothedDiscPos = Vector3.zero;
	private Vector3 smoothedDiscPosDirection = Vector3.zero;

	[Header("Movement Settings")] [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
	public float boost = 3.5f;

	[Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
	public float positionLerpTime = 0.2f;

	[Header("Rotation Settings")] [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
	public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

	[Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
	public float rotationLerpTime = 0.01f;

	[Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
	public bool invertY;


	private void Start()
	{
		targetCameraState.SetPosition(transform.position);
		targetCameraState.SetRotation(transform.rotation);
	}

	private void LateUpdate()
	{
		switch (mode)
		{
			case CameraMode.free:
				FreeCamMovement();
				break;
			case CameraMode.pov:
				if (playerTarget == null) Mode = CameraMode.free;
				break;
			case CameraMode.follow:
				if (playerTarget == null) Mode = CameraMode.free;
				break;
			case CameraMode.followOrbit:
				if (playerTarget == null) Mode = CameraMode.free;
				else OrbitMovement();
				break;
			case CameraMode.auto:
				break;
			case CameraMode.sideline:
				SidelineMovement();
				break;
			case CameraMode.recorded:
				RecordedCameraMovement();
				break;
			case CameraMode.followSpectator:
				if (spectatorCameraTarget.activeSelf == false) Mode = CameraMode.free;
				break;
		}

		// Framerate-independent interpolation
		// Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
		float positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
		float rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);

		interpolatingCameraState.LerpTowards(targetCameraState, positionLerpPct, rotationLerpPct);

		switch (mode)
		{
			case CameraMode.followOrbit:
				interpolatingCameraState.UpdateTransform(transform, playerTarget.position);
				break;
			case CameraMode.follow:
				targetCameraState.SetPosition(playerTarget.TransformPoint(followCamOffset));
				targetCameraState.SetRotation(playerTarget.rotation);
				targetCameraState.UpdateTransform(transform, Vector3.zero);
				break;
			case CameraMode.pov:
				targetCameraState.SetPosition(playerTarget.TransformPoint(povOffset));
				targetCameraState.SetRotation(playerTarget.rotation);
				targetCameraState.UpdateTransform(transform, Vector3.zero);
				break;
			case CameraMode.free:
			case CameraMode.auto:
			case CameraMode.sideline:
			case CameraMode.recorded:
				interpolatingCameraState.UpdateTransform(transform, Vector3.zero);
				break;
			case CameraMode.followSpectator:
				targetCameraState.SetPosition(spectatorCameraTarget.transform.position);
				targetCameraState.SetRotation(spectatorCameraTarget.transform.rotation);
				targetCameraState.UpdateTransform(transform, Vector3.zero);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void FreeCamMovement()
	{
		// Hide and lock cursor when right mouse button pressed
		if (Input.GetMouseButtonDown(1))
		{
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}

		// Unlock and show cursor when right mouse button released
		if (Input.GetMouseButtonUp(1))
		{
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
		}

		// Rotation
		if (Input.GetMouseButton(1))
		{
			Vector2 mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));
			float mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);
			targetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
			targetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
		}

		Vector2 controllerRightStick = new Vector2(Input.GetAxis("RightX"), Input.GetAxis("RightY"));
		targetCameraState.yaw += controllerRightStick.x * 1.25f;
		targetCameraState.pitch += controllerRightStick.y * 1.25f;

		// Translation
		Vector3 translation = GetInputTranslationDirection() * Time.deltaTime;

		// Speed up movement when shift key held
		if (Input.GetKey(KeyCode.LeftShift))
		{
			translation *= 10.0f;
		}

		// Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
		// boost += Input.mouseScrollDelta.y * 0.2f;
		translation *= Mathf.Pow(2.0f, boost);

		targetCameraState.Translate(translation);
	}

	private void OrbitMovement()
	{
		// Hide and lock cursor when right mouse button pressed
		if (Input.GetMouseButtonDown(1))
		{
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}

		// Unlock and show cursor when right mouse button released
		if (Input.GetMouseButtonUp(1))
		{
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
		}

		// Rotation
		if (Input.GetMouseButton(1))
		{
			Vector2 mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));
			float mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);
			targetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
			targetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
		}

		Vector2 controllerRightStick = new Vector2(Input.GetAxis("RightX"), Input.GetAxis("RightY"));
		targetCameraState.yaw += controllerRightStick.x * 1.25f;
		targetCameraState.pitch += controllerRightStick.y * 1.25f;

		// zoom
		targetCameraState.orbitDistance -= Input.mouseScrollDelta.y * orbitScrollSpeed;
		targetCameraState.orbitDistance = Mathf.Clamp(targetCameraState.orbitDistance, .1f, 20f);

		// generate the position that matches this orientation and orbitDistance
		Vector3 targetPos = transform.TransformDirection(Vector3.forward) * -targetCameraState.orbitDistance;
		targetCameraState.SetPosition(targetPos);
	}

	private void RecordedCameraMovement()
	{
		if (DemoStart.instance.playhead == null) return;
		Frame frame = DemoStart.instance.playhead.GetFrame();
		if (frame == null) return;
		targetCameraState.SetPosition(frame.player.Position);
		targetCameraState.SetRotation(frame.player.Rotation);
	}

	private void SidelineMovement()
	{
		if (DemoStart.instance.playhead == null) return;
		Frame frame = DemoStart.instance.playhead.GetFrame();
		if (frame == null) return;

		smoothedDiscPos = Vector3.Lerp(smoothedDiscPos, frame.disc.Position, Time.deltaTime * discPositionSmoothness);
		smoothedDiscPosDirection = Vector3.Lerp(smoothedDiscPosDirection, frame.disc.Position, Time.deltaTime * discPositionSmoothnessDirection);
			
		Vector3 pos = smoothedDiscPos;
		
		pos.x = Mathf.Clamp(pos.x, -24, 24);
		pos.z = 14.4f;
		pos.y = pos.x*pos.x * .009f;
		Vector3 direction = smoothedDiscPosDirection - pos;
		direction.z = -Mathf.Abs(direction.z);
		Quaternion rotation = Quaternion.LookRotation(direction);
		cam.fieldOfView = Mathf.Clamp(1/Vector3.Distance(pos, smoothedDiscPos) * 800f, 20, 90);
		SetCameraPositionLive.FOV = cam.fieldOfView;
		targetCameraState.SetPosition(pos);
		targetCameraState.SetRotation(rotation);
	}


	private static Vector3 GetInputTranslationDirection()
	{
		Vector3 direction = new Vector3();
		if (Input.GetKey(KeyCode.W))
		{
			direction += Vector3.forward;
		}

		if (Input.GetKey(KeyCode.S))
		{
			direction += Vector3.back;
		}

		if (Input.GetKey(KeyCode.A))
		{
			direction += Vector3.left;
		}

		if (Input.GetKey(KeyCode.D))
		{
			direction += Vector3.right;
		}

		if (Input.GetKey(KeyCode.Q))
		{
			direction += Vector3.down;
		}

		if (Input.GetKey(KeyCode.E))
		{
			direction += Vector3.up;
		}

		if (Input.GetButton("LeftBumper"))
		{
			direction += Vector3.down;
		}

		if (Input.GetButton("RightBumper"))
		{
			direction += Vector3.up;
		}

		direction.x += Input.GetAxis("LeftX") * 2.5f;
		direction.z += Input.GetAxis("LeftY") * -2.5f;
		return direction;
	}

	public void SetFollowCameraModeDropdown(int index)
	{
		followCameraMode = index switch
		{
			0 => CameraMode.followOrbit,
			1 => CameraMode.follow,
			2 => CameraMode.pov,
			3 => CameraMode.followSpectator,
			_ => Mode
		};

		if (playerTarget != null || followCameraMode == CameraMode.followSpectator)
		{
			Mode = followCameraMode;
		}
	}

	public void FocusPlayer(Transform playerHead = null)
	{
		playerTarget = playerHead;

		if (followCameraMode == CameraMode.followSpectator)
			Mode = CameraMode.followOrbit;
		else
			Mode = playerHead != null ? followCameraMode : CameraMode.free;
	}

	public void FocusPlayer(Player player)
	{
		FocusPlayer(DemoStart.FindPlayerObjectByName(player.name).ikController.head);
	}
}