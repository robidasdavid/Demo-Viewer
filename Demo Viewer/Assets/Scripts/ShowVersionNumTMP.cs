using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ShowVersionNumTMP : MonoBehaviour
{
	private void Awake()
	{
		GetComponent<TMP_Text>().text = "v " + Application.version;
	}
}