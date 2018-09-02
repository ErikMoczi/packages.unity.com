using UnityEngine;
using System.Collections;

namespace ProGrids.Editor
{
	[System.Serializable]
	class ToggleContent
	{
		public readonly string m_TextOn, m_TextOff;
		public Texture2D image_on, image_off;
		public string tooltip;

		GUIContent gc = new GUIContent();

		public ToggleContent(string t_on, string t_off, string tooltip)
		{
			m_TextOn = t_on;
			m_TextOff = t_off;
			image_on = null;
			image_off = null;
			this.tooltip = tooltip;

			gc.tooltip = tooltip;
		}

		public ToggleContent(string t_on, string t_off, Texture2D i_on, Texture2D i_off, string tooltip)
		{
			this.m_TextOn = t_on;
			this.m_TextOff = t_off;
			this.image_on = i_on;
			this.image_off = i_off;
			this.tooltip = tooltip;

			gc.tooltip = tooltip;
		}

		public static bool ToggleButton(Rect r, ToggleContent content, bool enabled, GUIStyle imageStyle, GUIStyle altStyle)
		{
			content.gc.image = enabled ? content.image_on : content.image_off;
			content.gc.text = content.gc.image == null ? (enabled ? content.m_TextOn : content.m_TextOff) : "";

			return GUI.Button(r, content.gc, content.gc.image != null ? imageStyle : altStyle);
		}
	}
}
