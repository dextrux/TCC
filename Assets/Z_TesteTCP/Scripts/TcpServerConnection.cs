using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetworkTcpClient = System.Net.Sockets.TcpClient;

public class TcpServerConnection
{
    public int Id { get; private set; }

    public bool IsConnected
    {
        get
        {
            return connected;
        }
    }

    public string RemoteEndpoint { get; private set; }

    private readonly NetworkTcpClient socket;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;
    private readonly object writeLock = new object();

    private Thread readThread;
    private volatile bool connected = true;

    private Action<int, string> messageCallback;
    private Action<int> disconnectedCallback;

    private int disconnectedNotified;

    public TcpServerConnection(int id, NetworkTcpClient socket)
    {
        Id = id;
        this.socket = socket;

        this.socket.NoDelay = true;

        RemoteEndpoint = socket.Client.RemoteEndPoint != null ? socket.Client.RemoteEndPoint.ToString() : "Unknown";

        NetworkStream stream = socket.GetStream();
        reader = new StreamReader(stream, new UTF8Encoding(false));
        writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.AutoFlush = true;
    }

    public void Start(Action<int, string> onMessage, Action<int> onDisconnected)
    {
        if (readThread != null)
        {
            return;
        }

        messageCallback = onMessage;
        disconnectedCallback = onDisconnected;

        readThread = new Thread(ReadLoop);
        readThread.IsBackground = true;
        readThread.Start();
    }

    public bool Send(string message)
    {
        if (!connected)
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
        catch
        {
            Close(true);
            return false;
        }
    }

    public void Close(bool notifyDisconnected)
    {
        if (!notifyDisconnected)
        {
            Interlocked.Exchange(ref disconnectedNotified, 1);
        }

        connected = false;

        try
        {
            socket.Close();
        }
        catch
        {
        }

        if (notifyDisconnected)
        {
            NotifyDisconnected();
        }
    }

    private void ReadLoop()
    {
        try
        {
            string line;

            while (connected && (line = reader.ReadLine()) != null)
            {
                if (messageCallback != null)
                {
                    messageCallback(Id, line);
                }
            }
        }
        catch
        {
        }
        finally
        {
            connected = false;

            try
            {
                socket.Close();
            }
            catch
            {
            }

            NotifyDisconnected();
        }
    }

    private void NotifyDisconnected()
    {
        if (Interlocked.Exchange(ref disconnectedNotified, 1) != 0)
        {
            return;
        }

        if (disconnectedCallback != null)
        {
            disconnectedCallback(Id);
        }
    }
}
