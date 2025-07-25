using ITM_Semiconductor;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VisionInspection
{
    /// <summary>
    /// Implements Serial COM data communication with the KEYENCE's SR-1000 scanner.
    /// Default COM settings of SR-1000 are: baudrate=115200, databits=8, stopbits=1, parity=Even.
    /// </summary>
    internal class ScannerCommTcp
    {
        public const String BANK_ID_1ST = "01";
        public const String BANK_ID_2ND = "02";
        public const String BANK_ID_3RD = "03";
        public const String BANK_ID_4TH = "01";
        public const String BANK_ID_5TH = "02";
        private const int CONNECT_TIMEOUT = 100;
        private const int READ_TIMEOUT = 300;

        private static MyLogger logger = new MyLogger("ScannerCommTcp");

        private static bool enableReadingLog = false;

        private ScannerSettings settings;
        private Socket tcpClient;
        private bool IsStarted = false;
        private Thread threadMonitor;

        private byte[] readBuf = new byte[1];
        private volatile bool isReading = false;
        private volatile List<byte> readingBuf;

        public delegate void RxDataHandler(byte rx);

        public delegate void DisconnectHandler();

        public event RxDataHandler EvDataReceived;

        public event DisconnectHandler EvDisconnect;

        public Boolean IsConnected
        {
            get
            {
                try
                {
                    if (tcpClient != null)
                    {
                        var sk = tcpClient;
                        if ((!sk.Connected) || sk.Poll(100, SelectMode.SelectRead) && (sk.Available == 0))
                        {
                            return false;
                        }
                        return true;
                    }
                }
                catch { }
                return false;
            }
        }

        public static void EnableReadingLog(bool enable)
        {
            enableReadingLog = true;
        }

        public ScannerCommTcp(ScannerSettings settings, RxDataHandler callback)
        {
            try
            {
                this.settings = settings;
                this.EvDataReceived = callback;
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("ScannerCommTcp error:" + ex.Message));
            }
        }

        public bool Start()
        {
            try
            {
                var scannerIp = IPAddress.Parse(settings.IpAddr);
                var scannerEp = new IPEndPoint(scannerIp, settings.TcpPort);
                tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                logger.Create(String.Format("Start connect to {0}", scannerEp.ToString()));
                IAsyncResult iar = tcpClient.BeginConnect(scannerEp, null, null);
                var success = iar.AsyncWaitHandle.WaitOne(CONNECT_TIMEOUT, true);
                if (IsConnected)
                {
                    tcpClient.EndConnect(iar);

                    IsStarted = true;
                    logger.Create(" -> connected!");

                    // Start async-reading:
                    tcpClient.BeginReceive(readBuf, 0, 1, SocketFlags.None, new AsyncCallback(readCallback), tcpClient);

                    // Monitor connection:
                    threadMonitor = new Thread(() => {
                        while (true)
                        {
                            try
                            {
                                Thread.Sleep(1000);
                                //IsConnected = !(tcpClient.Poll(1, SelectMode.SelectRead) && tcpClient.Available == 0);
                                if (IsStarted && (!IsConnected) && (EvDisconnect != null))
                                {
                                    EvDisconnect();
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Create("MonitorConnection error:" + ex.Message);
                            }
                        }
                    });
                    threadMonitor.IsBackground = true;
                    threadMonitor.Start();
                }
                else
                {
                    // NOTE, MUST CLOSE THE SOCKET
                    tcpClient.Close();
                    logger.Create(" -> connect failed!");
                }
            }
            catch (Exception ex)
            {
                logger.Create("Start error:" + ex.Message);
            }
            return IsConnected;
        }

        public void Stop()
        {
            try
            {
                IsStarted = false;
                if (tcpClient != null)
                {
                    if (threadMonitor != null)
                    {
                        threadMonitor.Abort();
                    }
                    tcpClient.Shutdown(SocketShutdown.Both);
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Stop error:" + ex.Message));
            }
        }

        public String ReadQR()
        {
            if (!IsConnected)
            {
                logger.Create(" -> disconnect -> discard ReadQR!");
                return "";
            }
            String ret = "";
            var qrLogger = new ScannerLogger();

            isReading = true;
            this.readingBuf = new List<byte>();
            var cmd = String.Format("LON{0}\r");

            if (enableReadingLog)
            {
                qrLogger.CreateTxLog(cmd);
            }

            // Send command:
            tcpClient.Send(ASCIIEncoding.ASCII.GetBytes(cmd));

            // Wait for result:
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
                    qrLogger.CreateRxLog(ret);
                }
            }
            else
            {
                // Finish reading:
                tcpClient.Send(ASCIIEncoding.ASCII.GetBytes("LOFF\r"));
                if (enableReadingLog)
                {
                    isReading = false;
                    qrLogger.CreateTxLog("LOFF\r");
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
                        qrLogger.CreateRxLog(ret);
                    }
                }
                else
                {
                    isReading = false;
                }

                // Log:
                //DbWrite.createEvent(new EventLog(EventLog.EV_SCANNER_READ_ERROR, String.Format("BankId={0}", bankId)));
            }

            // Check error:
            if ((ret != null) && (ret.Contains("ERROR")))
            {
                ret = "";
            }

            return ret;
        }

        public void Focusing()
        {
            if (!IsConnected)
            {
                logger.Create(" -> disconnect -> discard Focusing!");
            }
            var cmd = String.Format("FTUNE\r");
            tcpClient.Send(ASCIIEncoding.ASCII.GetBytes(cmd));
        }

        public void Tuning(String bankId)
        {
            if (!IsConnected)
            {
                logger.Create(" -> disconnect -> discard Tuning!");
            }
            var cmd = String.Format("TUNE{0}\r", bankId);
            tcpClient.Send(ASCIIEncoding.ASCII.GetBytes(cmd));
        }

        public void FinishTuning()
        {
            if (!IsConnected)
            {
                logger.Create(" -> disconnect -> discard FinishTuning!");
            }
            var cmd = String.Format("TQUIT\r");
            tcpClient.Send(ASCIIEncoding.ASCII.GetBytes(cmd));
        }

        private void readCallback(IAsyncResult iar)
        {
            try
            {
                var sk = (Socket)iar.AsyncState;
                if (!sk.Connected)
                {
                    logger.Create("readCallback: socket is disconnected -> stop reading!");
                    return;
                }
                int rxCnt = sk.EndReceive(iar);
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
                    if (this.EvDataReceived != null)
                    {
                        this.EvDataReceived(rx);
                    }
                }

                // Continue reading:
                sk.BeginReceive(readBuf, 0, 1, SocketFlags.None, new AsyncCallback(readCallback), sk);
            }
            catch (Exception ex)
            {
                logger.Create("readCallback error:" + ex.Message);
            }
        }
    }
}