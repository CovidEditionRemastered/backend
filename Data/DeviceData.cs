using System;
using System.Collections.Generic;

namespace SoapyBackend.Data
{
    public class DeviceData
    {
        public int Id { get; set; }

        public Guid DeviceId { get; set; }

        public string Password { get; set; }

        public bool PowerState { get; set; }
        
        public IEnumerable<ProgramData> Programs { get; set; }
        public IEnumerable<TriggerData> Triggers { get; set; }
    }
}