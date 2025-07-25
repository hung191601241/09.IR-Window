using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Development
{
    public enum DeviceCodeLSC
    {
        MX,
        DX,
        LX,
        KX,
        PX,
        RX,
        FX,
        TX,
        CX,
        IX,
        QX,
        WX,

        WW,
        QW,
        IW,
        CW,
        TW,
        FW,
        RW,
        PW,
        KW,
        MW,
        LW,
        DW
    }
     class XGTServerCom
    {
        /// <summary>
        /// Thư viện By Hiiep Nguyen 
        /// </summary>
        private bool isConnect;
        private SerialPort _serialPort;
        private const string STX = "\x05"; // Start of Text
        private const string ETX = "\x04"; // End of Text
        private readonly string _stationNo = "01";
        private object PLCLock = new object();

        public event Action<string> OnLogMessage; // Sự kiện để gửi thông báo log lên UI
        private static readonly Dictionary<DeviceCodeLSC, string> DeviceCodeMap = new Dictionary<DeviceCodeLSC, string>()
        {
            { DeviceCodeLSC.MX, "%MX" },
            { DeviceCodeLSC.LX, "%LX" },
            { DeviceCodeLSC.DX, "%DX" },
            { DeviceCodeLSC.KX, "%KX" },
            { DeviceCodeLSC.PX, "%PX" },
            { DeviceCodeLSC.RX, "%RX" },
            { DeviceCodeLSC.FX, "%FX" },
            { DeviceCodeLSC.TX, "%TX" },
            { DeviceCodeLSC.CX, "%CX" },
            { DeviceCodeLSC.IX, "%IX" },
            { DeviceCodeLSC.QX, "%QX" },
            { DeviceCodeLSC.WX, "%WX" },

            { DeviceCodeLSC.WW, "%WW" },
            { DeviceCodeLSC.QW, "%QW" },
            { DeviceCodeLSC.IW, "%IW" },
            { DeviceCodeLSC.CW, "%CW" },
            { DeviceCodeLSC.TW, "%TW" },
            { DeviceCodeLSC.FW, "%FW" },
            { DeviceCodeLSC.RW, "%RW" },
            { DeviceCodeLSC.PW, "%PW" },
            { DeviceCodeLSC.KW, "%KW" },
            { DeviceCodeLSC.LW, "%LW" },
            { DeviceCodeLSC.MW, "%MW" },
            { DeviceCodeLSC.DW, "%DW" }
        };
        public XGTServerCom(ModbusCOMSetting comSetting)
        {
            _stationNo = "01"; // Đảm bảo số trạm là 2 chữ số
            _serialPort = new SerialPort
            {
                PortName = comSetting.portName,
                BaudRate = comSetting.baudrate,
                Parity = comSetting.parity,
                DataBits = comSetting.dataBits,
                StopBits = comSetting.stopBits,
                ReadTimeout = 50,
                WriteTimeout = 50
            };
        }
        public bool isOpen()
        {
            return isConnect;
        }
        public void Open()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    OnLogMessage?.Invoke("Kết nối tới PLC thành công.");
                    isConnect = true;
                   
                }
                
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Lỗi khi mở kết nối: {ex.Message}");
                isConnect = false;
            }
        }
        public void Close()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                    OnLogMessage?.Invoke("Đã ngắt kết nối PLC.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Lỗi khi đóng kết nối: {ex.Message}");
            }
        }
        private bool SendAndReceive(byte[] dataToSend, out byte[] Value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Value = null;
                try
                {

                    if (!_serialPort.IsOpen)
                    {
                        throw new Exception("Cổng COM chưa được mở.");
                    }

                    // Gửi dữ liệu
                    _serialPort.Write(dataToSend, 0, dataToSend.Length);
                    string sendHex = BitConverter.ToString(dataToSend).Replace("-", " ");
                    OnLogMessage?.Invoke($"Gửi dữ liệu (hex): {sendHex}");

                    // Đọc phản hồi
                    List<byte> responseBytes = new List<byte>();
                    _serialPort.ReadTimeout = 50;

                    while (true)
                    {
                        try
                        {
                            int byteRead = _serialPort.ReadByte();
                            if (byteRead == -1)
                            {
                                break;
                            }
                            responseBytes.Add((byte)byteRead);
                        }
                        catch (TimeoutException)
                        {
                            break;
                        }
                    }

                    if (responseBytes.Count > 0)
                    {
                        byte[] response = responseBytes.ToArray();
                        Value = response;
                        string responseHex = BitConverter.ToString(response).Replace("-", " ");
                        OnLogMessage?.Invoke($"Nhận phản hồi (hex): {responseHex}");
                        Result = true;
                        return Result;
                    }
                    else
                    {
                        OnLogMessage?.Invoke("Không nhận được phản hồi từ PLC.");
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Lỗi khi gửi/nhận dữ liệu: {ex.Message}");

                }
                return Result;
            }
           
        }
        private string CalculateBCC(string frame)
        {
            byte bcc = 0;
            foreach (char c in frame)
            {
                bcc ^= (byte)c; // XOR toàn bộ frame
            }
            return bcc.ToString("X2"); // Chuyển sang Hex 2 chữ số
        }


        public bool ReadBits(DeviceCodeLSC deviceCode, int Address, out bool Value)
        {
            lock(PLCLock)
            {
                bool Result = false;
                Value = false;

                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {
                    return Result;
                }

                string stationNo = _stationNo;
                byte _StationNo = Convert.ToByte(stationNo, 16);
                string command = "RSS";
                string dataLength = "01"; // Số khối
                string device = _devCode + Address; // Ví dụ: "%MX10"
                string variableLength = device.Length.ToString("X2"); // "05" cho %MX10
                string frameBody = stationNo + command + dataLength + variableLength + device;
                string frameWithoutBCC = frameBody + ETX; // ETX = "\x03"
                string bcc = CalculateBCC(frameWithoutBCC); // Tính BCC
                byte bccByte = Convert.ToByte(bcc, 16);

                // Tạo mảng byte hoàn chỉnh
                byte[] frameBytesWithoutBCC = Encoding.ASCII.GetBytes(STX + frameWithoutBCC); // STX = "\x05"
                byte[] dataToSend = new byte[frameBytesWithoutBCC.Length + 1];
                Array.Copy(frameBytesWithoutBCC, dataToSend, frameBytesWithoutBCC.Length);
                dataToSend[dataToSend.Length - 1] = bccByte;

                // Gửi và nhận
                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {

                    if (response.Length < 12 || // Độ dài tối thiểu của frame phản hồi
                        response[0] != 0x06 || // ACK
                        response[1] != 0x30 || response[2] != (0x30 + _StationNo) || // Station No
                        response[3] != 0x52 || response[4] != 0x53 || response[5] != 0x53 || // RSS
                        response[6] != 0x30 || response[7] != 0x31 || // Số khối: 01
                        response[8] != 0x30 || response[9] != 0x31) // Độ dài dữ liệu: 01
                    {
                        throw new Exception("Subheader Error");
                    }

                    // Lấy dữ liệu bit (2 byte cuối trước ETX)
                    byte dataHigh = response[10];
                    byte dataLow = response[11];

                    // Chuyển dữ liệu thành giá trị bit (0 hoặc 1)
                    if (dataHigh == 0x30 && dataLow == 0x30) // "00"
                    {
                        Value = false;
                    }
                    else if (dataHigh == 0x30 && dataLow == 0x31) // "01"
                    {
                        Value = true;
                    }
                    else
                    {
                        throw new Exception("Invalid bit value in response");
                    }

                    Result = true;
                    return Result;
                }
                else
                {
                    return Result;
                }
            }    
           
        }
        public bool ReadWord(DeviceCodeLSC deviceCode, int Address, out ushort Value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Value = 0;

                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {

                    return Result;
                }
                string stationNo = _stationNo; // Giả sử _stationNo được định nghĩa trước (ví dụ: "01")
                byte _StationNo = Convert.ToByte(stationNo, 16);
                string command = "RSS";
                string dataLength = "01"; // Số khối
                string device = _devCode + Address; // Ví dụ: "%MW10"
                string variableLength = device.Length.ToString("X2"); // "05" cho %MW10
                string frameBody = stationNo + command + dataLength + variableLength + device;
                string frameWithoutBCC = frameBody + ETX; // ETX = "\x03"
                string bcc = CalculateBCC(frameWithoutBCC); // Tính BCC
                byte bccByte = Convert.ToByte(bcc, 16);

                // Tạo mảng byte hoàn chỉnh
                byte[] frameBytesWithoutBCC = Encoding.ASCII.GetBytes(STX + frameWithoutBCC); // STX = "\x05"
                byte[] dataToSend = new byte[frameBytesWithoutBCC.Length + 1];
                Array.Copy(frameBytesWithoutBCC, dataToSend, frameBytesWithoutBCC.Length);
                dataToSend[dataToSend.Length - 1] = bccByte;

                // Log frame gửi đi
                string sendHex = BitConverter.ToString(dataToSend).Replace("-", " ");
                OnLogMessage?.Invoke($"HÀM READWORD Gửi frame (hex): {sendHex}");

                // Gửi và nhận
                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {

                    // Lấy dữ liệu word (4 byte cuối trước ETX)
                    string dataHex = Encoding.ASCII.GetString(response, 10, 4); // Ví dụ: "0001"
                    if (!ushort.TryParse(dataHex, NumberStyles.HexNumber, null, out Value))
                    {
                        throw new Exception("Invalid word value in response");
                    }

                    Result = true;
                    return Result;
                }
                else
                {
                    OnLogMessage?.Invoke("Lỗi: Gửi/nhận dữ liệu thất bại");
                    return Result;
                }
            }
            
        }
        public bool ReadDoubleWord(DeviceCodeLSC deviceCode, int Address, out uint Value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Value = 0;

                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {

                    return false;
                }

                string stationNo = _stationNo;
                string command = "RSB";
                string numberOfData = "02";
                string device = _devCode + Address.ToString();
                string variableLength = device.Length.ToString("X2");
                string frameBody = stationNo + command + variableLength + device + numberOfData;
                string frameWithoutBCC = frameBody + ETX;
                string bcc = CalculateBCC(frameWithoutBCC);
                byte bccByte = Convert.ToByte(bcc, 16);

                byte[] dataToSend = new byte[Encoding.ASCII.GetBytes(STX + frameWithoutBCC).Length + 1];
                Array.Copy(Encoding.ASCII.GetBytes(STX + frameWithoutBCC), dataToSend, dataToSend.Length - 1);
                dataToSend[dataToSend.Length - 1] = bccByte;

                OnLogMessage?.Invoke($"Gửi frame (hex): {BitConverter.ToString(dataToSend).Replace("-", " ")}");

                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    OnLogMessage?.Invoke($"Nhận phản hồi (hex): {responseHex}");

                    if (response.Length < 19 || response[0] != 0x06)
                    {
                        OnLogMessage?.Invoke("Lỗi: Phản hồi không hợp lệ");
                        return false;
                    }

                    try
                    {
                        List<byte> lstRcv = new List<byte>();
                        lstRcv.AddRange(response);

                        // Kiểm tra dữ liệu nhận về có đúng farme không 

                        //1 Kiểm tra 0x06
                        if (lstRcv[0] != 0x06)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 1);

                        // 2.Bỏ qua kiểm tra số trạm 
                        lstRcv.RemoveRange(0, 2);


                        // 3.Kiểm tra đúng farme XGT không
                        if (lstRcv[0] != 0x52 || lstRcv[1] != 0x53 || lstRcv[2] != 0x42)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 3);

                        //4. kiểm tra data Number of bolck 
                        // Bỏ qua không kiểm tra
                        lstRcv.RemoveRange(0, 2);

                        // 5.Kiem tra Number of Data
                        lstRcv.RemoveRange(0, 2);

                        // 6. Lấy giá trị về:

                        string ascii = Encoding.ASCII.GetString(lstRcv.ToArray(), 0, 8);
                        string String01 = ascii.Substring(0, 4); // "967F"
                        string String02 = ascii.Substring(4, 4); // "0098"

                        ushort mw4 = Convert.ToUInt16(String01, 16);
                        ushort mw5 = Convert.ToUInt16(String02, 16);

                        Value = ((uint)mw5 << 16) | mw4;

                        Result = true;
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Lỗi xử lý dữ liệu phản hồi: {ex.Message}");
                    }
                }

                return Result;
            }
           
        }
        public bool ReadMultiDoubleWord(DeviceCodeLSC deviceCode, int Address, int count, out List<uint> _Value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _Value = new List<uint>();

                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {

                    return false;
                }

                string stationNo = _stationNo;
                string command = "RSB";
                string numberOfData = (count * 2).ToString("D2");
                string device = _devCode + Address.ToString();
                string variableLength = device.Length.ToString("X2");
                string frameBody = stationNo + command + variableLength + device + numberOfData;
                string frameWithoutBCC = frameBody + ETX;
                string bcc = CalculateBCC(frameWithoutBCC);
                byte bccByte = Convert.ToByte(bcc, 16);

                byte[] dataToSend = new byte[Encoding.ASCII.GetBytes(STX + frameWithoutBCC).Length + 1];
                Array.Copy(Encoding.ASCII.GetBytes(STX + frameWithoutBCC), dataToSend, dataToSend.Length - 1);
                dataToSend[dataToSend.Length - 1] = bccByte;

                OnLogMessage?.Invoke($"Gửi frame (hex): {BitConverter.ToString(dataToSend).Replace("-", " ")}");

                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {

                    try
                    {
                        List<byte> lstRcv = new List<byte>();
                        lstRcv.AddRange(response);

                        // Kiểm tra dữ liệu nhận về có đúng farme không 

                        //1 Kiểm tra 0x06
                        if (lstRcv[0] != 0x06)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 1);

                        // 2.Bỏ qua kiểm tra số trạm 
                        lstRcv.RemoveRange(0, 2);


                        // 3.Kiểm tra đúng farme XGT không
                        if (lstRcv[0] != 0x52 || lstRcv[1] != 0x53 || lstRcv[2] != 0x42)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 3);

                        //4. kiểm tra data Number of bolck 
                        // Bỏ qua không kiểm tra
                        lstRcv.RemoveRange(0, 2);

                        // 5.Kiem tra Number of Data
                        if (count * 2 < 100)
                        {
                            lstRcv.RemoveRange(0, 2);
                        }
                        else
                        {
                            lstRcv.RemoveRange(0, 3);
                        }

                        for (int i = 0; i < count; i++)
                        {
                            // Lấy 8 byte tại vị trí tương ứng
                            string ascii = Encoding.ASCII.GetString(lstRcv.ToArray(), i * 8, 8);

                            // Tách thành 2 chuỗi 4 ký tự HEX
                            string str1 = ascii.Substring(0, 4);
                            string str2 = ascii.Substring(4, 4);

                            // Convert từng phần từ HEX sang ushort
                            ushort mw4 = Convert.ToUInt16(str1, 16);
                            ushort mw5 = Convert.ToUInt16(str2, 16);

                            // Ghép lại thành 1 uint
                            uint value = ((uint)mw5 << 16) | mw4;

                            // Thêm vào list kết quả
                            _Value.Add(value);
                            // 6. Lấy giá trị về:
                        }

                        Result = true;
                        return Result;
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Lỗi xử lý dữ liệu phản hồi: {ex.Message}");
                    }
                }

                return Result;
            }
           
        }
        public bool ReadMultiWord(DeviceCodeLSC deviceCode, int Address, int count, out List<int> _Value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _Value = new List<int>();

                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {

                    return false;
                }

                string stationNo = _stationNo;
                string command = "RSB";
                string numberOfData = (count).ToString("D2");
                string device = _devCode + Address.ToString();
                string variableLength = device.Length.ToString("X2");
                string frameBody = stationNo + command + variableLength + device + numberOfData;
                string frameWithoutBCC = frameBody + ETX;
                string bcc = CalculateBCC(frameWithoutBCC);
                byte bccByte = Convert.ToByte(bcc, 16);

                byte[] dataToSend = new byte[Encoding.ASCII.GetBytes(STX + frameWithoutBCC).Length + 1];
                Array.Copy(Encoding.ASCII.GetBytes(STX + frameWithoutBCC), dataToSend, dataToSend.Length - 1);
                dataToSend[dataToSend.Length - 1] = bccByte;

                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {

                    try
                    {
                        List<byte> lstRcv = new List<byte>();
                        lstRcv.AddRange(response);

                        // Kiểm tra dữ liệu nhận về có đúng farme không 

                        //1 Kiểm tra 0x06
                        if (lstRcv[0] != 0x06)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 1);

                        // 2.Bỏ qua kiểm tra số trạm 
                        lstRcv.RemoveRange(0, 2);


                        // 3.Kiểm tra đúng farme XGT không
                        if (lstRcv[0] != 0x52 || lstRcv[1] != 0x53 || lstRcv[2] != 0x42)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 3);

                        //4. kiểm tra data Number of bolck 
                        // Bỏ qua không kiểm tra
                        lstRcv.RemoveRange(0, 2);

                        // 5.Kiem tra Number of Data
                        if (count * 2 < 100)
                        {
                            lstRcv.RemoveRange(0, 2);
                        }
                        else
                        {
                            lstRcv.RemoveRange(0, 3);
                        }

                        for (int i = 0; i < count; i++)
                        {
                            // Lấy 8 byte tại vị trí tương ứng
                            string ascii = Encoding.ASCII.GetString(lstRcv.ToArray(), i * 4, 4);


                            // Convert từng phần từ HEX sang ushort
                            ushort mw4 = Convert.ToUInt16(ascii, 16);
                            // Thêm vào list kết quả
                            _Value.Add(mw4);
                            // 6. Lấy giá trị về:
                        }

                        Result = true;
                        return Result;
                    }
                    catch (Exception)
                    {

                    }
                }

                return Result;
            }
           
        }
        public bool ReadMultiBit(DeviceCodeLSC deviceCode, int Address, int count, out List<bool> _Value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _Value = new List<bool>();

                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {
                    OnLogMessage?.Invoke("Lỗi: Chỉ hỗ trợ DeviceCode.DW (%DW)");
                    return false;
                }

                // Tính số địa chỉ word và offset bit
                int bitOffset = Address % 16; // Offset bit trong word
                int WorfAddress = Address / 16; // Địa chỉ word bắt đầu
                string numberOfAddress = WorfAddress.ToString("D2");

                // Tính số word cần đọc dựa trên số lượng bit và offset
                int totalBitsNeeded = bitOffset + count;
                int wordCount = (totalBitsNeeded + 15) / 16; // Số word cần đọc
                string numberOfData = wordCount.ToString("D2");

                string stationNo = _stationNo;
                string command = "RSB";
                string device = _devCode + numberOfAddress;

                string variableLength = device.Length.ToString("X2");
                string frameBody = stationNo + command + variableLength + device + numberOfData;
                string frameWithoutBCC = frameBody + ETX;
                string bcc = CalculateBCC(frameWithoutBCC);
                byte bccByte = Convert.ToByte(bcc, 16);

                byte[] dataToSend = new byte[Encoding.ASCII.GetBytes(STX + frameWithoutBCC).Length + 1];
                Array.Copy(Encoding.ASCII.GetBytes(STX + frameWithoutBCC), dataToSend, dataToSend.Length - 1);
                dataToSend[dataToSend.Length - 1] = bccByte;

                OnLogMessage?.Invoke($"Gửi frame (hex): {BitConverter.ToString(dataToSend).Replace("-", " ")}");

                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    OnLogMessage?.Invoke($"Nhận phản hồi (hex): {responseHex}");

                    try
                    {
                        List<byte> lstRcv = new List<byte>(response);

                        if (lstRcv[0] != 0x06)
                            throw new Exception("Subheader Error");
                        lstRcv.RemoveRange(0, 1); // remove ACK

                        lstRcv.RemoveRange(0, 2); // skip station no

                        if (lstRcv[0] != 0x52 || lstRcv[1] != 0x53 || lstRcv[2] != 0x42)
                            throw new Exception("Subheader Error");
                        lstRcv.RemoveRange(0, 3); // remove RSB

                        lstRcv.RemoveRange(0, 2); // skip block length

                        // Remove number of data chars
                        if (wordCount * 2 < 100)
                            lstRcv.RemoveRange(0, 2);
                        else
                            lstRcv.RemoveRange(0, 3);

                        List<bool> tempBits = new List<bool>();

                        for (int i = 0; i < wordCount; i++)
                        {
                            string hexString = Encoding.ASCII.GetString(lstRcv.ToArray(), i * 4, 4);
                            ushort value = Convert.ToUInt16(hexString, 16);
                            string binaryString = Convert.ToString(value, 2).PadLeft(16, '0');
                            string reversedBinary = new string(binaryString.Reverse().ToArray());

                            List<bool> bitList = reversedBinary.Select(c => c == '1').ToList();
                            tempBits.AddRange(bitList);
                        }

                        // Lấy đúng các bit bắt đầu từ bitOffset và đủ count bit
                        _Value = tempBits.Skip(bitOffset).Take(count).ToList();
                        Result = true;
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Lỗi xử lý dữ liệu phản hồi: {ex.Message}");
                    }
                }

                return Result;
            }
        }
        public bool WriteBits(DeviceCodeLSC deviceCode, string Address, bool Value)
        {
            lock (PLCLock)
            {
                bool Result = false;


                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {
                    return Result;
                }

                string stationNo = _stationNo;
                byte _StationNo = Convert.ToByte(stationNo, 16);
                string command = "WSS";
                string dataLength = "01"; // Số khối
                string device = _devCode + Address; // Ví dụ: "%MX10"
                string variableLength = device.Length.ToString("X2"); // "05" cho %MX10
                string Bool = "0000";
                if (Value)
                {
                    Bool = "0F";
                }
                else
                {
                    Bool = "00";
                }
                string frameBody = stationNo + command + dataLength + variableLength + device + Bool;
                string frameWithoutBCC = frameBody + ETX; // ETX = "\x03"
                string bcc = CalculateBCC(frameWithoutBCC); // Tính BCC
                byte bccByte = Convert.ToByte(bcc, 16);

                // Tạo mảng byte hoàn chỉnh
                byte[] frameBytesWithoutBCC = Encoding.ASCII.GetBytes(STX + frameWithoutBCC); // STX = "\x05"
                byte[] dataToSend = new byte[frameBytesWithoutBCC.Length + 1];
                Array.Copy(frameBytesWithoutBCC, dataToSend, frameBytesWithoutBCC.Length);
                dataToSend[dataToSend.Length - 1] = bccByte;

                // Gửi và nhận
                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    OnLogMessage?.Invoke($"HÀM READ Nhận phản hồi (hex): {responseHex}");

                    // Check lại kết nối xem đã write thành công chưa
                    try
                    {
                        List<byte> lstRcv = new List<byte>(response);

                        // 1. Check đúng farme nhận chưa ACK = 06
                        if (lstRcv[0] != 0x06)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 1); // remove ACK

                        // 2. Check đúng farme nhận chưa Station
                        lstRcv.RemoveRange(0, 2); // skip station no

                        // 3. Check đúng Command và Command Type chưa
                        if (lstRcv[0] != 0x57 || lstRcv[1] != 0x53 || lstRcv[2] != 0x53)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 3); // Command và Command Type
                        Result = true;
                    }
                    catch (Exception)
                    {

                    }

                    Result = true;
                    return Result;
                }
                else
                {
                    return Result;
                }
            }
           
        }
        public bool WriteWord(DeviceCodeLSC deviceCode, string Address, int Value)
        {
            lock (PLCLock)
            {
                bool Result = false;


                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {
                    return Result;
                }

                string stationNo = _stationNo;
                byte _StationNo = Convert.ToByte(stationNo, 16);
                string command = "WSB";
                string NumberOfData = "01";
                string device = _devCode + Address; // Ví dụ: "%MX10"
                string variableLength = device.Length.ToString("X2"); // "05" cho %MX10

                // Đổi từ INT sang HEX

                string hex = Value.ToString("X4");

                string frameBody = stationNo + command + variableLength + device + NumberOfData + hex;
                string frameWithoutBCC = frameBody + ETX; // ETX = "\x03"
                string bcc = CalculateBCC(frameWithoutBCC); // Tính BCC
                byte bccByte = Convert.ToByte(bcc, 16);

                // Tạo mảng byte hoàn chỉnh
                byte[] frameBytesWithoutBCC = Encoding.ASCII.GetBytes(STX + frameWithoutBCC); // STX = "\x05"
                byte[] dataToSend = new byte[frameBytesWithoutBCC.Length + 1];
                Array.Copy(frameBytesWithoutBCC, dataToSend, frameBytesWithoutBCC.Length);
                dataToSend[dataToSend.Length - 1] = bccByte;

                // Gửi và nhận
                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    OnLogMessage?.Invoke($"HÀM READ Nhận phản hồi (hex): {responseHex}");

                    // Check lại kết nối xem đã write thành công chưa
                    try
                    {
                        List<byte> lstRcv = new List<byte>(response);

                        // 1. Check đúng farme nhận chưa ACK = 06
                        if (lstRcv[0] != 0x06)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 1); // remove ACK

                        // 2. Check đúng farme nhận chưa Station
                        lstRcv.RemoveRange(0, 2); // skip station no

                        // 3. Check đúng Command và Command Type chưa
                        if (lstRcv[0] != 0x57 || lstRcv[1] != 0x53 || lstRcv[2] != 0x42)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 3); // Command và Command Type
                        Result = true;
                    }
                    catch (Exception)
                    {

                    }

                    Result = true;
                    return Result;
                }
                else
                {
                    return Result;
                }
            }
           
        }
        public bool WriteDoubleWord(DeviceCodeLSC deviceCode, string Address, int Value)
        {
            lock (PLCLock)
            {
                bool Result = false;


                if (!DeviceCodeMap.TryGetValue(deviceCode, out string _devCode))
                {
                    return Result;
                }

                string stationNo = _stationNo;
                byte _StationNo = Convert.ToByte(stationNo, 16);
                string command = "WSB";
                string NumberOfData = "02";
                string device = _devCode + Address; // Ví dụ: "%MX10"
                string variableLength = device.Length.ToString("X2"); // "05" cho %MX10

                // Đổi từ INT sang HEX
                ushort BitLow = (ushort)(Value & 0xFFFF);

                // Tách MW5: 16 bit cao
                ushort BitHight = (ushort)((Value >> 16) & 0xFFFF);

                // Chuyển về chuỗi HEX 4 ký tự
                string HEX1 = BitLow.ToString("X4");
                string HEX2 = BitHight.ToString("X4");

                // GỘP CHUỖI LẠI
                string frameBody = stationNo + command + variableLength + device + NumberOfData + HEX1 + HEX2;
                string frameWithoutBCC = frameBody + ETX; // ETX = "\x03"
                string bcc = CalculateBCC(frameWithoutBCC); // Tính BCC
                byte bccByte = Convert.ToByte(bcc, 16);

                // Tạo mảng byte hoàn chỉnh
                byte[] frameBytesWithoutBCC = Encoding.ASCII.GetBytes(STX + frameWithoutBCC); // STX = "\x05"
                byte[] dataToSend = new byte[frameBytesWithoutBCC.Length + 1];
                Array.Copy(frameBytesWithoutBCC, dataToSend, frameBytesWithoutBCC.Length);
                dataToSend[dataToSend.Length - 1] = bccByte;

                // Gửi và nhận
                byte[] response;
                if (SendAndReceive(dataToSend, out response))
                {

                    // Check lại kết nối xem đã write thành công chưa
                    try
                    {
                        List<byte> lstRcv = new List<byte>(response);

                        // 1. Check đúng farme nhận chưa ACK = 06
                        if (lstRcv[0] != 0x06)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 1); // remove ACK

                        // 2. Check đúng farme nhận chưa Station
                        lstRcv.RemoveRange(0, 2); // skip station no

                        // 3. Check đúng Command và Command Type chưa
                        if (lstRcv[0] != 0x57 || lstRcv[1] != 0x53 || lstRcv[2] != 0x42)
                        {
                            throw new Exception("Subheader Error");
                        }
                        lstRcv.RemoveRange(0, 3); // Command và Command Type
                        Result = true;
                    }
                    catch (Exception)
                    {

                    }

                    Result = true;
                    return Result;
                }
                else
                {
                    return Result;
                }

            }
           
        }
    }
}
