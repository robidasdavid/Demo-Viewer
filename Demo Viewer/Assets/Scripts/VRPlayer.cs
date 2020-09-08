using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using unityutilities;

public class VRPlayer : MonoBehaviour
{
	public Movement m;
	public Rigidbody rb;


	private bool lastGrabAir;
	private bool lastGrabWalls;
	private float lastDrag;
	private Vector3 lastScale;
	private bool goalieTrainingMode;
	public bool GoalieTrainingMode {
		get => goalieTrainingMode;
		set {
			if (value)
			{
				SaveLastValues();

				m.grabAir = false;
				m.grabWalls = true;
				rb.drag = 0;
				transform.localScale = Vector3.one;
			}
			else
			{
				m.grabAir = lastGrabAir;
				m.grabWalls = lastGrabWalls;
				rb.drag = lastDrag;
				transform.localScale = lastScale;
			}
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		SaveLastValues();

	}

	private void SaveLastValues()
	{
		// initialize the last values
		lastGrabAir = m.grabAir;
		lastGrabWalls = m.grabWalls;
		lastDrag = rb.drag;
		lastScale = transform.localScale;
	}

	// Update is called once per frame
	void Update()
	{

	}
}
