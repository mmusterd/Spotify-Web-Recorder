namespace SpotifyWebRecorder
{
	[System.Runtime.CompilerServices.CompilerGenerated]
	[System.CodeDom.Compiler.GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
	internal sealed class Settings : System.Configuration.ApplicationSettingsBase
	{
		private static Settings defaultInstance;

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("70")]
		[System.Configuration.UserScopedSetting]
		public int ArtQuality
		{
			get
			{
				return (int)this["ArtQuality"];
			}
			set
			{
				this["ArtQuality"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("6")]
		[System.Configuration.UserScopedSetting]
		public int Bitrate
		{
			get
			{
				return (int)this["Bitrate"];
			}
			set
			{
				this["Bitrate"] = value;
			}
		}

		public static Settings Default
		{
			get
			{
				return defaultInstance;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("")]
		[System.Configuration.UserScopedSetting]
		public string DefaultDevice
		{
			get
			{
				return (string)this["DefaultDevice"];
			}
			set
			{
				this["DefaultDevice"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("30")]
		[System.Configuration.UserScopedSetting]
		public int DeleteThreshold
		{
			get
			{
				return (int)this["DeleteThreshold"];
			}
			set
			{
				this["DeleteThreshold"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("True")]
		[System.Configuration.UserScopedSetting]
		public bool DeleteThresholdEnabled
		{
			get
			{
				return (bool)this["DeleteThresholdEnabled"];
			}
			set
			{
				this["DeleteThresholdEnabled"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("True")]
		[System.Configuration.UserScopedSetting]
		public bool MuteAds
		{
			get
			{
				return (bool)this["MuteAds"];
			}
			set
			{
				this["MuteAds"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("")]
		[System.Configuration.UserScopedSetting]
		public string OutputPath
		{
			get
			{
				return (string)this["OutputPath"];
			}
			set
			{
				this["OutputPath"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("https://play.spotify.com/")]
		[System.Configuration.UserScopedSetting]
		public string URL
		{
			get
			{
				return (string)this["URL"];
			}
			set
			{
				this["URL"] = value;
			}
		}

		[System.Diagnostics.DebuggerNonUserCode]
		[System.Configuration.DefaultSettingValue("Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko")]
		[System.Configuration.UserScopedSetting]
		public string UserAgent
		{
			get
			{
				return (string)this["UserAgent"];
			}
			set
			{
				this["UserAgent"] = value;
			}
		}

		static Settings()
		{
			defaultInstance = (Settings)Synchronized(new Settings());
		}
	}
}