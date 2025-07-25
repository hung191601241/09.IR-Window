using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VisionInspection;

namespace Development
{

    
    enum PLC_Type
    {
        XGK = 0XA0,
        XGR = 0XA8,
        XGI = 0XA4,
        XGBMK = 0XB0,
        XGBIEC = 0XB4,
    }
    enum DeviceCodeLS
    {
        PX,
        MX,
        KX,
        FX,
        TX,
        CX,
        UX,
        ZX,
        SX,
        LX,
        NX,
        DX,
        RX,
        ZRX,

        PB,
        MB,
        KB,
        FB,
        TB,
        CB,
        UB,
        ZB,
        SB,
        LB,
        NB,
        DB,
        RB,
        ZRB,

        PW,
        MW,
        KW,
        FW,
        TW,
        CW,
        UW,
        ZW,
        SW,
        LW,
        NW,
        DW,
        RW,
        ZRW,

        PD,
        MD,
        KD,
        FD,
        TD,
        CD,
        UD,
        ZD,
        SD,
        LD,
        ND,
        DD,
        RD,
        ZRD,

    }
    class XGTServerTCP
    {
        private static readonly Dictionary<DeviceCodeLS, byte[]> DeviceCodeToBytes = new Dictionary<DeviceCodeLS, byte[]>
        {
        { DeviceCodeLS.PX, new byte[] { 0x25, 0x50, 0x58 } }, // %PX
        { DeviceCodeLS.MX, new byte[] { 0x25, 0x4D, 0x58 } }, // %MX
        { DeviceCodeLS.KX, new byte[] { 0x25, 0x4B, 0x58 } }, // %KX
        { DeviceCodeLS.FX, new byte[] { 0x25, 0x46, 0x58 } }, // %FX
        { DeviceCodeLS.TX, new byte[] { 0x25, 0x54, 0x58 } }, // %TX
        { DeviceCodeLS.CX, new byte[] { 0x25, 0x43, 0x58 } }, // %CX
        { DeviceCodeLS.UX, new byte[] { 0x25, 0x55, 0x58 } }, // %UX
        { DeviceCodeLS.ZX, new byte[] { 0x25, 0x5A, 0x58 } }, // %ZX
        { DeviceCodeLS.SX, new byte[] { 0x25, 0x53, 0x58 } }, // %SX
        { DeviceCodeLS.LX, new byte[] { 0x25, 0x4C, 0x58 } }, // %LX
        { DeviceCodeLS.NX, new byte[] { 0x25, 0x4E, 0x58 } }, // %NX
        { DeviceCodeLS.DX, new byte[] { 0x25, 0x44, 0x58 } }, // %DX
        { DeviceCodeLS.RX, new byte[] { 0x25, 0x52, 0x58 } }, // %RX
        { DeviceCodeLS.ZRX, new byte[] { 0x25, 0x5A, 0x52, 0x58 } }, // %ZRX

        { DeviceCodeLS.PB, new byte[] { 0x25, 0x50, 0x42 } }, // %PB
        { DeviceCodeLS.MB, new byte[] { 0x25, 0x4D, 0x42 } }, // %MB
        { DeviceCodeLS.KB, new byte[] { 0x25, 0x4B, 0x42 } }, // %KB
        { DeviceCodeLS.FB, new byte[] { 0x25, 0x46, 0x42 } }, // %FB
        { DeviceCodeLS.TB, new byte[] { 0x25, 0x54, 0x42 } }, // %TB
        { DeviceCodeLS.CB, new byte[] { 0x25, 0x43, 0x42 } }, // %CB
        { DeviceCodeLS.UB, new byte[] { 0x25, 0x55, 0x42 } }, // %UB
        { DeviceCodeLS.ZB, new byte[] { 0x25, 0x5A, 0x42 } }, // %ZB
        { DeviceCodeLS.SB, new byte[] { 0x25, 0x53, 0x42 } }, // %SB
        { DeviceCodeLS.LB, new byte[] { 0x25, 0x4C, 0x42 } }, // %LB
        { DeviceCodeLS.NB, new byte[] { 0x25, 0x4E, 0x42 } }, // %NB
        { DeviceCodeLS.DB, new byte[] { 0x25, 0x44, 0x42 } }, // %DB
        { DeviceCodeLS.RB, new byte[] { 0x25, 0x52, 0x42 } }, // %RB
        { DeviceCodeLS.ZRB, new byte[] { 0x25, 0x5A, 0x52, 0x42 } }, // %ZRB

        { DeviceCodeLS.PW, new byte[] { 0x25, 0x50, 0x57 } }, // %PW
        { DeviceCodeLS.MW, new byte[] { 0x25, 0x4D, 0x57 } }, // %MW
        { DeviceCodeLS.KW, new byte[] { 0x25, 0x4B, 0x57 } }, // %KW
        { DeviceCodeLS.FW, new byte[] { 0x25, 0x46, 0x57 } }, // %FW
        { DeviceCodeLS.TW, new byte[] { 0x25, 0x54, 0x57 } }, // %TW
        { DeviceCodeLS.CW, new byte[] { 0x25, 0x43, 0x57 } }, // %CW
        { DeviceCodeLS.UW, new byte[] { 0x25, 0x55, 0x57 } }, // %UW
        { DeviceCodeLS.ZW, new byte[] { 0x25, 0x5A, 0x57 } }, // %ZW
        { DeviceCodeLS.SW, new byte[] { 0x25, 0x53, 0x57 } }, // %SW
        { DeviceCodeLS.LW, new byte[] { 0x25, 0x4C, 0x57 } }, // %LW
        { DeviceCodeLS.NW, new byte[] { 0x25, 0x4E, 0x57 } }, // %NW
        { DeviceCodeLS.DW, new byte[] { 0x25, 0x44, 0x57 } }, // %DW
        { DeviceCodeLS.RW, new byte[] { 0x25, 0x52, 0x57 } }, // %RW
        { DeviceCodeLS.ZRW, new byte[] { 0x25, 0x5A, 0x52, 0x57 } }, // %ZRW

        { DeviceCodeLS.PD, new byte[] { 0x25, 0x50, 0x44 } }, // %PD
        { DeviceCodeLS.MD, new byte[] { 0x25, 0x4D, 0x44 } }, // %MD
        { DeviceCodeLS.KD, new byte[] { 0x25, 0x4B, 0x44 } }, // %KD
        { DeviceCodeLS.FD, new byte[] { 0x25, 0x46, 0x44 } }, // %FD
        { DeviceCodeLS.TD, new byte[] { 0x25, 0x54, 0x44 } }, // %TD
        { DeviceCodeLS.CD, new byte[] { 0x25, 0x43, 0x44 } }, // %CD
        { DeviceCodeLS.UD, new byte[] { 0x25, 0x55, 0x44 } }, // %UD
        { DeviceCodeLS.ZD, new byte[] { 0x25, 0x5A, 0x44 } }, // %ZD
        { DeviceCodeLS.SD, new byte[] { 0x25, 0x53, 0x44 } }, // %SD
        { DeviceCodeLS.LD, new byte[] { 0x25, 0x4C, 0x44 } }, // %LD
        { DeviceCodeLS.ND, new byte[] { 0x25, 0x4E, 0x44 } }, // %ND
        { DeviceCodeLS.DD, new byte[] { 0x25, 0x44, 0x44 } }, // %DD
        { DeviceCodeLS.RD, new byte[] { 0x25, 0x52, 0x44 } }, // %RD
        { DeviceCodeLS.ZRD, new byte[] { 0x25, 0x5A, 0x52, 0x44 } }, // %ZRD

       
        };
        private MyLogger logger = new MyLogger("XGTServerTCP");
        private string Ip = "127.0.0.1";
        private Socket client;
        bool IsRunning = false;
        private Thread ThreadMonitor;
        private object PLCLock = new object();
        public XGTServerTCP(string IP)
        {
            Ip = IP;
            if (client == null)
            {
                client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
        }
        //// OK
        #region Auto Reconnect
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
        public void ShutdownDisconnect()
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


                        await Task.Delay(2000);


                        bool isConnected = ConnectWithTimeOut(2000);

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
        #endregion
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

                client.Connect(Ip, 2004);
                if (client.Connected)
                {

                    result = true;
                }
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Connect Socket FENET Protocol Error: " + ex.Message));
            }
            return result;
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
            catch (Exception ex)
            {
                logger.Create(String.Format("XGTServer disconnect Error: " + ex.Message));
            }

            return Result;
        }
        public bool isOpen()
        {
            if (client != null && client.Connected)
            {
                return true;
            }

            return false;
        }
        public bool WriteBit(PLC_Type plcType, DeviceCodeLS deviceCode, int bitAddress, bool value)
        {
            lock(PLCLock)
            {
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Chuyển địa chỉ bit thành chuỗi ASCII (ví dụ: 100 -> "100")
                    string addressStr = bitAddress.ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Lấy mã thiết bị (%MX, %QX, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    ushort variableLength = (ushort)(deviceBytes.Length + addressBytes.Length);
                    byte[] variableLengthBytes = BitConverter.GetBytes(variableLength);

                    // Tạo frame yêu cầu ghi
                    List<byte> frame = new List<byte>
                {
                    // === Header (20 bytes) ===
                    0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                    0x00, 0x00,                         // PLC Info
                    (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                    0x33,                               // Source (Client)
                    0x00, 0x01,                         // Invoke ID
                    0x00, 0x00,                         // Length (sẽ cập nhật sau)
                    0x00,                               // Slot/Base
                    0x3F,                               // Checksum (giả định)

                    // === Command Section ===
                    0x58, 0x00,                         // Command: WRITE
                    0x00, 0x00,                         // Data Type: Bit
                    0x00, 0x00,                         // Reserved
                    0x01, 0x00                          // Block Count = 1
                };

                    // Thêm Variable Length
                    frame.AddRange(variableLengthBytes);

                    // Thêm Variable (địa chỉ, ví dụ: %MX100)
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Thêm Data Size (2 bytes, luôn là 0x0100 cho 1 bit)
                    frame.Add(0x01);
                    frame.Add(0x00);

                    // Thêm Data (1 byte: 0x01 cho true, 0x00 cho false)
                    frame.Add((byte)(value ? 0x01 : 0x00));

                    // Cập nhật Length trong header (từ byte 17-18)
                    ushort length = (ushort)(frame.Count - 20); // Length không tính header
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)((length >> 8) & 0xFF);

                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    int received = client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>(arrRcv.Take(received));

                    // Kiểm tra Company ID
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 ||
                        lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 ||
                        lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 10);

                    // Kiểm tra PLC Info (2 bytes) và CPU Info (1 byte)
                    lstRcv.RemoveRange(0, 3);

                    // Kiểm tra Frame Order No (1 byte, giá trị 0x11 cho phản hồi)
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Invoke ID (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Length (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Position (1 byte)
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Checksum (1 byte)
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Command (2 bytes, 0x0059 cho phản hồi WRITE)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x59)
                    {
                        throw new Exception("Invalid Command Response");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Data Type (2 bytes, 0x0000 cho Bit)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Invalid Data Type");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Reserved (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Error Status (2 bytes)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("PLC Error Response");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start WriteBit Error: " + ex.Message));
                    return false;
                }
            }    
            
        }
        public bool WriteWord(PLC_Type plcType, DeviceCodeLS deviceCode, int wordAddress, short value)
        {
            lock (PLCLock)
            {
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Chuyển địa chỉ Word thành chuỗi ASCII (ví dụ: 100 -> "100")
                    string addressStr = wordAddress.ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Lấy mã thiết bị (%MW, %QW, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    ushort variableLength = (ushort)(deviceBytes.Length + addressBytes.Length);
                    byte[] variableLengthBytes = BitConverter.GetBytes(variableLength);

                    // Tạo frame yêu cầu ghi
                    List<byte> frame = new List<byte>
                    {

                        // === Header (20 bytes) ===
                        0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                        0x00, 0x00,                         // PLC Info
                        (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                        0x33,                               // Source (Client)
                        0x00, 0x01,                         // Invoke ID
                        0x00, 0x00,                         // Length (sẽ cập nhật sau)
                        0x00,                               // Slot/Base
                        0x3F,                               // Checksum (giả định)

                        // === Command Section ===
                        0x58, 0x00,                         // Command: WRITE
                        0x02, 0x00,                         // Data Type: Word
                        0x00, 0x00,                         // Reserved
                        0x01, 0x00                          // Block Count = 1
                    };

                    // Thêm Variable Length
                    frame.AddRange(variableLengthBytes);

                    // Thêm Variable (địa chỉ, ví dụ: %MW100)
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Thêm Data Size (2 bytes, luôn là 0x0200 cho 1 Word)
                    frame.Add(0x02);
                    frame.Add(0x00);

                    // Thêm Data (2 bytes: giá trị Word)
                    byte[] valueBytes = BitConverter.GetBytes(value);
                    frame.AddRange(valueBytes);

                    // Cập nhật Length trong header (từ byte 17-18)
                    ushort length = (ushort)(frame.Count - 20); // Length không tính header
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)((length >> 8) & 0xFF);

                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    int received = client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>(arrRcv.Take(received));

                    // Kiểm tra Company ID
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 ||
                        lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 ||
                        lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 10);

                    // Kiểm tra PLC Info (2 bytes) và CPU Info (1 byte)
                    lstRcv.RemoveRange(0, 3);

                    // Kiểm tra Frame Order No (1 byte, giá trị 0x11 cho phản hồi)
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Invoke ID (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Length (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Position (1 byte)
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Checksum (1 byte)
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Command (2 bytes, 0x0059 cho phản hồi WRITE)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x59)
                    {
                        throw new Exception("Invalid Command Response");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Data Type (2 bytes, 0x0200 cho Word)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x02)
                    {
                        throw new Exception("Invalid Data Type");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Reserved (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Error Status (2 bytes)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("PLC Error Response");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start WriteWord Error: " + ex.Message));
                    return false;
                }
            }
           


        }
        public bool WriteDoubleWord(PLC_Type plcType, DeviceCodeLS deviceCode, int doubleWordAddress, int value)
        {
            lock (PLCLock)
            {
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Chuyển địa chỉ Double Word thành chuỗi ASCII (ví dụ: 100 -> "100")
                    string addressStr = (doubleWordAddress / 2).ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Lấy mã thiết bị (%MD, %QD, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    ushort variableLength = (ushort)(deviceBytes.Length + addressBytes.Length);
                    byte[] variableLengthBytes = BitConverter.GetBytes(variableLength);

                    // Tạo frame yêu cầu ghi
                    List<byte> frame = new List<byte>
                {
                    // === Header (20 bytes) ===
                    0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                    0x00, 0x00,                         // PLC Info
                    (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                    0x33,                               // Source (Client)
                    0x00, 0x01,                         // Invoke ID
                    0x00, 0x00,                         // Length (sẽ cập nhật sau)
                    0x00,                               // Slot/Base
                    0x3F,                               // Checksum (giả định)

                    // === Command Section ===
                    0x58, 0x00,                         // Command: WRITE
                    0x03, 0x00,                         // Data Type: DWord
                    0x00, 0x00,                         // Reserved
                    0x01, 0x00                          // Block Count = 1
                };

                    // Thêm Variable Length
                    frame.AddRange(variableLengthBytes);

                    // Thêm Variable (địa chỉ, ví dụ: %MD100)
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Thêm Data Size (2 bytes, luôn là 0x0400 cho 1 Double Word)
                    frame.Add(0x04);
                    frame.Add(0x00);

                    // Thêm Data (4 bytes: giá trị Double Word)
                    byte[] valueBytes = BitConverter.GetBytes(value);
                    frame.AddRange(valueBytes);

                    // Cập nhật Length trong header (từ byte 17-18)
                    ushort length = (ushort)(frame.Count - 20); // Length không tính header
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)((length >> 8) & 0xFF);

                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    int received = client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>(arrRcv.Take(received));

                    // Kiểm tra Company ID
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 ||
                        lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 ||
                        lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 10);

                    // Kiểm tra PLC Info (2 bytes) và CPU Info (1 byte)
                    lstRcv.RemoveRange(0, 3);

                    // Kiểm tra Frame Order No (1 byte, giá trị 0x11 cho phản hồi)
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");
                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Invoke ID (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Length (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Position (1 byte)
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Checksum (1 byte)
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Command (2 bytes, 0x0059 cho phản hồi WRITE)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x59)
                    {
                        throw new Exception("Invalid Command Response");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Data Type (2 bytes, 0x0300 cho DWord)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x03)
                    {
                        throw new Exception("Invalid Data Type");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Reserved (2 bytes)
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Error Status (2 bytes)
                    if (lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("PLC Error Response");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start WriteDouleWord Error: " + ex.Message));
                    return false;
                }
            }
         
        }
        public bool ReadBit(PLC_Type plcType, DeviceCodeLS deviceCode, int bitAddress, out bool value)
        {
            lock (PLCLock)
            {
                value = false;
                try
                {
                    if (!isOpen())
                        return false;

                    // Convert bit address to byte offset string
                    string addrStr = (bitAddress).ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addrStr);

                    // Get device code bytes
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Variable length = deviceBytes + addressBytes
                    ushort variableLength = (ushort)(deviceBytes.Length + addressBytes.Length);
                    byte[] variableLenBytes = BitConverter.GetBytes(variableLength);

                    // Build request frame
                    List<byte> frame = new List<byte>
                    {
                        0x4C,0x53,0x49,0x53,0x2D,0x58,0x47,0x54,0x00,0x00,  // "LSIS-XGT"
                        0x00,0x00,
                        (byte)plcType,
                        0x33,
                        0x00,0x01,
                        0x00,0x00, // Length placeholder
                        0x00,
                        0x3F,
                        0x54,0x00, // READ command
                        0x00,0x00, // Continuous BYTE
                        0x00,0x00,
                        0x01,0x00  // Block count = 1
                    };

                    frame.AddRange(variableLenBytes);
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Data count: 1 bit = 1 byte (in request)
                    frame.AddRange(BitConverter.GetBytes((ushort)1));

                    // Update length in header
                    ushort length = (ushort)(frame.Count - 20);
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)(length >> 8);

                    // Send request
                    client.Send(frame.ToArray());

                    // Receive response
                    byte[] buffer = new byte[100];
                    int received = client.Receive(buffer);
                    List<byte> resp = new List<byte>(buffer.Take(received));

                    // Remove header (first 32 bytes)
                    resp.RemoveRange(0, 32);

                    // 1 byte response for the bit
                    value = resp[0] != 0x00;
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start ReadBit Error: " + ex.Message));
                    return false;
                }
            }
            
        }
        public bool ReadWord(PLC_Type plcType, DeviceCodeLS deviceCode, int address, out ushort Result)
        {
            lock (PLCLock)
            {
                Result = 0;
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Lấy mã thiết bị (%DB, %MX, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Địa chỉ theo DWord, chuyển sang chuỗi ASCII không nhân 2 (vì là individual read)
                    string addressStr = (address).ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    int variableLength = deviceBytes.Length + addressBytes.Length;
                    byte[] variableLengthBytes = BitConverter.GetBytes((ushort)variableLength);

                    // Tạo frame theo cấu trúc 5.2.6 (1) - individual read
                    List<byte> frame = new List<byte>
                {
                    // === Header (20 bytes) ===
                    0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                    0x00, 0x00,                         // PLC Info
                    (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                    0x33,                               // Source (Client)
                    0x00, 0x00,                         // Invoke ID
                    0x0E, 0x00,                         // Length (sẽ cập nhật sau)
                    0x00,                               // Slot/Base
                    0x3C,                               // Checksum (giả định)

                    // === Command Section ===
                    0x54, 0x00,                         // Command: READ (Individual = 0x004D)
                    0x02, 0x00,                         // Data Type: Word = 0x0002
                    0x00, 0x00,                         // Variable Count = 1
                    0x01, 0x00,                         // Block No
                    0x04, 0x00,                         // Variable Length 

                };

                    //// Thêm độ dài và nội dung biến (Variable)

                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);


                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>(arrRcv);

                    // Kiểm tra xem có đúng Company ID nhân không 
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 || lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 || lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 10);   //10

                    // kiểm tra 3 byte PLC infomation 2 byte , CPU infomation 1 byte
                    lstRcv.RemoveRange(0, 3);    //13

                    // Kiểm tra 1 byte Framr Order No
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 1);    //14

                    // Kiểm tra 2 byte invoked ID
                    lstRcv.RemoveRange(0, 2);    //16

                    // Kiểm tra 2 byte Lenght
                    lstRcv.RemoveRange(0, 2);    //18

                    // Kiểm tra 1 byte Position
                    lstRcv.RemoveRange(0, 1);     //19


                    // Kiểm tra 1 byte CheckSum
                    lstRcv.RemoveRange(0, 1);      //20

                    // Kiểm tra 2 byte là Read hay Write - Read : H5500 Write :H5900
                    //if (lstRcv[0] != 0x00 || lstRcv[1] != 0x55)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);    //22

                    // Kiểm tra xem 2 byte nhận về là dạng Data type gì . Bit:H0000  - Byte:H0100 - Word:H0200 - Dword:H0300 - LWord:H0400 - Continuous:H1400
                    // kiểm tra xem 2 byte có đúng là kiểu đọc multi Continous không
                    //if (lstRcv[0] != 0x14 || lstRcv[1] != 0x00)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);   //24

                    // kiểm tra 2 byte Reserved area 
                    lstRcv.RemoveRange(0, 2);    //26

                    // kiểm tra 2 byte Error status
                    lstRcv.RemoveRange(0, 2);    //28

                    // kiểm tra 2 byte Variable Length
                    lstRcv.RemoveRange(0, 2);    //30

                    // kiểm tra 2 byte Data Count
                    lstRcv.RemoveRange(0, 2);    //32


                    // Đọc 4 byte (Word)
                    Result = BitConverter.ToUInt16(new byte[] { lstRcv[0], lstRcv[1] }, 0);

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start ReadWord Error: " + ex.Message));
                    return false;
                }
            }
          
        }
        public bool ReadDouleWord(PLC_Type plcType, DeviceCodeLS deviceCode, int address, out int Result)
        {
            lock (PLCLock)
            {
                Result = 0;
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Lấy mã thiết bị (%DB, %MX, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Địa chỉ theo DWord, chuyển sang chuỗi ASCII không nhân 2 (vì là individual read)
                    string addressStr = (address / 2).ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    int variableLength = deviceBytes.Length + addressBytes.Length;
                    byte[] variableLengthBytes = BitConverter.GetBytes((ushort)variableLength);

                    // Tạo frame theo cấu trúc 5.2.6 (1) - individual read
                    List<byte> frame = new List<byte>
                {
                    // === Header (20 bytes) ===
                    0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                    0x00, 0x00,                         // PLC Info
                    (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                    0x33,                               // Source (Client)
                    0x00, 0x00,                         // Invoke ID
                    0x0E, 0x00,                         // Length (sẽ cập nhật sau)
                    0x00,                               // Slot/Base
                    0x3C,                               // Checksum (giả định)

                    // === Command Section ===
                    0x54, 0x00,                         // Command: READ (Individual = 0x004D)
                    0x03, 0x00,                         // Data Type: DWord = 0x0003
                    0x00, 0x00,                         // Variable Count = 1
                    0x01, 0x00,                         // Block No
                    0x04, 0x00,                         // Variable Length 

                };

                    //// Thêm độ dài và nội dung biến (Variable)

                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);


                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>(arrRcv);

                    // Kiểm tra xem có đúng Company ID nhân không 
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 || lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 || lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 10);   //10

                    // kiểm tra 3 byte PLC infomation 2 byte , CPU infomation 1 byte
                    lstRcv.RemoveRange(0, 3);    //13

                    // Kiểm tra 1 byte Framr Order No
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 1);    //14

                    // Kiểm tra 2 byte invoked ID
                    lstRcv.RemoveRange(0, 2);    //16

                    // Kiểm tra 2 byte Lenght
                    lstRcv.RemoveRange(0, 2);    //18

                    // Kiểm tra 1 byte Position
                    lstRcv.RemoveRange(0, 1);     //19


                    // Kiểm tra 1 byte CheckSum
                    lstRcv.RemoveRange(0, 1);      //20

                    // Kiểm tra 2 byte là Read hay Write - Read : H5500 Write :H5900
                    //if (lstRcv[0] != 0x00 || lstRcv[1] != 0x55)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);    //22

                    // Kiểm tra xem 2 byte nhận về là dạng Data type gì . Bit:H0000  - Byte:H0100 - Word:H0200 - Dword:H0300 - LWord:H0400 - Continuous:H1400
                    // kiểm tra xem 2 byte có đúng là kiểu đọc multi Continous không
                    //if (lstRcv[0] != 0x14 || lstRcv[1] != 0x00)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);   //24

                    // kiểm tra 2 byte Reserved area 
                    lstRcv.RemoveRange(0, 2);    //26

                    // kiểm tra 2 byte Error status
                    lstRcv.RemoveRange(0, 2);    //28

                    // kiểm tra 2 byte Variable Length
                    lstRcv.RemoveRange(0, 2);    //30

                    // kiểm tra 2 byte Data Count
                    lstRcv.RemoveRange(0, 2);    //32


                    // Đọc 4 byte (DWord)
                    Result = BitConverter.ToInt32(new byte[] { lstRcv[0], lstRcv[1], lstRcv[2], lstRcv[3] }, 0);

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start ReadDouleWord Error: " + ex.Message));
                    return false;
                }
            }
           
        }
        public bool ReadMultiWord(PLC_Type plcType, DeviceCodeLS deviceCode, int address, int dataCount, out List<ushort> Result)
        {
            lock (PLCLock)
            {
                Result = new List<ushort>();
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Lấy mã thiết bị (%DB, %MX, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Chuyển địa chỉ thành chuỗi ASCII (ví dụ: 200 -> "200")
                    string addressStr = (address * 2).ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    int variableLength = deviceBytes.Length + addressBytes.Length;
                    byte[] variableLengthBytes = BitConverter.GetBytes((ushort)variableLength);

                    // Tạo frame yêu cầu đọc
                    List<byte> frame = new List<byte>
                {
                    // === Header (20 bytes) ===
                    0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                    0x00, 0x00,                         // PLC Info
                    (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                    0x33,                               // Source (Client)
                    0x00, 0x01,                         // Invoke ID
                    0x00, 0x00,                         // Length (sẽ cập nhật sau)
                    0x00,                               // Slot/Base
                    0x3F,                               // Checksum (giả định, không nghiêm trọng)

                    // === Command Section ===
                    0x54, 0x00,                         // Command: READ
                    0x14, 0x00,                         // Data Type: Continuous BYTE
                    0x00, 0x00,                         // Reserved
                    0x01, 0x00,                         // Block Count = 1
                };

                    // Thêm Variable Length
                    frame.AddRange(variableLengthBytes);

                    // Thêm Variable (địa chỉ, ví dụ: %DB200)
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Thêm Data Count
                    byte[] dataCountBytes = BitConverter.GetBytes((ushort)dataCount +1);
                    frame.AddRange(dataCountBytes);

                    // Cập nhật Length trong header (từ byte 17-18)
                    ushort length = (ushort)(frame.Count - 20); // Length không tính header
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)((length >> 8) & 0xFF);

                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);

                    // Kiểm tra xem có đúng Company ID nhân không 
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 || lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 || lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 10);

                    // kiểm tra 3 byte PLC infomation 2 byte , CPU infomation 1 byte
                    lstRcv.RemoveRange(0, 3);

                    // Kiểm tra 1 byte Framr Order No
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra 2 byte invoked ID
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra 2 byte Lenght
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra 1 byte Position
                    lstRcv.RemoveRange(0, 1);


                    // Kiểm tra 1 byte CheckSum
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra 2 byte là Read hay Write - Read : H5500 Write :H5900
                    //if (lstRcv[0] != 0x00 || lstRcv[1] != 0x55)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra xem 2 byte nhận về là dạng Data type gì . Bit:H0000  - Byte:H0100 - Word:H0200 - Dword:H0300 - LWord:H0400 - Continuous:H1400
                    // kiểm tra xem 2 byte có đúng là kiểu đọc multi Continous không
                    //if (lstRcv[0] != 0x14 || lstRcv[1] != 0x00)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Reserved area 
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Error status
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Variable Length
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Data Count
                    lstRcv.RemoveRange(0, 2);

                    for (int i = 0; i < dataCount * 2; i += 2)
                    {
                        ushort giaTri = BitConverter.ToUInt16(new byte[] { lstRcv[i], lstRcv[i + 1] }, 0);
                        Result.Add(giaTri);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start ReadMultiWord Error: " + ex.Message));
                    return false;
                }
            }
      
        }
        public bool ReadMultiDoubleWord(PLC_Type plcType, DeviceCodeLS deviceCode, int address, int dataCount, out List<int> Result)
        {
            lock (PLCLock)
            {
                Result = new List<int>();
                try
                {
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Lấy mã thiết bị (%DB, %MX, ...)
                    byte[] deviceBytes = DeviceCodeToBytes[deviceCode];

                    // Chuyển địa chỉ thành chuỗi ASCII (ví dụ: 200 -> "200")
                    string addressStr = (address * 2).ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Tính độ dài biến (deviceBytes + addressBytes)
                    int variableLength = deviceBytes.Length + addressBytes.Length;
                    byte[] variableLengthBytes = BitConverter.GetBytes((ushort)variableLength);

                    // Tạo frame yêu cầu đọc
                    List<byte> frame = new List<byte>
                {
                    // === Header (20 bytes) ===
                    0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                    0x00, 0x00,                         // PLC Info
                    (byte)plcType,                      // CPU Info (XGK, XGBIEC, ...)
                    0x33,                               // Source (Client)
                    0x00, 0x01,                         // Invoke ID
                    0x00, 0x00,                         // Length (sẽ cập nhật sau)
                    0x00,                               // Slot/Base
                    0x3F,                               // Checksum (giả định, không nghiêm trọng)

                    // === Command Section ===
                    0x54, 0x00,                         // Command: READ
                    0x14, 0x00,                         // Data Type: Continuous BYTE
                    0x00, 0x00,                         // Reserved
                    0x01, 0x00,                         // Block Count = 1
                };

                    // Thêm Variable Length
                    frame.AddRange(variableLengthBytes);

                    // Thêm Variable (địa chỉ, ví dụ: %DB200)
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Thêm Data Count
                    byte[] dataCountBytes = BitConverter.GetBytes((ushort)dataCount +1);
                    frame.AddRange(dataCountBytes);

                    // Cập nhật Length trong header (từ byte 17-18)
                    ushort length = (ushort)(frame.Count - 20); // Length không tính header
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)((length >> 8) & 0xFF);

                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi
                    byte[] arrRcv = new byte[10000];
                    client.Receive(arrRcv);
                    List<byte> lstRcv = new List<byte>();
                    lstRcv.AddRange(arrRcv);

                    // Kiểm tra xem có đúng Company ID nhân không 
                    if (lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 || lstRcv[3] != 0x53 || lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 || lstRcv[7] != 0x54 || lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 10);

                    // kiểm tra 3 byte PLC infomation 2 byte , CPU infomation 1 byte
                    lstRcv.RemoveRange(0, 3);

                    // Kiểm tra 1 byte Framr Order No
                    if (lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error");

                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra 2 byte invoked ID
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra 2 byte Lenght
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra 1 byte Position
                    lstRcv.RemoveRange(0, 1);


                    // Kiểm tra 1 byte CheckSum
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra 2 byte là Read hay Write - Read : H5500 Write :H5900
                    //if (lstRcv[0] != 0x00 || lstRcv[1] != 0x55)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra xem 2 byte nhận về là dạng Data type gì . Bit:H0000  - Byte:H0100 - Word:H0200 - Dword:H0300 - LWord:H0400 - Continuous:H1400
                    // kiểm tra xem 2 byte có đúng là kiểu đọc multi Continous không
                    //if (lstRcv[0] != 0x14 || lstRcv[1] != 0x00)
                    //{
                    //    throw new Exception("Subheader Error");

                    //}
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Reserved area 
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Error status
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Variable Length
                    lstRcv.RemoveRange(0, 2);

                    // kiểm tra 2 byte Data Count
                    lstRcv.RemoveRange(0, 2);

                    for (int i = 0; i < dataCount * 2; i += 2)
                    {
                        int giaTri = BitConverter.ToInt32(new byte[] { lstRcv[i], lstRcv[i + 1], lstRcv[i + 2], lstRcv[i + 3] }, 0);
                        Result.Add(giaTri);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create(String.Format("Start ReadMultiDouleWord Error: " + ex.Message));
                    return false;
                }
            }
          
        }
        public bool ReadMultiBit(PLC_Type plcType, DeviceCodeLS deviceCode, int bitAddress, int dataCount, out List<bool> result)
        {
            lock (PLCLock)
            {
                result = new List<bool>();
                try
                {
                    // Kiểm tra kết nối
                    if (!isOpen())
                    {
                        return false;
                    }

                    // Kiểm tra DeviceCode hợp lệ và là kiểu Byte (MB, PB, ...)
                    if (!DeviceCodeToBytes.TryGetValue(deviceCode, out byte[] deviceBytes) ||
                        !deviceCode.ToString().EndsWith("B"))
                    {
                        throw new ArgumentException("Invalid DeviceCode. Must be Byte type (MB, PB, ...)");
                    }

                    // Tính toán địa chỉ byte và số byte cần đọc
                    int startByteAddress = bitAddress / 8; // Byte chứa bit đầu tiên
                    int startBitIndex = bitAddress % 8;   // Vị trí bit trong byte đầu tiên
                    int byteCount = (startBitIndex + dataCount + 7) / 8; // Số byte cần đọc

                    // Chuyển địa chỉ byte thành chuỗi ASCII
                    string addressStr = startByteAddress.ToString();
                    byte[] addressBytes = Encoding.ASCII.GetBytes(addressStr);

                    // Tính độ dài biến
                    ushort variableLength = (ushort)(deviceBytes.Length + addressBytes.Length);
                    byte[] variableLengthBytes = BitConverter.GetBytes(variableLength);

                    // Tạo frame yêu cầu đọc
                    List<byte> frame = new List<byte>
                    {
                        // === Header (20 bytes) ===
                        0x4C, 0x53, 0x49, 0x53, 0x2D, 0x58, 0x47, 0x54, 0x00, 0x00, // Company ID: "LSIS-XGT"
                        0x00, 0x00,                         // PLC Info
                        (byte)plcType,                      // CPU Info
                        0x33,                               // Source (Client)
                        0x00, 0x01,                         // Invoke ID
                        0x00, 0x00,                         // Length (sẽ cập nhật sau)
                        0x00,                               // Slot/Base
                        0x3F,                               // Checksum (giả định)

                        // === Command Section ===
                        0x54, 0x00,                         // Command: READ
                        0x14, 0x00,                         // Data Type: Byte (Continuous)
                        0x00, 0x00,                         // Reserved
                        0x01, 0x00                          // Block Count = 1
                    };

                    // Thêm Variable Length
                    frame.AddRange(variableLengthBytes);

                    // Thêm Variable (địa chỉ, ví dụ: %MB0)
                    frame.AddRange(deviceBytes);
                    frame.AddRange(addressBytes);

                    // Thêm Data Count (số lượng byte cần đọc)
                    byte[] dataCountBytes = BitConverter.GetBytes((ushort)byteCount);
                    frame.AddRange(dataCountBytes);

                    // Cập nhật Length trong header
                    ushort length = (ushort)(frame.Count - 20);
                    frame[16] = (byte)(length & 0xFF);
                    frame[17] = (byte)((length >> 8) & 0xFF);

                    // Gửi frame
                    client.Send(frame.ToArray());

                    // Nhận phản hồi (đảm bảo nhận đủ byte)
                    byte[] arrRcv = new byte[10000];
                    int totalReceived = 0;
                    int expectedBytes = 32 + byteCount; // 32 bytes header + data
                    DateTime startTime = DateTime.Now;
                    client.ReceiveTimeout = 1000; // Timeout 1 giây

                    while (totalReceived < expectedBytes)
                    {
                        if (DateTime.Now.Subtract(startTime).TotalMilliseconds > client.ReceiveTimeout)
                        {
                            throw new Exception("Receive timeout: Not enough data received");
                        }

                        int received = client.Receive(arrRcv, totalReceived, arrRcv.Length - totalReceived, SocketFlags.None);
                        if (received == 0)
                        {
                            throw new Exception("Connection closed by PLC");
                        }
                        totalReceived += received;
                    }

                    List<byte> lstRcv = new List<byte>(arrRcv.Take(totalReceived));

                    // Log frame nhận được
                    Console.WriteLine($"Received frame (hex): {BitConverter.ToString(lstRcv.ToArray()).Replace("-", " ")}");

                    // Kiểm tra Company ID
                    if (lstRcv.Count < 10 || lstRcv[0] != 0x4C || lstRcv[1] != 0x53 || lstRcv[2] != 0x49 ||
                        lstRcv[3] != 0x53 || lstRcv[4] != 0x2D || lstRcv[5] != 0x58 || lstRcv[6] != 0x47 ||
                        lstRcv[7] != 0x54 || lstRcv[8] != 0x00)
                    {
                        throw new Exception("Subheader Error: Invalid Company ID");
                    }
                    lstRcv.RemoveRange(0, 10);

                    // Kiểm tra PLC Info và CPU Info
                    if (lstRcv.Count < 3)
                    {
                        throw new Exception("Insufficient Data: Missing PLC/CPU Info");
                    }
                    lstRcv.RemoveRange(0, 3);

                    // Kiểm tra Frame Order No
                    if (lstRcv.Count < 1 || lstRcv[0] != 0x11)
                    {
                        throw new Exception("Subheader Error: Invalid Frame Order No");
                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Invoke ID
                    if (lstRcv.Count < 2)
                    {
                        throw new Exception("Insufficient Data: Missing Invoke ID");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Length
                    if (lstRcv.Count < 2)
                    {
                        throw new Exception("Insufficient Data: Missing Length");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Position
                    if (lstRcv.Count < 1)
                    {
                        throw new Exception("Insufficient Data: Missing Position");
                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Checksum
                    if (lstRcv.Count < 1)
                    {
                        throw new Exception("Insufficient Data: Missing Checksum");
                    }
                    lstRcv.RemoveRange(0, 1);

                    // Kiểm tra Command
                    if (lstRcv.Count < 2 || lstRcv[0] != 0x55 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Invalid Command Response: Expected 0x0055");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Data Type
                    if (lstRcv.Count < 2 || lstRcv[0] != 0x14 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("Invalid Data Type: Expected 0x0100");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Reserved
                    if (lstRcv.Count < 2)
                    {
                        throw new Exception("Insufficient Data: Missing Reserved");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Error Status
                    if (lstRcv.Count < 2 || lstRcv[0] != 0x00 || lstRcv[1] != 0x00)
                    {
                        throw new Exception("PLC Error Response");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Variable Length
                    if (lstRcv.Count < 2)
                    {
                        throw new Exception("Insufficient Data: Missing Variable Length");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra Data Count
                    if (lstRcv.Count < 2)
                    {
                        throw new Exception("Insufficient Data: Missing Data Count");
                    }
                    lstRcv.RemoveRange(0, 2);

                    // Kiểm tra dữ liệu byte
                    if (lstRcv.Count < byteCount)
                    {
                        throw new Exception($"Insufficient Data: Expected {byteCount} bytes, received {lstRcv.Count}");
                    }

                    // Trích xuất các bit từ các byte nhận được
                    //for (int i = 0; i < dataCount; i++)
                    //{
                    //    int currentBitIndex = startBitIndex + i;
                    //    int byteIndex = currentBitIndex / 8;
                    //    int bitInByte = currentBitIndex % 8;
                    //    bool bitValue = (lstRcv[byteIndex] & (1 << bitInByte)) != 0;
                    //    result.Add(bitValue);
                    //}
                    int currentBitIndex = startBitIndex;
                    int byteIndex = currentBitIndex / 8;
                    int bitInByte = currentBitIndex % 8;

                    result = new List<bool>(dataCount); // nếu cần

                    for (int i = 0; i < dataCount; i++)
                    {
                        bool bitValue = (lstRcv[byteIndex] & (1 << bitInByte)) != 0;
                        result.Add(bitValue);

                        bitInByte++;
                        if (bitInByte == 8)
                        {
                            bitInByte = 0;
                            byteIndex++;
                        }
                    }

                    // Kiểm tra số bit trả về
                    if (result.Count != dataCount)
                    {
                        throw new Exception($"Unexpected result count: Expected {dataCount} bits, got {result.Count}");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in ReadMultiBit: {ex.Message}");
                    return false;
                }
            }
            
        }
    }


}
