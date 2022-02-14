using MenuTablet;
using UnityEngine;

public class Hand : MonoBehaviour
{
	public GameObject normalHand;
	public SnapUICursorAlignment snapUICursor;
	
	private void OnEnable()
	{
		// make sure the tool is switched back when the tablet is hidden
		MenuTabletMover.OnHide += HideMenuTablet;
	}

	private void OnDisable()
	{
		// make sure the tool is switched back when the tablet is hidden
		MenuTabletMover.OnHide -= HideMenuTablet;
	}

	private void HideMenuTablet(MenuTabletMover _)
	{
		SnapExit();
	}

	// Update is called once per frame
	void Update()
	{
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("SnapUI"))
		{
			normalHand.gameObject.SetActive(false);
			snapUICursor.gameObject.SetActive(true);
			snapUICursor.col = other;
			DemoStart.instance.inSnapUI = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("SnapUI"))
		{
			SnapExit();
		}
	}

	private void SnapExit()
	{
		normalHand.gameObject.SetActive(true);
		snapUICursor.gameObject.SetActive(false);
		DemoStart.instance.inSnapUI = false;
	}
}