using UnityEngine;
using unityutilities;

public class VRPlayer : MonoBehaviour
{
	public Movement m;
	public Rigidbody rb;

	public Transform startPosition;


	private bool lastGrabAir;
	private bool lastGrabWalls;
	private float lastDrag;
	private Vector3 lastScale;
	private bool goalieTrainingMode;

	public bool GoalieTrainingMode
	{
		get => goalieTrainingMode;
		set
		{
			if (value)
			{
				SaveLastValues();

				m.grabAirLeft = false;
				m.grabAirRight = false;
				m.grabWallsLeft = true;
				m.grabWallsRight = true;
				rb.drag = 0;
				transform.localScale = Vector3.one;
			}
			else
			{
				m.grabAirLeft = lastGrabAir;
				m.grabAirRight = lastGrabAir;
				m.grabWallsLeft = lastGrabWalls;
				m.grabWallsRight = lastGrabWalls;
				rb.drag = lastDrag;
				transform.localScale = lastScale;
			}
		}
	}

	// Start is called before the first frame update
	private void Start()
	{
		SaveLastValues();

		m.TeleportTo(startPosition.position, startPosition.rotation, true);
	}

	private void SaveLastValues()
	{
		// initialize the last values
		lastGrabAir = m.grabAirLeft;
		lastGrabWalls = m.grabWallsLeft;
		lastDrag = rb.drag;
		lastScale = transform.localScale;
	}
}