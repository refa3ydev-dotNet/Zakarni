using System.Threading.Tasks;

namespace Zakarni.Core.Interfaces;

public interface IAudioService
{
    Task PlayAdhanAsync();
    Task PlayNotificationSoundAsync();
    void StopAdhan();
    void SetVolume(double volume);
}
