using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace coordinateCtrlSys.ViewModel
{
    
    public class MainViewModel : ViewModelBase
    {
        private string _forFactory = "HK";
        public string forFactory { get => _forFactory; set { _forFactory = value; RaisePropertyChanged(); } }

        private string _PCBAVersion = "V 2022-02-17";
        public string PCBAVersion { get => _PCBAVersion; set { _PCBAVersion = value; RaisePropertyChanged(); } }

        private string _boardInterface = "UART";
        public string boardInterface { get => _boardInterface; set { _boardInterface = value; RaisePropertyChanged(); } }


        public ObservableCollection<nodeDevInfoModel> nodeDevInfoModels_one { get; set; }

        public MainViewModel()
        {
            nodeDevInfoModels_one = new ObservableCollection<nodeDevInfoModel>
            {
                new nodeDevInfoModel{ DevCnt = 0 },
                new nodeDevInfoModel{ DevCnt = 1 }
            };


        }

       
    }
}