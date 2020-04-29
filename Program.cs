using Newtonsoft.Json;
using SmartACClient.Models;
using SmartACDeviceAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SmartACClient
{
    /// <summary>
    /// This Console App emulates an AC Unit in the field.
    /// It starts by registering itself with the Smart AC Device API
    /// It then records 500 sensor measurements on a 10 second interval.
    /// Both the Device and Measurements are mocked, so it is a minimally interactive app.
    /// If the service is down, it backs up it's measurements to a loacl file.
    /// Once the serice comes back online, it publishes the backed up measurements and removes the backup file.
    /// 
    /// Not For Production Use
    /// </summary>
    class Program
    {
        private static Random random = new Random();
        private static readonly HttpClient client = new HttpClient();

        //TODO: switch these urls to the dev environment once it's available.
        private const string deviceURI = "http://smartacdeviceapi-dev.us-west-1.elasticbeanstalk.com/devices";
        private const string measurementsURI = "http://smartacdeviceapi-dev.us-west-1.elasticbeanstalk.com/devices/{0}/measurements";
        private const string authURI = "http://smartacdeviceapi-dev.us-west-1.elasticbeanstalk.com/authenticate";
        private const string backupFile = @"C:\temp\measurements";

        static async Task Main(string[] args)
        {
            //In case we need to delete all measurements, call this healper method first.
            //DDB.WipeMeasurements();

            //Register a single device and prepare for emulating measurements
            await RegisterDevice();

            while (true)
            {
                Thread.Sleep(5000);
            }
        }

        private static async Task RegisterDevice()
        {
            //create a mock device
            var device = MockDevice();

            //build it
            var byteContent = BuildContent(device);

            //post it - fail is service is down
            try
            {
                var result = await client.PostAsync(deviceURI, byteContent);
                var contents = await result.Content.ReadAsStringAsync();
                var deviceResponse = JsonConvert.DeserializeObject<Device>(contents);

                //display it            
                Console.Write(JsonConvert.SerializeObject(deviceResponse, Formatting.Indented));

                Console.WriteLine("Begin Metering? (y/n)");
                var clientResponse = Console.ReadLine();
                if (clientResponse.ToLower() == "y")
                {
                    new Timer(
                    RunDevice,
                    deviceResponse,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10)
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Service offline", ex.Message);
            }
        }

        private static void RunDevice(object state)
        {
            Device device = (Device)state;
            //get auth token
            string authToken = GetAuthorizationToken(device).Result;

            //build the measurement
            var measurements = new List<Measurement>();

            //Pickup any failed measurements
            {
                var failedMeasurements = HandleFaileMeasurements();
                measurements.AddRange(failedMeasurements);
            }

            for (int i = 0; i < 500; i++)
            {
                var measurement = MockMeasurement(device.SerialNumber);
                measurements.Add(measurement);
            }

            var byteContent = BuildContent(measurements);

            //post the measurment
            try
            {
                //client.DefaultRequestHeaders.Add("Authorization", string.Format("bearer {0}", authToken));
                var result = client.PostAsync(string.Format(measurementsURI, device.SerialNumber), byteContent);
                client.DefaultRequestHeaders.Clear();
                result.Wait();

                if (result.Result.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    //convert object to json string.
                    string json = JsonConvert.SerializeObject(measurements);

                    //export data to json file. 
                    using (TextWriter tw = new StreamWriter(backupFile))
                    {
                        tw.WriteLine(json);
                    };
                    Console.WriteLine("Service Down. Backed Up Measurements");
                }

                Console.WriteLine(string.Format("Succesful: {0}", result.Result.IsSuccessStatusCode));

            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("An error occured: , {0}", ex.Message));
            }
        }

        private static List<Measurement> HandleFaileMeasurements()
        {
            List<Measurement> values = new List<Measurement>();
            if (File.Exists(@"C:\temp\measurements"))
            {
                //Failed measurements are stored in a JSON file. Deserialize the measurements and delete the file when done.
                //If another failure occures, all measurements will be flushed backed to a new file.
                using (StreamReader reader = new StreamReader(backupFile))
                {
                    
                    while (!reader.EndOfStream)
                    {
                        var measurements = reader.ReadLine();
                        var failedMeasurements = JsonConvert.DeserializeObject<List<Measurement>>(measurements);
                        values.AddRange(failedMeasurements);
                    }
                }

                Console.WriteLine("Cleanup Measurement Backup");
                File.Delete(backupFile);
            }
            return values;
        }

        private static async Task<string> GetAuthorizationToken(Device device)
        {
            //Using the devices secret, Call the AUth service and return the JWT token.
            AuthorizationModel auth = new AuthorizationModel();
            auth.SerialNumber = device.SerialNumber;
            auth.Secret = device.Secret;

            var byteContent = BuildContent(auth);

            var result = await client.PostAsync(authURI, byteContent);
            var authToken = await result.Content.ReadAsStringAsync();
            return authToken;
        }

        private static ByteArrayContent BuildContent(object obj)
        {
            //Helper method to build request conetent
            var myContent = JsonConvert.SerializeObject(obj);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return byteContent;
        }

        private static Device MockDevice()
        {
            var device = new Device();
            device.SerialNumber = GenerateSerialNumber();
            device.Status = "healthy";
            device.RegistrationDate = DateTime.UtcNow.ToString("s");
            device.FirmwareVersion = "1.0.4";
            device.InAlarm = false;
            device.Secret = Reverse(device.SerialNumber);
            return device;
        }

        private static Measurement MockMeasurement(string serialNumber)
        {
            Measurement m = new Measurement();
            m.AirHumidity = random.Next(30, 60);
            m.CarbonMonoxide = random.Next(50, 100);
            m.DeviceSerialNumber = serialNumber;
            m.Id = Guid.NewGuid().ToString();
            m.RecordedTime = DateTime.UtcNow.ToString("s");
            m.Temperature = m.CarbonMonoxide = random.Next(10, 50);
            return m;
        }

        public static string GenerateSerialNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 16)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
