//TO-DO:    Setup blink function for each output, add relay thread for this?
//          Handle multiple commands in a single message
//
//          Handle network disconnection and reconnection cleanly (restart web thread?). Something like this
/*
            using Microsoft.SPOT.Net.NetworkInformation;

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged; 
 
            static bool isNetworkAvailable = false;

            static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
            {
                isNetworkAvailable = e.IsAvailable;
                if (isNetworkAvailable == true)
                {
                    WebThread.Start();
                }
                else
                {
                    _NetworkDisconnected = false;
                }
                Debug.Print(DateTime.UtcNow.ToString("u") + ": " + (isNetworkAvailable ? "CONNECTED" : "DISCONNECTED"));
            }
*/

//          Add password protected whitelist addition. First phone sends password and becomes master, additional phones can send password SMS message and be added to whitelist
//
//          Work out some way to store switch type and switch name, so we can determine what routing to run to actually switch it
//

#region // Preprocessor code

#undef LCD // set to #undef LCD if no screen is attached to COM2
#define CELL // Set to #undef to disable cellular code. Accessable only by network then. You must define either CELL or WEB.
#undef WEB // set to #undef WEB to disable web server
#define XBEE

#if (!CELL && !WEB)
#error No network transport defined
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
using seeedStudio.Grove.SerialLCD;
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
        public static OutputPort _shieldPower = new OutputPort((Cpu.Pin)0x012, false); // power pin to shields. Toggling reboots all shields
        public static int NumSwitches = 4;

#if (CELL)
            public static int gsmbaudrate = 115200;
            public static string gsmcomport = "COM1";
            public static bool InitTimeout = false;
#endif
#if (LCD)
            public static byte[] lcdMessageLine1;
            public static byte[] lcdMessageLine2;
            public static int LCDSleep = 0;
#endif
#if (LCD || WEB)
            public static string[] SWState = { "Off ", "Off ", "Off ", "Off ", "Off", "Off" }; // Maybe convert this to an array of an array so we can store Name, State (bool), LCDState etc for each switch
#endif
#if (WEB)
            const string WebFolder = "\\SD\\Web";
        #endif
#if XBEE
        public static ulong[] XbeeAddress = { 0x0013A20040A78109, 0x0013A20040A780F7 };
        public static string xbeecomport = "COM2";
        public static int NumXbeeSwitches = 2;
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
                    Thread.Sleep(50);
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
                Timer backlightTimer = new Timer(BacklightTimerOff, lcd, 30000, 0);

                Thread.Sleep(2000);

                // Turn on Backlight for LCD, flickers due to power when only running on USB
                lcd.backlightOn();
                lcd.clear();

                while (true)
                {
                    // Check if LCD data needs to be updated
                    if (System.Convert.ToBase64String(lcdMessageLine1) != System.Convert.ToBase64String(oldlcdMessageLine1) || System.Convert.ToBase64String(lcdMessageLine2) != System.Convert.ToBase64String(oldlcdMessageLine2))
                    {
                        lcd.backlightOn();
                        backlightTimer.Change(30000, 0); // Reset backlight timer to 30 seconds after screen change
                        if ((lcdMessageLine1 != oldlcdMessageLine1) && (lcdMessageLine2 != oldlcdMessageLine2))
                        {
                            lcd.clear();
                        }
                        if (lcdMessageLine1 != oldlcdMessageLine1)
                        {
                            oldlcdMessageLine1 = lcdMessageLine1;
                            lcd.setCursor(0, 0);
                            lcd.print(lcdMessageLine1);
                        }
                        if (lcdMessageLine2 != oldlcdMessageLine2)
                        {
                            oldlcdMessageLine2 = lcdMessageLine2;
                            lcd.setCursor(0, 1);
                            lcd.print(lcdMessageLine2);
                        }
                    }
                    Thread.Sleep(200);
                }
            }
            static void BacklightTimerOff(object state)
            {
                LCD lcd = (LCD)state;
                lcd.backlightOff();
            }
        #endif

        #if (CELL)
            static void Cellular_thread()
            {
                seeedStudioGSM gprs = new seeedStudioGSM(gsmcomport, gsmbaudrate);
                seeedStudioGSM.SIM900_TogglePower(); // If this actually powers down the modem we catch the power down message and TogglePower again through the DataHandler
                Timer ModemInitTimeout = new Timer(ModemInitTimer, gprs, 30000, 0); // Timeout for modem to return "Call Ready". If it doesn't we assume the modem is set to the wrong baud rate and send baud rate command

                int requestedSwitch;
                string requestedAction;
                bool WhitelistExists = false;
                bool MasterCellExists = false;
                bool MasterPasswordExists = false;
                string MasterPassword = "";
                string[] Whitelist = {""};
                string MasterCell = "";

                XBee xbee = new XBee(xbeecomport);

                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("  Powering up");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("     Modem");
                #endif

                while (InitTimeout == false && seeedStudioGSM.ModemReady == false)
                {
                    Thread.Sleep(100); // Wait for either the modem to report "Call Ready" or for 30 seconds to pass
                }

                ModemInitTimeout.Dispose(); // Dispose of timer even if never fired.

                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("  Initializing");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("     Modem");
                #endif

                gprs.SIM900_FirmwareVersion();
                gprs.SIM900_SignalQuality();
                gprs.SIM900_SetTime();

                // Display signal quality on LCD
                #if (LCD)
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("Signal Quality:");
                #endif
                // Excellent Signal
                if ((seeedStudioGSM.SignalStrength >= 20) && (seeedStudioGSM.SignalStrength <= 31))
                {
                    Debug.Print("Signal: Excellent");
                    #if (LCD)
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("   Excellent");
                    #endif
                }
                // Good Signal
                else if ((seeedStudioGSM.SignalStrength >= 13) && (seeedStudioGSM.SignalStrength <= 19))
                {
                    Debug.Print("Signal: Good");
                    #if (LCD)
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      Good");
                    #endif
                }
                // Poor Signal
                else if ((seeedStudioGSM.SignalStrength >= 0) && (seeedStudioGSM.SignalStrength <= 12))
                {
                    Debug.Print("Signal: Poor");
                    #if (LCD)
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      Poor");
                    #endif
                }
                // No Signal
                else if (seeedStudioGSM.SignalStrength == 99)
                {
                    Debug.Print("No Signal");
                    #if (LCD)
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("      None");
                    #endif
                }
                else
                {
                    Debug.Print("Unknown Signal");
                    #if (LCD)
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("     Unknown");
                    #endif
                }

                gprs.InitializeSMS();
                gprs.DeleteAllSMS();

                // File containing Cellphone number to send initialization SMS too
                if (File.Exists(@"SD\settings\MasterCell.txt"))
                {
                    MasterCell = FileTools.ReadString("settings\\MasterCell.txt").TrimEnd(';');
                    MasterCellExists = true;
                }
                // File containing Master password to allow whitelist additions
                if (File.Exists(@"SD\settings\MasterPassword.txt"))
                {
                    MasterPassword = FileTools.ReadString("settings\\MasterPassword.txt").TrimEnd(';');
                    MasterPasswordExists = true;
                }
                // File containing cellphone numbers to accept commands from, all others are ignored, splitting on + sign since we get a single string back from the file read
                if (File.Exists(@"SD\settings\Whitelist.txt"))
                {
                    Whitelist = FileTools.ReadString("settings\\Whitelist.txt").Split(';');
                    WhitelistExists = true;
                }
                // Send SMS to default number saying we are up!
                if (MasterCellExists)
                    gprs.SendSMS(MasterCell, "Remote switch controller operational at " + DateTime.Now.ToString());

                while (true)
                {
                    if (seeedStudioGSM.LastMessage > 0)
                    {
                        gprs.ReadSMS(seeedStudioGSM.LastMessage);
                        gprs.DeleteAllSMS();
                        if (File.Exists(@"SD\Temp\SMS.cmd"))
                        {
                            string ReplySMS = "";
                            string[] command = FileTools.ReadString("Temp\\SMS.cmd").Split(';');
                            File.Delete(@"SD\Temp\SMS.cmd");
                            Debug.Print ("Commands: " + command[0] + "   " + command[1]);

                            if (WhitelistExists && MasterCellExists && MasterPasswordExists) // We have a whitelist, master phone and password otherwise first message is new master phone
                            {
                                if (CheckNumberWhitelist(command[0], Whitelist)) // Make sure incoming message was sent from allowed number
                                {
                                    if (command[1].Substring(0, 8).ToUpper() == "PASSWORD" && MasterCell == command[0])
                                    {
                                        byte[] master = SHA.computeSHA1(System.Text.Encoding.UTF8.GetBytes(command[1].Trim().Substring(9))); //Encode Master password
                                        MasterPassword = ""; // Clear variable before calculating new password
                                        foreach (byte b in master)
                                        {
                                            MasterPassword = MasterPassword + b.ToString();
                                        }
                                        File.Delete(@"SD\settings\MasterPassword.txt");
                                        FileTools.New("MasterPassword.txt", "settings", MasterPassword); // Encode master password and add to whitelist
                                        gprs.SendSMS(command[0], "New password " + command[1].Substring(9) + " saved");
                                    }
                                    else
                                    {
                                        if (command[1].Trim().ToUpper().Substring(0, command[1].Trim().Length - 1) == "ALL")
                                        {
                                            requestedSwitch = 0;
                                        }
                                        else
                                        {
                                            requestedSwitch = int.Parse(command[1].Trim().ToUpper().Substring(0, command[1].Trim().Length - 1));
                                        }

                                        requestedAction = command[1].Trim().ToUpper().Substring(command[1].Trim().Length - 1, 1);

                                        if (requestedAction == "+")
                                        {
                                            if (requestedSwitch == 0) // 0 = ALL
                                            {
                                                for (int i = 0; i < NumSwitches; i++)
                                                {
                                                    Relay.On(i + 1);
#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/ (No, as it shares the same namespace, it would work there)
                                                        SWState[i] = "On  ";
#endif
                                                }

                                                for (int i = 0; i < NumXbeeSwitches; i++)
                                                {
                                                    xbee.SetDigitalOutput(XbeeAddress[i], XBeeCommand.ADio0Configuration, true);
#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/ (No, as it shares the same namespace, it would work there)
                                                        SWState[i + 4] = "On  ";
#endif
                                                }

                                                ReplySMS = DateTime.Now.ToString() + ": All switches turned On.";
                                            }
                                            else if (requestedSwitch <= 4) // Turn on numbered switch
                                            {
                                                Relay.On(requestedSwitch);
                                                ReplySMS = DateTime.Now.ToString() + ": Switch " + requestedSwitch + " was turned On";

#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/
                                                    SWState[requestedSwitch - 1] = "On  ";
#endif
                                            }
                                            else if (requestedSwitch > 4) // Turn on Xbee numbered switch
                                            {
                                                xbee.SetDigitalOutput(XbeeAddress[requestedSwitch - 5], XBeeCommand.ADio0Configuration, true);

                                                Debug.Print("Xbee: " + XbeeAddress[requestedSwitch - 5]);

                                                ReplySMS = DateTime.Now.ToString() + ": Xbee Switch " + requestedSwitch + " was turned On";

#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/
                                                    SWState[requestedSwitch - 1] = "On  ";
#endif
                                            }
                                            else
                                            {
                                                ReplySMS = DateTime.Now.ToString() + ": Error turning On Switch " + requestedSwitch;
                                            }

                                        }
                                        else if (requestedAction == "-")
                                        {
                                            if (requestedSwitch == 0)
                                            {
                                                for (int i = 0; i < NumSwitches; i++)
                                                {
                                                    Relay.Off(i + 1);
#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/
                                                        SWState[i] = "Off ";
#endif
                                                }

                                                for (int i = 0; i < NumXbeeSwitches; i++)
                                                {
                                                    xbee.SetDigitalOutput(XbeeAddress[i], XBeeCommand.ADio0Configuration, false);
#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/ (No, as it shares the same namespace, it would work there)
                                                        SWState[i + 4] = "Off ";
#endif
                                                }

                                                ReplySMS = DateTime.Now.ToString() + ": All switches turned Off.";
                                            }
                                            else if (requestedSwitch <= 4) // Turn off numbered switch
                                            {
                                                Relay.Off(requestedSwitch);
                                                ReplySMS = DateTime.Now.ToString() + ": Switch " + requestedSwitch + " was turned Off";

#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/
                                                    SWState[requestedSwitch - 1] = "Off ";
#endif
                                            }
                                            else if (requestedSwitch > 4) // Turn off Xbee numbered switch
                                            {
                                                xbee.SetDigitalOutput(XbeeAddress[requestedSwitch - 5], XBeeCommand.ADio0Configuration, false);

                                                Debug.Print("Xbee: " + XbeeAddress[requestedSwitch - 5]);

                                                ReplySMS = DateTime.Now.ToString() + ": Xbee Switch " + requestedSwitch + " was turned Off";

#if (LCD) // Would be cleaner to move all this SW?State code into relay.cs, but would need to define LCD there too :/
                                                    SWState[requestedSwitch - 1] = "Off ";
#endif
                                            }
                                            else
                                            {
                                                ReplySMS = DateTime.Now.ToString() + ": Error turning Off Switch " + requestedSwitch;
                                            }

                                        }
                                        else if (requestedAction == "?")
                                        {
                                            if (Relay.State(requestedSwitch))
                                            {
                                                ReplySMS = DateTime.Now.ToString() + ": Switch " + requestedSwitch + " is On";
                                            }
                                            else
                                            {
                                                ReplySMS = DateTime.Now.ToString() + ": Switch " + requestedSwitch + " is Off";
                                            }
                                        }
                                        else
                                        {
                                            ReplySMS = "";
                                            Debug.Print(DateTime.Now.ToString() + ": Unknown Command: " + command[1] + " from " + command[0]);
                                            gprs.SendSMS(command[0], DateTime.Now.ToString() + ": Unknown command from " + command[0] + ": " + command[1]);
                                        }
                                        if (ReplySMS.Length > 0)
                                            gprs.SendSMS(command[0], ReplySMS);
                                    }
                                }
                                else
                                {
                                    string GivenPassword = "";

                                    byte[] master = SHA.computeSHA1(System.Text.Encoding.UTF8.GetBytes(command[1])); //Encode given Master password
                                    foreach (byte b in master)
                                    {
                                        GivenPassword = GivenPassword + b.ToString();
                                    }
                                    Debug.Print("Given: " + GivenPassword + " Master: " + MasterPassword);
                                    if (GivenPassword == MasterPassword)
                                    {
                                        FileTools.Add("Whitelist.txt", "settings", command[0]); // Add new phone to whitelist
                                        Whitelist = FileTools.ReadString("settings\\Whitelist.txt").Split(';');
                                        gprs.SendSMS(command[0], "You have been added to the whitelist");
                                    }
                                    else
                                        Debug.Print(command[0] + " not in whitelist, message ignored");
                                }
                            }
                            else // No whitelist file, incoming message is from new master
                            {
                                if (command[1].IndexOf(" ") == -1) // Checks if supplied password contains a space
                                {
                                    FileTools.New("MasterCell.txt", "settings", command[0]); // Add master number
                                    MasterCell = FileTools.ReadString("settings\\MasterCell.txt").TrimEnd(';');
                                    MasterCellExists = true;

                                    FileTools.New("Whitelist.txt", "settings", command[0]); // Add new master to whitelist
                                    Whitelist = FileTools.ReadString("settings\\Whitelist.txt").Split(';');
                                    WhitelistExists = true;

                                    byte[] master = SHA.computeSHA1(System.Text.Encoding.UTF8.GetBytes(command[1])); //Encode Master password
                                    foreach (byte b in master)
                                    {
                                        MasterPassword = MasterPassword + b.ToString();
                                    }
                                    Debug.Print("Master Password: " + MasterPassword);

                                    FileTools.New("MasterPassword.txt", "settings", MasterPassword); // Add encoded password to file
                                    MasterPassword = FileTools.ReadString("settings\\MasterPassword.txt").TrimEnd(';');
                                    MasterPasswordExists = true;

                                    gprs.SendSMS(command[0], "You are the new master, password " + command[1] + " saved");
                                }
                                else
                                {
                                    gprs.SendSMS(command[0], "Passwords can not contain spaces, try again");
                                }
                            }
                        }
                    }
                    #if (LCD)
                        lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("SW1:" + SWState[0] + "SW2:" + SWState[1]);
                        lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("SW3:" + SWState[2] + "SW4:" + SWState[3]);
                    #endif
                    Thread.Sleep(1000);
                }
            }
            public static Boolean CheckNumberWhitelist(string CellNumber, string[] Whitelist)
            {
                Debug.Print("Incoming Number: " + CellNumber);
                for (int j = 0; j < Whitelist.Length; j++)
                {
                    Debug.Print("Whitelist Number: " + Whitelist[j]);
                    if (CellNumber == Whitelist[j])
                    {
                        return true;
                    }
                }
                return false;
            }
            static void ModemInitTimer(object state)
            {
                // If we don't detect "Call Ready" from the modem, set the baud rate to the correct setting
                seeedStudioGSM gprs = (seeedStudioGSM)state;
                gprs.SIM900_SetBaudRate(gsmbaudrate);
                InitTimeout = true;
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
                private static void DisplayIP() // Display IP address on LCD for 10 seconds.
                {
                    var IPAddress = "";
                    IPAddress = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress;
                    lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("  IP Address:");
                    lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes(IPAddress);
                    LCDSleep = 10000; // This will tell the LCD thread to sleep for 10 seconds so the IP address can be displayed
                }
                private static void button_OnInterrupt(uint port, uint data, DateTime time)
                {
                    DisplayIP();
                }
            #endif

            private static void RequestReceived(Request request)
            {
                string html = "";
                int i = 0;
                Debug.Print("Request from " + request.Client.ToString() + " received at " + DateTime.Now.ToString() + ". Method: " + request.Method + " URL: " + request.URL);

                if (request.URL == "/" || request.URL == "/index.html") // Redirect to /switches
                {
                    request.SendResponse(@" <html xmlns=""http://www.w3.org/1999/xhtml"">    
                        <head>      
                            <title>Switches</title>      
                            <meta http-equiv=""refresh"" content=""0;URL='/switches'"" />    
                        </head>    
                        <body> 
                            <p>Redirecting to <a href=""/switches"">
                            switches</a>.</p> 
                        </body>  
                    </html>");
                }
                else if (request.URL.Substring(0, 9) == "/switches")
                {
                    if (request.URL.Length > 9)
                    {
                        string[] parameters = request.URL.Substring(request.URL.Length - 3, 3).Split('=');
                        if (int.Parse(parameters[1]) == 1)
                        {
                            if (Relay.On(int.Parse(parameters[0])))
                            {
                                SWState[int.Parse(parameters[0]) - 1] = "On  ";
                            }

                        }
                        else if (int.Parse(parameters[1]) == 0)
                        {
                            if (Relay.Off(int.Parse(parameters[0])))
                            {
                                SWState[int.Parse(parameters[0]) - 1] = "Off ";
                            }
                        }
                        #if (LCD)
                            lcdMessageLine1 = System.Text.Encoding.UTF8.GetBytes("SW1:" + SWState[0] + "SW2:" + SWState[1]);
                            lcdMessageLine2 = System.Text.Encoding.UTF8.GetBytes("SW3:" + SWState[2] + "SW4:" + SWState[3]);
                        #endif
                    }

                    html = "<head><h3><b>Cellular Remote Manual Mode</b></h3></head><body><p>";

                    for (i = 0; i < NumSwitches; i++)
                    {
                        html = html + "<p>Switch " + ((i + 1) / 10 >> 0) + ((i + 1) % 10) + ": <button style=\"width:100;height:28;background-color:";
                        if (SWState[i] == "On  ")
                        {
                            html = html + "lightgreen\" onclick=\"window.location.href='/switches?" + (i + 1) + "=0'\">Turn Off";
                        }
                        else
                        {
                            html = html + "lightgray\" onclick=\"window.location.href='/switches?" + (i + 1) + "=1'\">Turn On";
                        }
                        html = html + "</button></p></body></html>";
                    }
                    request.SendResponse(html);
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
