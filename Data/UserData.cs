namespace SoapyBackend.Data
{
    public class UserData
    {
        public int Id { get; set; }

        public string Aud { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        // FK
        public int DeviceId { get; set; }
        public DeviceData Device { get; set; }
    }
}