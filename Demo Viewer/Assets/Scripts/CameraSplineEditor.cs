using System.Collections.Generic;
using System.Linq;
using Spark;
using UnityEngine;
using unityutilities;

public class CameraSplineEditor : MonoBehaviour
{
	public GameObject controlPointPrefab;
	private AnimationKeyframes animation;
	private readonly List<GameObject> controlPoints = new List<GameObject>();
	public AnimationKeyframes Animation
	{
		get => animation;
		set
		{
			if (animation != value)
			{
				foreach (GameObject controlPoint in controlPoints)
				{
					Destroy(controlPoint);
				}
				controlPoints.Clear();
				if (value != null)
				{
					for (int i = 0; i < value.keyframes.Count; i++)
					{
						GameObject cp = Instantiate(controlPointPrefab, transform);
						cp.name = $"Control Point {i}";
						cp.transform.localPosition = value.keyframes[i].Position.ToVector3();
						cp.transform.localRotation = value.keyframes[i].Rotation.ToQuaternion();
						controlPoints.Add(cp);
					}
				}
			}
			animation = value;
		}
	}

	public LineRenderer lineRenderer;

	// Update is called once per frame
	private void Update()
	{
		if (animation == null)
		{
			lineRenderer.positionCount = 0;
			return;
		}

		for (int i = 0; i < controlPoints.Count; i++)
		{
			animation.keyframes[i].px = controlPoints[i].transform.localPosition.x;
			animation.keyframes[i].py = controlPoints[i].transform.localPosition.y;
			animation.keyframes[i].pz = controlPoints[i].transform.localPosition.z;

			animation.keyframes[i].qx = controlPoints[i].transform.localRotation.x;
			animation.keyframes[i].qy = controlPoints[i].transform.localRotation.y;
			animation.keyframes[i].qz = controlPoints[i].transform.localRotation.z;
			animation.keyframes[i].qw = controlPoints[i].transform.localRotation.w;
		}
		
		BezierSpline spline = new BezierSpline(animation);
		List<Vector3> positions = new List<Vector3>();

		for (float t = 0; t < 1; t += .01f)
		{
			Vector3 newPos = spline.GetPoint(t).ToVector3();
			Quaternion newRot = spline.GetRotation(t).ToQuaternion();

			positions.Add(lineRenderer.transform.InverseTransformPoint(newPos));
		}

		lineRenderer.positionCount = positions.Count;
		lineRenderer.SetPositions(positions.ToArray());
	}
}