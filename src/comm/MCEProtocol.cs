using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections;
using System.Threading;
using AutoLaserCuttingInput;

namespace VisionInspection
{
    class MCProtocol
    {
        ASCIIEncoding asc = new ASCIIEncoding();
        public enum DeviceCode { X, Y, M, D, }
        MySocket client = new MySocket();
        int monitoring_timer;
        bool isConnected = false;
        private static Object McLock = new object();
        private MyLogger logger = new MyLogger("MCProtocol");
        private bool Binary { get; set; } = false;

        public bool IsConnected()
        {
            return isConnected;
        }

        public MCProtocol()
        {
            monitoring_timer = 10;
            isConnected = false;
            client.SocketConnected += onConnected;
            client.SocketDisconnected += onDisConnected;
        }

        public delegate void EventConnectedHandler();
        public event EventConnectedHandler PLCConnected;
        public event EventConnectedHandler PLCDisConnected;
        public void Connect(string ip, int port, bool Mode)
        {
            this.Binary = Mode;
            Thread.Sleep(100);
            client.Connect(ip, port);
        }
        public void Disconnect()
        {
            if (!IsConnected())
                return;
            client.Disconnect();
        }



        private void onConnected()
        {
            isConnected = true;
            if (PLCConnected != null)
                PLCConnected();
        }
        private void onDisConnected()
        {
            isConnected = false;
            if (PLCDisConnected != null)
                PLCDisConnected();
        }

        #region READ
        public delegate void EventReceiveHandler(int[] data);
        public event EventReceiveHandler PLCReceived;

        public List<bool> ReadMultiBits(DataType.Devicecode dev, uint startPoint, uint length)
        {
            List<bool> boolskq = new List<bool> { };
            for (int i = 0; i < 7800; i++)
            {
                boolskq.Add(false);
            }
            if (isConnected == false)
                return boolskq;

            lock (McLock)
            {
                try
                {
                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);

                        DataSend.Add(0x0C);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x04);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(startPoint & 0xff));
                        DataSend.Add((Byte)(startPoint >> 8));
                        DataSend.Add((Byte)(startPoint >> 16));
                        //DataSend.Add((Byte)(startPoint >> 32));
                        // Device Code
                        DataSend.AddRange(SetDevice(dev));
                        // Number Point
                        DataSend.Add((Byte)(length & 0xff));
                        DataSend.Add((Byte)(length >> 8));
                        client.Send(DataSend.ToArray());
                        Thread.Sleep(0);
                        byte[] dataBinary = client.Receive();
                        int[] kq = ReceivedBinary(dataBinary);
                        for (int i = 0; i < kq.Length; i++)
                        {
                            if (kq[i] != 0)
                            {
                                boolskq[i] = true;
                            }
                            else
                            {
                                boolskq[i] = false;
                            }
                        }

                    }
                    else
                    {
                        List<byte> dat = new List<byte>();
                        // SubHeader + Access Route + Length
                        string str = "500000FF03FF000018";
                        ASCIIEncoding encode = new ASCIIEncoding();
                        dat.AddRange(encode.GetBytes(str));
                        // Monitoring
                        str = Convert.ToString(monitoring_timer, 10).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Command + SubCommand
                        str = "04010000";
                        dat.AddRange(encode.GetBytes(str));
                        dat.AddRange(SetDevice(dev));
                        // Start Point
                        str = Convert.ToString(startPoint, 10).PadLeft(6, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Number ò Point
                        str = Convert.ToString(length, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        client.Send(dat.ToArray());
                        Thread.Sleep(0);
                        byte[] data = client.Receive();
                        int[] kq = Received(data);
                        bool[] bools = new bool[16];
                        boolskq = new List<bool> { };
                        foreach (int i in kq)
                        {
                            var source = i;
                            var bitArray = new BitArray(new[] { source });

                            var target = new bool[32];
                            bitArray.CopyTo(target, 0);
                            for (int j = 0; j < 16; j++)
                            {
                                boolskq.Add(target[j]);
                            }
                        }
                        if (boolskq.Count < 16)
                        {
                            for (int i = 0; i < 7560; i++)
                            {
                                boolskq.Add(false);
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    logger.Create("PLC Read Multi Bits Err" + ex.ToString());
                    for (int i = 0; i < 7560; i++)
                    {
                        boolskq.Add(false);
                    }
                }
            }
            // Command 0401
            // Subcommand 0000


            return boolskq;

        }
        public int[] ReadSingleWord(DataType.Devicecode dev, uint startPoint, uint length)
        {
            int[] kq = new int[960];
            for (int i = 0; i < 960; i++)
            {
                kq[i] = 0;
            }
            if (isConnected == false)
                return kq;
            lock (McLock)
            {
                try
                {

                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00); 
                        DataSend.Add(0x0C);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x04);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(startPoint & 0xff));
                        DataSend.Add((Byte)(startPoint >> 8));
                        DataSend.Add((Byte)(startPoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(dev));
                        // Number Point
                        DataSend.Add((Byte)(length & 0xff));
                        DataSend.Add((Byte)(length >> 8));
                        client.Send(DataSend.ToArray());
                        Thread.Sleep(0);
                        byte[] dataBinary = client.Receive();
                        kq = ReceivedBinaryOneWord(dataBinary);

                    }
                    else
                    {
                        List<byte> dat = new List<byte>();
                        // SubHeader + Access Route + Length
                        string str = "500000FF03FF000018";
                        ASCIIEncoding encode = new ASCIIEncoding();
                        dat.AddRange(encode.GetBytes(str));
                        // Monitoring
                        str = Convert.ToString(monitoring_timer, 10).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Command + SubCommand
                        str = "04010000";
                        dat.AddRange(encode.GetBytes(str));
                        dat.AddRange(SetDevice(dev));
                        // Start Point
                        str = Convert.ToString(startPoint, 10).PadLeft(6, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Number ò Point
                        str = Convert.ToString(length, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        client.Send(dat.ToArray());
                        Thread.Sleep(0);
                        byte[] data = client.Receive();
                        kq = Received(data);
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("PLC ReadSingleWord Err" + ex.ToString());
                    kq = null;
                }
            }
            // Command 0401
            // Subcommand 0000
            return kq;
            //str = encode.GetString(dat.ToArray());
        }
        public int[] ReadDoubleWord(DataType.Devicecode dev, uint startPoint, uint length)
        {
            int[] kq = new int[940];
            for (int i = 0; i < 940; i++)
            {
                kq[i] = 0;
            }
            if (isConnected == false)
                return kq;
            lock (McLock)
            {
                try
                {

                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00); 
                        DataSend.Add(0x0C);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x04);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(startPoint & 0xff));
                        DataSend.Add((Byte)(startPoint >> 8));
                        DataSend.Add((Byte)(startPoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(dev));
                        // Number Point
                        DataSend.Add((Byte)((length * 2) & 0xff));
                        DataSend.Add((Byte)((length * 2) >> 8));
                        client.Send(DataSend.ToArray());
                        Thread.Sleep(0);
                        byte[] dataBinary = client.Receive();
                        kq = ReceivedBinaryTwoWord(dataBinary);

                    }
                    else
                    {
                        // Command 0401
                        // Subcommand 0000
                        List<byte> dat = new List<byte>();
                        // SubHeader + Access Route + Length
                        string str = "500000FF03FF000018";
                        ASCIIEncoding encode = new ASCIIEncoding();
                        dat.AddRange(encode.GetBytes(str));
                        // Monitoring
                        str = Convert.ToString(monitoring_timer, 10).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Command + SubCommand
                        str = "04010000";
                        dat.AddRange(encode.GetBytes(str));
                        dat.AddRange(SetDevice(dev));
                        // Start Point
                        str = Convert.ToString(startPoint, 10).PadLeft(6, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Number ò Point
                        str = Convert.ToString(length * 2, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        client.Send(dat.ToArray());
                        Thread.Sleep(0);
                        byte[] data = client.Receive();
                        kq = ReceivedDoubleWord(data);
                    }

                }
                catch (Exception ex)
                {
                    logger.Create("PLC Read Double Word Err" + ex.ToString());
                }
            }
            return kq;
            //str = encode.GetString(dat.ToArray());
        }

        private int[] ReceivedBinary(byte[] data)
        {
            if (data[0] != 0xd0 || data[1] != 0x00)
            {
                logger.Create("SubHeader Error ");
                return null;
            }
            if (data[2] != 0x00)
            {
                logger.Create("Network No Error ");
                return null;
            }
            if (data[3] != 0xff)
            {
                logger.Create("PC No Error ");
                return null;
            }
            if (data[4] != 0xff)
            {
                logger.Create("Request destination module I/ O No Error ");
                return null;
            }
            if (data[5] != 0x03)
            {
                logger.Create("Request destination module I/ O No Error ");
                return null;
            }
            if (data[6] != 0x00)
            {
                logger.Create("Request destination module station No Error ");
                return null;
            }

            //int[] ret = new int[960];
            int[] ret = new int[7800];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = 0;
            }
            for (int i = 0; i < ret.Length; i++)
            {
                Int32 byteIdx = 11 + i / 8;
                int bitPos = i % 8;
                if ((data[byteIdx] & (1 << bitPos)) != 0)
                {
                    ret[i] = 1;
                }
                else
                {
                    ret[i] = 0;
                }
            }
            return ret;
        }
        private int[] ReceivedBinaryOneWord(byte[] data)
        {
            if (data[0] != 0xd0 || data[1] != 0x00)
            {
                logger.Create("SubHeader Error ");
                return null;
            }
            if (data[2] != 0x00)
            {
                logger.Create("Network No Error ");
                return null;
            }
            if (data[3] != 0xff)
            {
                logger.Create("PC No Error ");
                return null;
            }
            if (data[4] != 0xff)
            {
                logger.Create("Request destination module I/ O No Error ");
                return null;
            }
            if (data[5] != 0x03)
            {
                logger.Create("Request destination module I/ O No Error ");
                return null;
            }
            if (data[6] != 0x00)
            {
                logger.Create("Request destination module station No Error ");
                return null;
            }
            int[] ret = new int[940];
            ret[0] = (UInt16)(data[11] | (data[12] << 8));
            return ret;
        }
        private int[] ReceivedBinaryTwoWord(byte[] data)
        {
            int[] ret = new int[940];
            int[] da = new int[4000];
            try
            {
                if (data[0] != 0xd0 || data[1] != 0x00)
                {
                    logger.Create("SubHeader Error ");
                    return null;
                }
                if (data[2] != 0x00)
                {
                    logger.Create("Network No Error ");
                    return null;
                }
                if (data[3] != 0xff)
                {
                    logger.Create("PC No Error ");
                    return null;
                }
                if (data[4] != 0xff)
                {
                    logger.Create("Request destination module I/ O No Error ");
                    return null;
                }
                if (data[5] != 0x03)
                {
                    logger.Create("Request destination module I/ O No Error ");
                    return null;
                }
                if (data[6] != 0x00)
                {
                    logger.Create("Request destination module station No Error ");
                    return null;
                }

                int sum = 0;
                int sum1 = 0;
                int sum2 = 1;
                for (int i = 0; i < 940; i++)
                {
                    da[i + sum1] = (Int32)(data[13 + sum] | (data[14 + sum] << 8));
                    da[i + sum2] = (Int32)(data[11 + sum] | (data[12 + sum] << 8));
                    ret[i] = (Int32)(da[i + sum2] | (da[i + sum1] << 16));
                    sum1 += 1;
                    sum2 += 1;
                    sum += 4;

                }
            }
            catch (Exception ex)
            {
                logger.Create("" + ex.Message);
            }

            //da[0] = (Int32)(data[13] | (data[14] << 8));
            //da[1] = (Int32)(data[11] | (data[12] << 8));
            //ret[0] = (Int32)(da[1] | (da[0] << 16));

            return ret;

        }

        private int[] Received(byte[] data)
        {
            int[] dtOut = new int[940];
            for (int i = 0; i < 940; i++)
            {
                dtOut[i] = 0;
            }
            if (isConnected == false)
                return dtOut;
            if (data == null)
                return dtOut;
            try
            {
                //int length = data[14] - 0x30;
                ASCIIEncoding encode = new ASCIIEncoding();

                string str = encode.GetString(data, 14, 4);
                int length = Convert.ToInt32(str, 16);

                str = encode.GetString(data, 18, 4);
                int endcode = Convert.ToInt32(str, 16);

                if (length <= 4)
                    return dtOut;

                //byte[] dtOut = new byte[length - 4];
                //Array.Copy(data, 22, dtOut, 0, dtOut.Length);
                //PLCReceived(dtOut);

                dtOut = new int[(length / 4 - 1)];

                for (int i = 0; i < dtOut.Length; i++)
                {
                    str = encode.GetString(data, 22 + i * 4, 4);
                    if (str.Substring(0, 1) == "F")
                        dtOut[i] = Convert.ToInt32("FFFF" + str, 16);
                    else
                        dtOut[i] = Convert.ToInt32(str, 16);
                }
                return dtOut;
            }
            catch (Exception ex)
            {
                logger.Create("Received Data Err" + ex.ToString());
                return dtOut;
            }

        }
        private int[] ReceivedDoubleWord(byte[] data)
        {
            int[] dtOut = new int[940];
            for (int i = 0; i < 940; i++)
            {
                dtOut[i] = 0;
            }
            if (isConnected == false)
                return dtOut;
            if (data == null)
                return dtOut;
            try
            {
                //int length = data[14] - 0x30;
                ASCIIEncoding encode = new ASCIIEncoding();

                string str = encode.GetString(data, 14, 4);
                int length = Convert.ToInt32(str, 16);

                str = encode.GetString(data, 18, 4);
                int endcode = Convert.ToInt32(str, 16);

                if (length <= 4)
                    return dtOut;

                //byte[] dtOut = new byte[length - 4];
                //Array.Copy(data, 22, dtOut, 0, dtOut.Length);
                //PLCReceived(dtOut);

                dtOut = new int[(length / 4 - 1) / 2];

                for (int i = 0; i < dtOut.Length; i++)

                {

                    string strTg = encode.GetString(data, 22 + i * 8, 8);
                    str = strTg.Substring(4, 4) + strTg.Substring(0, 4);
                    dtOut[i] = Convert.ToInt32(str, 16);
                }
                return dtOut;
            }
            catch (Exception ex)
            {
                logger.Create("Received Data Err" + ex.ToString());
                return dtOut;
            }

        }

        public void SetBit(DataType.Devicecode dev, DataType.Devicecode devBinary, int devicePoint)
        {
            lock (McLock)
            {
                try
                {
                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00); 
                        DataSend.Add(0x0D);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x14);
                        DataSend.Add(0x01);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(devicePoint & 0xff));
                        DataSend.Add((Byte)(devicePoint >> 8));
                        DataSend.Add((Byte)(devicePoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(devBinary));
                        // Number Point
                        DataSend.Add(0x01);
                        DataSend.Add(0x00);
                        //Value
                        DataSend.Add(0x10);
                        client.Send(DataSend.ToArray());
                        client.Receive();
                    }
                    else
                    {
                        List<byte> lstDataSend = InitialData();

                        lstDataSend[14] = 0x30;
                        lstDataSend[15] = 0x30;
                        lstDataSend[16] = 0x31;
                        lstDataSend[17] = 0x39;

                        // Command (22-25) : 1401 
                        lstDataSend.AddRange(new List<byte> { 0x31, 0x34, 0x30, 0x31 });
                        // Subcommand (26-29): 0001
                        lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x31 });

                        // DeviceName (30-31)
                        lstDataSend.AddRange(shortToArrByte(dev));

                        // Device Number (32-37) - Cập nhật từ textbox
                        string strDevicePoint = devicePoint.ToString().ToUpper().PadLeft(6, '0');
                        lstDataSend.AddRange(asc.GetBytes(strDevicePoint));

                        // Number of DevicePoint (4 byte) 38-41: 0001
                        lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x31 });

                        //Value
                        //lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30 });
                        lstDataSend.Add(0x31);

                        client.Send(lstDataSend.ToArray());
                        client.Receive();
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("PLC Set Bit Err" + ex.ToString());
                }
            }


        }
        public void SetMultiBit(DataType.Devicecode dev, DataType.Devicecode devBinary, int devicePoint, uint length, bool[] data)
        {
            lock (McLock)
            {
                try
                {
                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00); 
                        DataSend.Add(0x0D);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x14);
                        DataSend.Add(0x01);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(devicePoint & 0xff));
                        DataSend.Add((Byte)(devicePoint >> 8));
                        DataSend.Add((Byte)(devicePoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(devBinary));
                        // Number Point
                        DataSend.Add(0x08);
                        DataSend.Add(0x00);
                        //Value
                        for(int i = 0; i<4; i++)
                        {
                             DataSend.Add(0x11);
                        }
                        
                        client.Send(DataSend.ToArray());
                        client.Receive();
                    }
                    else
                    {
                        List<byte> lstDataSend = InitialData();

                        lstDataSend[14] = 0x30;
                        lstDataSend[15] = 0x30;
                        lstDataSend[16] = 0x31;
                        lstDataSend[17] = 0x39;

                        // Command (22-25) : 1401 
                        lstDataSend.AddRange(new List<byte> { 0x31, 0x34, 0x30, 0x31 });
                        // Subcommand (26-29): 0001
                        lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x31 });

                        // DeviceName (30-31)
                        lstDataSend.AddRange(shortToArrByte(dev));

                        // Device Number (32-37) - Cập nhật từ textbox
                        string strDevicePoint = devicePoint.ToString().ToUpper().PadLeft(6, '0');
                        lstDataSend.AddRange(asc.GetBytes(strDevicePoint));

                        // Number of DevicePoint (4 byte) 38-41: 0001
                        lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x31 });

                        //Value
                        //lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30 });
                        lstDataSend.Add(0x31);

                        client.Send(lstDataSend.ToArray());
                        client.Receive();
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("PLC Set Bit Err" + ex.ToString());
                }
            }


        }
        public void RstBit(DataType.Devicecode dev, DataType.Devicecode devBinary, int devicePoint)
        {
            lock (McLock)
            {
                try
                {
                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00); 
                        DataSend.Add(0x0D);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x14);
                        DataSend.Add(0x01);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(devicePoint & 0xff));
                        DataSend.Add((Byte)(devicePoint >> 8));
                        DataSend.Add((Byte)(devicePoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(devBinary));
                        // Number Point
                        DataSend.Add(0x01);
                        DataSend.Add(0x00);
                        //Value
                        DataSend.Add(0x00);
                        client.Send(DataSend.ToArray());
                        client.Receive();
                    }
                    else
                    {
                        List<byte> lstDataSend = InitialData();

                        lstDataSend[14] = 0x30;
                        lstDataSend[15] = 0x30;
                        lstDataSend[16] = 0x31;
                        lstDataSend[17] = 0x39;

                        // Command (22-25) : 1401 
                        lstDataSend.AddRange(new List<byte> { 0x31, 0x34, 0x30, 0x31 });
                        // Subcommand (26-29): 0001
                        lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x31 });

                        // DeviceName (30-31)
                        lstDataSend.AddRange(shortToArrByte(dev));

                        // Device Number (32-37) - Cập nhật từ textbox
                        string strDevicePoint = devicePoint.ToString().ToUpper().PadLeft(6, '0');
                        lstDataSend.AddRange(asc.GetBytes(strDevicePoint));

                        // Number of DevicePoint (4 byte) 38-41: 0001
                        lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x31 });

                        //Value
                        //lstDataSend.AddRange(new List<byte> { 0x30, 0x30, 0x30 });
                        lstDataSend.Add(0x30);

                        client.Send(lstDataSend.ToArray());
                        client.Receive();
                    }

                }
                catch (Exception ex)
                {
                    logger.Create("PLC Rst Bit Err" + ex.ToString());
                }
            }


        }

        public List<byte> InitialData()
        {
            List<byte> lstData = new List<byte>();

            //Subheader : 5000 :  0 - 3
            lstData.AddRange(new List<byte> { 0x35, 0x30, 0x30, 0x30 });

            //Access Route (10 byte):(00FF03FF00) 4-13

            lstData.AddRange(new List<byte> { 0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30 });

            // Data length :(4 byte): 14-17

            lstData.AddRange(new List<byte> { 0x30, 0x30, 0x30, 0x30 });

            // Monitor Timer : (4 byte): 18-21

            lstData.AddRange(new List<byte> { 0x30, 0x30, 0x31, 0x30 });

            return lstData;
        }


        #endregion
        #region WRITE
        public void WriteBit(DataType.Devicecode dev, uint startPoint, int[] data)  //data[0]
        {
            lock (McLock)
            {
                try
                {
                    if (isConnected == false)
                        return;

                    /*string str = "
                     * 5000         Subheader
                     * 00FF03FF00   Access route
                     * 0020         Lenght
                     * 0010         Monitoring
                     * 1401         Command
                     * 0000         Subcommand
                     * D*           Device Code
                     * 000100       Start point
                     * 0002         Number point
                     * 12345678";   Data
                    */
                    List<byte> dat = new List<byte>();
                    // SubHeader + Access Route
                    string str = "500000FF03FF00";
                    ASCIIEncoding encode = new ASCIIEncoding();
                    dat.AddRange(encode.GetBytes(str));
                    // Length
                    int length = 24 + 1 * data.Length;
                    str = Convert.ToString(length, 16).PadLeft(4, '0').ToUpper();
                    dat.AddRange(encode.GetBytes(str));
                    // Monitoring
                    str = Convert.ToString(monitoring_timer, 10).PadLeft(4, '0').ToUpper();
                    dat.AddRange(encode.GetBytes(str));
                    // Command + SubCommand
                    str = "14010000";
                    dat.AddRange(encode.GetBytes(str));
                    dat.AddRange(SetDevice(dev));
                    // Start Point
                    str = Convert.ToString(startPoint, 10).PadLeft(6, '0').ToUpper();
                    dat.AddRange(encode.GetBytes(str));
                    // Number of Point
                    str = Convert.ToString(data.Length, 16).PadLeft(4, '0').ToUpper();
                    dat.AddRange(encode.GetBytes(str));
                    // Add data
                    foreach (int tmp in data)
                    {
                        str = Convert.ToString(tmp).ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                    }
                    client.Send(dat.ToArray());
                    //str = encode.GetString(dat.ToArray());
                }
                catch (Exception ex)
                {
                    logger.Create("Write Bit Er" + ex.ToString());
                }
            }

        }

        public bool WriteSingleWord(DataType.Devicecode dev, uint startPoint, int[] data)  //data[0]
        {
            bool ret = false;
            if (isConnected == false)
                return false;
            lock (McLock)
            {
                try
                {
                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00); 
                        DataSend.Add(0x0E);
                        DataSend.Add(0x00);
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x14);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(startPoint & 0xff));
                        DataSend.Add((Byte)(startPoint >> 8));
                        DataSend.Add((Byte)(startPoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(dev));
                        // Number Point
                        DataSend.Add(0x01);
                        DataSend.Add(0x00);
                        // Value
                        DataSend.Add((Byte)(data[0] & 0xff));
                        DataSend.Add((Byte)(data[0] >> 8));
                        client.Send(DataSend.ToArray());
                        Thread.Sleep(0);
                        client.Receive();
                        ret = true;
                    }
                    else
                    {
                        /*string str = "
* 5000         Subheader
* 00FF03FF00   Access route
* 0020         Lenght
* 0010         Monitoring
* 1401         Command
* 0000         Subcommand
* D*           Device Code
* 000100       Start point
* 0002         Number point
* 12345678";   Data
*/
                        List<byte> dat = new List<byte>();
                        // SubHeader + Access Route
                        string str = "500000FF03FF00";
                        ASCIIEncoding encode = new ASCIIEncoding();
                        dat.AddRange(encode.GetBytes(str));
                        // Length
                        int length = 24 + 4 * data.Length;
                        str = Convert.ToString(length, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Monitoring
                        str = Convert.ToString(monitoring_timer, 10).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Command + SubCommand
                        str = "14010000";
                        dat.AddRange(encode.GetBytes(str));
                        dat.AddRange(SetDevice(dev));
                        // Start Point
                        str = Convert.ToString(startPoint, 10).PadLeft(6, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Number ò Point
                        str = Convert.ToString(data.Length, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Add data
                        foreach (int tmp in data)
                        {
                            if (tmp < 0)
                                str = Convert.ToString(tmp, 16).Substring(4, 4).PadLeft(4, '0').ToUpper();
                            else
                                str = Convert.ToString(tmp, 16).PadLeft(4, '0').ToUpper();
                            dat.AddRange(encode.GetBytes(str));

                        }
                        client.Send(dat.ToArray());
                        Thread.Sleep(0);
                        client.Receive();

                    }
                    //str = encode.GetString(dat.ToArray());
                }
                catch (Exception ex)
                {
                    logger.Create("Write Single Word Err" + ex.ToString());
                }
                return ret;
            }



        }
        public bool WriteDoubleWord(DataType.Devicecode dev, uint startPoint, int[] data)  //data[0]
        {
            bool ret = false;
            if (isConnected == false)
                return ret;
            lock (McLock)
            {
                try
                {
                    if (Binary)
                    {
                        List<byte> DataSend = new List<byte>();
                        int Number = data.Length;
                        DataSend.Add(0x50);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        DataSend.Add(0xFF);
                        DataSend.Add(0xFF);
                        DataSend.Add(0x03);
                        DataSend.Add(0x00);
                        //DataSend.Add(0x00);
                        DataSend.Add((Byte)((0x10 + (Number * 4) - 4) & 0xff));
                        DataSend.Add((Byte)((0x10 + (Number * 4) - 4) >> 8));
                        // Monitoring timer
                        DataSend.Add((Byte)(monitoring_timer & 0xff));
                        DataSend.Add((Byte)(monitoring_timer >> 8));
                        // Command + Subcommand
                        DataSend.Add(0x01);
                        DataSend.Add(0x14);
                        DataSend.Add(0x00);
                        DataSend.Add(0x00);
                        // Start Point
                        DataSend.Add((Byte)(startPoint & 0xff));
                        DataSend.Add((Byte)(startPoint >> 8));
                        DataSend.Add((Byte)(startPoint >> 16));
                        // Device Code
                        DataSend.AddRange(SetDevice(dev));
                        // Number Point

                        DataSend.Add((Byte)((0x02 * Number) & 0xff));
                        DataSend.Add((Byte)((0x02 * Number) >> 8));
                        // Value
                        for (int i = 0; i < data.Length; i++)
                        {
                            DataSend.Add((Byte)(data[i] & 0xff));
                            DataSend.Add((Byte)(data[i] >> 8));
                            DataSend.Add((Byte)(data[i] >> 16));
                            DataSend.Add((Byte)(data[i] >> 24));
                        }

                        client.Send(DataSend.ToArray());
                        Thread.Sleep(0);
                        client.Receive();
                        ret = true;
                    }
                    else
                    {
                        /*string str = "
* 5000         Subheader
* 00FF03FF00   Access route
* 0020         Lenght
* 0010         Monitoring
* 1401         Command
* 0000         Subcommand
* D*           Device Code
* 000100       Start point
* 0002         Number point
* 12345678";   Data
*/
                        List<byte> dat = new List<byte>();
                        // SubHeader + Access Route
                        string str = "500000FF03FF00";
                        ASCIIEncoding encode = new ASCIIEncoding();
                        dat.AddRange(encode.GetBytes(str));
                        // Length
                        int length = 24 + 8 * data.Length;
                        str = Convert.ToString(length, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Monitoring
                        str = Convert.ToString(monitoring_timer, 10).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Command + SubCommand
                        str = "14010000";
                        dat.AddRange(encode.GetBytes(str));
                        dat.AddRange(SetDevice(dev));
                        // Start Point
                        str = Convert.ToString(startPoint, 10).PadLeft(6, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Number ò Point
                        str = Convert.ToString(data.Length * 2, 16).PadLeft(4, '0').ToUpper();
                        dat.AddRange(encode.GetBytes(str));
                        // Add data
                        foreach (int tmp in data)
                        {
                            str = Convert.ToString(tmp, 16).PadLeft(8, '0').ToUpper();
                            dat.AddRange(encode.GetBytes(str.Substring(4, 4)));
                            dat.AddRange(encode.GetBytes(str.Substring(0, 4)));
                        }
                        client.Send(dat.ToArray());
                        Thread.Sleep(0);
                        client.Receive();
                        ret = true;
                    }

                    //str = encode.GetString(dat.ToArray());
                }
                catch (Exception ex)
                {
                    logger.Create("Write Double Word Err" + ex.ToString());
                }
                return ret;
            }


        }
        #endregion
        private byte[] SetDevice(DataType.Devicecode dev)
        {
            if (!Binary)
            {
                switch (dev)
                {
                    case DataType.Devicecode.D: return new byte[] { 0x44, 0x2A };
                    case DataType.Devicecode.M: return new byte[] { 0x4D, 0x2A };
                    case DataType.Devicecode.X: return new byte[] { 0x58, 0x2A };
                    case DataType.Devicecode.Y: return new byte[] { 0x59, 0x2A };
                    default:
                        return null;
                }
            }
            else
            {
                switch (dev)
                {
                    case DataType.Devicecode.D: return new byte[] { 0xA8 };
                    case DataType.Devicecode.M: return new byte[] { 0x90 };
                    case DataType.Devicecode.X: return new byte[] { 0x9C };
                    case DataType.Devicecode.Y: return new byte[] { 0x9D };
                    default:
                        return null;
                }
            }

        }
        public enum DevicePLC : short
        {
            D = 0x442A,
            ZR = 0x5A52,
            M = 0x4D2A,
            W = 0x572A,
            X = 0x582A,
            Y = 0x592A,
        }
        public static byte[] shortToArrByte(DataType.Devicecode a)
        {
            byte[] intBytes = BitConverter.GetBytes((short)a);  // Chuyển 1 giá trị kiểu short sang mảng byte
            if (BitConverter.IsLittleEndian)                              // Kiểm tra thứ tự byte quy định trong kiến trúc của máy tính đang chạy chương trình
                Array.Reverse(intBytes);                                  // Nếu ĐÚNG là kiểu "Little-Endian" cần đảo được thứ thự mảng byte
            return intBytes;
        }
    }
}

