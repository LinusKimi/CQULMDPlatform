using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys
{
    public class particularNodeConfig: INotifyPropertyChanged
    {
        // 被选中子控制器ID
        private int _selectNodeID = 0;
        public int SelectNodeID { get => _selectNodeID; set { _selectNodeID = value; OnPropertyChanged("SelectNodeID"); } }

        // 被选中子控制器名称
        private string _selectNodeName = "";
        public string SelectNodeName { get => _selectNodeName; set { _selectNodeName = value; OnPropertyChanged("SelectNodeName"); } }

        // 被选中子控制器工作模式  默认连续采样
        private int _selectNodeWorkPatter = 3;
        public int SelectNodeWorkPatter { get => _selectNodeWorkPatter; set { _selectNodeWorkPatter = value; OnPropertyChanged("SelectNodeWorkPatter"); } }

        // 次级源数量
        private int _secondarySourceOutputCnt = 0;
        public int SecondarySourceOutputCnt { get => _secondarySourceOutputCnt; set { _secondarySourceOutputCnt = value; OnPropertyChanged("SecondarySourceOutputCnt"); } }

        // 次级源编号
        private List<int> _secondarySourceOutputChannel = new List<int> { };
        public List<int> SecondarySourceOutputChannel { get => _secondarySourceOutputChannel; set { _secondarySourceOutputChannel = value; OnPropertyChanged("SecondarySourceOutputChannel"); } }

        // 误差传感器数量
        private int _errorSensorCnt = 0;
        public int ErrorSensorCnt { get => _errorSensorCnt;set { _errorSensorCnt = value; OnPropertyChanged("ErrorSensorCnt"); } }

        // 误差传感器编号
        private List<int> _errorSensorList = new List<int> { };
        public List<int> ErrorSensorList { get => _errorSensorList; set { _errorSensorList = value; OnPropertyChanged("ErrorSensorList"); } }

        // 频率个数
        private int _freqCnt = 0;
        public int FreqCnt { get=>_freqCnt; set { _freqCnt = value;OnPropertyChanged("FreqCnt"); } }

        // 频率
        private List<float> _freqList = new List<float> { };
        public List<float> FreqList { get => _freqList;set { _freqList = value;OnPropertyChanged("FreqList"); } }

        // 步长
        private List<float> _freqStep = new List<float> { };
        public List<float> FreqStep { get => _freqStep; set { _freqStep = value; OnPropertyChanged("FreqStep"); } }

        // 辨识幅值
        private float _idenRange = 0;
        public float IdenRange { get => _idenRange; set { _idenRange = value; OnPropertyChanged("IdenRange"); } }

        // 最大幅值
        private float _maxRange = 0;
        public float MaxRange { get => _maxRange; set { _maxRange = value; OnPropertyChanged("MaxRange"); } }


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
