using System.IO.Ports;
using System.Threading;

namespace seeedStudio.Grove.SerialLCD
{
    class LCD
    {
        private SerialPort _lcd;

        private byte[] SLCD_INIT = { 0xA3 };
        private byte[] SLCD_INIT_ACK = { 0xA5 };
        private byte[] SLCD_INIT_DONE = { 0xAA };

        //WorkingMode Commands or Responses
        private byte[] SLCD_CONTROL_HEADER = { 0x9F };
        private byte[] SLCD_CHAR_HEADER = { 0xFE };
        private byte[] SLCD_CURSOR_HEADER = { 0xFF };
        private byte[] SLCD_CURSOR_ACK = { 0x5A };

        private byte[] SLCD_RETURN_HOME = { 0x61 };
        private byte[] SLCD_DISPLAY_OFF = { 0x63 };
        private byte[] SLCD_DISPLAY_ON = { 0x64 };
        private byte[] SLCD_CLEAR_DISPLAY = { 0x65 };
        private byte[] SLCD_CURSOR_OFF = { 0x66 };
        private byte[] SLCD_CURSOR_ON = { 0x67 };
        private byte[] SLCD_BLINK_OFF = { 0x68 };
        private byte[] SLCD_BLINK_ON = { 0x69 };
        private byte[] SLCD_SCROLL_LEFT = { 0x6C };
        private byte[] SLCD_SCROLL_RIGHT = { 0x72 };
        private byte[] SLCD_NO_AUTO_SCROLL = { 0x6A };
        private byte[] SLCD_AUTO_SCROLL = { 0x6D };
        private byte[] SLCD_LEFT_TO_RIGHT = { 0x70 };
        private byte[] SLCD_RIGHT_TO_LEFT = { 0x71 };
        private byte[] SLCD_POWER_ON = { 0x83 };
        private byte[] SLCD_POWER_OFF = { 0x82 };
        private byte[] SLCD_INVALIDCOMMAND = { 0x46 };
        private byte[] SLCD_BACKLIGHT_ON = { 0x81 };
        private byte[] SLCD_BACKLIGHT_OFF = { 0x80 };
        
        
        public LCD(string com)
        { 
            _lcd = new SerialPort(com, 9600, Parity.None, 8, StopBits.One);
            _lcd.Open();
            Thread.Sleep(2);
            _lcd.Write(SLCD_CONTROL_HEADER,0 ,1);
            _lcd.Write(SLCD_POWER_OFF, 0, 1);
            Thread.Sleep(2);
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_POWER_ON, 0, 1);
            Thread.Sleep(2);
            _lcd.Write(SLCD_INIT_ACK, 0, 1);
        }

        public void clear()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0 , 1);
            Thread.Sleep(5);
            _lcd.Write(SLCD_CLEAR_DISPLAY, 0, 1);
            Thread.Sleep(5);
        }
        // Return to home(top-left corner of LCD)
        public void home()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0 , 1);
            _lcd.Write(SLCD_RETURN_HOME, 0, 1);
        }
        // Set Cursor to (Column,Row) Position
        public void setCursor(byte column, byte row)
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            Thread.Sleep(5);
            _lcd.Write(SLCD_CURSOR_HEADER, 0 , 1); //cursor header command
            Thread.Sleep(5);
            byte[] coords = new byte[] { column, row };
            _lcd.Write(coords, 0, 1);
            _lcd.Write(coords, 1, 1);
        }

        // Switch the display off without clearing RAM
        public void displayOff() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_DISPLAY_OFF, 0, 1);
        }      

        // Switch the display on
        public void displayOn()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_DISPLAY_ON, 0, 1);
        }

        // Switch the underline cursor off
        public void cursorOff() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_CURSOR_OFF, 0, 1);
        }

        // Switch the underline cursor on
        public void cursorOn() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            Thread.Sleep(10);
            _lcd.Write(SLCD_CURSOR_ON, 0, 1);
            Thread.Sleep(10);
        }

        // Switch off the blinking cursor
        public void blinkOff() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_BLINK_OFF, 0, 1);
        }

        // Switch on the blinking cursor
        public void blinkOn() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_BLINK_ON, 0, 1);
        }

        // Scroll the display left without changing the RAM
        public void scrollDisplayLeft() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_SCROLL_LEFT, 0, 1);
        }

        // Scroll the display right without changing the RAM
        public void scrollDisplayRight() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_SCROLL_RIGHT, 0, 1);
        }

        // Set the text flow "Left to Right"
        public void leftToRight() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_LEFT_TO_RIGHT, 0, 1);
        }

        // Set the text flow "Right to Left"
        public void rightToLeft() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_RIGHT_TO_LEFT, 0, 1);
        }

        // This will 'right justify' text from the cursor
        public void autoscrollOn() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_AUTO_SCROLL, 0, 1);
        }

        // This will 'left justify' text from the cursor
        public void autoscrollOff() 
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_NO_AUTO_SCROLL, 0, 1);
        }
        public void powerOn()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_POWER_ON, 0, 1);
        }
        public void powerOff()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_POWER_OFF, 0, 1);
        }
        //Turn off the backlight
        public void backlightOff()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_BACKLIGHT_OFF, 0, 1);
        }
        //Turn on the back light
        public void backlightOn()
        {
            _lcd.Write(SLCD_CONTROL_HEADER, 0, 1);
            _lcd.Write(SLCD_BACKLIGHT_ON, 0, 1);
        }
        // Print Command
        public void print(byte[] b)
        {
            _lcd.DiscardInBuffer();
            _lcd.Write(SLCD_CHAR_HEADER, 0, 1);
            Thread.Sleep(5);
            _lcd.Write(b, 0, b.Length);
            Thread.Sleep(5);
        }
    }
}