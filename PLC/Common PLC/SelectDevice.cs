using ITM_Semiconductor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using VisionInspection;

namespace Development
{
    
   
    public class SelectDevice
    {
      
        public Device device;
        private MyLogger logger = new MyLogger("Select Device");

        

 
        private int limitDeviceBits;
        private int limitDeviceWord;


        public SelectDevice (SaveDevice device, SettingDevice settingDevice)
        {
            
            if (SaveDevice.Mitsubishi_MC_Protocol_Binary_TCP == device)
            {
                this.limitDeviceWord = 900;
                this.limitDeviceBits = 2000;
                this.device = new ServiceTCPMCProtocolBinary(settingDevice.MC_TCP_Binary);
            }
            if (SaveDevice.Mitsubishi_RS422_SC09 == device)
            {
                this.limitDeviceBits = 100;
                this.limitDeviceWord = 100;
                this.device = new ServiceRs422Fx(settingDevice.sc09Setting);
            }
            if (SaveDevice.LS_XGTServer_TCP == device)
            {
                this.limitDeviceBits = 500;
                this.limitDeviceWord = 500;
                this.device = new ServiceXGTServerTCP(settingDevice.LSXGTServerTCPSetting);
            }
            if (SaveDevice.LS_XGTServer_COM == device)
            {
                this.limitDeviceBits = 300;
                this.limitDeviceWord = 120;
                this.device = new ServiceLSXGTServerCOM(settingDevice.XGTServerCOMSetting);
            }
        }

      
    }
    public class DeviceItem
    {
        public string type { get; set; }
        public ushort address { get; set; }
    }
}
