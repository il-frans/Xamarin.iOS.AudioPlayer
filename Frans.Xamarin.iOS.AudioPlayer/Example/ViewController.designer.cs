// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace Example
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel CurrentTrackTimeLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIActivityIndicatorView LoadingActivityIndicator { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton NextButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISlider PlaybackProgressSlider { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton PlayPauseButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton PreviousButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ReplayButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton StopButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel TitleLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel TrackDurationLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISlider VolumeSlider { get; set; }

        [Action ("NextButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void NextButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("PlaybackProgressSlider_ValueChanged:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void PlaybackProgressSlider_ValueChanged (UIKit.UISlider sender);

        [Action ("PlayPauseButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void PlayPauseButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("PreviousButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void PreviousButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("ReplayButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ReplayButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("StopButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void StopButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("VolumeSlider_ValueChanged:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void VolumeSlider_ValueChanged (UIKit.UISlider sender);

        void ReleaseDesignerOutlets ()
        {
            if (CurrentTrackTimeLabel != null) {
                CurrentTrackTimeLabel.Dispose ();
                CurrentTrackTimeLabel = null;
            }

            if (LoadingActivityIndicator != null) {
                LoadingActivityIndicator.Dispose ();
                LoadingActivityIndicator = null;
            }

            if (NextButton != null) {
                NextButton.Dispose ();
                NextButton = null;
            }

            if (PlaybackProgressSlider != null) {
                PlaybackProgressSlider.Dispose ();
                PlaybackProgressSlider = null;
            }

            if (PlayPauseButton != null) {
                PlayPauseButton.Dispose ();
                PlayPauseButton = null;
            }

            if (PreviousButton != null) {
                PreviousButton.Dispose ();
                PreviousButton = null;
            }

            if (ReplayButton != null) {
                ReplayButton.Dispose ();
                ReplayButton = null;
            }

            if (StopButton != null) {
                StopButton.Dispose ();
                StopButton = null;
            }

            if (TitleLabel != null) {
                TitleLabel.Dispose ();
                TitleLabel = null;
            }

            if (TrackDurationLabel != null) {
                TrackDurationLabel.Dispose ();
                TrackDurationLabel = null;
            }

            if (VolumeSlider != null) {
                VolumeSlider.Dispose ();
                VolumeSlider = null;
            }
        }
    }
}