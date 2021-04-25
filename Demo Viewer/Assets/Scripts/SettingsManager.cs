using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
	public static SettingsManager instance;

	private void Start()
	{
		instance = this;

	}
}