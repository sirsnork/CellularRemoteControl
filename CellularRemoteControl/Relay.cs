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

        public static Boolean SW1_On()
        {
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
        }

        public static Boolean SW1_Off()
        {
            relay1.Write(false);
            if (!relay1.Read())
            {
                Debug.Print("Switch 1 Off.");
                return true;
            }
            else
            {
                Debug.Print("Problem turning off Switch 1.");
                return false;
            }
        }

        public static Boolean SW1_State()
        {
            return relay1.Read();
        }

        public static Boolean SW2_On()
        {
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
        }

        public static Boolean SW2_Off()
        {
            relay2.Write(false);
            if (!relay2.Read())
            {
                Debug.Print("Switch 2 Off.");
                return true;
            }
            else
            {
                Debug.Print("Problem turning off Switch 2.");
                return false;
            }
        }

        public static Boolean SW2_State()
        {
            return relay2.Read();
        }

        public static Boolean SW3_On()
        {
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
        }

        public static Boolean SW3_Off()
        {
            relay3.Write(false);
            if (!relay3.Read())
            {
                Debug.Print("Switch 3 Off.");
                return true;
            }
            else
            {
                Debug.Print("Problem turning off Switch 3.");
                return false;
            }
        }

        public static Boolean SW3_State()
        {
            return relay3.Read();
        }

        public static Boolean SW4_On()
        {
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
        }

        public static Boolean SW4_Off()
        {
            relay4.Write(false);
            if (!relay4.Read())
            {
                Debug.Print("Switch 4 Off.");
                return true;
            }
            else
            {
                Debug.Print("Problem turning off Switch 4.");
                return false;
            }
        }

        public static Boolean SW4_State()
        {
            return relay4.Read();
        }
    }
}
