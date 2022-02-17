using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys
{
    public class UartServer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Logger _logger;
        private string[] _uartPortName;

        private SerialPort serialPort;

        public string[] PortName
        {
            get => _uartPortName;
            set
            {
                _uartPortName = value;
                NotifyPropertyChanged();

                _logger.writeToFile(value + "串口发生变化");
                _logger.writeToConsole(value + "串口发生变化");
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;

            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public UartServer(Logger logger)
        {
            _logger = logger;

            serialPort = new SerialPort();
            serialPort.DataReceived += _serialPortDataRecv;

            PortName = SerialPort.GetPortNames();
        }

        public void CheckPort() => PortName = SerialPort.GetPortNames();

        public void OpenPort(string _name)
        {
            serialPort.PortName = _name;
            serialPort.BaudRate = 115200;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            
            serialPort.ReceivedBytesThreshold = 1;

            serialPort.Open();
            _logger.writeToFile("串口打开 -");
            _logger.writeToConsole("串口打开 -");
        }

        public void ClosePort() => serialPort.Close();

        public void SendData(byte[] data)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(data, 0, data.Length);
            }
            else
            {
                _logger.writeToFile("串口未打开！");
                _logger.writeToConsole("串口未打开！");
            }
        }

        private void _serialPortDataRecv(object sender, SerialDataReceivedEventArgs e)
        { 
        
        }

    }
}
