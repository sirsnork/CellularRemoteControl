#define LCD // set to #undef LCD if no screen is attached to COM2
#define CELL // Set to #undef to disable cellular code. Accessable only by network then. You must define either CELL or WEB.
#define WEB // set to #undef WEB to disable web server (not yet implemented)
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
using seeedStudio.SerialLCD;

namespace CellularRemoteControl
{
    public class Program
    {
        public static OutputPort _GPRS_Power_Active = new OutputPort(Pins.GPIO_PIN_D9, false); //soft power on pin for GPRS shield
        public static OutputPort _shieldPower = new OutputPort((Cpu.Pin)0x012, false); // power pin to shields Shields. Toggling reboots all shields

        #if (LCD)
            public static byte[] lcdMessageLine1;
            public static byte[] lcdMessageLine2;
            public static string SW1State = "SW1:Off ";
            public static string SW2State = "SW2:Off ";
            public static string SW3State = "SW3:Off ";
            public static string SW4State = "SW4:Off ";
        #endif

        public static void Main()
        {
            // Power cycle the shields
            Thread.Sleep(200); // Don't bounce power to the shields too fast
            _shieldPower.Write(true);
            Thread.Sleep(700); // Let the shields come up before trying to access them

            #if (LCD)
                var lcdThread = new Thread(LCD_thread);
                lcdThread.Start();
            #endif

            #if (CELL)
                var cellularThread = new Thread(Cellular_thread);
                cellularThread.Start();
            #endif

           #if (WEB)
                var WebThread = new Thread(Web_thread);
                WebThread.Start();
            #endif

            Thread.Sleep(Timeout.Infinite);
        }
        #if (LCD)
            static void LCD_thread()
            {
                byte[] oldlcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("0");
                byte[] oldlcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("0");
                Timer backlightTimer = null;
                TimerCallback backlightTimeout = null;

                // initialise the LCD display
                LCD lcd = new LCD("COM2");

                // Timer turns Backlight off after 30 seconds of inactivity
                backlightTimeout = new TimerCallback(BacklightTimerInterrupt);
                backlightTimer = new Timer(backlightTimeout, lcd, 0, 30000);

                Thread.Sleep(4000);

                // Turn on Backlight for LCD, flickers due to power when only running on USB
                lcd.backlight();
                lcd.Clear();

                while (true)
                {
                    // Check if LCD data needs to be updated
                    if (System.Convert.ToBase64String(lcdMessageLine1) != System.Convert.ToBase64String(oldlcdMessageLine1) || System.Convert.ToBase64String(lcdMessageLine2) != System.Convert.ToBase64String(oldlcdMessageLine2))
                    {
                        lcd.backlight();
                        backlightTimer.Change(30000, 0); // Reset backlight timer to 30 seconds after screen change
                        if (lcdMessageLine1 != oldlcdMessageLine1)
                        {
                            oldlcdMessageLine1 = lcdMessageLine1;
                            lcd.SetCursor(0, 0);
                            lcd.print(lcdMessageLine1);
                        }
                        if (lcdMessageLine2 != oldlcdMessageLine2)
                        {
                            oldlcdMessageLine2 = lcdMessageLine2;
                            lcd.SetCursor(0, 1);
                            lcd.print(lcdMessageLine2);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            static void BacklightTimerInterrupt(object state)
            {
                LCD lcd = (LCD)state;
                lcd.noBacklight();
            }
        #endif

        #if (CELL)
            static void Cellular_thread()
            {
                seeedStudioGSM gprs = new seeedStudioGSM();

                // Automatically power up the SIM900.
                Debug.Print("Powering up Modem");
                _GPRS_Power_Active.Write(true);
                Thread.Sleep(2500);
                _GPRS_Power_Active.Write(false);        
                // End of SIM900 power up.

                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("  Initializing");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("     Modem");
                #endif

                Thread.Sleep(20000);
            
                gprs.SIM900_FirmwareVersion();
                gprs.SIM900_SignalQuality();

                Thread.Sleep(5000);
            
                // Excellent Signal
                if ((seeedStudioGSM.SignalStrength >= 20) && (seeedStudioGSM.SignalStrength <= 31))
                {
                    Debug.Print("Signal: Excellent");
                    #if (LCD)
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("   Excellent");
                    #endif
                }
                // Good Signal
                if ((seeedStudioGSM.SignalStrength >= 13) && (seeedStudioGSM.SignalStrength <= 19))
                {
                    Debug.Print("Signal: Good");
                    #if (LCD)
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      Good");
                    #endif
                }
                // Poor Signal
                if ((seeedStudioGSM.SignalStrength >= 0) && (seeedStudioGSM.SignalStrength <= 12))
                {
                    Debug.Print("Signal: Poor");
                    #if (LCD)
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      Poor");
                    #endif
                }
                // No Signal
                if (seeedStudioGSM.SignalStrength == 99)
                {
                    Debug.Print("No Signal");
                    #if (LCD)
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      None");
                    #endif
                }

                gprs.InitializeSMS();
                gprs.DeleteAllSMS();
                gprs.SIM900_GetTime();

                // File containing Cellphone number to send initialization SMS too
                string NumCellDefault = FileTools.ReadString("settings\\NumCellDefault.txt");
                // File containing cellphone numbers to accept commands from, all others are ignored, splitting on + sign since we get a single string back from the file read
                string[] CellWhitelist = FileTools.ReadString("settings\\Whitelist.txt").Split('+');

                // Send SMS to default number saying we are up!
                //gprs.SendSMS(NumCellDefault, "Remote switch controller operational");

                while (true)
                {
                    if (seeedStudioGSM.LastMessage > 0)
                    {
                       gprs.ReadSMS(seeedStudioGSM.LastMessage);
                       gprs.DeleteAllSMS();
                       if (File.Exists(@"SD\\Temp\\SMS.cmd"))
                       {
                            string ReplySMS = "";
                            string[] command = FileTools.ReadString("Temp\\SMS.cmd").Split(';');
                            File.Delete(@"SD\\Temp\\SMS.cmd");
                            Debug.Print ("Commands: " + command[0] + "   " + command[1]);

                            if (CheckNumberWhitelist(command[0], CellWhitelist)) // Make sure incoming message was sent from allowed number
                            {
                                switch (command[1].Trim().ToUpper())
                                {
                                case "SW1_ON":
                                    if (Relay.SW1_On())
                                    {
                                        ReplySMS = "Switch 1 was turned On";
                                        #if (LCD)
                                            SW1State = "SW1:On  ";
                                        #endif
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
                                        #if (LCD)
                                            SW1State = "SW1:Off ";
                                        #endif
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
                                        #if (LCD)
                                            SW2State = "SW2:On  ";
                                        #endif
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
                                        #if (LCD)
                                            SW2State = "SW2:Off ";
                                        #endif
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
                                        #if (LCD)
                                            SW3State = "SW3:On  ";
                                        #endif
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
                                        #if (LCD)
                                            SW3State = "SW3:Off ";
                                        #endif
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
                                        #if (LCD)
                                            SW4State = "SW4:On  ";
                                        #endif
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
                                        #if (LCD)
                                            SW4State = "SW1:Off ";
                                        #endif
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
                                       gprs.SendSMS(command[0], "Unknown command from " + command[0] + ": " + command[1]);
                                       break;
                               }
                               if (ReplySMS.Length > 0)
                                   gprs.SendSMS(command[0], ReplySMS);
                           }
                           else
                               Debug.Print(command[0] + " not in whitelist, message ignored");
                        }
                    }
                    #if (LCD)
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes(SW1State + SW2State);
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes(SW3State + SW4State);
                    #endif
                    Thread.Sleep(5000);
                }
            }
            public static Boolean CheckNumberWhitelist(string CellNumber, string[] CellWhiteList)
            {
                for (int j = 1; j < CellWhiteList.Length; j++) // start at 1 as the first entry in CellWhiteList will be blank as we split on '+' and '+' was the first character
                {
                    if (CellNumber == '+' + CellWhiteList[j])
                    {
                        return true;
                    }
                }
                return false;
            }
        #endif

        #if (WEB)
            public static void Web_thread()
            {
            }
        #endif
    }
}
