using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Development
{
    public class COMSetting
    {
        public String portName { get; set; }
        public Int32 baudrate { get; set; }
        public Int32 dataBits { get; set; }
        public StopBits stopBits { get; set; }
        public Parity parity { get; set; }
        public Handshake Handshake { get; set; }
        public COMSetting()
        {
            this.portName = "COM1";
            this.baudrate = 9600;
            this.dataBits = 8;
            this.stopBits = StopBits.One;
            this.parity = Parity.None;
            this.Handshake = Handshake.None;
        }
        public static StopBits ParseStopBits(string s)
        {
            if (s == null)
            {
                return StopBits.None;
            }
            if (s.Equals("One"))
            {
                return StopBits.One;
            }
            else if (s.Equals("Two"))
            {
                return StopBits.Two;
            }
            return StopBits.None;
        }

        public static Parity ParseParity(string s)
        {
            if (s == null)
            {
                return Parity.None;
            }
            if (s.Equals("Even"))
            {
                return Parity.Even;
            }
            else if (s.Equals("Odd"))
            {
                return Parity.Odd;
            }
            else if (s.Equals("Mark"))
            {
                return Parity.Mark;
            }
            else if (s.Equals("Space"))
            {
                return Parity.Space;
            }
            return Parity.None;
        }
    }
}
