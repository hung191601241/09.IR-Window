using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace VisionInspection
{
    /// <summary>
    /// Implements Serial COM data communication with the KEYENCE's SR-1000 scanner.
    /// Default COM settings of SR-1000 are: baudrate=115200, databits=8, stopbits=1, parity=Even.
    /// </summary>
    class ScannerComm
    {
        public const String BANK_ID_PKG = "01";
        public const String BANK_ID_JIG = "02";
        public const String BANK_ID_DUMMY = "03";

        private const int READ_TIMEOUT = 300;

        private static MyLogger logger = new MyLogger("ScannerComm");

        private static bool enableReadingLog = false;

        private SerialPort serialPort;
        private byte[] readBuf = new byte[1];
        private volatile bool isReading = false;
        private volatile List<byte> readingBuf;

        public delegate void RxDataHandler(byte rx);
        public event RxDataHandler DataReceived;

        public static void EnableReadingLog(bool enable)
        {
            enableReadingLog = true;
        }

        public ScannerComm(ComSettings settings, RxDataHandler callback)
        {
            try
            {
                this.serialPort = new SerialPort(settings.portName, settings.baudrate,
                    settings.parity, settings.dataBits, settings.stopBits);
                this.DataReceived = callback;
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("ScannerComm error:" + ex.Message));
            }
        }

        public void Start()
        {
            try
            {
                if (!this.serialPort.IsOpen)
                {
                    this.serialPort.Open();

                    this.serialPort.BaseStream.BeginRead(this.readBuf, 0, 1,
                        new AsyncCallback(this.readCallback), this.serialPort);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Start error:" + ex.Message);
            }
        }

        private void readCallback(IAsyncResult iar)
        {
            try
            {
                var port = (SerialPort)iar.AsyncState;
                if (!port.IsOpen)
                {
                    logger.Create("readCallback: port is closed -> stop reading!");
                    return;
                }
                int rxCnt = port.BaseStream.EndRead(iar);
                if (rxCnt == 1)
                {
                    byte rx = this.readBuf[0];

                    // Update reading buffer:
                    if (isReading)
                    {
                        if (rx == 0x0d)
                        {
                            isReading = false;
                        }
                        else
                        {
                            this.readingBuf.Add(rx);
                        }
                    }

                    // Update to UI.log:
                    if (this.DataReceived != null)
                    {
                        this.DataReceived(rx);
                    }
                }

                // Continue reading:            
                port.BaseStream.BeginRead(this.readBuf, 0, 1, new AsyncCallback(readCallback), port);
            }
            catch (Exception ex)
            {
                logger.Create("readCallback error:" + ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                if (this.serialPort != null && this.serialPort.IsOpen)
                {
                    this.serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Stop error:" + ex.Message));
            }
        }

        public String ReadQR(String bankId)
        {
            String ret = "";
            var logger = new ScannerLogger();

            isReading = true;
            this.readingBuf = new List<byte>();
            var cmd = String.Format("LON{0}\r", bankId);

            if (enableReadingLog)
            {
                logger.CreateTxLog(cmd);
            }

            this.serialPort.Write(cmd);

            for (int i = 0; i < READ_TIMEOUT / 10; i++)
            {
                if (!isReading)
                {
                    break;
                }
                Thread.Sleep(10);
            }
            if (!isReading)
            {
                ret = ASCIIEncoding.ASCII.GetString(this.readingBuf.ToArray());

                // Log:
                if (enableReadingLog)
                {
                    logger.CreateRxLog(ret);
                }
            }
            else
            {
                // Finish reading:
                this.serialPort.Write("LOFF\r");
                if (enableReadingLog)
                {
                    isReading = true;
                    logger.CreateTxLog("LOFF\r");
                    for (int i = 0; i < READ_TIMEOUT / 10; i++)
                    {
                        if (!isReading)
                        {
                            break;
                        }
                        Thread.Sleep(10);
                    }
                    if (!isReading)
                    {
                        ret = ASCIIEncoding.ASCII.GetString(this.readingBuf.ToArray());
                        logger.CreateRxLog(ret);
                    }
                }
                else
                {
                    isReading = false;
                }

                // Log:
                //DbWrite.createEvent(new EventLog(EventLog.EV_SCANNER_READ_ERROR, String.Format("BankId={0}", bankId)));
            }

            // Update counters:
            //ScannerManager.UpdateCounters();

            // Check error:
            if ((ret != null) && (ret.Contains("ERROR")))
            {
                ret = "";
            }

            return ret;
        }

        public void Focusing()
        {
            var cmd = String.Format("FTUNE\r");
            this.serialPort.Write(cmd);
        }

        public void Tuning(String bankId)
        {
            var cmd = String.Format("TUNE{0}\r", bankId);
            this.serialPort.Write(cmd);
        }

        public void FinishTuning()
        {
            var cmd = String.Format("TQUIT\r");
            this.serialPort.Write(cmd);
        }

        public bool IsOpen()
        {
            return serialPort.IsOpen;
        }
    }
}
