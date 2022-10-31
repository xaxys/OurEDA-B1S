using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UI
{
    public partial class MainForm : Form
    {
        // 预定义常量
        static Color DisableColor = Color.FromArgb(67, 67, 67);
        static Color EnableColor = Color.FromArgb(0, 64, 0);
        static Color BusyColor = Color.FromArgb(255, 77, 59);
        static Color NormalColor = Color.FromArgb(0, 192, 0);
        static Color ErrorColor = Color.FromArgb(255, 0, 0);
        static string MainCamURL = "http://192.168.0.123:5000/mono_cam_viewer_no_det";
        static string MainDetectURL = "http://192.168.0.123:5000/mono_cam_viewer";
        static string DualCamURL = "http://192.168.0.123:5000/stero_cam_viewer_no_det";
        static string MainEnhanceURL = "http://192.168.0.123:5000/enhance";
        static string DualDetectURL = "http://192.168.0.123:5000/stero_cam_viewer";
        static string ViceCamURL = "http://192.168.0.8";

        static TimeSpan SerialSendInterval = TimeSpan.FromMilliseconds(50);
        static TimeSpan ControlPermDetectInterval = TimeSpan.FromMilliseconds(400);
        static int HalfAutoSendInterval = 90;

        enum BrowserVision
        {
            None, MainCam, MainDetect, DualCam, MainEnhance, DualDetect, ViceCam
        }

        enum ControlMode
        {
            None, Manual, Auto, HalfAuto 
        }

        public delegate void UpdateUIMethod();
        UpdateUIMethod OnUpdateUI;
        Thread updateUIThread;

        SerialManager serial; // 串口连接
        NetworkManager roboClient; // 下位机连接
        NetworkManager autoClient; // 自主抓取上位机连接
        NetworkManager halfAutoServer; // 半自主抓取上位机连接 对方要求是Server 呃呃
        BackgroundWorker halfAutoSendWorker;

        Color mainCabColor = NormalColor;  // 控制舱是否漏水
        Color powerCabColor = NormalColor; // 电源舱是否漏水

        DateTime lastSerialTime;
        BrowserVision vision;
        ControlMode ctrlMode;

        // 历史遗留的神必变量，暂时没重构掉
        int ks1 = -1, ks2 = -1;

        // 神奇的解析完的传感器数据的储存的地方
        string[] comm = new string[20];//控制仓
        string[] comm2 = new string[20];//电源仓
        // 神奇的解析完的串口数据（并没有解析）的储存的地方
        string[] temcomm = new string[17];

        // 用于自动半自动切换间保留的控制指令数据
        byte[] serialCommand = new byte[30];
        byte[] halfAutoCommand = new byte[30];

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainFormLoad(object sender, EventArgs e)
        {

            torectangle();

            temcomm[6] = "1500";
            comm[0] = "0";
            comm[1] = "0";
            comm[2] = "0";
            comm[3] = "0";
            comm[4] = "0";
            comm[5] = "0";
            comm[6] = "0";
            comm[7] = "0";
            comm[8] = "0";
            comm[9] = "0";
            comm[10] = "0";
            comm[11] = "0";
            comm[12] = "0";
            comm[13] = "0";
            comm[14] = "0";
            comm[15] = "0";
            comm[16] = "0";
            comm[17] = "0";
            comm[18] = "0";

            comm2[0] = "0";
            comm2[1] = "0";
            comm2[2] = "0";

            temcomm[1] = "1500";
            temcomm[2] = "1500";
            temcomm[3] = "1500";
            temcomm[5] = "1500";
            temcomm[6] = "1500";
            mainCabTempLabel.BringToFront();
            updateUIThread = new Thread(draw);
            OnUpdateUI += updateInfo;
            updateUIThread.Start();

            // 上面代码不是我写的啊，太难维护了
            // 下面是我写的
            serial = new SerialManager("COM3", 9600);
            serial.OnReceived += onSerialMessage;
            serial.OnLog += addInfo;

            roboClient = new NetworkManager(new IPEndPoint(IPAddress.Parse("192.168.0.1"), 1234));
            roboClient.BufferSize = 128;
            roboClient.ReceiveSize = 47;
            roboClient.CleanThreshold = 0.6;
            roboClient.Detector += SensorDecoder.Detect;
            roboClient.OnReceived += onSensorData;
            roboClient.OnLog += addInfo;

            autoClient = new NetworkManager(new IPEndPoint(IPAddress.Parse("192.168.0.123"), 6000));
            autoClient.BufferSize = 128;
            autoClient.ReceiveSize = 30;
            autoClient.CleanThreshold = 0.7;
            autoClient.Detector += OperationDecoder.Detect;
            autoClient.OnReceived += onOperationData;
            autoClient.OnLog += addInfo;

            halfAutoServer = new NetworkManager(new IPEndPoint(IPAddress.Any, 4567));
            halfAutoServer.ListenBackLog = 0;
            autoClient.BufferSize = 128;
            autoClient.ReceiveSize = 30;
            autoClient.CleanThreshold = 0.7;
            halfAutoServer.Detector += OperationDecoder.Detect;
            halfAutoServer.OnReceived += onHalfAutoOperationData;
            halfAutoServer.OnLog += addInfo;
            halfAutoServer.Listen();

            halfAutoSendWorker = new BackgroundWorker();
            halfAutoSendWorker.WorkerSupportsCancellation = true;
            halfAutoSendWorker.DoWork += (object a, DoWorkEventArgs b) =>
            {
                BackgroundWorker worker = a as BackgroundWorker;
                while (!worker.CancellationPending && ctrlMode == ControlMode.HalfAuto)
                {
                    Thread.Sleep(HalfAutoSendInterval);
                    bool hasSerialCommand = false;
                    for (var i = 0; i < serialCommand.Length; i++)
                    {
                        hasSerialCommand |= serialCommand[i] != 0;
                    }
                    if (!hasSerialCommand)
                    {
                        Console.WriteLine();
                        Console.WriteLine("半自动模式：串口历史数据为空，拒绝下发数据");
                        Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffff"));
                        continue;
                    }

                    bool hasHalfAutoCommand = false;
                    for (var i = 0; i < halfAutoCommand.Length; i++)
                    {
                        hasHalfAutoCommand |= halfAutoCommand[i] != 0;
                    }
                    if (!hasHalfAutoCommand)
                    {
                        Console.WriteLine();
                        Console.WriteLine("半自动模式：半自动脚本历史数据为空，拒绝下发数据");
                        Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffff"));
                        continue;
                    }

                    var combinedData = new byte[30];
                    if (ctrlMode == ControlMode.HalfAuto)
                    {
                        for (var i = 0; i < 9; i++)
                        {
                            combinedData[i] = serialCommand[i];
                        }
                        combinedData[9] = halfAutoCommand[9];
                        combinedData[10] = halfAutoCommand[10];
                        combinedData[11] = serialCommand[11];
                        combinedData[12] = serialCommand[12];
                        for (var i = 13; i < 27; i++)
                        {
                            combinedData[i] = halfAutoCommand[i];
                        }
                        for (var i = 27; i < 30; i++)
                        {
                            combinedData[i] = serialCommand[i];
                        }
                    }
                    roboClient.Send(combinedData);
                    Console.WriteLine();
                    Console.WriteLine("半自动模式下发数据");
                    Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffff"));
                    Console.WriteLine(BitConverter.ToString(combinedData, 0).Replace("-", string.Empty));
                }
            };
            halfAutoSendWorker.RunWorkerCompleted += (object a, RunWorkerCompletedEventArgs ev) =>
            {
                if (ev.Cancelled)
                {
                    addInfo("半自主模式定时发送正常退出");
                }
                else
                {
                    addInfo("半自主模式定时发送终止");
                }
            };
        }

        private void addInfo(string info)
        {
            Invoke(new Action(() =>
            {
                infoListBox.Items.Add(info);
                infoListBox.TopIndex = infoListBox.Items.Count - (infoListBox.Height / infoListBox.ItemHeight);
            }));
        }

        void torectangle()
        {
            GraphicsPath gp1 = new GraphicsPath();
            gp1.AddEllipse(shoppingCartPic.ClientRectangle);
            Region region1 = new Region(gp1);
            shoppingCartPic.Region = region1;

            gp1.Dispose();
            region1.Dispose();
        }

        // 一言难尽，实在是改不动了
        private void updateInfo() //信息处理函数定义
        {
            mainCabTempLabel.Text = "温度:" + Math.Round(Convert.ToDouble(comm[0]), 1) + "℃";//仓内温度
            powerCabTempLabel.Text = "温度:" + Math.Round(Convert.ToDouble(comm2[0]), 1) + "℃";

            mainCabPressLabel.Text = "气压:" + Math.Round(Convert.ToDouble(comm[1]), 1) + "KPa";
            powerCabPressLabel.Text = "气压:" + Math.Round(Convert.ToDouble(comm2[1]), 1) + "KPa";

            mainCabHumidLabel.Text = "湿度:" + comm[2] + "%"; //湿度
            powerCabHumidLabel.Text = "湿度:" + comm2[2]+ "%";

            waterTempLabel.Text = "水温:" + Math.Round(Convert.ToDouble(comm[11]), 1) + "℃";//水温
            depthLabel.Text = "深度:" + Math.Round(Convert.ToDouble(comm[12]), 2) + "M"; //水深
            heightLabel.Text = "高度:" + Math.Round(Convert.ToDouble(comm[9]), 2) + "M";

            rollLabel.Text = string.Format("横滚角{0}°", comm[3]);
            pitchLabel.Text = string.Format("俯仰角{0}°", comm[4]);
            yawLabel.Text = string.Format("航向角{0}°", comm[5]);

            horiCt.Hori_Disp(Convert.ToDouble(comm[4]), -Convert.ToDouble(comm[3]));
            compointCt.Compass_Disp(Convert.ToDouble(comm[5]));

            mainCabErrorLabel.BackColor = mainCabColor;
            powerCabErrorLabel.BackColor = powerCabColor;
            controlPermLabel.BackColor = DateTime.Now.Subtract(lastSerialTime) > ControlPermDetectInterval ? DisableColor : EnableColor;
            //label32.BackColor = sd1;

            var qjht = (Convert.ToInt32(temcomm[1]) - 1500) / 10;
            var zy1 = (Convert.ToInt32(temcomm[2]) - 1500) / 10;
            var sx1 = (Convert.ToInt32(temcomm[3]) - 1500) / 10;

            xLabel.Text = qjht.ToString();
            yLabel.Text = zy1.ToString();
            zLabel.Text = sx1.ToString();

            xProcessLine.Value = Math.Abs(qjht);
            yProcessLine.Value = Math.Abs(zy1);
            zProcessLine.Value = Math.Abs(sx1);

            //侧推1 定向2 定深4 机械臂8
            //25 23        22  27
            var scd1 = (Convert.ToInt32(temcomm[6]) - 1500) / 10;
            if (scd1 > 0) { ucConveyor1.ConveyorDirection = HZH_Controls.Controls.ConveyorDirection.Forward; rightArrow1.BackColor = EnableColor; }
            if (scd1 < 0) { ucConveyor1.ConveyorDirection = HZH_Controls.Controls.ConveyorDirection.Backward; leftArrow.BackColor = EnableColor; }
            if (scd1 == 0) { ucConveyor1.ConveyorDirection = HZH_Controls.Controls.ConveyorDirection.None; rightArrow1.BackColor = BusyColor; leftArrow.BackColor = BusyColor; }
            //侧推模式
            if (temcomm[14] == "1") {
                modeSidePushLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = DisableColor;
                keepYawLabel.BackColor = DisableColor;
                armLabel.BackColor = DisableColor;
            }
            else if (temcomm[14] == "9") {
                modeSidePushLabel.BackColor = EnableColor;
                armLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = DisableColor;
                keepYawLabel.BackColor = DisableColor;
            }
            else if (temcomm[14] == "2") {
                keepYawLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = DisableColor;
                modeSidePushLabel.BackColor = DisableColor;
                armLabel.BackColor = DisableColor;
            } //定向
            else if (temcomm[14] == "4") {
                keepDepthLabel.BackColor = EnableColor;
                keepYawLabel.BackColor = DisableColor;
                modeSidePushLabel.BackColor = DisableColor;
                armLabel.BackColor = DisableColor;
            } //定深
            else if (temcomm[14] == "8") { armLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = DisableColor; keepYawLabel.BackColor = DisableColor;
                modeSidePushLabel.BackColor = DisableColor;
            } //机械臂
            else if (temcomm[14] == "3") {
                keepYawLabel.BackColor = EnableColor;
                modeSidePushLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = DisableColor;
                armLabel.BackColor = DisableColor;
            }
            else if (temcomm[14] == "5") {
                keepDepthLabel.BackColor = EnableColor; modeSidePushLabel.BackColor = EnableColor;
                keepYawLabel.BackColor = DisableColor; armLabel.BackColor = DisableColor;

            }
            else if (temcomm[14] == "6") {
                keepDepthLabel.BackColor = EnableColor; keepYawLabel.BackColor = EnableColor;
                modeSidePushLabel.BackColor = DisableColor; armLabel.BackColor = DisableColor;
            }
            else if (temcomm[14] == "10") {
                keepYawLabel.BackColor = EnableColor; armLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = DisableColor; modeSidePushLabel.BackColor = DisableColor;
            }
            else if (temcomm[14] == "12") {

                keepDepthLabel.BackColor = EnableColor; armLabel.BackColor = EnableColor;
                keepYawLabel.BackColor = DisableColor; modeSidePushLabel.BackColor = DisableColor;

            }
            else if (temcomm[14] == "7") { keepDepthLabel.BackColor = EnableColor; keepYawLabel.BackColor = EnableColor; modeSidePushLabel.BackColor = EnableColor; armLabel.BackColor = DisableColor; }
            else if (temcomm[14] == "11") { armLabel.BackColor = EnableColor; keepYawLabel.BackColor = EnableColor; modeSidePushLabel.BackColor = EnableColor; keepDepthLabel.BackColor = DisableColor; }
            else if (temcomm[14] == "14") { armLabel.BackColor = EnableColor; keepYawLabel.BackColor = EnableColor; keepDepthLabel.BackColor = EnableColor; modeSidePushLabel.BackColor = DisableColor; }
            else if (temcomm[14] == "15") {
                armLabel.BackColor = EnableColor; keepYawLabel.BackColor = EnableColor;
                keepDepthLabel.BackColor = EnableColor; modeSidePushLabel.BackColor = EnableColor;
            }
            else { keepDepthLabel.BackColor = DisableColor; keepYawLabel.BackColor = DisableColor; modeSidePushLabel.BackColor = DisableColor; armLabel.BackColor = DisableColor; }


            if (Convert.ToInt32(temcomm[3]) == 1300)
                modeDownLabel.BackColor = EnableColor;
            else
                modeDownLabel.BackColor = DisableColor;

            angleLabel.Text = "云台:" + (int)((Convert.ToInt32(temcomm[5]) - 500) / 2000.0 * 180.0);

            if (Convert.ToInt32(temcomm[6]) > 1520)
            {
                //label29.BackColor = EnableColor;
                driverLabel.BackColor = EnableColor;
                ucConveyor1.ConveyorDirection = HZH_Controls.Controls.ConveyorDirection.Forward;
                rightArrow1.ArrowColor = EnableColor;
                leftArrow.ArrowColor = BusyColor;
            }
            if (Convert.ToInt32(temcomm[6]) < 1480)
            {
                // label29.BackColor = EnableColor;
                driverLabel.BackColor = EnableColor;
                ucConveyor1.ConveyorDirection = HZH_Controls.Controls.ConveyorDirection.Backward;
                leftArrow.ArrowColor = EnableColor;
                rightArrow1.ArrowColor = BusyColor;
            }
            if ((Convert.ToInt32(temcomm[6]) > 1480) && (Convert.ToInt32(temcomm[6]) < 1520)) {
                // label29.BackColor = DisableColor;
                driverLabel.BackColor = DisableColor;
                ucConveyor1.ConveyorDirection = HZH_Controls.Controls.ConveyorDirection.None;
                leftArrow.ArrowColor = BusyColor;
                rightArrow1.ArrowColor = BusyColor;
            }

            if (Convert.ToInt32(temcomm[7]) > 100)//照明
                brightnessLabel.BackColor = EnableColor;
            else
                brightnessLabel.BackColor = DisableColor;

            manualLabel.BackColor = ctrlMode == ControlMode.Manual ? EnableColor : DisableColor;
            autoLabel.BackColor = ctrlMode == ControlMode.Auto ? EnableColor : DisableColor;
            halfAutoLabel.BackColor = ctrlMode == ControlMode.HalfAuto ? EnableColor : DisableColor;
        }

        private void draw()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                    Invoke(OnUpdateUI);
                    Application.DoEvents();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex);
                    break;
                }
            }
        }

        private void networkStatClick(object sender, EventArgs e)
        {
            if (!roboClient.IsOpen)
            {
                roboClient.Connect();
                if (roboClient.IsOpen)
                {
                    networkStatLabel.BackColor = EnableColor;
                }
                else
                {
                    MessageBox.Show("连接网络失败");
                }
            }
            else
            {
                roboClient.Disconnect();
                if (!roboClient.IsOpen)
                {
                    networkStatLabel.BackColor = DisableColor;
                }
                else
                {
                    MessageBox.Show("断开网络失败");
                }
            }
        }

        void onSensorData(byte[] data)
        {
            var (sensor, start, end) = SensorDecoder.Decode(data);
            if (sensor != null)
            {
                if (sensor.Cab == SensorDecoder.SensorData.CabType.ControlCab && sensor.Leak)
                {
                    ks1 = 1;
                    mainCabColor = ErrorColor;
                }
                if (sensor.Cab == SensorDecoder.SensorData.CabType.PowerCab && sensor.Leak)
                {
                    ks2 = 1;
                    powerCabColor = ErrorColor;
                }

                // 开始魔法

                if (sensor.Cab == SensorDecoder.SensorData.CabType.ControlCab)
                {
                    if (sensor.Temperature <= 60.0 && sensor.Temperature > 1.1)
                    {
                        comm[0] = sensor.Temperature.ToString();
                    }

                    if (sensor.Pressure < 1000 && sensor.Pressure > 10)
                    {
                        comm[1] = sensor.Pressure.ToString();
                    }
                    if (sensor.Humidity > 1.1)
                    {
                        comm[2] = sensor.Humidity.ToString();
                    }

                    if (sensor.Roll < 60.0)
                    {
                        comm[3] = "-" + ((int)sensor.Roll).ToString();
                    }
                    if (sensor.Roll > 300.0)
                    {
                        comm[3] = ((int)(360 - sensor.Roll)).ToString();
                    }

                    if (sensor.Pitch < 50)
                    {
                        comm[4] = ((int)sensor.Pitch).ToString();
                    }

                    if (sensor.Pitch > 310)
                    {
                        comm[4] = ((int)sensor.Pitch - 360).ToString();
                    }

                    double yaw = 0;
                    if (sensor.Yaw > 0)
                    {
                        yaw = 360 - sensor.Yaw;
                    }
                    if (sensor.Yaw <= 0)
                    {
                        yaw = -sensor.Yaw;
                    }
                    if (sensor.Yaw == 360)
                    {
                        yaw = 0;
                    }

                    yaw = (yaw + 360 - 90) % 360;
                    comm[5] = ((int)yaw).ToString();

                    if (sensor.WaterTemp > 1.01 && sensor.WaterTemp < 99.9)
                    {
                        comm[11] = sensor.WaterTemp.ToString();
                    }

                    comm[12] = sensor.WaterDepth.ToString();
                    if (sensor.Height <= 30 && sensor.Height > 0)
                    {
                        comm[9] = sensor.Height.ToString(); //声呐depth
                    }
                    comm[10] = sensor.SonarAccurancy.ToString(); //声呐确信度

                    comm[6] = sensor.Hx.ToString();
                    comm[7] = sensor.Hy.ToString();
                    comm[8] = sensor.Hz.ToString();

                    comm[13] = sensor.Ax.ToString();
                    comm[14] = sensor.Ay.ToString();
                    comm[15] = sensor.Az.ToString();
                    comm[16] = sensor.Wx.ToString();
                    comm[17] = sensor.Wy.ToString();
                    comm[18] = sensor.Wz.ToString();
                }

                if (sensor.Cab == SensorDecoder.SensorData.CabType.PowerCab)
                {
                    if (sensor.Temperature <= 60.0 && sensor.Temperature > 1.1)
                    {
                        comm2[0] = sensor.Temperature.ToString();
                    }

                    if (sensor.Pressure < 1000 && sensor.Pressure > 10)
                    {
                        comm2[1] = sensor.Pressure.ToString();
                    }
                    if (sensor.Humidity > 1.1)
                    {
                        comm2[2] = sensor.Humidity.ToString();
                    }
                }
            }
        }

        private void serialStatClick(object sender, EventArgs e)
        {
            if (!serial.IsOpen)
            {
                serial.Connect();
                if (serial.IsOpen)
                {
                    serialStatLabel.BackColor = EnableColor;
                    if (ctrlMode == ControlMode.None)
                    {
                        ctrlMode = ControlMode.Manual;
                    }
                }
                else
                {
                    MessageBox.Show("连接串口失败");
                }
            }
            else
            {
                serial.Disconnect();
                if (!serial.IsOpen)
                {
                    serialStatLabel.BackColor = DisableColor;
                    if (ctrlMode == ControlMode.Manual)
                    {
                        ctrlMode = ControlMode.None;
                    }
                }
                else
                {
                    MessageBox.Show("断开串口失败");
                }
            }
        }

        private void onSerialMessage(string str)
        {
            try
            {
                if (str == null) return;

                var time = DateTime.Now;
                if (time.Subtract(lastSerialTime) < SerialSendInterval) return;

                var comm = str.Split(':');
                if (comm.Length != 17) return;

                temcomm = comm;
                lastSerialTime = time;

                var sb = new StringBuilder();
                sb.Append("25");
                for (int i = 1; i < comm.Length - 1; i++)
                {
                    if (i < 14)
                    {
                        int a = Convert.ToInt32(comm[i]);
                        sb.Append(a.ToString("X4"));
                    }
                    else
                    {
                        int a = Convert.ToInt32(comm[i]);
                        sb.Append(a.ToString("X2"));
                    }
                }
                sb.Append("21");

                var s = sb.ToString();

                byte[] buft = new byte[30];
                for (int j1 = 0, j2 = 0; j2 <= s.Length - 2; j1++)
                {
                    buft[j1] = Convert.ToByte(s.Substring(j2, 2), 16);
                    j2 = j2 + 2;
                }

                if (ctrlMode == ControlMode.Manual)
                {
                    roboClient.Send(buft);
                    Console.WriteLine();
                    Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffff"));
                    Console.WriteLine(str);
                    Console.WriteLine(s);
                    halfAutoCommand = serialCommand;
                }
                serialCommand = buft;

                if (sw != null)
                {
                    try
                    {
                        sw.WriteLine();
                        sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffff"));
                        sw.Write(str);
                        sw.WriteLine(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                if (ks1 == 0 && ks2 == 0) serial.Send("0");
                if (ks1 == 1 || ks2 == 1) serial.Send("1");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void disableCurrentControlMode()
        {
            switch (ctrlMode)
            {
                case ControlMode.Auto:
                    {
                        autoClient.Disconnect();
                        if (!autoClient.IsOpen)
                        {
                            modeAutoButton.BackColor = DisableColor;
                            ctrlMode = serial.IsOpen ? ControlMode.Manual : ControlMode.None;
                        }
                        else
                        {
                            MessageBox.Show("断开自主抓取服务失败");
                        }
                        break;
                    }
                case ControlMode.HalfAuto:
                    {
                        halfAutoModeButton.BackColor = DisableColor;
                        halfAutoSendWorker.CancelAsync();
                        ctrlMode = serial.IsOpen ? ControlMode.Manual : ControlMode.None;
                        break;                   
                    }
                default:
                    break;
            }
        }

        private void halfAutoModeButtonClick(object sender, EventArgs e)
        {
            halfAutoCommand = serialCommand;
            if (ctrlMode != ControlMode.HalfAuto)
            {
                if (halfAutoServer.IsWorking)
                {
                    ctrlMode = ControlMode.HalfAuto;
                    halfAutoSendWorker.RunWorkerAsync();
                    modeAutoButton.BackColor = DisableColor;
                    halfAutoModeButton.BackColor = EnableColor;
                    addInfo("切换半自动模式");
                }
                else
                {
                    MessageBox.Show("半自动模式启动失败");
                }
            }
            else
            {
                halfAutoModeButton.BackColor = DisableColor;
                ctrlMode = serial.IsOpen ? ControlMode.Manual : ControlMode.None;
                halfAutoSendWorker.CancelAsync();
                addInfo("切换手动模式");
            }
        }

        private void onHalfAutoOperationData(byte[] data)
        {
            halfAutoCommand = data;
        }

        // unused
        private void haltButtonClick(object sender, EventArgs e)
        {
            var halt = new NetworkManager(new IPEndPoint(IPAddress.Parse("192.168.0.8"), 12345));
            halt.OnLog += addInfo;
            halt.Connect();
            if (!halt.IsOpen)
            {
                MessageBox.Show("连接关机服务失败");
                return;
            }
            var StopCommand = Encoding.UTF8.GetBytes("stop");
            halt.Send(StopCommand);
            halt.Disconnect();
            if (halt.IsOpen)
            {
                MessageBox.Show("关闭关机服务失败");
                return;
            }
        }
        
        private void modeAutoButtonClick(object sender, EventArgs e)
        {
            if (ctrlMode != ControlMode.Auto)
            {
                autoClient.Connect();
                if (autoClient.IsOpen)
                {
                    string asd1 = "2505DC05DC05DC05DC05DC05DC0000000000000000000000000000000021";
                    byte[] buft = new byte[30];
                    for (int j1 = 0, j2 = 0; j2 <= asd1.Length - 2; j1++)
                    {
                        buft[j1] = Convert.ToByte(asd1.Substring(j2, 2), 16);
                        j2 = j2 + 2;
                    }
                    for (int zxcaq = 0; zxcaq < 10; zxcaq++)
                    {
                        autoClient.Send(buft);
                    }

                    halfAutoModeButton.BackColor = DisableColor;
                    modeAutoButton.BackColor = EnableColor;
                    if (ctrlMode == ControlMode.HalfAuto)
                    {
                        halfAutoSendWorker.CancelAsync();
                    }
                    ctrlMode = ControlMode.Auto;
                    addInfo("切换自动模式");
                }
                else
                {
                    MessageBox.Show("连接自主抓取服务失败");
                }
            }
            else
            {
                autoClient.Disconnect();
                if (!autoClient.IsOpen)
                {
                    modeAutoButton.BackColor = DisableColor;
                    ctrlMode = serial.IsOpen ? ControlMode.Manual : ControlMode.None;
                    addInfo("切换手动模式");
                }
                else
                {
                    MessageBox.Show("断开自主抓取服务失败");
                }
            }
        }

        void onOperationData(byte[] data)
        {
            if (ctrlMode == ControlMode.Auto)
            {
                Console.WriteLine();
                Console.WriteLine("全自动模式下发数据");
                Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:ffff"));
                Console.WriteLine(BitConverter.ToString(data, 0).Replace("-", string.Empty));
                roboClient.Send(data);
            }
        }


        private void disableCamUI()
        {
            switch (vision)
            {
                case BrowserVision.MainCam:
                    mainCamButton.BackColor = DisableColor;
                    break;
                case BrowserVision.MainDetect:
                    mainDetectButton.BackColor = DisableColor;
                    break;
                case BrowserVision.DualCam:
                    dualCamButton.BackColor = DisableColor;
                    break;
                case BrowserVision.MainEnhance:
                    mainEnhanceButton.BackColor = DisableColor;
                    break;
                case BrowserVision.DualDetect:
                    dualDetectButton.BackColor = DisableColor;
                    break;
                case BrowserVision.ViceCam:
                    viceCamButton.BackColor = DisableColor;
                    break;
                default:
                    break;
            }
        }

        private void mainCamButtonClick(object sender, EventArgs e) // 主摄像头
        {
            if (vision == BrowserVision.ViceCam)
            {
                webBrowser.Stop();
            }
            disableCamUI();

            if (vision != BrowserVision.MainCam)
            {
                chromiumWebBrowser.Load(MainCamURL);
                chromiumWebBrowser.BringToFront();
                vision = BrowserVision.MainCam;
                mainCamButton.BackColor = EnableColor;
            }
            else
            {
                chromiumWebBrowser.Stop();
                mainPanelMask.BringToFront();
                vision = BrowserVision.None;
                mainCamButton.BackColor = DisableColor;
            }
        }

        private void mainDetectButtonClick(object sender, EventArgs e)
        {
            if (vision == BrowserVision.ViceCam)
            {
                webBrowser.Stop();
            }
            disableCamUI();

            if (vision != BrowserVision.MainDetect)
            {
                chromiumWebBrowser.Load(MainDetectURL);
                chromiumWebBrowser.BringToFront();
                vision = BrowserVision.MainDetect;
                mainDetectButton.BackColor = EnableColor;
            }
            else
            {
                chromiumWebBrowser.Stop();
                mainPanelMask.BringToFront();
                vision = BrowserVision.None;
                mainDetectButton.BackColor = DisableColor;
            }
        }

        private void DualCamButtonClick(object sender, EventArgs e) // 双目
        {
            if (vision == BrowserVision.ViceCam)
            {
                webBrowser.Stop();
            }
            disableCamUI();

            if (vision != BrowserVision.DualCam)
            {
                chromiumWebBrowser.Load(DualCamURL);
                chromiumWebBrowser.BringToFront();
                vision = BrowserVision.DualCam;
                dualCamButton.BackColor = EnableColor;
            }
            else
            {
                chromiumWebBrowser.Stop();
                mainPanelMask.BringToFront();
                vision = BrowserVision.None;
                dualCamButton.BackColor = DisableColor;
            }
        }

        private void mainEnhanceButtonClick(object sender, EventArgs e)//图像增强
        {
            if (vision == BrowserVision.ViceCam)
            {
                webBrowser.Stop();
            }
            disableCamUI();

            if (vision != BrowserVision.MainEnhance)
            {
                chromiumWebBrowser.Load(MainEnhanceURL);
                chromiumWebBrowser.BringToFront();
                vision = BrowserVision.MainEnhance;
                mainEnhanceButton.BackColor = EnableColor;
            }
            else
            {
                chromiumWebBrowser.Stop();
                mainPanelMask.BringToFront();
                vision = BrowserVision.None;
                mainEnhanceButton.BackColor = DisableColor;
            }
        }

        private void dualDetectButtonClick(object sender, EventArgs e) // 双目检测
        {
            if (vision == BrowserVision.ViceCam)
            {
                webBrowser.Stop();
            }
            disableCamUI();

            if (vision != BrowserVision.DualDetect)
            {
                chromiumWebBrowser.Load(DualDetectURL);
                chromiumWebBrowser.BringToFront();
                vision = BrowserVision.DualDetect;
                dualDetectButton.BackColor = EnableColor;
            }
            else
            {
                chromiumWebBrowser.Stop();
                mainPanelMask.BringToFront();
                vision = BrowserVision.None;
                dualDetectButton.BackColor = DisableColor;
            }
        }

        private void viceCamButtonClick(object sender, EventArgs e)
        {
            disableCamUI();
            if (vision != BrowserVision.ViceCam)
            {
                webBrowser.Navigate(ViceCamURL);
                webBrowser.ScriptErrorsSuppressed = true;
                webBrowser.BringToFront();
                vision = BrowserVision.ViceCam;
                viceCamButton.BackColor = EnableColor;
            }
            else
            {
                webBrowser.Stop();
                mainPanelMask.BringToFront();
                vision = BrowserVision.None;
                viceCamButton.BackColor = DisableColor;
            }
        }

        private void panel19_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel19.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel2.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel3.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel4.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel5.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel6.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel7_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel7.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel16_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel16.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel17_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                                 this.panel17.ClientRectangle,
                                 Color.Silver,//7f9db9
                                 1,
                                 ButtonBorderStyle.Solid,
                                 Color.Silver,
                                 1,
                                 ButtonBorderStyle.Solid,
                                 Color.Silver,
                                 1,
                                 ButtonBorderStyle.Solid,
                                 Color.Silver,
                                 1,
                                 ButtonBorderStyle.Solid);

        }

        private void panel12_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                              this.panel12.ClientRectangle,
                              Color.Silver,//7f9db9
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid);
        }

        private void panel10_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                              this.panel10.ClientRectangle,
                              Color.Silver,//7f9db9
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid);
        }

        private void panel11_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                              this.panel11.ClientRectangle,
                              Color.Silver,//7f9db9
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid);
        }

    
      
       
        private void panel13_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                              this.panel13.ClientRectangle,
                              Color.Silver,//7f9db9
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid);
        }

        private void panel14_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                              this.panel14.ClientRectangle,
                              Color.Silver,//7f9db9
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid);
        }

        private void panel15_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                              this.panel15.ClientRectangle,
                              Color.Silver,//7f9db9
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid,
                              Color.Silver,
                              1,
                              ButtonBorderStyle.Solid);
        }

        // 以下一堆paint不知道写来干嘛用，不敢动
       private void label22_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                             this.keepDepthLabel.ClientRectangle,
                             Color.Silver,//7f9db9
                             1,
                             ButtonBorderStyle.Solid,
                             Color.Silver,
                             1,
                             ButtonBorderStyle.Solid,
                             Color.Silver,
                             1,
                             ButtonBorderStyle.Solid,
                             Color.Silver,
                             1,
                             ButtonBorderStyle.Solid);
        }

        private void label23_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                            this.keepYawLabel.ClientRectangle,
                            Color.Silver,//7f9db9
                            1,
                            ButtonBorderStyle.Solid,
                            Color.Silver,
                            1,
                            ButtonBorderStyle.Solid,
                            Color.Silver,
                            1,
                            ButtonBorderStyle.Solid,
                            Color.Silver,
                            1,
                            ButtonBorderStyle.Solid);
        }

        private void label25_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.modeSidePushLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);

        }

        private void label26_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                                  this.angleLabel.ClientRectangle,
                                  Color.Silver,//7f9db9
                                  1,
                                  ButtonBorderStyle.Solid,
                                  Color.Silver,
                                  1,
                                  ButtonBorderStyle.Solid,
                                  Color.Silver,
                                  1,
                                  ButtonBorderStyle.Solid,
                                  Color.Silver,
                                  1,
                                  ButtonBorderStyle.Solid);

        }

        private void label27_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.armLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void label28_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.modeDownLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void label29_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.brightnessLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void label30_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.driverLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void panel18_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.panel18.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void label32_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.manualLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void label38_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.autoLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }

        private void label31_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.controlPermLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);
        }
        private void halfAutoLabel_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics,
                               this.halfAutoLabel.ClientRectangle,
                               Color.Silver,//7f9db9
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid,
                               Color.Silver,
                               1,
                               ButtonBorderStyle.Solid);

        }


        // deprecated
        string curdateTime = "";
        public void GetScreen()//截图
        {
            if (!System.IO.Directory.Exists(@"D:\picture"))
            {
                System.IO.Directory.CreateDirectory(@"D:\picture");//不存在就创建目录 
            }

            //获取整个屏幕图像,不包括任务栏
            Rectangle ScreenArea = Screen.GetWorkingArea(this);
            Bitmap bmp = new Bitmap(ScreenArea.Width, ScreenArea.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, new Size(ScreenArea.Width, ScreenArea.Height));
                curdateTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                bmp.Save(@"D:\picture" + @"\" + curdateTime + ".jpg");//直接存储在c盘会出错
            }
            return;
        }

        // deprecated
        public void GetCommand()
        {
            // string tem_com1 = downstr;
            // string result = tem_com1.Trim(); //输入文本
            StreamWriter sw = File.AppendText(@"D:\picture\"+ curdateTime+".txt"); //保存到指定路径
            // sw.Write(result);
            sw.Flush();
            sw.Close();
        }

        StreamWriter sw;
        private void recordCommand(object sender, EventArgs e)
        {
            if (sw != null)
            {
                MessageBox.Show("已在记录中");
                return;
            }
            var curDateTime = DateTime.Now;
            sw = File.AppendText(@"C:\Users\ROV\Documents\SerialRecord\" + curDateTime.ToString("yyyy-MM-dd HH-mm-ss-ffff") + ".txt");
            addInfo(string.Format("开始记录:{0}", curDateTime.ToString("HH:mm:ss:ffff")));
        }

        private void recordCommandStop(object sender, EventArgs e)
        {
            if (sw == null)
            {
                MessageBox.Show("请先开始记录");
                return;
            }
            sw.Flush();
            sw.Close();
            sw = null;
            addInfo("结束记录");
        }

        // deprecated
        private void pictureBox4_Click(object sender, EventArgs e)//保存截图和命令
        {
            GetScreen();
            infoListBox.Items.Add("截图成功");
            GetCommand();
        }
    }
}
