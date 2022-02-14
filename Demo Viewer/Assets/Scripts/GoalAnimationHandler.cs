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
				foreach (LineRenderer line in a.GetComponentsInChildren<LineRenderer>())
				{
					line.material.SetColor(EmissionColor, colors[teamIndex]);
				}

				a.gameObject.SetActive(true);
				a.Play();
			}
		};
	}
}