using UnityEngine;

public class BubbleScale : MonoBehaviour
{

	// Update is called once per frame
	void Update()
	{
		transform.localScale = Vector3.one * ((Mathf.Sin(Time.time * 10) + 1) / 10 + 1);
	}
}
