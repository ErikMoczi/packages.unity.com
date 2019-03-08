using System;
using UnityEngine;

namespace UnityEditor.SettingsManagement.Examples
{
	static class MySettingsProvider
	{
		const string k_PreferencesPath = "Preferences/My Settings";

#if UNITY_2018_3_OR_NEWER
		[SettingsProvider]
		static SettingsProvider CreateSettingsProvider()
		{
			var provider = new UserSettingsProvider(k_PreferencesPath,
				MySettingsManager.instance,
				new [] { typeof(MySettingsProvider).Assembly });

			return provider;
		}
#else

	[NonSerialized]
	static UserSettingsProvider s_SettingsProvider;

	[PreferenceItem("ProBuilder")]
	static void ProBuilderPreferencesGUI()
	{
		if (s_SettingsProvider == null)
			s_SettingsProvider = new UserSettingsProvider(MySettingsManager.instance, new[] { typeof(MySettingsProvider).Assembly });

		s_SettingsProvider.OnGUI(null);
	}

#endif
	}
}
