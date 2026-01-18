using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EventFramework;

namespace EventFramework
{


    /// <summary>
    /// 命令解释器代理，负责接收 UDP 命令并在逻辑线程执行
    /// 使用方式：在逻辑线程初始化时创建实例，每帧调用 ProcessPendingCommands(currentFrame)
    /// 支持多客户端同时运行（使用 UDP 广播 + 端口复用）
    /// </summary>
    public class CommandInterpreterProxy<DataType> : IDisposable, ICanRegisterPresetCommand
    {
        public Action<string> ErrorHandler;
        public Action<string> LogHandler;

        private UdpClient udpListener;
        private CommandInterpreterV2 interpreter;
        private Thread receiveThread;
        private volatile bool isRunning;

        // 线程安全的命令队列
        protected readonly object commandQueueLock = new object();
        protected readonly System.Collections.Generic.Queue<DataType> commandQueue = new System.Collections.Generic.Queue<DataType>();

        // 延迟执行的命令列表（等待特定帧执行）
        protected readonly System.Collections.Generic.List<DataType> delayedCommands = new System.Collections.Generic.List<DataType>();

        /// <summary>
        /// 创建命令解释器代理
        /// </summary>
        public CommandInterpreterProxy()
        {
            interpreter = new CommandInterpreterV2();
        }

        /// <summary>
        /// 启动 UDP 监听（广播模式，支持多客户端）
        /// </summary>
        public void Start()
        {
            if (isRunning) return;

            try
            {
                udpListener = new UdpClient();

                // 【关键1】启用地址复用，允许多个客户端绑定同一端口
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // 【关键2】绑定到本地端口，监听所有网络接口
                udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, CommandInterpreterHelper.UDP_BROADCAST_PORT));

                isRunning = true;

                // 【关键3】设置为后台线程
                receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "CommandInterpreterProxy_Receiver"
                };
                receiveThread.Start();

                LogHandler?.Invoke($"[CommandInterpreterProxy] 已启动（广播模式），监听端口 {CommandInterpreterHelper.UDP_BROADCAST_PORT}");
            }
            catch (Exception ex)
            {
                ErrorHandler?.Invoke($"[CommandInterpreterProxy] 启动失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止 UDP 监听
        /// </summary>
        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;

            if (udpListener != null)
            {
                try
                {
                    udpListener.Close();
                    udpListener.Dispose();
                }
                catch { }
                udpListener = null;
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000); // 等待最多 1 秒
            }

            LogHandler?.Invoke("[CommandInterpreterProxy] 已停止");
        }

        /// <summary>
        /// UDP 接收循环（在独立线程运行）
        /// </summary>
        private void ReceiveLoop()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                try
                {
                    byte[] data = udpListener.Receive(ref remoteEP);
                    OnReceive(data);
                }
                catch (SocketException)
                {
                    // 正常关闭时会抛出此异常，忽略
                    if (!isRunning) break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        LogHandler?.Invoke($"[CommandInterpreterProxy] 接收错误: {ex.Message}");
                    }
                }
            }
        }
        public virtual void OnReceive(byte[] data)
        {

        }
        /// <summary>
        /// 执行单条命令
        /// </summary>
        protected void ExecuteCommand(string command)
        {
            LogHandler?.Invoke($"[CommandInterpreterProxy] 执行: {command}");

            try
            {
                string result = interpreter.Execute(command);

                if (result.StartsWith("Error:"))
                {
                    ErrorHandler?.Invoke($"[CommandInterpreterProxy] {result}");
                }
                else
                {
                    LogHandler?.Invoke($"[CommandInterpreterProxy] {result}");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler?.Invoke($"[CommandInterpreterProxy] 执行异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册变量到解释器
        /// </summary>
        public void RegisterVariable(string name, object value)
        {
            interpreter.RegisterVariable(name, value);
        }

        /// <summary>
        /// 注册预设变量到解释器
        /// </summary>
        public void RegisterPresetVariable(string name, Func<object> getter)
        {
            interpreter.RegisterPresetVariable(name, getter);
        }

        /// <summary>
        /// 注册预设函数到解释器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public void RegisterPresetFunc(string name, object func)
        {
            interpreter.RegisterPresetFunc(name, func);
        }

        public void RegisterPresetFunc(string name, Type type, string funcname)
        {
            interpreter.RegisterPresetFunc(name, type, funcname);
        }

        /// <summary>
        /// 获取内部的 CommandInterpreterV2 实例
        /// </summary>
        public CommandInterpreterV2 Interpreter => interpreter;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}
