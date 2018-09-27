using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using RJCP.IO.Ports;

namespace ShiolWinSvc
{
    class AsyncSerial
    {
        public delegate void OnConnectedEventHander();
        public delegate void OnDisconnectedEventHander(string str);
        public delegate void OnConnectExceptionHandler(Exception ex);
        public delegate void OnDataReceivedEventHandler(string data);
        public delegate void OnErrorExceptionHandler(SerialErrorReceivedEventArgs ex);
        public event OnConnectedEventHander OnConnected;
        public event OnDisconnectedEventHander OnDisconnected;
        public event OnDataReceivedEventHandler OnDataReceived;
        public event OnErrorExceptionHandler OnErrorReceived;


        public AsyncSerial(string settings)
        {
            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPortStream();

            // Set the read/write timeouts
        //    _serialPort.ReadTimeout = 500;
        //    _serialPort.WriteTimeout = 500;
    
            String[] fila = settings.Split(',');

            SetPortName(fila[0].ToString()); //"NombrePuerto"
            SetPortBaudRate(Convert.ToInt32(fila[1].ToString())); //"velocidadPuerto"
            SetPortDataBits(Convert.ToInt32(fila[2].ToString())); //"bitsDatos"
            Parity paridad = (Parity)Convert.ToInt32(fila[3].ToString() == "None" ? "0" : fila[3].ToString()); // Parity
            SetPortParity(paridad); 
            Handshake flujo = (Handshake)Convert.ToInt32(fila[4].ToString() == "None" ? "0" : fila[4].ToString()); //flujo
            SetPortHandshake(flujo);

            Console.WriteLine($"Set to: {_serialPort.PortName},{_serialPort.BaudRate},{_serialPort.DataBits},{_serialPort.Parity},{_serialPort.Handshake}");
        }

        static bool _continue;
        //private SerialPort _serialPort;
        private Thread _serialReadThread;
        private SerialPortStream _serialPort;

        public void Connect()
        {
            try
            {

                if (_serialPort.IsOpen)
                    _serialPort.Close();
                
                _serialPort.ReadTimeout = -1;
                _serialPort.ErrorReceived += _serialPort_ErrorReceived;
                _serialPort.Open();
                if (_serialPort.IsOpen)
                {
                    if (OnConnected != null)
                        OnConnected();

                    _continue = true;
                    _serialReadThread = new Thread(SerialPortRead);
                    _serialReadThread.Start();
                }
            } catch (Exception e)
            {
                if (OnDisconnected != null)
                    OnDisconnected(e.Message);
            }
        }
        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            if (OnErrorReceived != null)
                OnErrorReceived(e);
        }
        public void Disconnect()
        {   
            Close();
            
        }
        public void Close()
        {
            _continue = false;
            _serialPort.Close();
            _serialReadThread.Join();
        }
        public void SerialPortRead()
        {
            while (_continue)
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        string message = _serialPort.ReadLine();
                        
                        if (OnDataReceived != null)
                            OnDataReceived(message);
                    }
                    catch (TimeoutException) { }
                }
            }
        }
        public string SetPortName(string defaultPortName)
        {
            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPortStream.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }
            _serialPort.PortName = defaultPortName;

            return defaultPortName;
        }
        public int SetPortBaudRate(int defaultPortBaudRate)
        {  
            _serialPort.BaudRate = defaultPortBaudRate;
            return defaultPortBaudRate;
        }
        public Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            //Console.WriteLine("Available Parity options:");
            /*
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                Console.WriteLine("   {0}", s);
            }
            */
            
             parity = defaultPortParity.ToString();

            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity, true);
                
            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }
        public int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;
            
            dataBits = defaultPortDataBits.ToString();

            _serialPort.DataBits = int.Parse(dataBits.ToUpperInvariant());
            return int.Parse(dataBits.ToUpperInvariant());
        }
        public StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            //Console.WriteLine("Available StopBits options:");
            /*
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                Console.WriteLine("   {0}", s);
            }
            */

            Console.Write("Enter StopBits value (None is not supported and \n" +
             "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            
            stopBits = defaultPortStopBits.ToString();

            _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }
        public Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            //Console.WriteLine("Available Handshake options:");
            /*
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                Console.WriteLine("   {0}", s);
            }
            */
            
            handshake = defaultPortHandshake.ToString();
            
            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }
        
    }
}
