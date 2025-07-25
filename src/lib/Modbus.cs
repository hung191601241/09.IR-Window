using System;
using System.IO.Ports;
using System.Threading;
using VisionInspection;

namespace MyLib
{
    class Modbus
    {
        private const Byte FC_READ_COIL_STATUS = 0x01;
        private const Byte FC_READ_INPUT_STATUS = 0x02;
        private const Byte FC_READ_HOLDING_REGISTERS = 0x03;
        private const Byte FC_READ_INPUT_REGISTERS = 0x04;
        private const Byte FC_WRITE_SINGLE_COIL = 0x05;
        private const Byte FC_WRITE_SINGLE_REGISTER = 0x06;
        private const Byte FC_WRITE_MULTIPLE_COILS = 0x15;
        private const Byte FC_WRITE_MULTIPLE_REGISTERS = 0x16;

        private const Int32 MB_READ_TIMEOUT = 500;

        private SerialPort serialPort;

        private static MyLogger logger = new MyLogger("Modbus");

        private static void mbLog(String log)
        {
            logger.Create(log);
        }

        private Boolean waitForResponse(Int32 rxExpectedCnt, Int32 tout)
        {
            while (tout > 0)
            {
                if (this.serialPort.BytesToRead >= rxExpectedCnt)
                {
                    return true;
                }
                Thread.Sleep(10);
                tout -= 10;
            }
            return false;
        }

        public static UInt16 CRC16(byte[] buf, Int32 len)
        {
            UInt16 crc16 = 0xFFFF;
            Int32 i, j, tmp8;

            for (i = 0; i < len; ++i)
            {
                crc16 ^= (byte)buf[i];
                for (j = 8; j > 0; --j)
                {
                    tmp8 = crc16 & 0x0001;
                    crc16 >>= 1;
                    if (tmp8 == 1)
                    {
                        crc16 ^= 0xA001;
                    }
                }
            }
            return crc16;
        }

        // Verify if a packet is a valid Modbus packet by checking the checksum.
        // Modbus packet = [Frame(N)][CRC(2)]
        private static Boolean VerifyChecksum(byte[] rxBuf, Int32 rxLen)
        {
            if (rxLen <= 2)
            {
                return false;
            }
            UInt16 crcCal = CRC16(rxBuf, rxLen - 2);
            UInt16 crcRx = (UInt16)(rxBuf[rxLen - 2] + (rxBuf[rxLen - 1] << 8));
            if (crcCal != crcRx)
            {
                return false;
            }
            return true;
        }

        public Modbus(SerialPort port)
        {
            this.serialPort = port;
        }

        // [DevAddr(1)][Fc(1)][CoilIndex(2)][CoinCnt(2)][CRC(2)]
        public Byte[] ReadCoilStatus(Byte devAddr, UInt16 coilIndex, UInt16 coinCnt)
        {
            byte[] txBuf = new byte[32];
            Int32 idx = 0;
            Byte functionCode = FC_READ_COIL_STATUS;

            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(coilIndex >> 8);
            txBuf[idx++] = (Byte)(coilIndex & 0xff);
            txBuf[idx++] = (Byte)(coinCnt >> 8);
            txBuf[idx++] = (Byte)(coinCnt & 0xff);
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            //mbLog(" -> send txBuf..." + txBuf[0].ToString() + txBuf[1].ToString() + txBuf[2].ToString() + txBuf[3].ToString() + txBuf[4].ToString() + txBuf[5].ToString() + txBuf[6].ToString() + txBuf[7].ToString());

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);


            // Wait for response: [DevAddr(1)][Fc(1)][ByteCnt(1)=M][Coins(M)][CRC(2)]
            // Coils[x] is MSB: B7...B0 -> Coil[7]...Coil[0]
            Int32 rxByteCnt = (coinCnt + 7) / 8;
            Int32 rxExpectedCnt = 3 + rxByteCnt + 2;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                return null;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                // Verify:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    return null;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    return null;
                }

                // Get Coil values:
                byte[] ret = new byte[coinCnt];
                for (int i = 0; i < coinCnt; i++)
                {
                    Int32 byteIdx = 3 + i / 8;
                    if ((rxBuf[byteIdx] & (1 << i)) != 0)
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
            return null;
        }

        // [DevAddr(1)][Fc(1)][CoilIndex(2)][CoinCnt(2)][CRC(2)]
        public Byte[] ReadInputStatus(Byte devAddr, UInt16 coilIndex, UInt16 coinCnt)
        {
            byte[] txBuf = new byte[32];
            Int32 idx = 0;
            Byte functionCode = FC_READ_INPUT_STATUS;

            // Normalize Input address:
            if (coilIndex >= 10000)
            {
                coilIndex -= 10000;
            }

            // Create TX buffer:
            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(coilIndex >> 8);
            txBuf[idx++] = (Byte)(coilIndex & 0xff);
            txBuf[idx++] = (Byte)(coinCnt >> 8);
            txBuf[idx++] = (Byte)(coinCnt & 0xff);
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: [DevAddr(1)][Fc(1)][ByteCnt(1)=M][Coins(M)][CRC(2)]
            // Coils[x] is MSB: B7...B0 -> Coil[7]...Coil[0]
            Int32 rxByteCnt = (coinCnt + 7) / 8;
            Int32 rxExpectedCnt = 3 + rxByteCnt + 2;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                return null;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                // Verify DevAddr & Fc:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    return null;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    return null;
                }

                // Get Coil values:
                byte[] ret = new byte[coinCnt];
                for (int i = 0; i < coinCnt; i++)
                {
                    Int32 byteIdx = 3 + i / 8;
                    if ((rxBuf[byteIdx] & (1 << i)) != 0)
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
            return null;
        }

        // [DevAddr(1)][Fc(1)][StartAddr(2)][RegisterCnt(2)][CRC(2)]
        public UInt16[] ReadHoldingRegisters(Byte devAddr, UInt16 startAddr, UInt16 regCnt)
        {
            byte[] txBuf = new byte[32];
            Int32 idx = 0;
            Byte functionCode = FC_READ_HOLDING_REGISTERS;

            mbLog(String.Format("ReadHoldingRegisters: devAddr={0}, startAddr={1:X4}, regCnt={2}",
                devAddr, startAddr, regCnt));

            // Normalize address:
            if (startAddr >= 40000)
            {
                startAddr -= 40000;
            }

            // Create TX buffer:
            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(startAddr >> 8);
            txBuf[idx++] = (Byte)(startAddr & 0xff);
            txBuf[idx++] = (Byte)(regCnt >> 8);
            txBuf[idx++] = (Byte)(regCnt & 0xff);
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            mbLog(" -> send txBuf..." + txBuf[0].ToString() + txBuf[1].ToString() + txBuf[2].ToString() + txBuf[3].ToString() + txBuf[4].ToString() + txBuf[5].ToString() + txBuf[6].ToString() + txBuf[7].ToString());
            //this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: [DevAddr(1)][Fc(1)][ByteCnt(1)=M][Regs(M/2)][CRC(2)]
            // Regs[i] is 16-bit register MSB.
            this.serialPort.ReadTimeout = MB_READ_TIMEOUT;
            Int32 rxByteCnt = regCnt * 2;
            Int32 rxExpectedCnt = 3 + rxByteCnt + 2;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                mbLog(" -> waitForResponse timed out!");
                return null;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);

            mbLog(" -> rxCnt=" + rxCnt.ToString());

            if (rxCnt == rxExpectedCnt)
            {
                mbLog(String.Format(" -> RxFrame: addr={0}, fc={1:X2}", rxBuf[0], rxBuf[1]));

                // Verify DevAddr & Fc:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    mbLog(" -> invalid DeviceAddress or FunctionCode");
                    return null;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    mbLog(" -> invalid checksum");
                    return null;
                }

                // Get Registers values:
                UInt16[] ret = new UInt16[regCnt];
                for (int i = 0; i < regCnt; i++)
                {
                    ret[i] = (UInt16)((rxBuf[3 + i * 2] << 8) | rxBuf[3 + i * 2 + 1]);
                }
                return ret;
            }
            return null;
        }

        // [DevAddr(1)][Fc(1)][StartAddr(2)][RegisterCnt(2)][CRC(2)]
        public UInt16[] ReadInputRegisters(Byte devAddr, UInt16 startAddr, UInt16 regCnt)
        {
            byte[] txBuf = new byte[32];
            Int32 idx = 0;
            Byte functionCode = FC_READ_INPUT_REGISTERS;

            // Normalize address:
            if (startAddr >= 30000)
            {
                startAddr -= 30000;
            }

            // Create TX buffer:
            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(startAddr >> 8);
            txBuf[idx++] = (Byte)(startAddr & 0xff);
            txBuf[idx++] = (Byte)(regCnt >> 8);
            txBuf[idx++] = (Byte)(regCnt & 0xff);
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: [DevAddr(1)][Fc(1)][ByteCnt(1)=M][Regs(M/2)][CRC(2)]
            // Regs[i] is 16-bit register MSB.
            Int32 rxByteCnt = regCnt * 2;
            Int32 rxExpectedCnt = 3 + rxByteCnt + 2;
            byte[] rxBuf = new byte[rxExpectedCnt];
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                return null;
            }
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                // Verify DevAddr & Fc:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    return null;
                }

                // Get Registers values:
                UInt16[] ret = new UInt16[regCnt];
                for (int i = 0; i < regCnt; i++)
                {
                    ret[i] = (UInt16)((rxBuf[3 + i * 2] << 8) | rxBuf[3 + i * 2 + 1]);
                }
                return ret;
            }
            return null;
        }

        // [DevAddr(1)][Fc(1)][CoilIndex(2)][CoilValue(2)][CRC(2)]
        public Boolean WriteSingleCoil(Byte devAddr, UInt16 coilIndex, Byte value)
        {
            byte[] txBuf = new byte[32];
            Int32 idx = 0;
            Byte functionCode = FC_WRITE_SINGLE_COIL;
            UInt16 coilValue = 0x0000;
            if (value != 0)
            {
                coilValue = 0xff00;
            }

            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(coilIndex >> 8);
            txBuf[idx++] = (Byte)(coilIndex & 0xff);
            txBuf[idx++] = (Byte)(coilValue >> 8);
            txBuf[idx++] = (Byte)(coilValue & 0xff);
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: same as request = [DevAddr(1)][Fc(1)][CoilIndex(2)][CoinValue(2)][CRC(2)]
            Int32 rxExpectedCnt = 8;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                return false;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                // Verify:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    return false;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    return false;
                }
                if (((rxBuf[2] << 8) + rxBuf[3]) != coilIndex)
                {
                    return false;
                }
                if (((rxBuf[4] << 8) + rxBuf[5]) != coilValue)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        // [DevAddr(1)][Fc(1)][RegAddr(2)][RegValue(2)][CRC(2)]
        public Boolean WriteSingleRegister(Byte devAddr, UInt16 regAddr, UInt16 regValue)
        {
            byte[] txBuf = new byte[32];
            Int32 idx = 0;
            Byte functionCode = FC_WRITE_SINGLE_REGISTER;

            mbLog(String.Format("WriteSingleRegister: devAddr={0}, regAddr={1:X4}, regValue={2:X4}",
               devAddr, regAddr, regValue));

            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(regAddr >> 8);
            txBuf[idx++] = (Byte)(regAddr & 0xff);
            txBuf[idx++] = (Byte)(regValue >> 8);
            txBuf[idx++] = (Byte)(regValue & 0xff);
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: same as request = [DevAddr(1)][Fc(1)][RegAddr(2)][RegValue(2)][CRC(2)]
            Int32 rxExpectedCnt = 8;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                mbLog(" -> waitForResponse timed out!");
                return false;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                mbLog(String.Format(" -> RxFrame: addr={0}, fc={1:X2}", rxBuf[0], rxBuf[1]));

                // Verify:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    mbLog(" -> invalid DeviceAddress or FunctionCode");
                    return false;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    mbLog(" -> invalid checksum");
                    return false;
                }
                if (((rxBuf[2] << 8) + rxBuf[3]) != regAddr)
                {
                    mbLog(" -> invalid Echo.RegAddr");
                    return false;
                }
                if (((rxBuf[4] << 8) + rxBuf[5]) != regValue)
                {
                    mbLog(" -> invalid Echo.RegVal");
                    return false;
                }
                return true;
            }
            return false;
        }

        // [DevAddr(1)][Fc(1)][CoilStartIndex(2)][CoilCount(2)][ByteCount(1)=N][CoilValues(N)][CRC(2)]
        public Boolean WriteMultipleCoils(Byte devAddr, UInt16 startCoilIndex, Byte[] coilValueArray)
        {
            byte[] txBuf;
            Int32 idx = 0;
            Byte functionCode = FC_WRITE_MULTIPLE_COILS;
            if ((coilValueArray == null) || (coilValueArray.Length == 0))
            {
                return false;
            }
            Int32 byteCnt = (coilValueArray.Length + 7) / 8;
            Byte[] values = new byte[byteCnt];
            for (int i = 0; i < coilValueArray.Length; i++)
            {
                int byteIdx = i / 8;
                int bitPos = i % 8;
                if (coilValueArray[i] == 0)
                {
                    values[byteIdx] |= (Byte)(1 << bitPos);
                }
                else
                {
                    values[byteIdx] &= (Byte)~(1 << bitPos);
                }
            }

            txBuf = new byte[byteCnt + 16];
            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(startCoilIndex >> 8);
            txBuf[idx++] = (Byte)(startCoilIndex & 0xff);
            txBuf[idx++] = (Byte)(coilValueArray.Length >> 8);
            txBuf[idx++] = (Byte)(coilValueArray.Length & 0xff);
            txBuf[idx++] = (Byte)byteCnt;
            Array.Copy(values, 0, txBuf, idx, byteCnt);
            idx += byteCnt;
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: [DevAddr(1)][Fc(1)][CoilStartIndex(2)][CoilCount(2)][CRC(2)]
            this.serialPort.ReadTimeout = MB_READ_TIMEOUT;
            Int32 rxExpectedCnt = 8;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                return false;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                // Verify:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    return false;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    return false;
                }
                if (((rxBuf[2] << 8) + rxBuf[3]) != startCoilIndex)
                {
                    return false;
                }
                if (((rxBuf[4] << 8) + rxBuf[5]) != coilValueArray.Length)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        // [DevAddr(1)][Fc(1)][StartAddr(2)][RegisterCount(2)][ByteCount(1)=N][RegisterValues(N x 2)][CRC(2)]
        public Boolean WriteMultipleRegisters(Byte devAddr, UInt16 startAddr, UInt16[] regArray)
        {
            byte[] txBuf;
            Int32 idx = 0;
            Byte functionCode = FC_WRITE_MULTIPLE_REGISTERS;
            if ((regArray == null) || (regArray.Length == 0))
            {
                return false;
            }

            txBuf = new byte[regArray.Length * 2 + 16];
            txBuf[idx++] = devAddr;
            txBuf[idx++] = functionCode;
            txBuf[idx++] = (Byte)(startAddr >> 8);
            txBuf[idx++] = (Byte)(startAddr & 0xff);
            txBuf[idx++] = (Byte)(regArray.Length >> 8);
            txBuf[idx++] = (Byte)(regArray.Length & 0xff);
            txBuf[idx++] = (Byte)(regArray.Length * 2);
            for (int i = 0; i < regArray.Length; i++)
            {
                txBuf[idx] = (Byte)(regArray[i] >> 8);
                txBuf[idx + 1] = (Byte)(regArray[i] & 0xff);
                idx += 2;
            }
            UInt16 crc = CRC16(txBuf, idx);
            txBuf[idx++] = (Byte)(crc & 0xff); // CRC is LSB
            txBuf[idx++] = (Byte)(crc >> 8);

            // Send:
            this.serialPort.DiscardInBuffer();
            this.serialPort.Write(txBuf, 0, idx);

            // Wait for response: [DevAddr(1)][Fc(1)][StartAddr(2)][RegisterCount(2)][CRC(2)]
            this.serialPort.ReadTimeout = MB_READ_TIMEOUT;
            Int32 rxExpectedCnt = 8;
            if (!waitForResponse(rxExpectedCnt, MB_READ_TIMEOUT))
            {
                return false;
            }
            byte[] rxBuf = new byte[rxExpectedCnt];
            Int32 rxCnt = this.serialPort.Read(rxBuf, 0, rxExpectedCnt);
            if (rxCnt == rxExpectedCnt)
            {
                // Verify:
                if ((rxBuf[0] != devAddr) || (rxBuf[1] != functionCode))
                {
                    return false;
                }
                if (!VerifyChecksum(rxBuf, rxCnt))
                {
                    return false;
                }
                if (((rxBuf[2] << 8) + rxBuf[3]) != startAddr)
                {
                    return false;
                }
                if (((rxBuf[4] << 8) + rxBuf[5]) != regArray.Length)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
