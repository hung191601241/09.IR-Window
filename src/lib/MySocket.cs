using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace VisionInspection
{
    public class MySocket
    {
        Socket sock;// = new Socket(SocketType.Stream, ProtocolType.Tcp);

        #region Connect
        public delegate void EventConnectState();
        public event EventConnectState SocketConnected;
        public event EventConnectState SocketDisconnected;

        void onConnected()
        {
            if (SocketConnected != null)
                SocketConnected();
        }

        void onDisconnected()
        {
            if (SocketDisconnected != null)
                SocketDisconnected();
        }

        public void Connect(string IPServer, int port)
        {
            AsyncCallback connectCallback = new AsyncCallback((IAsyncResult result) =>
            {
                try
                {
                    sock.EndConnect(result);
                    onConnected();
                }
                catch(Exception ex)
                {

                }
                
            });
            sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.BeginConnect(IPServer, port, connectCallback, null);
            //try
            //{
            //    sock.Connect(IPServer, port);
            //    onConnected();
            //}
            //catch(Exception ex)
            //{
                
            //}
            
        }

        public void Disconnect()
        {
            AsyncCallback disconnectCallback = new AsyncCallback((IAsyncResult result) =>
            {
                sock.EndDisconnect(result);
                onDisconnected();
            });

            sock.BeginDisconnect(false, disconnectCallback, null);
        }

        #endregion

        #region Data Sent
        public delegate void EventDataSent();
        public event EventDataSent DataSent;

        void onSent()
        {
            if (DataSent != null)
                DataSent();
        }

        public void Send(byte[] data)
        {
            if (!sock.Connected)
                return;
            AsyncCallback sentCallback = new AsyncCallback((IAsyncResult result) =>
            {
                SocketError err;
                sock.EndSend(result, out err);
                if (err == SocketError.Success)
                {
                    onSent();
                }
                else
                {
                    Disconnect();
                }
            });

            sock.BeginSend(data, 0, data.Length, SocketFlags.None, sentCallback, null);
            //sock.Send(data);
        }
        #endregion

        #region Data Received
        public delegate void EventDataReceived(byte[] data);
        public event EventDataReceived DataReceived;

        void onReceived(byte[] data)
        {
            if (DataReceived != null)
                DataReceived(data);
        }

        public byte[] Receive()
        {
            byte[] data = new byte[16348];
            if (!sock.Connected)
                return data;
            
            AsyncCallback receiveCallback = new AsyncCallback((IAsyncResult result) =>
            {
                sock.EndReceive(result);
            });
            //sock.BeginReceive(data, 0, data.Length, SocketFlags.None, receiveCallback, null);
            sock.Receive(data);
            return (data);
        }
        #endregion
    }
}
