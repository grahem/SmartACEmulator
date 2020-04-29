# Smart AC Emulator

This Console App represents a simple Smart AC Device emulator.

It has the following features

1. Generate a mock device with a randomzied SerialNumber
2. Generate mock measurements with randomized values
3. Register the device with the Smart AC Device API
4. Emit 500 measurements every 10 seconds.
5. Adss support for service outage detection

## This emulator should NOT be used in a production environment

| Usage
1. clone this repository
2. Open the project in Visual Studio Code
3. Hit F-5 to debug the console
4. The console will register a device and ask if you wish to continue. Type y and hit enter.
5. Monitor the console

| Notes
During a service outage (service is in maintenance mode), this emulator responds by crteating a temporary local file and stores a 
JSON representation of the measurements that failed to send to the API.
Once the service is online, the file contents are read and flushed to the API. The file is then deleted.

To emulate a service outage, you must have access to the DynamoDB table **SystemConfig**. 
There you will find an entry for "InMaintenance". Update the value to true to monitoring the apps behavior.


