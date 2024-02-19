using Opc.UaFx;
using Opc.UaFx.Client;
using Projekt.Properties;
using Projekt.VirtualDevice;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

class Program
{
    private static async Task Main(string[] args)
    {
        string cfgPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../config.json");
        Config cfg = JsonConvert.DeserializeObject<Config>(File.ReadAllText(cfgPath));

        using (var opcClient = new OpcClient(Resources.opcServerAddress))
        {
            try
            {
                opcClient.Connect();

                //Connect IoT Hub devices to Opc Client
                Dictionary<VirtualDevice, OpcDeviceData> iotHubDeviceToOpcDeviceData = new Dictionary<VirtualDevice, OpcDeviceData>();
                for (int i = 0; i < cfg.devices.Length; i++)
                {
                    DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(Resources.iotDeviceConnectionString, cfg.devices[i][1]);
                    await deviceClient.OpenAsync();
                    VirtualDevice vDevice = new VirtualDevice(deviceClient, opcClient);
                    OpcDeviceData deviceData = new OpcDeviceData(cfg.devices[i][0], cfg.devices[i][1]);

                    readOpcDeviceData(opcClient, deviceData);

                    Console.WriteLine("OPCUA Device \"{0}\" is connected to IoT device \"{1}\"", cfg.devices[i][0], cfg.devices[i][1]);

                    await vDevice.SetTwinDataAsync(deviceData.DeviceError, deviceData.ProductionRate, deviceData.LastMaitananceDate.Date, deviceData.LastErrorDate);
                    await vDevice.InitializeHandlers(cfg.devices[i][0]);

                    if (deviceData.DeviceError > 0) sendDeviceErrorReport(vDevice, deviceData);

                    iotHubDeviceToOpcDeviceData.Add(vDevice, deviceData);
                }

                while(true)
                {
                    foreach (var device in iotHubDeviceToOpcDeviceData)
                    {
                        int prevErrorCode = device.Value.DeviceError;
                        int prevProdRate = device.Value.ProductionRate;
                        readOpcDeviceData(opcClient, device.Value);
                        if (device.Value.ProductionStatus == 1)
                        {
                            await device.Key.SendMessage(device.Value.getTelemetryJSON());
                            if (device.Value.DeviceError > 0 && device.Value.DeviceError != prevErrorCode)
                            {
                                sendDeviceErrorReport(device.Key, device.Value);
                            }
                            if (device.Value.ProductionRate != prevProdRate)
                            {
                                await device.Key.UpdateTwinProductionRateAsync(device.Value.DeviceError, device.Value.ProductionRate);
                            }
                        }
                    }
                    Console.WriteLine();
                    await Task.Delay(5000);
                }
                opcClient.Disconnect();
            }
            catch (OpcException opcex)
            {
                Console.WriteLine(opcex.Message);
            }
        }
    }

    private async static void sendDeviceErrorReport(VirtualDevice virtualDevice, OpcDeviceData deviceData)
    {
        Console.WriteLine("Error from device {0} was reported", deviceData.IoTDeviceId);
        await virtualDevice.SendMessage(deviceData.getErrorsJSON());
        await virtualDevice.UpdateTwinErrorDataAsync(deviceData.DeviceError);
    }

    private static void readOpcDeviceData(OpcClient opcClient, OpcDeviceData deviceData)
    {
        deviceData.ProductionStatus = (int)opcClient.ReadNode(deviceData.nodeId + "/ProductionStatus").Value;
        deviceData.WorkorderId = (string)opcClient.ReadNode(deviceData.nodeId + "/WorkorderId").Value;
        deviceData.ProductionRate = (int)opcClient.ReadNode(deviceData.nodeId + "/ProductionRate").Value;
        deviceData.GoodCount = (long)opcClient.ReadNode(deviceData.nodeId + "/GoodCount").Value;
        deviceData.BadCount = (long)opcClient.ReadNode(deviceData.nodeId + "/BadCount").Value;
        deviceData.Temperature = (double)opcClient.ReadNode(deviceData.nodeId + "/Temperature").Value;
        deviceData.DeviceError = (int)opcClient.ReadNode(deviceData.nodeId + "/DeviceError").Value;
    }

}

