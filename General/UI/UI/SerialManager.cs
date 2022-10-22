using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UI
{
    class SerialManager
    {
        private SerialPort serialPort;
        private BackgroundWorker worker;

        public delegate void ReceivedHandler(string data);
        public ReceivedHandler OnReceived;

        public delegate void LogHandler(string info);
        public LogHandler OnLog;

        public SerialManager(string portName, int baudRate)
        {
            // logger = LogManager.GetLogger("Serial");
            serialPort = new SerialPort(portName, baudRate);

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                listen(worker, e);
            };
            worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (e.Error != null)
                {
                    OnLog?.Invoke("串口监听异常终止");
                    Console.WriteLine(e.Error);
                }
                else if (e.Cancelled)
                {
                    OnLog?.Invoke("串口监听正常退出");
                }
                else
                {
                    OnLog?.Invoke("串口监听终止");
                }
            };

        }

        public bool IsOpen
        {
            get {
                return serialPort.IsOpen;
            }
        }

        public void Connect()
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("连接串口失败");
                Console.WriteLine("连接串口失败");
                Console.WriteLine(ex);
                return;
            }
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                OnLog?.Invoke("启动串口监听");
            }
        }

        public void Disconnect()
        {
            OnLog?.Invoke("断开串口");
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("断开串口失败");
                Console.WriteLine("断开串口失败");
                Console.WriteLine(ex);
            }
        }

        public void Send(string data)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Write(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发送数据失败");
                Console.WriteLine(ex);
            }
        }

        private void listen(BackgroundWorker worker, DoWorkEventArgs e)
        {
            while (serialPort.IsOpen && !worker.CancellationPending) {
                try
                {
                    var str = serialPort.ReadLine();
                    OnReceived(str);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("接收数据失败");
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
