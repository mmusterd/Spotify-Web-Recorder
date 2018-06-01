namespace SpotifyWebRecorder
{
	public class Util
	{
		public static int GetDefaultArtQuality()
		{
			return Settings.Default.ArtQuality;
		}

		public static int GetDefaultBitrate()
		{
			return Settings.Default.Bitrate;
		}

		public static string GetDefaultDevice()
		{
			return Settings.Default.DefaultDevice;
		}

		public static bool GetDefaultMuteAdsEnabled()
		{
			return Settings.Default.MuteAds;
		}

		public static string GetDefaultOutputPath()
		{
			if (string.IsNullOrEmpty(Settings.Default.OutputPath))
			{
				Settings.Default.OutputPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic), "SpotifyRecorder");
				if (!System.IO.Directory.Exists(Settings.Default.OutputPath))
				{
					System.IO.Directory.CreateDirectory(Settings.Default.OutputPath);
				}
				Settings.Default.Save();
			}
			return Settings.Default.OutputPath;
		}

		public static int GetDefaultThreshold()
		{
			return Settings.Default.DeleteThreshold;
		}

		public static bool GetDefaultThresholdEnabled()
		{
			return Settings.Default.DeleteThresholdEnabled;
		}

		public static string GetDefaultURL()
		{
			return Settings.Default.URL;
		}

		public static string GetDefaultUserAgent()
		{
			return Settings.Default.UserAgent;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet=System.Runtime.InteropServices.CharSet.None, ExactSpelling=false)]
		public static extern System.IntPtr SendMessageW(System.IntPtr hWnd, int Msg, System.IntPtr wParam, System.IntPtr lParam);

		public static void SetDefaultArtQuality(int quality)
		{
			Settings.Default.ArtQuality = quality;
			Settings.Default.Save();
		}

		public static void SetDefaultBitrate(int bitrate)
		{
			Settings.Default.Bitrate = bitrate;
			Settings.Default.Save();
		}

		public static void SetDefaultDevice(string device)
		{
			Settings.Default.DefaultDevice = device;
			Settings.Default.Save();
		}

		public static void SetDefaultMuteAdsEnabled(bool mute)
		{
			Settings.Default.MuteAds = mute;
			Settings.Default.Save();
		}

		public static void SetDefaultOutputPath(string outputPath)
		{
			Settings.Default.OutputPath = outputPath;
			Settings.Default.Save();
		}

		public static void SetDefaultThreshold(int threshold)
		{
			Settings.Default.DeleteThreshold = threshold;
			Settings.Default.Save();
		}

		public static void SetDefaultThresholdEnabled(bool threshold)
		{
			Settings.Default.DeleteThresholdEnabled = threshold;
			Settings.Default.Save();
		}

		public static void SetDefaultURL(string url)
		{
			Settings.Default.URL = url;
			Settings.Default.Save();
		}

		public static void SetDefaultUserAgent(string ua)
		{
			Settings.Default.UserAgent = ua;
			Settings.Default.Save();
		}

		public static void ToggleMuteVolume(System.IntPtr Handle)
		{
			SendMessageW(Handle, 793, Handle, (System.IntPtr)524288);
		}
	}
}