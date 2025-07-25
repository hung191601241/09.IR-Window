using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Development
{
    public class TCPSetting
    {

        public string Ip { get; set; }
        public int Port { get; set; }
        public TCPSetting()
        {
            this.Ip = "127.0.0.1";
            this.Port = 6000;
        }
    }
}
