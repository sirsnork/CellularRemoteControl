using System;
using System.IO.Ports;
using System.Text;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Threading;

namespace CellularRemoteControl
{
    class seedStudioGSM
    {
        static string awatingResponseString = "";
        static bool awaitingResponse = false;
        static bool connected = false;

        static SerialPort serialPort;
        const int bufferMax = 1024;
        static byte[] buffer = new Byte[bufferMax];
        static StringBuilder output = new StringBuilder();

        public static int LastMessage=0;
        public static int SignalStrength = 0;

        //static int bufferLength = 0;
        public seedStudioGSM(string portName = "COM1", int baudRate = 19200, Parity parity = Parity.Odd, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            try
            {
                serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                serialPort.ReadTimeout = -1;
                serialPort.WriteTimeout = 10;
                serialPort.Handshake = Handshake.XOnXOff;
                serialPort.Open();
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
            }
            catch
            {
            }
        }

        static byte[] readbuff(byte[] inputBytes)
        {
            for (int i = 0; i < inputBytes.Length; i++)
            {
                if (inputBytes[i] == 230)
                {
                    inputBytes[i] = 32;
                }
            }
            return inputBytes;
        }
        static void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Check if Chars are received
            if (e.EventType == SerialData.Chars)
            {
                // Create new buffer
                byte[] ReadBuffer = new byte[serialPort.BytesToRead];
                // Read bytes from buffer
                serialPort.Read(ReadBuffer, 0, ReadBuffer.Length);


                if (ReadBuffer.Length > 0 && (ReadBuffer[ReadBuffer.Length - 1] == 10 || ReadBuffer[ReadBuffer.Length - 1] == 0))
                {
                    // New line or terminated string.
                    output.Append(GetUTF8StringFrombytes(ReadBuffer));

                    if (!awatingResponseString.Equals("") && output.ToString().IndexOf(awatingResponseString) > -1)
                    {
                        Debug.Print("Response Matched : " + output.ToString());
                        awatingResponseString = "";
                    }
                    else
                    {
                        Debug.Print(output.ToString());
                        if (output.ToString().IndexOf("+CMTI: \"SM\"") > -1)
                        {
                            Program._led_NewMessage .Write(true);
                            Debug.Print("New Message received.");
                            string[] sCMTI = output.ToString().Split(',');
                            LastMessage = int.Parse (FileTools.strMID(sCMTI[1],0,sCMTI[1].Length-2));
                            Thread.Sleep(1000);
                            Program._led_NewMessage.Write(false);
                        }

                        if (output.ToString().IndexOf("CONNECT OK") > -1)
                        {
                            connected = true;
                            Debug.Print("CONNECTED!");
                        }

                        if (output.ToString().IndexOf("+CMGR:") > -1)
                        {
                            Debug.Print("Read Message");
                            RegisterSMS(output.ToString());
                        }

                        if (output.ToString().IndexOf("+CSQ:") > -1)
                        {
                            Debug.Print("Signal Quality (value)");
                            string[] sSignalStrength = output.ToString().Split(',');
                            SignalStrength = int.Parse(FileTools.strMID(sSignalStrength[0], sSignalStrength[0].IndexOf(": ") + 2, sSignalStrength[0].Length));
                        }
                        if (output.ToString().IndexOf("+CCLK:") > -1)
                        {
                            string[] sTime = output.ToString().Split(',');
                            SetDateTime(sTime[0] + "\",\"" + sTime[1]);
                        }
                    }
                    output.Clear();
                    awaitingResponse = false;
                }
                else
                {
                    try
                    {
                        //output.Append(UTF8Encoding.UTF8.GetChars(readbuff(ReadBuffer)));
                        output.Append(GetUTF8StringFrombytes(ReadBuffer));
                        //UTF8Encoding.UTF8.GetChars(
                    }
                    catch (Exception ecx)
                    {
                        Debug.Print("Cannot parse : " + ecx.StackTrace);
                    }
                }
            }
        }

        private void Print(string line)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            byte[] bytesToSend = encoder.GetBytes(line);
            serialPort.Write(bytesToSend, 0, bytesToSend.Length);
        }
        public  void PrintLine(string line, bool awaitResponse = false, string awaitResponseString = "")
        {
            if (!awaitResponseString.Equals(""))
            {
                awatingResponseString = awaitResponseString;
                while (!awatingResponseString.Equals(""))
                {
                    Thread.Sleep(1000);
                }
            }
            Print(line + "\r");
            if (awaitResponse)
            {
                awaitingResponse = true;
                while (awaitingResponse)
                {
                    Thread.Sleep(100);
                }
            }
        }
        private void PrintEnd()
        {
            byte[] bytesToSend = new byte[1];
            bytesToSend[0] = 26;
            serialPort.Write(bytesToSend, 0, 1);
            Thread.Sleep(200);
        }        

        public void postRequest(string apn, string gatewayip, string host, string page, string port, string parameters)
        {
            //setup_start:
            PrintEnd();
            serialPort.Flush();
            PrintLine("ATE1", true);
            PrintLine("AT+CIPMUX=0", true); //We only want a single IP Connection at a time.
            PrintLine("AT+CIPMODE=0", true); //Selecting "Normal Mode" and NOT "Transparent Mode" as the TCP/IP Application Mode
            PrintLine("AT+CGDCONT=1,\"IP\",\"" + apn + "\",\"" + gatewayip + "\",0,0", true); //Defining the Packet Data
            //Protocol Context - i.e. the Protocol Type, Access Point Name and IP Address
            PrintLine("AT+CSTT=\"" + apn + "\"", true);//Start Task and set Access Point Name (and username and password if any)
            PrintLine("AT+CIPSHUT", true); //Close any GPRS Connection if open
            PrintLine("AT+CIPSTART=\"TCP\",\"" + host + "\",\"" + port + "\"", false);
            Thread.Sleep(300);

            while (connected == false)
            {
                Thread.Sleep(1000);
            }
            connected = false;
            //Thread.Sleep(10000);// Wait until CONNECT OK is recieved can be up to 8 secs (should make something smarter)
            serialPort.Flush();

            PrintLine("AT+CIPSEND");//Start data through TCP connection
            serialPort.Flush();
            Thread.Sleep(1000);
            StringBuilder POST = new StringBuilder();
            POST.Append(parameters);
            Print("POST " + page + " HTTP/1.1\r\nHost: " + host + "\r\nContent-Length: " + POST.Length.ToString() + "\r\n\r\n" + POST.ToString());
            PrintEnd();
            Thread.Sleep(5000);
            PrintLine("AT+CIPACK", true);
            Thread.Sleep(300);
            PrintLine("AT+CIPSHUT", true); //Close any GPRS Connection if open
            Thread.Sleep(1000000);
        }
        public void getRequest(string apn, string gatewayip, string host, string page, string port)
        {
            //setup_start:
            PrintEnd();
            serialPort.Flush();
            PrintLine("ATE1", true);
            PrintLine("AT+CIPMUX=0", true); //We only want a single IP Connection at a time.
            PrintLine("AT+CIPMODE=0", true); //Selecting "Normal Mode" and NOT "Transparent Mode" as the TCP/IP Application Mode
            PrintLine("AT+CGDCONT=1,\"IP\",\"" + apn + "\",\"" + gatewayip + "\",0,0", true); //Defining the Packet Data
            //Protocol Context - i.e. the Protocol Type, Access Point Name and IP Address
            PrintLine("AT+CSTT=\"" + apn + "\"", true);//Start Task and set Access Point Name (and username and password if any)
            PrintLine("AT+CIPSHUT", true); //Close any GPRS Connection if open
            PrintLine("AT+CIPSTART=\"TCP\",\"" + host + "\",\"" + port + "\"", false);
            Thread.Sleep(300);
            Thread.Sleep(10000);// Wait until CONNECT OK is recieved can be up to 8 secs (should make something smarter)
            serialPort.Flush();
            PrintLine("AT+CIPSEND");//Start data through TCP connection
            serialPort.Flush();
            Thread.Sleep(1000);
            StringBuilder POST = new StringBuilder();
            POST.Append("channel=1&rnd=329999932324&colorCode=33;120;255");
            //Print("POST " + page + " HTTP/1.1\r\nHost: " + host + "\r\nContent-Length: " + POST.Length.ToString() + "\r\n\r\n" + POST.ToString());
            Print("GET " + page + " HTTP/1.1\\r\\n");
            Thread.Sleep(300);
            Print("Host: " + host + "\\r\\n");
            Thread.Sleep(300);
            Print("Connection: close\\r\\n");
            Thread.Sleep(300);
            PrintEnd();
            Thread.Sleep(5000);
            PrintLine("AT+CIPACK", true);
            Thread.Sleep(300);
            PrintLine("AT+CIPSHUT", true); //Close any GPRS Connection if open
            Thread.Sleep(1000);
        }

        public void InitializeSMS()
        {
            Debug.Print("SMS PDU mode (0) o Text (1)");
            PrintLine("AT+CMGF=1", true);
            Thread.Sleep(100);
            Debug.Print("SMS Storage SIM");
            PrintLine("AT+CPMS=\"SM\"", true);
            Thread.Sleep(100);
            PrintEnd();
            Thread.Sleep(500);
        }

        public void SendSMS(string msisdn, string message)
        {
            try
            {
                //PrintLine("");
                PrintLine("AT+CMGF=1", true);
                PrintLine("AT+CMGS=\"" + msisdn + "\"", false);
                Thread.Sleep(100);
                PrintLine(message);
                Thread.Sleep(100);
                PrintEnd();
                Thread.Sleep(500);
                //Debug.Print("SMS Sent!");
            }
            catch (Exception ecx)
            {
                Debug.Print("SendSMS : " + ecx.Message.ToString());
            }
        }
        
        public void ReadSMS(int indexSMS)
        {
            try
            {
                Debug.Print("SMS Read");
                PrintLine("AT+CMGR=" + indexSMS.ToString(), true);
                Thread.Sleep(100);
                PrintEnd();
                Thread.Sleep(500);
            }
            catch (Exception ecx)
            {
                Debug.Print("ReadSMS : " + ecx.Message.ToString());
            }
        }

        public void DeleteAllSMS()
        {
            try
            {
                Debug.Print("SMS All delete");
                PrintLine("AT+CMGD=0,4", true);
                Thread.Sleep(100);
                PrintEnd();
                Thread.Sleep(500);
                LastMessage = 0;
            }
            catch (Exception ecx)
            {
                Debug.Print("DeleteAllSMS : " + ecx.Message.ToString());
            }

        }

        public void SIM900_FirmwareVersion()
        {
            try
            {
                Debug.Print("Firmware Version");
                PrintLine("AT+GSV", true);
                Thread.Sleep(100);
                PrintEnd();
                Thread.Sleep(500);
            }
            catch (Exception ecx)
            {
                Debug.Print("SIM900_FirmwareVersion : " + ecx.Message.ToString());
            }
        }

        public void SIM900_SignalQuality()
        {
            try
            {
                Debug.Print("Signal Quality");
                PrintLine("AT+CSQ", true);
                Thread.Sleep(100);
                PrintEnd();
                Thread.Sleep(500);
            }
            catch (Exception ecx)
            {
                Debug.Print("SIM900_SignalQuality : " + ecx.Message.ToString());
            }
        }
        public void SIM900_GetTime()
        {
            try
            {
                Debug.Print("Get Network Time");
                PrintLine("AT+CCLK?", true);
                Thread.Sleep(100);
                PrintEnd();
                Thread.Sleep(500);
            }
            catch (Exception ecx)
            {
                Debug.Print("SIM900_GetTime : " + ecx.Message.ToString());
            }
        }

        public void placeCall(string msisdn)
        {
            PrintLine("ATD" + msisdn);
            Thread.Sleep(100);
            Debug.Print("Calling....." + msisdn);
        }

        private static string GetUTF8StringFrombytes(byte[] byteVal)
        {
            byte[] btOne = new byte[1];
            StringBuilder sb = new StringBuilder("");
            char uniChar;
            for (int i = 0; i < byteVal.Length; i++)
            {
                btOne[0] = byteVal[i];
                if (btOne[0] > 127)
                {
                    uniChar = Convert.ToChar(btOne[0]);
                    sb.Append(uniChar);
                }
                else
                    sb.Append(new string(Encoding.UTF8.GetChars(btOne)));
            }
            return sb.ToString();
        }

        private static void RegisterSMS(string Message)
        {
            string Cellular="";
            string Data = "";
            string Ora = "";

            string[] tmpOutputStr1 = Message.Split(',');
            string[] tmpOutputStr2 = tmpOutputStr1[0].Split(':');
            
            string[] lines = tmpOutputStr1[11].Split('\n');
            for (int i = 0; i < lines.Length; i += 1)
                lines[i] = lines[i].Trim();
                
             
            Cellular = FileTools.Replace(tmpOutputStr1[1].ToString(), "\"", "");
            Debug.Print("Sender: " + Cellular);
                    
            Data = FileTools.Replace(tmpOutputStr1[3].ToString(), "\"", "");
            Data = FileTools.Replace(Data, "/", "");
           
            Ora = FileTools.Replace(tmpOutputStr1[4].ToString(), "\"", "");
                
            char[] chrOra = Ora.ToCharArray();
                
            Ora = chrOra[0].ToString() + chrOra[1].ToString() + chrOra[3].ToString() + chrOra[4].ToString() + chrOra[6].ToString() + chrOra[7].ToString();

            Debug.Print("Received : " + Data + " to " + Ora);

            FileTools.New(Data + Ora + ".sms", "SMS Received", lines[1]);
            Debug.Print("Messageo saved to SD.");

            FileTools.New("SMS.cmd", "", Cellular+";"+lines[1]);
            Debug.Print("Command saved to SD for test.");
        }
        // set the system's date and time from a string in this format:  "+CCLK: yy/MM/dd,hh:mm:ss+zz" maybe, not sure if the +CCLK will be here
        public static void SetDateTime(string dts)
        {
            var year = Str2Int(dts, 0);      // convert each of the numbers
            var month = Str2Int(dts, 3);
            var day = Str2Int(dts, 6);
            var hour = Str2Int(dts, 9);
            var minute = Str2Int(dts, 11);
            var second = Str2Int(dts, 13);
            System.DateTime dt = new System.DateTime(year, month, day, hour, minute, second);
            Debug.Print("Date: " + dt);
            Microsoft.SPOT.Hardware.Utility.SetLocalTime(dt);
        }

        // convert a string to an "int"  stops at the end-of-string, or at the first non-digit found
        public static int Str2Int(string input, int offset)
        {
            int ret = 0;   // built the result here
            for (int i = offset; i < input.Length; i++)  // stop when all chars have been processed
            {
                char c = input[i];      // get the next char
                if (c < '0') break;     // stop if a non-number is found
                if (c > '9') break;
                int n = (int)c - 48;    // convert the ascii value to a number, IE '1' = 49
                ret = n + 10 * ret;     // accumulate the result
            }
            return ret;   // return the result to caller
        }
    }
}