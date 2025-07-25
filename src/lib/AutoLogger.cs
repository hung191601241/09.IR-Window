using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VisionInspection
{
    class AutoLogger
    {
        private static Object autoLocker = new Object();

        private String productId = "";

        public AutoLogger(String productId)
        {
            this.productId = productId;
        }

        public void CreateMesLog(String pcmQr, String Position, String Result, String lotId)
        {
            lock (autoLocker)
            {
                try
                {
                    // Check file existing:                    
                    var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LotCounterData");
                    folder = Path.Combine(folder, DateTime.Today.ToString("yyyy-MM-dd"), lotId);
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    var fileName = String.Format("[{0}]-MES.csv", this.productId);
                    var filePath = Path.Combine(folder, fileName);

                    // Create Headers if file not existed:
                    if (!File.Exists(filePath))
                    {
                        using (var strWriter = new StreamWriter(filePath, false))
                        {
                            var header = "LOT, QRCODE, POSITION, RESULT, TIME";
                            strWriter.WriteLine(header);
                        }
                    }


                    // Create log:
                    var log = String.Format("'{0:yyyy-MM-dd HH:mm:ss}, {1}, {2}, {3}, {4}", lotId, pcmQr, Position, Result, DateTime.Now);
                    using (var strWriter = new StreamWriter(filePath, true))
                    {
                        strWriter.WriteLine(log);
                        strWriter.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write("\r\nAutoLogger.CreateMesLog error:" + ex.Message);
                }
            }
        }

        public void CreateTesterLog(String lotId, List<String> pcmCodes)
        {
            lock (autoLocker)
            {
                try
                {
                    // Check file existing:                    
                    var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "LotCounterData");
                    folder = Path.Combine(folder, DateTime.Today.ToString("yyyy-MM-dd"), lotId);
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    var fileName = String.Format("[{0}].csv", this.productId);
                    var filePath = Path.Combine(folder, fileName);

                    // Create log:
                    var log = new StringBuilder();
                    foreach (var qr in pcmCodes)
                    {
                        log.Append(qr + ",");
                    }
                    using (var strWriter = new StreamWriter(filePath, true))
                    {
                        strWriter.WriteLine(log.ToString());
                        strWriter.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write("\r\nAutoLogger.CreateTesterLog error:" + ex.Message);
                }
            }
        }
    }
}
