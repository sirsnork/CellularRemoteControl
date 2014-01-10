using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace CellularRemoteControl
{
    class Relay
    {
        // Supports Seeedstudio Relay Shield V2
        // http://www.seeedstudio.com/depot/relay-shield-v20-p-1376.html
        private static OutputPort relay1 = new OutputPort(Pins.GPIO_PIN_D4, false);
        private static OutputPort relay2 = new OutputPort(Pins.GPIO_PIN_D5, false);
        private static OutputPort relay3 = new OutputPort(Pins.GPIO_PIN_D6, false);
        private static OutputPort relay4 = new OutputPort(Pins.GPIO_PIN_D7, false);

        public Relay()
        {
        }
        public static Boolean On(int Switch)
        {
            switch (Switch)
            {
                case 1:
                    relay1.Write(true);
                    if (relay1.Read())
                    {
                        Debug.Print("Switch 1 On.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning on Switch 1.");
                        return false;
                    }
                case 2:
                    relay2.Write(true);
                    if (relay2.Read())
                    {
                        Debug.Print("Switch 2 On.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning on Switch 2.");
                        return false;
                    }
                case 3:
                    relay3.Write(true);
                    if (relay3.Read())
                    {
                        Debug.Print("Switch 3 On.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning on Switch 3.");
                        return false;
                    }
                case 4:
                    relay4.Write(true);
                    if (relay4.Read())
                    {
                        Debug.Print("Switch 4 On.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning on Switch 4.");
                        return false;
                    }
                default:
                    return false;
            }
        }

        public static Boolean Off(int Switch)
        {
            switch (Switch)
            {
                case 1:
                    relay1.Write(false);
                    if (!relay1.Read())
                    {
                        Debug.Print("Switch " + Switch + " Off.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning off Switch " + Switch + ".");
                        return false;
                    }
                case 2:
                    relay2.Write(false);
                    if (!relay2.Read())
                    {
                        Debug.Print("Switch " + Switch + " Off.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning off Switch " + Switch + ".");
                        return false;
                    }
                case 3:
                    relay3.Write(false);
                    if (!relay3.Read())
                    {
                        Debug.Print("Switch " + Switch + " Off.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning off Switch " + Switch + ".");
                        return false;
                    }
                case 4:
                    relay4.Write(false);
                    if (!relay4.Read())
                    {
                        Debug.Print("Switch " + Switch + " Off.");
                        return true;
                    }
                    else
                    {
                        Debug.Print("Problem turning off Switch " + Switch + ".");
                        return false;
                    }
                default:
                    return false;
            }
        }

        public static Boolean State(int Switch)
        {
            switch (Switch)
            {
                case 1:
                    return relay1.Read();
                case 2:
                    return relay2.Read();
                case 3:
                    return relay3.Read();
                case 4:
                    return relay4.Read();
                default:
                    return false;
            }
        }
    }
}
