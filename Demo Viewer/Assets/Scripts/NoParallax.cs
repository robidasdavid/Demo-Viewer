using UnityEngine;

public class NoParallax : MonoBehaviour
{
	public Transform cam;

	private void Start()
	{
		cam = Camera.main.transform;
	}

	private void LateUpdate()
	{
		if (cam != null) transform.position = cam.transform.position;
	}
}