using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using UnityEngine;

public class NatTraversal : MonoBehaviour
{
    public bool IsBusy
    {
        get
        {
            lock (stateLock)
            {
                return isBusy;
            }
        }
    }

    public bool PortMapped
    {
        get
        {
            lock (stateLock)
            {
                return portMapped;
            }
        }
    }

    public string StatusMessage
    {
        get
        {
            lock (stateLock)
            {
                return statusMessage;
            }
        }
    }

    public string LocalIp
    {
        get
        {
            lock (stateLock)
            {
                return localIp;
            }
        }
    }

    public string RouterExternalIp
    {
        get
        {
            lock (stateLock)
            {
                return routerExternalIp;
            }
        }
    }

    public int MappedPort
    {
        get
        {
            lock (stateLock)
            {
                return mappedPort;
            }
        }
    }

    private readonly object stateLock = new object();

    private bool isBusy;
    private bool portMapped;
    private string statusMessage = "UPnP ainda não iniciado.";
    private string localIp = "";
    private string routerExternalIp = "";
    private int mappedPort = -1;

    private string serviceType = "";
    private string controlUrl = "";

    private Thread workerThread;

    public void OpenPort(int port)
    {
        if (port < 1 || port > 65535)
        {
            SetStatus(false, false, "Porta inválida.", "", "", -1);
            return;
        }

        lock (stateLock)
        {
            if (isBusy)
            {
                return;
            }

            if (portMapped && mappedPort == port)
            {
                return;
            }

            isBusy = true;
            portMapped = false;
            statusMessage = "Procurando roteador com UPnP...";
            mappedPort = port;
        }

        workerThread = new Thread(() => OpenPortWorker(port));
        workerThread.IsBackground = true;
        workerThread.Start();
    }

    private void OpenPortWorker(int port)
    {
        string detectedLocalIp = GetLocalIPv4Address();

        if (string.IsNullOrEmpty(detectedLocalIp))
        {
            SetStatus(false, false, "Não foi possível descobrir o IP local.", "", "", port);
            return;
        }

        lock (stateLock)
        {
            localIp = detectedLocalIp;
            statusMessage = "Procurando roteador com UPnP...";
        }

        string discoveredServiceType;
        string discoveredControlUrl;

        if (!DiscoverGateway(detectedLocalIp, out discoveredServiceType, out discoveredControlUrl))
        {
            SetStatus(false, false, "UPnP não encontrado no roteador.", detectedLocalIp, "", port);
            return;
        }

        serviceType = discoveredServiceType;
        controlUrl = discoveredControlUrl;

        string externalIp = GetExternalIpFromRouter();

        lock (stateLock)
        {
            routerExternalIp = externalIp;
            statusMessage = "Roteador encontrado. Abrindo porta TCP " + port + "...";
        }

        string error;

        if (!AddPortMapping(port, detectedLocalIp, 0, out error))
        {
            if (!AddPortMapping(port, detectedLocalIp, 3600, out error))
            {
                SetStatus(false, false, "Não foi possível abrir a porta via UPnP. " + error, detectedLocalIp, externalIp, port);
                return;
            }
        }

        SetStatus(false, true, "Porta TCP " + port + " aberta automaticamente via UPnP.", detectedLocalIp, externalIp, port);
    }

    public void ClosePortMapping()
    {
        int port;
        bool shouldClose;

        lock (stateLock)
        {
            shouldClose = portMapped && !string.IsNullOrEmpty(controlUrl) && !string.IsNullOrEmpty(serviceType);
            port = mappedPort;
        }

        if (!shouldClose)
        {
            return;
        }

        Thread closeThread = new Thread(() => ClosePortWorker(port));
        closeThread.IsBackground = true;
        closeThread.Start();
    }

    private void ClosePortWorker(int port)
    {
        DeletePortMapping(port);

        lock (stateLock)
        {
            portMapped = false;
            mappedPort = -1;
            statusMessage = "Mapeamento UPnP removido.";
        }
    }

    private bool DiscoverGateway(string interfaceIp, out string discoveredServiceType, out string discoveredControlUrl)
    {
        discoveredServiceType = "";
        discoveredControlUrl = "";

        List<string> locations = DiscoverDeviceLocations(interfaceIp);

        foreach (string location in locations)
        {
            if (TryGetGatewayService(location, out discoveredServiceType, out discoveredControlUrl))
            {
                return true;
            }
        }

        return false;
    }

    private List<string> DiscoverDeviceLocations(string interfaceIp)
    {
        List<string> locations = new List<string>();
        HashSet<string> uniqueLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        UdpClient udpClient = null;

        try
        {
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse(interfaceIp), 0);
            udpClient = new UdpClient(localEndpoint);
            udpClient.Client.ReceiveTimeout = 500;

            IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

            string[] searchTargets =
            {
                "urn:schemas-upnp-org:device:InternetGatewayDevice:1",
                "urn:schemas-upnp-org:device:InternetGatewayDevice:2",
                "ssdp:all"
            };

            foreach (string searchTarget in searchTargets)
            {
                string request = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: " + searchTarget + "\r\n\r\n";
                byte[] requestBytes = Encoding.ASCII.GetBytes(request);

                udpClient.Send(requestBytes, requestBytes.Length, multicastEndpoint);
                udpClient.Send(requestBytes, requestBytes.Length, multicastEndpoint);
            }

            DateTime endTime = DateTime.UtcNow.AddSeconds(4);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    IPEndPoint remoteEndpoint = null;
                    byte[] responseBytes = udpClient.Receive(ref remoteEndpoint);
                    string response = Encoding.UTF8.GetString(responseBytes);

                    string location = GetHeaderValue(response, "LOCATION");

                    if (!string.IsNullOrEmpty(location) && uniqueLocations.Add(location))
                    {
                        locations.Add(location);
                    }
                }
                catch (SocketException)
                {
                }
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

        return locations;
    }

    private string GetHeaderValue(string response, string headerName)
    {
        string[] lines = response.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            int separatorIndex = line.IndexOf(':');

            if (separatorIndex <= 0)
            {
                continue;
            }

            string name = line.Substring(0, separatorIndex).Trim();

            if (!string.Equals(name, headerName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return line.Substring(separatorIndex + 1).Trim();
        }

        return "";
    }

    private bool TryGetGatewayService(string descriptionUrl, out string discoveredServiceType, out string discoveredControlUrl)
    {
        discoveredServiceType = "";
        discoveredControlUrl = "";

        try
        {
            string xml = DownloadString(descriptionUrl);

            if (string.IsNullOrEmpty(xml))
            {
                return false;
            }

            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);

            XmlNodeList serviceNodes = document.SelectNodes("//*[local-name()='service']");

            string fallbackServiceType = "";
            string fallbackControlUrl = "";

            foreach (XmlNode serviceNode in serviceNodes)
            {
                string currentServiceType = GetChildNodeValue(serviceNode, "serviceType");
                string currentControlUrl = GetChildNodeValue(serviceNode, "controlURL");

                if (string.IsNullOrEmpty(currentServiceType) || string.IsNullOrEmpty(currentControlUrl))
                {
                    continue;
                }

                if (currentServiceType.IndexOf("WANIPConnection", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    discoveredServiceType = currentServiceType;
                    discoveredControlUrl = new Uri(new Uri(descriptionUrl), currentControlUrl).AbsoluteUri;
                    return true;
                }

                if (currentServiceType.IndexOf("WANPPPConnection", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fallbackServiceType = currentServiceType;
                    fallbackControlUrl = new Uri(new Uri(descriptionUrl), currentControlUrl).AbsoluteUri;
                }
            }

            if (!string.IsNullOrEmpty(fallbackServiceType))
            {
                discoveredServiceType = fallbackServiceType;
                discoveredControlUrl = fallbackControlUrl;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private string GetChildNodeValue(XmlNode parentNode, string childName)
    {
        XmlNode childNode = parentNode.SelectSingleNode("*[local-name()='" + childName + "']");

        if (childNode == null)
        {
            return "";
        }

        return childNode.InnerText.Trim();
    }

    private string DownloadString(string url)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Timeout = 4000;
        request.ReadWriteTimeout = 4000;

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    private string GetExternalIpFromRouter()
    {
        string response;
        string error;

        if (!SendSoapAction("GetExternalIPAddress", "", out response, out error))
        {
            return "";
        }

        try
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(response);

            XmlNode ipNode = document.SelectSingleNode("//*[local-name()='NewExternalIPAddress']");

            if (ipNode != null)
            {
                return ipNode.InnerText.Trim();
            }
        }
        catch
        {
        }

        return "";
    }

    private bool AddPortMapping(int port, string internalIp, int leaseDuration, out string error)
    {
        string arguments = "<NewRemoteHost></NewRemoteHost><NewExternalPort>" + port + "</NewExternalPort><NewProtocol>TCP</NewProtocol><NewInternalPort>" + port + "</NewInternalPort><NewInternalClient>" + EscapeXml(internalIp) + "</NewInternalClient><NewEnabled>1</NewEnabled><NewPortMappingDescription>Unity FPS TCP</NewPortMappingDescription><NewLeaseDuration>" + leaseDuration + "</NewLeaseDuration>";

        string response;
        return SendSoapAction("AddPortMapping", arguments, out response, out error);
    }

    private bool DeletePortMapping(int port)
    {
        string arguments = "<NewRemoteHost></NewRemoteHost><NewExternalPort>" + port + "</NewExternalPort><NewProtocol>TCP</NewProtocol>";

        string response;
        string error;

        return SendSoapAction("DeletePortMapping", arguments, out response, out error);
    }

    private bool SendSoapAction(string action, string arguments, out string responseText, out string error)
    {
        responseText = "";
        error = "";

        if (string.IsNullOrEmpty(controlUrl) || string.IsNullOrEmpty(serviceType))
        {
            error = "Serviço UPnP não configurado.";
            return false;
        }

        string soapBody = "<?xml version=\"1.0\" encoding=\"utf-8\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:" + action + " xmlns:u=\"" + EscapeXml(serviceType) + "\">" + arguments + "</u:" + action + "></s:Body></s:Envelope>";

        byte[] bodyBytes = Encoding.UTF8.GetBytes(soapBody);

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(controlUrl);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=\"utf-8\"";
            request.Headers.Add("SOAPACTION", "\"" + serviceType + "#" + action + "\"");
            request.ContentLength = bodyBytes.Length;
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                responseText = reader.ReadToEnd();
            }

            return true;
        }
        catch (WebException exception)
        {
            error = ReadWebException(exception);
            return false;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private string ReadWebException(WebException exception)
    {
        if (exception.Response == null)
        {
            return exception.Message;
        }

        try
        {
            using (Stream stream = exception.Response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string body = reader.ReadToEnd();

                XmlDocument document = new XmlDocument();
                document.LoadXml(body);

                XmlNode errorCodeNode = document.SelectSingleNode("//*[local-name()='errorCode']");
                XmlNode errorDescriptionNode = document.SelectSingleNode("//*[local-name()='errorDescription']");

                string errorCode = errorCodeNode != null ? errorCodeNode.InnerText : "";
                string errorDescription = errorDescriptionNode != null ? errorDescriptionNode.InnerText : exception.Message;

                if (!string.IsNullOrEmpty(errorCode))
                {
                    return "UPnP " + errorCode + ": " + errorDescription;
                }

                return errorDescription;
            }
        }
        catch
        {
            return exception.Message;
        }
    }

    private string EscapeXml(string value)
    {
        string escapedValue = SecurityElement.Escape(value);
        return escapedValue ?? "";
    }

    private string GetLocalIPv4Address()
    {
        UdpClient udpClient = null;

        try
        {
            udpClient = new UdpClient();
            udpClient.Connect("8.8.8.8", 65530);

            IPEndPoint localEndpoint = udpClient.Client.LocalEndPoint as IPEndPoint;

            if (localEndpoint != null)
            {
                return localEndpoint.Address.ToString();
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

        return "";
    }

    public static bool IsNonPublicAddress(string ipText)
    {
        IPAddress address;

        if (!IPAddress.TryParse(ipText, out address))
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

    private void SetStatus(bool busy, bool mapped, string message, string detectedLocalIp, string detectedExternalIp, int port)
    {
        lock (stateLock)
        {
            isBusy = busy;
            portMapped = mapped;
            statusMessage = message;
            localIp = detectedLocalIp;
            routerExternalIp = detectedExternalIp;
            mappedPort = port;
        }
    }

    void OnApplicationQuit()
    {
        if (PortMapped)
        {
            DeletePortMapping(MappedPort);
        }
    }
}