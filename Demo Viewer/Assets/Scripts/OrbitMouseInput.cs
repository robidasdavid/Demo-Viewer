using UnityEngine;
using unityutilities;

public class OrbitMouseInput : MonoBehaviour
{
	public float sensitivity = .5f;
	public float scrollSensitivity = .02f;
	public float pinchSensitivity = 1f;
	public Vector3 orbitAround;

	private Vector2 mousePos = Vector2.zero;

	// Update is called once per frame
	void Update()
	{
		float deltaX = Input.mousePosition.x - mousePos.x;
		float deltaY = Input.mousePosition.y - mousePos.y;

		// left click/touch
		if (Input.GetMouseButtonDown(0))
		{
			deltaX = 0;
			deltaY = 0;
		}


		// Scroll zoom
		float scrollDelta = Input.mouseScrollDelta.y;
		if (scrollDelta != 0)
		{
			transform.Translate(0, 0, scrollDelta * scrollSensitivity, Space.Self);
			transform.LookAt(orbitAround);
		}

		// Pinch to zoom
		if (Input.touchCount == 2)
		{
			// get current touch positions
			Touch t1 = Input.GetTouch(0);
			Touch t2 = Input.GetTouch(1);
			// get touch position from the previous frame
			Vector2 t1Prev = t1.position - t1.deltaPosition;
			Vector2 t2Prev = t2.position - t2.deltaPosition;

			float oldTouchDistance = Vector2.Distance(t1Prev, t2Prev);
			float currentTouchDistance = Vector2.Distance(t1.position, t2.position);

			// get offset value
			float pinchDeltaDistance = oldTouchDistance - currentTouchDistance;

			if (pinchDeltaDistance != 0)
			{
				transform.Translate(0, 0, -pinchDeltaDistance * pinchSensitivity, Space.Self);
				transform.LookAt(orbitAround);
			}
		}
		else
		{
			// left click/touch
			if (Input.GetMouseButton(0))
			{
				transform.RotateAround(orbitAround, Vector3.up, deltaX * sensitivity);
				transform.RotateAround(orbitAround, transform.right, -deltaY * sensitivity);
			}
		}


		mousePos.x = Input.mousePosition.x;
		mousePos.y = Input.mousePosition.y;
	}
}