using System;

using Foundation;
using AVFoundation;
using MediaPlayer;
using System.Collections.Generic;
using CoreMedia;
using UIKit;
using ObjCRuntime;
using CoreFoundation;

namespace Frans.Xamarin.iOS.AudioPlayer
{
	public class AudioPlayerItem : NSObject
	{
		public class Meta
		{
			public double? Duration
			{
				get;
				set;
			}

			public string Title
			{
				get;
				set;
			}

			public string Album
			{
				get;
				set;
			}

			public string Artist
			{
				get;
				set;
			}

			public UIImage Artwork
			{
				get;
				set;
			}

			internal void process(AVMetadataItem item)
			{
				switch (item.CommonKey)
				{
					case "title":
						Title = item.Value?.ToString();
						break;
					case "albumName":
						Album = item.Value?.ToString();
						break;
					case "artist":
						Artist = item.Value?.ToString();
						break;
					case "artwork":
						// todo handle art work
						//Album = item.Value?.ToString();
						break;
					default:
						break;
				}
			}
		}

		#region Properties

		public string Identifier { get; set; }
		public string LocalTitle { get; set; }
		public NSUrl URL { get; set; }

		public event Action<AudioPlayerItem> DidLoadPlayerItem;
		public event Action<AudioPlayerItem> DidUpdate;
		public event Action<AudioPlayerItem> DidFail;

		public AVPlayerItem PlayerItem { get; private set; }
		public double? CurrentTime { get; private set; }
		public Meta Metadata { get; set; }

		private bool _DidLoad = false;
		private NSTimer _Timer;
		private readonly NSString OBSERVED_VALUE = new NSString("timedMetadata");

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="T:JukeboxLib.JukeboxItem"/> class.
		/// </summary>
		/// <param name="p_URL">Local or remote URL of the audio file</param>
		/// <param name="p_LocalTitle">An optional itle for the file</param>
		public AudioPlayerItem(NSUrl p_URL, string p_LocalTitle = null)
		{
			this.URL = p_URL;
			this.Identifier = Guid.NewGuid().ToString();
			this.LocalTitle = p_LocalTitle;

			this.Metadata = new Meta();

			configureMetadata();
		}

		~AudioPlayerItem()
		{
			PlayerItem?.RemoveObserver(this, OBSERVED_VALUE);
		}

		#endregion

		public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
		{
			if (change["new"] == null)
			{
				DidFail?.Invoke(this);
				return;
			}

			if (keyPath == OBSERVED_VALUE)
			{
				// not sure about the last check, need to test it
				if ((PlayerItem != null) && (PlayerItem == ofObject))
				{
					if (PlayerItem.TimedMetadata == null)
						return;

					foreach (AVMetadataItem item in PlayerItem.TimedMetadata)
					{
						Metadata.process(item);
					}
				}

				scheduleNotification();
			}
		}

		public void LoadPlayerItem()
		{
			if (PlayerItem != null)
			{
				RefreshPlayerItem(PlayerItem.Asset);
				DidLoadPlayerItem?.Invoke(this);
				return;
			}
			else if (_DidLoad == true)
			{
				return;
			}
			else
			{
				_DidLoad = true;

				// TODO possible issue, setting loaded before the asset is actually loaded
			}

			loadAsync((asset) =>
			{
				if (validateAsset(asset) == true)
				{
					RefreshPlayerItem(asset);
					DidLoadPlayerItem?.Invoke(this);
				}
				else
				{
					_DidLoad = false;

					// todo possible issue not notifying when the asset fails to load
				}
			});
		}

		public void RefreshPlayerItem(AVAsset p_Asset)
		{
			PlayerItem?.RemoveObserver(this, OBSERVED_VALUE);

			PlayerItem = new AVPlayerItem(p_Asset);
			PlayerItem.AddObserver(this, OBSERVED_VALUE, NSKeyValueObservingOptions.New, IntPtr.Zero);
			Update();
		}

		public void Update()
		{
			if (PlayerItem != null)
			{
				this.Metadata.Duration = PlayerItem.Asset.Duration.Seconds;
				CurrentTime = PlayerItem.CurrentTime.Seconds;
			}
		}

		public override string ToString()
		{
			return string.Format("[JukeboxItem: Identifier={0}, LocalTitle={1}, URL={2}, PlayerItem={3}, CurrentTime={4}, Metadata={5}]", Identifier, LocalTitle, URL, PlayerItem, CurrentTime, Metadata);
		}

		private bool validateAsset(AVAsset p_Asset)
		{
			NSError error = null;

			p_Asset.StatusOfValue("duration", out error);

			if (error != null)
			{
				var message = "Asset validation failed";
				if (error.Code == -1022)
				{
					message += "\r\n Have you allowed HTTP connections in the info.plist file ?";
				}

				throw new Exception(message);
				//return false;
			}

			return true;
		}

		private void scheduleNotification()
		{
			_Timer?.Invalidate();
			_Timer = null;
			_Timer = NSTimer.CreateScheduledTimer(0.5, notifyDelegate);
		}

		private void notifyDelegate(NSTimer p_Timer)
		{
			_Timer?.Invalidate();
			_Timer = null;
			DidUpdate?.Invoke(this);
		}

		private void loadAsync(Action<AVAsset> p_CompletionBlock)
		{
			var asset = new AVUrlAsset(URL);

			asset.LoadValuesAsynchronously(new string[] { "duration" }, () =>
			{
				DispatchQueue.MainQueue.DispatchAsync(() =>
				{
					p_CompletionBlock(asset);
				});
			});
		}

		private void configureMetadata()
		{
			DispatchQueue.GetGlobalQueue(DispatchQueuePriority.Background).DispatchAsync(() =>
			{
				var metadataArray = new AVPlayerItem(URL).Asset.CommonMetadata;

				foreach (var item in metadataArray)
				{
					item.LoadValuesAsynchronously(new string[] { AVMetadata.KeySpaceCommon }, () =>
					{
						Metadata.process(item);

						DispatchQueue.MainQueue.DispatchAsync(() =>
						{
							scheduleNotification();
						});
					}); ;
				}
			});
		}
	}
}
