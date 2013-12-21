using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace CellularRemoteControl
{
    class Relay
    {
        private static OutputPort relay2 = new OutputPort(Pins.GPIO_PIN_D6, false);
        private static OutputPort relay1 = new OutputPort(Pins.GPIO_PIN_D7, false);
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
    }
}
