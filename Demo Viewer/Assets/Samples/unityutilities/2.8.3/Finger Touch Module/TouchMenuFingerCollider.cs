using UnityEngine;
using UnityEngine.UI;
using unityutilities;

public class TouchMenuFingerCollider : MonoBehaviour
{
	public bool isLeft;
	public AudioSource audioSource;
	public Transform followCollider;    // TODO These seem to hang around, since they aren't parented anymore

	private Vector3[] lastPos = new Vector3[2];
	private int lastPosIndex;
	private bool fingerEnabled = true;


	private bool locked;
	private const float lockTime = .2f;
	private float lockTimer;
	private Button selected;

	private const float stayTime = .5f;
	private float stayTimer;

	public float maxVel = 1f;

	private Rig rig;

	private void Start()
	{
		rig = GetComponentInParent<Rig>();
		followCollider.SetParent(rig.transform);
	}

	private void FixedUpdate()
	{
		lastPos[lastPosIndex++ % lastPos.Length] = transform.position;
	}


	void Update()
	{
		lockTimer += Time.deltaTime;
	}

	private void DisableCollider(Side side)
	{
		if (side == Side.Left && isLeft)
		{
			fingerEnabled = false;
		}
		else if (side == Side.Right && !isLeft)
		{
			fingerEnabled = false;
		}
		Debug.Log("" + side + " finger disabled");
	}

	private void EnableCollider(Side side)
	{
		fingerEnabled = true;
		Debug.Log("fingers enabled");
	}

	private void OnTriggerEnter(Collider c)
	{
		if (!fingerEnabled)
			return;
		Button s = c.GetComponent<Button>();
		if (s != null)
		{
			SelectableEnter(s, this, (lastPos[lastPosIndex % lastPos.Length] - transform.position) / Time.fixedDeltaTime);
		}
	}

	private void OnTriggerExit(Collider c)
	{
		if (!fingerEnabled) return;
		Button s = c.GetComponent<Button>();
		if (s != null)
		{
			SelectableExit(s, this);
		}
	}

	void ClickSelectedButton()
	{
		selected?.onClick?.Invoke();
	}

	public void SelectableEnter(Button i, TouchMenuFingerCollider finger, Vector3 velocity)
	{
		if (lockTimer < lockTime) return;
		if (velocity.magnitude > maxVel * rig.transform.localScale.x) return;

		i.Select();
		selected = i;

		lockTimer = 0;
		stayTimer = 0;
		audioSource.Play();
		Invoke(nameof(ClickSelectedButton), .1f);

		InputMan.Vibrate(finger.isLeft ? Side.Left : Side.Right, 1);
	}

	public void SelectableExit(Button i, TouchMenuFingerCollider finger)
	{
		Invoke(nameof(SetSelectedToNull), .2f);
	}

	void SetSelectedToNull()
	{
		selected = null;
	}
}