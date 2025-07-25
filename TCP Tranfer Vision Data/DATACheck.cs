using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionTCPClient
{
     public class DATACheck
    {
        public string EquipmentId { get; set; }
        public string Status { get; set; }
        public string CheckSum { get; set; }
        public VisionFormat FormatVision { get; set; }

        public DATACheck() 
        {
            this.EquipmentId = "AutoVision";
            this.Status = "AUTO01";
            this.CheckSum = "END";
            this.FormatVision = new VisionFormat();
        }
    }
}
