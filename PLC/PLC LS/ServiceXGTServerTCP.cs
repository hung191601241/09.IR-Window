using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisionInspection;


namespace Development
{
    public class ServiceXGTServerTCP :Device
    {
        private MyLogger logger = new MyLogger("ServiceXGTServerTCP");
        private XGTServerTCP PLC;
        private string IP = "127.0.0.100";
       
        private object PLCLock = new object();

        public ServiceXGTServerTCP(TCPSetting setting)
        {
            this.IP = setting.Ip;
        }

        public bool isOpen()
        {
            lock (PLCLock)
            {
                if (PLC != null) 
                { 
                    return PLC.isOpen();
                }
                else
                {
                    return false; 
                }
            }
        }
        public void Open()
        {
            
            PLC = new XGTServerTCP(IP);
            PLC.Start();
           
        }
        public void Close()
        {
            if (PLC != null)
            {
                PLC.Disconnect();
            }
        }
        public bool WriteWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                short writeValue = (short)_writeValue;
                bool Result = false;
                if(DeviceCode.D == devCode)
                {
                    Result = PLC.WriteWord(PLC_Type.XGBMK, DeviceCodeLS.DW, _devNumber, writeValue);
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.WriteWord(PLC_Type.XGBMK, DeviceCodeLS.RW, _devNumber, writeValue);
                }
                return Result;
            }
        }
        public bool ReadWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                _value = 0;
                ushort value = 0;
                bool Result = false;
                if (DeviceCode.D == devCode)
                {

                    Result = PLC.ReadWord(PLC_Type.XGBMK, DeviceCodeLS.DW, _devNumber,out value);
                    
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.ReadWord(PLC_Type.XGBMK, DeviceCodeLS.RW, _devNumber,out value);
                    
                }
                _value = value;
                return Result;
            }
        }
        public bool WriteDoubleWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                short writeValue = (short)_writeValue;
                bool Result = false;
                if (DeviceCode.D == devCode)
                {
                    Result = PLC.WriteDoubleWord(PLC_Type.XGBMK, DeviceCodeLS.DD, _devNumber, _writeValue);
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.WriteDoubleWord(PLC_Type.XGBMK, DeviceCodeLS.RD, _devNumber, _writeValue);
                }
                return Result;
            }
        }
        public bool ReadDoubleWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                _value = 0;
                bool Result = false;
                if (DeviceCode.D == devCode)
                {

                    Result = PLC.ReadDouleWord(PLC_Type.XGBMK, DeviceCodeLS.DD, _devNumber,out _value);
                   
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.ReadDouleWord(PLC_Type.XGBMK, DeviceCodeLS.RD, _devNumber,out _value);
                    
                }
                return Result;
            }
        }
        public bool WriteBit(DeviceCode devCode, int _devNumber, bool _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if(DeviceCode.M == devCode)
                {
                    Result = PLC.WriteBit(PLC_Type.XGBMK, DeviceCodeLS.MX, _devNumber, _value);
                }
                if (DeviceCode.L == devCode)
                {
                    Result = PLC.WriteBit(PLC_Type.XGBMK, DeviceCodeLS.LX, _devNumber, _value);
                }
                if (DeviceCode.K_LS == devCode)
                {
                    Result = PLC.WriteBit(PLC_Type.XGBMK, DeviceCodeLS.KX, _devNumber, _value);
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
                if (DeviceCode.M == devCode)
                {
                    Result = PLC.ReadBit(PLC_Type.XGBMK, DeviceCodeLS.MX, _devNumber,out _value);
                   
                }
                if (DeviceCode.L == devCode)
                {
                    Result = PLC.ReadBit(PLC_Type.XGBMK, DeviceCodeLS.LX, _devNumber,out _value);
                    
                }
                if (DeviceCode.K_LS == devCode)
                {
                    Result = PLC.ReadBit(PLC_Type.XGBMK, DeviceCodeLS.KX, _devNumber,out _value);
                   
                }
                return Result;
            }
        }
        public bool ReadMultiBits(DeviceCode devCode, int _devNumber, int _count, out List<bool> _lstValue)
        {
            lock (PLCLock)
            {
                _lstValue = new List<bool>();
                bool result = false;

                // Ánh xạ DeviceCode sang DeviceCodeLS
                DeviceCodeLS lsDeviceCode;
                switch (devCode)
                {
                    case DeviceCode.M:
                        lsDeviceCode = DeviceCodeLS.MB;
                        break;
                    case DeviceCode.L:
                        lsDeviceCode = DeviceCodeLS.LB;
                        break;
                    case DeviceCode.K_LS:
                        lsDeviceCode = DeviceCodeLS.KB;
                        break;
                    case DeviceCode.P_LS:
                        lsDeviceCode = DeviceCodeLS.PB;
                        break;
                    default:
                        logger.Create("Error: Only DeviceCode.M, L, K_LS, or P_LS is supported");
                        return false;
                }

                // Giới hạn đọc tối đa mỗi lần (128 bit = 16 byte, an toàn với frame XGT)
                const int maxReadCount = 1000;
                int totalRead = 0;

                try
                {
                    while (totalRead < _count)
                    {
                        // Tính số bit cần đọc trong lần này
                        int remaining = _count - totalRead;
                        int readCount = Math.Min(remaining, maxReadCount);

                        // Tính địa chỉ bắt đầu cho lần đọc này
                        int currentAddress = _devNumber + totalRead;

                        // Đọc bit từ PLC
                        List<bool> lstValue;
                        bool success = PLC.ReadMultiBit(PLC_Type.XGBMK, lsDeviceCode, currentAddress, readCount, out lstValue);
                        if (!success)
                        {
                            //logger.Create($"Error: Failed to read {readCount} bits at address {currentAddress}");
                            return false;
                        }

                        // Thêm đúng số bit vào _lstValue
                        _lstValue.AddRange(lstValue.Take(readCount));

                        totalRead += readCount;
                        result = true;
                    }

                    // Kiểm tra số bit trả về
                    if (_lstValue.Count != _count)
                    {
                        logger.Create($"Error: Expected {_count} bits, but got {_lstValue.Count}");
                        return false;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    logger.Create($"Error in ReadMultiBits: {ex.Message}");
                    return false;
                }
            }
        }
        public bool ReadMultiWord(DeviceCode devCode, int _devNumber, int _count, out List<short> _value)
        {
            lock (PLCLock)
            {
                _value = new List<short>();
                bool result = false;

                // Ánh xạ DeviceCode sang DeviceCodeLS
                DeviceCodeLS lsDeviceCode;
                switch (devCode)
                {
                    case DeviceCode.D:
                        lsDeviceCode = DeviceCodeLS.DB;
                        break;
                    case DeviceCode.ZR:
                        lsDeviceCode = DeviceCodeLS.RB;
                        break;
                    default:
                        logger.Create("Error: Only DeviceCode.D or DeviceCode.ZR is supported");
                        return false;
                }

                // Giới hạn đọc tối đa mỗi lần
                const int maxReadCount = 298;
                int totalRead = 0;

                try
                {
                    while (totalRead < _count)
                    {
                        // Tính số Word cần đọc trong lần này
                        int remaining = _count - totalRead;
                        int readCount = Math.Min(remaining, maxReadCount);

                        // Tính địa chỉ bắt đầu cho lần đọc này
                        int currentAddress = _devNumber + totalRead;

                        // Đọc Word từ PLC
                        List<ushort> lstValue;
                        bool success = PLC.ReadMultiWord(PLC_Type.XGBMK, lsDeviceCode, currentAddress, readCount, out lstValue);
                        if (!success)
                        {
                            //logger.Create($"Error: Failed to read {readCount} Words at address {currentAddress}");
                            return false;
                        }

                        // Chuyển đổi từ ushort sang short và thêm vào _value
                        foreach (ushort value in lstValue)
                        {
                            _value.Add((short)value); // Chuyển đổi an toàn
                        }

                        totalRead += readCount;
                        result = true;
                    }

                    // Kiểm tra số Word trả về
                    if (_value.Count != _count)
                    {
                        logger.Create($"Error: Expected {_count} Words, but got {_value.Count}");
                        return false;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    logger.Create($"Error in ReadMultiWord: {ex.Message}");
                    return false;
                }
            }
        }
        public bool ReadMultiDoubleWord(DeviceCode devCode, int _devNumber, int _count, out List<int> _value)
        {
           
            lock (PLCLock)
            {
                _value = new List<int>();
                bool result = false;

                // Ánh xạ DeviceCode sang DeviceCodeLS
                DeviceCodeLS lsDeviceCode;
                switch (devCode)
                {
                    case DeviceCode.D:
                        lsDeviceCode = DeviceCodeLS.DB;
                        break;
                    case DeviceCode.ZR:
                        lsDeviceCode = DeviceCodeLS.RB;
                        break;
                    default:
                        logger.Create("Error: Only DeviceCode.D or DeviceCode.ZR is supported");
                        return false;
                }

                // Giới hạn đọc tối đa mỗi lần
                const int maxReadCount = 298;
                int totalRead = 0;

                try
                {
                    while (totalRead < _count)
                    {
                        // Tính số Word cần đọc trong lần này
                        int remaining = _count - totalRead;
                        int readCount = Math.Min(remaining, maxReadCount);

                        // Tính địa chỉ bắt đầu cho lần đọc này
                        int currentAddress = _devNumber + totalRead;

                        // Đọc Word từ PLC
                        List<int> lstValue;
                        bool success = PLC.ReadMultiDoubleWord(PLC_Type.XGBMK, lsDeviceCode, currentAddress, readCount, out lstValue);
                        if (!success)
                        {
                            //logger.Create($"Error: Failed to read {readCount} Words at address {currentAddress}");
                            return false;
                        }

                        // Chuyển đổi từ ushort sang short và thêm vào _value
                        foreach (int value in lstValue)
                        {
                            _value.Add((int)value); // Chuyển đổi an toàn
                        }

                        totalRead += readCount;
                        result = true;
                    }

                    // Kiểm tra số Word trả về
                    if (_value.Count != _count)
                    {
                        logger.Create($"Error: Expected {_count} Words, but got {_value.Count}");
                        return false;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    logger.Create($"Error in ReadMultiWord: {ex.Message}");
                    return false;
                }
            }
        }
        public bool ReadASCIIString(DeviceCode devCode, int _devNumber, int _count, out string result)
        {
            lock (PLCLock)
            {
                result = "";
                bool resultFlag = false;

                // Ánh xạ DeviceCode sang DeviceCodeLS
                DeviceCodeLS lsDeviceCode;
                switch (devCode)
                {
                    case DeviceCode.D:
                        lsDeviceCode = DeviceCodeLS.DB;
                        break;
                    case DeviceCode.ZR:
                        lsDeviceCode = DeviceCodeLS.RB;
                        break;
                    default:
                        logger.Create("Error: Only DeviceCode.D or DeviceCode.ZR is supported");
                        return false;
                }

                try
                {
                    // Đọc Word từ PLC
                    List<ushort> wordValues;
                    bool success = PLC.ReadMultiWord(PLC_Type.XGBMK, lsDeviceCode, _devNumber, _count, out wordValues);
                    if (!success)
                    {
                        //logger.Create($"Error: Failed to read {_count} Words at address {_devNumber}");
                        return false;
                    }

                    // Kiểm tra số Word trả về
                    if (wordValues.Count != _count)
                    {
                        logger.Create($"Error: Expected {_count} Words, but got {wordValues.Count}");
                        return false;
                    }

                    // Chuyển Word thành byte (mỗi Word chứa 2 ký tự ASCII)
                    List<byte> byteList = new List<byte>();
                    foreach (short val in wordValues)
                    {
                        byte low = (byte)(val & 0xFF);        // Byte thấp (ký tự ASCII 1)
                        byte high = (byte)((val >> 8) & 0xFF); // Byte cao (ký tự ASCII 2)
                                                               // Thêm theo thứ tự little-endian (low, high) - điều chỉnh nếu PLC dùng big-endian
                        byteList.Add(low);
                        byteList.Add(high);
                    }

                    // Chuyển byte thành chuỗi ASCII, loại bỏ ký tự null
                    result = Encoding.ASCII.GetString(byteList.ToArray()).TrimEnd('\0');
                    resultFlag = true;

                    return resultFlag;
                }
                catch (Exception ex)
                {
                    logger.Create($"Error in ReadASCIIString: {ex.Message}");
                    return false;
                }
            }
        }
        public bool WriteString(DeviceCode devCode, int _devNumber, string _writeString)
        {
            lock (PLCLock)
            {
                // Kiểm tra đầu vào
                if (string.IsNullOrEmpty(_writeString))
                {
                    logger.Create("Error: Write string is null or empty");
                    return false;
                }
                if (_devNumber < 0)
                {
                    logger.Create("Error: Invalid device number");
                    return false;
                }

                // Ánh xạ DeviceCode sang DeviceCodeLS
                DeviceCodeLS lsDeviceCode;
                switch (devCode)
                {
                    case DeviceCode.D:
                        lsDeviceCode = DeviceCodeLS.DW;
                        break;
                    case DeviceCode.ZR:
                        lsDeviceCode = DeviceCodeLS.RW;
                        break;
                    default:
                        logger.Create("Error: Only DeviceCode.D or DeviceCode.ZR is supported");
                        return false;
                }

                try
                {
                    // Chuyển chuỗi thành byte
                    byte[] byteData = Encoding.ASCII.GetBytes(_writeString);
                    if (byteData.Length % 2 != 0)
                    {
                        Array.Resize(ref byteData, byteData.Length + 1); // Padding với 0x00
                    }
                    bool isBigEndian = false;
                    // Chuyển byte thành Word
                    short[] shortData = new short[byteData.Length / 2];
                    for (int i = 0; i < shortData.Length; i++)
                    {
                        if (isBigEndian)
                        {
                            // Big-endian: byte đầu tiên ở byte cao
                            shortData[i] = (short)((byteData[i * 2] << 8) | byteData[i * 2 + 1]);
                        }
                        else
                        {
                            // Little-endian: byte đầu tiên ở byte thấp
                            shortData[i] = (short)((byteData[i * 2 + 1] << 8) | byteData[i * 2]);
                        }
                    }

                    // Ghi từng Word
                    for (int i = 0; i < shortData.Length; i++)
                    {
                        int address = _devNumber + i;
                        bool success = PLC.WriteWord(PLC_Type.XGBMK, lsDeviceCode, address, shortData[i]);
                        if (!success)
                        {
                            logger.Create($"Error: Failed to write Word at address {address}");
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Create($"Error in WriteString: {ex.Message}");
                    return false;
                }
            }
        }



    }
}
