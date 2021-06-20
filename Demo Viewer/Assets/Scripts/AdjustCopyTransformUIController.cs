using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using unityutilities;

public class AdjustCopyTransformUIController : MonoBehaviour
{
	public CopyTransform positionCopyTransform;
	public CopyTransform rotationCopyTransform;

	//public Slider positionSlider;
	//public Slider rotationSlider;

	//private void Start()
	//{
	//	positionSlider
	//}

	public void SetPositionSmoothness(float value)
	{
		positionCopyTransform.smoothness = value;
	}

	public void SetRotationSmoothness(float value)
	{
		rotationCopyTransform.smoothness = value;
	}
}
