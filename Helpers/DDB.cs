using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartACClient.Helpers
{
    public class DDB { 
    
        public static void WipeMeasurements()
        {
            
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            DynamoDBContext context = new DynamoDBContext(client);
            ScanRequest scanRequest = new ScanRequest("Measurements");

            var scan = client.ScanAsync(scanRequest);
            foreach (Dictionary<string, AttributeValue> item in scan.Result.Items)
            {
                string id = "";
                string range = "";
                foreach (var keyValuePair in item)
                {
                    if (keyValuePair.Key == "Id")
                    {
                        id = keyValuePair.Value.S;
                    } else if (keyValuePair.Key == "DeviceSerialNumber")
                    {
                        range = keyValuePair.Value.S;
                    }

                }
                context.DeleteAsync<DDBMeasurement>(id, range);
            }
        }
    }
}
