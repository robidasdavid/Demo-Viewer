using UnityEngine;

public class SpectatorCameraVisualizer : MonoBehaviour
{
	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.playhead == null) return;
		Frame frame = DemoStart.playhead.GetFrame();
		if (frame == null) return;

		// transform.localPosition = frame.player.vr_position.ToVector3();
		// transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(frame.player.vr_forward.ToVector3(), frame.player.vr_up.ToVector3()));
		
		
		transform.localPosition = frame.player.vr_position.ToVector3();
		transform.localRotation = Quaternion.LookRotation(frame.player.vr_forward.ToVector3(), frame.player.vr_up.ToVector3());
		
		Debug.DrawRay(transform.localPosition,frame.player.vr_forward.ToVector3(), Color.blue);
		Debug.DrawRay(transform.localPosition,frame.player.vr_up.ToVector3(), Color.green);
		Debug.DrawRay(transform.localPosition,frame.player.vr_left.ToVector3(), Color.red);
		
		
		// Debug.DrawRay(frame.teams[1].players[0].Head.position.ToVector3(),frame.teams[1].players[0].Head.forward.ToVector3(), Color.blue);
		// Debug.DrawRay(frame.teams[1].players[0].Head.position.ToVector3(),frame.teams[1].players[0].Head.up.ToVector3(), Color.green);
		// Debug.DrawRay(frame.teams[1].players[0].Head.position.ToVector3(),frame.teams[1].players[0].Head.left.ToVector3(), Color.red);
	}
}