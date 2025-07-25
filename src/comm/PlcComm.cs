using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using MyLib;
using System.Threading;

namespace VisionInspection
{
    class PlcComm
    {
        private const Int32 TIMEOUT_STEP = 100;

        private static MyLogger logger = new MyLogger("PlcComm");

        private static Object mbLock = new object();

        private SerialPort serialPort;
        private Byte plcMbAddr = 0x01;

        public PlcComm(Byte plcModbusAddress, String portName, int baudrate)
        {
            try
            {
                this.plcMbAddr = plcModbusAddress;
                if (String.IsNullOrEmpty(portName))
                {
                    return;
                }
                this.serialPort = new SerialPort(portName, baudrate, Parity.Even, 8, StopBits.One);
                this.serialPort.Open();
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("PlcComm error:" + ex.Message));
            }
        }

        public void Stop()
        {
            try
            {
                if (this.serialPort != null && this.serialPort.IsOpen)
                {
                    this.serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Stop error:" + ex.Message));
            }
        }

        public bool WriteBit(UInt16 bitAddr, Byte bitVal)
        {
            var ret = false;

            lock (mbLock)
            {
                try
                {
                    var coilIndex = (UInt16)(bitAddr);
                    var mb = new Modbus(this.serialPort);
                    ret = mb.WriteSingleCoil(this.plcMbAddr, coilIndex, bitVal);
                    if (!ret)
                    { // Retry
                        Thread.Sleep(100);
                        ret = mb.WriteSingleCoil(this.plcMbAddr, coilIndex, bitVal);
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("WriteBit error:" + ex.Message);
                }
            }
            return ret;
        }

        public Boolean PollClearBit(UInt16 bitAddr, Int32 timeoutMs)
        {
            var ret = false;

            try
            {
                long timeoutTick = timeoutMs * 10000;

                // Polling Bit:
                long startTime = DateTime.Now.Ticks;
                while (true)
                {
                    var bitVal = this.ReadBit(bitAddr);
                    if (bitVal == 1)
                    {
                        ret = true;
                        break;
                    }
                    Thread.Sleep(TIMEOUT_STEP);
                    long t = (DateTime.Now.Ticks - startTime);
                    if (t > timeoutTick)
                    {
                        break;
                    }
                }

                // Clear if the bit is set:
                if (ret)
                {
                    this.WriteBit(bitAddr, 0);
                }
            }
            catch (Exception ex)
            {
                logger.Create("PollClearBit error:" + ex.Message);
            }
            return ret;
        }

        /// <summary>
        /// Read a Bit from PLC Bit memory.
        /// </summary>
        /// <param name="bitAddr"></param>
        /// <returns>The bit value, or -1 if an error occurs.</returns>
        public Int32 ReadBit(UInt16 bitAddr)
        {
            Int32 ret = -1;

            lock (mbLock)
            {
                try
                {
                    var coilIndex = (UInt16)(bitAddr);
                    var rsp = new Modbus(this.serialPort).ReadCoilStatus(this.plcMbAddr, coilIndex, 1);
                    if ((rsp != null) && (rsp.Length > 0))
                    {
                        ret = rsp[0];
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("ReadBit error:" + ex.Message);
                }
            }
            return ret;
        }

        public Int32 ReadWord(UInt16 wordAddr)
        {
            Int32 ret = -1;

            lock (mbLock)
            {
                try
                {
                    var rsp = new Modbus(this.serialPort).ReadHoldingRegisters(this.plcMbAddr, wordAddr, 2);
                    if ((rsp != null) && (rsp.Length > 0))
                    {
                        ret = rsp[0];
                        //logger.Create("ReadWord error:" + rsp[0].ToString());
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("ReadWord error:" + ex.Message);
                }
            }
            return ret;
        }

        public bool WriteWord(UInt16 wAddr, UInt16 wVal)
        {
            bool ret = false;

            lock (mbLock)
            {
                try
                {
                    ret = new Modbus(this.serialPort).WriteSingleRegister(this.plcMbAddr, wAddr, wVal);
                }
                catch (Exception ex)
                {
                    logger.Create("WriteWord error:" + ex.Message);
                }
            }
            return ret;
        }


        public Boolean IsOpen()
        {
            if (!serialPort.IsOpen)
                return false;
            else
            {
                return true;
            }

        }


    }
}
