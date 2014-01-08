CellularRemoteControl
=====================

Netduino code to switch relays based on incoming SMS messages or from a web browser.

Incorperated code from Netduino forums user's Jair code posted here
http://forums.netduino.com/index.php?/topic/8433-remote-control-heating/

Instructions:

"#define" or "#undef" the modules you need a the top of Program.cs. You can include or exclude LCD, CELL and WEB if you don't have some of those, however you need either CELL or WEB enabled

Set GPRS Sheild to "HardwareSerial/Arduino". This sets it to use D0 and D1 as it's serial port, which corresponds to COM1 on the Netduino 2. 
The LCD Connects to COM2 (if enabled), or D2/D3 leaving D4-D7 free for the SeeedStudion Relay Sheild V2
Connect a push-button microswitch to D8. Pressing this will display the current IP address on the LCD for 10 seconds once booted.


You must install an SD card with the following folder/file structure

```
Root
  |
  | - settings - | - NumCellDefault.txt : Contains the default number to use to send system messages (system bootup etc)
  |              | - Whitelist.txt      : Contains list of full international numbers that the system will accept commands from
  |
  | - temp                              : Used to temporarily store commands being actioned
  | - ReceivedSMS                       : Contains every SMS recieved with date/time stamp as the filename.
  | - Web                               : Contains any static web pages you want the web server to use
  
```  
Current commands are:

* 1+    : Turns on D4 Pin
* 1-    : Turns off D4 Pin
* 1?    : Returns current state of D4 Pin
* 2+    : Turns on D5 Pin
* 2-    : Turns off D5 Pin
* 2?    : Returns current state of D5 Pin
* 3+    : Turns on D6 Pin
* 3-    : Turns off D6 Pin
* 3?    : Returns current state of D6 Pin
* 4+    : Turns on D7 Pin
* 4-    : Turns off D7 Pin
* 4?    : Returns current state of D7 Pin
