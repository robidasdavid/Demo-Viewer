using TMPro;
using UnityEngine;

public class ReplayFileInfo : MonoBehaviour
{
	private string originalFilename;
	public string OriginalFilename {
		get => originalFilename;
		set {
			originalFilename = value;
			originalFilenameText.text = value;
		}
	}
	[SerializeField]
	private TextMeshProUGUI originalFilenameText;

	private string serverFilename;
	public string ServerFilename {
		get => serverFilename;
		set {
			serverFilename = value;
		}
	}

	private string createdBy;
	public string CreatedBy {
		get => createdBy;
		set {
			createdBy = value;
			createdByText.text = value;
		}
	}
	[SerializeField]
	private TextMeshProUGUI createdByText;

	private string notes;
	public string Notes {
		get => notes;
		set {
			notes = value;
			notesText.text = value;
		}
	}
	[SerializeField]
	private TextMeshProUGUI notesText;
}
