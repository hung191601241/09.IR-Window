using System;
using System.Collections.Generic;
using VisionInspection;


namespace Development
{
    class ServiceLSXGTServerCOM :Device
    {
        private MyLogger logger = new MyLogger("ServiceModbusRTUCOM");
        private ModbusCOMSetting XGTServerCOM;
        private XGTServerCom PLC;

        private object PLCLock = new object();
        //private SaveDevice modbusDevice;
        public ServiceLSXGTServerCOM(ModbusCOMSetting comSetting)
        {
            XGTServerCOM = comSetting;
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
            if (PLC == null)
            {
                PLC = new XGTServerCom(XGTServerCOM);
                PLC.Open();
            }

        }
        public void Close()
        {
            if (PLC != null || PLC.isOpen())
            {
                PLC.Close();
            }
        }
        public bool WriteWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if (DeviceCode.D == devCode)
                {
                    Result = PLC.WriteWord(DeviceCodeLSC.DW ,_devNumber.ToString(), _writeValue);
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.WriteWord(DeviceCodeLSC.RW, _devNumber.ToString(), _writeValue);
                }
                return Result;
            }
        }
        public bool ReadWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                ushort value = 0;
                bool Result = false;
                if (DeviceCode.D == devCode)
                {
                    
                    Result = PLC.ReadWord(DeviceCodeLSC.DW, _devNumber,out value);
                    _value = (ushort)value;
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.ReadWord(DeviceCodeLSC.RW, _devNumber, out value);
                    _value = (ushort)value;
                }
                _value = (ushort)value;
                return Result;
            }
        }
        public bool WriteDoubleWord(DeviceCode devCode, int _devNumber, int _writeValue)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if (DeviceCode.D == devCode)
                {
                    Result = PLC.WriteDoubleWord(DeviceCodeLSC.DW, _devNumber.ToString(), _writeValue);
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.WriteDoubleWord(DeviceCodeLSC.RW, _devNumber.ToString(), _writeValue);
                }
                return Result;
              
            }
        }
        public bool ReadDoubleWord(DeviceCode devCode, int _devNumber, out int _value)
        {
            lock (PLCLock)
            {
                uint value = 0;
                bool Result = false;
                if (DeviceCode.D == devCode)
                {

                    Result = PLC.ReadDoubleWord(DeviceCodeLSC.DW, _devNumber, out value);
                    _value = (int)value;
                }
                if (DeviceCode.ZR == devCode)
                {
                    Result = PLC.ReadDoubleWord(DeviceCodeLSC.RW, _devNumber, out value);
                    _value = (int)value;
                }
                _value = (int)value;
                return Result;
            }
        }
        public bool WriteBit(DeviceCode devCode, int _devNumber, bool _value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if (DeviceCode.M == devCode)
                {
                    Result = PLC.WriteBits(DeviceCodeLSC.MX, _devNumber.ToString(), _value);
                }
                if (DeviceCode.L == devCode)
                {
                    Result = PLC.WriteBits(DeviceCodeLSC.LX, _devNumber.ToString(), _value);
                }
                if (DeviceCode.P_LS == devCode)
                {
                    Result = PLC.WriteBits(DeviceCodeLSC.PX, _devNumber.ToString(), _value);
                }
                if (DeviceCode.K_LS == devCode)
                {
                    Result = PLC.WriteBits(DeviceCodeLSC.KX, _devNumber.ToString(), _value);
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
                    Result = PLC.ReadBits(DeviceCodeLSC.MX, _devNumber,out _value);
                }
                if (DeviceCode.L == devCode)
                {
                    Result = PLC.ReadBits(DeviceCodeLSC.LX, _devNumber, out _value);
                }
                if (DeviceCode.P_LS == devCode)
                {
                    Result = PLC.ReadBits(DeviceCodeLSC.PX, _devNumber, out _value);
                }
                if (DeviceCode.K_LS == devCode)
                {
                    Result = PLC.ReadBits(DeviceCodeLSC.KX, _devNumber, out _value);
                }
                return Result;
            }
        }
        public bool ReadMultiBits(DeviceCode devCode, int _devNumber, int _count, out List<bool> _lstValue)
        {
            lock (PLCLock)
            {
                bool Result = true;
                _lstValue = new List<bool>();
                const int MAX_READ_COUNT = 600;

                try
                {
                    if (_count <= 0)
                    {
                        return false;
                    }


                    DeviceCodeLSC deviceCodeLS;
                    switch (devCode)
                    {
                        case DeviceCode.M:
                            deviceCodeLS = DeviceCodeLSC.MW;
                            break;
                        case DeviceCode.L:
                            deviceCodeLS = DeviceCodeLSC.LW;
                            break;
                        case DeviceCode.P_LS:
                            deviceCodeLS = DeviceCodeLSC.PW;
                            break;
                        case DeviceCode.K_LS:
                            deviceCodeLS = DeviceCodeLSC.KW;
                            break;
                        default:
                            return false;
                    }


                    int remainingCount = _count;
                    int offset = _devNumber;

                    while (remainingCount > 0)
                    {

                        int currentCount = Math.Min(remainingCount, MAX_READ_COUNT);


                        List<bool> tempValue;
                        bool readSuccess = PLC.ReadMultiBit(deviceCodeLS, offset, currentCount, out tempValue);

                        if (!readSuccess || tempValue == null || tempValue.Count == 0)
                        {
                            Result = false;
                            break;
                        }


                        _lstValue.AddRange(tempValue);


                        remainingCount -= currentCount;
                        offset += currentCount;
                    }
                }
                catch (Exception)
                {

                    Result = false;
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
                const int MAX_READ_COUNT = 38; 

                try
                {
                    if (_count <= 0)
                    {
                        return false; 
                    }

                   
                    DeviceCodeLSC deviceCodeLS = (devCode == DeviceCode.D) ? DeviceCodeLSC.DW : DeviceCodeLSC.RW;
                    if (devCode != DeviceCode.D && devCode != DeviceCode.ZR)
                    {
                        return false; 
                    }

                   
                    int remainingCount = _count;
                    int offset = _devNumber;

                    while (remainingCount > 0)
                    {
                        
                        int currentCount = Math.Min(remainingCount, MAX_READ_COUNT);

                       
                        List<int> tempValue;
                        bool readSuccess = PLC.ReadMultiWord(deviceCodeLS, offset, currentCount, out tempValue);

                        if (!readSuccess || tempValue == null || tempValue.Count == 0)
                        {
                            Result = false;
                            break; 
                        }

                       
                        foreach (int val in tempValue)
                        {
                            _value.Add((short)val);
                        }

                        
                        remainingCount -= currentCount;
                        offset += currentCount; 
                    }
                }
                catch (Exception)
                {
                    
                    Result = false;
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
                const int MAX_READ_COUNT = 19; 

                try
                {
                    if (_count <= 0)
                    {
                        return false; 
                    }

                  
                    DeviceCodeLSC deviceCodeLS = (devCode == DeviceCode.D) ? DeviceCodeLSC.DW : DeviceCodeLSC.RW;
                    if (devCode != DeviceCode.D && devCode != DeviceCode.ZR)
                    {
                        return false; 
                    }

                  
                    int remainingCount = _count;
                    int offset = _devNumber;

                    while (remainingCount > 0)
                    {
                      
                        int currentCount = Math.Min(remainingCount, MAX_READ_COUNT);

                     
                        List<uint> tempValue;
                        bool readSuccess = PLC.ReadMultiDoubleWord(deviceCodeLS, offset, currentCount, out tempValue);

                        if (!readSuccess || tempValue == null || tempValue.Count == 0)
                        {
                            Result = false;
                            break; 
                        }

                      
                        foreach (uint val in tempValue)
                        {
                            _value.Add((int)val);
                        }

                        
                        remainingCount -= currentCount;
                        offset += currentCount; 
                    }
                }
                catch (Exception)
                {
                    
                    Result = false;
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
                return Result;
            }
        }
        public bool WriteString(DeviceCode devCode, int _devNumber, string _writeString)
        {
            lock (PLCLock)
            {
                bool Result = false;
                return Result;
            }
        }

    }

}
