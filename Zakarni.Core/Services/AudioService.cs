using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Zakarni.Core.Interfaces;

namespace Zakarni.Core.Services;

public class AudioService : IAudioService
{
    private readonly MediaPlayer _mediaPlayer;

    public AudioService()
    {
        _mediaPlayer = new MediaPlayer();
    }

    public Task PlayAdhanAsync()
    {
        // Prevent overlapping audio if already playing
        if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            return Task.CompletedTask;
        }

        try
        {
            // Resolve the absolute path to the deployed asset
            string filePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "allah akbar.mp3");
            if (System.IO.File.Exists(filePath))
            {
                var uri = new Uri(filePath, UriKind.Absolute);
                _mediaPlayer.Source = MediaSource.CreateFromUri(uri);
                _mediaPlayer.Play();
            }
        }
        catch { /* Silently fallback if audio hardware is unavailable */ }
        
        return Task.CompletedTask;
    }

    public Task PlayNotificationSoundAsync()
    {
        // Using a common Windows notification sound URI
        var uri = new Uri("ms-winsoundevent:Notification.Default");
        _mediaPlayer.Source = MediaSource.CreateFromUri(uri);
        _mediaPlayer.Play();
        return Task.CompletedTask;
    }

    public void StopAdhan() { _mediaPlayer.Pause(); }

    public void SetVolume(double volume)
    {
        _mediaPlayer.Volume = Math.Clamp(volume, 0.0, 1.0);
    }
}
