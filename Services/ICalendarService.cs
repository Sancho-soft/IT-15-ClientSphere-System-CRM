using System.Threading.Tasks;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface ICalendarService
    {
        Task<string> GetAuthorizationUrlAsync(string userId);
        Task<bool> SyncAppointmentAsync(Appointment appointment, string accessToken);
        Task<bool> DeleteAppointmentAsync(string graphEventId, string accessToken);
        Task<string> HandleCallbackAsync(string code, string userId);
    }
}
