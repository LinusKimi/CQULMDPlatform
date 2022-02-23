﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Autofac;
using coordinateCtrlSys.ViewModel;
using Crc;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace coordinateCtrlSys
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly IContainer container;

        private DispatcherTimer _UITimer;

        private delegate void ShowMsg();

        private Logger logger;

        private MainViewModel _MainViewModel;

        private ActionBlock<byte[]> _uartActionBlock;

        private UartServer uartServer;

        private Crc8Base crc8 = new Crc8Base(0x07, 0x00, 0x00, false, false);

        private const string DefaultPortName = "COM3";

        private const int PostADCDataCnt = 1400;

        // bool value

        private bool checkJlinkValue = false;

        private bool configSystemDone = false;

        private const int crcByteCnt = 1;

        private string[] jlinkPortSN = new string[2] ;

        private bool[] jlinkUsed = new bool[] { false, false};

        private bool StartSystem = false;

        private bool MCUConnectValue = false;

        private float[,] runCurrentValue = new float[16, 3];

        private enum cmdType {
            // 响应下位机连接
            requestConnected = 0xfc ,
                       
            // Jlink 区分
            requestJlink = 0xf0,
        
            // 获取 node 配置
            getNodeCmd = 0xA0,
            getNodeBool = 0xB0,

            // 上报消息
            putRunStatus = 0xCA,

            // 空板电流
            requestEmptyValue = 0x43,

            // jlink 烧写指令
            requestJlinkProg = 0x50,

            // 运行电流
            requestRunValue_1 = 0x44,
            requestRunValue_2 = 0x45,
            requestRunValue_3 = 0x46,

            requestRealData = 0x5E,

            requestADCStatus = 0x5C

        };
        
        private enum msgType
        {
            startFlag = 0xDA,
            putDownFlag = 0xD0,
            nodeConnect = 0x91,
            nodeShortOut = 0x92,
            nodeVersion = 0x89,
            nodeVersionErr = 0x77
        };

        // ------------- 

        private int cmdcnt = 0;

       



        public MainWindow()
        {
            InitializeComponent();

            var builder = new ContainerBuilder();

            builder.RegisterType<Logger>().SingleInstance();
            builder.RegisterType<MainViewModel>().SingleInstance();
            builder.RegisterType<ReadConfiguration>().As<IConfigReader>().SingleInstance();

            container = builder.Build();

            logger = container.Resolve<Logger>();
            _MainViewModel = container.Resolve<MainViewModel>();

            _uartActionBlock = new ActionBlock<byte[]>(data =>
            {
                ProcessTask(data);
            }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 3 });

            //_uartActionBlock = new ActionBlock<byte[]>(ProcessTask);

            uartServer = new UartServer(logger, _uartActionBlock);

            DataContext = _MainViewModel;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AddMsg("启动应用");

            //_UITimer = new DispatcherTimer();

            //_UITimer.Interval = new TimeSpan(0, 1, 0);
            //_UITimer.IsEnabled = true;
            //_UITimer.Tick += UITimer_timeout;
            //_UITimer.Start();

            checkJlinkTask();
        }

        private void UITimer_timeout(object sender, EventArgs e)
        {
            AddMsg("Timer Task");
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            AddMsg("关闭应用");
        }

        private void AddMsg(string msg)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ShowMsg)delegate
            {
                if (MsgBox.Items.Count >= 50)
                    MsgBox.Items.RemoveAt(0);

                MsgBox.Items.Add(DateTime.Now.ToString("HH:mm:ss") + ": " + msg);
                MsgBox.ScrollIntoView(MsgBox.Items[MsgBox.Items.Count - 1]);
            });

            logger.writeToFile(msg);
        }

        private void checkJlinkTask()
        {
            // 检查 jlink
            AddMsg("检查外设中 ...");

            var lsportJlink = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\lsPort.jlink";

            string _text = "ShowEmuList USB \r\n" +
                            "q";

            File.WriteAllText(lsportJlink, _text);

            var lsjlinkbatpath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\lsJlinkPort.bat";

            string text = "echo start\r\n" +
                "jlink.exe -CommandFile " + lsportJlink;

            File.WriteAllText(lsjlinkbatpath, text);

            var proc = new Process();
            proc.StartInfo.FileName = lsjlinkbatpath;
            proc.StartInfo.CreateNoWindow = true;

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            proc.Start();
            proc.WaitForExit(5);

            string processOut = proc.StandardOutput.ReadToEnd();
            string processError = proc.StandardError.ReadToEnd();

            proc.Close();

            if (string.Empty != processError)
            {
                logger.writeToConsole(processError);
                AddMsg("外设自检执行失败");
            }

            logger.writeToConsole(processOut);

            if (processOut.Contains("J-Link[0]:") && processOut.Contains("J-Link[1]:"))
            {
                int i0 = processOut.IndexOf("Serial number:");
                int i1 = processOut.IndexOf("Serial number:", i0 + 8);

                jlinkPortSN[0] = processOut.Substring(i0 + 15, 8);
                jlinkPortSN[1] = processOut.Substring(i1 + 15, 8);

                AddMsg("JLink[0] SN. " + jlinkPortSN[0]);
                AddMsg("JLink[1] SN. " + jlinkPortSN[1]);

                AddMsg("外设自检执行成功");

                checkJlinkValue = true;
                checkJlink.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddMsg("J-Link 外设连接异常");
            }
        }

        // 检查 J_Link
        private void checkJlink_Click(object sender, RoutedEventArgs e)
        {
            checkJlinkTask();
        }

        // 选择 配置文件
        private void selectJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (!checkJlinkValue)
            {
                AddMsg("请检查JLink设备连接");
                return;
            }

            var openFileDialg = new OpenFileDialog()
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "Settings\\",
                Filter = "json file(*json)|*.json",
                Title = "配置文件选择"
            };

            var selectResult = openFileDialg.ShowDialog();

            if (selectResult != true) return;

            var filePath = openFileDialg.FileName;

            _MainViewModel.getSettingFile(filePath);

            // 判断 配置项
            string binPath = AppDomain.CurrentDomain.BaseDirectory + "FlashBin\\" + _MainViewModel.configurationData.systemConfig.BinFileName;
           

            if (!File.Exists(binPath))
            {               
                AddMsg("烧结文件不存在");
                return;
            }

            if (string.Empty == _MainViewModel.configurationData.systemConfig.MCU ||
                string.Empty == _MainViewModel.configurationData.systemConfig.FlashAddress)
            {
                AddMsg("芯片配置信息不存在");
                return;
            }

            // 初始化烧写脚本
            {
                string _jlinkFileText = "erase\r\n" +
                                        "loadfile " + binPath + " " + _MainViewModel.configurationData.systemConfig.FlashAddress + "\r\n" +
                                        "r\r\n" +
                                        "qc\r\n";

                string _jlinkFilePath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\progBin.jlink";

                File.WriteAllText(_jlinkFilePath, _jlinkFileText);

                string _progBatText = "jlink.exe usb " + jlinkPortSN[0] + " " +
                                      "-device " +
                                      _MainViewModel.configurationData.systemConfig.MCU +
                                      " -if swd -speed 1000 " +
                                      " -CommandFile " + _jlinkFilePath;

                string _progBatPath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\progJlinkBin_0.bat";

                File.WriteAllText(_progBatPath, _progBatText);

                _progBatText = "jlink.exe usb " + jlinkPortSN[1] + " " +
                                      "-device " +
                                      _MainViewModel.configurationData.systemConfig.MCU +
                                      " -if swd -speed 1000 " +
                                      " -CommandFile " + _jlinkFilePath;

                _progBatPath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\progJlinkBin_1.bat";

                File.WriteAllText(_progBatPath, _progBatText);
            }

            configSystemDone = true;

            AddMsg("加载 " + System.IO.Path.GetFileName(filePath));
        }

        // 配置系统 开始运行
        private void configSystem_Click(object sender, RoutedEventArgs e)
        {
            if (!configSystemDone)
            {
                AddMsg("请先选择配置文件");
                return;
            }

            if (!checkJlinkValue)
            {
                AddMsg("请检查JLink设备连接");
                return;
            }

            if (uartServer.OpenPort(DefaultPortName))
            {
                _MainViewModel.portOpend = true;

                StartSystem = true;
                AddMsg("成功打开通讯接口");
            }
            else
            {
                _MainViewModel.portOpend = false;

                StartSystem = false;
                AddMsg("通讯接口打开失败");
            }
        }

        // 停止系统响应
        private void stopSystem_Click(object sender, RoutedEventArgs e)
        {
            if (uartServer.IsOpen())
            {
                uartServer.ClosePort();
            }

            _MainViewModel.portOpend = false;

            StartSystem = false;
            MCUConnectValue = false;

            AddMsg("通讯接口关闭");
        }

        // 主 处理函数
        private void ProcessTask(byte[] data)
        {
            cmdcnt++;

            Console.WriteLine("recv data length: " + data.Length + " cmdcnt: " + cmdcnt);
            Console.WriteLine(" ThreadId:" + Thread.CurrentThread.ManagedThreadId + " Execute Time:" + DateTime.Now);

            // 数据校验

            if (!StartSystem)
            {
                return;
            }

            crc8.AutoReset = true;
            var crcTemp = crc8.ComputeHash(data, 0, data.Length - crcByteCnt);

            if (crcTemp[0] == data[data.Length - 1])
            {
                Console.WriteLine("cmd data check successed !");
            }
            else
            {
                Console.WriteLine("cmd data check failed !");
                return;
            }

            // 数据解析  MCUConnectValue  需要添加判断

            switch ((cmdType)data[6])
            {
                case cmdType.requestConnected:
                    RequestConnectedTask();
                    break;

                case cmdType.requestJlink:
                    bool nodenumber = (data[7] == 0x00) ? true : false;
                    bool ioSet = (data[8] == 0x01) ? true : false;
                    DifferJlink(nodenumber, ioSet);
                    break;

                case cmdType.getNodeCmd:
                    getNodeCmdTask(data);
                    break;

                case cmdType.getNodeBool:
                    getNodeBoolTask(data);
                    break;

                case cmdType.putRunStatus:
                    RunStatusTask(data);
                    break;

                case cmdType.requestEmptyValue:
                    EmptyCurrentTask(data);
                    break;

                case cmdType.requestJlinkProg:
                    ProgrameFlashTask(data);
                    break;

                // 运行电流
                case cmdType.requestRunValue_1:
                    runCurrentTask(data, 0);
                    break;

                case cmdType.requestRunValue_2:
                    runCurrentTask(data, 1);
                    break;

                case cmdType.requestRunValue_3:
                    runCurrentTask(data, 2);
                    break;

                case cmdType.requestRealData:
                    putRealADCDataTask(data);
                    break;

                case cmdType.requestADCStatus:
                    putADCStatusTask(data);
                    break;

                default:

                    break;
            }

        }
       
        // 请求连接
        public void RequestConnectedTask()
        {
            byte[] _responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x01, 0xcf, 0x00 };

            _responseData[7] = crc8.ComputeHash(_responseData, 0, _responseData.Length - 1)[0];

            uartServer.SendData(_responseData);

            MCUConnectValue = true;
            AddMsg("下位机已连接, 等待测试");
        }

        // 区分jlink
        public void DifferJlink(bool nodeNmber, bool ioSet)
        {           
            string _jlinkFileText = (ioSet ? "tck1\r\nt1\r\n": "tck0\r\nt0\r\n") +                                    
                                    "sleep 1000\r\n" +
                                    "q";
            var jlinkFilePath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\diffPort.jlink";

            File.WriteAllText(jlinkFilePath, _jlinkFileText);

            var batFilePath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\diffJlinkPort.bat";

            string _batText = "echo diffJlinkPort\r\n" +
                                "jlink.exe usb "+ jlinkPortSN[nodeNmber ? 0 : 1] +
                                " " +
                                " -CommandFile " + jlinkFilePath;

            File.WriteAllText(batFilePath, _batText);

            var proc = new Process();
            proc.StartInfo.FileName = batFilePath;
            proc.StartInfo.CreateNoWindow = true;

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0xf0, (nodeNmber ? (byte)0x00 : (byte)0x01), (ioSet ? (byte)0x01:(byte)0x00), 0x00 };
            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

            proc.Start();
            uartServer.SendData(responseData);

            proc.WaitForExit(1);

            string processOut = proc.StandardOutput.ReadToEnd();
            string processError = proc.StandardError.ReadToEnd();

            proc.Close();

            if (!processOut.Contains("Sleep"))
            {
                logger.writeToConsole("jlink 区分执行失败");
                return;
            }

            AddMsg("Jlink[" + (nodeNmber?"0":"1") + "] " + "区分指令执行");

        }

        // 获取 节点配置
        public void getNodeCmdTask(byte[] requestData)
        {
            string _configData = "";
            byte[] returnBytes;
            int cmdLen = 0;
            byte[] responseData;

            List<byte> _resData = new List<byte>();
            _resData.Clear();
            _resData.Add(0xeb); _resData.Add(0x90); _resData.Add(0x09); _resData.Add(0xbe);

            switch ((byte)requestData[7])
            {
                case 0x0D:
                    _configData = _MainViewModel.configurationData.ConfigurationNode.SignalCMD.Replace(" ", "");
                    break;

                case 0x89:
                    _configData = _MainViewModel.configurationData.ConfigurationNode.InnerVersion.Replace(" ", "");
                    break;

                case 0xFF:
                    _configData = _MainViewModel.configurationData.ConfigurationNode.ResetDev.Replace(" ", "");
                    break;

                case 0xAE:
                    _configData = _MainViewModel.configurationData.ConfigurationNode.USEAE.Replace(" ", "");
                    break;

                case 0xC0:
                    string connectType = _MainViewModel.configurationData.systemConfig.BoardInterface;

                    _resData.Add(0x00); _resData.Add(0x07);
                    _resData.Add(0xA0); _resData.Add(0xC0);

                    UInt32 _baud = 0;

                    if (connectType.Contains("IIC"))
                    {                        
                        _resData.Add(0x01);
                        _baud = (UInt32)int.Parse(_MainViewModel.configurationData.systemConfig.IICBaud);
                    }
                    else if(connectType.Contains("UART"))
                    {
                        _resData.Add(0x00);
                        _baud = (UInt32)int.Parse(_MainViewModel.configurationData.systemConfig.UARTBaud);
                    }

                    _resData.Add((byte)((_baud >> 24) & 0xFF));
                    _resData.Add((byte)((_baud >> 16) & 0xFF));
                    _resData.Add((byte)((_baud >> 8) & 0xFF));
                    _resData.Add((byte)((_baud >> 0) & 0xFF));

                    _resData.Add(0x00);

                    responseData = _resData.ToArray();
                    responseData[responseData.Length - 1] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

                    uartServer.SendData(responseData);
                    return;

                default:

                    return;
            }

            returnBytes = new byte[_configData.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(_configData.Substring(i * 2, 2), 16);

            cmdLen = 2 + returnBytes.Length;

            _resData.Add((byte)(cmdLen / 8));
            _resData.Add((byte)(cmdLen % 8));

            _resData.Add(0xA0); _resData.Add(0x0D);

            for (int i = 0; i < returnBytes.Length; i++)
                _resData.Add(returnBytes[i]);

            responseData = new byte[_resData.Count + 1];
            Parallel.For(0, _resData.Count, i =>
            {
                responseData[i] = _resData[i];
            });

            responseData[_resData.Count] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

            uartServer.SendData(responseData);

        }

        // 获取 节点 Bool 配置
        public void getNodeBoolTask(byte[] requestData)
        {
            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0xb0, 0xe5, 0x00, 0x00 };

            var _config = _MainViewModel.configurationData.ConfigurationNode.EnableAEEA;

            responseData[8] = _config ? (byte)0x01 : (byte)0x00;
            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

            uartServer.SendData(responseData);
        }

        // 运行状态 上报 处理函数
        public void RunStatusTask(byte[] requestData)
        {
            switch ((msgType)requestData[7])
            {
                case msgType.startFlag:
                    _MainViewModel.StartStatus();
                    AddMsg("开始测试流程");
                    break;

                case msgType.putDownFlag:
                    AddMsg("压板" + (requestData[8] == 0x01 ? "" : "不") + "到位");
                    break;

                case msgType.nodeConnect:
                    int nodeNumber = (ushort)requestData[8];
                    bool nodeConnectFlag = (requestData[9] == 0x01) ? true : false;

                    _MainViewModel.nodeConnectStatus((nodeNumber / 8), (nodeNumber % 8 + 1), nodeConnectFlag);

                    AddMsg("Block " + (nodeNumber / 8 + 1) + "# No. " + (nodeNumber % 8 + 1) + (nodeConnectFlag ? " " : " 未") + "连接");
                    break;

                case msgType.nodeShortOut:
                    int nodeShortOutNumber = (ushort)requestData[8];
                    bool nodeShortOutFlag = (requestData[9] == 0x01) ? true : false;

                    _MainViewModel.nodeShortOutStatus((nodeShortOutNumber / 8), (nodeShortOutNumber % 8 + 1), nodeShortOutFlag ? 2 : 3);

                    AddMsg("Block " + (nodeShortOutNumber / 8 + 1) + "# No. " + (nodeShortOutNumber % 8 + 1) + (nodeShortOutFlag ? " " : " 未") + "短路");

                    break;

                case msgType.nodeVersion:
                    int nodeVersion = (ushort)requestData[8];

                    string _version = Encoding.ASCII.GetString(requestData, 9, requestData.Length - 9);

                    _MainViewModel.nodeVersionStatus((nodeVersion / 8 + 1), (nodeVersion % 8 + 1), false, _version.Substring(_version.Length - 4, 4));

                    break;

                case msgType.nodeVersionErr:
                    int nodeVersionErr = (ushort)requestData[8];

                    _MainViewModel.nodeVersionStatus((nodeVersionErr / 8 + 1), (nodeVersionErr % 8 + 1), true, "超时");

                    break;

                default:
                    break;
            }
        }

        // 空板电流 处理函数
        public void EmptyCurrentTask(byte[] requestData)
        {
            int nodeNumber = (ushort)requestData[7];

            float[] adcData = new float[100];

            Parallel.For(0, 100, i=> {
                adcData[i] = (float)BitConverter.ToUInt16(requestData, i * 2 + 8) ;
            });

            double s = 0;
            for (int i = 0; i < 100; i++)
            {
                s += adcData[i] * adcData[i];
            }

            double rmsdata = Math.Sqrt(s / 100);

            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x43, (requestData[7]), 0x00, 0x00 };

            var dataRange = _MainViewModel.configurationData.ConfigurationNode.EmptyCurrentValue;
            bool statusFlag = false;

            if (rmsdata < dataRange[0] || rmsdata > dataRange[1])
            {
                responseData[8] = 0x00;
            }
            else
            {
                responseData[8] = 0x01;
                statusFlag = true;
            }

            _MainViewModel.nodeEmptyCurrentStatus((nodeNumber / 8), (nodeNumber % 8 + 1), statusFlag, (float)Math.Round(rmsdata, 2));

            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

            uartServer.SendData(responseData);
        }

        // jlink 烧写处理函数
        public void ProgrameFlashTask(byte[] requestData)
        {
            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x50, (requestData[7]), 0x00, 0x00 };

            int nodeNumber = (ushort)requestData[7];
            int blockNumber = nodeNumber / 8;

            if (jlinkUsed[blockNumber == 0 ? 0 : 1])
            {
                responseData[8] = 0xBB;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                AddMsg("Jlink[" + blockNumber + "] 正在使用中");
                return;
            }

            jlinkUsed[blockNumber == 0 ? 0 : 1] = true;

            var proc = new Process();
            proc.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\progJlinkBin_" + (blockNumber == 0 ? 0 : 1) + ".bat";
            proc.StartInfo.CreateNoWindow = true;

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            proc.Start();
            proc.WaitForExit(5);

            string processOut = proc.StandardOutput.ReadToEnd();
            proc.Close();

            jlinkUsed[blockNumber == 0 ? 0 : 1] = false;

            if (processOut.Contains("Downloading file") &&
                processOut.Contains("Reset delay:") &&
                processOut.Contains("Script processing completed"))
            {
                responseData[8] = 0x01;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.jlinkProgStatus((blockNumber == 0 ? 0 : 1), (nodeNumber % 8 + 1), true);
                AddMsg("Block " + (blockNumber + 1) + " NO."+ (nodeNumber % 8 + 1) + " 烧写成功");

                return;
            }
            else
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.jlinkProgStatus((blockNumber == 0 ? 0 : 1), (nodeNumber % 8 + 1), false);
                AddMsg("Block " + (blockNumber + 1) + " NO." + (nodeNumber % 8 + 1) + " 烧写失败");

                return;
            }


        }

        // 运行 电流处理函数
        public void runCurrentTask(byte[] requestData, int stepCnt)
        {
            int nodeNumber = requestData[7];

            int blockNumber = nodeNumber / 8 + 1;

            float[] adcData = new float[100];

            Parallel.For(0, 100, i=> {
                adcData[i] = (float)BitConverter.ToUInt16(requestData, i * 2 + 8);
            });

            double s = 0;
            for (int i = 0; i < 100; i++)
            {
                s += adcData[i] * adcData[i];
            }

            float rmsdata = (float)Math.Round(Math.Sqrt(s / 100), 2);

            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, (requestData[6]), (requestData[7]), 0x00, 0x00 };

            var runCurrntRange = _MainViewModel.configurationData.ConfigurationNode.BoardCurrentValue;

            runCurrentValue[nodeNumber, stepCnt - 1] = rmsdata;

            if (rmsdata < runCurrntRange[0] || rmsdata > runCurrntRange[1])
            {
                responseData[8] = 0x00;

                // show view
                _MainViewModel.boardCurrentTask(blockNumber, nodeNumber + 1, rmsdata, 1);
            }
            else
            {
                if (stepCnt == 3)
                {
                    // show view
                    float _sum = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        _sum += runCurrentValue[nodeNumber, i];
                    }

                }

                responseData[8] = 0x01;
            }

            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

            uartServer.SendData(responseData);


        }

        // 上报 ADC数据
        public void putRealADCDataTask(byte[] requestData)
        {
            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x5E, (requestData[7]), 0x00, 0x00 };

            // 地址修改
            if ((requestData.Length != (PostADCDataCnt * 2)) || requestData[PostADCDataCnt * 2 + 1] == 0xEB)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                logger.writeToConsole("ADC data cmd error");
                return;
            }

            float[] adcData = new float[PostADCDataCnt];

            Parallel.For(0, PostADCDataCnt, i =>
            {
                adcData[i] = BitConverter.ToSingle(requestData, i * 4 + 8);
            });

            float[] peakValue = new float[4];
            float[] peakIndex = new float[4];
            Parallel.For(0, 4, n =>
            {
                
            });

            // 添加判断算法


        }

        // ADC 数据异常报告
        public void putADCStatusTask(byte[] requestData)
        {
            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x5C, (requestData[7]), 0x00, 0x00 };

            int nodeNumber = requestData[7];

            responseData[8] = 0x01;
            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
            uartServer.SendData(responseData);

            _MainViewModel.funTestTask((nodeNumber / 8 + 1), (nodeNumber % 8 + 1), false);

            AddMsg("Block "+ (nodeNumber / 8 + 1) + " # No. " + (nodeNumber % 8 + 1) + " ADC OR/UR");
        }

        
    }
}
