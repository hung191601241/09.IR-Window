﻿using System;
using System.Diagnostics;
using System.IO;

namespace VisionInspection
{
    class ScannerLogger
    {
        private static Object myKey = new Object();

        public ScannerLogger() {
        }

        public void CreateTxLog(String tx) {
            var log = String.Format("{0} [SEND] : {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), tx);
            this.Create(log);
        }

        public void CreateRxLog(String rx) {
            var log = String.Format("{0} [READ] : {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), rx);
            this.Create(log);
        }

        private void Create(String log) {
            lock (myKey) {
                try {
                    // Check file existing:                    
                    var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "QRCodeLogs");
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
