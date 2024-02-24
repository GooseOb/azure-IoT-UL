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
in your `Projekt/Properties/Resources.resx`.

Set `IoTHubConnectiontring`
in `Projekt.FunctionsApps/Properties/Resources.resx`.

In `Projekt.FunctionsApps/local.settings.json`
set `AzureWebJobsStorage` and `ServiceBusConnectionString`.

Set the list of your devices in `Project/config.json`.
It should look like this

```json
{
    "devices": [
        ["local_device_name_1", "azure_device_name_1"],
        ["local_device_name_2", "azure_device_name_2"]
    ]
}
```

## Project startup

Open and run `Projekt/Projekt.sln` for reading and sending data, and `Projekt.FunctionApps/Projekt.FunctionApps.sln` to call function on triggers.

## D2C messages

Data are read from devices and sent to Azure as D2C messages every 5 seconds.

Example of the D2C message

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

Example of data stored in device twin

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

In order to execute direct methods
you can use Azure IoT Explorer.

Available methods:

- EmergencyStop

- ResetErrorStatus

- DecreaseProductRate

No payload needed.

If the execution is successful, the response is
```json
{
    "status":0,
    "payload":null
}
```

## Business logic

Features implemented with Stream Analytics job,
Service Bus queue and Function Apps:

- Counting KPI every minute
    - Triggering rate decrease if less than 90
- Counting device errors every minute 
    - Triggering emergency stop if more than 3

Features implemented with Stream Analytics job and Blob storage:

- Recording Device production KPI every 5 minutes
- Recording Device's temperature every 5 minutes
- Recording number of errors per machine every minute
