using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    class NetworkManager
    {  
        private Socket socket;
        private EndPoint endPoint;
        private BackgroundWorker worker;

        public int ReceiveSize = 50; // 每次接收大小
        public int BufferSize = 1024; // 缓冲区大小
        public double CleanThreshold = 0.8; // 达到BufferSize * CleanThreshold后清理全部缓冲区;

        public delegate (int, int) DataDetector(byte[] data); // 用于检测接收到的数据中是否有有效数据
        public DataDetector Detector;

        public delegate void ReceivedHandler(byte[] data);
        public ReceivedHandler OnReceived;

        public delegate void LogHandler(string info);
        public LogHandler OnLog;

        public NetworkManager(EndPoint endPoint) : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), endPoint) { }

        public NetworkManager(Socket socket, EndPoint endPoint)
        {
            this.socket = socket;
            this.endPoint = endPoint;

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
                    OnLog?.Invoke("网络监听异常终止");
                    Console.WriteLine(e.Error);
                }
                else if (e.Cancelled)
                {
                    OnLog?.Invoke("网络监听正常退出");
                }
                else
                {
                    OnLog?.Invoke("网络监听终止");
                }
            };

        }

        public bool IsOpen
        {
            get
            {
                return socket.Connected;
            }
        }

        public void Connect()
        {
            try
            {
                if (!IsOpen)
                {
                    socket.Connect(endPoint);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("连接网络失败");
                Console.WriteLine("连接网络失败");
                Console.WriteLine(ex);
                return;
            }
            while (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                OnLog?.Invoke("启动网络监听");
            }
        }

        public void Disconnect()
        {
            OnLog?.Invoke("断开网络");
            try
            {
                if (IsOpen)
                {
                    socket.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("断开网络失败");
                Console.WriteLine("断开网络失败");
                Console.WriteLine(ex);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                if (IsOpen)
                {
                    socket.Send(data);
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
            var receive = new byte[BufferSize];
            var index = 0;
            while (IsOpen && !worker.CancellationPending)
            {
                try
                {
                    int length = socket.Receive(receive, index, ReceiveSize, 0);
                    if (length == 0) continue;
                    index += length;

                    if (index > receive.Length * CleanThreshold)
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
                    if (Detector == null) {
                        OnReceived(receive);
                        index = 0;
                        continue;
                    }

                    var (start, end) = Detector.Invoke(receive);
                    for (; !(start == 0 && end == 0); (start, end) = Detector.Invoke(receive))
                    {
                        var data = new byte[end - start];
                        for (var i = 0; i < data.Length; i++)
                        {
                            data[i] = receive[start + i];
                        }
                        OnReceived(data);

                        // clean buffer
                        for (var i = end; i < receive.Length; i++)
                        {
                            receive[i - end] = receive[i];
                        }
                        for (var i = receive.Length - end; i < receive.Length; i++)
                        {
                            receive[i] = 0;
                        }
                        index -= end;
                    }
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
