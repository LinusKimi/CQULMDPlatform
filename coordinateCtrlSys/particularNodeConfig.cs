using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys
{
    public class particularNodeConfig : INotifyPropertyChanged
    {
        // 单次检测指令
        private string _singleCMD = "";
        public string SignalCMD { 
            get => _singleCMD; 
            set { _singleCMD = value; OnPropertyChanged("SignalCMD"); } 
        }

        private string _continualCMD = "";
        public string ContinualCMD {
            get => _continualCMD; 
            set { _continualCMD = value; OnPropertyChanged("ContinualCMD"); } 
        }

        private string _stopContCMD = "";
        public string StopContCMD { 
            get => _stopContCMD; 
            set { _stopContCMD = value; OnPropertyChanged("StopContCMD"); }
        }

        private List<float> _EmptyCurrentValue;

        public List<float> EmptyCurrentValue
        {
            get { return _EmptyCurrentValue; }
            set { _EmptyCurrentValue = value; OnPropertyChanged("EmptyCurrentValue"); }
        }

        private List<float> _BoardCurrentValue;

        public List<float> BoardCurrentValue
        {
            get { return _BoardCurrentValue; }
            set { _BoardCurrentValue = value; OnPropertyChanged("BoardCurrentValue"); }
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
