using System;

using Foundation;
using AVFoundation;
using MediaPlayer;
using System.Collections.Generic;
using CoreMedia;
using UIKit;


namespace Frans.Xamarin.iOS.AudioPlayer
{
	public class AudioPlayer : NSObject
	{
		public enum State
		{
			Ready, Playing, Paused, Loading, Failed
		}

		#region Properties

		private AVPlayer _Player;
		private NSObject _ProgressObserver;
		private NSObject _DidPlayToEndTimeNotification;
		private nint _BackgroundIndentifier;

		/// <summary>
		/// Occurs when the state is changed.
		/// The state param is the old state
		/// </summary>
		public event Action<AudioPlayer, State> DidChangeState;
		public event Action<AudioPlayer> DidChangePlaybackProgress;
		public event Action<AudioPlayer, AudioPlayerItem> DidLoadItem;
		public event Action<AudioPlayer, AudioPlayerItem> DidUpdateMetadata;

		public int PlayIndex
		{
			get;
			private set;
		}

		public IList<AudioPlayerItem> QueuedItems
		{
			get;
			private set;
		}

		private State _CurrentState = State.Ready;
		public State CurrentState
		{
			get
			{
				return _CurrentState;
			}
			private set
			{
				var oldState = _CurrentState;
				_CurrentState = value;
				DidChangeState?.Invoke(this, oldState);
			}
		}

		public float Volume
		{
			get
			{
				return (_Player != null) ? _Player.Volume : 0;
			}
			set
			{
				if (_Player != null)
					_Player.Volume = value;
			}
		}

		public AudioPlayerItem CurrentItem
		{
			get
			{
				if ((PlayIndex >= 0) && (PlayIndex < QueuedItems.Count))
				{
					return QueuedItems[PlayIndex];
				}

				return null;
			}
		}

		public bool IsPlayerOperational
		{
			get
			{
				return ((_Player != null) && (CurrentItem != null));
			}
		}

		#endregion

		#region Constructors

		public AudioPlayer(IList<AudioPlayerItem> p_Items)
		{
			configureAudioSession(); // todo wrap around try catch and rasie a fail event

			assignQueuedItems(p_Items);
			configureObservers();
		}

		~AudioPlayer()
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(this);
		}

		#endregion

		#region Jukebox Item Events

		public void JukeboxItem_DidLoadPlayerItem(AudioPlayerItem p_Item)
		{
			DidLoadItem?.Invoke(this, p_Item);

			var indexOfItem = QueuedItems.IndexOf(p_Item);

			if ((CurrentState == State.Loading) && (PlayIndex == indexOfItem))
			{
				registerForPlayToEndNotification();
				startNewPlayer(p_Item.PlayerItem);
			}
		}

		public void JukeboxItem_DidUpdate(AudioPlayerItem p_Item)
		{
			if (CurrentItem != null) // not sure about this, orginal code guard let item = currentItem else return
			{
				updateInfoCenter();
				DidUpdateMetadata?.Invoke(this, p_Item);
			}
		}

		public void JukeboxItem_DidFail(AudioPlayerItem p_Item)
		{
			Stop();
			CurrentState = State.Failed;
		}

		#endregion

		#region Playback

		private void updateInfoCenter()
		{
			var item = CurrentItem;
			if (item == null)
				return;

			var title = item.Metadata.Title ?? item.LocalTitle ?? item.URL.LastPathComponent;
			var currentTime = item.CurrentTime ?? 0;
			var duration = item.Metadata.Duration ?? 0;
			var trackNumber = PlayIndex;
			var tracksCount = QueuedItems.Count;

			var nowPlayingInfo = new MPNowPlayingInfo();
			nowPlayingInfo.PlaybackDuration = duration;
			nowPlayingInfo.Title = title;
			nowPlayingInfo.ElapsedPlaybackTime = currentTime;
			nowPlayingInfo.PlaybackQueueCount = tracksCount;
			nowPlayingInfo.PlaybackQueueIndex = trackNumber;
			nowPlayingInfo.MediaType = MPNowPlayingInfoMediaType.Audio;

			nowPlayingInfo.Artist = item.Metadata?.Artist;
			nowPlayingInfo.AlbumTitle = item.Metadata?.Album;

			if (item.Metadata?.Artwork != null)
			{
				nowPlayingInfo.Artwork = new MPMediaItemArtwork(item.Metadata.Artwork);
			}

			MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = nowPlayingInfo;
		}

		private void playerCurrentItem(AVAsset p_Asset)
		{
			QueuedItems[PlayIndex].RefreshPlayerItem(p_Asset);
			startNewPlayer(QueuedItems[PlayIndex].PlayerItem);

			if (QueuedItems[PlayIndex].PlayerItem != null)
			{
				registerForPlayToEndNotification();
			}
		}

		private void resumePlayback()
		{
			if (CurrentState != State.Playing)
			{
				startProgressTimer();

				if (_Player != null)
				{
					_Player.Play();
				}
				else
				{
					try
					{
						CurrentItem.RefreshPlayerItem(CurrentItem.PlayerItem.Asset);
						startNewPlayer(CurrentItem.PlayerItem);
					}
					catch (Exception ex)
					{
						throw ex;
					}
				}

				CurrentState = State.Playing;
			}
		}

		private void invalidatePlayback(bool p_ShouldResetIndex = true)
		{
			startProgressTimer();

			_Player?.Pause();
			_Player = null;

			if (p_ShouldResetIndex)
			{
				PlayIndex = 0;
			}
		}

		private void startNewPlayer(AVPlayerItem p_Item)
		{
			invalidatePlayback(false);

			_Player = new AVPlayer(p_Item);
			_Player.AllowsExternalPlayback = false;

			startProgressTimer();

			Seek(0, true);

			updateInfoCenter();
		}

		#endregion

		#region Items Related

		private void assignQueuedItems(IList<AudioPlayerItem> p_Items)
		{
			QueuedItems = p_Items;

			foreach (var item in QueuedItems)
			{
				addJukeboxItemEvents(item);
			}
		}

		private void loadPlaybackItem()
		{
			if ((PlayIndex >= 0) && (PlayIndex < QueuedItems.Count))
			{
				stopProgressTimer();
				_Player?.Pause();
				QueuedItems[PlayIndex].LoadPlayerItem();
				CurrentState = State.Loading;
			}
		}

		private void preloadNextAndPrevious(int p_AtIndex)
		{
			if (QueuedItems.Count > 0)
			{
				if (p_AtIndex - 1 >= 0)
				{
					QueuedItems[p_AtIndex - 1].LoadPlayerItem();
				}

				if (p_AtIndex + 1 < QueuedItems.Count)
				{
					QueuedItems[p_AtIndex + 1].LoadPlayerItem();
				}
			}
		}

		#endregion

		#region Progress Tracking

		private void startProgressTimer()
		{
			if ((_Player != null) && (_Player.CurrentItem?.Duration.IsInvalid == false))
			{
				_ProgressObserver = _Player.AddPeriodicTimeObserver(
					interval: CMTime.FromSeconds(0.05, 1000000000),
					queue: null,
					handler: (p_CMTime) => timeAction());
			}
		}

		private void stopProgressTimer()
		{
			if (_ProgressObserver != null)
			{
				_Player?.RemoveTimeObserver(_ProgressObserver);
				_ProgressObserver = null;
			}
		}

		#endregion

		#region Configuration

		private void configureBackgroundAudioTask()
		{
			_BackgroundIndentifier = UIApplication.SharedApplication.BeginBackgroundTask(() =>
			{
				UIApplication.SharedApplication.EndBackgroundTask(_BackgroundIndentifier);
				_BackgroundIndentifier = UIApplication.BackgroundTaskInvalid;
			});
		}

		private void configureAudioSession()
		{
			try
			{
				AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
				AVAudioSession.SharedInstance().SetActive(true);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void configureObservers()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.PlaybackStalledNotification, handleStall);
		}

		#endregion

		#region Notifications

		private void handleStall(NSNotification p_NSNotification)
		{
			_Player?.Pause();
			_Player?.Play();
		}

		private void playerItemDidPlayToEnd(NSNotification p_NSNotification)
		{
			if (PlayIndex >= QueuedItems.Count - 1)
			{
				Stop();
			}
			else
			{
				play(PlayIndex + 1);
			}
		}

		private void timeAction()
		{
			if (_Player?.CurrentItem != null)
			{
				CurrentItem?.Update();
			}

			if (CurrentItem?.CurrentTime != null)
			{
				DidChangePlaybackProgress?.Invoke(this);
			}
		}

		private void registerForPlayToEndNotification()
		{
			// Make sure this is working and the method is getting called
			_DidPlayToEndTimeNotification = NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, playerItemDidPlayToEnd);
		}

		private void unregisterForPlayToEndNotification()
		{
			if (_DidPlayToEndTimeNotification != null)
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(_DidPlayToEndTimeNotification);
			}
		}

		#endregion

		public void Play()
		{
			play(PlayIndex);
		}

		private void play(int p_Index)
		{
			if ((p_Index >= 0) && (p_Index < QueuedItems.Count))
			{
				configureBackgroundAudioTask();

				if ((QueuedItems[p_Index].PlayerItem != null) && (PlayIndex == p_Index))
				{
					resumePlayback();
				}
				else
				{
					if (CurrentItem?.PlayerItem != null)
					{
						unregisterForPlayToEndNotification();
					}

					PlayIndex = p_Index;

					if (QueuedItems[p_Index].PlayerItem?.Asset != null)
					{
						playerCurrentItem(QueuedItems[p_Index].PlayerItem.Asset);
					}
					else
					{
						loadPlaybackItem();
					}

					preloadNextAndPrevious(p_Index);
				}

				updateInfoCenter();
			}
		}

		public void Pause()
		{
			stopProgressTimer();

			_Player?.Pause();
			CurrentState = State.Paused;
		}

		public void Stop()
		{
			invalidatePlayback();
			CurrentState = State.Ready;

			UIApplication.SharedApplication.EndBackgroundTask(_BackgroundIndentifier);
			_BackgroundIndentifier = UIApplication.BackgroundTaskInvalid;
		}

		public void Replay()
		{
			if (IsPlayerOperational == true)
			{
				stopProgressTimer();
				Seek(0);
				play(0);
			}
		}

		public void PlayNext()
		{
			if (IsPlayerOperational == true)
			{
				play(PlayIndex + 1);
			}
		}

		public void PlayPrevious()
		{
			if (IsPlayerOperational == true)
			{
				play(PlayIndex - 1);
			}
		}

		public void ReplayCurrentItem()
		{
			if (IsPlayerOperational == true)
			{
				Seek(0, true);
			}
		}

		public void Seek(int p_ToSecond, bool p_ShouldPlay = false)
		{
			if ((_Player != null) && (CurrentItem != null))
			{
				_Player.Seek(new CMTime(p_ToSecond, 1));
				CurrentItem.Update();

				if (p_ShouldPlay == true)
				{
					_Player.Play();

					if (CurrentState != State.Playing)
					{
						CurrentState = State.Playing;
					}
				}

				DidChangePlaybackProgress?.Invoke(this);
			}
		}

		public void Append(AudioPlayerItem p_Item, bool p_LoadAsset)
		{
			QueuedItems.Add(p_Item);

			addJukeboxItemEvents(p_Item);

			if (p_LoadAsset == true)
			{
				p_Item.LoadPlayerItem();
			}
		}

		public void Remove(AudioPlayerItem p_Item)
		{
			throw new NotImplementedException();
		}

		private void addJukeboxItemEvents(AudioPlayerItem p_Item)
		{
			p_Item.DidUpdate += JukeboxItem_DidUpdate;
			p_Item.DidLoadPlayerItem += JukeboxItem_DidLoadPlayerItem;
			p_Item.DidFail += JukeboxItem_DidFail;
		}
	}
}
