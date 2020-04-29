using System;
using System.Collections.Generic;
using System.Text;

namespace SmartACClient.Models
{
    class Device
    {
     
        public string SerialNumber { get; set; }

        public string Status { get; set; }
        
        public string RegistrationDate { get; set; }
        
        public string FirmwareVersion { get; set; }
        
        public string Secret { get; set; }
       
        public bool InAlarm { get; set; }
    }
}
