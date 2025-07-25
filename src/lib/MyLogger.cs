using OxyPlot;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace VisionInspection
{
    class MyLogger
    {
        private static Object objLock = new Object();

        private String prefix = "";

        public MyLogger(String prefix) {
            this.prefix = prefix;
        }
        //Hàm lấy ra dòng bị lỗi khi có bug
        private int GetLineNumber(Exception ex)
        {
            var st = new System.Diagnostics.StackTrace(ex, true);
            var frame = st.GetFrames().Last();
            return frame.GetFileLineNumber();
        }

        public void Create(String content) {
            // Get FilePath:
            var fileName = String.Format("{0}.log", DateTime.Now.ToString("yyyy-MM-dd"));
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "DebugLogs", "Unit");
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
            var filePath = System.IO.Path.Combine(folder, fileName);

            lock (objLock) {
                try {
                    string log = String.Format("\r\n{0}-{1}: {2}", DateTime.Now.ToString("HH:mm:ss.ff"), this.prefix, content);

                    System.Diagnostics.Debug.Write(log);

                    using (var strWriter = new StreamWriter(filePath, true)) {
                        strWriter.Write(log);
                        strWriter.Flush();
                    }
                } catch (Exception ex) {
                    Debug.Write("\r\nMyLoger.Create error:" + ex.Message);
                }
            }
        }
        public void Create(String content, Exception except)
        {
            // Get FilePath:
            var fileName = String.Format("{0}.log", DateTime.Now.ToString("yyyy-MM-dd"));
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "DebugLogs", "Unit");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var filePath = System.IO.Path.Combine(folder, fileName);

            lock (objLock)
            {
                try
                {
                    string log = String.Format("\r\n{0}-{1}-Line {2}: {3}", DateTime.Now.ToString("HH:mm:ss.ff"), this.prefix, GetLineNumber(except).ToString(), content);

                    System.Diagnostics.Debug.Write(log);

                    using (var strWriter = new StreamWriter(filePath, true))
                    {
                        strWriter.Write(log);
                        strWriter.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write("\r\nMyLoger.Create error:" + ex.Message);
                }
            }
        }
    }
}
