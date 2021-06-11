using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

public class FileAssociation
{
	public string Extension { get; set; }
	public string ProgId { get; set; }
	public string FileTypeDescription { get; set; }
	public string ExecutableFilePath { get; set; }
}

public class FileAssociations
{
	// needed so that Explorer windows get refreshed after the registry is updated
	[System.Runtime.InteropServices.DllImport("Shell32.dll")]
	private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

	private const int SHCNE_ASSOCCHANGED = 0x8000000;
	private const int SHCNF_FLUSH = 0x1000;

	public static void EnsureAssociationsSet()
	{
		ProcessModule processModule = Process.GetCurrentProcess().MainModule;
		if (processModule == null) return;
		string filePath = processModule.FileName;
		EnsureAssociationsSet(
			new FileAssociation
			{
				Extension = ".ucs",
				ProgId = "UCS_Editor_File",
				FileTypeDescription = "UCS File",
				ExecutableFilePath = filePath
			});
	}

	public static void EnsureAssociationsSet(params FileAssociation[] associations)
	{
		bool madeChanges = associations.Aggregate(false, (current, association) => current | SetAssociation(association.Extension, association.ProgId, association.FileTypeDescription, association.ExecutableFilePath));

		if (!madeChanges) return;
		SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
		SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
	}

	public static bool SetAssociation(string extension, string progId, string fileTypeDescription, string applicationFilePath)
	{
		bool madeChanges = false;
		madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
		madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, fileTypeDescription);
		madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", "\"" + applicationFilePath + "\" \"%1\"");
		return madeChanges;
	}

	private static bool SetKeyDefaultValue(string keyPath, string value)
	{
		using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
		{
			if (key?.GetValue(null) as string == value) return false;
			key?.SetValue(null, value);
			return true;
		}
	}
}