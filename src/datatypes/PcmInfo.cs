using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionInspection
{
    internal class PcmInfo
    {
        public const int PCM_NG = 0;
        public const int PCM_OK = 1;
        public const int PCM_EM = 2;
        public const int PCM_OF = 3;

        public int id { get; set; }
        public string lotId { get; set; }
        public string qr { get; set; }
        public int result { get; set; }
        public DateTime updatedTime { get; set; }

        public PcmInfo()
        {
        }

        public PcmInfo(string lotId, string pcmQr, int result)
        {
            this.id = 0;
            this.lotId = lotId;
            this.qr = pcmQr;
            this.result = result;
            this.updatedTime = DateTime.Now;
        }
    }
}