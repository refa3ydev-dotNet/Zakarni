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
        // Stream directly without blocking UI
        var uri = new Uri("https://download.quranicaudio.com/adhan/makkah.mp3");
        _mediaPlayer.Source = MediaSource.CreateFromUri(uri);
        _mediaPlayer.Play();
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
