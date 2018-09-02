using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProGrids.Editor
{
	static class IconUtility
	{
		const string k_IconPath = "/GUI/Icons/";

		public static Texture2D LoadIcon(string iconName)
		{
			string iconPath = k_IconPath + iconName;

			var img = EditorUtility.LoadInternalAsset<Texture2D>(iconPath);

			if (!img)
				Debug.LogError("ProGrids failed to locate menu image: " + iconName +
					".\nThis can happen if the GUI folder is moved or deleted.  " +
					"Deleting and re-importing ProGrids will fix this error.\nSearching at path: " + EditorUtility.GetProGridsInstallDirectory() + iconPath);

			return img;
		}
	}
}
