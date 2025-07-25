using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

namespace VisionInspection
{
    class Visual
    {
        private Thread tcpManagerThread;
        private TcpClient tcpClientMcs;
        Socket sock;
        

        #region Connect
        public delegate void EventConnectedHandler();
        public event EventConnectedHandler VisualConnected;
        public event EventConnectedHandler VisualDisconnected;
        public event EventConnectedHandler ConnectErr;
        public bool Connected = false;
        public void Start(string ip, int port)
        {
            try
            {
                AsyncCallback connectCallback = new AsyncCallback((IAsyncResult result) =>
                {
                    sock.EndConnect(result);
                    Connected = true;
                    
                });
                sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                sock.BeginConnect(ip, port, connectCallback, null);
            }
            catch (Exception ex)
            {
                Start(ip, port);
                Console.WriteLine("Start error:" + ex.Message);
            }
            
        }


        public void Stop()
        {
            AsyncCallback disconnectCallback = new AsyncCallback((IAsyncResult result) =>
            {
                sock.EndDisconnect(result);
                onDisconnected();
            });

            sock.BeginDisconnect(false, disconnectCallback, null);
        }
        void onConnected()
        {
            if (VisualConnected != null)
                VisualConnected();
        }


        void onDisconnected()
        {
            if (VisualDisconnected != null)
                VisualDisconnected();
        }

        void ErrCnt()
        {
            if (ConnectErr != null)
                ConnectErr();

        }

        #endregion


        #region Send QRcode
        public delegate void EventDataSendVisual();
        public event EventDataSendVisual DataSentOKEvent;
        public event EventDataSendVisual DataSentNGEvent;
        public void SendQrCode(string lot, string codejig, List<String> codeproduct)
        {
            try
            {

                ASCIIEncoding encode = new ASCIIEncoding();
                //Fame Send
                List<byte> dat = new List<byte>();

                //add STX
                dat.Add(0x02);

                // add Lot Leng
                string str = lot.Length.ToString().PadLeft(2, '0').ToUpper();
                dat.AddRange(encode.GetBytes(str));

                //add lotid
                str = lot;
                dat.AddRange(encode.GetBytes(lot));

                //Add JigCode Leng
                str = codejig.Length.ToString().PadLeft(2, '0').ToUpper();
                dat.AddRange(encode.GetBytes(str));

                //Add Jigcode
                str = codejig;
                dat.AddRange(encode.GetBytes(str));

                //Add Code product Count
                str = codeproduct.Count.ToString().PadLeft(2, '0').ToUpper(); ;
                dat.AddRange(encode.GetBytes(str));

                //Add Code product Leng
                str = codeproduct[0].Length.ToString().PadLeft(2, '0').ToUpper(); ;
                dat.AddRange(encode.GetBytes(str));

                for (int i = 0; i < codeproduct.Count; i++)
                {
                    str = codeproduct[i];
                    dat.AddRange(encode.GetBytes(str));
                }

                //Add ETX
                dat.Add(0x03);



                //Write command to Read Data
                Encoding asii = Encoding.ASCII;
                byte[] dataSent = dat.ToArray();
                AsyncCallback sentCallback = new AsyncCallback((IAsyncResult result) =>
                {
                    SocketError err;
                    sock.EndSend(result, out err);
                    if (err == SocketError.Success)
                    {
                    }
                    else

                    {
                    }
                });
                sock.BeginSend(dataSent, 0, dataSent.Length, SocketFlags.None, sentCallback, null);
                Thread.Sleep(10);

                // Read respone Data
                byte[] rcvData = new byte[1024];

                AsyncCallback receiveCallback = new AsyncCallback((IAsyncResult result) =>
                {
                    try
                    {
                        sock.EndReceive(result);
                        if (asii.GetString(rcvData).Contains("OK"))
                        {
                            DataSendOK();
                        }
                        else
                        {
                            DataSendNG();
                        }
                    }
                    catch
                    {

                    }

                });

                sock.BeginReceive(rcvData, 0, rcvData.Length, SocketFlags.None, receiveCallback, null);


            }
            catch(Exception ex)
            {
                Console.Out.WriteLine("Err code is " + ex.ToString());
                DataSendNG();
            }
        }

        void DataSendOK()
        {
            if (DataSentOKEvent != null)
                DataSentOKEvent();
            
        }
        void DataSendNG()
        {
            if (DataSentNGEvent != null)
                DataSentNGEvent();
        }
        #endregion

        private void tcpManager()
        {
        }

        public bool IsOpen()
        {
            if (Connected)
                return true;
            else
            {
                return false;
            }
        }

    }
}
