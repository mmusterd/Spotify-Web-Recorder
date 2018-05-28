﻿using System.ComponentModel;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;

namespace SpotifyWebRecorder.Forms.UI
{
    public partial class MainForm : Form
    {
		Timer stateCheckTimer = new Timer();

		private enum RecorderState
        {
            NotRecording = 1,
            WaitingForRecording = 2,
            Recording = 3,
            Closing = 4,
        }

        private SoundCardRecorder SoundCardRecorder { get; set; }
		private bool MutedSound = false;
        private FolderBrowserDialog folderDialog;
        private RecorderState _currentApplicationState = RecorderState.NotRecording;


		private enum SpotifyState
		{
			Unknown = 0,
			Paused = 1,
			Playing = 2,
			Ad = 3,
		}

		private Mp3Tag currentTrack = new Mp3Tag("","","");
		private Mp3Tag recordingTrack = new Mp3Tag("","","");
		private SpotifyState currentSpotifyState = SpotifyState.Unknown;

		public MainForm()
        {
            InitializeComponent();

            //check if it is windows 7
            if (Environment.OSVersion.Version.Major < 6)
            {
                MessageBox.Show("This application is optimized for windows 7 or higher");
                Close();
            }

            Load += OnLoad;
            Closing += OnClosing;

			string baseDir = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location );

			stateCheckTimer.Interval = 25;
			stateCheckTimer.Tick += new EventHandler( stateCheckTimer_Tick );
			stateCheckTimer.Start();

			addToLog( "Application started..." );

#if !DEBUG
			tabControl1.TabPages.RemoveByKey("tabPageLog");
			browser.Navigate( Util.GetDefaultURL() );
#else
			//browser.Navigate( Util.GetDefaultURL() );
#endif
		}

        private void ChangeApplicationState(RecorderState newState)
        {
            ChangeGui(newState);

            switch (_currentApplicationState)
            {
                case RecorderState.NotRecording:
                    switch (newState)
                    {
                        case RecorderState.NotRecording:
                            break;
                        case RecorderState.WaitingForRecording:
                            break;
                        case RecorderState.Recording:
							StartRecording( (MMDevice)deviceListBox.SelectedItem);
                            break;
                        case RecorderState.Closing:
                            break;
                    }

                    break;
                case RecorderState.WaitingForRecording:
                    switch (newState)
                    {
                        case RecorderState.NotRecording:
                            break;
                        case RecorderState.WaitingForRecording:
                            throw new Exception(string.Format("NY {0} - {1}",_currentApplicationState,newState));
                        case RecorderState.Recording:
							StartRecording( (MMDevice)deviceListBox.SelectedItem);
                            break;
                        case RecorderState.Closing:
                            //Close();
                            break;
                    }
                    break;
                case RecorderState.Recording:
                    switch (newState)
                    {
                        case RecorderState.NotRecording:
                            StopRecording();
                            break;
                        case RecorderState.Recording: //file changed
                            StopRecording();
							StartRecording( (MMDevice)deviceListBox.SelectedItem);
                            break;
                        case RecorderState.WaitingForRecording: //file changed
                            StopRecording();
                            break;
                    }
                    break;

            }
            _currentApplicationState = newState;
        }

        private void ChangeGui(RecorderState state)
        {
            switch (state)
            {
                case RecorderState.NotRecording:
                    browseButton.Enabled = true;
                    buttonStartRecording.Enabled = true;
                    buttonStopRecording.Enabled = false;
                    deviceListBox.Enabled = true;
                    break;
                case RecorderState.WaitingForRecording:
                    browseButton.Enabled = false;
                    buttonStartRecording.Enabled = false;
                    buttonStopRecording.Enabled = true;
                    deviceListBox.Enabled = false;
                    break;
                case RecorderState.Recording:
                    browseButton.Enabled = false;
                    buttonStartRecording.Enabled = false;
                    buttonStopRecording.Enabled = true;
                    deviceListBox.Enabled = false;
                    break;
            }
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            //load the available devices
            LoadWasapiDevicesCombo();

            //load the different bitrates
            LoadBitrateCombo();

            //Load user settings
            LoadUserSettings();

            //set the change event if filePath is 
            songLabel.Text = string.Empty;
			encodingLabel.Text = string.Empty;

            folderDialog = new FolderBrowserDialog { SelectedPath = outputFolderTextBox.Text };

            versionLabel.Text = string.Format("Version {0}", Application.ProductVersion);

            ChangeApplicationState(_currentApplicationState);

			// instantiate the sound recorder once in an attempt to reduce lag the first time used
			try
			{
				SoundCardRecorder = new SoundCardRecorder( (MMDevice)deviceListBox.SelectedItem, CreateOutputFile( "deleteme", "wav" ), "" );
				SoundCardRecorder.Dispose();
				SoundCardRecorder = null;
				if( File.Exists( CreateOutputFile( "deleteme", "wav" )  ) ) File.Delete( CreateOutputFile( "deleteme", "wav" ) );
			}
			catch( Exception ex )
			{
			}

        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
			StopRecording();
            ChangeApplicationState(RecorderState.Closing);
			Util.SetDefaultBitrate( bitrateComboBox.SelectedIndex );
            Util.SetDefaultDevice(deviceListBox.SelectedItem.ToString());
            Util.SetDefaultOutputPath(outputFolderTextBox.Text);
            Util.SetDefaultThreshold((int)thresholdTextBox.Value);
            Util.SetDefaultThreshold((int)thresholdTextBox.Value);
            Util.SetDefaultThresholdEnabled(thresholdCheckBox.Checked);
			Util.SetDefaultMuteAdsEnabled( MuteOnAdsCheckBox.Checked );
        }

        private void ButtonPlayClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                Process.Start(CreateOutputFile((string)listBoxRecordings.SelectedItem, "mp3"));
            }
        }

        private void ButtonDeleteClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                try
                {
                    File.Delete(CreateOutputFile((string)listBoxRecordings.SelectedItem, "mp3"));
                    listBoxRecordings.Items.Remove(listBoxRecordings.SelectedItem);
                    if (listBoxRecordings.Items.Count > 0)
                    {
                        listBoxRecordings.SelectedIndex = 0;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not delete recording");
                }
            }
        }

        private void ButtonOpenFolderClick(object sender, EventArgs e)
        {
            Process.Start(outputFolderTextBox.Text);
        }

        private void ButtonStartRecordingClick(object sender, EventArgs e)
        {
            ChangeApplicationState(songLabel.Text.Trim().Length > 0
                                       ? RecorderState.Recording
                                       : RecorderState.WaitingForRecording);
        }

        private void ButtonStopRecordingClick(object sender, EventArgs e)
        {
            ChangeApplicationState(RecorderState.NotRecording);
        }

        private void ClearButtonClick(object sender, EventArgs e)
        {
            listBoxRecordings.Items.Clear();
        }

        private string CreateOutputFile(string song, string extension)
        {
            song = RemoveInvalidFilePathCharacters(song, string.Empty);
            return Path.Combine(outputFolderTextBox.Text, string.Format("{0}.{1}", song, extension));
        }

        private void StartRecording(MMDevice device)
        {
            if (device != null)
            {
                if(SoundCardRecorder!=null)
                    StopRecording();

				recordingTrack = new Mp3Tag( currentTrack.Title, currentTrack.Artist, currentTrack.TrackUri );

                SoundCardRecorder = new SoundCardRecorder(
								device, CreateOutputFile( recordingTrack.Artist + " - " + recordingTrack.Title, "wav" ),
								recordingTrack.Artist + " - " + recordingTrack.Title );
                SoundCardRecorder.Start();

				addToLog( "Recording!" );
            }
        }

        private void StopRecording()
        {
            string filePath = string.Empty;
            string song = string.Empty;
			int duration = 0;
            if (SoundCardRecorder != null)
            {
				addToLog( "Recording stopped" );

                SoundCardRecorder.Stop();
                filePath = SoundCardRecorder.FilePath;
                song = SoundCardRecorder.Song;
                duration = SoundCardRecorder.Duration;
					addToLog( "Duration: " + duration + " (Limit: " + thresholdTextBox.Value + ")");
				SoundCardRecorder.Dispose();
                SoundCardRecorder = null;

				if( duration < (int)thresholdTextBox.Value && thresholdCheckBox.Checked )
				{
					File.Delete( filePath );
					addToLog( "Recording too short; deleting file..." );
				}
				else
				{
					if( !string.IsNullOrEmpty( filePath ) )
					{
						addToLog( "Recorded file: " + filePath );
						encodingLabel.Text = song;
						PostProcessing( song );
					}
				}
			}
        }

		private void PostProcessing( string song )
		{
			string bitrate = (string)bitrateComboBox.SelectedValue;
			Task t = new Task( () => ConvertToMp3( song, bitrate ) );
			t.Start();
		}

		private void ConvertToMp3( string filePath, string bitrate )
        {
            if (!File.Exists(CreateOutputFile(filePath, "wav")))
                return;

			addToLog( "Converting to mp3... " );

            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            //Mp3Tag tag = Util.ExtractMp3Tag(filePath);

            process.StartInfo.FileName = "lame.exe";
			process.StartInfo.Arguments = string.Format( "{2} --tt \"{3}\" --ta \"{4}\" --tc \"{5}\"  \"{0}\" \"{1}\"",
				CreateOutputFile( filePath, "wav" ),
				CreateOutputFile( recordingTrack.Artist + " - " + recordingTrack.Title, "mp3" ),
				bitrate,
				recordingTrack.Title,
				recordingTrack.Artist,
				recordingTrack.TrackUri );

            process.StartInfo.WorkingDirectory = new FileInfo(Application.ExecutablePath).DirectoryName;
            addToLog( "Starting LAME..." );
			process.Start();
            //process.WaitForExit(20000);
			process.WaitForExit();
			addToLog( "  LAME exit code: " + process.ExitCode );
			if( !process.HasExited )
			{
				addToLog( "Killing LAME process!" );
				process.Kill();
			}
			addToLog( "LAME finished!" );

			addToLog( "Deleting wav file... " );
            File.Delete(CreateOutputFile(filePath, "wav"));

			addToLog( "Mp3 ready: " + CreateOutputFile( filePath, "mp3" ) );
			AddSongToList( filePath );
        }

		private void AddSongToList(string song)
		{
			if( this.InvokeRequired )
			{
				// if required for thread safety, call self using invoke instead
				this.Invoke( new MethodInvoker( delegate() { AddSongToList( song ); } ) );
			}
			else
			{
				int newItemIndex = listBoxRecordings.Items.Add( song );
				listBoxRecordings.SelectedIndex = newItemIndex;
				encodingLabel.Text = "";
			}
		}

        private void LoadWasapiDevicesCombo()
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            deviceListBox.DataSource = devices;
            deviceListBox.DisplayMember = "FriendlyName";
        }
        private void LoadBitrateCombo()
        {
			Dictionary<string, string> bitrate = new Dictionary<string, string>();
			bitrate.Add( "VBR Extreme (V0)" , "--preset extreme" );
			bitrate.Add( "VBR Standard (V2)" , "--preset standard" );
			bitrate.Add( "VBR Medium (V5)" , "--preset medium" );
			bitrate.Add( "CBR 320" , "--preset insane" );
			bitrate.Add( "CBR 256" , "-b 256" );
			bitrate.Add( "CBR 192" , "-b 192" );
			bitrate.Add( "CBR 160" , "-b 160" );
			bitrate.Add( "CBR 128" , "-b 128" );
			bitrate.Add( "CBR 96" , "-b 96" );

			bitrateComboBox.DataSource = new BindingSource( bitrate	, null ); ;
			bitrateComboBox.DisplayMember = "Key";
			bitrateComboBox.ValueMember = "Value";
        }

        /// <summary>
        /// load the setting from a previous session
        /// </summary>
        private void LoadUserSettings()
        {
            //get/set the device
            string defaultDevice = Util.GetDefaultDevice();

            foreach (MMDevice device in deviceListBox.Items)
            {
                if (device.FriendlyName.Equals(defaultDevice))
                    deviceListBox.SelectedItem = device;
            }

            //set the default output to the music directory
            outputFolderTextBox.Text = Util.GetDefaultOutputPath();

            //set the default bitrate
            bitrateComboBox.SelectedIndex = Util.GetDefaultBitrate();

            thresholdTextBox.Value = Util.GetDefaultThreshold();
            thresholdCheckBox.Checked = Util.GetDefaultThresholdEnabled();
			MuteOnAdsCheckBox.Checked = Util.GetDefaultMuteAdsEnabled();

        }

        public static string RemoveInvalidFilePathCharacters(string filename, string replaceChar)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename, replaceChar);
        }

        private void BrowseButtonClick(object sender, EventArgs e)
        {
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                outputFolderTextBox.Text = folderDialog.SelectedPath;
                Util.SetDefaultOutputPath(folderDialog.SelectedPath);
            }
        }

		private void webBrowser_DocumentCompleted( object sender, WebBrowserDocumentCompletedEventArgs e )
		{
			//Console.Write("Loaded!");
		}
		private void webBrowser_Navigating( object sender, WebBrowserNavigatingEventArgs e )
		{
			//Console.WriteLine( "Navigating to: " + e.Url );
		}

		private void addToLog( string text )
		{
			if( this.InvokeRequired )
			{
				// if required for thread safety, call self using invoke instead
				this.Invoke( new MethodInvoker( delegate() { addToLog( text ); } ) );
			}
			else
			{
				System.Diagnostics.Debug.WriteLine( "[" + DateTime.Now.ToShortTimeString() + "] " + text );
				listBoxLog.Items.Add( "[" + DateTime.Now.ToShortTimeString() + "] " + text );
				listBoxLog.SelectedIndex = listBoxLog.Items.Count-1;
			}
		}

		private void thresholdCheckBox_CheckedChanged( object sender, EventArgs e )
		{
			thresholdTextBox.Enabled = thresholdCheckBox.Checked;
		}

		private void toolStripMenuItem_Play_Click( object sender, EventArgs e )
		{
			if( listBoxRecordings.SelectedItem != null )
			{
				try
				{
					Process.Start( CreateOutputFile( (string)listBoxRecordings.SelectedItem, "mp3" ) );
				}
				catch
				{
					MessageBox.Show( "Could not play song..." );
				}
			}
		}

		private void toolStripMenuItem_Open_Click( object sender, EventArgs e )
		{
			Process.Start( outputFolderTextBox.Text );
		}

		private void toolStripMenuItem_Delete_Click( object sender, EventArgs e )
		{
			if( listBoxRecordings.SelectedItem != null )
			{
				try
				{
					File.Delete( CreateOutputFile( (string)listBoxRecordings.SelectedItem, "mp3" ) );
					listBoxRecordings.Items.Remove( listBoxRecordings.SelectedItem );
					if( listBoxRecordings.Items.Count > 0 )
					{
						listBoxRecordings.SelectedIndex = 0;
					}
				}
				catch( Exception )
				{
					MessageBox.Show( "Could not delete recording..." );
				}
			}
		}

		private void toolStripMenuItem_ClearList_Click( object sender, EventArgs e )
		{
			listBoxRecordings.Items.Clear();
		}

		private void openRecordingDevicesButton_Click( object sender, EventArgs e )
		{
			Process.Start( "control.exe" , "mmsys.cpl,,1");

			/*
			 * If you want to access the Mixer and the other functions, you can use these shortcuts:
			• Master Volume Left: SndVol.exe -f 0
			• Master Volume Right: SndVol.exe -f 49825268
			• Volume Mixer Left: SndVol.exe -r 0
			• Volume Mixer Right: SndVol.exe -r 49490633
			• Playback Devices: control.exe mmsys.cpl,,0
			• Recording Devices: control.exe mmsys.cpl,,1
			• Sounds: control.exe mmsys.cpl,,2
			From http://www.errorforum.com/microsoft-windows-vista-error/4636-vista-tips-tricks-tweaks.html
			 * */

		}

		private void OpenMixerButtonClick( object sender, EventArgs e )
		{
			Process.Start( "sndvol" );
		}

		private void toolStripButtonHideSidebar_Click( object sender, EventArgs e )
		{
			if( toolStripButtonHideSidebar.Checked )
			{
				splitContainer1.Panel1Collapsed = false;
			}
			else
			{
				splitContainer1.Panel1Collapsed = true;
			}
			toolStripButtonHideSidebar.Checked = !toolStripButtonHideSidebar.Checked;
		}

		void stateCheckTimer_Tick( object sender, EventArgs e )
		{
			// figure out what spotify is doing right now
			songLabel.Visible = _currentApplicationState == RecorderState.Recording;
		}

	}
}
