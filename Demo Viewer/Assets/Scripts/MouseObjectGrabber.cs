using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using unityutilities;
using unityutilities.VRInteraction;

public class MouseObjectGrabber : MonoBehaviour
{
	[Tooltip("This object doesn't move, and has canGrab set to false. It is just used to send events.")]
	public VRGrabbableHand mouseHand;
	public new Camera camera;

	private void Update()
	{
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			VRGrabbable g = hit.transform.GetComponent<VRGrabbable>();
			if (g != null)
			{
				if (Input.GetMouseButtonDown(0))
				{
					mouseHand.Grab(g);
				} else if (Input.GetMouseButtonUp(0))
				{
					mouseHand.Release();
				}
				else
				{
					g.HandleSelection();
				}
			} 
		}
	}
}
