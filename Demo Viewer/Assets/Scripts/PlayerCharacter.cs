using UnityEngine;
using UnityEngine.UI;

public class PlayerCharacter : MonoBehaviour
{
	public GameObject boost;
	public GameObject playerShield;

	public GameObject lftForearm;
	public GameObject rtForearm;
	
	public bool stunnedInitiated;
	public Text playerName;
	public IKController ikController;
	public PlayerStatsHover hoverStats;
	public Transform playspaceVisualizer;
	public TrailRenderer trailRenderer;

	public Vector3 PlayspaceLocation
	{
		set
		{
			playspaceVisualizer.localPosition = value;
		}
	}

	void Start()
	{
		stunnedInitiated = false;
	}
}
