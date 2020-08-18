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

	void Start()
	{
		stunnedInitiated = false;
	}
}
