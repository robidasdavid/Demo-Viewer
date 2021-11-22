using System.Linq;
using Spark;
using UnityEngine;

public class CameraSplineManager : MonoBehaviour
{
	public CameraSplineEditor editor;
	public int animationIndex;

	private void Awake()
	{
		// CameraWriteSettings.Load();
	}

	private void Start()
	{
	}

	// Update is called once per frame
	private void Update()
	{
		// editor.Animation = CameraWriteSettings.instance.animations.Values.ToArray()[animationIndex];
	}
	
	
}