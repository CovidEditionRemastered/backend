using System.Threading.Tasks;
using NodaTime;

namespace SoapyBackend.StatusManager
{
    public interface IServerStatusService
    {
        Task<ServerStatus> Get();

        void Bump();
    }

    public class ServerStatusService : IServerStatusService
    {
        private readonly ServerStatus _status = new ServerStatus
            {Requests = 0, StartTime = SystemClock.Instance.GetCurrentInstant()};

        public Task<ServerStatus> Get()
        {
            return Task.FromResult(this._status);
        }

        public void Bump()
        {
            _status.Requests++;
        }
    }
}