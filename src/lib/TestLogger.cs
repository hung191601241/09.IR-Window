using System;
using System.Diagnostics;
using System.IO;

namespace VisionInspection
{
    class TestLogger
    {
        private String filePath = "";
        private Object objLock = new Object();
        private String fileName = "";


        public TestLogger(String fileName) {
            try {
                fileName += ".log";
                this.fileName = fileName;


                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testlog");
                if (!Directory.Exists(folder)) {
                    Directory.CreateDirectory(folder);
                }
                filePath = System.IO.Path.Combine(folder, fileName);
            } catch (Exception ex) {
                Debug.Write("\r\nTestLogger error:" + ex.Message);
            }
        }

        public void Create(String content) {

            lock (objLock) {
                try {
                    var log = String.Format("\r\n{0}: {1}", DateTime.Now.ToString("HH:mm:ss.ff"), content);

                    System.Diagnostics.Debug.Write(log);

                    using (var strWriter = new StreamWriter(filePath, true)) {
                        strWriter.Write(log);
                        strWriter.Flush();
                    }
                } catch (Exception ex) {
                    Debug.Write("\r\nTestLogger.Create error:" + ex.Message);
                }
            }
        }
    }
}
