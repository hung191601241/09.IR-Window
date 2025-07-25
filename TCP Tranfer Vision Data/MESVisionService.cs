using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VisionTCPClient
{
    class MESVisionService
    {
        private VisionTCPClient _client;
        public bool isAccept { get; set; }

        private SemaphoreSlim modbusSemaphore = new SemaphoreSlim(1, 1);

        public MESVisionService(string ip , int  port)
        {
            _client = new VisionTCPClient(ip , port);
        }
        public async Task Connect()
        {
            await _client.ConnectServer();
        }
        public void Disconnect()
        {
            _client.DisconnectServer();
        }

        public async Task<bool> SendReady(DATACheck entity)
        {
            await modbusSemaphore.WaitAsync();
            try
            {
                if (entity.EquipmentId.Length != 10)
                {
                    entity.EquipmentId = entity.EquipmentId.PadRight(10, ' ');
                } 
                return await _client.SendReady(entity);
            }
            finally
            {
                modbusSemaphore.Release();
            }
          
        }

        public async Task<bool>SendDataVision( DATACheck entity)
        {
            await modbusSemaphore.WaitAsync();
            try
            {
                if (entity.EquipmentId.Length != 10)
                {
                    entity.EquipmentId = entity.EquipmentId.PadRight(10, ' ');
                }
                if (entity.Status.Length != 6)
                {
                    entity.Status = entity.Status.PadRight(6, ' ');
                }
                return await _client.SendData(entity);
            }
            finally
            {
                modbusSemaphore.Release();
            }
        }
        public bool IsConnected()
        {
            return _client.CheckMESConnection();
        }
    }
}
