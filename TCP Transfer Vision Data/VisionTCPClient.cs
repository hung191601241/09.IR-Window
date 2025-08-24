using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VisionInspection;

namespace VisionTCPClient
{
    class VisionTCPClient
    {
        private MyLogger logger = new MyLogger("VisionTCPClient");
        private TcpClient client;
        private string IP;
        private int port;
        private bool isRunning = false;
        private CancellationTokenSource cts = new CancellationTokenSource();

        protected bool isReceiver = false;
        public VisionTCPClient(string ip , int port) 
        {
            this.client = new TcpClient();
            this.IP = ip;
            this.port = port;
        }

        private string DataReceiver;
        public async Task ConnectServer()
        {
            if(isRunning) return;
            await client.ConnectAsync(IP, port);
            isRunning = true;
            _ = Task.Run(() => ReceiveDataAsync(cts.Token));


        }
        public void DisconnectServer()
        {
            isRunning = false;
            cts.Cancel();
            client.Close();
        }
        private async Task ReceiveDataAsync(CancellationToken cancellationToken)
        {
            try
            {

                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];

                while (isRunning && !cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                      
                        string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        logger.Create($"TCPClientVision Receive: {data}");

                      
                        DataReceiver  = data;
                        this.isReceiver = true;
                    }
                    else
                    {
                       
                        logger.Create("Server Disconnect !");
                        isRunning = false;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create($"ReceiveDataAsync Error: {ex.Message}");
                isRunning = false;
            }
            finally
            {
                if (!isRunning)
                {
                    client.Close();
                    logger.Create("TCPClientVision : Disconnect");
                }
            }
        }
        public async Task<bool> SendReady(DATACheck entity)
        {
            this.isReceiver = false;
            try
            {
                if (!this.CheckMESConnection())
                {
                    logger.Create(" -> TCP connection not ready -> discard sending SendReady!");
                    return false;
                }
                var packet = new List<byte>();
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes(entity.EquipmentId));
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes("AUTO01"));
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes("READYOK"));
                var txBuf = packet.ToArray();
                logger.Create($@"TCPClientVision.SEND Ready :" + ASCIIEncoding.ASCII.GetString(txBuf));
                await this.SendToMES(txBuf);
                await WaitMESReturnData();
                if (this.isReceiver && !string.IsNullOrEmpty(this.DataReceiver))
                {
                    this.isReceiver = false;
                    logger.Create($@"TCPClientVision.RECEIVER Ready:" + this.DataReceiver);
                    var mesData = FilterData(this.DataReceiver, entity);
                    if (mesData != null) return true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Create($@"SendReady : " + ex.Message);
            }
            return false;
        }
        public async Task<bool> SendData(DATACheck entity)
        {
            this.isReceiver = false;
            try
            {
                if (!this.CheckMESConnection())
                {
                    logger.Create(" -> TCP connection not ready -> discard sending SendReady!");
                    return false;
                }
                var packet = new List<byte>();
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes(entity.EquipmentId));
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes(entity.Status));
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes(";"));


                List<object> DATA = new List<object>();
                DATA.Add(entity.FormatVision);
                var formatVision = new { DATA };
                string formatData = JsonConvert.SerializeObject(formatVision);
                packet.AddRange(ASCIIEncoding.ASCII.GetBytes(formatData));
                //packet.AddRange(ASCIIEncoding.ASCII.GetBytes(";"));
                //packet.AddRange(ASCIIEncoding.ASCII.GetBytes(entity.CheckSum));


                var txBuf = packet.ToArray();
                //logger.Create($@"MES.SEND:" + ASCIIEncoding.ASCII.GetString(txBuf));
                logger.Create($@"TCPClientVision.SEND: Đã send chuỗi hình ảnh đi");

                await this.SendToMES(txBuf);

                await WaitMESReturnData();
                if (this.isReceiver && !string.IsNullOrEmpty(this.DataReceiver))
                {
                    this.isReceiver = false;
                    logger.Create($@"TCPClientVision.RECEIVER Ready:" + this.DataReceiver);
                    var mesData = FilterData(this.DataReceiver, entity);
                    if (mesData != null) return true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Create($@"SendReady : " + ex.Message);
            }
            return false;
        }
        private DATACheck FilterData(string data, DATACheck mesOld)
        {
            try
            {

                DATACheck newMESCheck = new DATACheck();
                int idex = 0;
                // EQUIPMENT ID
                string equipmentId = data.Substring(idex, 10);
                if (mesOld.EquipmentId != equipmentId)
                {
                    logger.Create($@"TCPClientVision EquipmentId Is Diffrent: Old('" + mesOld.EquipmentId + "') , New('" + equipmentId + "')");
                    return null;
                }
                logger.Create($@"TCPClientVision.RECEIVER EquipmentId:" + equipmentId);
                newMESCheck.EquipmentId = equipmentId;
                idex += 10;

                // STATUS
                string status = data.Substring(idex,6);
                if (status == "AUTO02")
                {
                    logger.Create($@"TCPClientVision.RECEIVER Status :" + status);
                    newMESCheck.Status = status;
                    return newMESCheck;
                }
                if (status == "AUTO11")
                {
                    logger.Create($@"TCPClientVision.RECEIVER Status :" + status);
                    newMESCheck.Status = status;
                    return newMESCheck;
                }
                return mesOld;
               
            }
            catch (Exception ex)
            {
                logger.Create($@"TCPClientVision FilterData  ""data"" {data}: " + ex.Message);
            }
            return null;
        }
        public async Task SendToMES(byte[] txBuf)
        {

            if (!isRunning)
            {
                return;
            }
           
            byte[] lengthBytes = BitConverter.GetBytes(txBuf.Length);

            NetworkStream stream = client.GetStream();

            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            await stream.WriteAsync(txBuf, 0, txBuf.Length);

        }
        public async Task SendToData(string json)
        {

            if (!isRunning)
            {
                return;
            }
            try
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);

                NetworkStream stream = client.GetStream();

                await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
                await stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }
            catch (Exception ex)
            {
                //// Log lỗi nếu cần
                //Console.WriteLine($"SendToData Error: {ex.Message}");
            }
           
        }
        private async Task WaitMESReturnData()
        {
            int counterDelayReceiver = 0;
            await Task.Run(async () => {
                while (!this.isReceiver)
                {
                    if (counterDelayReceiver > 1000)
                    {
                        break;
                    }
                    await Task.Delay(10); // Đợi 10 giây
                    counterDelayReceiver++;
                }
            });
        }
        public bool CheckMESConnection()
        {
            return client != null && client.Connected;
        }

    }
}
