using System;

namespace SoapyBackend.Data
{
    public class TriggerData
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        // FK
        public int DeviceId { get; set; }
        public DeviceData Device { get; set; }
    }
}