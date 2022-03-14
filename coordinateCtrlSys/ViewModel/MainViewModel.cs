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

        // 解析配置
        public void getSettingFile(string path)
        {
            // 两种方式  可扩展/
            configurationData = _configReader.ReadFile(path);
            forFactory = configurationData.systemConfig.Factory;
            PCBAVersion = configurationData.systemConfig.SoftwareVersion;

            var _t = configurationData.systemConfig.BoardInterface;

            if (_t.Contains("IIC"))
                boardInterface = _t + " / " + configurationData.systemConfig.IICBaud;
            else if (_t.Contains("UART"))
                boardInterface = _t + " / " + configurationData.systemConfig.UARTBaud;
        }

        public void clearSettingFile()
        {
            boardInterface = "";
            PCBAVersion = "";
            forFactory = "";
            configurationData = null;
        }

        // 开始测试
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
                item.NodeVersion = "-";
                item.VersionErr = 0;
                item.FuncTest = 0;
                item.BoardCurrent = 0;
                item.BoardCurrentError = 0;
            }
        }

        // 节点 连接状态
        public void nodeConnectStatus(int block, int nodeNo, bool status)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                        item.DevConnect = status ? 2 : 1;
                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                        item.DevConnect = status ? 2 : 1;
                }
            }
        }

        // 节点短路状态
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

        // 空板电流
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

        // 烧写 bin
        public void jlinkProgStatus(int block, int nodeNo, bool flag)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.JlinkProg = flag ? 2 : 1;
                    }

                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.JlinkProg = flag ? 2 : 1;
                    }
                }
            }

        }

        // 节点版本号
        public void nodeVersionStatus(int block, int nodeNo, bool vE, string data)
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
                            item.NodeVersion = data;
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
                            item.NodeVersion = data;
                        }
                    }
                }
            }

        }

        // 运行电流
        public void boardCurrentTask(int block, int nodeNo, float value, int flag)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.BoardCurrent = value;
                        item.BoardCurrentError = flag;
                    }

                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.BoardCurrent = value;
                        item.BoardCurrentError = flag;
                    }
                }
            }
        }

        // 功能测试
        public void funTestTask(int block, int nodeNo, bool flag)
        {
            if (block == 0)
            {
                foreach (var item in nodeDevInfoModels_one)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.FuncTest = flag ? 1 : 2;
                    }

                }
            }

            if (block == 1)
            {
                foreach (var item in nodeDevInfoModels_two)
                {
                    if (item.DevCnt == nodeNo)
                    {
                        item.FuncTest = flag ? 1 : 2;
                    }
                }
            }
        }

    }
}