
using Mitsubishi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionInspection;

namespace Development
{
    public class ServiceRs422Fx :Device
    {
        private object PLCLock = new object();
        private String COM = "COM1";
        //private FX3_SC09 PLC = new FX3_SC09();
        private FX3_SC09 PLC = new FX3_SC09();

        public ServiceRs422Fx(SC09Setting Setting)
        {
            COM = Setting.COM;
        }
        public bool isOpen()
        {
            lock (PLCLock)
            {
                bool Result = false;
                Result = PLC.IsOpen();
                return Result;
            }
        }
        public void Open()
        {
            lock (PLCLock)
            {
                PLC.Open(COM);
            }
        }
        public void Close()
        {
            lock (PLCLock)
            {
                PLC.Close();
            }
        }
        public bool WriteWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Result = PLC.WriteWord(devCode, _devNumber, _writeValue);
                return Result;
            }
        }
        public bool ReadWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                short value = 0;
                Result = PLC.ReadWord(devCode, _devNumber, out value);
                _value = value;
                return Result;
            }
        }
        public bool WriteDoubleWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                PLC.WriteDWord(devCode, _devNumber, _writeValue);
                return Result;
            }
        }
        public bool ReadDoubleWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _value = 0;
                Result = PLC.ReadDWord(devCode, _devNumber, out _value);
                return Result;
            }
        }
        public bool WriteBit(DeviceCode devCode, int _devNumber, bool _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Result = PLC.WriteBit(devCode, _devNumber, _value);
                return Result;
            }
        }
        public bool ReadBit(DeviceCode devCode, int _devNumber, out bool _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _value = false;
                Result = PLC.ReadBit(devCode, _devNumber, out _value);
                return Result;
            }
        }
        public bool ReadMultiBits(DeviceCode devCode, int _devNumber, int _count, out List<bool> _lstValue)
        {
            lock (PLCLock)
            {
                
                bool Result = true;
                _lstValue = new List<bool>();

                Result = PLC.ReadMultiBit(devCode, _devNumber, _count, out _lstValue);

                return Result;
            }
        }
        public bool ReadMultiWord(DeviceCode devCode, int _devNumber, int _count, out List<short> _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _value = new List<short>();
                Result = PLC.ReadMultiWord(devCode, _devNumber ,_count, out _value);
                return Result;
            }
        }
        public bool ReadMultiDoubleWord(DeviceCode devCode, int _devNumber, int _count, out List<int> _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Result = PLC.ReadMultiDWord(devCode, _devNumber, _count, out _value);
                return Result;
            }
        }
        public bool ReadASCIIString(DeviceCode devCode, int _devNumber, int _count, out string result)
        {
            lock (PLCLock)
            {
                bool Result = false;
                result = string.Empty;
                if (_count > 200)
                {
                    return Result;
                }
                if (PLC.ReadMultiWord(devCode, _devNumber, _count, out List<short> _value))
                {
                    char[] chars = new char[_count * 2];

                    for (int i = 0; i < _count; i++)
                    {
                        chars[i * 2] = (char)(_value[i] & 0xFF);       // Lấy byte thấp
                        chars[i * 2 + 1] = (char)((_value[i] >> 8) & 0xFF); // Lấy byte cao
                    }

                    result = new string(chars).TrimEnd('\0'); // Chuyển thành chuỗi, loại bỏ ký tự null
                    Result = true;
                }

                return Result;
            }
        }
        public bool WriteString(DeviceCode devCode, int _devNumber, string _writeString)
        {
            lock (PLCLock)
            {
                bool result = true;

                // Đảm bảo chiều dài chuỗi là số chẵn (mỗi 2 ký tự thành 1 short)
                if (_writeString.Length % 2 != 0)
                {
                    _writeString += '\0';
                }

                for (int i = 0; i < _writeString.Length; i += 2)
                {
                    // Chuyển đổi 2 ký tự thành 1 giá trị short
                    int value = (_writeString[i + 1] << 8) | _writeString[i];

                    if (!PLC.WriteWord(devCode, _devNumber + (i / 2), value))
                    {
                        result = false; // Nếu bất kỳ lần ghi nào thất bại, đánh dấu result = false
                    }
                }

                return result;
            }
        }

    }
}
