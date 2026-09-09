using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetworkTcpClient = System.Net.Sockets.TcpClient;

public class TcpClientTransport
{
    public bool IsConnected
    {
        get
        {
            return connected;
        }
    }

    public bool IsRunning
    {
        get
        {
            return running;
        }
    }

    public string LastError { get; private set; } = "";

    private NetworkTcpClient socket;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread receiveThread;

    private volatile bool connected;
    private volatile bool running;

    private readonly Queue<string> incomingMessages = new Queue<string>();
    private readonly object incomingLock = new object();
    private readonly object writeLock = new object();

    public bool Connect(string ip, int port)
    {
        if (connected)
        {
            return true;
        }

        ClearIncomingMessages();
        LastError = "";

        try
        {
            socket = new NetworkTcpClient();
            socket.Connect(ip, port);
            socket.NoDelay = true;

            NetworkStream stream = socket.GetStream();
            reader = new StreamReader(stream, new UTF8Encoding(false));
            writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;

            connected = true;
            running = true;

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            connected = false;
            running = false;
            CloseSocket();
            return false;
        }
    }

    public bool Send(string message)
    {
        if (!connected || writer == null)
        {
            return false;
        }

        try
        {
            lock (writeLock)
            {
                writer.WriteLine(message);
            }

            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            connected = false;
            running = false;
            CloseSocket();
            return false;
        }
    }

    public bool TryDequeue(out string message)
    {
        lock (incomingLock)
        {
            if (incomingMessages.Count == 0)
            {
                message = null;
                return false;
            }

            message = incomingMessages.Dequeue();
            return true;
        }
    }

    public void Disconnect()
    {
        connected = false;
        running = false;
        CloseSocket();
    }

    private void ReceiveLoop()
    {
        try
        {
            string line;

            while (running && (line = reader.ReadLine()) != null)
            {
                lock (incomingLock)
                {
                    incomingMessages.Enqueue(line);
                }
            }
        }
        catch (Exception exception)
        {
            if (running)
            {
                LastError = exception.Message;
            }
        }
        finally
        {
            connected = false;
            running = false;
            CloseSocket();
        }
    }

    private void ClearIncomingMessages()
    {
        lock (incomingLock)
        {
            incomingMessages.Clear();
        }
    }

    private void CloseSocket()
    {
        try
        {
            if (socket != null)
            {
                socket.Close();
            }
        }
        catch
        {
        }

        socket = null;
        reader = null;
        writer = null;
    }
}
