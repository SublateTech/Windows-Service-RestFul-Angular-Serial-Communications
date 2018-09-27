using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Configuration;
using System.Globalization;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SimpleSocketClient;


namespace ShiolWinSvc
{
    public  class SerialDeviceProvider:UniversalDeviceProvider
    {
        public override event OnDataReceivedEventHandler OnDataReceived;
        public delegate void OnConnectedEventHander();

        public event OnConnectedEventHander OnConnected;

        private AsyncSerial client;
        
        public override void Connect()
        {
            client = new AsyncSerial(ShiolConfiguration.Instance.Config.Communication.SerialSettings);
            client.OnDataReceived += Client_OnSerialDataReceived;
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
           // client.OnErrorReceived += Client_OnErrorReceived;    
            client.Connect();
        }

        private void Client_OnDisconnected(string str)
        {
            Console.WriteLine("Disconnected: " + str);
            LogFile.saveRegistro("Disconnected", levels.info);
        }

        private void Client_OnErrorReceived(SerialErrorReceivedEventArgs ex)
        {
            Console.WriteLine(ex.ToString());
        }

        private void Client_OnConnected()
        {
            Console.WriteLine("Connected.");
            LogFile.saveRegistro("Connected", levels.info);
        }

        public override void Disconnect()
        {
            client.Disconnect();
        }
        
        private void Client_OnSerialDataReceived(string data)
        {
            if (data != null && data.Trim() != "")
            {
                Console.WriteLine(data);
                LogFile.saveRegistro(data, levels.debug);
                if (OnDataReceived != null)
                    OnDataReceived(data);
            }
        }
    }
}



