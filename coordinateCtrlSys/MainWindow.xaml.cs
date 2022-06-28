using System;
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

        //private ActionBlock<byte[]> _uartActionBlock;

        private UartServer uartServer;

        private Crc8Base crc8 = new Crc8Base(0x07, 0x00, 0x00, false, false);

        private const string DefaultPortName = "COM27";

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

        private string RawDataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rawData");

        private enum cmdType {
            //// 响应下位机连接
            //requestConnected = 0xfc ,
                       
            // Jlink 区分
            requestJlink = 0xf0,
        
            // 获取 node 配置
            getNodeCmd = 0xA0,

            // 上报消息
            putRunStatus = 0xCA,

            // 空板电流
            //requestEmptyValue = 0x43,

            // jlink 烧写指令
            requestJlinkProg = 0x50,

            // 运行电流
            //requestRunValue_1 = 0x44,
            //requestRunValue_2 = 0x45,
            //requestRunValue_3 = 0x46,

            // 上报 实际adc 数据
            requestRealData = 0x5E,

            // ADC 超出动态范围
            requestADCStatus = 0x5C

        };
        
        private enum msgType
        {
            // jlink 检测错误
            jlinkDiffErrFlag = 0xF1,

            // 可以开始测试上报
            startFlag = 0xDA,

            // 按下测试按钮
            buttonDownFlag = 0xDB,

            // 压板到位状态指示
            putDownFlag = 0xD0,

            // 节点存在状态上报
            nodeConnect = 0x90,

            // 节点测试硬件版本上报
            nodeBlockVersion = 0x91,

            //节点短路
            nodeShortOut = 0x92,

            // 空板/运行  电流
            emptyCurValue = 0x43,
            runCurrValue_0 = 0x44,
            runCurrValue_1 = 0x45,
            runCurrValue_2 = 0x46,

            // 节点软件版本
            nodeVersion = 0x89,

            // 节点软件版本超时
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

            //builder.Register(c => new ActionBlock<byte[]>(ProcessTask)).SingleInstance();

            builder.Register(c => new ActionBlock<byte[]>(data=> {
                ProcessTask(data);
            }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1})).SingleInstance();

            builder.RegisterType<UartServer>().SingleInstance();

            container = builder.Build();

            logger = container.Resolve<Logger>();
            _MainViewModel = container.Resolve<MainViewModel>();

            //_uartActionBlock = new ActionBlock<byte[]>(data =>
            //{
            //    ProcessTask(data);
            //}, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });         

            uartServer = container.Resolve<UartServer>();
          
            DataContext = _MainViewModel;

            crc8.AutoReset = true;
            
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            logger.writeToFile("=============================>");
            AddMsg("启动应用");

            // 检查保存数据文件
            if (Directory.Exists(RawDataDir) == false)
            {
                Directory.CreateDirectory(RawDataDir);
            }

            _UITimer = new DispatcherTimer();

            _UITimer.Interval = new TimeSpan(0, 0, 10);

            _UITimer.Tick += UITimer_timeout;         

            checkJlinkTask();
        }

        private void UITimer_timeout(object sender, EventArgs e)
        {
            _UITimer.Stop();

            if (uartServer.IsOpen())
            {
                uartServer.ClosePort();
            }

            _MainViewModel.portOpend = false;

            StartSystem = false;
            MCUConnectValue = false;

            AddMsg("下位机连接超时");
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            if (_MainViewModel.portOpend)
            {
                RequestDisonnectedTask();
            }

            if (uartServer.IsOpen())
            {
                uartServer.ClosePort();
            }
            logger.writeToFile("=============================>");
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
                            "qc";

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
                int e0 = processOut.IndexOf(",", i0 + 1);
                int i1 = processOut.IndexOf("Serial number:", e0 + 8);
                int e1 = processOut.IndexOf(",", i1 + 1);

                jlinkPortSN[0] = processOut.Substring(i0 + 15, e0 - i0 - 15).Replace(" ", "");
                jlinkPortSN[1] = processOut.Substring(i1 + 15, e1 - i1 - 15).Replace(" ", "");

                // 新增Jlink SN 排序功能
                try
                {
                    var _J_0 = Convert.ToInt32(jlinkPortSN[0]);
                    var _J_1 = Convert.ToInt32(jlinkPortSN[1]);

                    if (_J_0 > _J_1)
                    {
                        var _t = jlinkPortSN[0];
                        jlinkPortSN[0] = jlinkPortSN[1];
                        jlinkPortSN[1] = _t;
                    }

                }
                catch (Exception e)
                {
                    logger.writeToFile("jlink SN short - error " + e);
                    AddMsg("Jlink SN 排序错误");
                    AddMsg("请联系技术支持");

                    return;
                }
                
                // ...

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
                _MainViewModel.clearSettingFile();
                return;
            }

            /*** MD5 Check * http://www.metools.info/other/o21.html **/

            byte[] retMD5Val;
            byte[] checkMd5 = new byte[_MainViewModel.configurationData.systemConfig.BinFileSecurity.Length / 2];

            using (var _binFS = new FileStream(binPath, FileMode.Open))
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                retMD5Val = md5.ComputeHash(_binFS);
            }

            for (int i = 0; i < checkMd5.Length; i++)
                checkMd5[i] = Convert.ToByte(_MainViewModel.configurationData.systemConfig.BinFileSecurity.Substring(i * 2, 2), 16);

            if (retMD5Val.Length != checkMd5.Length)
            {
                AddMsg("烧结文件校验失败");
                _MainViewModel.clearSettingFile();
                return;
            }
            else
            {
                for (int j = 0; j < retMD5Val.Length; j++)
                {
                    if (retMD5Val[j] != checkMd5[j])
                    {
                        AddMsg("烧结文件校验失败");
                        _MainViewModel.clearSettingFile();
                        return;
                    }
                }
            }

            if (string.Empty == _MainViewModel.configurationData.systemConfig.MCU ||
                string.Empty == _MainViewModel.configurationData.systemConfig.FlashAddress)
            {
                AddMsg("芯片配置信息不存在");
                _MainViewModel.clearSettingFile();
                return;
            }

            // 初始化烧写脚本
            {
                string _jlinkFileText = "erase\r\n" +
                                        "loadfile " + binPath + " " + _MainViewModel.configurationData.systemConfig.FlashAddress + "\r\n" +
                                        //"r\r\n" +
                                        "qc\r\n";

                string _jlinkFilePath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\progBin.jlink";

                File.WriteAllText(_jlinkFilePath, _jlinkFileText);

                string _progBatText = "jlink.exe usb " + jlinkPortSN[0] + " " +
                                      "-device " +
                                      _MainViewModel.configurationData.systemConfig.MCU +
                                      " -if swd -speed 4000 " +
                                      " -CommandFile " + _jlinkFilePath;

                string _progBatPath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\progJlinkBin_0.bat";

                File.WriteAllText(_progBatPath, _progBatText);

                _progBatText = "jlink.exe usb " + jlinkPortSN[1] + " " +
                                      "-device " +
                                      _MainViewModel.configurationData.systemConfig.MCU +
                                      " -if swd -speed 4000 " +
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

                RequestConnectedTask();
               
                _UITimer.Start();

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
            if (_MainViewModel.portOpend)
            {
                RequestDisonnectedTask();
            }

            if (uartServer.IsOpen())
            {
                uartServer.ClosePort();
            }

            if (_UITimer.IsEnabled)
            {
                _UITimer.Stop();
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
            if (MCUConnectValue)
            {
                ;
            }
            else
            {
                if (_UITimer.IsEnabled)
                {
                    _UITimer.Stop();
                    MCUConnectValue = true;
                    AddMsg("下位机已连接, 等待测试");
                }
                else
                {
                    return;
                }
            }

            for (int i = 0; i < data.Length; i++)
            {
                Console.Write("{0:X2} ", data[i]);               
            }
            Console.WriteLine();

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
                //case cmdType.requestConnected:
                //    RequestConnectedTask();
                //    break;

                case cmdType.requestJlink:
                    DifferJlink();
                    break;

                case cmdType.getNodeCmd:
                    getNodeCmdTask(data);
                    break;

                case cmdType.putRunStatus:
                    RunStatusTask(data);
                    break;

                //case cmdType.requestEmptyValue:
                //    EmptyCurrentTask(data);
                //    break;

                case cmdType.requestJlinkProg:
                    ProgrameFlashTask(data);
                    break;

                //// 运行电流
                //case cmdType.requestRunValue_1:
                //    runCurrentTask(data, 0);
                //    break;

                //case cmdType.requestRunValue_2:
                //    runCurrentTask(data, 1);
                //    break;

                //case cmdType.requestRunValue_3:
                //    runCurrentTask(data, 2);
                //    break;

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

            AddMsg("请求连接指令已发送");
        }

        public void RequestDisonnectedTask()
        {
            byte[] _responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x01, 0xfe, 0x00 };

            _responseData[7] = crc8.ComputeHash(_responseData, 0, _responseData.Length - 1)[0];

            uartServer.SendData(_responseData);

            AddMsg("复位下位机指令已发送");
        }

        // 区分jlink
        public void DifferJlink()
        {           
            string _jlinkFileText = "tck1\r\nt1\r\n" +                                   
                                    "sleep 500\r\n" +
                                    "tck0\r\nt0\r\n" +
                                    "sleep 500\r\n" +
                                    "qc";
            var jlinkFilePath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\diffPort.jlink";

            File.WriteAllText(jlinkFilePath, _jlinkFileText);

            var batFilePath_0 = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\diffJlinkPort_0.bat";

            string _batText = "echo diffJlinkPort\r\n" +
                                "jlink.exe usb "+ jlinkPortSN[0] +
                                " " +
                                " -CommandFile " + jlinkFilePath;

            File.WriteAllText(batFilePath_0, _batText);

            var batFilePath_1 = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\diffJlinkPort_1.bat";

            string _batText_1 = "echo diffJlinkPort\r\n" +
                                "jlink.exe usb " + jlinkPortSN[1] +
                                " " +
                                " -CommandFile " + jlinkFilePath;

            File.WriteAllText(batFilePath_1, _batText_1);


            var proc_0 = new Process();
            proc_0.StartInfo.FileName = batFilePath_0;
            proc_0.StartInfo.CreateNoWindow = true;
            
            proc_0.StartInfo.RedirectStandardOutput = true;
            proc_0.StartInfo.RedirectStandardError = true;
            proc_0.StartInfo.UseShellExecute = false;

            var proc_1 = new Process();
            proc_1.StartInfo.FileName = batFilePath_1;
            proc_1.StartInfo.CreateNoWindow = true;

            proc_1.StartInfo.RedirectStandardOutput = true;
            proc_1.StartInfo.RedirectStandardError = true;
            proc_1.StartInfo.UseShellExecute = false;

            proc_0.Start();
            proc_1.Start();

            proc_0.WaitForExit(2);
            proc_1.WaitForExit(2);

            string processOut_0 = proc_0.StandardOutput.ReadToEnd();
            string processError_0 = proc_0.StandardError.ReadToEnd();

            string processOut_1 = proc_1.StandardOutput.ReadToEnd();
            string processError_1 = proc_1.StandardError.ReadToEnd();

            proc_0.Close();
            proc_1.Close();

            if (string.Empty != processError_0 && string.Empty != processError_1)
            {
                AddMsg("jlink 测试指令无效");
            }

            if (!processOut_0.Contains("Sleep") || !processOut_1.Contains("Sleep"))
            {
                logger.writeToConsole("jlink 区分执行失败");
                return;
            }

            AddMsg("执行Jlink区分指令");

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
                case 0x53:
                    logger.writeToConsole("0x53");
                    _configData = _MainViewModel.configurationData.ConfigurationNode.StopContCMD.Replace(" ", "");
                    break;

                case 0x0D:
                    logger.writeToConsole("0x0D");
                    _configData = _MainViewModel.configurationData.ConfigurationNode.SignalCMD.Replace(" ", "");
                    break;

                case 0x89:
                    logger.writeToConsole("0x89");
                    _configData = _MainViewModel.configurationData.ConfigurationNode.InnerVersion.Replace(" ", "");
                    break;

                case 0xFF:
                    logger.writeToConsole("0xFF");
                    _configData = _MainViewModel.configurationData.ConfigurationNode.ResetDev.Replace(" ", "");
                    break;

                case 0xAE:
                    logger.writeToConsole("0xAE");
                    _configData = _MainViewModel.configurationData.ConfigurationNode.USEAE.Replace(" ", "");
                    break;

                case 0xC0:
                    logger.writeToConsole("0xC0");
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

                    _resData.Add((byte)((_baud >> 0) & 0xFF));
                    _resData.Add((byte)((_baud >> 8) & 0xFF));
                    _resData.Add((byte)((_baud >> 16) & 0xFF));
                    _resData.Add((byte)((_baud >> 24) & 0xFF));

                    _resData.Add(0x00);

                    responseData = _resData.ToArray();
                    responseData[responseData.Length - 1] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

                    uartServer.SendData(responseData);
                    return;

                case 0xEC:
                    logger.writeToConsole("0xEC");
                    var _ev = _MainViewModel.configurationData.ConfigurationNode.EmptyCurrentValue;
                    var data_0 = BitConverter.GetBytes(_ev[0]);
                    var data_1 = BitConverter.GetBytes(_ev[1]);

                    _resData.Add(0x00); _resData.Add((byte)(2 + data_0.Length + data_1.Length) );
                    _resData.Add(0xA0); _resData.Add(0xEC);

                    for (int i = 0; i < data_0.Length; i++)
                        _resData.Add(data_0[i]);

                    for (int i = 0; i < data_1.Length; i++)
                        _resData.Add(data_1[i]);

                    _resData.Add(0x00);

                    responseData = _resData.ToArray();
                    responseData[responseData.Length - 1] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

                    uartServer.SendData(responseData);


                    return;

                case 0xBC:
                    logger.writeToConsole("0xBC");
                    var _bv = _MainViewModel.configurationData.ConfigurationNode.BoardCurrentValue;
                    var Bdata_0 = BitConverter.GetBytes(_bv[0]);
                    var Bdata_1 = BitConverter.GetBytes(_bv[1]);

                    _resData.Add(0x00); _resData.Add((byte)(2 + Bdata_0.Length + Bdata_1.Length));
                    _resData.Add(0xA0); _resData.Add(0xBC);

                    for (int i = 0; i < Bdata_0.Length; i++)
                        _resData.Add(Bdata_0[i]);

                    for (int i = 0; i < Bdata_1.Length; i++)
                        _resData.Add(Bdata_1[i]);

                    _resData.Add(0x00);

                    responseData = _resData.ToArray();
                    responseData[responseData.Length - 1] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

                    uartServer.SendData(responseData);

                    return;

                case 0xE5:
                    logger.writeToConsole("0xE5");
                    var _AEEAConfig = _MainViewModel.configurationData.ConfigurationNode.EnableAEEA;

                    _resData.Add(0x00); _resData.Add(0x03);
                    _resData.Add(0xA0); _resData.Add(0xE5);
                    _resData.Add((byte)(_AEEAConfig ? 0x01:0x00));
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

            if (requestData[7] == 0x0D || requestData[7] == 0x89)
            {
                cmdLen = 3 + returnBytes.Length;
            }
            else
            {
                cmdLen = 2 + returnBytes.Length;
            }
            

            _resData.Add((byte)(cmdLen / 8));
            _resData.Add((byte)(cmdLen % 8));

            _resData.Add(0xA0); _resData.Add(requestData[7]);

            for (int i = 0; i < returnBytes.Length; i++)
                _resData.Add(returnBytes[i]);

            if (requestData[7] == 0x0D )
            {
                _resData.Add((byte)_MainViewModel.configurationData.ConfigurationNode.ReturnSignalCMD);
            }
            else if(requestData[7] == 0x89)
            {
                _resData.Add((byte)_MainViewModel.configurationData.ConfigurationNode.ReturnInVer);
            }

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
                case msgType.jlinkDiffErrFlag:
                    var status = requestData[8];

                    if (status == 0x00)
                        AddMsg("jlink[0] 连接异常");
                    else if (status == 0x01)
                        AddMsg("jlink[1] 连接异常");
                    else if (status == 0x02)
                        AddMsg("jlink[0, 1] 连接异常");

                    break;

                case msgType.startFlag:                   
                    AddMsg("可以开始进行测试");
                    break;

                case msgType.buttonDownFlag:
                    _MainViewModel.StartStatus();

                    Array.Clear(runCurrentValue, 0, runCurrentValue.Length);
                    AddMsg("启动测试流程");
                    break;

                case msgType.putDownFlag:
                    AddMsg("压板" + (requestData[8] == 0x01 ? "" : "不") + "到位");
                    break;

                case msgType.nodeConnect:
                    int nodeNumber = (ushort)requestData[8];
                    bool nodeConnectFlag = (requestData[9] == 0x01) ? true : false;

                    _MainViewModel.nodeConnectStatus(nodeNumber / 8, nodeNumber % 8 + 1, nodeConnectFlag);

                    AddMsg("Block " + (nodeNumber / 8 + 1) + "# No. " + (nodeNumber % 8 + 1) + (nodeConnectFlag ? " " : " 未") + "连接");
                    break;

                case msgType.nodeBlockVersion:
                    int panelNum = (ushort)requestData[8];

                    string panelStr = "";

                    if (panelNum == 0)
                    {
                        panelStr = "Block 1# ";
                    }
                    else if(panelNum == 1)
                    {
                        panelStr = "Block 2# ";
                    }
                    else
                    {
                        AddMsg("Block 版本指令异常");
                        return;
                    }

                    byte _version = requestData[9];

                    string showStr = "V5";
                    if (_version == 0x0A)
                    {
                        showStr = showStr + "A ";
                    }
                    else if (_version == 0x0B)
                    {
                        showStr = showStr + "B ";
                    }
                    else
                    {
                        showStr = "异常";
                    }

                    AddMsg(panelStr + showStr + "版本");

                    //AddMsg(showStr + "+" + _MainViewModel.configurationData.systemConfig.HardwareVersion);

                    if (!showStr.Contains(_MainViewModel.configurationData.systemConfig.HardwareVersion))
                    {
                        //stopSystem.RaiseEvent(new RoutedEventArgs(Tile.ClickEvent));
                        stopSystem_Click(null,null);
                        AddMsg("硬件版本不对应 !!!");
                        return;
                    }

                    break;

                case msgType.nodeShortOut:
                    int nodeShortOutNumber = (ushort)requestData[8];
                    bool nodeShortOutFlag = (requestData[9] == 0x01) ? true : false;

                    _MainViewModel.nodeShortOutStatus((nodeShortOutNumber / 8), (nodeShortOutNumber % 8 + 1), nodeShortOutFlag ? 2 : 3);

                    AddMsg("Block " + (nodeShortOutNumber / 8 + 1) + "# No. " + (nodeShortOutNumber % 8 + 1) + (nodeShortOutFlag ? " " : " 未") + "短路");

                    break;

                case msgType.emptyCurValue:
                    var _emptyRange = _MainViewModel.configurationData.ConfigurationNode.EmptyCurrentValue;

                    int nodeEmptyNumber = (ushort)requestData[8];

                    float emptyValue = BitConverter.ToSingle(requestData, 9);

                    if (emptyValue < _emptyRange[0] || emptyValue > _emptyRange[1])
                    {
                        _MainViewModel.nodeEmptyCurrentStatus((nodeEmptyNumber / 8), (nodeEmptyNumber % 8 + 1), false, (float)Math.Round(emptyValue, 2));
                    }
                    else
                    {
                        _MainViewModel.nodeEmptyCurrentStatus((nodeEmptyNumber / 8), (nodeEmptyNumber % 8 + 1), true, (float)Math.Round(emptyValue, 2));
                    }

                    logger.writeToConsole("EmptyCurrent," + (float)Math.Round(emptyValue, 2));
                    logger.saveCurrentValue("EmptyCurrent," + (float)Math.Round(emptyValue, 2));
                   
                    break;

                case msgType.runCurrValue_0:
                    var runCurrntRange_0 = _MainViewModel.configurationData.ConfigurationNode.BoardCurrentValue;

                    var nodeRunCurr_0 = (ushort)requestData[8];

                    float runCurr_0 = BitConverter.ToSingle(requestData, 9);

                    runCurrentValue[nodeRunCurr_0, 0] = runCurr_0;

                    if (runCurr_0 < runCurrntRange_0[0] || runCurr_0 > runCurrntRange_0[1])
                    {
                        _MainViewModel.boardCurrentTask((nodeRunCurr_0 / 8), (nodeRunCurr_0 % 8 + 1), (float)Math.Round(runCurr_0, 2), 1);
                    }

                    logger.writeToConsole("runCurrent_0," + (float)Math.Round(runCurr_0, 2));
                    logger.saveCurrentValue("runCurrent_0," + (float)Math.Round(runCurr_0, 2));

                    break;

                case msgType.runCurrValue_1:
                    var runCurrntRange_1 = _MainViewModel.configurationData.ConfigurationNode.BoardCurrentValue;

                    var nodeRunCurr_1 = (ushort)requestData[8];

                    float runCurr_1 = BitConverter.ToSingle(requestData, 9);

                    runCurrentValue[nodeRunCurr_1, 1] = runCurr_1;

                    if (runCurr_1 < runCurrntRange_1[0] || runCurr_1 > runCurrntRange_1[1])
                    {
                        _MainViewModel.boardCurrentTask((nodeRunCurr_1 / 8), (nodeRunCurr_1 % 8 + 1), (float)Math.Round(runCurr_1, 2), 1);
                    }

                    logger.writeToConsole("runCurrent_1," + (float)Math.Round(runCurr_1, 2));
                    logger.saveCurrentValue("runCurrent_1," + (float)Math.Round(runCurr_1, 2));

                    break;

                case msgType.runCurrValue_2:

                    var runCurrntRange_2 = _MainViewModel.configurationData.ConfigurationNode.BoardCurrentValue;

                    var nodeRunCurr_2 = (ushort)requestData[8];

                    float runCurr_2 = BitConverter.ToSingle(requestData, 9);

                    runCurrentValue[nodeRunCurr_2, 2] = runCurr_2;

                    if (runCurr_2 < runCurrntRange_2[0] || runCurr_2 > runCurrntRange_2[1])
                    {
                        _MainViewModel.boardCurrentTask((nodeRunCurr_2 / 8), (nodeRunCurr_2 % 8 + 1), (float)Math.Round(runCurr_2, 2), 1);
                    }
                    else
                    {
                        var avgValue = (runCurrentValue[nodeRunCurr_2, 0] + runCurrentValue[nodeRunCurr_2, 1] + runCurrentValue[nodeRunCurr_2, 2]) / 3;
                        _MainViewModel.boardCurrentTask((nodeRunCurr_2 / 8), (nodeRunCurr_2 % 8 + 1), (float)Math.Round(avgValue, 2), 2);
                    }

                    logger.writeToConsole("runCurrent_2," + (float)Math.Round(runCurr_2, 2));
                    logger.saveCurrentValue("runCurrent_2," + (float)Math.Round(runCurr_2, 2));

                    break;

                case msgType.nodeVersion:
                    int nodeSoftVersion = (ushort)requestData[8];

                    string _SoftVersion = Encoding.ASCII.GetString(requestData, 9, requestData.Length - 10);

                    logger.writeToConsole("_SoftVersion " + _SoftVersion);

                    _MainViewModel.nodeVersionStatus(nodeSoftVersion / 8 , (nodeSoftVersion % 8 + 1), false, _SoftVersion/*.Substring(_SoftVersion.Length - 4, 4)*/);

                    break;

                case msgType.nodeVersionErr:
                    int nodeSoftVersionErr = (ushort)requestData[8];

                    _MainViewModel.nodeVersionStatus(nodeSoftVersionErr / 8, (nodeSoftVersionErr % 8 + 1), true, "超时");

                    break;

                default:
                    break;
            }
        }

        //// 空板电流 处理函数
        //public void EmptyCurrentTask(byte[] requestData)
        //{
        //    int nodeNumber = (ushort)requestData[7];

        //    float[] adcData = new float[100];

        //    Parallel.For(0, 100, i=> {
        //        adcData[i] = (float)BitConverter.ToUInt16(requestData, i * 2 + 8) ;
        //    });

        //    double s = 0;
        //    for (int i = 0; i < 100; i++)
        //    {
        //        s += adcData[i] * adcData[i];
        //    }

        //    double rmsdata = Math.Sqrt(s / 100);

        //    byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x43, (requestData[7]), 0x00, 0x00 };

        //    var dataRange = _MainViewModel.configurationData.ConfigurationNode.EmptyCurrentValue;
        //    bool statusFlag = false;

        //    if (rmsdata < dataRange[0] || rmsdata > dataRange[1])
        //    {
        //        responseData[8] = 0x00;
        //    }
        //    else
        //    {
        //        responseData[8] = 0x01;
        //        statusFlag = true;
        //    }

        //    _MainViewModel.nodeEmptyCurrentStatus((nodeNumber / 8), (nodeNumber % 8 + 1), statusFlag, (float)Math.Round(rmsdata, 2));

        //    responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

        //    uartServer.SendData(responseData);
        //}

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
            proc.WaitForExit(2);

            string processOut = proc.StandardOutput.ReadToEnd();
            proc.Close();
            proc.Dispose();

            jlinkUsed[blockNumber == 0 ? 0 : 1] = false;

            logger.writeToConsole(processOut);

            if (processOut.Contains("Downloading file") &&
                //processOut.Contains("Reset delay:") &&
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

        //// 运行 电流处理函数
        //public void runCurrentTask(byte[] requestData, int stepCnt)
        //{
        //    int nodeNumber = requestData[7];

        //    int blockNumber = nodeNumber / 8 + 1;

        //    float[] adcData = new float[100];

        //    Parallel.For(0, 100, i=> {
        //        adcData[i] = (float)BitConverter.ToUInt16(requestData, i * 2 + 8);
        //    });

        //    double s = 0;
        //    for (int i = 0; i < 100; i++)
        //    {
        //        s += adcData[i] * adcData[i];
        //    }

        //    float rmsdata = (float)Math.Round(Math.Sqrt(s / 100), 2);

        //    byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, (requestData[6]), (requestData[7]), 0x00, 0x00 };

        //    var runCurrntRange = _MainViewModel.configurationData.ConfigurationNode.BoardCurrentValue;

        //    runCurrentValue[nodeNumber, stepCnt - 1] = rmsdata;

        //    if (rmsdata < runCurrntRange[0] || rmsdata > runCurrntRange[1])
        //    {
        //        responseData[8] = 0x00;

        //        // show view
        //        _MainViewModel.boardCurrentTask(blockNumber, nodeNumber + 1, rmsdata, 1);
        //    }
        //    else
        //    {
        //        if (stepCnt == 3)
        //        {
        //            // show view
        //            float _sum = 0;
        //            for (int i = 0; i < 3; i++)
        //            {
        //                _sum += runCurrentValue[nodeNumber, i];
        //            }

        //        }

        //        responseData[8] = 0x01;
        //    }

        //    responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];

        //    uartServer.SendData(responseData);


        //}

        // 上报 ADC数据
        public void putRealADCDataTask(byte[] requestData)
        {
            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x5E, (requestData[7]), 0x00, 0x00 };

            logger.writeToConsole("RealADC Receive Length: " + requestData.Length);

            int _blockNo = requestData[7] / 8;
            int _nodeNo = requestData[7] % 8;

            // 地址修改
            if (requestData.Length != (8 + PostADCDataCnt * 2 + _MainViewModel.configurationData.ConfigurationNode.ReturnSignalCMD + 1))
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                logger.writeToConsole("ADC data cmd error");
                return;
            }

            // save raw data to file
            if (_MainViewModel.configurationData.systemConfig.SaveRawDataBool)
            {
                string _dirPath = System.IO.Path.Combine(RawDataDir, DateTime.Now.ToLongDateString().ToString());

                if (Directory.Exists(_dirPath) == false)
                    Directory.CreateDirectory(_dirPath);

                string _filePath = System.IO.Path.Combine(_dirPath, DateTime.Now.ToString("HH_mm_ss_ff_") + _MainViewModel.configurationData.systemConfig.SoftwareVersion + "_B" + _blockNo + "_N" + _nodeNo + ".rawBin");

                using (var _rawDataFS = new FileStream(_filePath, FileMode.Append))
                {
                    _rawDataFS.Write(requestData, 0, requestData.Length);
                    _rawDataFS.Flush();
                }
            }

            // get model result
            byte _modelResult = requestData[8 + _MainViewModel.configurationData.ConfigurationNode.ModelResultIndex];
            logger.writeToConsole("model Result: " + (_modelResult != 0x00));
            logger.writeToConsole("model Result: " + (_modelResult != 0x01));
            logger.writeToConsole("model Result: " + (_modelResult != 0x02));

            // 判断结果是否合格
            if (_modelResult != (byte)0x00 && _modelResult != (byte)0x01 && _modelResult != (byte)0x02)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                logger.writeToConsole("model data - result - format error");
                return;
            }

            // 判断是否是硬材质
            if (_modelResult != (byte)0x01)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                logger.writeToConsole("model data - result - error" + _modelResult);
                return;
            }

            // format adc raw data
            UInt16[] adcData = new UInt16[PostADCDataCnt] ;

            Array.Clear(adcData, 0, adcData.Length);

            Parallel.For(0, PostADCDataCnt, i =>
            {
                adcData[i] = BitConverter.ToUInt16(requestData, i * 2 + 8 + _MainViewModel.configurationData.ConfigurationNode.ReturnSignalCMD);
            });

            // 添加判断算法
            UInt32 sumAdcData = 0;
            UInt32 avgAdcData = 0;

            for (int i = 0; i < 200; i++)
            {
                sumAdcData += adcData[PostADCDataCnt - i - 1];
            }
            avgAdcData = sumAdcData / 200;

            // 求平均值 
            if (avgAdcData > 300)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                logger.writeToConsole("ADC data avg - error - " + avgAdcData);
                return;
            }

            int _onePeak = requestData[8 + _MainViewModel.configurationData.ConfigurationNode.ModelResultIndex + 1] * 256 
                         + requestData[8 + _MainViewModel.configurationData.ConfigurationNode.ModelResultIndex + 2];

            if (_onePeak < 1500)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                logger.writeToConsole("one peak data - error - " + _onePeak);
                return;
            }

            // ....
            int max_index = 0;
            UInt16 max_data = 0;
            for (int i = 0; i < 140; i++)
            {
                if (adcData[i] > max_data)
                {
                    max_index = i;
                    max_data = adcData[i];
                }
            }

            logger.writeToConsole("ADC max index: " + max_index + " max data " + max_data);

            if (max_data < 10000)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                return;
            }

            int maxBV = 0;
            UInt16 fall_2500_point = 1;
            UInt16 less_100_num = 0;
            for (UInt16 i = 0; i < 140; i++)
            {
                if (adcData[i] > maxBV)
                {
                    maxBV = adcData[i];
                }

                if (adcData[i] < 8000 && fall_2500_point < 2)
                {
                    fall_2500_point = i;
                }
                if (adcData[i] > 8000 && fall_2500_point > 2)
                {
                    fall_2500_point = 1;
                }
                //if (adcBuff[i] < 500 && fall_500_point > 14)
                //{
                //    fall_500_point = i;
                //}
                //if (adcData[i] < 100)
                //{
                //    less_100_num = (ushort)(less_100_num + 1);
                //}
            }

            if (maxBV < 10000 || fall_2500_point < 100)
            {
                responseData[8] = 0x00;
                responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
                uartServer.SendData(responseData);

                _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, false);
                logger.writeToConsole("maxBV: " + maxBV + " fall_2500_point " + fall_2500_point);
                return;
            }



            _MainViewModel.funTestTask(_blockNo, _nodeNo + 1, true);

            responseData[8] = 0x01;
            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
            uartServer.SendData(responseData);

            logger.writeToConsole("ADC data cmd pass --");
        }

        // ADC 数据异常报告
        public void putADCStatusTask(byte[] requestData)
        {
            byte[] responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x03, 0x5C, (requestData[7]), 0x00, 0x00 };

            int nodeNumber = requestData[7];

            responseData[8] = 0x01;
            responseData[9] = crc8.ComputeHash(responseData, 0, responseData.Length - 1)[0];
            uartServer.SendData(responseData);

            _MainViewModel.funTestTask(nodeNumber / 8 , (nodeNumber % 8 + 1), false);

            AddMsg("Block "+ (nodeNumber / 8 + 1) + " # No. " + (nodeNumber % 8 + 1) + " ADC OR/UR");
        }

        
    }
}
