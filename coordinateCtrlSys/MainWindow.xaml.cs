using System;
using System.Collections.Generic;
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

        // bool value

        private bool checkJlinkValue = false;

        private bool MCUConnectValue = false;

        private const int crcByteCnt = 1;

        private enum cmdType { requestConnected = 0xfc , requestSettings = 0xae, requestJlink = 0xf0};
        
        
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
            AddMsg("Start Application");

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
            AddMsg("Stop Application");
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

            AddMsg("Check peripherals ...");
        }

        // 选择 配置文件
        private void selectJsonFile_Click(object sender, RoutedEventArgs e)
        {
            _MainViewModel.getSettingFile("./Settings/config.json");

            if (uartServer.OpenPort("COM3"))
            {
                AddMsg("Port open sucessed ");
            }
            else
            {
                AddMsg("Port open failed");
            }

        }

        // 配置系统 开始运行
        private void configSystem_Click(object sender, RoutedEventArgs e)
        {
            uartServer.SendData(new byte[] { 0x01 });
        }

        // 停止系统响应
        private void stopSystem_Click(object sender, RoutedEventArgs e)
        {
            if (uartServer.IsOpen())
            {
                uartServer.ClosePort();
            }
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
