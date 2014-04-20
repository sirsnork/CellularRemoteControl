CellularRemoteControl
=====================

Netduino code to switch relays based on incoming SMS messages or from a web browser.

Incorporated code from Netduino forums user's Jair code posted here
http://forums.netduino.com/index.php?/topic/8433-remote-control-heating/

Instructions:

"#define" or "#undef" the modules you need a the top of Program.cs. You can include or exclude LCD, CELL, XBEE and WEB if you don't have some of those, however you need either CELL or WEB enabled

Set GPRS Sheild to "HardwareSerial/Arduino". This sets it to use D0 and D1 as it's serial port, which corresponds to COM1 on the Netduino 2. 
The LCD Connects to COM2 (if enabled), or D2/D3 leaving D4-D7 free for the SeeedStudion Relay Sheild V2
Connect a push-button microswitch to D8. Pressing this will display the current IP address on the LCD for 10 seconds once booted.

Xbee code is still in development, eventually you will be able to have Xbee radios connected to relays to remote switch sockets.

Setup:

When first powering on the system, let it complete bootup and then send it a SMS message from the master phone, the content of the message should be the password you want to set. The password is used to whitelist other phones. To whitelist another phone send a SMS message with the password as the only content. Messages from phones that aren't whitelisted will be ignored.

If you ever want to change the password send a SMS message containing the following "password <password>". That is, the word password, a space followed by the new password. This can only be done from the master phone.

The password is SHA1 hashed and stored on the SD card, it is not stored anywhere in cleartext


The follwoing file structure will be created on the SD card

```
Root
  |
  | - settings                          : Used to store configuration files
  | - temp                              : Used to temporarily store commands being actioned
  | - ReceivedSMS                       : Contains every SMS recieved with date/time stamp as the filename.
  | - Web                               : Contains any static web pages you want the web server to use
  
```  
Current commands are:

* 1+    : Turns on D7 Pin
* 1-    : Turns off D7 Pin
* 1?    : Returns current state of D4 Pin
* 2+    : Turns on D6 Pin
* 2-    : Turns off D6 Pin
* 2?    : Returns current state of D5 Pin
* 3+    : Turns on D5 Pin
* 3-    : Turns off D5 Pin
* 3?    : Returns current state of D6 Pin
* 4+    : Turns on D4 Pin
* 4-    : Turns off D4 Pin
* 4?    : Returns current state of D7 Pin
* All+  : Turns on ALL Pins
* All-  : Turns off ALL Pins
