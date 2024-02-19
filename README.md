# Azure IoT Case Study project

## Project depencencies

### Project.sln:
```
Microsoft.Azure.Devices
Microsoft.Azure.Devices.Client
Projekt.VirtualDevice
```

### Projekt.FunctionApps.sln:
```
Microsoft.Azure.Devices
Microsoft.Azure.WebJobs.Extensions.ServiceBus
Microsoft.NET.Sdk.Functions
```

## Configuration

Set `iotDeviceConnectionString`,
`iotHubOwnerConnectionString`,
`opcServerAddress` (default is `opc.tcp://localhost:4840/`)
in your `Projekt/Properties/Resources.resx`

Set `IoTHubConnectiontring`
in `Projekt.FunctionsApps/Properties/Resources.resx`

In `Projekt.FunctionsApps/local.settings.json`
set `AzureWebJobsStorage` and `ServiceBusConnectionString`

Set the list of your devices in `Project/config.json`.
It should look like this
```json
{
    "devices": [
        ["local_device_name_1", "azure_device_name_1"],
        ["local_device_name_2", "azure_device_name_2"]
        /* ... */
    ]
}
```

## Project startup

Open and run `Projekt/Projekt.sln` for reading and sending data, and `Projekt.FunctionApps/Projekt.FunctionApps.sln` to call function on triggers.

Data are read from devices and sent to Azure as D2C messages every 5 seconds.

```
Sending data to IoT Hub...
05.01.2024 10:40:41> D2C Sending message: {"opc_device_id":"ns=2;s=Device 1" "iot_device_id":"Device_1", "production_status": 1, "workorder_id":"8985f4b3-9 31e-4ad7-9dac-bff281e3be03", "good_count": 0, "bad_count":0,"temperature": 25.04
2404819579332}
```

If an error occured:

```
Error from device Device_1 was reported
Sending data to IoT Hub...
05.01.2024 10:45:32> D2C Sending message: {"opc_device_id":"ns=2; s=Device 1" "iot_device_id":"Device_1","is_error": true, "error_unknown": false, "error_sen
sor": false, "error_power":true,"error_emergency_stop":false}
05.01.2024 10:45:32> Device Twin value was updated.
```

## Device twin 

Example of data sent to device twin

```json
{
    "deviceId": "Device_1",
    "etag": "AAAAAAAAAAE=",
    "deviceEtag": "Njg5MTkwMzkx",
    "status": "enabled",
    "statusUpdateTime": "0001-01-01T00:00:00Z",
    "connectionState": "Disconnected",
    "lastActivityTime": "2023-01-05T06:49:44.9351578Z",
    "cloudToDeviceMessageCount": 0,
    "authenticationType": "sas",
    "x509Thumbprint": {
    "primaryThumbprint": null,
    "secondaryThumbprint": null
    },
    "modelId": "",
    "version": 61,
    "properties": {
        "desired": {
            "$metadata": {
                "$lastUpdated": "2022-12-21T17:37:58.6058459Z"
            },
            "$version": 1
        },
        "reported": {
            "device_errors": 0,
            "production_rate": 60,
            "last_maintenance_date": "0001-01-01T00:00:00",
            "last_error_date": "0001-01-01T00:00:00",
            "$metadata": {
                "$lastUpdated": "2023-01-05T06:49:20.404008Z",
                "device_errors": {
                    "$lastUpdated": "2023-01-05T06:49:20.404008Z"
                },
                "production_rate": {
                    "$lastUpdated": "2023-01-05T06:49:20.404008Z"
                },
                "last_maintenance_date": {
                    "$lastUpdated": "2023-01-05T06:49:20.404008Z"
                },
                    "last_error_date": {
                        "$lastUpdated": "2023-01-05T06:49:20.404008Z"
                }
            },
        }
    }
}
```

## Direct methods

EmergencyStop

```cs
async Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object userContext)
```

ResetErrorStatus

```cs
async Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodRequest, object userContext)
```

DecreaseProductRate

```cs
async Task<MethodResponse> DecreaseProductRateHandler(MethodRequest methodRequest, object userContext)
```

`userContext` is the id of the node

## Analytics

```sql
-- Count KPI every minute and trigger rate decrease if less than 90
SELECT
System.Timestamp() time,
iot_device_id AS deviceId
INTO [device-decrease-rate]
FROM [zajecia-iot-ul-322]
GROUP BY iot_device_id, TumblingWindow(minute, 1)
HAVING (MAX(good_count)*100)/(MAX(good_count)+MAX(bad_count)) < 90

-- Count device errors every minute and trigger emergency stop if more than 3
SELECT
System.Timestamp() time,
iot_device_id AS deviceId,
SUM(COALESCE(error_emergency_stop,0)) + SUM(COALESCE(error_power,0)) + SUM
(COALESCE(error_sensor,0)) + SUM(COALESCE(error_unknown,0)) AS errorSum
INTO [device-emergency-stop]
FROM [zajecia-iot-ul-322]
GROUP BY iot_device_id, TumblingWindow(minute, 1)
HAVING errorSum > 3

-- Device production KPI every 5 minutes
SELECT
System.Timestamp() time,
iot_device_id,
(MAX(good_count)*100)/(MAX(good_count)+MAX(bad_count)) as production_kpi
INTO [productionkpi]
FROM [zajecia-iot-ul-322]
GROUP BY iot_device_id, TumblingWindow(minute, 5)

-- Device's temperature every 5 minutes
SELECT
System.Timestamp() log_time,
iot_device_id,
MAX(temperature) as temperature_max,
MIN(temperature) as temperature_min,
AVG(temperature) as temperature_avg
INTO [devicetemperature]
FROM [zajecia-iot-ul-322]
GROUP BY iot_device_id, TumblingWindow(minute, 5)

-- Errors per machine every 1 minutes
SELECT
System.Timestamp() time,
iot_device_id,
SUM(error_unknown) AS error_unknown_count,
SUM(error_sensor) AS error_sensor_count,
SUM(error_power) AS error_power_count,
SUM(error_emergency_stop) AS error_emergency_stop_count
INTO [deviceerrors]
FROM [zajecia-iot-ul-322]
GROUP BY iot_device_id, TumblingWindow(minute, 1)
```