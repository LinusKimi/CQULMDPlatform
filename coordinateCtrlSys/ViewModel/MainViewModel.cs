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

        public void StartStatus()
        {
            foreach (var item in nodeDevInfoModels_one)
            {
                item.DevConnect = 0;
                item.EmptyCurrent = 0;
                item.EmptyCurrentError = 0;
                item.JlinkProg = 0;
                item.NodeVersion = "-";
                item.VersionErr = 0;
                item.FuncTest = 0;
                item.BoardCurrent = 0;
                item.BoardCurrentError = 0;
            }

            foreach (var item in nodeDevInfoModels_two)
            {
                item.DevConnect = 0;
                item.EmptyCurrent = 0;
                item.EmptyCurrentError = 0;
                item.JlinkProg = 0;
                item.FuncTest = 0;
                item.BoardCurrent = 0;
                item.BoardCurrentError = 0;
            }
        }

        public void nodeConnectStatus(int block, int nodeNo, bool status)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                        item.DevConnect = status ? 0 : 1;
                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                        item.DevConnect = status ? 0 : 1;
                }
            }
        }

        public void nodeShortOutStatus(int block, int nodeNo, int status)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                        item.DevConnect = status;
                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                        item.DevConnect = status;
                }
            }
        }

        public void nodeVersionStatus(int block, int nodeNo, bool vE, byte[] data)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        if (vE)
                        {
                            item.VersionErr = 1;
                            item.NodeVersion = "超时";
                        }
                        else
                        {
                            item.VersionErr = 2;
                            item.NodeVersion = System.Text.Encoding.ASCII.GetString(data);
                        }
                    }

                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        if (vE)
                        {
                            item.VersionErr = 1;
                            item.NodeVersion = "超时";
                        }
                        else
                        {
                            item.VersionErr = 2;
                            item.NodeVersion = System.Text.Encoding.ASCII.GetString(data);
                        }
                    }
                }
            }

        }

        public void nodeEmptyCurrentStatus(int block, int nodeNo, bool flag, float data)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.EmptyCurrent = data;
                        item.EmptyCurrentError = flag ? 2 : 1;
                    }

                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.EmptyCurrent = data;
                        item.EmptyCurrentError = flag ? 2 : 1;
                    }
                }
            }

        }






    }
}