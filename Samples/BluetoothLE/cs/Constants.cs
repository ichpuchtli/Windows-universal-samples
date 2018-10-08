using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace SDKTemplate
{
    // Define the characteristics and other properties of our custom service.
    public class Constants
    {
        // BT_Code: Initializes custom local parameters w/ properties, protection levels as well as common descriptors like User Description. 
        public static readonly GattLocalCharacteristicParameters gattOnboardingRequestParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Write |
                                       GattCharacteristicProperties.WriteWithoutResponse,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Onboarding Request Characteristic"
        };

        public static readonly GattLocalCharacteristicParameters gattOnboardingResultParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Onboarding Result Characteristic"
        };


        public static readonly GattLocalCharacteristicParameters gattProtocolVersionParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Protocol Version Characteristic"
        };

        public static readonly GattLocalCharacteristicParameters gattRossVersionParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "ROSS Version Characteristic"
        };

        public static readonly GattLocalCharacteristicParameters gattWifiListParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "WifiList Status Characteristic"
        };

        public static readonly Guid OnboardingServiceUuid = Guid.Parse("3B8D9532-ABD4-4F22-B1A5-717589EE84CB");

        public static readonly Guid ProtocolVersionCharacteristicUuid = Guid.Parse("951A87A5-5DE0-4A06-B846-871B0C0CCAEF");
        public static readonly Guid RossVersionCharacteristicUuid = Guid.Parse("0E531536-1FA2-4867-AD78-DFD4949731DF");
        public static readonly Guid WifiListRequestCharacteristicUuid = Guid.Parse("720BD6A4-5085-4CE7-AEE7-3644DBA6E5DC");
        public static readonly Guid OnboardingResultCharacteristicUuid = Guid.Parse("caec2ebc-e1d9-11e6-bf01-fe55135034f2");
        public static readonly Guid OnboardingRequestCharacteristicUuid = Guid.Parse("AC316D7E-8ADB-4D78-A7F8-DF628DB2CFFC");
    };
}
