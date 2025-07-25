using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using System.Runtime.InteropServices.WindowsRuntime;

using System.Net;
using System.Net.NetworkInformation;

using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace VisionInspection
{

    
    class PingIP
    {
        private static MyLogger logger = new MyLogger("Ping IP PLC");
       
        public int LocalPing()
        {
            int ret = 2;
            // Ping's the local machine.
            Ping pingSender = new Ping();
            IPAddress address = IPAddress.Loopback;
            //PingReply reply = pingSender.Send(address);
            PingReply reply = pingSender.Send("192.168.3.39", 1015);
            if (reply.Status == IPStatus.Success)
            {
                logger.Create("PLC Connect Success");
                ret = 1;
            }
            else
            {
                logger.Create("PLC Connect Lost");
                ret = 2;
            }
            return ret;
        }


    }
}
