using System.Threading.Tasks;
using Zakarni.Core.Models;

namespace Zakarni.Core.Interfaces;

public interface ILocationService
{
    Task<LocationInfo> GetCurrentLocationAsync();
}
