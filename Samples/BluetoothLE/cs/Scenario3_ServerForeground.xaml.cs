//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SDKTemplate
{
    // This scenario declares support for a calculator service. 
    // Remote clients (including this sample on another machine) can supply:
    // - Operands 1 and 2
    // - an operator (+,-,*,/)
    // and get a result
    public sealed partial class Scenario3_ServerForeground : Page
    {
        private MainPage rootPage = MainPage.Current;

        GattServiceProvider serviceProvider;

        private GattLocalCharacteristic op1Characteristic;
        private int operand1Received = 0;

        private GattLocalCharacteristic op2Characteristic;
        private int operand2Received = 0;

        private GattLocalCharacteristic operatorCharacteristic;
        CalculatorOperators operatorReceived = 0;
        private GattLocalCharacteristic resultCharacteristic;
        private int resultVal = 0;

        private bool peripheralSupported;
        private WiFiAdapter WiFiAdapter;

        private enum CalculatorCharacteristics
        {
            Operand1 = 1,
            Operand2 = 2,
            Operator = 3
        }

        private enum CalculatorOperators
        {
            Add = 1,
            Subtract = 2,
            Multiply = 3,
            Divide = 4
        }

        public Scenario3_ServerForeground()
        {
            InitializeComponent();
            InitializeFirstAdapter();
        }

        private async Task InitializeFirstAdapter()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                throw new Exception("WiFiAccessStatus not allowed");
            }
            else
            {
                var wifiAdapterResults =
                  await DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                if (wifiAdapterResults.Count >= 1)
                {
                    this.WiFiAdapter =
                      await WiFiAdapter.FromIdAsync(wifiAdapterResults[0].Id);
                }
                else
                {
                    throw new Exception("WiFi Adapter not found.");
                }
            }

            if (this.WiFiAdapter != null)
            {
                await this.WiFiAdapter.ScanAsync();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            peripheralSupported = await CheckPeripheralRoleSupportAsync();
            if (peripheralSupported)
            {
                ServerPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PeripheralWarning.Visibility = Visibility.Visible;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (serviceProvider != null)
            {
                if (serviceProvider.AdvertisementStatus != GattServiceProviderAdvertisementStatus.Stopped)
                {
                    serviceProvider.StopAdvertising();
                }
                serviceProvider = null;
            }
        }

        private async void PublishButton_ClickAsync()
        {
            // Server not initialized yet - initialize it and start publishing
            if (serviceProvider == null)
            {
                var serviceStarted = await ServiceProviderInitAsync();
                if (serviceStarted)
                {
                    rootPage.NotifyUser("Service successfully started", NotifyType.StatusMessage);
                    PublishButton.Content = "Stop Service";
                }
                else
                {
                    rootPage.NotifyUser("Service not started", NotifyType.ErrorMessage);
                }
            }
            else
            {
                // BT_Code: Stops advertising support for custom GATT Service 
                serviceProvider.StopAdvertising();
                serviceProvider = null;
                PublishButton.Content = "Start Service";
            }
        }

        private async Task<bool> CheckPeripheralRoleSupportAsync()
        {
            // BT_Code: New for Creator's Update - Bluetooth adapter has properties of the local BT radio.
            var localAdapter = await BluetoothAdapter.GetDefaultAsync();

            if (localAdapter != null)
            {
                return localAdapter.IsPeripheralRoleSupported;
            }
            else
            {
                // Bluetooth is not turned on 
                return false;
            }
        }

        /// <summary>
        /// Uses the relevant Service/Characteristic UUIDs to initialize, hook up event handlers and start a service on the local system.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ServiceProviderInitAsync()
        {
            // BT_Code: Initialize and starting a custom GATT Service using GattServiceProvider.
            GattServiceProviderResult serviceResult = await GattServiceProvider.CreateAsync(Constants.OnboardingServiceUuid);
            if (serviceResult.Error == BluetoothError.Success)
            {
                serviceProvider = serviceResult.ServiceProvider;
            }
            else
            {
                rootPage.NotifyUser($"Could not create service provider: {serviceResult.Error}", NotifyType.ErrorMessage);
                return false;
            }

            GattLocalCharacteristicResult result = await serviceProvider.Service.CreateCharacteristicAsync(
                Constants.ProtocolVersionCharacteristicUuid, Constants.gattProtocolVersionParameters);
            if (result.Error == BluetoothError.Success)
            {
                result.Characteristic.ReadRequested += ProtocolVersionCharacteristic_ReadRequestedAsync;
            }
            else
            {
                rootPage.NotifyUser($"Could not create operand1 characteristic: {result.Error}", NotifyType.ErrorMessage);
                return false;
            }

            result = await serviceProvider.Service.CreateCharacteristicAsync(
                Constants.RossVersionCharacteristicUuid, Constants.gattRossVersionParameters);

            if (result.Error == BluetoothError.Success)
            {
                result.Characteristic.ReadRequested += RossCharacteristic_ReadRequestedAsync;
            }
            else
            {
                rootPage.NotifyUser($"Could not create operand2 characteristic: {result.Error}", NotifyType.ErrorMessage);
                return false;
            }

            result = await serviceProvider.Service.CreateCharacteristicAsync(
                Constants.WifiListRequestCharacteristicUuid, Constants.gattWifiListParameters);
            if (result.Error == BluetoothError.Success)
            {
                result.Characteristic.ReadRequested += WifiListCharacteristic_ReadRequestedAsync;
            }
            else
            {
                rootPage.NotifyUser($"Could not create operator characteristic: {result.Error}", NotifyType.ErrorMessage);
                return false;
            }

     //       // Add presentation format - 32-bit unsigned integer, with exponent 0, the unit is unitless, with no company description
     //       GattPresentationFormat intFormat = GattPresentationFormat.FromParts(
     //           GattPresentationFormatTypes.UInt32,
     //           PresentationFormats.Exponent,
     //           Convert.ToUInt16(PresentationFormats.Units.Unitless),
     //           Convert.ToByte(PresentationFormats.NamespaceId.BluetoothSigAssignedNumber),
     //           PresentationFormats.Description);

     //       Constants.gattWifiListParameters.PresentationFormats.Add(intFormat);

            result = await serviceProvider.Service.CreateCharacteristicAsync(
                Constants.OnboardingResultCharacteristicUuid, Constants.gattOnboardingResultParameters);

            if (result.Error == BluetoothError.Success)
            {
                resultCharacteristic = result.Characteristic;
                resultCharacteristic.ReadRequested += OnboardingStatusCharacteristic_ReadRequestedAsync;
            }
            else
            {
                rootPage.NotifyUser($"Could not create result characteristic: {result.Error}", NotifyType.ErrorMessage);
                return false;
            }

            result = await serviceProvider.Service.CreateCharacteristicAsync(
                Constants.OnboardingRequestCharacteristicUuid, Constants.gattOnboardingRequestParameters);

            if (result.Error == BluetoothError.Success)
            {
                result.Characteristic.WriteRequested += OnboardingRequestCharacteristic_WriteRequestedAsync;
            }
            else
            {
                rootPage.NotifyUser($"Could not create result characteristic: {result.Error}", NotifyType.ErrorMessage);
                return false;
            }

            // BT_Code: Indicate if your sever advertises as connectable and discoverable.
            GattServiceProviderAdvertisingParameters advParameters = new GattServiceProviderAdvertisingParameters
            {
                // IsConnectable determines whether a call to publish will attempt to start advertising and 
                // put the service UUID in the ADV packet (best effort)
                IsConnectable = peripheralSupported,
                

                // IsDiscoverable determines whether a remote device can query the local device for support 
                // of this service
                IsDiscoverable = true
            };
            serviceProvider.AdvertisementStatusChanged += ServiceProvider_AdvertisementStatusChanged;
            serviceProvider.StartAdvertising(advParameters);
            return true;
        }

        private async void OnboardingRequestCharacteristic_WriteRequestedAsync(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                var request = await args.GetRequestAsync();

                var reader = DataReader.FromBuffer(request.Value);

                //var bytes = new byte[4098];

                var result = reader.ReadString(reader.UnconsumedBufferLength);

                //var result = Encoding.UTF8.GetString(bytes);

                rootPage.NotifyUser($"Onboarding Request : {result}", NotifyType.StatusMessage);

                // Complete the request if needed
                if (request.Option == GattWriteOption.WriteWithResponse)
                {
                    request.Respond();
                }
            }
        }

        private void OnboardingStatusCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
        {
            rootPage.NotifyUser($"New device subscribed. New subscribed count: {sender.SubscribedClients.Count}", NotifyType.StatusMessage);
        }

        private void ServiceProvider_AdvertisementStatusChanged(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args)
        {
            // Created - The default state of the advertisement, before the service is published for the first time.
            // Stopped - Indicates that the application has canceled the service publication and its advertisement.
            // Started - Indicates that the system was successfully able to issue the advertisement request.
            // Aborted - Indicates that the system was unable to submit the advertisement request, or it was canceled due to resource contention.

            rootPage.NotifyUser($"New Advertisement Status: {sender.AdvertisementStatus}", NotifyType.StatusMessage);
        }
        private async void OnboardingStatusCharacteristic_ReadRequestedAsync(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            // BT_Code: Process a read request. 
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request. 
                GattReadRequest request = await args.GetRequestAsync();
                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    rootPage.NotifyUser("Access to device not allowed", NotifyType.ErrorMessage);
                    return;
                }

                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteInt32(400);

                // Can get details about the request such as the size and offset, as well as monitor the state to see if it has been completed/cancelled externally.
                // request.Offset
                // request.Length
                // request.State
                // request.StateChanged += <Handler>

                // Gatt code to handle the response
                request.RespondWithValue(writer.DetachBuffer());
            }
        }

        private async void ProtocolVersionCharacteristic_ReadRequestedAsync(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request.
                GattReadRequest request = await args.GetRequestAsync();

                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    return;
                }

                var writer = new DataWriter();

                writer.WriteString("0");

                // Gatt code to handle the response
                request.RespondWithValue(writer.DetachBuffer());
            }
        }

        private async void RossCharacteristic_ReadRequestedAsync(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request.
                GattReadRequest request = await args.GetRequestAsync();

                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    return;
                }

                var writer = new DataWriter();

                writer.WriteString("1.5.14.0");

                // Gatt code to handle the response
                request.RespondWithValue(writer.DetachBuffer());
            }
        }

        private async void WifiListCharacteristic_ReadRequestedAsync(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                // Get the request information.  This requires device access before an app can access the device's request.
                GattReadRequest request = await args.GetRequestAsync();

                if (request == null)
                {
                    // No access allowed to the device.  Application should indicate this to the user.
                    return;
                }

                var writer = new DataWriter();

                var result = JsonConvert.SerializeObject(
                    new WIfiNetworkPayload
                    {
                        AvailableAdapters =
                        this.WiFiAdapter.NetworkReport.AvailableNetworks.Select(x => new WifiNetwork { Ssid = x.Ssid })
                    });

                writer.WriteString(result);
                //var wifiAdapters = 
                //writer.WriteString("{ \"AvailableAdapters\": [{ \"Ssid\": \"ilab\" }, { \"Ssid\": \"uqconnect\" }] }");

                // Gatt code to handle the response
                request.RespondWithValue(writer.DetachBuffer());
            }
        }

        public class WIfiNetworkPayload
        {
            public IEnumerable<WifiNetwork> AvailableAdapters { get; set; }
        }
    }
}
