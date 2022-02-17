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

        private int _JlinkProg = 0;
        public int JlinkProg { get => _JlinkProg; set { _JlinkProg = value; OnPropertyChanged("JlinkProg"); } }

        private int _FuncTest = 0;
        public int FuncTest { get => _FuncTest; set { _FuncTest = value; OnPropertyChanged("FuncTest"); } }

        private float _BoardCurrent = 0;
        public float BoardCurrent { get => _BoardCurrent; set { _BoardCurrent = value; OnPropertyChanged("BoardCurrent"); } }


        //public string NodeSocketID { get => _socketID; set { _socketID = value; OnPropertyChanged("NodeSocketID"); } }
        //public bool NodeSelect { get => _select; set { _select = value; OnPropertyChanged("NodeSelect"); } }
        //public string NodeName { get => _name; set { _name = value; OnPropertyChanged("NodeName"); } }
        //public string NodeIP { get => _ip; set { _ip = value; OnPropertyChanged("NodeIP"); } }
        //public string NodePort { get => _port; set { _port = value; OnPropertyChanged("NodePort"); } }
        //public bool NodeConnect { get => _connect; set { _connect = value; OnPropertyChanged("NodeConnect"); } }
        //public int NodeWorkPattern { get => _workPattern; set { _workPattern = value; OnPropertyChanged("NodeWorkPattern"); } }

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
