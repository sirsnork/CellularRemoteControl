﻿//TO-DO: Setup blink function for each output, add relay thread for this?

#region // Preprocessor code

#define LCD // set to #undef LCD if no screen is attached to COM2
#define CELL // Set to #undef to disable cellular code. Accessable only by network then. You must define either CELL or WEB.
#define WEB // set to #undef WEB to disable web server

#if (!CELL && !WEB)
    #error No network transprt defined
#endif

#endregion

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
#if (LCD)
    using seeedStudio.SerialLCD;
#endif
#if (CELL)
    using seeedStudio.GPRS;
#endif
#if (WEB)
    using NetduinoPlusWebServer;
#endif

namespace CellularRemoteControl
{
    public class Program
    {
        public static OutputPort _shieldPower = new OutputPort((Cpu.Pin)0x012, false); // power pin to shields Shields. Toggling reboots all shields

        #if (LCD)
            public static byte[] lcdMessageLine1;
            public static byte[] lcdMessageLine2;
        #endif
        #if (LCD || WEB)
            public static string SW1State = "Off ";
            public static string SW2State = "Off ";
            public static string SW3State = "Off ";
            public static string SW4State = "Off ";
            public static int LCDSleep = 0;
        #endif
        #if (WEB)
            const string WebFolder = "\\SD\\Web";
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

            #if (LCD)
                while (true)
                {
                    if (LCDSleep > 0) // This checks if we want to stop updating the LCD for a period of time (like when we display the IP)
                    {
                        Thread.Sleep(200); // Make sure we wait for a single LCD update before pausing the thread
                        lcdThread.Suspend();
                        Thread.Sleep(LCDSleep);
                        lcdThread.Resume();
                        LCDSleep = 0;
                    }
                    Thread.Sleep(10);
                }
            #else
                Thread.Sleep(Timeout.Infinite);
            #endif

            
        }
        #if (LCD)
            static void LCD_thread()
            {
                byte[] oldlcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("0");
                byte[] oldlcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("0");

                // initialise the LCD display
                LCD lcd = new LCD("COM2");

                // Timer turns Backlight off after 30 seconds of inactivity
                Timer backlightTimer = new Timer(BacklightTimerOff, lcd, 0, 30000);

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
                        if ((lcdMessageLine1 != oldlcdMessageLine1) && (lcdMessageLine2 != oldlcdMessageLine2))
                        {
                            lcd.Clear();
                        }
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
                    Thread.Sleep(200);
                }
            }
            static void BacklightTimerOff(object state)
            {
                LCD lcd = (LCD)state;
                lcd.noBacklight();
            }
        #endif

        #if (CELL)
            static void Cellular_thread()
            {
                seeedStudioGSM gprs = new seeedStudioGSM();

                gprs.TogglePower();

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
                                case "1+":
                                    if (Relay.SW1_On())
                                    {
                                        ReplySMS = "Switch 1 was turned On";
                                        #if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/
                                            SW1State = "On  ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning On Switch 1";
                                    }
                                    break;
                                case "1-":
                                    if (Relay.SW1_Off())
                                    {
                                        ReplySMS = "Switch 1 was turned Off.";
                                        #if (LCD)
                                            SW1State = "Off ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning Off Switch 1";
                                    }
                                    break;
                                case "1?":
                                    if (Relay.SW1_State())
                                    {
                                        ReplySMS = "Switch 1 is On";
                                    }
                                    else
                                    {
                                        ReplySMS = "Switch 1 is Off";
                                    }
                                    break;
                                case "2+":
                                    if (Relay.SW2_On())
                                    {
                                        ReplySMS = "Switch 2 was turned On";
                                        #if (LCD)
                                            SW2State = "On  ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning On Switch 2";
                                    }
                                    break;
                                case "2-":
                                    if (Relay.SW2_Off())
                                    {
                                        ReplySMS = "Switch 2 was turned Off.";
                                        #if (LCD)
                                            SW2State = "Off ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning Off Switch 2";
                                    }
                                    break;
                                case "2?":
                                    if (Relay.SW2_State())
                                    {
                                        ReplySMS = "Switch 2 is On";
                                    }
                                    else
                                    {
                                        ReplySMS = "Switch 2 is Off";
                                    }
                                    break;
                                case "3+":
                                    if (Relay.SW3_On())
                                    {
                                        ReplySMS = "Switch 3 was turned On";
                                        #if (LCD)
                                            SW3State = "On  ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning On Switch 3";
                                    }
                                    break;
                                case "3-":
                                    if (Relay.SW3_Off())
                                    {
                                        ReplySMS = "Switch 3 was turned Off.";
                                        #if (LCD)
                                            SW3State = "Off ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning Off Switch 3";
                                    }
                                    break;
                                case "3?":
                                    if (Relay.SW3_State())
                                    {
                                        ReplySMS = "Switch 3 is On";
                                    }
                                    else
                                    {
                                        ReplySMS = "Switch 3 is Off";
                                    }
                                    break;
                                case "4+":
                                    if (Relay.SW4_On())
                                    {
                                        ReplySMS = "Switch 4 was turned On";
                                        #if (LCD)
                                            SW4State = "On  ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning On Switch 4";
                                    }
                                    break;
                                case "4-":
                                    if (Relay.SW4_Off())
                                    {
                                        ReplySMS = "Switch 4 was turned Off.";
                                        #if (LCD)
                                            SW4State = "Off ";
                                        #endif
                                    }
                                    else
                                    {
                                        ReplySMS = "Error turning Off Switch 4";
                                    }
                                    break;
                                case "4?":
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
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("SW1:" + SW1State + "SW2:" + SW2State);
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("SW3:" + SW3State + "SW4:" + SW4State);
                    #endif
                    Thread.Sleep(1000);
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
                #if (LCD)
                    InterruptPort button = new InterruptPort(Pins.GPIO_PIN_D8, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
                    // Create an event handler for the button
                    button.OnInterrupt += new NativeEventHandler(button_OnInterrupt);
                #endif

                Listener webServer = new Listener(RequestReceived);

                Thread.Sleep(Timeout.Infinite);

            }
            #if (LCD)
                public static void DisplayIP()
                {
                    var IPAddress = "";
                    IPAddress = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress;
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("  IP Address:");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes(IPAddress);
                    LCDSleep = 10000;
                }
                private static void button_OnInterrupt(uint port, uint data, DateTime time)
                {
                    DisplayIP();
                }
            #endif

            private static void RequestReceived(Request request)
            {
/*
                string Button1On = "";
                string Button1Off = "";
                string Button2On = "";
                string Button2Off = "";
                string Button3On = "";
                string Button3Off = "";
                string Button4On = "";
                string Button4Off = "";
*/
                // Use this for a really basic check that it's working
                //request.SendResponse("<html><body><p>Request from " + request.Client.ToString() + " received at " + DateTime.Now.ToString() + "</p><p>Method: " + request.Method + "<br />URL: " + request.URL + "</p></body></html>");
                Debug.Print("Request from " + request.Client.ToString() + " received at " + DateTime.Now.ToString() + ". Method: " + request.Method + " URL: " + request.URL);

                if (request.URL.Substring(0, 9) == "/switches")
                {
                    if (request.URL.Length > 9)
                    {
                        string[] parameters = request.URL.Substring(request.URL.Length - 3, 3).Split('=');
                        switch (parameters[0])
                        {
                            case "1":
                                if (parameters[1] == "1")
                                {
                                    if (Relay.SW1_On())
                                        SW1State = "On  ";
                                }
                                else if (parameters[1] == "0")
                                {
                                    if (Relay.SW1_Off())
                                        SW1State = "Off ";
                                }
                                else
                                {
                                }
                                break;
                            case "2":
                                if (parameters[1] == "1")
                                {
                                    if (Relay.SW2_On())
                                        SW2State = "On  ";
                                }
                                else if (parameters[1] == "0")
                                {
                                    if (Relay.SW2_Off())
                                        SW2State = "Off ";
                                }
                                else
                                {
                                }
                                break;
                            case "3":
                                if (parameters[1] == "1")
                                {
                                    if (Relay.SW3_On())
                                        SW3State = "On  ";
                                }
                                else if (parameters[1] == "0")
                                {
                                    if (Relay.SW3_Off())
                                        SW3State = "Off ";
                                }
                                else
                                {
                                }
                                break;
                            case "4":
                                if (parameters[1] == "1")
                                {
                                    if (Relay.SW4_On())
                                        SW4State = "On  ";
                                }
                                else if (parameters[1] == "0")
                                {
                                    if (Relay.SW4_Off())
                                        SW4State = "Off ";
                                }
                                else
                                {
                                }
                                break;
                            default:
                                break;
                        }
                        #if (LCD)
                            lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("SW1:" + SW1State + "SW2:" + SW2State);
                            lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("SW3:" + SW3State + "SW4:" + SW4State);
                        #endif

/*
                    }

                    if (Relay.SW1_State())
                    {
                        Button1On = "false";
                        Button1Off = "true";
                    }
                    else
                    {
                        Button1On = "true";
                        Button1Off = "false";
                    }
                    if (Relay.SW2_State())
                    {
                        Button2On = "false";
                        Button2Off = "true";
                    }
                    else
                    {
                        Button2On = "true";
                        Button2Off = "false";
                    }
                    if (Relay.SW3_State())
                    {
                        Button3On = "false";
                        Button3Off = "true";
                    }
                    else
                    {
                        Button3On = "true";
                        Button3Off = "false";
                    }
                    if (Relay.SW4_State())
                    {
                        Button4On = "false";
                        Button4Off = "true";
                    }
                    else
                    {
                        Button4On = "true";
                        Button4Off = "false";
                    }
*/
                    request.SendResponse(@"<html>
                        <head>
                        </head>
                        <body>
                        <p>
                        <input type=""button"" value=""Switch 1 on""
                        onclick=""window.location.href='/switches?1=1'""/>
                        <input type=""button"" value=""Switch 1 off""
                        onclick=""window.location.href='/switches?1=0'""/><br>
                        <input type=""button"" value=""Switch 2 on""
                        onclick=""window.location.href='/switches?2=1'""/>
                        <input type=""button"" value=""Switch 2 off""
                        onclick=""window.location.href='/switches?2=0'""/><br>
                        <input type=""button"" value=""Switch 3 on""
                        onclick=""window.location.href='/switches?3=1'""/>
                        <input type=""button"" value=""Switch 3 off""
                        onclick=""window.location.href='/switches?3=0'""/><br>
                        <input type=""button"" value=""Switch 4 on""
                        onclick=""window.location.href='/switches?4=1'""/>
                        <input type=""button"" value=""Switch 4 off""
                        onclick=""window.location.href='/switches?4=0'""/>
                        </p>
                        </body>
                        </html>");
                }
                else
                {
                    // Try to send a file
                    TrySendFile(request);
                }
            }

            /// <summary>
            /// Look for a file on the SD card and send it back if it exists
            /// </summary>
            /// <param name="request"></param>
            
            private static void TrySendFile(Request request)
            {
                // Replace / with \
                string filePath = WebFolder + request.URL.Replace('/', '\\');

                if (File.Exists(filePath))
                    request.SendFile(filePath);
                else
                    request.Send404();
            }
        #endif
    }
}
