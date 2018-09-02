using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProGrids.Editor
{
	[InitializeOnLoad]
	static class pg_IconUtility
	{
		public static Texture2D LoadIcon(string iconName)
		{
			var img = pg_EditorUtility.LoadInternalAsset<Texture2D>("GUI/ProGridsToggles/" + iconName);

			if(!img)
				Debug.LogError("ProGrids failed to locate menu image: " + iconName + ".\nThis can happen if the GUI folder is moved or deleted.  Deleting and re-importing ProGrids will fix this error.");

			return img;
		}
	}
}
