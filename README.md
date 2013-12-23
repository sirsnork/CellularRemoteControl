CellularRemoteControl
=====================

Netduino code to switch relays based on incoming SMS messages

All code based upon Netduino forums user's Jair code posted here
http://forums.netduino.com/index.php?/topic/8433-remote-control-heating/

Instructions:

Set GPRS Sheild to "SoftwareSerial". This sets it to use D7 and D8 as it's serial port, which corresponds to COM3 on the Netduino 2. This leaves the traditional D0/D1 pins available for shields that only supposrt Arduino's COM1 setup

You must install an SD card with the following folder/file structure

```
Root
  |
  | - settings - | - NumCellDefault.txt : Contains the default number to use to send system messages (system bootup etc)
  |              | - Whitelist.txt      : Contains list of full international numbers that the system will accept commands from
  |
  | - temp                              : Used to temporarily store commands being actioned
  | - ReceivedSMS                       : Contains every SMS recieved with date/time stamp as the filename.
```  
Current commands are:

* SW1_ON      : Turns on D5 Pin
* SW1_OFF     : Turns off D5 Pin
* SW1_STATE   : Returns current state of D5 Pin
* SW2_ON      : Turns on D6 Pin
* SW2_OFF     : Turns off D6 Pin
* SW2_STATE   : Returns current state of D6 Pin

