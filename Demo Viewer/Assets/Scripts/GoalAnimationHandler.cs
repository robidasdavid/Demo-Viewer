using Spark;
using UnityEngine;

public class GoalAnimationHandler : MonoBehaviour
{
	public Animation[] animations;

	[ColorUsage(true, true)] public Color[] colors;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	// Start is called before the first frame update
	private void Start()
	{
		GameEvents.Goal += (lastScore) =>
		{
			if (!DemoStart.instance.playhead.isPlaying) return;
			
			// TODO allow goal animations on remote.
			// This doesn't fix the root issue
			if (!GameManager.instance.netFrameMan.IsLocalOrServer) return;

			int teamIndex = lastScore.team == "blue" ? 0 : 1;

			foreach (Animation a in animations)
			{
				foreach (AnimationState state in a) // Without using Animators instead of Animations, this is the best this is going to get
                {
					state.speed = DemoStart.instance.playhead.playbackMultiplier;
                }
				foreach (LineRenderer line in a.GetComponentsInChildren<LineRenderer>())
				{
					line.material.SetColor(EmissionColor, colors[teamIndex]);
				}

				a.gameObject.SetActive(true);
				a.Play();
			}
		};
	}
	private void Update()
    {
		// Deactivate the animation when regular playback stops. Necessary because goal animations use Animations instead of Animators.
		if (DemoStart.instance.playhead.isScrubbing || !DemoStart.instance.playhead.isPlaying)
		{
			foreach (Animation a in animations)
			{
				a.gameObject.SetActive(false);
			}
		}
    }
}