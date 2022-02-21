using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace coordinateCtrlSys
{
    public class UartServer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Logger _logger;
        private string[] _uartPortName;

        private ActionBlock<byte[]> _actionBlock;

        private SerialPort serialPort;

        private List<byte> _uartrecvbuf = new List<byte>() { };

        private byte[] cmdHead = new byte[] { 0xEB, 0x80, 0x08, 0xBE};

        private const int cmdHeadCnt = 4;

        private const int cmdLenByteCnt = 2;

        private const int cmdCRCByteCnt = 1;

        private int neadRecvLength = 0;

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


        public UartServer(Logger logger, ActionBlock<byte[]> actionBlock)
        {
            _logger = logger;

            serialPort = new SerialPort();
            serialPort.DataReceived += _serialPortDataRecv;

            PortName = SerialPort.GetPortNames();
            _actionBlock = actionBlock;
         
        }

        public void CheckPort() => PortName = SerialPort.GetPortNames();

        public bool OpenPort(string _name)
        {
            _uartrecvbuf.Clear();
            bool ret = false;

            if (!((IList)PortName).Contains(_name))
            {
                ret = false;
                return ret;
            }

            try
            {
                serialPort.PortName = _name;
                serialPort.BaudRate = 115200;
                serialPort.DataBits = 8;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.One;

                //serialPort.ReceivedBytesThreshold = 1;


                serialPort.Open();
            }
            catch (Exception e)
            {
                _logger.writeToConsole("串口打开异常 - " + _name);
                _logger.writeToFile("串口打开异常 - " + _name + " | " + e);
                
                return ret;
            }

            ret = true;
            _logger.writeToConsole("串口 " + _name + " 打开正常 ");
            _logger.writeToFile("串口打开正常 - " + _name);

            return ret;
        }

        public void ClosePort() => serialPort.Close();

        public bool IsOpen() => serialPort.IsOpen;

        public void SendData(byte[] data)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(data, 0, data.Length);
            }
            else
            {               
                _logger.writeToConsole("串口未打开！");
            }
        }

        private void _serialPortDataRecv(object sender, SerialDataReceivedEventArgs e)
        {
            int length = serialPort.BytesToRead;
            byte[] buff = new byte[length];//创建缓存数据数组
            serialPort.Read(buff, 0, length);//把数据读取到buff数组

            for (int i = 0; i < length; i++)
            {
                _uartrecvbuf.Add(buff[i]);
            }

            while (_uartrecvbuf.Count >= 4)
            {
                if (_uartrecvbuf[0] != cmdHead[0] ||
                    _uartrecvbuf[1] != cmdHead[1] ||
                    _uartrecvbuf[2] != cmdHead[2] ||
                    _uartrecvbuf[3] != cmdHead[3])
                {
                    _uartrecvbuf.RemoveAt(0);
                    continue;
                }
                else
                {
                    if (_uartrecvbuf.Count < 6)
                        break;
                    else
                    {
                        neadRecvLength = _uartrecvbuf[4] * 256 + _uartrecvbuf[5];
                        int cmdLength = cmdHeadCnt + cmdLenByteCnt + neadRecvLength + cmdCRCByteCnt;

                        if (_uartrecvbuf.Count < cmdLength)
                            break;
                        else
                        {
                            byte[] data = new byte[cmdLength];

                            Parallel.For ( 0, data.Length, i => {
                                data[i] = _uartrecvbuf[i];
                            }) ;
                               
                            _actionBlock.Post(data);
                            _uartrecvbuf.RemoveRange(0, cmdLength);
                        }

                    }
                }
            }
        }

    }
}
