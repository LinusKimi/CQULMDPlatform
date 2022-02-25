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

        private int _returnSignalCMD;
        public int ReturnSignalCMD
        {
            get { return _returnSignalCMD; }
            set { _returnSignalCMD = value; OnPropertyChanged("ReturnSignalCMD"); }
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

        private string _InnerVersion;

        public string InnerVersion
        {
            get { return _InnerVersion; }
            set { _InnerVersion = value; OnPropertyChanged("InnerVersion"); }
        }

        private string _ResetDev;

        public string ResetDev
        {
            get { return _ResetDev; }
            set { _ResetDev = value; OnPropertyChanged("ResetDev"); }
        }

        private string _USEAE;

        public string USEAE
        {
            get { return _USEAE; }
            set { _USEAE = value; OnPropertyChanged("USEAE"); }
        }

        private bool _EnableAEEA;

        public bool EnableAEEA
        {
            get { return _EnableAEEA; }
            set { _EnableAEEA = value; OnPropertyChanged("EnableAEEA"); }
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
