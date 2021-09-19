using System.Collections.Generic;
using System.Linq;
using Spark;
using UnityEngine;

public class CameraSplineEditor : MonoBehaviour
{
	private List<CameraTransform> animation;
	private readonly List<GameObject> controlPoints = new List<GameObject>();
	public List<CameraTransform> Animation
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
				for (int i = 0; i < value.Count; i++)
				{
					controlPoints.Add(new GameObject($"Control Point {i}"));
					controlPoints.Last().transform.SetParent(transform);
					controlPoints.Last().transform.localPosition = value[i].position;
				}
			}
			animation = value;
		}
	}

	public LineRenderer lineRenderer;

	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	private void Update()
	{
		if (animation == null) return;

		for (int i = 0; i < controlPoints.Count; i++)
		{
			animation[i].position = controlPoints[i].transform.localPosition;
		}
		
		BezierSpline spline = new BezierSpline(animation);
		List<Vector3> positions = new List<Vector3>();

		for (float t = 0; t < 1; t += .01f)
		{
			Vector3 newPos = spline.GetPoint(t);
			Quaternion newRot = spline.GetRotation(t);

			positions.Add(lineRenderer.transform.InverseTransformPoint(newPos));
		}

		lineRenderer.positionCount = positions.Count;
		lineRenderer.SetPositions(positions.ToArray());
	}
}