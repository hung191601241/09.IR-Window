using Development;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
///// Thư viện By Hiiep //////
////////////////////////////////
namespace Mitsubishi
{

    public class FX3_SC09
    {
        #region Properties
        // Serial Port:
        private SerialPort Comport = new SerialPort();
        // private SerialPort serial;

        private object PLCLock = new object();
        #endregion

        private object ComportLock = new object();

        public bool Open(string Port)
        {
            try
            {
                this.Comport.PortName = Port;
                this.Comport.BaudRate = 9600;
                this.Comport.Parity = Parity.Even;
                this.Comport.DataBits = 7;
                this.Comport.StopBits = StopBits.One;
                this.Comport.ReadTimeout = 1000;
                this.Comport.WriteTimeout = 1000;
                this.Comport.ReadBufferSize = 1024;
                this.Comport.WriteBufferSize = 1024;
                if (this.Comport.IsOpen)
                    this.Comport.Close();
                this.Comport.Open();
                return this.Comport.IsOpen;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void Close() => this.Comport.Close();
        public bool WriteWord(DeviceCode device, int address, int value)
        {
            lock (PLCLock)
            {
                bool Result = false;
                if (this.Comport == null || !this.Comport.IsOpen)
                    throw new Exception("Please check the status of the serial port!");

                // Chuyển đổi địa chỉ thành ASCII dựa trên Devicecode
                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.D:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.D, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                // Chuyển đổi giá trị thành ASCII
                byte ascii0_1, ascii1_1, ascii0_2, ascii1_2;
                this.Dec2Ascii((byte)(value & 0xFF), out ascii0_1, out ascii1_1);
                this.Dec2Ascii((byte)((value >> 8) & 0xFF), out ascii0_2, out ascii1_2);

                // Tạo gói tin
                byte[] numArray = new byte[13]
                {
                    (byte)2,          // STX - Bắt đầu chuỗi
                    (byte)49,         // Lệnh ghi
                    outAddress[3],    // Địa chỉ (MSB -> LSB)
                    outAddress[2],
                    outAddress[1],
                    outAddress[0],
                    (byte)48,         // Số lượng ghi (02 byte)
                    (byte)50,
                    ascii1_1,         // Dữ liệu (byte thấp)
                    ascii0_1,
                    ascii1_2,         // Dữ liệu (byte cao)
                    ascii0_2,
                    (byte)3           // ETX - Kết thúc chuỗi
                };

                // Tính toán Checksum
                byte sum0, sum1;
                this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                // Chuẩn bị gửi dữ liệu
                List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                // Gửi dữ liệu qua cổng COM
                lock (this.ComportLock)
                {
                    this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                    Result = this.ReadVoidAck(); // Kiểm tra phản hồi từ PLC
                    Result = true;
                }
                return Result;
            }
        }
        public bool WriteDWord(DeviceCode device, int address, int value)
        {
            bool Result = false;
            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                {
                    return Result;
                }


                // Chuyển đổi địa chỉ thành ASCII dựa trên Devicecode
                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.D:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.D, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }
                byte ascii0_1;
                byte ascii1_1;
                this.Dec2Ascii((byte)(value & (int)byte.MaxValue), out ascii0_1, out ascii1_1);
                byte ascii0_2;
                byte ascii1_2;
                this.Dec2Ascii((byte)(value >> 8 & (int)byte.MaxValue), out ascii0_2, out ascii1_2);
                byte ascii0_3;
                byte ascii1_3;
                this.Dec2Ascii((byte)(value >> 16 & (int)byte.MaxValue), out ascii0_3, out ascii1_3);
                byte ascii0_4;
                byte ascii1_4;
                this.Dec2Ascii((byte)(value >> 24 & (int)byte.MaxValue), out ascii0_4, out ascii1_4);
                byte[] numArray = new byte[17]
                {
                (byte) 2,
                (byte) 49,
                outAddress[3],
                outAddress[2],
                outAddress[1],
                outAddress[0],
                (byte) 48,
                (byte) 52,
                ascii1_1,
                ascii0_1,
                ascii1_2,
                ascii0_2,
                ascii1_3,
                ascii0_3,
                ascii1_4,
                ascii0_4,
                (byte) 3
                };
                byte sum0;
                byte sum1;
                this.CheckSum((IEnumerable<byte>)numArray, 1, numArray.Length - 1, out sum0, out sum1);
                List<byte> byteList = new List<byte>((IEnumerable<byte>)numArray);
                byteList.Add(sum1);
                byteList.Add(sum0);
                lock (this.ComportLock)
                {
                    this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                    Result = this.ReadVoidAck();
                    Result = true;
                }
            }
            return Result;
        }
        public bool WriteBit(DeviceCode device, int address, bool value)
        {
            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                    throw new Exception("Please check the status of the serial port!");

                // Xác định loại lệnh: 55 = Set, 56 = Reset
                byte command = value ? (byte)55 : (byte)56;

                // Chuyển đổi địa chỉ dựa vào Devicecode
                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.Y:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.WY, address, out outAddress);
                        break;
                    case DeviceCode.M:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.WM, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                // Tạo gói tin
                byte[] numArray = new byte[7]
                {
                    (byte)2,           // STX - Bắt đầu chuỗi
                    command,           // 55 (Set) hoặc 56 (Reset)
                    outAddress[1],     // Địa chỉ
                    outAddress[0],
                    outAddress[3],
                    outAddress[2],     // Giá trị
                    (byte)3            // ETX - Kết thúc chuỗi
                };

                // Tính toán Checksum
                byte sum0, sum1;
                this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                // Chuẩn bị gửi dữ liệu
                List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                // Gửi dữ liệu qua cổng COM
                lock (this.ComportLock)
                {
                    this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                    return this.Set_or_Reset_Bit_Ack();
                }
            }
        }
        public bool ReadBit(DeviceCode device, int address, out bool value)
        {
            bool Result = false;
            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                {
                    Result = false;
                }


                // Chuyển đổi địa chỉ dựa vào Devicecode
                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.Y:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.RY, address, out outAddress);
                        break;
                    case DeviceCode.M:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.RM, address, out outAddress);
                        break;
                    case DeviceCode.X:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.RX, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                // Tạo gói tin
                byte[] numArray = new byte[9]
                {
                    (byte)2,            // STX - Bắt đầu chuỗi
                    (byte)48,           // Lệnh đọc
                    outAddress[3],      // Địa chỉ bit (MSB -> LSB)
                    outAddress[2],
                    outAddress[1],
                    outAddress[0],
                    (byte)48,           // Số lượng đọc (01 bit)
                    (byte)49,
                    (byte)3             // ETX - Kết thúc chuỗi
                };

                // Tính toán Checksum
                byte sum0, sum1;
                this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                // Chuẩn bị gửi dữ liệu
                List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                // Gửi dữ liệu qua cổng COM
                lock (this.ComportLock)
                {
                    this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                    value = this.Read_Bit_Ack(address);
                    Result = true;
                }
            }
            return Result;
        }
        public bool ReadWord(DeviceCode device, int address, out short value)
        {
            value = 0;
            bool result = false;
            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                    return false;

                // Chuyển đổi địa chỉ dựa vào Devicecode
                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.D:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.D, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                // Tạo gói tin
                byte[] numArray = new byte[9]
                {
                    (byte)2,            // STX - Bắt đầu chuỗi
                    (byte)48,           // Lệnh đọc
                    outAddress[3],      // Địa chỉ (MSB -> LSB)
                    outAddress[2],
                    outAddress[1],
                    outAddress[0],
                    (byte)48,           // Số lượng đọc (02 byte)
                    (byte)50,
                    (byte)3             // ETX - Kết thúc chuỗi
                };

                // Tính toán Checksum
                byte sum0, sum1;
                this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                // Chuẩn bị gửi dữ liệu
                List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                // Gửi dữ liệu qua cổng COM
                lock (this.ComportLock)
                {
                    this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                    value = this.ReadIntAck();

                }
                result = true;
            }
            return result;
        }
        public bool ReadMultiWord(DeviceCode device, int address, int number, out List<short> values)
        {
            values = new List<short>();
            bool result = false;

            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                    return false;

                for (int i = 0; i < number; i++)  // Đọc nhiều lần
                {
                    byte[] outAddress;
                    switch (device)
                    {
                        case DeviceCode.D:
                            this.AddressToAscii(FX3_SC09.REGISTER_TYPE.D, address + i, out outAddress);
                            break;
                        default:
                            throw new Exception("Unsupported device type");
                    }

                    // Tạo gói tin
                    byte[] numArray = new byte[9]
                    {
                        (byte)2,            // STX - Bắt đầu chuỗi
                        (byte)48,           // Lệnh đọc
                        outAddress[3],      // Địa chỉ (MSB -> LSB)
                        outAddress[2],
                        outAddress[1],
                        outAddress[0],
                        (byte)48,           // Số lượng đọc (02 byte)
                        (byte)50,
                        (byte)3             // ETX - Kết thúc chuỗi
                    };

                    // Tính toán Checksum
                    byte sum0, sum1;
                    this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                    // Chuẩn bị gửi dữ liệu
                    List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                    // Gửi dữ liệu qua cổng COM
                    lock (this.ComportLock)
                    {
                        this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                        short wordValue = this.ReadIntAck();
                        values.Add(wordValue);
                        result = true;
                    }
                }
            }
            return result;
        }
        public bool ReadMultiDWord(DeviceCode device, int address, int number, out List<int> values)
        {
            values = new List<int>();
            bool result = false;

            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                    throw new Exception("Please check the status of the serial port!");

                for (int i = 0; i < number; i++)
                {
                    // Chuyển đổi địa chỉ dựa vào Devicecode
                    byte[] outAddress;
                    switch (device)
                    {
                        case DeviceCode.D:
                            this.AddressToAscii(FX3_SC09.REGISTER_TYPE.D, address + (i * 2), out outAddress);
                            break;
                        default:
                            throw new Exception("Unsupported device type");
                    }

                    // Tạo gói tin
                    byte[] numArray = new byte[9]
                    {
                        (byte)2,            // STX - Bắt đầu chuỗi
                        (byte)48,           // Lệnh đọc
                        outAddress[3],      // Địa chỉ (MSB -> LSB)
                        outAddress[2],
                        outAddress[1],
                        outAddress[0],
                        (byte)48,           // Số lượng đọc (04 byte cho DINT)
                        (byte)52,
                        (byte)3             // ETX - Kết thúc chuỗi
                    };

                    // Tính toán Checksum
                    byte sum0, sum1;
                    this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                    // Chuẩn bị gửi dữ liệu
                    List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                    // Gửi dữ liệu qua cổng COM
                    lock (this.ComportLock)
                    {
                        this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                        int dwordValue = this.ReadDintAck();
                        values.Add(dwordValue);
                        result = true;
                    }
                }
            }
            return result;
        }
        public bool ReadDWord(DeviceCode device, int address, out int value)
        {
            bool Result = false;
            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                    throw new Exception("Please check the status of the serial port!");

                // Chuyển đổi địa chỉ dựa vào Devicecode
                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.D:
                        this.AddressToAscii(FX3_SC09.REGISTER_TYPE.D, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                // Tạo gói tin
                byte[] numArray = new byte[9]
                {
                    (byte)2,            // STX - Bắt đầu chuỗi
                    (byte)48,           // Lệnh đọc
                    outAddress[3],      // Địa chỉ (MSB -> LSB)
                    outAddress[2],
                    outAddress[1],
                    outAddress[0],
                    (byte)48,           // Số lượng đọc (04 byte cho DINT)
                    (byte)52,
                    (byte)3             // ETX - Kết thúc chuỗi
                };

                // Tính toán Checksum
                byte sum0, sum1;
                this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);

                // Chuẩn bị gửi dữ liệu
                List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                // Gửi dữ liệu qua cổng COM
                lock (this.ComportLock)
                {
                    this.Comport.Write(byteList.ToArray(), 0, byteList.Count);
                    value = this.ReadDintAck();
                    Result = true;
                }
            }
            return Result;
        }

        public bool ReadMultiBit(DeviceCode device, int address, int number, out List<bool> values)
        {
            values = new List<bool>();
            lock (PLCLock)
            {
                if (this.Comport == null || !this.Comport.IsOpen)
                {
                    Console.WriteLine("Serial port is not open");
                    return false;
                }

                if (number <= 0 || number > 64)
                    throw new ArgumentException("Number must be between 1 and 64.");

                switch (device)
                {
                    case DeviceCode.X:
                        if (address < 0 || address + number - 1 > 0x1FF)
                            throw new ArgumentException("X address out of range (0-377 octal)");
                        break;
                    case DeviceCode.Y:
                        if (address < 0 || address + number - 1 > 0x1FF)
                            throw new ArgumentException("Y address out of range (0-377 octal)");
                        break;
                    case DeviceCode.M:
                        if (address < 0 || address + number - 1 > 8191)
                            throw new ArgumentException("M address out of range (0-8191)");
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                int byteCount = (number + 7) / 8;
                string points = byteCount.ToString("X2").ToUpper();

                byte[] outAddress;
                switch (device)
                {
                    case DeviceCode.X:
                        this.AddressToAscii(REGISTER_TYPE.RX, address, out outAddress);
                        break;
                    case DeviceCode.Y:
                        this.AddressToAscii(REGISTER_TYPE.RY, address, out outAddress);
                        break;
                    case DeviceCode.M:
                        this.AddressToAscii(REGISTER_TYPE.RM, address, out outAddress);
                        break;
                    default:
                        throw new Exception("Unsupported device type");
                }

                byte[] numArray = new byte[9]
                {
                (byte)2,
                (byte)48,
                outAddress[3], outAddress[2],
                outAddress[1], outAddress[0],
                (byte)points[0], (byte)points[1],
                (byte)3
                };

                byte sum0, sum1;
                this.CheckSum(numArray, 1, numArray.Length - 1, out sum0, out sum1);
                List<byte> byteList = new List<byte>(numArray) { sum1, sum0 };

                lock (this.ComportLock)
                {
                    try
                    {
                        this.Comport.DiscardInBuffer();
                        this.Comport.DiscardOutBuffer();
                        Console.WriteLine($"Sending ReadMultiBit: {BitConverter.ToString(byteList.ToArray())}");
                        this.Comport.Write(byteList.ToArray(), 0, byteList.Count);

                        long ticks = DateTime.Now.Ticks;
                        List<byte> response = new List<byte>();
                        bool startFlag = false;

                        int expectedLength = 1 + 2 * byteCount + 1;
                        while (TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds <= 3000)
                        {
                            if (this.Comport.BytesToRead > 0)
                            {
                                int num = this.Comport.ReadByte();
                                if (num == 2)
                                    startFlag = true;
                                if (startFlag)
                                {
                                    response.Add((byte)num);
                                    if (response.Count >= expectedLength && num == 3)
                                        break;
                                }
                            }
                        }

                        Console.WriteLine($"Received ReadMultiBit: {BitConverter.ToString(response.ToArray())}");
                        if (response.Count < expectedLength)
                            throw new Exception($"Communication timeout: Expected {expectedLength} bytes, received {response.Count}");

                        if (response[0] == 2 && response[response.Count - 1] == 3)
                        {
                            int bitOffset = address % 8;
                            int bitsRead = 0;
                            for (int i = 0; i < byteCount && bitsRead < number; i++)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append((char)response[1 + i * 2]);
                                sb.Append((char)response[2 + i * 2]);
                                string hexValue = sb.ToString();
                                byte byteValue;
                                try
                                {
                                    byteValue = Convert.ToByte(hexValue, 16);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error parsing byte {i} (hex: {hexValue}): {ex.Message}");
                                    throw new Exception($"Invalid byte data at index {i}: {hexValue}");
                                }

                                for (int bit = bitOffset; bit < 8 && bitsRead < number; bit++, bitsRead++)
                                {
                                    bool bitValue = (byteValue & (1 << bit)) != 0;
                                    values.Add(bitValue);
                                    Console.WriteLine($"Bit {bitsRead} (Y{address + bitsRead}) = {bitValue}");
                                }
                                bitOffset = 0;
                            }
                            return true;
                        }
                        else if (response[0] == 0x15)
                        {
                            string errorCode = response.Count > 1 ? $"0x{response[1]:X2}" : "unknown";
                            throw new Exception($"PLC returned NAK with error code: {errorCode}");
                        }
                        throw new Exception($"Invalid response from PLC: {BitConverter.ToString(response.ToArray())}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading multi-bit: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool IsOpen() => this.Comport.IsOpen;

        private void CheckSum(
          IEnumerable<byte> byteArray,
          int nStartPos,
          int nEndPos,
          out byte sum0,
          out byte sum1)
        {
            if (nEndPos > byteArray.Count<byte>() || nStartPos < 0)
                throw new Exception("The starting range of the array is incorrect, please check carefully! ");
            List<byte> source = new List<byte>();
            for (int index = nStartPos; index <= nEndPos; ++index)
                source.Add(byteArray.ElementAt<byte>(index));
            int num1 = source.Count<byte>();
            int num2 = 0;
            for (int index = 0; index < num1; ++index)
                num2 += (int)source.ElementAt<byte>(index);
            string upper = string.Format("{0:X2}", (object)(num2 & (int)byte.MaxValue)).ToUpper();
            sum0 = (byte)upper[1];
            sum1 = (byte)upper[0];
        }

        private void AddressToAscii(
          FX3_SC09.REGISTER_TYPE nType,
          int dwAddress,
          out byte[] outAddress)
        {
            outAddress = new byte[4];
            switch (nType)
            {
                case FX3_SC09.REGISTER_TYPE.RX:
                    dwAddress = dwAddress / 8 + 128;
                    string upper1 = string.Format("{0:X4}", (object)dwAddress).ToUpper();
                    for (int index = 0; index < 4; ++index)
                        outAddress[3 - index] = (byte)upper1[index];
                    break;
                case FX3_SC09.REGISTER_TYPE.RY:
                    dwAddress = dwAddress / 8 + 160;
                    string upper2 = string.Format("{0:X4}", (object)dwAddress).ToUpper();
                    for (int index = 0; index < 4; ++index)
                        outAddress[3 - index] = (byte)upper2[index];
                    break;
                case FX3_SC09.REGISTER_TYPE.RM:
                    dwAddress = dwAddress / 8 + 256;
                    string upper3 = string.Format("{0:X4}", (object)dwAddress).ToUpper();
                    for (int index = 0; index < 4; ++index)
                        outAddress[3 - index] = (byte)upper3[index];
                    break;
                case FX3_SC09.REGISTER_TYPE.WY:
                    string str = "05" + this.ConvertDataFx(dwAddress);
                    for (int index = 0; index < 4; ++index)
                        outAddress[3 - index] = (byte)str[index];
                    break;
                case FX3_SC09.REGISTER_TYPE.WM:
                    dwAddress += 2048;
                    string upper4 = string.Format("{0:X4}", (object)dwAddress).ToUpper();
                    for (int index = 0; index < 4; ++index)
                        outAddress[3 - index] = (byte)upper4[index];
                    break;
                case FX3_SC09.REGISTER_TYPE.D:
                    dwAddress = dwAddress * 2 + 4096;
                    string upper5 = string.Format("{0:X4}", (object)dwAddress).ToUpper();
                    for (int index = 0; index < 4; ++index)
                        outAddress[3 - index] = (byte)upper5[index];
                    break;
            }
        }


        private void Dec2Ascii(byte nData, out byte ascii0, out byte ascii1)
        {
            string str = string.Format("{0:X2}", (object)nData);
            ascii0 = (byte)str[1];
            ascii1 = (byte)str[0];
        }
        private bool ReadVoidAck()
        {
            long ticks = DateTime.Now.Ticks;
            do
            {
                if (this.Comport.BytesToRead > 0)
                    goto label_1;
            }
            while (TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds <= 2000.0);
            goto label_5;
        label_1:
            return this.Comport.ReadByte() == 6;
        label_5:
            throw new Exception("Communication timeout");
        }
        private short ReadIntAck()
        {
            long ticks = DateTime.Now.Ticks;
            List<byte> byteList = new List<byte>();
            bool flag = false;
            do
            {
                if (this.Comport.BytesToRead > 0)
                {
                    int num = this.Comport.ReadByte();
                    if (num == 2)
                        flag = true;
                    if (flag)
                    {
                        byteList.Add((byte)num);
                        if (byteList.Count == 6 && num == 3)
                            goto label_5;
                    }
                }
            }
            while (TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds <= 2000.0);
            goto label_8;
        label_5:
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append((char)byteList[3]);
            stringBuilder.Append((char)byteList[4]);
            stringBuilder.Append((char)byteList[1]);
            stringBuilder.Append((char)byteList[2]);
            return Convert.ToInt16(stringBuilder.ToString(), 16);
        label_8:
            throw new Exception("Communication timeout");
        }
        private int ReadDintAck()
        {
            long ticks = DateTime.Now.Ticks;
            List<byte> byteList = new List<byte>();
            bool flag = false;
            do
            {
                if (this.Comport.BytesToRead > 0)
                {
                    int num = this.Comport.ReadByte();
                    if (num == 2)
                        flag = true;
                    if (flag)
                    {
                        byteList.Add((byte)num);
                        if (byteList.Count == 10 && num == 3)
                            goto label_5;
                    }
                }
            }
            while (TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds <= 2000.0);
            goto label_8;
        label_5:
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append((char)byteList[7]);
            stringBuilder.Append((char)byteList[8]);
            stringBuilder.Append((char)byteList[5]);
            stringBuilder.Append((char)byteList[6]);
            stringBuilder.Append((char)byteList[3]);
            stringBuilder.Append((char)byteList[4]);
            stringBuilder.Append((char)byteList[1]);
            stringBuilder.Append((char)byteList[2]);
            return Convert.ToInt32(stringBuilder.ToString(), 16);
        label_8:
            throw new Exception("Communication timeout");
        }
        private bool Set_or_Reset_Bit_Ack()
        {
            long ticks = DateTime.Now.Ticks;
            List<byte> byteList = new List<byte>();
            do
            {
                if (this.Comport.BytesToRead > 0 && this.Comport.ReadByte() == 6)
                    goto label_1;
            }
            while (TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds <= 2000.0);
            goto label_3;
        label_1:
            return true;
        label_3:
            throw new Exception("Communication timeout");
        }

        private bool Read_Bit_Ack(int BitCheck)
        {
            long ticks = DateTime.Now.Ticks;
            List<byte> byteList = new List<byte>();
            bool startFlag = false;

            // Chờ dữ liệu trả về tối đa 2000ms
            while (TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds <= 2000)
            {
                if (this.Comport.BytesToRead > 0)
                {
                    int receivedByte = this.Comport.ReadByte();

                    // Đánh dấu khi nhận được byte bắt đầu (STX = 2)
                    if (receivedByte == 2)
                        startFlag = true;

                    if (startFlag)
                    {
                        byteList.Add((byte)receivedByte);

                        // Kiểm tra đủ 4 byte phản hồi và kết thúc bằng ETX = 3
                        if (byteList.Count == 4 && receivedByte == 3)
                            break;
                    }
                }
            }

            // Kiểm tra timeout nếu không nhận đủ dữ liệu
            if (byteList.Count < 4)
                throw new Exception("Communication timeout");

            // Giải mã dữ liệu phản hồi (byte 1 và byte 2 là dữ liệu trả về)
            int response = Convert.ToInt32($"{(char)byteList[1]}{(char)byteList[2]}", 16);
            string binaryData = Convert.ToString(response, 2).PadLeft(8, '0');

            // Kiểm tra bit cụ thể (đảm bảo lấy đúng bit theo thứ tự từ trái sang phải)
            return binaryData[7 - (BitCheck % 8)] == '1';
        }

        public string ConvertDataFx(int buff)
        {
            int num1 = 0;
            if (buff > 8)
                num1 = buff / 16;
            int num2 = num1 <= 0 ? buff - 10 * num1 : buff - 10 * (num1 + 1);
            string str = "";
            switch (num2)
            {
                case 0:
                    str = "0";
                    break;
                case 1:
                    str = "1";
                    break;
                case 2:
                    str = "2";
                    break;
                case 3:
                    str = "3";
                    break;
                case 4:
                    str = "4";
                    break;
                case 5:
                    str = "5";
                    break;
                case 6:
                    str = "6";
                    break;
                case 7:
                    str = "7";
                    break;
                case 10:
                    str = "8";
                    break;
                case 11:
                    str = "9";
                    break;
                case 12:
                    str = "A";
                    break;
                case 13:
                    str = "B";
                    break;
                case 14:
                    str = "C";
                    break;
                case 15:
                    str = "D";
                    break;
                case 16:
                    str = "E";
                    break;
                case 17:
                    str = "F";
                    break;
            }
            return num1.ToString() + str;
        }


        private enum REGISTER_TYPE
        {
            RX,
            RY,
            RM,
            WY,
            WM,
            D,

        }

    }
}
