using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace coordinateCtrlSys.ViewModel
{
    
    public class MainViewModel : ViewModelBase
    {
        private string _forFactory = "";
        public string forFactory { get => _forFactory; set { _forFactory = value; RaisePropertyChanged(); } }

        private string _PCBAVersion = "";
        public string PCBAVersion { get => _PCBAVersion; set { _PCBAVersion = value; RaisePropertyChanged(); } }

        private string _boardInterface = "";
        public string boardInterface { get => _boardInterface; set { _boardInterface = value; RaisePropertyChanged(); } }

        private bool _portOpened = false;
        public bool portOpend{get => _portOpened;  set { _portOpened = value; RaisePropertyChanged(); }}


        private IConfigReader _configReader;
        public ConfigurationData configurationData;

        public ObservableCollection<nodeDevInfoModel> nodeDevInfoModels_one { get; set; }
        public ObservableCollection<nodeDevInfoModel> nodeDevInfoModels_two { get; set; }

        public MainViewModel(IConfigReader configReader)
        {
            _configReader = configReader;
            nodeDevInfoModels_one = new ObservableCollection<nodeDevInfoModel>
            {
                new nodeDevInfoModel{ DevCnt = 1 },
                new nodeDevInfoModel{ DevCnt = 2 },
                new nodeDevInfoModel{ DevCnt = 3 },
                new nodeDevInfoModel{ DevCnt = 4 },
                new nodeDevInfoModel{ DevCnt = 5 },
                new nodeDevInfoModel{ DevCnt = 6 },
                new nodeDevInfoModel{ DevCnt = 7 },
                new nodeDevInfoModel{ DevCnt = 8 },
            };

            nodeDevInfoModels_two = new ObservableCollection<nodeDevInfoModel>
            {
                new nodeDevInfoModel{ DevCnt = 1 },
                new nodeDevInfoModel{ DevCnt = 2 },
                new nodeDevInfoModel{ DevCnt = 3 },
                new nodeDevInfoModel{ DevCnt = 4 },
                new nodeDevInfoModel{ DevCnt = 5 },
                new nodeDevInfoModel{ DevCnt = 6 },
                new nodeDevInfoModel{ DevCnt = 7 },
                new nodeDevInfoModel{ DevCnt = 8 },
            };

        }

        public void getSettingFile(string path)
        {
            // 两种方式  可扩展/
            configurationData = _configReader.ReadFile(path);
            forFactory = configurationData.systemConfig.Factory;
            PCBAVersion = configurationData.systemConfig.SoftwareVersion;
            boardInterface = configurationData.systemConfig.BoardInterface;
        }
       
    }
}