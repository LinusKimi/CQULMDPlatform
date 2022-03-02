using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys
{
    public class particularSystemConfig : INotifyPropertyChanged
    {
        private string _Factory;

        public string Factory
        {
            get { return _Factory; }
            set { _Factory = value; OnPropertyChanged("Factory"); }
        }

        // 软件版本
        private string _SoftwareVersion;

        public string SoftwareVersion
        {
            get { return _SoftwareVersion; }
            set { _SoftwareVersion = value; OnPropertyChanged("SoftwareVersion"); }
        }


        // 下位机接口 IIC / UART
        private string _BoardInterface;

        public string BoardInterface
        {
            get { return _BoardInterface; }
            set { _BoardInterface = value; OnPropertyChanged("BoardInterface"); }
        }


        // IIC 速率
        private string _IICBaud;

        public string IICBaud
        {
            get { return _IICBaud; }
            set { _IICBaud = value; OnPropertyChanged("IICBaud"); }
        }


        // 串口 波特率 （8 - N - 1 暂时默认）
        private string _UARTBaud;

        public string UARTBaud
        {
            get { return _UARTBaud; }
            set { _UARTBaud = value; OnPropertyChanged("UARTBaud"); }
        }


        // 芯片类型
        private string _MCU;

        public string MCU
        {
            get { return _MCU; }
            set { _MCU = value; OnPropertyChanged("MCU"); }
        }


        // 芯片烧写Flash首地址
        private string _FlashAddress;

        public string FlashAddress
        {
            get { return _FlashAddress; }
            set { _FlashAddress = value; OnPropertyChanged("FlashAddress"); }
        }


        // 烧结文件名
        private string _BinFileName;

        public string BinFileName
        {
            get { return _BinFileName; }
            set { _BinFileName = value; OnPropertyChanged("BinFileName"); }
        }

        private string _BinFileSecurity;

        public string BinFileSecurity
        {
            get { return _BinFileSecurity; }
            set { _BinFileSecurity = value; OnPropertyChanged("BinFileSecurity"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string v)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }
    }
}
