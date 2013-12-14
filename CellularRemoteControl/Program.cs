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


        public static void Main()
        {
            // write your code here
            seedStudioGSM seed = new seedStudioGSM();
            
            _led_Active.Write(true);
            Thread.Sleep(30000);
            _led_Active.Write(false);
            
            seed.SIM900_FirmwareVersion();
            seed.SIM900_SignalQuality();

            Thread.Sleep(10000);
            
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
            
            // File containing Cellphone number to send SMS too
            string NumCellDefault = FileTools.ReadString("settings\\NumCellDefault.txt");

            _led_NewMessage.Write(true);
            Thread.Sleep(2000);
            seed.SendSMS(NumCellDefault, "Remote switch controller operational");
            _led_NewMessage.Write(false);
  
            while (true)
            {
                //Debug.Print("Received SMS");
                _led_Active.Write(true);
                Thread.Sleep(5000);
                if (seedStudioGSM.LastMessage > 0)
                {
                    _led_NewMessage.Write(true);
                   seed.ReadSMS(seedStudioGSM.LastMessage);
                   seed.DeleteAllSMS();
                   if (File.Exists(@"SD\\SMS.cmd"))
                   {
                       string ReplySMS = "";
                       string[] command = FileTools.ReadString("SMS.cmd").Split(';');
                       File.Delete(@"SD\\SMS.cmd");
                       Debug.Print (command[0] + "   " + command[1]);

                       switch (command[1].ToUpper())
                       {
                           case "SW1_ON":
                               if (Cellular.Sw1_On())
                               {
                                   ReplySMS = "Switch 1 was turned On"; 
                               }
                               else
                               {
                                   ReplySMS = "Error turning On Switch 1"; 
                               }
                               break;
                           case "SW1_OFF":
                               if (Cellular.SW1_Off())
                               {
                                   ReplySMS = "Switch 1 was turned Off.";
                               }
                               else
                               {
                                   ReplySMS = "Error turning Off Switch 1";
                               }
                               break;
                           case "SW1_STATE":
                               if (Cellular.SW1_State())
                               {
                                   ReplySMS = "Switch 1 is On"; 
                               }
                               else
                               {
                                   ReplySMS = "Switch 1 is Off"; 
                               }
                               break;
                           case "SW2_ON":
                               if (Cellular.SW2_On())
                               {
                                   ReplySMS = "Switch 2 was turned On";
                               }
                               else
                               {
                                   ReplySMS = "Error turning On Switch 2";
                               }
                               break;
                           case "SW2_OFF":
                               if (Cellular.SW2_Off())
                               {
                                   ReplySMS = "Switch 2 was turned Off.";
                               }
                               else
                               {
                                   ReplySMS = "Error turning Off Switch 2";
                               }
                               break;
                           case "SW2_STATE":
                               if (Cellular.SW2_State())
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
                               seed.SendSMS(NumCellDefault,"SMS da: " + command[0] + "\n\r" + command[1]);
                               break;
                       }
                       if (ReplySMS.Length > 0)
                           seed.SendSMS(command[0], ReplySMS);
                   }
                   _led_NewMessage.Write(false);
                }

                _led_Active.Write(false);
                Thread.Sleep(5000);
           }
        }
    }
}
