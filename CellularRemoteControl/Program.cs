#define LCD
#define WEB
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
        public static OutputPort _GPRS_Power_Active = new OutputPort(Pins.GPIO_PIN_D9, false);
        public static OutputPort _shieldPower = new OutputPort((Cpu.Pin)0x012, false); // power pin to shields Shields. Toggling reboots all shields

        public static void Main()
        {
            #if (LCD)
                byte[] lcdMessageLine1;
                byte[] lcdMessageLine2;
                byte[] oldlcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("0");
                byte[] oldlcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("0");
                string SW1State;
                string SW2State;
                string SW3State;
                string SW4State;
                bool backlightState = false;
            #endif
            // Power cycle the shields
            Thread.Sleep(200); // Don't bounce power to the shields too fast
            _shieldPower.Write(true);
            Thread.Sleep(200);

            seedStudioGSM seed = new seedStudioGSM();
           
            #if (LCD)
                // initialise the LCD display
                LCD mylcd = new LCD("COM2");

                Thread.Sleep(500);

                // Turn on Backlight for LCD, flickers due to power when only running on USB
                mylcd.backlight();
                mylcd.Clear();
            
                lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("  Initializing");
                lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("     Modem");
                mylcd.SetCursor(0, 0);
                mylcd.print(lcdMessageLine1);
                mylcd.SetCursor(0, 1);
                mylcd.print(lcdMessageLine2);
            #endif
            // Automatically power up the SIM900.
            Debug.Print("Powering up Modem");
            _GPRS_Power_Active.Write(true);
            Thread.Sleep(2500);
            _GPRS_Power_Active.Write(false);        
            // End of SIM900 power up.

            Thread.Sleep(15000);
            
            seed.SIM900_FirmwareVersion();
            seed.SIM900_SignalQuality();

            Thread.Sleep(5000);
            
            // Excellent Signal
            if ((seedStudioGSM.SignalStrength >= 20) && (seedStudioGSM.SignalStrength <= 31))
            {
                Debug.Print("Signal: Excellent");
                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("   Excellent");
                #endif
            }
            // Good Signal
            if ((seedStudioGSM.SignalStrength >= 13) && (seedStudioGSM.SignalStrength <= 19))
            {
                Debug.Print("Signal: Good");
                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      Good");
                #endif
            }
            // Poor Signal
            if ((seedStudioGSM.SignalStrength >= 0) && (seedStudioGSM.SignalStrength <= 12))
            {
                Debug.Print("Signal: Poor");
                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      Poor");
                #endif
            }
            // No Signal
            if (seedStudioGSM.SignalStrength == 99)
            {
                Debug.Print("No Signal");
                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      None");
                #endif
            }
            #if (LCD)
                mylcd.Clear();
                mylcd.SetCursor(0, 0);
                mylcd.print(lcdMessageLine1);
                mylcd.SetCursor(0, 1);
                mylcd.print(lcdMessageLine2);
            #endif           
            Thread.Sleep(10000);
            
            seed.InitializeSMS();
            seed.DeleteAllSMS();
            seed.SIM900_GetTime();

            // File containing Cellphone number to send initialization SMS too
            string NumCellDefault = FileTools.ReadString("settings\\NumCellDefault.txt");
            // File containing cellphone numbers to accept commands from, all others are ignored, splitting on + sign since we get a single string back from the file read
            string[] CellWhitelist = FileTools.ReadString("settings\\Whitelist.txt").Split('+');

            // Send SMS to default number saying we are up!
            //seed.SendSMS(NumCellDefault, "Remote switch controller operational");

            while (true)
            {
                if (seedStudioGSM.LastMessage > 0)
                {
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
                               case "SW3_ON":
                                   if (Relay.SW3_On())
                                   {
                                       ReplySMS = "Switch 3 was turned On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning On Switch 3";
                                   }
                                   break;
                               case "SW3_OFF":
                                   if (Relay.SW3_Off())
                                   {
                                       ReplySMS = "Switch 3 was turned Off.";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning Off Switch 3";
                                   }
                                   break;
                               case "SW3_STATE":
                                   if (Relay.SW3_State())
                                   {
                                       ReplySMS = "Switch 3 is On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Switch 3 is Off";
                                   }
                                   break;
                               case "SW4_ON":
                                   if (Relay.SW4_On())
                                   {
                                       ReplySMS = "Switch 4 was turned On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning On Switch 4";
                                   }
                                   break;
                               case "SW4_OFF":
                                   if (Relay.SW4_Off())
                                   {
                                       ReplySMS = "Switch 4 was turned Off.";
                                   }
                                   else
                                   {
                                       ReplySMS = "Error turning Off Switch 4";
                                   }
                                   break;
                               case "SW4_STATE":
                                   if (Relay.SW4_State())
                                   {
                                       ReplySMS = "Switch 4 is On";
                                   }
                                   else
                                   {
                                       ReplySMS = "Switch 4 is Off";
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
                       }
                       else
                           Debug.Print(command[0] + " not in whitelist, message ignored");
                    }
                }
                #if (LCD)
                    if (Relay.SW1_State())
                    {
                        SW1State = "SW1:On  ";
                    }
                    else
                    {
                        SW1State = "SW1:Off ";
                    }
                    if (Relay.SW2_State())
                    {
                        SW2State = "SW2:On";
                    }
                    else
                    {
                        SW2State = "SW2:Off";
                    }
                    if (Relay.SW3_State())
                    {
                        SW3State = "SW3:On  ";
                    }
                    else
                    {
                        SW3State = "SW3:Off ";
                    }
                    if (Relay.SW4_State())
                    {
                        SW4State = "SW4:On ";
                    }
                    else
                    {
                        SW4State = "SW4:Off";
                    }
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes(SW1State + SW2State);
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes(SW3State + SW4State);
                    if (System.Convert.ToBase64String(lcdMessageLine1) != System.Convert.ToBase64String(oldlcdMessageLine1) || System.Convert.ToBase64String(lcdMessageLine2) != System.Convert.ToBase64String(oldlcdMessageLine2))
                    {
                        mylcd.backlight();
                        backlightState = true;
                        if (lcdMessageLine1 != oldlcdMessageLine1)
                        {
                            //mylcd.Clear();
                            oldlcdMessageLine1 = lcdMessageLine1;
                            mylcd.SetCursor(0, 0);
                            mylcd.print(lcdMessageLine1);
                        }
                        if (lcdMessageLine2 != oldlcdMessageLine2)
                        {
                            oldlcdMessageLine2 = lcdMessageLine2;
                            mylcd.SetCursor(0, 1);
                            mylcd.print(lcdMessageLine2);
                        }
                    }
                    else
                    {
                        if (backlightState == true)
                        {
                            mylcd.noBacklight();
                            backlightState = false;
                        }
                    }
                #endif
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
