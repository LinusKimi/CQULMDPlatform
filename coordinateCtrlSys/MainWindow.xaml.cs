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

        private ActionBlock<byte[]> _uartActionBlock;

        private UartServer uartServer;

        private Crc8Base crc8 = new Crc8Base(0x07, 0x00, 0x00, false, false);

        private const string DefaultPortName = "COM3";

        // bool value

        private bool checkJlinkValue = false;

        private bool MCUConnectValue = false;

        private const int crcByteCnt = 1;

        private enum cmdType {
            // 响应下位机连接
            requestConnected = 0xfc ,
            
            // 下位机获取系统配置
            requestSettings = 0xae,
            
            // Jlink 区分
            requestJlink = 0xf0};
        
        
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

            _UITimer = new DispatcherTimer();

            _UITimer.Interval = new TimeSpan(0, 1, 0);
            _UITimer.IsEnabled = true;
            _UITimer.Tick += UITimer_timeout;
            _UITimer.Start();

            checkJlink();
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
                if (MsgBox.Items.Count >= 29)
                    MsgBox.Items.RemoveAt(0);

                MsgBox.Items.Add(DateTime.Now.ToString("HH:mm:ss") + ": " + msg);
            });

            logger.writeToFile(msg);
        }

        private void checkJlink()
        {
            // 检查 jlink
            AddMsg("检查外设中 ...");

            var lsjlinkbatpath = AppDomain.CurrentDomain.BaseDirectory + "InnerShell\\lsJlinkPort.bat";

            string text = "echo helloworld";
            
            File.WriteAllText(lsjlinkbatpath, text);
            

            var proc = new Process();
            proc.StartInfo.FileName = lsjlinkbatpath;
            proc.StartInfo.CreateNoWindow = false;
            
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
                AddMsg("外设检查指令执行失败");
            }
            else
            {
                AddMsg("外设检查指令执行成功");
            }

        }

        // 选择 配置文件
        private void selectJsonFile_Click(object sender, RoutedEventArgs e)
        {
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

            AddMsg("成功加载配置文件");
        }

        // 配置系统 开始运行
        private void configSystem_Click(object sender, RoutedEventArgs e)
        {
            if (uartServer.OpenPort(DefaultPortName))
            {
                _MainViewModel.portOpend = true;
                AddMsg("成功打开通讯接口");
            }
            else
            {
                _MainViewModel.portOpend = false;
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
        }

        private void ProcessTask(byte[] data)
        {
            cmdcnt++;

            Console.WriteLine("recv data length: " + data.Length + " cmdcnt: " + cmdcnt);
            Console.WriteLine(" ThreadId:" + Thread.CurrentThread.ManagedThreadId + " Execute Time:" + DateTime.Now);

            // 数据校验

            var _dataTemp = new byte[data.Length - crcByteCnt];
            Parallel.For(0, _dataTemp.Length, i => {
                _dataTemp[i] = data[i];
            });

            crc8.AutoReset = true;
            var crcTemp = crc8.ComputeHash(_dataTemp);

            if (crcTemp[0] == data[data.Length - 1])
            {
                Console.WriteLine("cmd data check successed !");
            }
            else
            {
                Console.WriteLine("cmd data check failed !");
                return;
            }

            // 数据解析

            switch ((cmdType)data[6])
            {
                case cmdType.requestConnected:
                    RequestConnectedTask();
                    break;

                case cmdType.requestJlink:
                    
                    break;

                default:

                    break;
            }

        }

        
        public void RequestConnectedTask()
        {
            byte[] _t = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x01, 0xcf};
            byte[] _responseData = new byte[] { 0xeb, 0x90, 0x09, 0xbe, 0x00, 0x01, 0xcf, 0x00 };

            var _tcrc = crc8.ComputeHash(_t);
            _responseData[7] = _tcrc[0];
            uartServer.SendData(_responseData);

            MCUConnectValue = true;
        }

        public void DifferJlink()
        { 
            
        }
        public void EmptyCurrentTask(int blocknum, int boardnum, float value)
        {

        }

        public void ProgrameFlashTask()
        {

        }




    }
}
