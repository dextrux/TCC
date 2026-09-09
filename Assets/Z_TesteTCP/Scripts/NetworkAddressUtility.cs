using System.Net;
using System.Net.Sockets;

public static class NetworkAddressUtility
{
    public static string GetLocalIPv4Address()
    {
        UdpClient udpClient = null;

        try
        {
            udpClient = new UdpClient();
            udpClient.Connect("8.8.8.8", 65530);

            IPEndPoint endpoint = udpClient.Client.LocalEndPoint as IPEndPoint;

            if (endpoint != null)
            {
                return endpoint.Address.ToString();
            }
        }
        catch
        {
        }
        finally
        {
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                {
                    return address.ToString();
                }
            }
        }
        catch
        {
        }

        return "Não encontrado";
    }

    public static bool IsNonPublicAddress(string ip)
    {
        IPAddress address;

        if (!IPAddress.TryParse(ip, out address))
        {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();

        if (bytes.Length != 4)
        {
            return false;
        }

        if (bytes[0] == 10)
        {
            return true;
        }

        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
        {
            return true;
        }

        if (bytes[0] == 192 && bytes[1] == 168)
        {
            return true;
        }

        if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
        {
            return true;
        }

        return false;
    }
}
