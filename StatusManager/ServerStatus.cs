using System;
using Humanizer;
using NodaTime;

namespace SoapyBackend.StatusManager
{
    public class ServerStatusResponse
    {
        public ServerStatusResponse(ServerStatus s)
        {
            Request = s.Requests;
            StartTime = s.StartTime.ToDateTimeUtc();
            Started = StartTime.Humanize();
        }

        public long Request { get; }

        public DateTime StartTime { get; }

        public string Started { get; }
    }

    public class ServerStatus
    {
        public long Requests { get; set; }
        public Instant StartTime { get; set; }
    }
}