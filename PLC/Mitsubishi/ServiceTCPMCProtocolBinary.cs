using Mitsubishi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Development
{
     public class ServiceTCPMCProtocolBinary : Device
    {
        private TCP_MCProtocol PLC;
        private string IP = "127.0.0.100";
        private int Port = 6001;
        private object PLCLock = new object();
        public ServiceTCPMCProtocolBinary(TCPSetting tcpSetting)
        {
            this.IP = tcpSetting.Ip;
            this.Port = tcpSetting.Port;
        }
        public bool isOpen()
        {
            lock (PLCLock)
            {
                bool isConnect = false;
                isConnect = PLC.isOpen();
                return isConnect;
            }
        }
        public void Open()
        {
            if(PLC == null)
            {
                PLC = new TCP_MCProtocol(IP,Port);
                PLC.Start();
            }    
           
        }
        public void Close()
        {
            if(PLC != null || PLC.isOpen())
            {
                PLC.Disconnect();
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
                _value = 0;
                Result = PLC.ReadWord(devCode, _devNumber, out _value);
                return Result;
            }
        }
        public bool WriteDoubleWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                Result = PLC.WriteDoubleWord(devCode, _devNumber, _writeValue);
                return Result;
            }
        }
        public bool ReadDoubleWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                _value = 0;
                Result = PLC.ReadDoubleWord(devCode, _devNumber, out _value);
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

                const int MAX_READ = 1000; // Giới hạn tối đa mỗi lần đọc
                int totalReads = _count / MAX_READ; // Số lần đọc đầy đủ
                int remaining = _count % MAX_READ; // Số lượng còn lại

                for (int i = 0; i < totalReads; i++)
                {
                    if (!PLC.ReadMultiBits(devCode, _devNumber + i * MAX_READ, MAX_READ, out List<bool> tempValue))
                    {
                        Result = false;
                        break; // Thoát khỏi vòng lặp nếu có lỗi
                    }
                    _lstValue.AddRange(tempValue);
                }

                // Đọc phần còn lại nếu có
                if (remaining > 0 && Result)
                {
                    if (!PLC.ReadMultiBits(devCode, _devNumber + totalReads * MAX_READ, remaining, out List<bool> tempValue))
                    {
                        Result = false;
                    }
                    else
                    {
                        _lstValue.AddRange(tempValue);
                    }
                }

                return Result;
            }
        }
        public bool ReadMultiWord(DeviceCode devCode, int _devNumber, int _count, out List<short> _value)
        {
            lock (PLCLock)
            {
                bool Result = true;
                _value = new List<short>();

                const int MAX_READ = 960; // Giới hạn tối đa mỗi lần đọc
                int totalReads = _count / MAX_READ; // Số lần đọc đầy đủ
                int remaining = _count % MAX_READ; // Số lượng còn lại

                for (int i = 0; i < totalReads; i++)
                {
                    if (!PLC.ReadMultiWord(devCode, _devNumber + i * MAX_READ, MAX_READ, out List<short> tempValue))
                    {
                        Result = false;
                        break; // Thoát khỏi vòng lặp nếu có lỗi
                    }
                    _value.AddRange(tempValue);
                }

                // Đọc phần còn lại nếu có
                if (remaining > 0 && Result)
                {
                    if (!PLC.ReadMultiWord(devCode, _devNumber + totalReads * MAX_READ, remaining, out List<short> tempValue))
                    {
                        Result = false;
                    }
                    else
                    {
                        _value.AddRange(tempValue);
                    }
                }

                return Result;
            }
        }
        public bool ReadMultiDoubleWord(DeviceCode devCode, int _devNumber, int _count, out List<int> _value)
        {
            lock (PLCLock)
            {
                bool Result = true;
                _value = new List<int>();

                const int MAX_READ = 960; // Giới hạn tối đa mỗi lần đọc
                int totalReads = _count / MAX_READ; // Số lần đọc đầy đủ
                int remaining = _count % MAX_READ; // Số lượng còn lại

                for (int i = 0; i < totalReads; i++)
                {
                    if (!PLC.ReadMultiDoubleWord(devCode, _devNumber + i * MAX_READ, MAX_READ, out List<int> tempValue))
                    {
                        Result = false;
                        break; // Thoát khỏi vòng lặp nếu có lỗi
                    }
                    _value.AddRange(tempValue);
                }

                // Đọc phần còn lại nếu có
                if (remaining > 0 && Result)
                {
                    if (!PLC.ReadMultiDoubleWord(devCode, _devNumber + totalReads * MAX_READ, remaining, out List<int> tempValue))
                    {
                        Result = false;
                    }
                    else
                    {
                        _value.AddRange(tempValue);
                    }
                }

                return Result;
            }
        }
        public bool ReadASCIIString(DeviceCode devCode, int _devNumber, int _count, out string result)
        {
            lock (PLCLock)
            {
                bool Result = false;
                result = string.Empty;
                Result = PLC.ReadASCIIString(devCode, _devNumber, _count, out result);
                return Result;
            }
        }
        public bool WriteString (DeviceCode devCode, int _devNumber,string _writeString)
        {
            lock (PLCLock)
            {
                bool Result = false;

                Result = PLC.WriteString(devCode, _devNumber, _writeString);
                return Result;
            }
        }
    }
}
