
using Development;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using VisionInspection;

namespace Mitsubishi
{
    public class TCP_MCProtocol
    {
        private static readonly Dictionary<DeviceCode, byte> DeviceCodeMap = new Dictionary<DeviceCode, byte>()
        {
            { DeviceCode.SM, 0x91 },
            { DeviceCode.SD, 0xA9 },
            { DeviceCode.X, 0x9C },
            { DeviceCode.Y, 0x9D },
            { DeviceCode.M, 0x90 },
            { DeviceCode.L, 0x92 },
            { DeviceCode.F, 0x93 },
            { DeviceCode.V, 0x94 },
            { DeviceCode.B, 0xA0 },
            { DeviceCode.D, 0xA8 },
            { DeviceCode.W, 0xB4 },
            { DeviceCode.SB, 0xA1 },
            { DeviceCode.R, 0xAF },
            { DeviceCode.ZR, 0xB0 }
        };

        private Thread ThreadMonitor;
        bool IsRunning = false;
        private object PLCLock = new object();

        #region Properties
        private static MyLogger logger = new MyLogger("PLC Q_Enthernet");
        // Socket
        private Socket client;
        private string ipAdress;
        private int portNo;


        //SLMP:
        private int netWorkNo;
        private int pcNo;
        private int stationNo;

        public string IpAdress { get; set; }
        public int PortNo { get; set; }
        public bool IsConnected { get; set; }
        public int NetWorkNo { get; set; }
        public int PcNo { get; set; }
        public int StationNo { get; set; }
        #endregion

        #region Method
        public TCP_MCProtocol()
        {
            try
            {
                this.ipAdress = "192.168.3.39";
                this.portNo = 1026;
                this.netWorkNo = 0;
                this.pcNo = 0xFF;
                this.stationNo = 0;
                if (client == null)
                {
                    client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
            }
            catch (Exception)
            {
            }
        }
        public TCP_MCProtocol(string _ipAddress, int _portNo)
        {
            try
            {
                this.ipAdress = _ipAddress;
                this.portNo = _portNo;
                this.netWorkNo = 0;
                this.pcNo = 0xFF;
                this.stationNo = 0;
                if (client == null)
                {
                    client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
            }
            catch (Exception err)
            {
                logger.Create(String.Format("Q_Enthernet settong IP_Port Error: " + err.Message));
            }
        }
        public TCP_MCProtocol(int _netWorkNo, int _pcNo, int _stationNo)
        {
            try
            {
                this.ipAdress = "192.168.3.39";
                this.portNo = 1026;
                this.netWorkNo = _netWorkNo;
                this.pcNo = _pcNo;
                this.stationNo = _stationNo;
                if (client == null)
                {
                    client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
            }
            catch (Exception err)
            {
                logger.Create(String.Format("Q_Enthernet setting  Error: " + err.Message));

            }
        }
        public void Start()
        {
            try
            {
                if (IsRunning == false)
                {
                    this.IsRunning = true;
                    this.ThreadMonitor = new Thread(new ThreadStart(this.managerSocketFENETProtocol));
                    this.ThreadMonitor.IsBackground = true;
                    this.ThreadMonitor.Start();
                }

            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Start Manager Socket FENET Protocol Error: " + ex.Message));
            }
        }
        private async void managerSocketFENETProtocol()
        {
            int retryCount = 0;
            //int maxRetries = 5;   

            try
            {
                while (IsRunning)
                {
                    if (!isOpen())
                    {

                        logger.Create("Connection lost. Attempting to reconnect...");


                        this.ShutdownDisconnect();


                        await Task.Delay(5000);

                        //if (retryCount >= maxRetries)
                        //{
                        //    logger.Create("Max retry attempts reached. Stopping reconnection attempts.", LogLevel.Error);
                        //    break;
                        //}

                        // Attempt to reconnect
                        bool isConnected = ConnectWithTimeOut(5000);

                        if (isConnected)
                        {
                            retryCount = 0;
                            logger.Create("Reconnected successfully to PLC.");
                        }
                        else
                        {
                            retryCount++;
                            logger.Create($"Reconnection attempt {retryCount} failed.");
                        }
                    }
                    else
                    {
                        // Connection is alive, reset retry count
                        retryCount = 0;
                    }

                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                logger.Create($"Error in managerSocketFENETProtocol: {ex.Message}");
            }
        }
        public bool ConnectWithTimeOut(int timeOut)
        {
            bool ResultConnect = false;
            Action action = delegate
            {
                ResultConnect = Connect();
            };
            IAsyncResult asyncResult = action.BeginInvoke(null, null);
            if (asyncResult.AsyncWaitHandle.WaitOne(timeOut))
            {
                return ResultConnect;
            }

            return false;
        }
        public bool Connect()
        {
            bool result = false;
            try
            {
                if (client == null)
                {
                    client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }

                if (client.Connected)
                {
                    result = true;
                    return result;
                }

                client.Connect(ipAdress, portNo);
                if (client.Connected)
                {

                    result = true;
                }
            }
            catch (Exception)
            {
            }

            return result;
        }
        public bool isOpen()
        {
            if (client != null && client.Connected)
            {
                return true;
            }

            return false;
        }
        //public void ShutdownDisconnect()
        //{
        //    if (client != null)
        //    {
        //        if (client.Connected)
        //        {
        //            client.Shutdown(SocketShutdown.Both);
        //            client.Close();
        //        }

        //        client.Close();
        //        client.Dispose();
        //        client = null;
        //    }
        //}
        private void ShutdownDisconnect()
        {
            if (client != null)
            {
                try
                {
                    if (client.Connected)
                    {
                        client.Shutdown(SocketShutdown.Both);

                    }
                }
                catch (SocketException ex)
                {
                    logger.Create($"Socket shutdown error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    client.Dispose();
                    client = null;
                }
            }
        }
        public bool Disconnect()
        {
            bool Result = false;
            try
            {
                this.IsRunning = false;
                if (client == null)
                {
                    Result = false;
                    return Result;
                }
                if (!client.Connected)
                {
                    Result = true;
                    return Result;
                }
                Task tskDissConnect = new Task(new Action(() =>
                {
                    client.Disconnect(false);
                }));
                tskDissConnect.Start();
                tskDissConnect.Wait(3000);
                if (!client.Connected)
                {

                    Result = true;
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    client.Dispose();
                    client = null;
                }

            }
            catch (Exception err)
            {
                logger.Create(String.Format("Q_Enthernet disconnect Error: " + err.Message));
            }

            return Result;
        }
        public bool WriteWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            bool Result = false;
            if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
            {
                return Result;
            }
            lock (PLCLock)
            {

                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();
                    // 1. Header : Tự động thêm vào
                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);
                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 1401 (Viết xuống) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x14);
                    //6.2 Subcommand : 00000 ( đơn vị là thanh ghi) 2 byte
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;   // int là kiểu 32 bit nên nó tương ứng 4 byte
                    for (int i = 0; i < 3; i++)   // tách 3 byte đầu ra ( Như kiểu từ bit 0 đến bit 23). Bỏ 8 bit sau cùng.
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x00);

                    // 6.5. Data: 2 byte với 1 thanh ghi
                    int intData = _writeValue;
                    for (int i = 0; i < 2; i++)
                    {
                        lstSendData.Add((byte)(intData >> (i * 8)));
                    }
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    // bit: 1 bit
                    // byte: 8 bit: 0 - 255
                    // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                    // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];


                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet writework Error: " + err.Message));
                }
                return Result;
            }

        }
        public bool ReadWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                _value = 0;
                bool Result = false;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 0401 (Đọc lên) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    //6.2 Subcommand : 0000 ( đơn vị là thanh ghi) 2 byte
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 2 byte
                    lstSendData.Add(0x01);   // 1 thanh ghi 16 bit
                    lstSendData.Add(0x00);

                    //// 6.5. Data: 2 byte với 1 thanh ghi
                    //int intData = 1234;
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    lstSendData.Add((byte)(intData >> (i * 8)));
                    //}
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);  // Chỗ Request Data length có 2 byte nên khi BitConverter.GetBytes vào mảng byte thì nó chỉ chuyển đổi thành 2 byte được thôi.
                                                                       // bit: 1 bit
                                                                       // byte: 8 bit: 0 - 255
                                                                       // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                                                                       // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    // 5. Lấy giá trị về:
                    _value = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet ReadWork Error: " + err.Message));
                }

                return Result;
            }

        }
        public bool WriteDoubleWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 1401 (Viết xuống) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x14);
                    //6.2 Subcommand : 0000 ( đơn vị là thanh ghi) 2 byte
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 4 byte (Ghi 32 bit)
                    lstSendData.Add(0x02);
                    lstSendData.Add(0x00);

                    // 6.5. Data: 4 byte với 1 thanh ghi 32 bit
                    int intData = _writeValue;
                    for (int i = 0; i < 4; i++)
                    {
                        lstSendData.Add((byte)(intData >> (i * 8)));
                    }
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    // bit: 1 bit
                    // byte: 8 bit: 0 - 255
                    // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                    // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet WriteDoubleWork Error: " + err.Message));
                }

                return Result;
            }

        }
        public bool ReadDoubleWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _value = 0;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 0401 (Đọc lên) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    //6.2 Subcommand : 0000 ( đơn vị là thanh ghi) 2 byte
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 4 byte bằng 32 bit
                    lstSendData.Add(0x02);  // 2 thanh ghi bằng 32 bit
                    lstSendData.Add(0x00);

                    //// 6.5. Data: 2 byte với 1 thanh ghi
                    //int intData = 1234;
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    lstSendData.Add((byte)(intData >> (i * 8)));
                    //}
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    // bit: 1 bit
                    // byte: 8 bit: 0 - 255
                    // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                    // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    // 5. Lấy giá trị về:
                    _value = BitConverter.ToInt32(new byte[] { lstRcv[0], lstRcv[1], lstRcv[2], lstRcv[3] }, 0);  // 32 bit bằng 4 byte nên phải lấy 1 mảng 4 byte.
                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet ReadDoubleWork Error: " + err.Message));
                }
                return Result;
            }
        }
        public bool WriteBit(DeviceCode devCode, int _devNumber, bool _value)
        {
            lock (PLCLock)
            {
                bool Result = true;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 1401 (Viết xuống) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x14);
                    //6.2 Subcommand : 0001 ( đơn vị là bit) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 2 byte
                    lstSendData.Add(0x01);  // Số lượng bit truy cập.
                    lstSendData.Add(0x00);

                    // 6.5. Data: 4 byte với 1 thanh ghi 32 bit
                    if (_value)
                    {
                        lstSendData.Add(0x10);//ON
                    }
                    else
                    {
                        lstSendData.Add(0x00);//OFF
                    }

                    //lstSendData.Add(0x00);//OFF
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    // bit: 1 bit
                    // byte: 8 bit: 0 - 255
                    // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                    // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet WriteBit Error: " + err.Message));
                }

                return Result;
            }

        }
        public bool ReadBit(DeviceCode devCode, int _devNumber, out bool _value)
        {
            lock (PLCLock)
            {
                _value = false;
                bool Result = false;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        _value = false;
                        return Result;

                    }
                    if (!client.Connected)
                    {
                        _value = false;
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 0401 (Đọc giá trị của bit) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    //6.2 Subcommand : 0001 ( đơn vị là bit) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte : M, X, Y, L , B, SB
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 4 byte bằng 32 bit: Muốn đọc bao nhiêu thằng

                    lstSendData.Add(0x01);  // 1 bit
                    lstSendData.Add(0x00);

                    //// 6.5. Data: 2 byte với 1 thanh ghi
                    //int intData = 1234;
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    lstSendData.Add((byte)(intData >> (i * 8)));
                    //}
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    // bit: 1 bit
                    // byte: 8 bit: 0 - 255
                    // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                    // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    // Giá trị
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1], lstRcv[2] }, 0);
                    if (resLength < 3)  // End code 2 byte cộng Response Data 1 byte
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 5. Response Data:

                    // lstRcv[0] = 0x00; M1 và M2 đều OFF
                    // lstRcv[0] = 0x01; M1 OFF và M2 ON
                    // lstRcv[0] = 0x10; M1 ON và M2 OFF
                    // lstRcv[0] = 0x11; M1 ON và M2 ON
                    if (lstRcv[0] == 0x00)
                    {
                        _value = false;
                    }
                    else
                    {
                        _value = true;
                    }
                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet Readbit Error: " + err.Message));
                    _value = false;
                }

                return Result;
            }

        }
        public bool ReadMultiBits(DeviceCode devCode, int _devNumber, int _count, out List<bool> _lstValue)
        {
            lock (PLCLock)
            {
                _lstValue = new List<bool>();
                bool retValue = false;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return retValue;
                }
                try
                {
                    if (this.client == null)
                    {
                        return retValue;
                    }
                    if (!client.Connected)
                    {
                        return retValue;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);

                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 0401 (Đọc giá trị của bit) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    //6.2 Subcommand : 0001 ( đơn vị là bit) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte : M, X, Y, L , B, SB
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 4 byte bằng 32 bit: Muốn đọc bao nhiêu thằng

                    lstSendData.AddRange(BitConverter.GetBytes((short)_count));


                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    // bit: 1 bit
                    // byte: 8 bit: 0 - 255
                    // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                    // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[3000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    // Giá trị
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1], lstRcv[2] }, 0);
                    if (resLength < 3)  // End code 2 byte cộng Response Data 1 byte
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    //4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 5. Response Data:

                    // lstRcv[0] = 0x00; M1 và M2 đều OFF
                    // lstRcv[0] = 0x01; M1 OFF và M2 ON
                    // lstRcv[0] = 0x10; M1 ON và M2 OFF
                    // lstRcv[0] = 0x11; M1 ON và M2 ON
                    List<bool> lstResult = new List<bool>();
                    int bytes = 0;
                    if (_count % 2 == 0)
                    {
                        bytes = _count / 2;
                    }
                    else
                    {
                        bytes = _count / 2 + 1;
                    }
                    for (int i = 0; i < bytes; i++)
                    {
                        var temp1 = (byte)((lstRcv[i] >> 2) & 0x0F);
                        if (temp1 == 0)
                        {
                            lstResult.Add(false);
                        }
                        else
                            lstResult.Add(true);

                        var temp2 = (byte)((lstRcv[i] >> 0) & 0x0F);
                        if (temp2 == 0)
                        {
                            lstResult.Add(false);
                        }
                        else
                            lstResult.Add(true);
                    }
                    for (int i = 0; i < _count; i++)
                    {
                        _lstValue.Add(lstResult[i]);
                    }
                    retValue = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet ReadMutiBit Error: " + err.Message));
                }

                return retValue;
            }

        }
        public bool ReadMultiWord(DeviceCode devCode, int _devNumber, int _count, out List<short> _value)
        {
            lock (PLCLock)
            {
                _value = new List<short>();
                bool retValue = false;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return retValue;
                }
                try
                {
                    if (client == null)
                    {
                        return retValue;
                    }
                    if (!client.Connected)
                    {
                        return retValue;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 0401 (Đọc lên) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    //6.2 Subcommand : 0000 ( đơn vị là thanh ghi) 2 byte
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 2 byte
                    lstSendData.AddRange(BitConverter.GetBytes((short)_count));

                    //// 6.5. Data: 2 byte với 1 thanh ghi
                    //int intData = 1234;
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    lstSendData.Add((byte)(intData >> (i * 8)));
                    //}
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);  // Chỗ Request Data length có 2 byte nên khi BitConverter.GetBytes vào mảng byte thì nó chỉ chuyển đổi thành 2 byte được thôi.
                                                                       // bit: 1 bit
                                                                       // byte: 8 bit: 0 - 255
                                                                       // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                                                                       // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[5000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?


                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    // 5. Lấy giá trị về:
                    //_value = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    for (int i = 0; i < _count * 2; i += 2)
                    {
                        short giaTri = BitConverter.ToInt16(new byte[] { lstRcv[i], lstRcv[i + 1] }, 0);
                        _value.Add(giaTri);
                    }
                    retValue = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet ReadNutiWork Error: " + err.Message));
                }

                return retValue;
            }

        }
        public bool ReadMultiDoubleWord(DeviceCode devCode, int _devNumber, int _count, out List<int> _value)
        {
            lock (PLCLock)
            {
                _value = new List<int>();
                bool retValue = true;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return retValue;
                }
                try
                {
                    if (client == null)
                    {
                        return retValue;
                    }
                    if (!client.Connected)
                    {
                        return retValue;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    // 16*250ms = 4000ms = 4s
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);
                    // 6. Request Data:
                    //6.1 Command: 0401 (Đọc lên) 2 byte
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    //6.2 Subcommand : 0000 ( đơn vị là thanh ghi) 2 byte
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);
                    //6.3. Device Access:
                    //6.3.1. Device number: 3 byte
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    //6.3.2. Device code: 1 byte
                    lstSendData.Add((byte)_devCode);

                    // 6.4. Number of device point: 2 byte

                    lstSendData.AddRange(BitConverter.GetBytes((ushort)_count));

                    //// 6.5. Data: 2 byte với 1 thanh ghi
                    //int intData = 1234;
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    lstSendData.Add((byte)(intData >> (i * 8)));
                    //}
                    // Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);  // Chỗ Request Data length có 2 byte nên khi BitConverter.GetBytes vào mảng byte thì nó chỉ chuyển đổi thành 2 byte được thôi.
                                                                       // bit: 1 bit
                                                                       // byte: 8 bit: 0 - 255
                                                                       // short: 16 bit: -32768 đến 32767 tương ứng với 1 Word trong PLC
                                                                       // int : 32 bit từ -2.147.483.648 đến 2.147.483.647 tương đương 2 word trong PLC - double word: 4 byte.

                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[10000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);
                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");

                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");

                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4.End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));

                    }
                    lstRcv.RemoveRange(0, 2);
                    // 5. Lấy giá trị về:
                    //_value = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    for (int i = 0; i < _count * 2; i += 2)
                    {
                        // short giaTri = BitConverter.ToInt16(new byte[] { lstRcv[i], lstRcv[i + 1] }, 0);
                        int giaTri = BitConverter.ToInt32(new byte[] { lstRcv[i], lstRcv[i + 1], lstRcv[i + 2], lstRcv[i + 3] }, 0);
                        _value.Add(giaTri);
                    }
                    retValue = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet ReadNutiWork Error: " + err.Message));
                }

                return retValue;
            }

        }
        public bool ReadASCIIString(DeviceCode devCode, int _devNumber, int _count, out string result)
        {
            lock (PLCLock)
            {
                List<string> _value = new List<string>();

                bool Result = false;
                result = string.Empty;

                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }
                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    //3 . Access route : 2 - 6
                    //3.1. Network No: 1 byte
                    lstSendData.Add((byte)this.netWorkNo);
                    //3.2. PC No: 1 byte
                    lstSendData.Add((byte)this.pcNo);
                    //3.3. Request destination module I/ O No. 2 byte
                    lstSendData.Add(0xFF);
                    lstSendData.Add(0x03);
                    //3.4. Request destination module station no: 1 byte
                    lstSendData.Add((byte)this.stationNo);

                    //4.Request Data length: 2 byte từ 7-8
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 10hexa = 16 thập phân 2 byte 9-10
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);

                    // 6. Request Data:
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x04);
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }
                    lstSendData.Add((byte)_devCode);

                    lstSendData.AddRange(BitConverter.GetBytes((ushort)_count));

                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    client.Send(lstSendData.ToArray());

                    byte[] arrRcv = new byte[10000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);

                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 2);

                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");
                    }
                    lstRcv.RemoveRange(0, 5);

                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");
                    }
                    lstRcv.RemoveRange(0, 2);

                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));
                    }
                    lstRcv.RemoveRange(0, 2);

                    for (int i = 0; i < _count * 4; i += 4)
                    {
                        string asciiString = System.Text.Encoding.ASCII.GetString(new byte[] { lstRcv[i], lstRcv[i + 1], lstRcv[i + 2], lstRcv[i + 3] });
                        _value.Add(asciiString);
                    }

                    result = string.Join("", _value);

                    // Loại bỏ các ký tự không mong muốn (chỉ giữ lại ký tự in được)
                    result = new string(result.Where(c => c >= 32 && c <= 126).ToArray());

                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet ReadNutiWork Error: " + err.Message));
                }

                return Result;
            }
        }
        public bool WriteString(DeviceCode devCode, int _devNumber, string _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if (!DeviceCodeMap.TryGetValue(devCode, out byte _devCode))
                {
                    return Result;
                }
                try
                {
                    if (client == null)
                    {
                        return Result;
                    }
                    if (!client.Connected)
                    {
                        return Result;
                    }

                    List<byte> lstSendData = new List<byte>();

                    // 1. Header : Tự động thêm vào

                    // 2. Subheader: 2 byte từ byte 0 đến byte 1
                    lstSendData.Add(0x50);
                    lstSendData.Add(0x00);

                    // 3. Access route : 5 byte từ byte 2 đến byte 6
                    lstSendData.Add((byte)this.netWorkNo);     // Network No
                    lstSendData.Add((byte)this.pcNo);          // PC No
                    lstSendData.Add(0xFF);                     // Request destination module I/O No (2 byte)
                    lstSendData.Add(0x03);
                    lstSendData.Add((byte)this.stationNo);     // Request destination module station no

                    // 4. Request Data length: 2 byte từ 7-8 (tạm thời để 0x00, 0x00, sẽ cập nhật sau)
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 5. Monitoring Timer: 2 byte 9-10 (tạm thời để 0x10, 0x00)
                    lstSendData.Add(0x10);
                    lstSendData.Add(0x00);

                    // 6. Request Data:
                    // 6.1 Command: 2 byte 11-12
                    lstSendData.Add(0x01);
                    lstSendData.Add(0x14);

                    // 6.2 Subcommand: 2 byte 13-14
                    lstSendData.Add(0x00);
                    lstSendData.Add(0x00);

                    // 6.3 Device Access:
                    // 6.3.1 Device number: 3 byte 15-17
                    int intDeviceNumber = _devNumber;
                    for (int i = 0; i < 3; i++)
                    {
                        lstSendData.Add((byte)(intDeviceNumber >> (i * 8)));
                    }

                    // 6.3.2 Device code: 1 byte 18
                    lstSendData.Add((byte)_devCode);

                    // 6.4 Number of device points: 2 byte 19-20
                    // Tính số lượng điểm thiết bị cần ghi (devicePoints), làm tròn lên đến bội số của 4
                    int writeLength = _writeValue.Length;
                    int remainder = writeLength % 4;
                    if (remainder > 0)
                    {
                        // Thêm dấu cách để làm cho độ dài của chuỗi là bội số của 4
                        _writeValue = _writeValue.PadRight(writeLength + (4 - remainder), ' ');
                    }
                    ushort devicePoints = (ushort)(_writeValue.Length / 2); // Mỗi ký tự chiếm 2 byte
                    lstSendData.AddRange(BitConverter.GetBytes(devicePoints));

                    // 6.5 Data: Chuyển chuỗi _writeValue thành mảng byte và thêm vào danh sách
                    byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(_writeValue);
                    lstSendData.AddRange(asciiBytes);

                    // 6.6 Sửa Request Data length ở byte 7 và byte 8
                    int length = lstSendData.Count - 9;
                    byte[] arrLength = BitConverter.GetBytes(length);
                    lstSendData[7] = arrLength[0];
                    lstSendData[8] = arrLength[1];

                    // Gửi đi:
                    client.Send(lstSendData.ToArray());

                    // Nhận về:
                    byte[] arrRcv = new byte[512];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);

                    // Kiểm tra dữ liệu nhận về xem có đúng khung MC Protocol hay không ?

                    // 1. Subheader: 2 byte D000:
                    if (lstRcv[0] != 0xD0 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // 2. Access Route: 5 byte 
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0xFF || lstRcv[2] != 0xFF || lstRcv[3] != 0x03 || lstRcv[4] != 0x00)
                    {
                        throw new Exception("Access Route Error");
                    }
                    lstRcv.RemoveRange(0, 5);

                    // 3. Response Data Length: 2 byte
                    short resLength = BitConverter.ToInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);
                    if (resLength < 2)
                    {
                        throw new Exception("Response Data Length Error");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // 4. End Code: 2 byte
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception(string.Format("Get Error with error code: {0}{1}", lstRcv[0], lstRcv[1]));
                    }
                    lstRcv.RemoveRange(0, 2);

                    Result = true;
                }
                catch (Exception err)
                {
                    logger.Create(String.Format("Q_Enthernet WriteString Error: " + err.Message));
                }

                return Result;
            }

        }

        #endregion
    }

}
