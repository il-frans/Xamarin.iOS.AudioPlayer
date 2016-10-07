using System;
using System.Collections.Generic;
using Frans.Xamarin.iOS.AudioPlayer;
using UIKit;

namespace Example
{
	public partial class ViewController : UIViewController
	{
		AudioPlayer _AudioPlayer;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			resetUI();

			// Configure AudioPlayer
			_AudioPlayer = new AudioPlayer(new List<AudioPlayerItem>()
			{
				new AudioPlayerItem(new Foundation.NSUrl("http://www.noiseaddicts.com/samples_1w72b820/2514.mp3"), "Fight Club"),
				new AudioPlayerItem(new Foundation.NSUrl("http://www.kissfm.ro/listen.pls")),
				new AudioPlayerItem(new Foundation.NSUrl("http://www.kissfm.ro/listen.pls"))
			});

			// Register for the events
			_AudioPlayer.DidChangePlaybackProgress += AudioPlayer_DidChangePlaybackProgress;
			_AudioPlayer.DidChangeState += AudioPlayer_DidChangeState;
			_AudioPlayer.DidLoadItem += AudioPlayer_DidLoadItem;
			_AudioPlayer.DidUpdateMetadata += AudioPlayer_DidUpdateMetadata;
			_AudioPlayer.DidStartPlayingNewItem += AudioPlayer_DidStartPlayingNewItem;

			_AudioPlayer.Play();
			// Perform any additional setup after loading the view, typically from a nib.
		}

		private void resetUI()
		{
			PlaybackProgressSlider.Value = 0f;
			VolumeSlider.Value = 1f;

			TitleLabel.Text = "Track Title";

			TrackDurationLabel.Text = "0:00";
			CurrentTrackTimeLabel.Text = "0:00";

			PlayPauseButton.SetTitle("Play", UIControlState.Normal);

			PlaybackProgressSlider.Enabled = false;
		}

		#region AudioPlayer Events

		private void AudioPlayer_DidLoadItem(AudioPlayer p_Sender, AudioPlayerItem p_Item)
		{
			System.Diagnostics.Debug.WriteLine("Item load : '{0}'", p_Item.LocalTitle);
		}

		private void AudioPlayer_DidChangePlaybackProgress(AudioPlayer p_Sender)
		{
			if (p_Sender.CurrentItem != null)
			{
				var currentTime = p_Sender.CurrentItem.CurrentTime;

				if (PlaybackProgressSlider.Enabled == true)
				{
					// Only update the progress if it is enabled. This solves the issue of progress for infinit steams
					PlaybackProgressSlider.Value = (int)currentTime;
				}
				CurrentTrackTimeLabel.Text = secondsToTimeString((int)currentTime);
			}
			else
			{
				resetUI();
			}
		}

		private void AudioPlayer_DidChangeState(AudioPlayer p_Sender, AudioPlayer.State p_OldState)
		{
			if (p_Sender.CurrentState == AudioPlayer.State.Loading)
			{
				LoadingActivityIndicator.StartAnimating();
				PlayPauseButton.Enabled = false;
			}
			else
			{
				LoadingActivityIndicator.StopAnimating();
				PlayPauseButton.Enabled = true;
			}

			switch (p_Sender.CurrentState)
			{
				case AudioPlayer.State.Ready:
				case AudioPlayer.State.Paused:
				case AudioPlayer.State.Failed:
					PlayPauseButton.SetTitle("Play", UIControlState.Normal);
					break;
				case AudioPlayer.State.Loading:
				case AudioPlayer.State.Playing:
					PlayPauseButton.SetTitle("Pause", UIControlState.Normal);
					break;					
			}

			switch (p_Sender.CurrentState)
			{
				case AudioPlayer.State.Failed:
				case AudioPlayer.State.Loading:
					PlaybackProgressSlider.Enabled = false;
					break;
				case AudioPlayer.State.Paused:
				case AudioPlayer.State.Playing:
				case AudioPlayer.State.Ready:
					PlaybackProgressSlider.Enabled = true;
					break;
			}

			VolumeSlider.Value = _AudioPlayer.Volume;
		}

		private void AudioPlayer_DidUpdateMetadata(AudioPlayer p_Sender, AudioPlayerItem p_Item)
		{
			// use the event to update an UI with data from the metadata once it is fetched.
		}

		private void AudioPlayer_DidStartPlayingNewItem(AudioPlayer p_Sender, AudioPlayerItem p_Item)
		{
			TrackDurationLabel.Text = secondsToTimeString((int)p_Item.Metadata.Duration);

			TitleLabel.Text = p_Sender.CurrentItem.LocalTitle;

			if (double.IsNaN(p_Item.Metadata.Duration.Value) != true)
			{
				// NaN may occur if the streaming media is infinit such as radio
				PlaybackProgressSlider.MaxValue = (float)p_Item.Metadata.Duration;
			}
			else
			{
				PlaybackProgressSlider.Enabled = false;
				PlaybackProgressSlider.Value = 0;
			}
		}

		#endregion

		partial void VolumeSlider_ValueChanged(UISlider sender)
		{
			if (_AudioPlayer != null)
			{
				_AudioPlayer.Volume = VolumeSlider.Value;
			}
		}

		partial void PlaybackProgressSlider_ValueChanged(UISlider sender)
		{
			_AudioPlayer?.Seek((int)PlaybackProgressSlider.Value, (_AudioPlayer.CurrentState == AudioPlayer.State.Playing));
		}

		partial void PreviousButton_TouchUpInside(UIButton sender)
		{
			_AudioPlayer?.PlayPrevious();
		}

		partial void NextButton_TouchUpInside(UIButton sender)
		{
			_AudioPlayer?.PlayNext();
		}

		partial void PlayPauseButton_TouchUpInside(UIButton sender)
		{
			switch (_AudioPlayer.CurrentState)
			{
				case AudioPlayer.State.Ready:
				case AudioPlayer.State.Paused:
					_AudioPlayer.Play();
					break;
				case AudioPlayer.State.Playing:
					_AudioPlayer.Pause();
					break;
			}
		}

		partial void ReplayButton_TouchUpInside(UIButton sender)
		{
			resetUI();
			_AudioPlayer.Replay();
		}

		protected ViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		private string secondsToTimeString(int p_Seconds)
		{
			TimeSpan time = TimeSpan.FromSeconds(p_Seconds);
			return time.ToString(@"mm\:ss");
		}
	}
}
