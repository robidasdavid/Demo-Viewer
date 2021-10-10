using MenuTablet;
using UnityEngine;

public class Hand : MonoBehaviour
{
	public GameObject normalHand;
	public SnapUICursorAlignment snapUICursor;
	
	private void Start()
	{
		// make sure the tool is switched back when the tablet is hidden
		MenuTabletMover.OnHide += (_) =>
		{
			normalHand.gameObject.SetActive(true);
			snapUICursor.gameObject.SetActive(false);
		};
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
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("SnapUI"))
		{
			normalHand.gameObject.SetActive(true);
			snapUICursor.gameObject.SetActive(false);
		}
	}
}