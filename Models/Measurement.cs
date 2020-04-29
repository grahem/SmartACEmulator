namespace SmartACDeviceAPI.Models
{
    public class Measurement
    {

        public string Id { get; set; }

        public string DeviceSerialNumber { get; set; }

        public string RecordedTime { get; set; }

        public double AirHumidity { get; set; }

        public double CarbonMonoxide { get; set; }

        public double Temperature { get; set; }
    }
}
