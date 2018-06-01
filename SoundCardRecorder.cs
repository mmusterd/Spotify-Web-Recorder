namespace SpotifyWebRecorder
{
    public class SoundCardRecorder : System.IDisposable
	{
		private NAudio.Wave.IWaveIn _waveIn;

		private NAudio.Wave.WaveFileWriter _writer;

		private System.DateTime _startTime;

		private int _lastRecordingDuration;

		public int Duration => _lastRecordingDuration;

		public string FilePath
		{
			get;
			set;
		}

		public string Song
		{
			get;
			set;
		}

		public SoundCardRecorder(string filePath, string song)
		{
			FilePath = filePath;
			Song = song;
			_waveIn = new NAudio.Wave.WasapiLoopbackCapture();
			_writer = new NAudio.Wave.WaveFileWriter(FilePath, _waveIn.WaveFormat);
			_waveIn.DataAvailable += OnDataAvailable;
		}

		public void Dispose()
		{
			if (_waveIn != null)
			{
				_waveIn.StopRecording();
				_waveIn.Dispose();
				_waveIn = null;
			}
			if (_writer != null)
			{
				_writer.Close();
				_writer.Dispose();
				_writer = null;
			}
		}

		private void OnDataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
		{
			if (_writer != null)
			{
				_writer.Write(e.Buffer, 0, e.BytesRecorded);
			}
		}

		public void Start()
		{
			_waveIn.StartRecording();
			_startTime = System.DateTime.Now;
		}

		public void Stop()
		{
			System.TimeSpan timeSpan = System.DateTime.Now.Subtract(_startTime);
			_lastRecordingDuration = (int)timeSpan.TotalSeconds;
			if (_waveIn != null)
			{
				_waveIn.StopRecording();
				_waveIn.Dispose();
				_waveIn = null;
			}
			if (_writer != null)
			{
				_writer.Close();
				_writer.Dispose();
				_writer = null;
			}
		}
	}
}