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
        string[] comm = new string[20];//控制仓
        string[] comm2 = new string[20];//电源仓
        string[] temcomm = new string[17];
        Thread myThread;
        public delegate void MyDelegateUI(); //定义委托类型
        MyDelegateUI myDelegateUI; //声明委托对象
        static Socket clientSocket;
        static Socket serverSocket;
        SerialManager serial;
        ChromiumWebBrowser chromeBrowser;
        ChromiumWebBrowser chromeBrowser2;
        ChromiumWebBrowser chromeBrowser3;
        ChromiumWebBrowser chromeBrowser4;
        ChromiumWebBrowser chromeBrowser5;
        ChromiumWebBrowser chromeBrowser6;

        static Color DisableColor = Color.FromArgb(67, 67, 67);
        static Color EnableColor = Color.FromArgb(0, 64, 0);
        static Color BusyColor = Color.FromArgb(255, 77, 59);
        static Color NormalColor = Color.FromArgb(0, 192, 0);
        static Color ErrorColor = Color.FromArgb(255, 0, 0);

        static TimeSpan SerialSendInterval = TimeSpan.FromMilliseconds(100);
        static TimeSpan ControlPermDetectInterval = TimeSpan.FromMilliseconds(400);

        Color serialColor = DisableColor;//串口状态
        Color networkColor = DisableColor;//网口状态

        Color mainCabColor = NormalColor;//控制舱是否漏水
        Color powerCabColor = NormalColor;//电源舱是否漏水
        DateTime lastSerialTime;
        int ks1 = -1, ks2 = -1;
        int qjht = 0;//前进  后退

        int zy1 = 0;//左右
        int sx1 = 0;//上下
        int scd1 = 0;
        string downstr = "";

        public MainForm()
        {
            InitializeComponent();
            CefSettings settings = new CefSettings();
            Cef.Initialize(settings);
        }

        private void Form1_Load(object sender, EventArgs e)
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
            comm[9] = "1";
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
            mainCabTempLabel.BringToFront();
            myThread = new Thread(doWork);
            myDelegateUI += updateInfo;//绑定委托
            myThread.Start();

            serial = new SerialManager("COM3", 9600);
            serial.OnReceived += onSerialMessage;
            serial.OnLog += (string info) => listBox1.Items.Add(info);
        }

        void torectangle()
        {
            GraphicsPath gp1 = new GraphicsPath();
            GraphicsPath gp3 = new GraphicsPath();
            GraphicsPath gp4 = new GraphicsPath();
            GraphicsPath gp5 = new GraphicsPath();
            GraphicsPath gp6 = new GraphicsPath();
            gp1.AddEllipse(pictureBox1.ClientRectangle);
            gp3.AddEllipse(pictureBox3.ClientRectangle);
            gp4.AddEllipse(pictureBox4.ClientRectangle);
            gp5.AddEllipse(pictureBox5.ClientRectangle);
            gp6.AddEllipse(pictureBox6.ClientRectangle);
            Region region1 = new Region(gp1);
            Region region3 = new Region(gp3);
            Region region4 = new Region(gp3);
            Region region5 = new Region(gp3);
            Region region6 = new Region(gp3);
            pictureBox1.Region = region1;
            pictureBox3.Region = region3;
            pictureBox4.Region = region4;
            pictureBox5.Region = region5;
            pictureBox6.Region = region6;

            gp1.Dispose();
            region1.Dispose();
            gp3.Dispose();
            region3.Dispose();

            gp4.Dispose();
            region4.Dispose();
            gp5.Dispose();
            region5.Dispose();
            gp6.Dispose();
            region6.Dispose();
        }

        // 一言难尽，实在是改不动了
        private void updateInfo() //信息处理函数定义
        {
            serialStatLabel.BackColor = serialColor;
            networkStatLabel.BackColor = networkColor;
            mainCabTempLabel.Text = "温度:" + Math.Round(Convert.ToDouble(comm[0]), 1) + "℃";//仓内温度
            powerCabTempLabel.Text = "温度:" + Math.Round(Convert.ToDouble(comm2[0]), 1) + "℃";

            mainCabPressLabel.Text = "气压:" + Math.Round(Convert.ToDouble(comm[1]), 1) + "KPa";
            powerCabPressLabel.Text = "气压:" + Math.Round(Convert.ToDouble(comm2[1]), 1) + "KPa";

            mainCabHumidLabel.Text = "湿度:" + comm[2] + "%";//湿度
            powerCabHumidLabel.Text = "湿度:" + comm2[2].ToString() + "%";

            waterTempLabel.Text = "水温:" + Math.Round(Convert.ToDouble(comm[11]), 1) + "℃";//水温
            depthLabel.Text = "深度:" + Math.Round(Convert.ToDouble(comm[12]), 2) + "M"; //水深
            heightLabel.Text = "高度:" + Math.Round(Convert.ToDouble(comm[9]), 2) + "M";

            rollLabel.Text = string.Format("横滚角{0}°", comm[3]);
            pitchLabel.Text = string.Format("俯仰角{0}°", comm[4]);
            yawLabel.Text = string.Format("航向角{0}°", comm[5]);
            horiCt.Hori_Disp(Convert.ToDouble(comm[4]), -Convert.ToDouble(comm[3]));

            mainCabErrorLabel.BackColor = mainCabColor;
            powerCabErrorLabel.BackColor = powerCabColor;
            controlPermLabel.BackColor = DateTime.Now.Subtract(lastSerialTime) > ControlPermDetectInterval ? DisableColor : EnableColor;
            //label32.BackColor = sd1;

            xLabel.Text = qjht.ToString();
            yLabel.Text = zy1.ToString();
            zLabel.Text = sx1.ToString();
            if (qjht >= 0)
                xProcessLine.Value = qjht;
            else
                xProcessLine.Value = -qjht;
            if (zy1 >= 0)
                yProcessLine.Value = zy1;
            else
                yProcessLine.Value = -zy1;
            if (sx1 >= 0)
                zProcessLine.Value = sx1;
            else
                zProcessLine.Value = -sx1;



            //侧推1 定向2 定深4 机械臂8
            //25 23        22  27
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

            angleLabel.Text = "云台:" + (Convert.ToInt32(temcomm[10]) - 500) / 10;


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
        }



        private void doWork()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                    Invoke(myDelegateUI);
                    Application.DoEvents();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex);
                    break;
                }
            }
        }

        private void networkPanel_Click(object sender, EventArgs e)
        {
            var IP = "192.168.0.1";
            int port = 1234;

            IPAddress ip = IPAddress.Parse(IP);  //将IP地址字符串转换成IPAddress实例   
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//使用指定的地址簇协议、套接字类型和通信协议
            IPEndPoint endPoint = new IPEndPoint(ip, port); // 用指定的ip和端口号初始化IPEndPoint实例
            listBox1.Items.Add("网络连接中...");
            try
            {
                clientSocket.Connect(endPoint);  //与远程主机建立连接
                Console.WriteLine("网络连接成功");
                listBox1.Items.Add("网络连接成功");
                networkColor = EnableColor;
                Thread th = new Thread(ReceiveMsg);
                th.IsBackground = true;
                th.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show("网口连接失败");
                listBox1.Items.Add("网络连接失败");
            }

        }
        void ReceiveMsg()
        {
            var receive = new byte[1024];
            var index = 0;
            while (true)
            {
                try
                {
                    int length = clientSocket.Receive(receive, index, 47, 0);
                    if (length == 0) continue;
                    index += length;

                    if (index > receive.Length * 0.8)
                    {
                        index = 0;
                        for (var i = 0; i < receive.Length; i++)
                        {
                            receive[i] = 0;
                        }
                        continue;
                    }

                    /*Console.WriteLine();
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine(BitConverter.ToString(receive, 0, index).Replace("-", " "));*/

                    var (sensor, start, end) = SensorDecoder.Decode(receive);
                    for (; sensor != null; (sensor, start, end) = SensorDecoder.Decode(receive))
                    {

                        for (var i = end; i < receive.Length; i++)
                        {
                            receive[i - end] = receive[i];
                        }
                        for (var i = receive.Length - end; i < receive.Length; i++)
                        {
                            receive[i] = 0;
                        }
                        index -= end;

                        /*Console.WriteLine(sensor.ToString());*/

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

                            // int hx1 = sensor.Yaw;
                            // int hx2 = hx1 - 180;
                            // if (hx2 > 0) hx2 = System.Math.Abs(hx2 - 180);
                            // else hx2 = System.Math.Abs(hx2) + 180;

                            // if (hx2 < 190) hx2 = hx2 + 170;
                            // else hx2 = hx2 + 170 - 360;

                            // comm[5] = hx1.ToString();

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
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    clientSocket.Close();
                    break;
                }
            }
        }


        /*        private void pictureBox3_Click(object sender, EventArgs e)
                { 

                    try
                    { sp2.PortName = "COM8";
                    sp2.BaudRate = 9600;
                        sp2.Open();
                    }
                    catch(Exception ex) { MessageBox.Show(ex.ToString()); Console.WriteLine(ex); }
                }*/


        private void serialPanel_Click(object sender, EventArgs e)
        {
            if (!serial.IsOpen)
            {
                serial.Connect();
                if (serial.IsOpen)
                {
                    manualLabel.BackColor = EnableColor;
                    serialColor = EnableColor;
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
                    manualLabel.BackColor = DisableColor;
                    serialColor = DisableColor;
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
                lastSerialTime = time;

                downstr = str;
                var comm = str.Split(':');
                temcomm = comm;

                var sb = new StringBuilder();
                sb.Append(25);
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
                qjht = (Convert.ToInt32(temcomm[1]) - 1500) / 10; // 前进后退
                zy1 = (Convert.ToInt32(temcomm[2]) - 1500) / 10; // 左右
                sx1 = (Convert.ToInt32(temcomm[3]) - 1500) / 10;
                scd1 = (Convert.ToInt32(temcomm[6]) - 1500) / 10; // 传送带
                clientSocket.Send(buft);

                Console.WriteLine();
                Console.WriteLine(DateTime.Now);
                Console.WriteLine(str);
                Console.WriteLine(s);

                if (ks1 == 0 && ks2 == 0) serial.Send("0");
                if (ks1 == 1 || ks2 == 1) serial.Send("1");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        int fla = 0;//主摄像头
        int fla2 = 0;
        int fla3 = 0;
        int fla4 = 0;//多摄像头
        int fla5 = 0;
        int fla6 = 0;
        private void mainCamButtonClick(object sender, EventArgs e)//主摄像头
        {
           
            if (fla2 == 1) { chromeBrowser2.Stop(); fla2 = 0; } 
            if (fla3 == 1) { chromeBrowser3.Stop(); fla3 = 0; }
            if (fla4 == 1) { chromeBrowser4.Stop(); fla4 = 0; }
            if (fla5 == 1) { chromeBrowser5.Stop(); fla5 = 0; }
            if (fla6 == 1) { chromeBrowser6.Stop(); fla6 = 0; }

            if (fla == 0)
            {
                string page = "http://192.168.0.123:5000/mono_cam_viewer_no_det";
             //   string page = "http://www.baidu.com";
                chromeBrowser = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser);
                fla = 1;
                chromeBrowser.Dock = DockStyle.Fill;
                chromeBrowser.BringToFront();
               // Cef.EnableHighDPISupport();
              
               // chromeBrowser.Site=
                //chromeBrowser.
              // chromeBrowser.SetZoomLevel(1.25);
            }
            webBrowser1.Visible = false;
        }

        private void mainDetectButtonClick(object sender, EventArgs e)
        {
            if (fla == 1) { chromeBrowser.Stop(); fla = 0; }
            if (fla2 == 1) { chromeBrowser2.Stop(); fla2 = 0; }
            if (fla4 == 1) { chromeBrowser4.Stop(); fla4 = 0; }
            if (fla5 == 1) { chromeBrowser5.Stop(); fla5 = 0; }
            if (fla6 == 1) { chromeBrowser6.Stop(); fla6 = 0; }

            if (fla3 == 0)
            {
                string page = "http://192.168.0.123:5000/mono_cam_viewer";
                chromeBrowser3 = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser3);
                fla3 = 1;
                chromeBrowser3.Dock = DockStyle.Fill;
                chromeBrowser3.BringToFront();
            }
            webBrowser1.Visible = false;
        }

        private void DualCamButtonClick(object sender, EventArgs e)//双目
        {
            if (fla == 1) { chromeBrowser.Stop(); fla = 0; }
            if (fla3 == 1) { chromeBrowser3.Stop(); fla3 = 0; }
            if (fla4 == 1) { chromeBrowser4.Stop(); fla4 = 0; }
            if (fla5 == 1) { chromeBrowser5.Stop(); fla5 = 0; }
            if (fla6 == 1) { chromeBrowser6.Stop(); fla6 = 0; }

            if (fla2 == 0)
            {
                string page = "http://192.168.0.123:5000/stero_cam_viewer_no_det";
                chromeBrowser2 = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser2);
                fla2 = 1;
                chromeBrowser2.Dock = DockStyle.Fill;
                chromeBrowser2.BringToFront();
            }
            webBrowser1.Visible = false;
           
        }

        private void mainEnhanceButtonClick(object sender, EventArgs e)//图像增强
        {
            if (fla == 1) { chromeBrowser.Stop(); fla = 0; }
            if (fla2 == 1) { chromeBrowser2.Stop(); fla2 = 0; }
            if (fla3 == 1) { chromeBrowser3.Stop(); fla3 = 0; }
            if (fla4 == 1) { chromeBrowser4.Stop(); fla4 = 0; }
            if (fla6 == 1) { chromeBrowser6.Stop(); fla6 = 0; }

            if (fla5 == 0)
            {
                string page = "http://192.168.0.123:5000/enhance";
                chromeBrowser5 = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser5);
                fla5 = 1;
                chromeBrowser5.Dock = DockStyle.Fill;
                chromeBrowser5.BringToFront();
            }
            webBrowser1.Visible = false;
        }

        private void viceCamButtonClick(object sender, EventArgs e)
        {
            if (fla == 1) { chromeBrowser.Stop(); fla = 0; }
            if (fla2 == 1) { chromeBrowser2.Stop(); fla2 = 0; }
            if (fla3 == 1) { chromeBrowser3.Stop(); fla3 = 0; }
            if (fla4 == 1) { chromeBrowser4.Stop(); fla4 = 0; }
            if (fla5 == 1) { chromeBrowser5.Stop(); fla5 = 0; }

            if (fla6 == 0)
            {
                string page = "http://192.168.0.8";
                chromeBrowser6 = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser6);
                fla6 = 1;
                chromeBrowser6.Dock = DockStyle.Fill;
                chromeBrowser6.SendToBack();
                webBrowser1.Visible = true;
                webBrowser1.Navigate("http://192.168.0.8");
                webBrowser1.ScriptErrorsSuppressed = true;
                webBrowser1.BringToFront();
            }

            /*if (fla == 1) { chromeBrowser.Stop(); fla = 0; }
            if (fla2 == 1) { chromeBrowser2.Stop(); fla2 = 0; }
            if (fla3 == 1) { chromeBrowser3.Stop(); fla3 = 0; }
            if (fla4 == 1) { chromeBrowser4.Stop(); fla4 = 0; }
            if (fla5 == 1) { chromeBrowser5.Stop(); fla5 = 0; }

            if (fla6 == 0)
            {
                string page = "http://192.168.0.8";
                chromeBrowser6 = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser6);
                fla6 = 1;
                chromeBrowser6.Dock = DockStyle.Fill;
                chromeBrowser6.BringToFront();
            }
           */

            webBrowser1.Visible = true;

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

        int zzms = 0;
        private void modeAutoButtonClick(object sender, EventArgs e)
        {
            if (zzms == 0)//开启自主抓取
            { 
                var IP = "192.168.0.123";
                int port = 6000;
                IPAddress ip = IPAddress.Parse(IP);  //将IP地址字符串转换成IPAddress实例   
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//使用指定的地址簇协议、套接字类型和通信协议
                IPEndPoint endPoint = new IPEndPoint(ip, port); // 用指定的ip和端口号初始化IPEndPoint实例
                modeAutoButton.BackColor = EnableColor;
                manualLabel.BackColor = DisableColor;
                autoLabel.BackColor = EnableColor;
                zzms = 1;
                try
                {
                    serverSocket.Connect(endPoint);  //与远程主机建立连接
                    Console.WriteLine("服务连接成功");
                    listBox1.Items.Add("服务连接成功");
                    
                    Thread th = new Thread(ReceiveMsg2);
                    th.IsBackground = true;
                    th.Start();
                    string asd1 = "2505DC05DC05DC05DC05DC05DC0000000000000000000000000000000021";
                    byte[] buft = new byte[30];
                    for (int j1 = 0, j2 = 0; j2 <= asd1.Length - 2; j1++)
                    {
                        buft[j1] = Convert.ToByte(asd1.Substring(j2, 2), 16);
                        j2 = j2 + 2;
                    }
                    for(int zxcaq=0;zxcaq<10;zxcaq++)
                        serverSocket.Send(buft);


                    /*for (int j1 = 0; ; j1++)
                    {
                        buft = Convert.ToByte(command.Substring(84, 4), 16);
                    
                    }*/
                 

                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("自主抓取机网络连接失败{0}", ex));  
                    listBox1.Items.Add("自主抓取机网络连接失败");
                }
            }
            else//关闭自主抓取
            {
               /* try 
                {
                  
      
                    listenThread.Start();
                    
                }
                catch(Exception ex) { MessageBox.Show(ex.ToString());}*/
                listBox1.Items.Add("关闭自主抓取");
                manualLabel.BackColor = EnableColor;
                autoLabel.BackColor = DisableColor;
                modeAutoButton.BackColor = DisableColor;
                zzms = 0;
                try 
                {
                    serverSocket.Close(); 
                }
                catch(Exception ex) { MessageBox.Show(ex.ToString()); }
                
            }
        
        }
        void ReceiveMsg2()
        {
            byte[] receive2 = new byte[8 * 1024];
            string buffer2;
            string tem_ttt;
            while (true)
            {
                try
                {
                    int length = serverSocket.Receive(receive2);
                    if (length == 0)
                        continue;
                    buffer2 = BitConverter.ToString(receive2).Replace("-", "");
                    if (buffer2.IndexOf("25", 1) == -1)
                        continue;
                    tem_ttt = buffer2.Substring(buffer2.IndexOf("25", 1), 60);
                    if (tem_ttt[58] != '2' || tem_ttt[59] != '1' )
                        continue;
                  
                    byte[] buft = new byte[30];
                    for (int j1 = 0, j2 = 0; j2 <= tem_ttt.Length - 2; j1++)
                    {
                        buft[j1] = Convert.ToByte(tem_ttt.Substring(j2, 2), 16);
                        j2 = j2 + 2;
                    }

                    clientSocket.Send(buft);
               
                  /* byte[] buft2= Encoding.UTF8.GetBytes(command.Substring(84, 4));
                    serverSocket.Send(buft2);*/
                }
                catch(Exception ex) {
                    Console.WriteLine(ex);
                    serverSocket.Close();
                    break;
                }
            }
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
                bmp.Save("D:\\picture" + "\\" + curdateTime + ".jpg");//直接存储在c盘会出错
            }
            return;
        }

        // deprecated
        public void GetCommand()
        {
            string tem_com1 = downstr;
            string result = tem_com1.Trim(); //输入文本
            StreamWriter sw = File.AppendText(@"D:\\picture\\"+ curdateTime+".txt"); //保存到指定路径
            sw.Write(result);
            sw.Flush();
            sw.Close();
        }
       

        // deprecated
        private void pictureBox4_Click(object sender, EventArgs e)//保存截图和命令
        {
            GetScreen();
            listBox1.Items.Add("截图成功");
            GetCommand();
        }

        private void haltButton_Click(object sender, EventArgs e)
        {

        }

        private void dualDetectButtonClick(object sender, EventArgs e)//双目检测
        {
            if (fla == 1) { chromeBrowser.Stop();fla = 0; }
            if (fla2 == 1) { chromeBrowser2.Stop(); fla2 = 0; }
            if (fla3 == 1) { chromeBrowser3.Stop(); fla3 = 0; }
            if (fla5 == 1) { chromeBrowser5.Stop(); fla5 = 0; }
            if (fla6 == 1) { chromeBrowser6.Stop(); fla6 = 0; }

            if (fla4 == 0)
            {
                //string page = "http://192.168.0.123:5000";
                string page = "http://192.168.0.123:5000/stero_cam_viewer";
                chromeBrowser4 = new ChromiumWebBrowser(page);
                panel1.Controls.Add(chromeBrowser4);
                fla4 = 1;
                chromeBrowser4.Dock = DockStyle.Fill;
                chromeBrowser4.BringToFront();
            }
            webBrowser1.Visible = false;
        }

    }
}
