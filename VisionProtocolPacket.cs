using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLaserCuttingInput
{
    public class VisionProtocolPacket
    {
        public bool ResultCH1 { get; set; } = false;
        public string ResultCH1_Vision { get; set; } = "NG Name Error CH1";
        public bool ResultCH2 { get; set; } = false;
        public string ResultCH2_Vision { get; set; } = "NG Name Error CH2";

        public VisionProtocolPacket Clone()
        {
            return new VisionProtocolPacket
            {
                ResultCH1 = this.ResultCH1,
                ResultCH1_Vision = string.Copy(this.ResultCH1_Vision),
                ResultCH2 = this.ResultCH2,
                ResultCH2_Vision = string.Copy(this.ResultCH2_Vision),
            };
        }
    }
}
