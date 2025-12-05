using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EventFramework;

namespace EventFramework
{
    /// <summary>
    /// 命令数据结构
    /// </summary>
    public struct CommandData
    {
        public int TargetFrame;  // 目标执行帧号，0 表示立即执行
        public string Command;   // 命令内容

        public CommandData(int targetFrame, string command)
        {
            TargetFrame = targetFrame;
            Command = command;
        }
    }

    /// <summary>
    /// 命令解释器代理，负责接收 UDP 命令并在逻辑线程执行
    /// 使用方式：在逻辑线程初始化时创建实例，每帧调用 ProcessPendingCommands(currentFrame)
    /// </summary>
    public class CommandInterpreterProxy : IDisposable
    {
        public Action<string> ErrorHandler;
        public Action<string> LogHandler;
        private const int UDP_PORT = 11451;
        
        private UdpClient udpListener;
        private CommandInterpreter interpreter;
        private Thread receiveThread;
        private volatile bool isRunning;
        
        // 线程安全的命令队列
        private readonly object commandQueueLock = new object();
        private readonly System.Collections.Generic.Queue<CommandData> commandQueue = new System.Collections.Generic.Queue<CommandData>();
        
        // 延迟执行的命令列表（等待特定帧执行）
        private readonly System.Collections.Generic.List<CommandData> delayedCommands = new System.Collections.Generic.List<CommandData>();

        /// <summary>
        /// 创建命令解释器代理
        /// </summary>
        public CommandInterpreterProxy()
        {
            interpreter = new CommandInterpreter();
        }

        /// <summary>
        /// 启动 UDP 监听
        /// </summary>
        public void Start()
        {
            if (isRunning) return;

            try
            {
                udpListener = new UdpClient(UDP_PORT);
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                isRunning = true;

                receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "CommandInterpreterProxy_Receiver"
                };
                receiveThread.Start();

                LogHandler?.Invoke($"[CommandInterpreterProxy] 已启动，监听端口 {UDP_PORT}");
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
                udpListener.Close();
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
                    
                    // 解析数据：前4字节为帧号(int)，后续为命令字符串(UTF8)
                    if (data.Length >= 4)
                    {
                        int targetFrame = BitConverter.ToInt32(data, 0);
                        string command = Encoding.UTF8.GetString(data, 4, data.Length - 4);

                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            lock (commandQueueLock)
                            {
                                commandQueue.Enqueue(new CommandData(targetFrame, command));
                            }
                        }
                    }
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

        /// <summary>
        /// 处理待执行的命令（应在逻辑线程每帧调用）
        /// </summary>
        /// <param name="currentFrame">当前逻辑帧号</param>
        public void ProcessPendingCommands(int currentFrame)
        {
            // 从队列中取出所有命令
            while (true)
            {
                CommandData cmdData;
                bool hasCommand = false;

                lock (commandQueueLock)
                {
                    if (commandQueue.Count > 0)
                    {
                        cmdData = commandQueue.Dequeue();
                        hasCommand = true;
                    }
                    else
                    {
                        cmdData = default;
                    }
                }

                if (!hasCommand) break;

                // 判断是否需要延迟执行
                if (cmdData.TargetFrame <= 0 || cmdData.TargetFrame <= currentFrame)
                {
                    // 立即执行（TargetFrame <= 0 表示立即执行）
                    ExecuteCommand(cmdData.Command);
                }
                else
                {
                    // 加入延迟队列
                    delayedCommands.Add(cmdData);
                }
            }

            // 检查延迟队列中是否有需要执行的命令
            for (int i = delayedCommands.Count - 1; i >= 0; i--)
            {
                if (delayedCommands[i].TargetFrame <= currentFrame)
                {
                    ExecuteCommand(delayedCommands[i].Command);
                    delayedCommands.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 执行单条命令
        /// </summary>
        private void ExecuteCommand(string command)
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
        /// 获取内部的 CommandInterpreter 实例
        /// </summary>
        public CommandInterpreter Interpreter => interpreter;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}
