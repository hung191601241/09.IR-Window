using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLaserCuttingInput
{
    public class DataCheckPKG
    {
        #region Common
        public string RECIPE { get; set; } = "";
        public string CONFIG { get; set; } = "";
        public string LOT_NUMBER { get; set; } = "";
        #endregion

        #region Vision
        public DateTime TimePKG_Check_Vision { get; set; } = DateTime.Now;
        public bool ResultVision { get; set; } = false;
        public string ResultVision_String { get; set; } = "Wait";
        #endregion

        #region Scanner
        public String QrCodePKG { get; set; } = "0";
        public DateTime TimePKG_Read_QRCode { get; set; } = DateTime.Now;
        #endregion

        #region Status
        public string OnlineChecking { get; set; }
        #endregion

        #region MES
        public bool ResultMES { get; set; } = false;
        public String MESFeedback { get; set; } = "";
        #endregion
    }
}
