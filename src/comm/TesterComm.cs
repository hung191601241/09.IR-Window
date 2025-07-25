using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace VisionInspection
{
    internal enum RX_STATES
    {
        WAIT_SOT = 0,
        WAIT_OPCODE,
        WAIT_LEN_H,
        WAIT_LEN_L,
        WAIT_JIG_HEAD,
        WAIT_JIG_DATA,
        WAIT_DATA,
        WAIT_CRC_H,
        WAIT_CRC_L,
        WAIT_EOT
    }

    internal class RX_FRAME
    {
        public const Char FRAME_DATA_SEPERATOR = '$';

        public RX_STATES Stm;

        public Byte Command { get; set; }
        public int Len { get; set; }
        public StringBuilder JigQr { get; set; }
        public List<byte> Data { get; set; }
        public int Crc { get; set; }

        public RX_FRAME() {
            this.Stm = RX_STATES.WAIT_SOT;
        }

        public List<String> GetQrCodes() {
            var ret = new List<String>();
            String str = ASCIIEncoding.ASCII.GetString(this.Data.ToArray());
            str = str.Replace('^', FRAME_DATA_SEPERATOR);
            var arrStr = str.Split(FRAME_DATA_SEPERATOR);
            ret.AddRange(arrStr);
            return ret;
        }

        public void SetQrCodes(List<String> qrCodes) {
            List<byte> ret = new List<byte>(0);
            for (int i = 0; i < qrCodes.Count; i++) {
                var bArr = ASCIIEncoding.ASCII.GetBytes(qrCodes[i]);
                ret.AddRange(bArr);
                ret.Add((Byte)FRAME_DATA_SEPERATOR);
            }
            this.Data = ret;
        }
    }

    internal class TesterComm
    {
        private static MyLogger logger = new MyLogger("TesterComm");

        private const Byte FRAME_SOT = 0x02;
        private const Byte FRAME_EOT = 0x03;

        public String Name { get; set; } = "Tester1";

        private SerialPort serialPort;
        private byte[] readBuf = new byte[1];
        private RX_FRAME rxFrame;
        private int crcCal = 0;

        public delegate void RxPacketHandler(TesterComm comm, RX_FRAME frame);

        public event RxPacketHandler PacketReceived;

        public delegate void RxRawDataHandler(TesterComm comm, Byte rx);

        public event RxRawDataHandler RawDataReceived;

        public TesterComm(ComSettings settings) {
            try {
                this.serialPort = new SerialPort(settings.portName, settings.baudrate,
                    settings.parity, settings.dataBits, settings.stopBits);
            } catch (Exception ex) {
                logger.Create("TesterComm error:" + ex.Message);
            }
        }

        public void Start() {
            try {
                if (serialPort != null && (!serialPort.IsOpen)) {
                    endReception();

                    this.serialPort.Open();
                    this.serialPort.BaseStream.BeginRead(this.readBuf, 0, 1, new AsyncCallback(this.readCallback), this.serialPort);
                }
            } catch (Exception ex) {
                logger.Create("Start error:" + ex.Message);
            }
        }

        public void Stop() {
            try {
                if (serialPort != null && serialPort.IsOpen) {
                    this.serialPort.Close();
                }
            } catch (Exception ex) {
                logger.Create("Stop error:" + ex.Message);
            }
        }

        public void Send_0x92(String jigQr, List<String> qrCodes) {
            var frame = new RX_FRAME();
            frame.Command = 0x92;
            frame.JigQr = new StringBuilder(jigQr);
            frame.Data = new List<byte>(0);
            for (int i = 0; i < qrCodes.Count; i++) {
                var arr = ASCIIEncoding.ASCII.GetBytes(qrCodes[i]);
                frame.Data.AddRange(arr);
                if (i < qrCodes.Count - 1) {
                    //frame.Data.Add((byte)'$');
                    if ((i & 0x01) != 0) {
                        frame.Data.Add((byte)'$');
                    } else {
                        frame.Data.Add((byte)'^');
                    }
                }
            }
            frame.Len = frame.Data.Count;
            var txBuf = createFrame(frame);

            //var strBuilder = new StringBuilder("\r\nTX.0x92:");
            //for (int i = 0; i < txBuf.Length; i++) {
            //    strBuilder.Append(String.Format("{0:X2}", txBuf[i]));
            //}
            //logger.Create(strBuilder.ToString());

            if (this.serialPort != null && this.serialPort.IsOpen) {
                new TesterLogger(this.Name).CreateTx92(txBuf);
                this.serialPort.BaseStream.WriteAsync(txBuf, 0, txBuf.Length);
            }
        }

        public void Send_0x94(String jigQr, Boolean confirmResult) {
            var frame = new RX_FRAME();
            frame.Command = 0x94;
            frame.JigQr = new StringBuilder(jigQr);
            frame.Data = new List<byte>(0);
            if (confirmResult) {
                frame.Data.Add(0xff);
            } else {
                frame.Data.Add(0x00);
            }
            //frame.Data.Add(0xff); // always send 0xff
            frame.Len = frame.Data.Count;
            var txBuf = createFrame(frame);
            if (this.serialPort != null && this.serialPort.IsOpen) {
                new TesterLogger(this.Name).CreateTx94(txBuf);
                this.serialPort.BaseStream.WriteAsync(txBuf, 0, txBuf.Length);
            }
        }

        private void readCallback(IAsyncResult iar) {
            var port = (SerialPort)iar.AsyncState;
            if (!port.IsOpen) {
                logger.Create("readCallback: port is closed -> stop reading!");
                return;
            }

            int rxCnt = port.BaseStream.EndRead(iar);
            if (rxCnt == 1) {
                byte rx = this.readBuf[0];
                string s = char.ConvertFromUtf32(rx);
                //Debug.Write(String.Format("{0:X2}", rx));

                if (this.RawDataReceived != null) {
                    this.RawDataReceived(this, rx);
                }

                // Calculate checksum:
                if (rxFrame.Stm < RX_STATES.WAIT_CRC_H) {
                    crcCal ^= rx;
                }

                // Process RX byte:
                switch (this.rxFrame.Stm) {
                case RX_STATES.WAIT_SOT:
                    if (rx == FRAME_SOT) {
                        rxFrame.JigQr = new StringBuilder();
                        rxFrame.Data = new List<byte>(0);
                        rxFrame.Stm = RX_STATES.WAIT_OPCODE;
                    }
                    break;

                case RX_STATES.WAIT_OPCODE:
                    rxFrame.Command = rx;
                    rxFrame.Stm = RX_STATES.WAIT_LEN_H;
                    break;

                case RX_STATES.WAIT_LEN_H:
                    rxFrame.Len = rx;
                    rxFrame.Stm = RX_STATES.WAIT_LEN_L;
                    break;

                case RX_STATES.WAIT_LEN_L:
                    rxFrame.Len = (rxFrame.Len << 8) | rx;
                    rxFrame.Stm = RX_STATES.WAIT_JIG_HEAD;
                    break;

                case RX_STATES.WAIT_JIG_HEAD:
                    if (rx == ',') {
                        rxFrame.Stm = RX_STATES.WAIT_JIG_DATA;
                    } else {
                        endReception();
                    }
                    break;

                case RX_STATES.WAIT_JIG_DATA:
                    if (rx == ',') {
                        if (rxFrame.Len > 0) {
                            rxFrame.Stm = RX_STATES.WAIT_DATA;
                        } else {
                            rxFrame.Stm = RX_STATES.WAIT_CRC_H;
                        }
                    } else {
                        rxFrame.JigQr.Append(Char.ConvertFromUtf32(rx));
                    }
                    break;

                case RX_STATES.WAIT_DATA:
                    if (rxFrame.Data.Count < rxFrame.Len) {
                        rxFrame.Data.Add(rx);
                    }
                    if (rxFrame.Data.Count == rxFrame.Len) {
                        rxFrame.Stm = RX_STATES.WAIT_CRC_H;
                    }
                    break;

                case RX_STATES.WAIT_CRC_H:
                    rxFrame.Crc = asciiToHex(rx);
                    rxFrame.Stm = RX_STATES.WAIT_CRC_L;
                    break;

                case RX_STATES.WAIT_CRC_L:
                    rxFrame.Crc = (rxFrame.Crc << 4) | asciiToHex(rx);
                    rxFrame.Stm = RX_STATES.WAIT_EOT;
                    // Verify checksum:
                    if (crcCal != rxFrame.Crc) {
                        //endReception();
                        logger.Create(" -> Invalid CRC!");
                    }
                    //else {
                    //    rxFrame.Stm = RX_STATES.WAIT_EOT;
                    //}
                    break;

                case RX_STATES.WAIT_EOT:
                    if (rx == FRAME_EOT) {
                        if (this.PacketReceived != null) {
                            this.PacketReceived(this, rxFrame);
                        }
                        endReception();
                    }
                    break;
                }
            }

            // Continue reading:
            port.BaseStream.BeginRead(this.readBuf, 0, 1, new AsyncCallback(readCallback), port);
        }

        private void endReception() {
            this.crcCal = 0;
            this.rxFrame = new RX_FRAME();
        }

        private int hexToAscii(int x) {
            int ret = x + 0x30;
            if (x >= 10) {
                ret = x - 10 + 'A';
            }
            return ret;
        }

        private int asciiToHex(int c) {
            if ((0x30 <= c) && (c <= 0x39)) {
                return c & 0x0f;
            } else if (c >= 'A') {
                return c - 'A' + 10;
            }
            return 0;
        }

        private byte[] createFrame(RX_FRAME frame) {
            var ret = new List<byte>(0);

            ret.Add(FRAME_SOT);
            ret.Add(frame.Command);
            ret.Add((byte)(frame.Len >> 8));
            ret.Add((byte)(frame.Len & 0xff));
            ret.Add((byte)',');
            var jig = ASCIIEncoding.ASCII.GetBytes(frame.JigQr.ToString());
            ret.AddRange(jig);
            ret.Add((byte)',');
            ret.AddRange(frame.Data);

            int crc = checksum(ret.ToArray());
            ret.Add((byte)hexToAscii(crc >> 4));
            ret.Add((byte)hexToAscii(crc & 0xf));
            ret.Add(FRAME_EOT);
            return ret.ToArray();
        }

        private int checksum(byte[] arr) {
            int ret = 0;
            for (int i = 0; i < arr.Length; i++) {
                ret ^= arr[i];
            }
            return ret;
        }
    }
}