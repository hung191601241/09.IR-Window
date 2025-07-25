using System;
using System.Collections;
using System.Collections.Generic;

namespace VisionInspection.src.comm
{
    class QrBuffer
    {
        public const int MAX_CAPACITY = 50;
        public const int MIN_CAPACITY = 1;
        public const int DEFAULT_CAPACITY = 20;

        static MyLogger logger = new MyLogger("QrBuffer");

        static Hashtable jigTable = new Hashtable();

        private List<String> qrBuf;
        private int bufCapacity;
        private String jigCode;

        public static QrBuffer FindJigData(String jigCode) {
            if (jigTable.ContainsKey(jigCode)) {
                return (QrBuffer)jigTable[jigCode];
            }
            return null;
        }

        public static void UpdateJigData(String code, QrBuffer buf) {
            if ((!String.IsNullOrEmpty(code)) && (buf != null)) {
                jigTable[code] = buf;
            }
        }

        public QrBuffer(int capacity) {
            if (capacity > MAX_CAPACITY || capacity < MIN_CAPACITY) {
                capacity = DEFAULT_CAPACITY;
            }
            bufCapacity = capacity;
            qrBuf = new List<string>(0);
        }

        public void SetJigCode(String code) {
            if (!String.IsNullOrEmpty(code)) {
                this.jigCode = code;
            }
        }

        public int Add(String qr) {
            if (qrBuf.Count < bufCapacity) {
                qrBuf.Add(qr);
            }
            return qrBuf.Count;
        }

        public Boolean IsFull() {
            if ((qrBuf.Count == bufCapacity) && (bufCapacity > 0)) {
                return true;
            }
            return false;
        }

        public int GetCapacity() {
            return bufCapacity;
        }

        public List<String> GetAllQr() {
            return qrBuf;
        }
    }
}
