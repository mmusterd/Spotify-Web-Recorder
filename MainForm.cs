using Enumerable = System.Linq.Enumerable;

namespace SpotifyWebRecorder
{
    public class MainForm : System.Windows.Forms.Form
	{
		private readonly SpotifyAPI.Local.SpotifyLocalAPI _spotify;

		private bool _isConnected;

		private SoundCardRecorder _soundCardRecorder;

		private string _baseDir;

		private System.Windows.Forms.FolderBrowserDialog _folderDialog;

		public int SongCountToSave;

		private int _duration;

		private string _logPath;

		private System.IO.StreamWriter _writer;

		private int _albumArtQuality;

		private Mp3Tag _currentTag;

		private Mp3Tag _recordingTag;

		private SpotifyAPI.Local.Models.Track _currentTrack;

		private System.DateTime _programStartingTime;

		private int _counter;

		private string _currentSongName;

		private string _oldSongName;

		private System.TimeSpan _time;

		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButton_Back;
		private System.Windows.Forms.ToolStripButton toolStripButton_Home;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Play;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Open;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Delete;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_ClearList;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton toolStripButtonHideSidebar;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.PictureBox pictureBoxAlbumCover;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Label timeLabel;
		private System.Windows.Forms.Label label231;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label nowPlayingLabel;
		private System.Windows.Forms.Label encodingLabel;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label songLabel;
		private System.Windows.Forms.LinkLabel donateLink;
		private System.Windows.Forms.Button buttonStartRecording;
		private System.Windows.Forms.Label versionLabel;
		private System.Windows.Forms.Button buttonStopRecording;
		private System.Windows.Forms.ListBox listBoxRecordings;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label versionLabel2;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label clientVersionLabel;
		private System.Windows.Forms.Button openRecordingDevicesButton;
		private System.Windows.Forms.Button openMixerButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox deviceListBox;
		private System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox outputFolderTextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox bitrateComboBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TabPage tabPageLog;
		private System.Windows.Forms.ListBox listBoxLog;
		private System.Windows.Forms.TextBox textBoxSongLimit;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.LinkLabel linkLabelAbout;
		private System.Windows.Forms.LinkLabel linkLabelHelp;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.TextBox textBoxAlbumArtQuality;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label labelShuffle;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.CheckBox checkBoxShutdown;

		public MainForm()
		{
			_spotify = new SpotifyAPI.Local.SpotifyLocalAPI(50);
			_spotify.OnPlayStateChange += _spotify_OnPlayStateChange;
			_spotify.OnTrackChange += _spotify_OnTrackChange;
			_spotify.OnTrackTimeChange += _spotify_OnTrackTimeChange;
			initVariables();
			_writer = new System.IO.StreamWriter(_logPath);
			InitializeComponent();
			WindowState = System.Windows.Forms.FormWindowState.Normal;
			Connect();
			if (System.Environment.OSVersion.Version.Major < 6)
			{
				System.Windows.Forms.MessageBox.Show(Properties.Resources.Windows7OrLater);
				Close();
			}
			Load += OnLoad;
			Closing += OnClosing;
			addToLog(_programStartingTime.ToString("- dd/MM/yy HH:mm:ss -"));
			addToLog("Application started...");
			if (_isConnected)
			{
				if (!_spotify.GetStatus().Shuffle)
				{
					labelShuffle.Visible = false;
				}
				else
				{
					labelShuffle.Visible = true;
				}
				UpdateTag();
				_recordingTag = _currentTag.Clone();
				nowPlayingLabel.Text = _currentSongName;
			}
        }

		public void _spotify_OnPlayStateChange(object sender, SpotifyAPI.Local.PlayStateEventArgs e)
		{
			if (!InvokeRequired)
			{
				return;
			}
			Invoke(new System.Action(() => _spotify_OnPlayStateChange(sender, e)));
		}

		public void _spotify_OnTrackChange(object sender, SpotifyAPI.Local.TrackChangeEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new System.Action(() => _spotify_OnTrackChange(sender, e)));
				return;
			}
			if (_spotify.GetStatus().Playing)
			{
				_spotify.Pause();
			}
			UpdateTrack(e.NewTrack);
			if (!buttonStartRecording.Enabled)
			{
				if (SongCountToSave == 0 || SongCountToSave - 1 > _counter)
				{
					if (e.NewTrack.IsAd())
					{
						addToLog("Advert Playing...");
						StopRecording();
						if (!_spotify.GetStatus().Playing)
						{
							_spotify.Play();
						}
						PostProccessMain(e.OldTrack);
						_recordingTag = _currentTag.Clone();
						return;
					}
					if (e.NewTrack.TrackType.Equals("local"))
					{
						addToLog("Local Song Playing...");
						StopRecording();
						if (!_spotify.GetStatus().Playing)
						{
							_spotify.Play();
						}
						PostProccessMain(e.OldTrack);
						_recordingTag = _currentTag.Clone();
						return;
					}
					if (_spotify.GetStatus().Playing)
					{
						_spotify.Pause();
					}
					StopRecording();
					StartRecording();
					_spotify.Play();
					PostProccessMain(e.OldTrack);
					_recordingTag = _currentTag.Clone();
					_counter++;
					return;
				}
				buttonStopRecording.PerformClick();
				if (checkBoxShutdown.CheckState == System.Windows.Forms.CheckState.Checked)
				{
					System.Threading.Thread.Sleep(20000);
					Close();
					System.Diagnostics.Process shutdown = new System.Diagnostics.Process();
					shutdown.StartInfo.FileName = "shutdown -s -t 3";
					if (checkBoxShutdown.CheckState == System.Windows.Forms.CheckState.Checked)
					{
						shutdown.Start();
					}
				}
			}
		}

		public void _spotify_OnTrackTimeChange(object sender, SpotifyAPI.Local.TrackTimeChangeEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new System.Action(() => _spotify_OnTrackTimeChange(sender, e)));
				return;
			}
			_time = System.TimeSpan.FromSeconds(e.TrackTime);
			timeLabel.Text = _time.ToString("hh\\:mm\\:ss");
			if (_spotify.GetStatus().Shuffle)
			{
				labelShuffle.Visible = true;
				return;
			}
			labelShuffle.Visible = false;
		}

		private void AddAllRecordingToList()
		{
			string[] songs = System.IO.Directory.GetFiles(outputFolderTextBox.Text);
			for (int i = 0; i < songs.Length; i++)
			{
				if (System.IO.Path.GetExtension(songs[i]).Equals(".mp3"))
				{
					AddSongToList(removePathInfo(songs[i], outputFolderTextBox.Text));
				}
			}
		}

		private void AddSongToList(string song)
		{
			if (InvokeRequired)
			{
				Invoke(new System.Windows.Forms.MethodInvoker(() => AddSongToList(song)));
				return;
			}
			int newItemIndex = listBoxRecordings.Items.Add(song);
			listBoxRecordings.SelectedIndex = newItemIndex;
			encodingLabel.Text = "";
		}

		private void addToLog(string text)
		{
			if (InvokeRequired)
			{
				Invoke(new System.Windows.Forms.MethodInvoker(() => addToLog(text)));
				return;
			}
			System.Windows.Forms.ListBox.ObjectCollection items = listBoxLog.Items;
			System.DateTime now = System.DateTime.Now;
			items.Add(string.Concat("[", now.ToShortTimeString(), "] ", text));
			listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
			_writer.WriteLine(text);
		}

		private void BrowseButtonClick(object sender, System.EventArgs e)
		{
			if (_folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				outputFolderTextBox.Text = _folderDialog.SelectedPath;
				Util.SetDefaultOutputPath(_folderDialog.SelectedPath);
			}
		}

		private void ButtonStartRecordingClick(object sender, System.EventArgs e)
		{
			_counter = 0;
			songLabel.Visible = true;
			StartRecording();
			buttonStartRecording.Enabled = false;
			buttonStopRecording.Enabled = true;
		}

		private void ButtonStopRecordingClick(object sender, System.EventArgs e)
		{
			songLabel.Visible = false;
			StopRecording();
			if (_currentTrack != null)
			{
				PostProccessMain(_currentTrack);
			}
            buttonStartRecording.Enabled = true;
			buttonStopRecording.Enabled = false;
		}

        private void CompressImage(System.Drawing.Image sourceImage, int imageQuality, string savePath)
        {
            System.Drawing.Imaging.ImageCodecInfo jpegCodec = null;
            System.Drawing.Imaging.EncoderParameter imageQualitysParameter = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, imageQuality);
            System.Drawing.Imaging.ImageCodecInfo[] alleCodecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            System.Drawing.Imaging.EncoderParameters codecParameter = new System.Drawing.Imaging.EncoderParameters(1);
            codecParameter.Param[0] = imageQualitysParameter;
            int i = 0;
            while (i < alleCodecs.Length)
            {
                if (alleCodecs[i].MimeType != "image/jpeg")
                {
                    i++;
                }
                else
                {
                    jpegCodec = alleCodecs[i];
                    break;
                }
            }

            if (jpegCodec != null)
            {
                sourceImage.Save(savePath, jpegCodec, codecParameter);
            }
        }

        public void Connect()
		{
			if (!SpotifyAPI.Local.SpotifyLocalAPI.IsSpotifyRunning())
			{
				System.Windows.Forms.MessageBox.Show(Properties.Resources.SpotifyNotRunningMsg);
				return;
			}
			if (!SpotifyAPI.Local.SpotifyLocalAPI.IsSpotifyWebHelperRunning())
			{
				System.Windows.Forms.MessageBox.Show(Properties.Resources.SpotifyWebHelperNotRunningMsg);
				return;
			}
			if (!_spotify.Connect())
			{
				if (System.Windows.Forms.MessageBox.Show(Properties.Resources.CouldntConnectMsg, Properties.Resources.SpotifyMsgHeader, System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
				{
					Connect();
				}
				return;
			}
			UpdateInfos();
			_spotify.ListenForEvents = true;
			_isConnected = true;
			addToLog("The Program connected to Spotify App Successfully!");
		}

		private void ConvertToMp3(string songName, string bitrate, SpotifyAPI.Local.Models.Track oldTrack)
		{
			if (!System.IO.File.Exists(CreateOutputFileName(songName, "wav")))
			{
				System.Windows.Forms.MessageBox.Show(Properties.Resources.WavFileCouldNotBeFound);
				return;
			}
			addToLog("Converting to mp3... ");
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			if (buttonStopRecording.Enabled)
			{
				UpdateRecordingTag(oldTrack);
			}
			SaveJpeg(_albumArtQuality, oldTrack.GetAlbumArt(SpotifyAPI.Local.Enums.AlbumArtSize.Size640));
			process.StartInfo.FileName = "lame.exe";
			process.StartInfo.Arguments = string.Format("{2} --tt \"{3}\" --ta \"{4}\" --tc \"{5}\" --tl \"{6}\" --ti \"{7}\" \"{0}\" \"{1}\"", CreateOutputFileName(songName, "wav"), CreateOutputFileName(songName, "mp3"), bitrate, _recordingTag.Title, _recordingTag.Artist, _recordingTag.TrackUri, _recordingTag.AlbumName, System.IO.Path.Combine(outputFolderTextBox.Text, "Album_Art.Png"));
			process.StartInfo.WorkingDirectory = (new System.IO.FileInfo(System.Windows.Forms.Application.ExecutablePath)).DirectoryName;
			addToLog("Starting LAME...");
			process.Start();
			process.WaitForExit();
			addToLog(string.Concat("  LAME exit code: ", process.ExitCode));
			if (!process.HasExited)
			{
				addToLog("Killing LAME process!");
				process.Kill();
			}
			addToLog("LAME finished!");
			addToLog("Deleting wav file... ");
			System.IO.File.Delete(CreateOutputFileName(songName, "wav"));
			addToLog("Deleting Album_Art.Png file... ");
			System.IO.File.Delete(System.IO.Path.Combine(outputFolderTextBox.Text, "Album_Art.Png"));
			addToLog(string.Concat("Mp3 ready: ", CreateOutputFileName(songName, "mp3")));
			AddSongToList(string.Concat(songName, ".mp3"));
		}

		private string CreateOutputFileName(string song, string extension)
		{
			song = RemoveInvalidFilePathCharacters(song, string.Empty);
			return System.IO.Path.Combine(outputFolderTextBox.Text, string.Format("{0}.{1}", song, extension));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			splitContainer1 = new System.Windows.Forms.SplitContainer();
			tabControl1 = new System.Windows.Forms.TabControl();
			tabPage1 = new System.Windows.Forms.TabPage();
			labelShuffle = new System.Windows.Forms.Label();
			label15 = new System.Windows.Forms.Label();
			label14 = new System.Windows.Forms.Label();
			textBoxSongLimit = new System.Windows.Forms.TextBox();
			timeLabel = new System.Windows.Forms.Label();
			label231 = new System.Windows.Forms.Label();
			label13 = new System.Windows.Forms.Label();
			nowPlayingLabel = new System.Windows.Forms.Label();
			encodingLabel = new System.Windows.Forms.Label();
			label12 = new System.Windows.Forms.Label();
			label11 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			linkLabel1 = new System.Windows.Forms.LinkLabel();
			label5 = new System.Windows.Forms.Label();
			songLabel = new System.Windows.Forms.Label();
			donateLink = new System.Windows.Forms.LinkLabel();
			buttonStartRecording = new System.Windows.Forms.Button();
			versionLabel = new System.Windows.Forms.Label();
			buttonStopRecording = new System.Windows.Forms.Button();
			listBoxRecordings = new System.Windows.Forms.ListBox();
			contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
			toolStripMenuItem_Open = new System.Windows.Forms.ToolStripMenuItem();
			toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem_Play = new System.Windows.Forms.ToolStripMenuItem();
			toolStripMenuItem_Delete = new System.Windows.Forms.ToolStripMenuItem();
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			toolStripMenuItem_ClearList = new System.Windows.Forms.ToolStripMenuItem();
			tabPage2 = new System.Windows.Forms.TabPage();
			checkBoxShutdown = new System.Windows.Forms.CheckBox();
			label18 = new System.Windows.Forms.Label();
			label17 = new System.Windows.Forms.Label();
			label16 = new System.Windows.Forms.Label();
			textBoxAlbumArtQuality = new System.Windows.Forms.TextBox();
			label9 = new System.Windows.Forms.Label();
			linkLabelAbout = new System.Windows.Forms.LinkLabel();
			linkLabelHelp = new System.Windows.Forms.LinkLabel();
			label10 = new System.Windows.Forms.Label();
			versionLabel2 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			clientVersionLabel = new System.Windows.Forms.Label();
			openRecordingDevicesButton = new System.Windows.Forms.Button();
			openMixerButton = new System.Windows.Forms.Button();
			label2 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			deviceListBox = new System.Windows.Forms.ComboBox();
			browseButton = new System.Windows.Forms.Button();
			label3 = new System.Windows.Forms.Label();
			outputFolderTextBox = new System.Windows.Forms.TextBox();
			label4 = new System.Windows.Forms.Label();
			bitrateComboBox = new System.Windows.Forms.ComboBox();
			label7 = new System.Windows.Forms.Label();
			tabPageLog = new System.Windows.Forms.TabPage();
			listBoxLog = new System.Windows.Forms.ListBox();
			pictureBoxAlbumCover = new System.Windows.Forms.PictureBox();
			toolStrip1 = new System.Windows.Forms.ToolStrip();
			toolStripButton_Back = new System.Windows.Forms.ToolStripButton();
			toolStripButton_Home = new System.Windows.Forms.ToolStripButton();
			toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			toolStripButtonHideSidebar = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			tabControl1.SuspendLayout();
			tabPage1.SuspendLayout();
			contextMenuStrip1.SuspendLayout();
			tabPage2.SuspendLayout();
			tabPageLog.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)pictureBoxAlbumCover).BeginInit();
			toolStrip1.SuspendLayout();
			SuspendLayout();
			splitContainer1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			splitContainer1.Location = new System.Drawing.Point(0, 28);
			splitContainer1.Name = "splitContainer1";
			splitContainer1.Panel1.Controls.Add(tabControl1);
			splitContainer1.Panel2.BackColor = System.Drawing.SystemColors.Control;
			splitContainer1.Panel2.Controls.Add(pictureBoxAlbumCover);
			splitContainer1.Size = new System.Drawing.Size(919, 638);
			splitContainer1.SplitterDistance = 253;
			splitContainer1.SplitterWidth = 6;
			splitContainer1.TabIndex = 0;
			tabControl1.Controls.Add(tabPage1);
			tabControl1.Controls.Add(tabPage2);
			tabControl1.Controls.Add(tabPageLog);
			tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			tabControl1.Location = new System.Drawing.Point(0, 0);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new System.Drawing.Size(253, 638);
			tabControl1.TabIndex = 79;
			tabPage1.Controls.Add(labelShuffle);
			tabPage1.Controls.Add(label15);
			tabPage1.Controls.Add(label14);
			tabPage1.Controls.Add(textBoxSongLimit);
			tabPage1.Controls.Add(timeLabel);
			tabPage1.Controls.Add(label231);
			tabPage1.Controls.Add(label13);
			tabPage1.Controls.Add(nowPlayingLabel);
			tabPage1.Controls.Add(encodingLabel);
			tabPage1.Controls.Add(label12);
			tabPage1.Controls.Add(label11);
			tabPage1.Controls.Add(label6);
			tabPage1.Controls.Add(linkLabel1);
			tabPage1.Controls.Add(label5);
			tabPage1.Controls.Add(songLabel);
			tabPage1.Controls.Add(donateLink);
			tabPage1.Controls.Add(buttonStartRecording);
			tabPage1.Controls.Add(versionLabel);
			tabPage1.Controls.Add(buttonStopRecording);
			tabPage1.Controls.Add(listBoxRecordings);
			tabPage1.Location = new System.Drawing.Point(4, 22);
			tabPage1.Name = "tabPage1";
			tabPage1.Padding = new System.Windows.Forms.Padding(3);
			tabPage1.Size = new System.Drawing.Size(245, 612);
			tabPage1.TabIndex = 0;
			tabPage1.Text = Properties.Resources.MainForm_InitializeComponent__Record_;
			tabPage1.UseVisualStyleBackColor = true;
			labelShuffle.AutoSize = true;
			labelShuffle.Location = new System.Drawing.Point(162, 155);
			labelShuffle.Name = "labelShuffle";
			labelShuffle.Size = new System.Drawing.Size(74, 13);
			labelShuffle.TabIndex = 89;
			labelShuffle.Text = Properties.Resources.MainForm_InitializeComponent__Shuffle_is_on__;
			label15.AutoSize = true;
			label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label15.Location = new System.Drawing.Point(163, 127);
			label15.Name = "label15";
			label15.Size = new System.Drawing.Size(72, 12);
			label15.TabIndex = 88;
			label15.Text = Properties.Resources.MainForm_InitializeComponent___0_for_Limitless__;
			label14.AutoSize = true;
			label14.Location = new System.Drawing.Point(12, 127);
			label14.Name = "label14";
			label14.Size = new System.Drawing.Size(106, 13);
			label14.TabIndex = 87;
			label14.Text = Properties.Resources.MainForm_InitializeComponent_Song_Count_to_Save_;
			textBoxSongLimit.Location = new System.Drawing.Point(125, 123);
			textBoxSongLimit.Name = "textBoxSongLimit";
			textBoxSongLimit.Size = new System.Drawing.Size(32, 20);
			textBoxSongLimit.TabIndex = 86;
			textBoxSongLimit.Text = "0";
			textBoxSongLimit.TextChanged += textBoxSongLimit_TextChanged;
			timeLabel.AutoSize = true;
			timeLabel.Location = new System.Drawing.Point(51, 106);
			timeLabel.Name = "timeLabel";
			timeLabel.Size = new System.Drawing.Size(16, 13);
			timeLabel.TabIndex = 84;
			timeLabel.Text = "...";
			label231.AutoSize = true;
			label231.Location = new System.Drawing.Point(12, 106);
			label231.Name = "label231";
			label231.Size = new System.Drawing.Size(33, 13);
			label231.TabIndex = 83;
			label231.Text = Properties.Resources.MainForm_InitializeComponent_Time_;
			label13.AutoSize = true;
			label13.Location = new System.Drawing.Point(12, 84);
			label13.Name = "label13";
			label13.Size = new System.Drawing.Size(69, 13);
			label13.TabIndex = 82;
			label13.Text = Properties.Resources.MainForm_InitializeComponent_Now_Playing_;
			nowPlayingLabel.AutoSize = true;
			nowPlayingLabel.Location = new System.Drawing.Point(80, 84);
			nowPlayingLabel.Name = "nowPlayingLabel";
			nowPlayingLabel.Size = new System.Drawing.Size(16, 13);
			nowPlayingLabel.TabIndex = 81;
			nowPlayingLabel.Text = "...";
			encodingLabel.AutoSize = true;
			encodingLabel.Location = new System.Drawing.Point(73, 61);
			encodingLabel.Name = "encodingLabel";
			encodingLabel.Size = new System.Drawing.Size(16, 13);
			encodingLabel.TabIndex = 80;
			encodingLabel.Text = "...";
			label12.AutoSize = true;
			label12.Location = new System.Drawing.Point(8, 155);
			label12.Name = "label12";
			label12.Size = new System.Drawing.Size(106, 13);
			label12.TabIndex = 79;
			label12.Text = Properties.Resources.MainForm_InitializeComponent_Finished_Recordings_;
			label11.AutoSize = true;
			label11.Location = new System.Drawing.Point(12, 61);
			label11.Name = "label11";
			label11.Size = new System.Drawing.Size(55, 13);
			label11.TabIndex = 78;
			label11.Text = Properties.Resources.MainForm_InitializeComponent_Encoding_;
			label6.AutoSize = true;
			label6.Location = new System.Drawing.Point(12, 42);
			label6.Name = "label6";
			label6.Size = new System.Drawing.Size(84, 13);
			label6.TabIndex = 76;
			label6.Text = Properties.Resources.MainForm_InitializeComponent_Now_Recording_;
			linkLabel1.ActiveLinkColor = System.Drawing.Color.Gray;
			linkLabel1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			linkLabel1.AutoSize = true;
			linkLabel1.LinkColor = System.Drawing.Color.FromArgb(64, 64, 64);
			linkLabel1.Location = new System.Drawing.Point(28, 691);
			linkLabel1.Name = "linkLabel1";
			linkLabel1.Size = new System.Drawing.Size(181, 13);
			linkLabel1.TabIndex = 75;
			linkLabel1.TabStop = true;
			linkLabel1.Text = "https://github.com/mmusterd/Spotify-Web-Recorder";
			label5.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			label5.AutoSize = true;
			label5.Location = new System.Drawing.Point(28, 668);
			label5.Name = "label5";
			label5.Size = new System.Drawing.Size(210, 13);
			label5.TabIndex = 74;
			label5.Text = Properties.Resources.MainForm_InitializeComponent_For_more_information__check_the_online_help;
			songLabel.AutoSize = true;
			songLabel.Location = new System.Drawing.Point(102, 42);
			songLabel.Name = "songLabel";
			songLabel.Size = new System.Drawing.Size(16, 13);
			songLabel.TabIndex = 58;
			songLabel.Text = "...";
			donateLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
			donateLink.AutoSize = true;
			donateLink.BackColor = System.Drawing.SystemColors.Control;
			donateLink.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			donateLink.LinkColor = System.Drawing.Color.FromArgb(64, 64, 64);
			donateLink.Location = new System.Drawing.Point(45, 716);
			donateLink.Name = "donateLink";
			donateLink.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			donateLink.Size = new System.Drawing.Size(100, 13);
			donateLink.TabIndex = 73;
			donateLink.TabStop = true;
			donateLink.Text = Properties.Resources.MainForm_InitializeComponent_Donate_with_PayPal;
			donateLink.VisitedLinkColor = System.Drawing.Color.FromArgb(64, 64, 64);
			buttonStartRecording.Image = (System.Drawing.Image)resources.GetObject("buttonStartRecording.Image");
			buttonStartRecording.Location = new System.Drawing.Point(8, 6);
			buttonStartRecording.Name = "buttonStartRecording";
			buttonStartRecording.Size = new System.Drawing.Size(112, 29);
			buttonStartRecording.TabIndex = 65;
			buttonStartRecording.Text = Properties.Resources.MainForm_InitializeComponent_Start_Monitoring;
			buttonStartRecording.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			buttonStartRecording.UseVisualStyleBackColor = true;
			buttonStartRecording.Click += ButtonStartRecordingClick;
			versionLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			versionLabel.AutoSize = true;
			versionLabel.Location = new System.Drawing.Point(28, 716);
			versionLabel.Name = "versionLabel";
			versionLabel.Size = new System.Drawing.Size(35, 13);
			versionLabel.TabIndex = 72;
			versionLabel.Text = "";
			buttonStopRecording.Image = (System.Drawing.Image)resources.GetObject("buttonStopRecording.Image");
			buttonStopRecording.Location = new System.Drawing.Point(126, 6);
			buttonStopRecording.Name = "buttonStopRecording";
			buttonStopRecording.Size = new System.Drawing.Size(112, 29);
			buttonStopRecording.TabIndex = 63;
			buttonStopRecording.Text = Properties.Resources.MainForm_InitializeComponent_Stop_Monitoring;
			buttonStopRecording.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			buttonStopRecording.UseVisualStyleBackColor = true;
			buttonStopRecording.Click += ButtonStopRecordingClick;
			listBoxRecordings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			listBoxRecordings.ContextMenuStrip = contextMenuStrip1;
			listBoxRecordings.FormattingEnabled = true;
			listBoxRecordings.IntegralHeight = false;
			listBoxRecordings.Location = new System.Drawing.Point(8, 173);
			listBoxRecordings.Name = "listBoxRecordings";
			listBoxRecordings.ScrollAlwaysVisible = true;
			listBoxRecordings.Size = new System.Drawing.Size(231, 431);
			listBoxRecordings.TabIndex = 66;
			contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripMenuItem_Open, toolStripSeparator2, toolStripMenuItem_Play, toolStripMenuItem_Delete, toolStripSeparator1, toolStripMenuItem_ClearList });
			contextMenuStrip1.Name = "contextMenuStrip1";
			contextMenuStrip1.Size = new System.Drawing.Size(177, 104);
			toolStripMenuItem_Open.Name = "toolStripMenuItem_Open";
			toolStripMenuItem_Open.Size = new System.Drawing.Size(176, 22);
			toolStripMenuItem_Open.Text = Properties.Resources.MainForm_InitializeComponent_Open_output_folder;
			toolStripMenuItem_Open.Click += toolStripMenuItem_Open_Click;
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new System.Drawing.Size(173, 6);
			toolStripMenuItem_Play.Name = "toolStripMenuItem_Play";
			toolStripMenuItem_Play.Size = new System.Drawing.Size(176, 22);
			toolStripMenuItem_Play.Text = Properties.Resources.MainForm_InitializeComponent_Play_selected;
			toolStripMenuItem_Play.Click += toolStripMenuItem_Play_Click;
			toolStripMenuItem_Delete.Name = "toolStripMenuItem_Delete";
			toolStripMenuItem_Delete.Size = new System.Drawing.Size(176, 22);
			toolStripMenuItem_Delete.Text = Properties.Resources.MainForm_InitializeComponent_Delete_selected;
			toolStripMenuItem_Delete.Click += toolStripMenuItem_Delete_Click;
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new System.Drawing.Size(173, 6);
			toolStripMenuItem_ClearList.Name = "toolStripMenuItem_ClearList";
			toolStripMenuItem_ClearList.Size = new System.Drawing.Size(176, 22);
			toolStripMenuItem_ClearList.Text = Properties.Resources.MainForm_InitializeComponent_Clear_List;
			toolStripMenuItem_ClearList.Click += toolStripMenuItem_ClearList_Click;
			tabPage2.Controls.Add(checkBoxShutdown);
			tabPage2.Controls.Add(label18);
			tabPage2.Controls.Add(label17);
			tabPage2.Controls.Add(label16);
			tabPage2.Controls.Add(textBoxAlbumArtQuality);
			tabPage2.Controls.Add(label9);
			tabPage2.Controls.Add(linkLabelAbout);
			tabPage2.Controls.Add(linkLabelHelp);
			tabPage2.Controls.Add(label10);
			tabPage2.Controls.Add(versionLabel2);
			tabPage2.Controls.Add(label8);
			tabPage2.Controls.Add(clientVersionLabel);
			tabPage2.Controls.Add(openRecordingDevicesButton);
			tabPage2.Controls.Add(openMixerButton);
			tabPage2.Controls.Add(label2);
			tabPage2.Controls.Add(label1);
			tabPage2.Controls.Add(deviceListBox);
			tabPage2.Controls.Add(browseButton);
			tabPage2.Controls.Add(label3);
			tabPage2.Controls.Add(outputFolderTextBox);
			tabPage2.Controls.Add(label4);
			tabPage2.Controls.Add(bitrateComboBox);
			tabPage2.Controls.Add(label7);
			tabPage2.Location = new System.Drawing.Point(4, 22);
			tabPage2.Name = "tabPage2";
			tabPage2.Padding = new System.Windows.Forms.Padding(3);
			tabPage2.Size = new System.Drawing.Size(245, 612);
			tabPage2.TabIndex = 1;
			tabPage2.Text = Properties.Resources.MainForm_InitializeComponent__Settings_;
			tabPage2.UseVisualStyleBackColor = true;
			checkBoxShutdown.AutoSize = true;
			checkBoxShutdown.Location = new System.Drawing.Point(23, 351);
			checkBoxShutdown.Name = "checkBoxShutdown";
			checkBoxShutdown.Size = new System.Drawing.Size(191, 17);
			checkBoxShutdown.TabIndex = 99;
			checkBoxShutdown.Text = Properties.Resources.MainForm_InitializeComponent_Shutdown_after_reaching_song_limit_;
			checkBoxShutdown.UseVisualStyleBackColor = true;
			label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label18.Location = new System.Drawing.Point(3, 274);
			label18.Name = "label18";
			label18.Size = new System.Drawing.Size(236, 74);
			label18.TabIndex = 98;
			label18.Text = Properties.Resources.NoteTxt;
			label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label17.Location = new System.Drawing.Point(10, 212);
			label17.Name = "label17";
			label17.Size = new System.Drawing.Size(68, 16);
			label17.TabIndex = 97;
			label17.Text = Properties.Resources.MainForm_InitializeComponent__Sound_Quality_;
			label17.Visible = false;
			label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label16.Location = new System.Drawing.Point(157, 231);
			label16.Name = "label16";
			label16.Size = new System.Drawing.Size(75, 13);
			label16.TabIndex = 96;
			label16.Text = Properties.Resources.MainForm_InitializeComponent__Between_0_100_;
			label16.Visible = false;
			textBoxAlbumArtQuality.Location = new System.Drawing.Point(105, 228);
			textBoxAlbumArtQuality.Name = "textBoxAlbumArtQuality";
			textBoxAlbumArtQuality.Size = new System.Drawing.Size(42, 20);
			textBoxAlbumArtQuality.TabIndex = 95;
			textBoxAlbumArtQuality.Text = "100";
			textBoxAlbumArtQuality.TextChanged += textBoxAlbumArtQuality_TextChanged;
			label9.AutoSize = true;
			label9.Location = new System.Drawing.Point(9, 231);
			label9.Name = "label9";
			label9.Size = new System.Drawing.Size(90, 13);
			label9.TabIndex = 94;
			label9.Text = Properties.Resources.MainForm_InitializeComponent_Album_Art_Quality_;
			linkLabelAbout.AutoSize = true;
			linkLabelAbout.Font = new System.Drawing.Font("Microsoft Sans Serif", 15f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			linkLabelAbout.Location = new System.Drawing.Point(83, 399);
			linkLabelAbout.Name = "linkLabelAbout";
			linkLabelAbout.Size = new System.Drawing.Size(64, 25);
			linkLabelAbout.TabIndex = 93;
			linkLabelAbout.TabStop = true;
			linkLabelAbout.Text = Properties.Resources.MainForm_InitializeComponent_About;
			linkLabelAbout.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			linkLabelAbout.LinkClicked += linkLabelAbout_LinkClicked;
			linkLabelHelp.AutoSize = true;
			linkLabelHelp.Font = new System.Drawing.Font("Microsoft Sans Serif", 15f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			linkLabelHelp.Location = new System.Drawing.Point(90, 369);
			linkLabelHelp.Name = "linkLabelHelp";
			linkLabelHelp.Size = new System.Drawing.Size(52, 25);
			linkLabelHelp.TabIndex = 92;
			linkLabelHelp.TabStop = true;
			linkLabelHelp.Text = Properties.Resources.MainForm_InitializeComponent_Help;
			linkLabelHelp.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			linkLabelHelp.LinkClicked += linkLabelHelp_LinkClicked;
			label10.AutoSize = true;
			label10.Location = new System.Drawing.Point(9, 585);
			label10.Name = "label10";
			label10.Size = new System.Drawing.Size(48, 13);
			label10.TabIndex = 89;
			label10.Text = Properties.Resources.MainForm_InitializeComponent_Version__;
			versionLabel2.AutoSize = true;
			versionLabel2.Location = new System.Drawing.Point(60, 585);
			versionLabel2.Name = "versionLabel2";
			versionLabel2.Size = new System.Drawing.Size(18, 13);
			versionLabel2.TabIndex = 88;
			versionLabel2.Text = Properties.Resources.MainForm_InitializeComponent__as;
			label8.AutoSize = true;
			label8.Location = new System.Drawing.Point(9, 570);
			label8.Name = "label8";
			label8.Size = new System.Drawing.Size(77, 13);
			label8.TabIndex = 87;
			label8.Text = Properties.Resources.MainForm_InitializeComponent_Client_Version__;
			clientVersionLabel.AutoSize = true;
			clientVersionLabel.Location = new System.Drawing.Point(85, 570);
			clientVersionLabel.Name = "clientVersionLabel";
			clientVersionLabel.Size = new System.Drawing.Size(18, 13);
			clientVersionLabel.TabIndex = 86;
			clientVersionLabel.Text = Properties.Resources.MainForm_InitializeComponent__as;
			openRecordingDevicesButton.Image = (System.Drawing.Image)resources.GetObject("openRecordingDevicesButton.Image");
			openRecordingDevicesButton.Location = new System.Drawing.Point(24, 79);
			openRecordingDevicesButton.Name = "openRecordingDevicesButton";
			openRecordingDevicesButton.Size = new System.Drawing.Size(101, 36);
			openRecordingDevicesButton.TabIndex = 83;
			openRecordingDevicesButton.Text = Properties.Resources.MainForm_InitializeComponent_Open_Rec__Devices;
			openRecordingDevicesButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			openRecordingDevicesButton.UseVisualStyleBackColor = false;
			openRecordingDevicesButton.Click += openRecordingDevicesButton_Click;
			openMixerButton.Image = (System.Drawing.Image)resources.GetObject("openMixerButton.Image");
			openMixerButton.Location = new System.Drawing.Point(131, 79);
			openMixerButton.Name = "openMixerButton";
			openMixerButton.Size = new System.Drawing.Size(101, 36);
			openMixerButton.TabIndex = 64;
			openMixerButton.Text = Properties.Resources.MainForm_InitializeComponent_Open_Sound_Mixer;
			openMixerButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			openMixerButton.UseVisualStyleBackColor = false;
			openMixerButton.Click += OpenMixerButtonClick;
			label2.AutoSize = true;
			label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label2.Location = new System.Drawing.Point(21, 53);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(189, 12);
			label2.TabIndex = 81;
			label2.Text = Properties.Resources.MainForm_InitializeComponent_Normally__Stereo_Mix___See_help_for_more_info_;
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(9, 13);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(94, 13);
			label1.TabIndex = 60;
			label1.Text = Properties.Resources.MainForm_InitializeComponent_Recording_device_;
			deviceListBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			deviceListBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			deviceListBox.FormattingEnabled = true;
			deviceListBox.Location = new System.Drawing.Point(24, 29);
			deviceListBox.Name = "deviceListBox";
			deviceListBox.Size = new System.Drawing.Size(215, 21);
			deviceListBox.TabIndex = 55;
			browseButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			browseButton.Image = (System.Drawing.Image)resources.GetObject("browseButton.Image");
			browseButton.Location = new System.Drawing.Point(211, 150);
			browseButton.Name = "browseButton";
			browseButton.Size = new System.Drawing.Size(28, 28);
			browseButton.TabIndex = 76;
			browseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			browseButton.UseVisualStyleBackColor = true;
			browseButton.Click += BrowseButtonClick;
			label3.AutoSize = true;
			label3.Location = new System.Drawing.Point(9, 138);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(169, 13);
			label3.TabIndex = 61;
			label3.Text = Properties.Resources.MainForm_InitializeComponent_Save_recorded_mp3_s_in_this_folder_;
			outputFolderTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			outputFolderTextBox.Location = new System.Drawing.Point(24, 155);
			outputFolderTextBox.Name = "outputFolderTextBox";
			outputFolderTextBox.ReadOnly = true;
			outputFolderTextBox.Size = new System.Drawing.Size(181, 20);
			outputFolderTextBox.TabIndex = 71;
			label4.AutoSize = true;
			label4.Location = new System.Drawing.Point(9, 191);
			label4.Name = "label4";
			label4.Size = new System.Drawing.Size(63, 13);
			label4.TabIndex = 62;
			label4.Text = Properties.Resources.MainForm_InitializeComponent_Mp3_bitrate_;
			bitrateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			bitrateComboBox.FormattingEnabled = true;
			bitrateComboBox.Location = new System.Drawing.Point(78, 188);
			bitrateComboBox.Name = "bitrateComboBox";
			bitrateComboBox.Size = new System.Drawing.Size(161, 21);
			bitrateComboBox.TabIndex = 54;
			label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label7.Location = new System.Drawing.Point(10, 244);
			label7.Name = "label7";
			label7.Size = new System.Drawing.Size(73, 16);
			label7.TabIndex = 57;
			label7.Text = Properties.Resources.MainForm_InitializeComponent__The_Picture_;
			label7.Visible = false;
			tabPageLog.Controls.Add(listBoxLog);
			tabPageLog.Location = new System.Drawing.Point(4, 22);
			tabPageLog.Name = "tabPageLog";
			tabPageLog.Padding = new System.Windows.Forms.Padding(3);
			tabPageLog.Size = new System.Drawing.Size(245, 612);
			tabPageLog.TabIndex = 5;
			tabPageLog.Text = Properties.Resources.MainForm_InitializeComponent_Log;
			tabPageLog.UseVisualStyleBackColor = true;
			listBoxLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			listBoxLog.FormattingEnabled = true;
			listBoxLog.Location = new System.Drawing.Point(8, 6);
			listBoxLog.Name = "listBoxLog";
			listBoxLog.Size = new System.Drawing.Size(231, 589);
			listBoxLog.TabIndex = 1;
			pictureBoxAlbumCover.Location = new System.Drawing.Point(3, 3);
			pictureBoxAlbumCover.Name = "pictureBoxAlbumCover";
			pictureBoxAlbumCover.Size = new System.Drawing.Size(654, 631);
			pictureBoxAlbumCover.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			pictureBoxAlbumCover.TabIndex = 85;
			pictureBoxAlbumCover.TabStop = false;
			toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripButton_Back, toolStripButton_Home, toolStripSeparator3, toolStripButtonHideSidebar });
			toolStrip1.Location = new System.Drawing.Point(0, 0);
			toolStrip1.Name = "toolStrip1";
			toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			toolStrip1.Size = new System.Drawing.Size(919, 25);
			toolStrip1.TabIndex = 0;
			toolStrip1.Text = "";
			toolStripButton_Back.Enabled = false;
			toolStripButton_Back.Image = (System.Drawing.Image)resources.GetObject("toolStripButton_Back.Image");
			toolStripButton_Back.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton_Back.Name = "toolStripButton_Back";
			toolStripButton_Back.Size = new System.Drawing.Size(52, 22);
			toolStripButton_Back.Text = Properties.Resources.MainForm_InitializeComponent_Back;
			toolStripButton_Home.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton_Home.Enabled = false;
			toolStripButton_Home.Image = (System.Drawing.Image)resources.GetObject("toolStripButton_Home.Image");
			toolStripButton_Home.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton_Home.Name = "toolStripButton_Home";
			toolStripButton_Home.Size = new System.Drawing.Size(23, 22);
			toolStripButton_Home.Text = Properties.Resources.MainForm_InitializeComponent_Reload;
			toolStripButton_Home.ToolTipText = Properties.Resources.MainForm_InitializeComponent_Reload_page;
			toolStripSeparator3.Name = "toolStripSeparator3";
			toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			toolStripButtonHideSidebar.Image = (System.Drawing.Image)resources.GetObject("toolStripButtonHideSidebar.Image");
			toolStripButtonHideSidebar.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButtonHideSidebar.Name = "toolStripButtonHideSidebar";
			toolStripButtonHideSidebar.Size = new System.Drawing.Size(127, 22);
			toolStripButtonHideSidebar.Text = Properties.Resources.MainForm_InitializeComponent_Hide_Control_Panel;
			toolStripButtonHideSidebar.Click += toolStripButtonHideSidebar_Click;
			AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(919, 666);
			Controls.Add(toolStrip1);
			Controls.Add(splitContainer1);
			Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			Name = "MainForm";
			Text = Properties.Resources.MainForm_InitializeComponent_Spotify_Recorder;
			WindowState = System.Windows.Forms.FormWindowState.Maximized;
			FormClosing += MainForm_FormClosing;
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel2.ResumeLayout(false);
			splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			tabControl1.ResumeLayout(false);
			tabPage1.ResumeLayout(false);
			tabPage1.PerformLayout();
			contextMenuStrip1.ResumeLayout(false);
			tabPage2.ResumeLayout(false);
			tabPage2.PerformLayout();
			tabPageLog.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)pictureBoxAlbumCover).EndInit();
			toolStrip1.ResumeLayout(false);
			toolStrip1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		private void initVariables()
		{
			_isConnected = false;
			_baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			_duration = 0;
			_albumArtQuality = 0;
			_currentTag = new Mp3Tag("", "", "", "", null);
			_recordingTag = new Mp3Tag("", "", "", "", null);
			_currentSongName = string.Empty;
			_oldSongName = string.Empty;
			_programStartingTime = System.DateTime.Now;
			SongCountToSave = 0;
			_logPath = System.IO.Path.Combine(_baseDir, "Log.txt");
		}

		private void linkLabelAbout_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("About.html");
		}

		private void linkLabelHelp_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("Help.html");
		}

		private void LoadBitrateCombo()
		{
			System.Collections.Generic.Dictionary<string, string> bitrate = new System.Collections.Generic.Dictionary<string, string>
			{
				{ "VBR Extreme (V0)", "--preset extreme" },
				{ "VBR Standard (V2)", "--preset standard" },
				{ "VBR Medium (V5)", "--preset medium" },
				{ "CBR 320", "--preset insane" },
				{ "CBR 256", "-b 256" },
				{ "CBR 192", "-b 192" },
				{ "CBR 160", "-b 160" },
				{ "CBR 128", "-b 128" },
				{ "CBR 96", "-b 96" }
			};
			bitrateComboBox.DataSource = new System.Windows.Forms.BindingSource(bitrate, null);
			bitrateComboBox.DisplayMember = "Key";
			bitrateComboBox.ValueMember = "Value";
		}

		private void LoadUserSettings()
		{
			string defaultDevice = Util.GetDefaultDevice();
			foreach (NAudio.CoreAudioApi.MMDevice device in deviceListBox.Items)
			{
				if (!device.FriendlyName.Equals(defaultDevice))
				{
					continue;
				}
				deviceListBox.SelectedItem = device;
			}
			outputFolderTextBox.Text = Util.GetDefaultOutputPath();
			bitrateComboBox.SelectedIndex = Util.GetDefaultBitrate();
			textBoxAlbumArtQuality.Text = Util.GetDefaultArtQuality().ToString();
		}

		private void LoadWasapiDevicesCombo()
		{
			System.Collections.Generic.List<NAudio.CoreAudioApi.MMDevice> devices = Enumerable.ToList((new NAudio.CoreAudioApi.MMDeviceEnumerator()).EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active));
			deviceListBox.DataSource = devices;
			deviceListBox.DisplayMember = "FriendlyName";
		}

		private void MainForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			System.Windows.Forms.Application.Exit();
		}

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
            buttonStopRecording.PerformClick();
            if (_isConnected)
            {
                _spotify.ListenForEvents = false;
                _spotify.Dispose();
            }

            Util.SetDefaultBitrate(bitrateComboBox.SelectedIndex);
            Util.SetDefaultDevice(deviceListBox.SelectedItem.ToString());
            Util.SetDefaultOutputPath(outputFolderTextBox.Text);
            Util.SetDefaultArtQuality(int.Parse(textBoxAlbumArtQuality.Text));
            _writer.Close();
            _writer.Dispose();
        }

        private void OnLoad(object sender, System.EventArgs eventArgs)
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic), "SpotifyRecorder"));
			LoadWasapiDevicesCombo();
			LoadBitrateCombo();
			LoadUserSettings();
			AddAllRecordingToList();
			songLabel.Text = string.Empty;
			encodingLabel.Text = string.Empty;
			_folderDialog = new System.Windows.Forms.FolderBrowserDialog
			{
				SelectedPath = outputFolderTextBox.Text
			};
			versionLabel.Text = string.Format("Version {0}", System.Windows.Forms.Application.ProductVersion);
			buttonStopRecording.Enabled = false;
			try
			{
				_soundCardRecorder = new SoundCardRecorder(CreateOutputFileName("deleteme", "wav"), "");
				_soundCardRecorder.Dispose();
				_soundCardRecorder = null;
				if (System.IO.File.Exists(CreateOutputFileName("deleteme", "wav")))
				{
					System.IO.File.Delete(CreateOutputFileName("deleteme", "wav"));
				}
			}
			catch (System.Exception exception)
			{
				System.Windows.Forms.MessageBox.Show(exception.Message);
			}
		}

		private void OpenMixerButtonClick(object sender, System.EventArgs e)
		{
			System.Diagnostics.Process.Start("sndvol");
		}

		private void openRecordingDevicesButton_Click(object sender, System.EventArgs e)
		{
			System.Diagnostics.Process.Start("control.exe", "mmsys.cpl,,1");
		}

		private void PostProccessMain(SpotifyAPI.Local.Models.Track oldTrack)
		{
			if (!oldTrack.IsAd() && !oldTrack.TrackType.Equals("local"))
			{
				string songy = string.Concat(oldTrack.ArtistResource.Name, " - ", oldTrack.TrackResource.Name);
				if (!string.IsNullOrEmpty(outputFolderTextBox.Text))
				{
					addToLog(string.Concat("Recorded file: ", outputFolderTextBox.Text));
					encodingLabel.Text = _oldSongName;
					if (string.IsNullOrEmpty(_oldSongName))
					{
						songy = string.Concat(oldTrack.ArtistResource.Name, " - ", oldTrack.TrackResource.Name);
					}
					PostProcessing(songy, oldTrack);
					return;
				}
				System.Windows.Forms.MessageBox.Show(Properties.Resources.MainForm_PostProccessMain_Please_set_a_output_folder_from_settings);
			}
		}

		private void PostProcessing(string song, SpotifyAPI.Local.Models.Track oldTrack)
		{
			string selectedValue = (string)bitrateComboBox.SelectedValue;
			(new System.Threading.Tasks.Task(() => ConvertToMp3(song, selectedValue, oldTrack))).Start();
		}

		public static string RemoveInvalidFilePathCharacters(string filename, string replaceChar)
		{
			string regexSearch = string.Concat(new string(System.IO.Path.GetInvalidFileNameChars()), new string(System.IO.Path.GetInvalidPathChars()));
			return (new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)))).Replace(filename, replaceChar);
		}

		private string removePathInfo(string song, string path)
		{
			return song.Replace(string.Concat(path, "\\"), "");
		}

		public void SaveJpeg(int Quality, System.Drawing.Image albumArt)
		{
			using (System.Drawing.Image image = albumArt)
			{
				int width = image.Size.Width;
				System.Drawing.Size size = image.Size;
				using (System.Drawing.Image clone = new System.Drawing.Bitmap(image, new System.Drawing.Size(width, size.Height)))
				{
					CompressImage(clone, Quality, System.IO.Path.Combine(outputFolderTextBox.Text, "Album_Art.Png"));
				}
			}
		}

		private void StartRecording()
		{
				if (!nowPlayingLabel.Text.Equals(Properties.Resources.MainForm_UpdateTrack_Advert))
				{
					_currentSongName = string.Concat(_currentTag.Artist, " - ", _currentTag.Title);
					if (System.IO.File.Exists(string.Concat(_currentSongName, ".mp3")))
					{
						addToLog(string.Concat(_currentSongName, ".mp3 Exists!"));
						return;
					}
					_soundCardRecorder = new SoundCardRecorder(CreateOutputFileName(_currentSongName, "wav"), _currentSongName);
					_soundCardRecorder.Start();
					songLabel.Text = _currentSongName;
					addToLog("Recording!");
					return;
				}
				addToLog("Advert Playing please wait for it to finish to start recording.");
		}

		private void StopRecording()
		{
			if (_soundCardRecorder != null)
			{
				addToLog("Recording stopped");
				_soundCardRecorder.Stop();
				_duration = _soundCardRecorder.Duration;
				addToLog(string.Concat("Duration: ", _duration));
				_soundCardRecorder.Dispose();
				_soundCardRecorder = null;
			}
		}

		private void textBoxAlbumArtQuality_TextChanged(object sender, System.EventArgs e)
		{
			try
			{
				if (int.Parse(textBoxAlbumArtQuality.Text) >= 100 || int.Parse(textBoxAlbumArtQuality.Text) <= 0)
				{
					System.Windows.Forms.MessageBox.Show(Properties.Resources.MainForm_textBoxAlbumArtQuality_TextChanged_This_value_must_be_between_0_100);
				}
				else
				{
					_albumArtQuality = int.Parse(textBoxAlbumArtQuality.Text);
				}
			}
			catch (System.Exception exception)
			{
				System.Windows.Forms.MessageBox.Show(exception.Message);
			}
		}

		private void textBoxSongLimit_TextChanged(object sender, System.EventArgs e)
		{
			try
			{
				SongCountToSave = int.Parse(textBoxSongLimit.Text);
			}
			catch (System.Exception exception)
			{
				System.Windows.Forms.MessageBox.Show(exception.Message);
			}
		}

		private void toolStripButtonHideSidebar_Click(object sender, System.EventArgs e)
		{
			if (!toolStripButtonHideSidebar.Checked)
			{
				splitContainer1.Panel1Collapsed = true;
			}
			else
			{
				splitContainer1.Panel1Collapsed = false;
			}
			toolStripButtonHideSidebar.Checked = !toolStripButtonHideSidebar.Checked;
		}

		private void toolStripMenuItem_ClearList_Click(object sender, System.EventArgs e)
		{
			listBoxRecordings.Items.Clear();
		}

		private void toolStripMenuItem_Delete_Click(object sender, System.EventArgs e)
		{
			if (listBoxRecordings.SelectedItem != null)
			{
				try
				{
					System.IO.File.Delete(CreateOutputFileName((string)listBoxRecordings.SelectedItem, "mp3"));
					listBoxRecordings.Items.Remove(listBoxRecordings.SelectedItem);
					if (listBoxRecordings.Items.Count > 0)
					{
						listBoxRecordings.SelectedIndex = 0;
					}
				}
				catch (System.Exception )
				{
					System.Windows.Forms.MessageBox.Show(Properties.Resources.MainForm_toolStripMenuItem_Delete_Click_Could_not_delete_recording___);
				}
			}
		}

		private void toolStripMenuItem_Open_Click(object sender, System.EventArgs e)
		{
			System.Diagnostics.Process.Start(outputFolderTextBox.Text);
		}

		private void toolStripMenuItem_Play_Click(object sender, System.EventArgs e)
		{
			if (listBoxRecordings.SelectedItem != null)
			{
				try
				{
					System.Diagnostics.Process.Start(System.IO.Path.Combine(outputFolderTextBox.Text, (string)listBoxRecordings.SelectedItem));
				}
				catch
				{
					System.Windows.Forms.MessageBox.Show(Properties.Resources.MainForm_toolStripMenuItem_Play_Click_Could_not_play_song___);
				}
			}
		}

		private async void UpdateAlbumArt()
		{
			if (_currentTrack.ArtistResource.Name != "Spotify")
			{
				System.Windows.Forms.PictureBox albumArtAsync = pictureBoxAlbumCover;
				albumArtAsync.Image = await _currentTrack.GetAlbumArtAsync(SpotifyAPI.Local.Enums.AlbumArtSize.Size640);
			}
		}

		public void UpdateInfos()
		{
			if (_isConnected)
			{
				SpotifyAPI.Local.Models.StatusResponse status = _spotify.GetStatus();
				if (status == null)
				{
					return;
				}
				clientVersionLabel.Text = status.ClientVersion;
				versionLabel2.Text = status.Version.ToString();
				if (status.Track != null)
				{
					UpdateTrack(status.Track);
				}
			}
        }

		private void UpdateRecordingTag(SpotifyAPI.Local.Models.Track oldTrack)
		{
			_recordingTag.Title = (!string.IsNullOrEmpty(oldTrack.TrackResource.Name) ? oldTrack.TrackResource.Name : string.Empty);
			_recordingTag.Artist = oldTrack.ArtistResource.Name;
			_recordingTag.TrackUri = oldTrack.TrackResource.Uri;
			_recordingTag.AlbumName = (!string.IsNullOrEmpty(oldTrack.AlbumResource.Name) ? oldTrack.AlbumResource.Name : string.Empty);
		}

		private void UpdateTag()
		{
			if (_currentTrack == null)
				return;
			if (_currentTrack.TrackType == "local")
			{
				return;
			}
			try
			{
				_currentTag.Title = (string.IsNullOrEmpty(_currentTrack.TrackResource.Name) ? string.Empty : _currentTrack.TrackResource.Name);
				_currentTag.Artist = _currentTrack.ArtistResource.Name;
				_currentTag.TrackUri = _currentTrack.TrackResource.Uri;
				_currentTag.AlbumName = (string.IsNullOrEmpty(_currentTrack.AlbumResource.Name) ? string.Empty : _currentTrack.AlbumResource.Name);
				UpdateAlbumArt();
			}
			catch (System.Exception exception)
			{
				System.Windows.Forms.MessageBox.Show(exception.Message);
			}
		}

		public void UpdateTrack(SpotifyAPI.Local.Models.Track track)
		{
			_currentTrack = track;
			if (_currentTrack.TrackType == "local")
			{
				System.Windows.Forms.Label label = nowPlayingLabel;
				label.Text = string.Concat(label.Text, "(Local)");
				return;
			}
			if (track.IsAd())
			{
				nowPlayingLabel.Text = Properties.Resources.MainForm_UpdateTrack_Advert;
				_currentSongName = string.Empty;
				return;
			}
			UpdateTag();
			_oldSongName = _currentSongName;
			_currentSongName = string.Concat(_currentTag.Artist, " - ", _currentTag.Title);
			nowPlayingLabel.Text = _currentSongName;
			addToLog(string.Concat("Now playing: ", _currentSongName, " (", _currentTag.TrackUri, ") RECORDING!"));
		}
		
	}
}