using UnityEngine;

public class SpectatorCameraVisualizer : MonoBehaviour
{
	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.playhead == null) return;
		Frame frame = DemoStart.playhead.GetFrame();
		if (frame == null) return;

		transform.localPosition = frame.player.vr_position.ToVector3();
		transform.localRotation = Quaternion.LookRotation(frame.player.vr_forward.ToVector3Backwards(), frame.player.vr_up.ToVector3Backwards());
	}
}