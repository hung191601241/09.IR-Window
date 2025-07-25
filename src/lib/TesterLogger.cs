using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VisionInspection
{
    class TesterLogger
    {
        private static Object myKey = new Object();

        private String testerName;

        public TesterLogger(String testerName) {
            if (testerName.Contains("Tester")) {
                this.testerName = testerName.Substring(6, 1);
            } else {
                this.testerName = testerName;
            }
        }

        public void CreateRx91(RX_FRAME frame) {
            try {
                var log = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [R] : ";
                log += String.Format("[2D CODE JIG ID] 0x91, 0x0000, ,{0},{1:X2}", frame.JigQr, frame.Crc);
                this.Create(log);
            } catch { }
        }

        public void CreateRx93(RX_FRAME frame) {
            try {
                var strData = ASCIIEncoding.ASCII.GetString(frame.Data.ToArray());
                var log = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [R] : ";
                log += String.Format("[2D CODE RET] 0x93, 0x{0:X4}, ,{1},{2}", frame.Len, frame.JigQr, strData);
                this.Create(log);
            } catch { }
        }

        public void CreateTx92(Byte[] txBuf) {
            try {
                if (txBuf == null || txBuf.Length < 24) {
                    return;
                }
                var strData = ASCIIEncoding.ASCII.GetString(txBuf, 4, txBuf.Length - 5);
                var log = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [W] : ";
                log += String.Format("[2D CODE] 0x92, 0x{0:X2}{1:X2}, {2}", txBuf[2], txBuf[3], strData);
                this.Create(log);
            } catch { }
        }

        public void CreateTx94(Byte[] txBuf) {
            try {
                if (txBuf == null || txBuf.Length < 24) {
                    return;
                }
                var strData = ASCIIEncoding.ASCII.GetString(txBuf, 4, txBuf.Length - 5);
                var log = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [W] : ";
                log += String.Format("[2D CODE CHECK] 0x94, 0x{0:X2}{1:X2}, {2}", txBuf[2], txBuf[3], strData);
                this.Create(log);
            } catch { }
        }

        private void Create(String log) {
            lock (myKey) {
                try {
                    // Check file existing:                    
                    var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", String.Format("IFCodeLogs{0}", this.testerName));
                    folder = Path.Combine(folder, DateTime.Today.ToString("yyyy-MM"));
                    if (!Directory.Exists(folder)) {
                        Directory.CreateDirectory(folder);
                    }
                    var fileName = String.Format("{0}.log", DateTime.Today.ToString("yyyy-MM-dd"));
                    var filePath = Path.Combine(folder, fileName);

                    // Create log:
                    using (var strWriter = new StreamWriter(filePath, true)) {
                        strWriter.WriteLine(log);
                        strWriter.Flush();
                    }
                } catch (Exception ex) {
                    Debug.Write("TesterLogger.Create error:" + ex.Message);
                }
            }
        }
    }
}
