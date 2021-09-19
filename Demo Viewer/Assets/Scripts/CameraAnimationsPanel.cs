using System.Collections.Generic;
using Spark;
using UnityEngine;

public class CameraAnimationsPanel : MonoBehaviour
{
	public CameraSplineManager manager;
	public Transform scrollViewContent;
	public GameObject rowPrefab;

	// Start is called before the first frame update
	private void Start()
	{
		Refresh();
	}

	// Update is called once per frame
	void Update()
	{
	}

	public void Refresh()
	{
		foreach (Transform child in scrollViewContent)
		{
			Destroy(child.gameObject);
		}
		CameraWriteSettings.Load();
		foreach (KeyValuePair<string,List<CameraTransform>> anim in CameraWriteSettings.instance.animations)
		{
			GameObject row = Instantiate(rowPrefab, scrollViewContent);
			AnimationRowController rowController = row.GetComponent<AnimationRowController>();
			rowController.nameLabel.text = anim.Key;
			rowController.loadButton.onClick.AddListener(() =>
			{
				manager.editor.Animation = anim.Value;
			});
			rowController.saveButton.onClick.AddListener(() =>
			{
				CameraWriteSettings.instance.animations[rowController.nameLabel.text] = manager.editor.Animation;
				CameraWriteSettings.instance.Save();
			});
		}
	}
}