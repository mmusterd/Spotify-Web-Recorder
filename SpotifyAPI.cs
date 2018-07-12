namespace SpotifyWebRecorder
{
    public class SpotifyAPI
    {
        private MainForm _mainForm;
        private readonly SpotifyAPI.Local.SpotifyLocalAPI _spotify;
        private bool _isConnected;

        public SpotifyAPI(SpotifyLocalAPI spotify, MainForm mainForm)
        {
            _spotify = spotify;
            _spotify.OnPlayStateChange += _spotify_OnPlayStateChange;
            _spotify.OnTrackChange += _spotify_OnTrackChange;
            _spotify.OnTrackTimeChange += _spotify_OnTrackTimeChange;
            initVariables();
            _mainForm = mainForm;
        }

        public void _spotify_OnPlayStateChange(object sender, SpotifyAPI.Local.PlayStateEventArgs e)
        {
            if (!_mainForm.InvokeRequired)
            {
                return;
            }

            _mainForm.Invoke(new System.Action(() => _spotify_OnPlayStateChange(sender, e)));
        }

        public void _spotify_OnTrackChange(object sender, SpotifyAPI.Local.TrackChangeEventArgs e)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new System.Action(() => _spotify_OnTrackChange(sender, e)));
                return;
            }
            if (_spotify.GetStatus().Playing)
            {
                _spotify.Pause();
            }
            UpdateTrack(e.NewTrack);
            if (!buttonStartRecording.Spotify.Enabled)
            {
                if (_mainForm.SongCountToSave == 0 || _mainForm.SongCountToSave - 1 > _mainForm._counter)
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
                        _mainForm._recordingTag = _mainForm._currentTag.Clone();
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
                        _mainForm._recordingTag = _mainForm._currentTag.Clone();
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
                    _mainForm._recordingTag = _mainForm._currentTag.Clone();
                    _mainForm._counter++;
                    return;
                }

                _mainForm.buttonStopRecording.PerformClick();
                if (_mainForm.checkBoxShutdown.CheckState == System.Windows.Forms.CheckState.Checked)
                {
                    System.Threading.Thread.Sleep(20000);
                    _mainForm.Close();
                    System.Diagnostics.Process shutdown = new System.Diagnostics.Process();
                    shutdown.StartInfo.FileName = "shutdown -s -t 3";
                    if (_mainForm.checkBoxShutdown.CheckState == System.Windows.Forms.CheckState.Checked)
                    {
                        shutdown.Start();
                    }
                }
            }
        }

        public void _spotify_OnTrackTimeChange(object sender, SpotifyAPI.Local.TrackTimeChangeEventArgs e)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new System.Action(() => _spotify_OnTrackTimeChange(sender, e)));
                return;
            }

            _mainForm._time = System.TimeSpan.FromSeconds(e.TrackTime);
            _mainForm.timeLabel.Text = _mainForm._time.ToString("hh\\:mm\\:ss");
            if (_spotify.GetStatus().Shuffle)
            {
                labelShuffle.Spotify.Visible = true;
                return;
            }
            labelShuffle.Spotify.Visible = false;
        }

        private void addToLog(string text)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new System.Windows.Forms.MethodInvoker(() => addToLog(text)));
                return;
            }
            System.Windows.Forms.ListBox.ObjectCollection items = _mainForm.listBoxLog.Items;
            System.DateTime now = System.DateTime.Now;
            items.Add(string.Concat("[", now.ToShortTimeString(), "] ", text));
            _mainForm.listBoxLog.SelectedIndex = _mainForm.listBoxLog.Items.Count - 1;
            _mainForm._writer.WriteLine(text);
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
            if (buttonStopRecording.Spotify.Enabled)
            {
                UpdateRecordingTag(oldTrack);
            }
            SaveJpeg(_mainForm._albumArtQuality, oldTrack.GetAlbumArt(SpotifyAPI.Local.Enums.AlbumArtSize.Size640));
            process.StartInfo.FileName = "lame.exe";
            process.StartInfo.Arguments = string.Format("{2} --tt \"{3}\" --ta \"{4}\" --tc \"{5}\" --tl \"{6}\" --ti \"{7}\" \"{0}\" \"{1}\"", CreateOutputFileName(songName, "wav"), CreateOutputFileName(songName, "mp3"), bitrate, _mainForm._recordingTag.Title, _mainForm._recordingTag.Artist, _mainForm._recordingTag.TrackUri, _mainForm._recordingTag.AlbumName, System.IO.Path.Combine(_mainForm.outputFolderTextBox.Text, "Album_Art.Png"));
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
            System.IO.File.Delete(System.IO.Path.Combine(_mainForm.outputFolderTextBox.Text, "Album_Art.Png"));
            addToLog(System.String.Concat((string) "Mp3 ready: ", (string) CreateOutputFileName(songName, "mp3")));
            _mainForm.AddSongToList(string.Concat(songName, ".mp3"));
        }

        private string CreateOutputFileName(string song, string extension)
        {
            song = RemoveInvalidFilePathCharacters(song, string.Empty);
            return System.IO.Path.Combine(_mainForm.outputFolderTextBox.Text, string.Format("{0}.{1}", song, extension));
        }

        private void InitializeComponent()
        {
            _mainForm.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            _mainForm.splitContainer1 = new System.Windows.Forms.SplitContainer();
            _mainForm.tabControl1 = new System.Windows.Forms.TabControl();
            _mainForm.tabPage1 = new System.Windows.Forms.TabPage();
            _mainForm.labelShuffle = new System.Windows.Forms.Label();
            _mainForm.label15 = new System.Windows.Forms.Label();
            _mainForm.label14 = new System.Windows.Forms.Label();
            _mainForm.textBoxSongLimit = new System.Windows.Forms.TextBox();
            _mainForm.timeLabel = new System.Windows.Forms.Label();
            _mainForm.label231 = new System.Windows.Forms.Label();
            _mainForm.label13 = new System.Windows.Forms.Label();
            _mainForm.nowPlayingLabel = new System.Windows.Forms.Label();
            _mainForm.encodingLabel = new System.Windows.Forms.Label();
            _mainForm.label12 = new System.Windows.Forms.Label();
            _mainForm.label11 = new System.Windows.Forms.Label();
            _mainForm.label6 = new System.Windows.Forms.Label();
            _mainForm.linkLabel1 = new System.Windows.Forms.LinkLabel();
            _mainForm.label5 = new System.Windows.Forms.Label();
            _mainForm.songLabel = new System.Windows.Forms.Label();
            _mainForm.donateLink = new System.Windows.Forms.LinkLabel();
            _mainForm.buttonStartRecording = new System.Windows.Forms.Button();
            _mainForm.versionLabel = new System.Windows.Forms.Label();
            _mainForm.buttonStopRecording = new System.Windows.Forms.Button();
            _mainForm.listBoxRecordings = new System.Windows.Forms.ListBox();
            _mainForm.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(_mainForm.components);
            _mainForm.toolStripMenuItem_Open = new System.Windows.Forms.ToolStripMenuItem();
            _mainForm.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            _mainForm.toolStripMenuItem_Play = new System.Windows.Forms.ToolStripMenuItem();
            _mainForm.toolStripMenuItem_Delete = new System.Windows.Forms.ToolStripMenuItem();
            _mainForm.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            _mainForm.toolStripMenuItem_ClearList = new System.Windows.Forms.ToolStripMenuItem();
            _mainForm.tabPage2 = new System.Windows.Forms.TabPage();
            _mainForm.checkBoxShutdown = new System.Windows.Forms.CheckBox();
            _mainForm.label18 = new System.Windows.Forms.Label();
            _mainForm.label17 = new System.Windows.Forms.Label();
            _mainForm.label16 = new System.Windows.Forms.Label();
            _mainForm.textBoxAlbumArtQuality = new System.Windows.Forms.TextBox();
            _mainForm.label9 = new System.Windows.Forms.Label();
            _mainForm.linkLabelAbout = new System.Windows.Forms.LinkLabel();
            _mainForm.linkLabelHelp = new System.Windows.Forms.LinkLabel();
            _mainForm.label10 = new System.Windows.Forms.Label();
            _mainForm.versionLabel2 = new System.Windows.Forms.Label();
            _mainForm.label8 = new System.Windows.Forms.Label();
            _mainForm.clientVersionLabel = new System.Windows.Forms.Label();
            _mainForm.openRecordingDevicesButton = new System.Windows.Forms.Button();
            _mainForm.openMixerButton = new System.Windows.Forms.Button();
            _mainForm.label2 = new System.Windows.Forms.Label();
            _mainForm.label1 = new System.Windows.Forms.Label();
            _mainForm.deviceListBox = new System.Windows.Forms.ComboBox();
            _mainForm.browseButton = new System.Windows.Forms.Button();
            _mainForm.label3 = new System.Windows.Forms.Label();
            _mainForm.outputFolderTextBox = new System.Windows.Forms.TextBox();
            _mainForm.label4 = new System.Windows.Forms.Label();
            _mainForm.bitrateComboBox = new System.Windows.Forms.ComboBox();
            _mainForm.label7 = new System.Windows.Forms.Label();
            _mainForm.tabPageLog = new System.Windows.Forms.TabPage();
            _mainForm.listBoxLog = new System.Windows.Forms.ListBox();
            _mainForm.pictureBoxAlbumCover = new System.Windows.Forms.PictureBox();
            _mainForm.toolStrip1 = new System.Windows.Forms.ToolStrip();
            _mainForm.toolStripButton_Back = new System.Windows.Forms.ToolStripButton();
            _mainForm.toolStripButton_Home = new System.Windows.Forms.ToolStripButton();
            _mainForm.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            _mainForm.toolStripButtonHideSidebar = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize) _mainForm.splitContainer1).BeginInit();
            _mainForm.splitContainer1.Panel1.Spotify.SuspendLayout();
            _mainForm.splitContainer1.Panel2.Spotify.SuspendLayout();
            _mainForm.splitContainer1.Spotify.SuspendLayout();
            _mainForm.tabControl1.Spotify.SuspendLayout();
            _mainForm.tabPage1.Spotify.SuspendLayout();
            _mainForm.contextMenuStrip1.Spotify.SuspendLayout();
            _mainForm.tabPage2.Spotify.SuspendLayout();
            _mainForm.tabPageLog.Spotify.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) _mainForm.pictureBoxAlbumCover).BeginInit();
            _mainForm.toolStrip1.Spotify.SuspendLayout();
            _mainForm.SuspendLayout();
            _mainForm.splitContainer1.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            _mainForm.splitContainer1.Spotify.Location = new System.Drawing.Point(0, 28);
            _mainForm.splitContainer1.Spotify.Name = "splitContainer1";
            _mainForm.splitContainer1.Panel1.Spotify.Controls.Add(_mainForm.tabControl1);
            _mainForm.splitContainer1.Panel2.Spotify.BackColor = System.Drawing.SystemColors.Control;
            _mainForm.splitContainer1.Panel2.Spotify.Controls.Add(_mainForm.pictureBoxAlbumCover);
            _mainForm.splitContainer1.Spotify.Size = new System.Drawing.Size(919, 638);
            _mainForm.splitContainer1.SplitterDistance = 253;
            _mainForm.splitContainer1.SplitterWidth = 6;
            _mainForm.splitContainer1.Spotify.TabIndex = 0;
            _mainForm.tabControl1.Spotify.Controls.Add(_mainForm.tabPage1);
            _mainForm.tabControl1.Spotify.Controls.Add(_mainForm.tabPage2);
            _mainForm.tabControl1.Spotify.Controls.Add(_mainForm.tabPageLog);
            _mainForm.tabControl1.Spotify.Dock = System.Windows.Forms.DockStyle.Fill;
            _mainForm.tabControl1.Spotify.Location = new System.Drawing.Point(0, 0);
            _mainForm.tabControl1.Spotify.Name = "tabControl1";
            _mainForm.tabControl1.SelectedIndex = 0;
            _mainForm.tabControl1.Spotify.Size = new System.Drawing.Size(253, 638);
            _mainForm.tabControl1.Spotify.TabIndex = 79;
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.labelShuffle);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label15);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label14);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.textBoxSongLimit);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.timeLabel);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label231);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label13);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.nowPlayingLabel);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.encodingLabel);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label12);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label11);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label6);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.linkLabel1);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.label5);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.songLabel);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.donateLink);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.buttonStartRecording);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.versionLabel);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.buttonStopRecording);
            _mainForm.tabPage1.Spotify.Controls.Add(_mainForm.listBoxRecordings);
            _mainForm.tabPage1.Location = new System.Drawing.Point(4, 22);
            _mainForm.tabPage1.Spotify.Name = "tabPage1";
            _mainForm.tabPage1.Spotify.Padding = new System.Windows.Forms.Padding(3);
            _mainForm.tabPage1.Spotify.Size = new System.Drawing.Size(245, 612);
            _mainForm.tabPage1.TabIndex = 0;
            _mainForm.tabPage1.Text = Properties.Resources.MainForm_InitializeComponent__Record_;
            _mainForm.tabPage1.UseVisualStyleBackColor = true;
            _mainForm.labelShuffle.AutoSize = true;
            _mainForm.labelShuffle.Spotify.Location = new System.Drawing.Point(162, 155);
            _mainForm.labelShuffle.Spotify.Name = "labelShuffle";
            _mainForm.labelShuffle.Spotify.Size = new System.Drawing.Size(74, 13);
            _mainForm.labelShuffle.Spotify.TabIndex = 89;
            _mainForm.labelShuffle.Text = Properties.Resources.MainForm_InitializeComponent__Shuffle_is_on__;
            _mainForm.label15.AutoSize = true;
            _mainForm.label15.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.label15.Spotify.Location = new System.Drawing.Point(163, 127);
            _mainForm.label15.Spotify.Name = "label15";
            _mainForm.label15.Spotify.Size = new System.Drawing.Size(72, 12);
            _mainForm.label15.Spotify.TabIndex = 88;
            _mainForm.label15.Text = Properties.Resources.MainForm_InitializeComponent___0_for_Limitless__;
            _mainForm.label14.AutoSize = true;
            _mainForm.label14.Spotify.Location = new System.Drawing.Point(12, 127);
            _mainForm.label14.Spotify.Name = "label14";
            _mainForm.label14.Spotify.Size = new System.Drawing.Size(106, 13);
            _mainForm.label14.Spotify.TabIndex = 87;
            _mainForm.label14.Text = Properties.Resources.MainForm_InitializeComponent_Song_Count_to_Save_;
            _mainForm.textBoxSongLimit.Spotify.Location = new System.Drawing.Point(125, 123);
            _mainForm.textBoxSongLimit.Spotify.Name = "textBoxSongLimit";
            _mainForm.textBoxSongLimit.Spotify.Size = new System.Drawing.Size(32, 20);
            _mainForm.textBoxSongLimit.Spotify.TabIndex = 86;
            _mainForm.textBoxSongLimit.Text = "0";
            _mainForm.textBoxSongLimit.Spotify.TextChanged += textBoxSongLimit_TextChanged;
            _mainForm.timeLabel.AutoSize = true;
            _mainForm.timeLabel.Spotify.Location = new System.Drawing.Point(51, 106);
            _mainForm.timeLabel.Spotify.Name = "timeLabel";
            _mainForm.timeLabel.Spotify.Size = new System.Drawing.Size(16, 13);
            _mainForm.timeLabel.Spotify.TabIndex = 84;
            _mainForm.timeLabel.Text = "...";
            _mainForm.label231.AutoSize = true;
            _mainForm.label231.Spotify.Location = new System.Drawing.Point(12, 106);
            _mainForm.label231.Spotify.Name = "label231";
            _mainForm.label231.Spotify.Size = new System.Drawing.Size(33, 13);
            _mainForm.label231.Spotify.TabIndex = 83;
            _mainForm.label231.Text = Properties.Resources.MainForm_InitializeComponent_Time_;
            _mainForm.label13.AutoSize = true;
            _mainForm.label13.Spotify.Location = new System.Drawing.Point(12, 84);
            _mainForm.label13.Spotify.Name = "label13";
            _mainForm.label13.Spotify.Size = new System.Drawing.Size(69, 13);
            _mainForm.label13.Spotify.TabIndex = 82;
            _mainForm.label13.Text = Properties.Resources.MainForm_InitializeComponent_Now_Playing_;
            _mainForm.nowPlayingLabel.AutoSize = true;
            _mainForm.nowPlayingLabel.Spotify.Location = new System.Drawing.Point(80, 84);
            _mainForm.nowPlayingLabel.Spotify.Name = "nowPlayingLabel";
            _mainForm.nowPlayingLabel.Spotify.Size = new System.Drawing.Size(16, 13);
            _mainForm.nowPlayingLabel.Spotify.TabIndex = 81;
            _mainForm.nowPlayingLabel.Text = "...";
            _mainForm.encodingLabel.AutoSize = true;
            _mainForm.encodingLabel.Spotify.Location = new System.Drawing.Point(73, 61);
            _mainForm.encodingLabel.Spotify.Name = "encodingLabel";
            _mainForm.encodingLabel.Spotify.Size = new System.Drawing.Size(16, 13);
            _mainForm.encodingLabel.Spotify.TabIndex = 80;
            _mainForm.encodingLabel.Text = "...";
            _mainForm.label12.AutoSize = true;
            _mainForm.label12.Spotify.Location = new System.Drawing.Point(8, 155);
            _mainForm.label12.Spotify.Name = "label12";
            _mainForm.label12.Spotify.Size = new System.Drawing.Size(106, 13);
            _mainForm.label12.Spotify.TabIndex = 79;
            _mainForm.label12.Text = Properties.Resources.MainForm_InitializeComponent_Finished_Recordings_;
            _mainForm.label11.AutoSize = true;
            _mainForm.label11.Spotify.Location = new System.Drawing.Point(12, 61);
            _mainForm.label11.Spotify.Name = "label11";
            _mainForm.label11.Spotify.Size = new System.Drawing.Size(55, 13);
            _mainForm.label11.Spotify.TabIndex = 78;
            _mainForm.label11.Text = Properties.Resources.MainForm_InitializeComponent_Encoding_;
            _mainForm.label6.AutoSize = true;
            _mainForm.label6.Spotify.Location = new System.Drawing.Point(12, 42);
            _mainForm.label6.Spotify.Name = "label6";
            _mainForm.label6.Spotify.Size = new System.Drawing.Size(84, 13);
            _mainForm.label6.Spotify.TabIndex = 76;
            _mainForm.label6.Text = Properties.Resources.MainForm_InitializeComponent_Now_Recording_;
            _mainForm.linkLabel1.ActiveLinkColor = System.Drawing.Color.Gray;
            _mainForm.linkLabel1.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _mainForm.linkLabel1.AutoSize = true;
            _mainForm.linkLabel1.LinkColor = System.Drawing.Color.FromArgb(64, 64, 64);
            _mainForm.linkLabel1.Spotify.Location = new System.Drawing.Point(28, 691);
            _mainForm.linkLabel1.Spotify.Name = "linkLabel1";
            _mainForm.linkLabel1.Spotify.Size = new System.Drawing.Size(181, 13);
            _mainForm.linkLabel1.Spotify.TabIndex = 75;
            _mainForm.linkLabel1.TabStop = true;
            _mainForm.linkLabel1.Text = "https://github.com/mmusterd/Spotify-Web-Recorder";
            _mainForm.label5.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _mainForm.label5.AutoSize = true;
            _mainForm.label5.Spotify.Location = new System.Drawing.Point(28, 668);
            _mainForm.label5.Spotify.Name = "label5";
            _mainForm.label5.Spotify.Size = new System.Drawing.Size(210, 13);
            _mainForm.label5.Spotify.TabIndex = 74;
            _mainForm.label5.Text = Properties.Resources.MainForm_InitializeComponent_For_more_information__check_the_online_help;
            _mainForm.songLabel.AutoSize = true;
            _mainForm.songLabel.Spotify.Location = new System.Drawing.Point(102, 42);
            _mainForm.songLabel.Spotify.Name = "songLabel";
            _mainForm.songLabel.Spotify.Size = new System.Drawing.Size(16, 13);
            _mainForm.songLabel.Spotify.TabIndex = 58;
            _mainForm.songLabel.Text = "...";
            _mainForm.donateLink.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.donateLink.AutoSize = true;
            _mainForm.donateLink.Spotify.BackColor = System.Drawing.SystemColors.Control;
            _mainForm.donateLink.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            _mainForm.donateLink.LinkColor = System.Drawing.Color.FromArgb(64, 64, 64);
            _mainForm.donateLink.Spotify.Location = new System.Drawing.Point(45, 716);
            _mainForm.donateLink.Spotify.Name = "donateLink";
            _mainForm.donateLink.Spotify.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            _mainForm.donateLink.Spotify.Size = new System.Drawing.Size(100, 13);
            _mainForm.donateLink.Spotify.TabIndex = 73;
            _mainForm.donateLink.TabStop = true;
            _mainForm.donateLink.Text = Properties.Resources.MainForm_InitializeComponent_Donate_with_PayPal;
            _mainForm.donateLink.VisitedLinkColor = System.Drawing.Color.FromArgb(64, 64, 64);
            _mainForm.buttonStartRecording.Image = (System.Drawing.Image)resources.GetObject("buttonStartRecording.Image");
            _mainForm.buttonStartRecording.Spotify.Location = new System.Drawing.Point(8, 6);
            _mainForm.buttonStartRecording.Spotify.Name = "buttonStartRecording";
            _mainForm.buttonStartRecording.Spotify.Size = new System.Drawing.Size(112, 29);
            _mainForm.buttonStartRecording.Spotify.TabIndex = 65;
            _mainForm.buttonStartRecording.Text = Properties.Resources.MainForm_InitializeComponent_Start_Monitoring;
            _mainForm.buttonStartRecording.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _mainForm.buttonStartRecording.UseVisualStyleBackColor = true;
            _mainForm.buttonStartRecording.Spotify.Click += _mainForm.ButtonStartRecordingClick;
            _mainForm.versionLabel.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _mainForm.versionLabel.AutoSize = true;
            _mainForm.versionLabel.Spotify.Location = new System.Drawing.Point(28, 716);
            _mainForm.versionLabel.Spotify.Name = "versionLabel";
            _mainForm.versionLabel.Spotify.Size = new System.Drawing.Size(35, 13);
            _mainForm.versionLabel.Spotify.TabIndex = 72;
            _mainForm.versionLabel.Text = "";
            _mainForm.buttonStopRecording.Image = (System.Drawing.Image)resources.GetObject("buttonStopRecording.Image");
            _mainForm.buttonStopRecording.Spotify.Location = new System.Drawing.Point(126, 6);
            _mainForm.buttonStopRecording.Spotify.Name = "buttonStopRecording";
            _mainForm.buttonStopRecording.Spotify.Size = new System.Drawing.Size(112, 29);
            _mainForm.buttonStopRecording.Spotify.TabIndex = 63;
            _mainForm.buttonStopRecording.Text = Properties.Resources.MainForm_InitializeComponent_Stop_Monitoring;
            _mainForm.buttonStopRecording.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _mainForm.buttonStopRecording.UseVisualStyleBackColor = true;
            _mainForm.buttonStopRecording.Spotify.Click += _mainForm.ButtonStopRecordingClick;
            _mainForm.listBoxRecordings.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.listBoxRecordings.Spotify.ContextMenuStrip = _mainForm.contextMenuStrip1;
            _mainForm.listBoxRecordings.FormattingEnabled = true;
            _mainForm.listBoxRecordings.IntegralHeight = false;
            _mainForm.listBoxRecordings.Spotify.Location = new System.Drawing.Point(8, 173);
            _mainForm.listBoxRecordings.Spotify.Name = "listBoxRecordings";
            _mainForm.listBoxRecordings.ScrollAlwaysVisible = true;
            _mainForm.listBoxRecordings.Spotify.Size = new System.Drawing.Size(231, 431);
            _mainForm.listBoxRecordings.Spotify.TabIndex = 66;
            _mainForm.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {_mainForm.toolStripMenuItem_Open, _mainForm.toolStripSeparator2, _mainForm.toolStripMenuItem_Play, _mainForm.toolStripMenuItem_Delete, _mainForm.toolStripSeparator1, _mainForm.toolStripMenuItem_ClearList });
            _mainForm.contextMenuStrip1.Spotify.Name = "contextMenuStrip1";
            _mainForm.contextMenuStrip1.Spotify.Size = new System.Drawing.Size(177, 104);
            _mainForm.toolStripMenuItem_Open.Name = "toolStripMenuItem_Open";
            _mainForm.toolStripMenuItem_Open.Size = new System.Drawing.Size(176, 22);
            _mainForm.toolStripMenuItem_Open.Text = Properties.Resources.MainForm_InitializeComponent_Open_output_folder;
            _mainForm.toolStripMenuItem_Open.Click += toolStripMenuItem_Open_Click;
            _mainForm.toolStripSeparator2.Name = "toolStripSeparator2";
            _mainForm.toolStripSeparator2.Size = new System.Drawing.Size(173, 6);
            _mainForm.toolStripMenuItem_Play.Name = "toolStripMenuItem_Play";
            _mainForm.toolStripMenuItem_Play.Size = new System.Drawing.Size(176, 22);
            _mainForm.toolStripMenuItem_Play.Text = Properties.Resources.MainForm_InitializeComponent_Play_selected;
            _mainForm.toolStripMenuItem_Play.Click += toolStripMenuItem_Play_Click;
            _mainForm.toolStripMenuItem_Delete.Name = "toolStripMenuItem_Delete";
            _mainForm.toolStripMenuItem_Delete.Size = new System.Drawing.Size(176, 22);
            _mainForm.toolStripMenuItem_Delete.Text = Properties.Resources.MainForm_InitializeComponent_Delete_selected;
            _mainForm.toolStripMenuItem_Delete.Click += toolStripMenuItem_Delete_Click;
            _mainForm.toolStripSeparator1.Name = "toolStripSeparator1";
            _mainForm.toolStripSeparator1.Size = new System.Drawing.Size(173, 6);
            _mainForm.toolStripMenuItem_ClearList.Name = "toolStripMenuItem_ClearList";
            _mainForm.toolStripMenuItem_ClearList.Size = new System.Drawing.Size(176, 22);
            _mainForm.toolStripMenuItem_ClearList.Text = Properties.Resources.MainForm_InitializeComponent_Clear_List;
            _mainForm.toolStripMenuItem_ClearList.Click += toolStripMenuItem_ClearList_Click;
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.checkBoxShutdown);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label18);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label17);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label16);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.textBoxAlbumArtQuality);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label9);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.linkLabelAbout);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.linkLabelHelp);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label10);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.versionLabel2);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label8);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.clientVersionLabel);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.openRecordingDevicesButton);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.openMixerButton);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label2);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label1);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.deviceListBox);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.browseButton);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label3);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.outputFolderTextBox);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label4);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.bitrateComboBox);
            _mainForm.tabPage2.Spotify.Controls.Add(_mainForm.label7);
            _mainForm.tabPage2.Location = new System.Drawing.Point(4, 22);
            _mainForm.tabPage2.Spotify.Name = "tabPage2";
            _mainForm.tabPage2.Spotify.Padding = new System.Windows.Forms.Padding(3);
            _mainForm.tabPage2.Spotify.Size = new System.Drawing.Size(245, 612);
            _mainForm.tabPage2.TabIndex = 1;
            _mainForm.tabPage2.Text = Properties.Resources.MainForm_InitializeComponent__Settings_;
            _mainForm.tabPage2.UseVisualStyleBackColor = true;
            _mainForm.checkBoxShutdown.AutoSize = true;
            _mainForm.checkBoxShutdown.Spotify.Location = new System.Drawing.Point(23, 351);
            _mainForm.checkBoxShutdown.Spotify.Name = "checkBoxShutdown";
            _mainForm.checkBoxShutdown.Spotify.Size = new System.Drawing.Size(191, 17);
            _mainForm.checkBoxShutdown.Spotify.TabIndex = 99;
            _mainForm.checkBoxShutdown.Text = Properties.Resources.MainForm_InitializeComponent_Shutdown_after_reaching_song_limit_;
            _mainForm.checkBoxShutdown.UseVisualStyleBackColor = true;
            _mainForm.label18.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 8f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.label18.Spotify.Location = new System.Drawing.Point(3, 274);
            _mainForm.label18.Spotify.Name = "label18";
            _mainForm.label18.Spotify.Size = new System.Drawing.Size(236, 74);
            _mainForm.label18.Spotify.TabIndex = 98;
            _mainForm.label18.Text = Properties.Resources.NoteTxt;
            _mainForm.label17.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.label17.Spotify.Location = new System.Drawing.Point(10, 212);
            _mainForm.label17.Spotify.Name = "label17";
            _mainForm.label17.Spotify.Size = new System.Drawing.Size(68, 16);
            _mainForm.label17.Spotify.TabIndex = 97;
            _mainForm.label17.Text = Properties.Resources.MainForm_InitializeComponent__Sound_Quality_;
            label17.Spotify.Visible = false;
            _mainForm.label16.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.label16.Spotify.Location = new System.Drawing.Point(157, 231);
            _mainForm.label16.Spotify.Name = "label16";
            _mainForm.label16.Spotify.Size = new System.Drawing.Size(75, 13);
            _mainForm.label16.Spotify.TabIndex = 96;
            _mainForm.label16.Text = Properties.Resources.MainForm_InitializeComponent__Between_0_100_;
            label16.Spotify.Visible = false;
            _mainForm.textBoxAlbumArtQuality.Spotify.Location = new System.Drawing.Point(105, 228);
            _mainForm.textBoxAlbumArtQuality.Spotify.Name = "textBoxAlbumArtQuality";
            _mainForm.textBoxAlbumArtQuality.Spotify.Size = new System.Drawing.Size(42, 20);
            _mainForm.textBoxAlbumArtQuality.Spotify.TabIndex = 95;
            _mainForm.textBoxAlbumArtQuality.Text = "100";
            _mainForm.textBoxAlbumArtQuality.Spotify.TextChanged += textBoxAlbumArtQuality_TextChanged;
            _mainForm.label9.AutoSize = true;
            _mainForm.label9.Spotify.Location = new System.Drawing.Point(9, 231);
            _mainForm.label9.Spotify.Name = "label9";
            _mainForm.label9.Spotify.Size = new System.Drawing.Size(90, 13);
            _mainForm.label9.Spotify.TabIndex = 94;
            _mainForm.label9.Text = Properties.Resources.MainForm_InitializeComponent_Album_Art_Quality_;
            _mainForm.linkLabelAbout.AutoSize = true;
            _mainForm.linkLabelAbout.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 15f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.linkLabelAbout.Spotify.Location = new System.Drawing.Point(83, 399);
            _mainForm.linkLabelAbout.Spotify.Name = "linkLabelAbout";
            _mainForm.linkLabelAbout.Spotify.Size = new System.Drawing.Size(64, 25);
            _mainForm.linkLabelAbout.Spotify.TabIndex = 93;
            _mainForm.linkLabelAbout.TabStop = true;
            _mainForm.linkLabelAbout.Text = Properties.Resources.MainForm_InitializeComponent_About;
            _mainForm.linkLabelAbout.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            _mainForm.linkLabelAbout.LinkClicked += linkLabelAbout_LinkClicked;
            _mainForm.linkLabelHelp.AutoSize = true;
            _mainForm.linkLabelHelp.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 15f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.linkLabelHelp.Spotify.Location = new System.Drawing.Point(90, 369);
            _mainForm.linkLabelHelp.Spotify.Name = "linkLabelHelp";
            _mainForm.linkLabelHelp.Spotify.Size = new System.Drawing.Size(52, 25);
            _mainForm.linkLabelHelp.Spotify.TabIndex = 92;
            _mainForm.linkLabelHelp.TabStop = true;
            _mainForm.linkLabelHelp.Text = Properties.Resources.MainForm_InitializeComponent_Help;
            _mainForm.linkLabelHelp.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            _mainForm.linkLabelHelp.LinkClicked += linkLabelHelp_LinkClicked;
            _mainForm.label10.AutoSize = true;
            _mainForm.label10.Spotify.Location = new System.Drawing.Point(9, 585);
            _mainForm.label10.Spotify.Name = "label10";
            _mainForm.label10.Spotify.Size = new System.Drawing.Size(48, 13);
            _mainForm.label10.Spotify.TabIndex = 89;
            _mainForm.label10.Text = Properties.Resources.MainForm_InitializeComponent_Version__;
            _mainForm.versionLabel2.AutoSize = true;
            _mainForm.versionLabel2.Spotify.Location = new System.Drawing.Point(60, 585);
            _mainForm.versionLabel2.Spotify.Name = "versionLabel2";
            _mainForm.versionLabel2.Spotify.Size = new System.Drawing.Size(18, 13);
            _mainForm.versionLabel2.Spotify.TabIndex = 88;
            _mainForm.versionLabel2.Text = Properties.Resources.MainForm_InitializeComponent__as;
            _mainForm.label8.AutoSize = true;
            _mainForm.label8.Spotify.Location = new System.Drawing.Point(9, 570);
            _mainForm.label8.Spotify.Name = "label8";
            _mainForm.label8.Spotify.Size = new System.Drawing.Size(77, 13);
            _mainForm.label8.Spotify.TabIndex = 87;
            _mainForm.label8.Text = Properties.Resources.MainForm_InitializeComponent_Client_Version__;
            _mainForm.clientVersionLabel.AutoSize = true;
            _mainForm.clientVersionLabel.Spotify.Location = new System.Drawing.Point(85, 570);
            _mainForm.clientVersionLabel.Spotify.Name = "clientVersionLabel";
            _mainForm.clientVersionLabel.Spotify.Size = new System.Drawing.Size(18, 13);
            _mainForm.clientVersionLabel.Spotify.TabIndex = 86;
            _mainForm.clientVersionLabel.Text = Properties.Resources.MainForm_InitializeComponent__as;
            _mainForm.openRecordingDevicesButton.Image = (System.Drawing.Image)resources.GetObject("openRecordingDevicesButton.Image");
            _mainForm.openRecordingDevicesButton.Spotify.Location = new System.Drawing.Point(24, 79);
            _mainForm.openRecordingDevicesButton.Spotify.Name = "openRecordingDevicesButton";
            _mainForm.openRecordingDevicesButton.Spotify.Size = new System.Drawing.Size(101, 36);
            _mainForm.openRecordingDevicesButton.Spotify.TabIndex = 83;
            _mainForm.openRecordingDevicesButton.Text = Properties.Resources.MainForm_InitializeComponent_Open_Rec__Devices;
            _mainForm.openRecordingDevicesButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _mainForm.openRecordingDevicesButton.UseVisualStyleBackColor = false;
            _mainForm.openRecordingDevicesButton.Spotify.Click += openRecordingDevicesButton_Click;
            _mainForm.openMixerButton.Image = (System.Drawing.Image)resources.GetObject("openMixerButton.Image");
            _mainForm.openMixerButton.Spotify.Location = new System.Drawing.Point(131, 79);
            _mainForm.openMixerButton.Spotify.Name = "openMixerButton";
            _mainForm.openMixerButton.Spotify.Size = new System.Drawing.Size(101, 36);
            _mainForm.openMixerButton.Spotify.TabIndex = 64;
            _mainForm.openMixerButton.Text = Properties.Resources.MainForm_InitializeComponent_Open_Sound_Mixer;
            _mainForm.openMixerButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _mainForm.openMixerButton.UseVisualStyleBackColor = false;
            _mainForm.openMixerButton.Spotify.Click += OpenMixerButtonClick;
            _mainForm.label2.AutoSize = true;
            _mainForm.label2.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.label2.Spotify.Location = new System.Drawing.Point(21, 53);
            _mainForm.label2.Spotify.Name = "label2";
            _mainForm.label2.Spotify.Size = new System.Drawing.Size(189, 12);
            _mainForm.label2.Spotify.TabIndex = 81;
            _mainForm.label2.Text = Properties.Resources.MainForm_InitializeComponent_Normally__Stereo_Mix___See_help_for_more_info_;
            _mainForm.label1.AutoSize = true;
            _mainForm.label1.Spotify.Location = new System.Drawing.Point(9, 13);
            _mainForm.label1.Spotify.Name = "label1";
            _mainForm.label1.Spotify.Size = new System.Drawing.Size(94, 13);
            _mainForm.label1.Spotify.TabIndex = 60;
            _mainForm.label1.Text = Properties.Resources.MainForm_InitializeComponent_Recording_device_;
            _mainForm.deviceListBox.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.deviceListBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _mainForm.deviceListBox.FormattingEnabled = true;
            _mainForm.deviceListBox.Spotify.Location = new System.Drawing.Point(24, 29);
            _mainForm.deviceListBox.Spotify.Name = "deviceListBox";
            _mainForm.deviceListBox.Spotify.Size = new System.Drawing.Size(215, 21);
            _mainForm.deviceListBox.Spotify.TabIndex = 55;
            _mainForm.browseButton.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.browseButton.Image = (System.Drawing.Image)resources.GetObject("browseButton.Image");
            _mainForm.browseButton.Spotify.Location = new System.Drawing.Point(211, 150);
            _mainForm.browseButton.Spotify.Name = "browseButton";
            _mainForm.browseButton.Spotify.Size = new System.Drawing.Size(28, 28);
            _mainForm.browseButton.Spotify.TabIndex = 76;
            _mainForm.browseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _mainForm.browseButton.UseVisualStyleBackColor = true;
            _mainForm.browseButton.Spotify.Click += _mainForm.BrowseButtonClick;
            _mainForm.label3.AutoSize = true;
            _mainForm.label3.Spotify.Location = new System.Drawing.Point(9, 138);
            _mainForm.label3.Spotify.Name = "label3";
            _mainForm.label3.Spotify.Size = new System.Drawing.Size(169, 13);
            _mainForm.label3.Spotify.TabIndex = 61;
            _mainForm.label3.Text = Properties.Resources.MainForm_InitializeComponent_Save_recorded_mp3_s_in_this_folder_;
            _mainForm.outputFolderTextBox.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.outputFolderTextBox.Spotify.Location = new System.Drawing.Point(24, 155);
            _mainForm.outputFolderTextBox.Spotify.Name = "outputFolderTextBox";
            _mainForm.outputFolderTextBox.ReadOnly = true;
            _mainForm.outputFolderTextBox.Spotify.Size = new System.Drawing.Size(181, 20);
            _mainForm.outputFolderTextBox.Spotify.TabIndex = 71;
            _mainForm.label4.AutoSize = true;
            _mainForm.label4.Spotify.Location = new System.Drawing.Point(9, 191);
            _mainForm.label4.Spotify.Name = "label4";
            _mainForm.label4.Spotify.Size = new System.Drawing.Size(63, 13);
            _mainForm.label4.Spotify.TabIndex = 62;
            _mainForm.label4.Text = Properties.Resources.MainForm_InitializeComponent_Mp3_bitrate_;
            _mainForm.bitrateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _mainForm.bitrateComboBox.FormattingEnabled = true;
            _mainForm.bitrateComboBox.Spotify.Location = new System.Drawing.Point(78, 188);
            _mainForm.bitrateComboBox.Spotify.Name = "bitrateComboBox";
            _mainForm.bitrateComboBox.Spotify.Size = new System.Drawing.Size(161, 21);
            _mainForm.bitrateComboBox.Spotify.TabIndex = 54;
            _mainForm.label7.Spotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _mainForm.label7.Spotify.Location = new System.Drawing.Point(10, 244);
            _mainForm.label7.Spotify.Name = "label7";
            _mainForm.label7.Spotify.Size = new System.Drawing.Size(73, 16);
            _mainForm.label7.Spotify.TabIndex = 57;
            _mainForm.label7.Text = Properties.Resources.MainForm_InitializeComponent__The_Picture_;
            label7.Spotify.Visible = false;
            _mainForm.tabPageLog.Spotify.Controls.Add(_mainForm.listBoxLog);
            _mainForm.tabPageLog.Location = new System.Drawing.Point(4, 22);
            _mainForm.tabPageLog.Spotify.Name = "tabPageLog";
            _mainForm.tabPageLog.Spotify.Padding = new System.Windows.Forms.Padding(3);
            _mainForm.tabPageLog.Spotify.Size = new System.Drawing.Size(245, 612);
            _mainForm.tabPageLog.TabIndex = 5;
            _mainForm.tabPageLog.Text = Properties.Resources.MainForm_InitializeComponent_Log;
            _mainForm.tabPageLog.UseVisualStyleBackColor = true;
            _mainForm.listBoxLog.Spotify.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _mainForm.listBoxLog.FormattingEnabled = true;
            _mainForm.listBoxLog.Spotify.Location = new System.Drawing.Point(8, 6);
            _mainForm.listBoxLog.Spotify.Name = "listBoxLog";
            _mainForm.listBoxLog.Spotify.Size = new System.Drawing.Size(231, 589);
            _mainForm.listBoxLog.Spotify.TabIndex = 1;
            _mainForm.pictureBoxAlbumCover.Spotify.Location = new System.Drawing.Point(3, 3);
            _mainForm.pictureBoxAlbumCover.Spotify.Name = "pictureBoxAlbumCover";
            _mainForm.pictureBoxAlbumCover.Spotify.Size = new System.Drawing.Size(654, 631);
            _mainForm.pictureBoxAlbumCover.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            _mainForm.pictureBoxAlbumCover.TabIndex = 85;
            _mainForm.pictureBoxAlbumCover.TabStop = false;
            _mainForm.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _mainForm.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {_mainForm.toolStripButton_Back, _mainForm.toolStripButton_Home, _mainForm.toolStripSeparator3, _mainForm.toolStripButtonHideSidebar });
            _mainForm.toolStrip1.Spotify.Location = new System.Drawing.Point(0, 0);
            _mainForm.toolStrip1.Spotify.Name = "toolStrip1";
            _mainForm.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            _mainForm.toolStrip1.Spotify.Size = new System.Drawing.Size(919, 25);
            _mainForm.toolStrip1.Spotify.TabIndex = 0;
            _mainForm.toolStrip1.Spotify.Text = "";
            _mainForm.toolStripButton_Back.Enabled = false;
            _mainForm.toolStripButton_Back.Image = (System.Drawing.Image)resources.GetObject("toolStripButton_Back.Image");
            _mainForm.toolStripButton_Back.ImageTransparentColor = System.Drawing.Color.Magenta;
            _mainForm.toolStripButton_Back.Name = "toolStripButton_Back";
            _mainForm.toolStripButton_Back.Size = new System.Drawing.Size(52, 22);
            _mainForm.toolStripButton_Back.Text = Properties.Resources.MainForm_InitializeComponent_Back;
            _mainForm.toolStripButton_Home.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _mainForm.toolStripButton_Home.Enabled = false;
            _mainForm.toolStripButton_Home.Image = (System.Drawing.Image)resources.GetObject("toolStripButton_Home.Image");
            _mainForm.toolStripButton_Home.ImageTransparentColor = System.Drawing.Color.Magenta;
            _mainForm.toolStripButton_Home.Name = "toolStripButton_Home";
            _mainForm.toolStripButton_Home.Size = new System.Drawing.Size(23, 22);
            _mainForm.toolStripButton_Home.Text = Properties.Resources.MainForm_InitializeComponent_Reload;
            _mainForm.toolStripButton_Home.ToolTipText = Properties.Resources.MainForm_InitializeComponent_Reload_page;
            _mainForm.toolStripSeparator3.Name = "toolStripSeparator3";
            _mainForm.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            _mainForm.toolStripButtonHideSidebar.Image = (System.Drawing.Image)resources.GetObject("toolStripButtonHideSidebar.Image");
            _mainForm.toolStripButtonHideSidebar.ImageTransparentColor = System.Drawing.Color.Magenta;
            _mainForm.toolStripButtonHideSidebar.Name = "toolStripButtonHideSidebar";
            _mainForm.toolStripButtonHideSidebar.Size = new System.Drawing.Size(127, 22);
            _mainForm.toolStripButtonHideSidebar.Text = Properties.Resources.MainForm_InitializeComponent_Hide_Control_Panel;
            _mainForm.toolStripButtonHideSidebar.Click += toolStripButtonHideSidebar_Click;
            _mainForm.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
            _mainForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            _mainForm.ClientSize = new System.Drawing.Size(919, 666);
            _mainForm.Controls.Add(_mainForm.toolStrip1);
            _mainForm.Controls.Add(_mainForm.splitContainer1);
            _mainForm.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            _mainForm.Name = "MainForm";
            _mainForm.Text = Properties.Resources.MainForm_InitializeComponent_Spotify_Recorder;
            _mainForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            _mainForm.FormClosing += MainForm_FormClosing;
            _mainForm.splitContainer1.Panel1.Spotify.ResumeLayout(false);
            _mainForm.splitContainer1.Panel2.Spotify.ResumeLayout(false);
            _mainForm.splitContainer1.Panel2.Spotify.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) _mainForm.splitContainer1).EndInit();
            _mainForm.splitContainer1.Spotify.ResumeLayout(false);
            _mainForm.tabControl1.Spotify.ResumeLayout(false);
            _mainForm.tabPage1.Spotify.ResumeLayout(false);
            _mainForm.tabPage1.Spotify.PerformLayout();
            _mainForm.contextMenuStrip1.Spotify.ResumeLayout(false);
            _mainForm.tabPage2.Spotify.ResumeLayout(false);
            _mainForm.tabPage2.Spotify.PerformLayout();
            _mainForm.tabPageLog.Spotify.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) _mainForm.pictureBoxAlbumCover).EndInit();
            _mainForm.toolStrip1.Spotify.ResumeLayout(false);
            _mainForm.toolStrip1.Spotify.PerformLayout();
            _mainForm.ResumeLayout(false);
            _mainForm.PerformLayout();
        }

        private void initVariables()
        {
            _isConnected = false;
            _mainForm._baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            _mainForm._duration = 0;
            _mainForm._albumArtQuality = 0;
            _mainForm._currentTag = new Mp3Tag("", "", "", "", null);
            _mainForm._recordingTag = new Mp3Tag("", "", "", "", null);
            _mainForm._currentSongName = string.Empty;
            _mainForm._oldSongName = string.Empty;
            _mainForm._programStartingTime = System.DateTime.Now;
            _mainForm.SongCountToSave = 0;
            _mainForm._logPath = System.IO.Path.Combine(_mainForm._baseDir, "Log.txt");
        }

        private void linkLabelAbout_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("About.html");
        }

        private void linkLabelHelp_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("Help.html");
        }

        private void MainForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
            _mainForm.buttonStopRecording.PerformClick();
            if (_isConnected)
            {
                _spotify.ListenForEvents = false;
                _spotify.Dispose();
            }

            Util.SetDefaultBitrate(_mainForm.bitrateComboBox.SelectedIndex);
            Util.SetDefaultDevice(_mainForm.deviceListBox.SelectedItem.ToString());
            Util.SetDefaultOutputPath(_mainForm.outputFolderTextBox.Text);
            Util.SetDefaultArtQuality(int.Parse(_mainForm.textBoxAlbumArtQuality.Text));
            _mainForm._writer.Close();
            _mainForm._writer.Dispose();
        }

        private void OnLoad(object sender, System.EventArgs eventArgs)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic), "SpotifyRecorder"));
            _mainForm.LoadWasapiDevicesCombo();
            _mainForm.LoadBitrateCombo();
            _mainForm.LoadUserSettings();
            _mainForm.AddAllRecordingToList();
            _mainForm.songLabel.Text = string.Empty;
            _mainForm.encodingLabel.Text = string.Empty;
            _mainForm._folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = _mainForm.outputFolderTextBox.Text
            };
            _mainForm.versionLabel.Text = string.Format("Version {0}", System.Windows.Forms.Application.ProductVersion);
            buttonStopRecording.Spotify.Enabled = false;
            try
            {
                _mainForm._soundCardRecorder = new SoundCardRecorder(CreateOutputFileName("deleteme", "wav"), "");
                _mainForm._soundCardRecorder.Dispose();
                _mainForm._soundCardRecorder = null;
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
                if (!string.IsNullOrEmpty(_mainForm.outputFolderTextBox.Text))
                {
                    addToLog(string.Concat("Recorded file: ", _mainForm.outputFolderTextBox.Text));
                    _mainForm.encodingLabel.Text = _mainForm._oldSongName;
                    if (string.IsNullOrEmpty(_mainForm._oldSongName))
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
            string selectedValue = (string) _mainForm.bitrateComboBox.SelectedValue;
            (new System.Threading.Tasks.Task(() => ConvertToMp3(song, selectedValue, oldTrack))).Start();
        }

        public static string RemoveInvalidFilePathCharacters(string filename, string replaceChar)
        {
            string regexSearch = string.Concat(new string(System.IO.Path.GetInvalidFileNameChars()), new string(System.IO.Path.GetInvalidPathChars()));
            return (new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)))).Replace(filename, replaceChar);
        }

        public void SaveJpeg(int Quality, System.Drawing.Image albumArt)
        {
            using (System.Drawing.Image image = albumArt)
            {
                int width = image.Size.Width;
                System.Drawing.Size size = image.Size;
                using (System.Drawing.Image clone = new System.Drawing.Bitmap(image, new System.Drawing.Size(width, size.Height)))
                {
                    _mainForm.CompressImage(clone, Quality, System.IO.Path.Combine(_mainForm.outputFolderTextBox.Text, "Album_Art.Png"));
                }
            }
        }

        private void StartRecording()
        {
            if (!_mainForm.nowPlayingLabel.Text.Equals(Properties.Resources.MainForm_UpdateTrack_Advert))
            {
                _mainForm._currentSongName = string.Concat(_mainForm._currentTag.Artist, " - ", _mainForm._currentTag.Title);
                if (System.IO.File.Exists(string.Concat(_mainForm._currentSongName, ".mp3")))
                {
                    addToLog(string.Concat(_mainForm._currentSongName, ".mp3 Exists!"));
                    return;
                }

                _mainForm._soundCardRecorder = new SoundCardRecorder(CreateOutputFileName(_mainForm._currentSongName, "wav"), _mainForm._currentSongName);
                _mainForm._soundCardRecorder.Start();
                _mainForm.songLabel.Text = _mainForm._currentSongName;
                addToLog("Recording!");
                return;
            }
            addToLog("Advert Playing please wait for it to finish to start recording.");
        }

        private void StopRecording()
        {
            if (_mainForm._soundCardRecorder != null)
            {
                addToLog("Recording stopped");
                _mainForm._soundCardRecorder.Stop();
                _mainForm._duration = _mainForm._soundCardRecorder.Duration;
                addToLog(string.Concat("Duration: ", _mainForm._duration));
                _mainForm._soundCardRecorder.Dispose();
                _mainForm._soundCardRecorder = null;
            }
        }

        private void textBoxAlbumArtQuality_TextChanged(object sender, System.EventArgs e)
        {
            try
            {
                if (int.Parse(_mainForm.textBoxAlbumArtQuality.Text) >= 100 || int.Parse(_mainForm.textBoxAlbumArtQuality.Text) <= 0)
                {
                    System.Windows.Forms.MessageBox.Show(Properties.Resources.MainForm_textBoxAlbumArtQuality_TextChanged_This_value_must_be_between_0_100);
                }
                else
                {
                    _mainForm._albumArtQuality = int.Parse(_mainForm.textBoxAlbumArtQuality.Text);
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
                _mainForm.SongCountToSave = int.Parse(_mainForm.textBoxSongLimit.Text);
            }
            catch (System.Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.Message);
            }
        }

        private void toolStripButtonHideSidebar_Click(object sender, System.EventArgs e)
        {
            if (!_mainForm.toolStripButtonHideSidebar.Checked)
            {
                _mainForm.splitContainer1.Panel1Collapsed = true;
            }
            else
            {
                _mainForm.splitContainer1.Panel1Collapsed = false;
            }

            _mainForm.toolStripButtonHideSidebar.Checked = !_mainForm.toolStripButtonHideSidebar.Checked;
        }

        private void toolStripMenuItem_ClearList_Click(object sender, System.EventArgs e)
        {
            _mainForm.listBoxRecordings.Items.Clear();
        }

        private void toolStripMenuItem_Delete_Click(object sender, System.EventArgs e)
        {
            if (_mainForm.listBoxRecordings.SelectedItem != null)
            {
                try
                {
                    System.IO.File.Delete(CreateOutputFileName((string) _mainForm.listBoxRecordings.SelectedItem, "mp3"));
                    _mainForm.listBoxRecordings.Items.Remove(_mainForm.listBoxRecordings.SelectedItem);
                    if (_mainForm.listBoxRecordings.Items.Count > 0)
                    {
                        _mainForm.listBoxRecordings.SelectedIndex = 0;
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
            System.Diagnostics.Process.Start(_mainForm.outputFolderTextBox.Text);
        }

        private void toolStripMenuItem_Play_Click(object sender, System.EventArgs e)
        {
            if (_mainForm.listBoxRecordings.SelectedItem != null)
            {
                try
                {
                    System.Diagnostics.Process.Start(System.IO.Path.Combine(_mainForm.outputFolderTextBox.Text, (string) _mainForm.listBoxRecordings.SelectedItem));
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show(Properties.Resources.MainForm_toolStripMenuItem_Play_Click_Could_not_play_song___);
                }
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

                _mainForm.clientVersionLabel.Text = status.ClientVersion;
                _mainForm.versionLabel2.Text = status.Version.ToString();
                if (status.Track != null)
                {
                    UpdateTrack(status.Track);
                }
            }
        }

        private void UpdateRecordingTag(SpotifyAPI.Local.Models.Track oldTrack)
        {
            _mainForm._recordingTag.Title = (!string.IsNullOrEmpty(oldTrack.TrackResource.Name) ? oldTrack.TrackResource.Name : string.Empty);
            _mainForm._recordingTag.Artist = oldTrack.ArtistResource.Name;
            _mainForm._recordingTag.TrackUri = oldTrack.TrackResource.Uri;
            _mainForm._recordingTag.AlbumName = (!string.IsNullOrEmpty(oldTrack.AlbumResource.Name) ? oldTrack.AlbumResource.Name : string.Empty);
        }

        private void UpdateTag()
        {
            if (_mainForm._currentTrack == null)
                return;
            if (_mainForm._currentTrack.TrackType == "local")
            {
                return;
            }
            try
            {
                _mainForm._currentTag.Title = (string.IsNullOrEmpty(_mainForm._currentTrack.TrackResource.Name) ? string.Empty : _mainForm._currentTrack.TrackResource.Name);
                _mainForm._currentTag.Artist = _mainForm._currentTrack.ArtistResource.Name;
                _mainForm._currentTag.TrackUri = _mainForm._currentTrack.TrackResource.Uri;
                _mainForm._currentTag.AlbumName = (string.IsNullOrEmpty(_mainForm._currentTrack.AlbumResource.Name) ? string.Empty : _mainForm._currentTrack.AlbumResource.Name);
                _mainForm.UpdateAlbumArt();
            }
            catch (System.Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.Message);
            }
        }

        public void UpdateTrack(SpotifyAPI.Local.Models.Track track)
        {
            _mainForm._currentTrack = track;
            if (_mainForm._currentTrack.TrackType == "local")
            {
                System.Windows.Forms.Label label = _mainForm.nowPlayingLabel;
                label.Text = string.Concat(label.Text, "(Local)");
                return;
            }
            if (track.IsAd())
            {
                _mainForm.nowPlayingLabel.Text = Properties.Resources.MainForm_UpdateTrack_Advert;
                _mainForm._currentSongName = string.Empty;
                return;
            }
            UpdateTag();
            _mainForm._oldSongName = _mainForm._currentSongName;
            _mainForm._currentSongName = string.Concat(_mainForm._currentTag.Artist, " - ", _mainForm._currentTag.Title);
            _mainForm.nowPlayingLabel.Text = _mainForm._currentSongName;
            addToLog(string.Concat("Now playing: ", _mainForm._currentSongName, " (", _mainForm._currentTag.TrackUri, ") RECORDING!"));
        }
    }
}