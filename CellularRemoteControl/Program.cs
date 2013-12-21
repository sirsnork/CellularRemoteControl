using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using System.IO;

namespace CellularRemoteControl
{
    public class Program
    {

        public static OutputPort _led_Active = new OutputPort(Pins.GPIO_PIN_D2, false);
        public static OutputPort _led_NewMessage = new OutputPort(Pins.GPIO_PIN_D3, false);
        public static OutputPort _GPRS_Power_Active = new OutputPort(Pins.GPIO_PIN_D9, false);

        public static void Main()
        {
            // write your code here
            seedStudioGSM seed = new seedStudioGSM();

            // Automatically power up the SIM900.
            Debug.Print("Powering up Modem");
            _GPRS_Power_Active.Write(true);
            Thread.Sleep(2500);
            _GPRS_Power_Active.Write(false);
            // End of SIM900 power up.

            _led_Active.Write(true);
            Thread.Sleep(10000);
            _led_Active.Write(false);
            
            seed.SIM900_FirmwareVersion();
            seed.SIM900_SignalQuality();

            Thread.Sleep(5000);
            
            // Excellent Signal
            if ((seedStudioGSM.SignalStrength >= 20) && (seedStudioGSM.SignalStrength <= 31))
            {
                Debug.Print("Signal: Excellent");
                _led_Active.Write(true);
                Thread.Sleep(3000);
                _led_Active.Write(false);
            }
            // Good Signal
            if ((seedStudioGSM.SignalStrength >= 13) && (seedStudioGSM.SignalStrength <= 19))
            {
                Debug.Print("Signal: Good");    
                _led_Active.Write(true);
                _led_NewMessage.Write(true);
                Thread.Sleep(3000);
                _led_Active.Write(false);
                _led_NewMessage.Write(false);
            }
            // Poor Signal
            if ((seedStudioGSM.SignalStrength >= 0) && (seedStudioGSM.SignalStrength <= 12))
            {
                Debug.Print("Signal: Poor");
                _led_NewMessage.Write(true);
                Thread.Sleep(3000);
                _led_NewMessage.Write(false);
            }
            // No Signal
            if (seedStudioGSM.SignalStrength == 99)
            {
                Debug.Print("No Signal"); 
                _led_Active.Write(true);
                _led_NewMessage.Write(true);
                Thread.Sleep(1000);
                _led_NewMessage.Write(false);
                Thread.Sleep(1000);
                _led_NewMessage.Write(true);
                Thread.Sleep(1000);
                _led_NewMessage.Write(false);
                Thread.Sleep(1000);
                _led_NewMessage.Write(true);
                Thread.Sleep(1000);
                _led_NewMessage.Write(false);
                Thread.Sleep(1000);
                _led_NewMessage.Write(true);
                Thread.Sleep(1000);
                _led_NewMessage.Write(false);
                Thread.Sleep(1000);
                _led_NewMessage.Write(true);
                Thread.Sleep(1000);
                _led_NewMessage.Write(false);
                _led_Active.Write(false);
            }
            
            Thread.Sleep(10000);
            
            seed.InitializeSMS();
            seed.DeleteAllSMS();
            seed.SIM900_GetTime();

            // File containing Cellphone number to send initialization SMS too
            string NumCellDefault = FileTools.ReadString("settings\\NumCellDefault.txt");
            // File containing cellphone numbers to accept commands from, all others are ignored, splitting on + sign since we get a single string back from the file read
            string[] CellWhitelist = FileTools.ReadString("settings\\Whitelist.txt").Split('+');

            _led_NewMessage.Write(true);
            Thread.Sleep(2000);
            seed.SendSMS(NumCellDefault, "Remote switch controller operational");
            _led_NewMessage.Write(false);
  
            while (true)
            {
                _led_Active.Write(true);
                Thread.Sleep(5000);
                if (seedStudioGSM.LastMessage > 0)
                {
                    _led_NewMessage.Write(true);
                   seed.ReadSMS(seedStudioGSM.LastMessage);
                   seed.DeleteAllSMS();
                   if (File.Exists(@"SD\\Temp\\SMS.cmd"))
                   {
                       string ReplySMS = "";
                       string[] command = FileTools.ReadString("Temp\\SMS.cmd").Split(';');
                       File.Delete(@"SD\\Temp\\SMS.cmd");
                       Debug.Print ("Commands: " + command[0] + "   " + command[1]);

                       if (CheckNumberWhitelist(command[0], CellWhitelist))
                       {
                           switch (command[1].Trim().ToUpper())
                           {
                               case "SW1_ON":
                                   if (Relay.SW1_On())
                                   {
                                       ReplySMS = "Switch 1 was turned On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning On Switch 1";
                                   }
                                   break;
                               case "SW1_OFF":
                                   if (Relay.SW1_Off())
                                   {
                                       ReplySMS = "Switch 1 was turned Off.";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning Off Switch 1";
                                   }
                                   break;
                               case "SW1_STATE":
                                   if (Relay.SW1_State())
                                   {
                                       ReplySMS = "Switch 1 is On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Switch 1 is Off";
                                   }
                                   break;
                               case "SW2_ON":
                                   if (Relay.SW2_On())
                                   {
                                       ReplySMS = "Switch 2 was turned On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning On Switch 2";
                                   }
                                   break;
                               case "SW2_OFF":
                                   if (Relay.SW2_Off())
                                   {
                                       ReplySMS = "Switch 2 was turned Off.";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning Off Switch 2";
                                   }
                                   break;
                               case "SW2_STATE":
                                   if (Relay.SW2_State())
                                   {
                                       ReplySMS = "Switch 2 is On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Switch 2 is Off";
                                   }
                                   break;
                               default:
                                   ReplySMS = "";
                                   Debug.Print("Unknown Command: " + command[1] + " from " + command[0]);
                                   seed.SendSMS(command[0],"Unknown command from " + command[0] + ": " + command[1]);
                                   break;
                           }
                           if (ReplySMS.Length > 0)
                               seed.SendSMS(command[0], ReplySMS);
                           _led_NewMessage.Write(false);
                       }
                       else
                           Debug.Print(command[0] + " not in whitelist, message ignored");
                    }
                }
                _led_Active.Write(false);
                Thread.Sleep(5000);
            }
        }
        public static Boolean CheckNumberWhitelist(string CellNumber, string[] CellWhiteList)
        {
            for (int j = 1; j < CellWhiteList.Length; j++) // start at 1 as the first entry in CellWhiteList will be blank as we split on '+'
            {
                if (CellNumber == '+' + CellWhiteList[j])
                {
                    return true;
                }
            }
            return false;
        }
    }
}
