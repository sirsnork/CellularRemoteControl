CellularRemoteControl
=====================

Netduino code to switch relays based on incoming SMS messages

All code based upon Netduino forums user's Jair code posted here
http://forums.netduino.com/index.php?/topic/8433-remote-control-heating/

Instructions:

Set GPRS Sheild to "HardwareSerial/Arduino". This sets it to use D0 and D1 as it's serial port, which corresponds to COM1 on the Netduino 2. The LCD Connects to COM2, or D2/D3 leaving D4-D7 free for the SeeedStudion Relay Shiled V2

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

* SW1_ON      : Turns on D4 Pin
* SW1_OFF     : Turns off D4 Pin
* SW1_STATE   : Returns current state of D4 Pin
* SW2_ON      : Turns on D5 Pin
* SW2_OFF     : Turns off D5 Pin
* SW2_STATE   : Returns current state of D5 Pin
* SW3_ON      : Turns on D6 Pin
* SW3_OFF     : Turns off D6 Pin
* SW3_STATE   : Returns current state of D6 Pin
* SW4_ON      : Turns on D7 Pin
* SW4_OFF     : Turns off D7 Pin
* SW4_STATE   : Returns current state of D7 Pin
