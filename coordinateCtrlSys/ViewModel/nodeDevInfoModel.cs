using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys.ViewModel
{
    public class nodeDevInfoModel: INotifyPropertyChanged
    {
        private int _DevCnt = 0;
        public int DevCnt { get => _DevCnt; set { _DevCnt = value; OnPropertyChanged("DevCnt"); } }

        private int _DevConnect = 0;
        public int DevConnect { get => _DevConnect; set { _DevConnect = value; OnPropertyChanged("DevConnect"); } }

        private float _EmptyCurrent = 0;
        public float EmptyCurrent { get => _EmptyCurrent; set { _EmptyCurrent = value; OnPropertyChanged("EmptyCurrent"); } }

        private int _EmptyCurrentError = 0;
        public int EmptyCurrentError { get => _EmptyCurrentError; set { _EmptyCurrentError = value; OnPropertyChanged("EmptyCurrentError"); } }

        private int _JlinkProg = 0;
        public int JlinkProg { get => _JlinkProg; set { _JlinkProg = value; OnPropertyChanged("JlinkProg"); } }

        private string _nodeVersion = "-";
        public string NodeVersion { get => _nodeVersion; set { _nodeVersion = value; OnPropertyChanged("NodeVersion"); } }

        private int _versionErr = 0;

        public int VersionErr { get => _versionErr; set { _versionErr = value; OnPropertyChanged("VersionErr"); } }       

        private int _FuncTest = 0;
        public int FuncTest { get => _FuncTest; set { _FuncTest = value; OnPropertyChanged("FuncTest"); } }

        private float _BoardCurrent = 0;
        public float BoardCurrent { get => _BoardCurrent; set { _BoardCurrent = value; OnPropertyChanged("BoardCurrent"); } }

        private int _BoardCurrentError = 0;
        public int BoardCurrentError { get => _BoardCurrentError; set { _BoardCurrentError = value; OnPropertyChanged("BoardCurrentError"); } }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
