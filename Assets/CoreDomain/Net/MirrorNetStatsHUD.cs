using System.Text;
using kcp2k;
using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MirrorNetStatsHUD : MonoBehaviour
{
    private struct InternetProfile
    {
        public readonly string Name;
        public readonly uint KcpIntervalMs;
        public readonly int SendRateHz;

        public InternetProfile(string name, uint kcpIntervalMs, int sendRateHz)
        {
            Name = name;
            KcpIntervalMs = kcpIntervalMs;
            SendRateHz = sendRateHz;
        }
    }

    private static readonly InternetProfile[] Profiles =
    {
        new InternetProfile("Fibra / Rede local (F5)", 10, 60),
        new InternetProfile("Wi-Fi mediano (F6)", 35, 30),
        new InternetProfile("4G lento / instavel (F7)", 90, 15)
    };

    [SerializeField] private bool visibleOnStart = true;
    [SerializeField] [Range(0.7f, 2.0f)] private float uiScale = 1.0f;
    [SerializeField] private float scaleStep = 0.15f;

    private bool visible;
    private int profileIndex;
    private int simulatedSendRateHz = 60;
    private float refreshTimer;
    private string cachedText = "";
    private GUIStyle boxStyle;
    private int lastBuiltFontSize = -1;
    private readonly StringBuilder builder = new StringBuilder();

    private void Awake()
    {
        visible = visibleOnStart;
        if (NetworkManager.singleton != null)
        {
            simulatedSendRateHz = NetworkManager.singleton.sendRate;
        }
    }

    private void Update()
    {
        if (PressedKey(KeyCode.F3))
        {
            visible = !visible;
        }

        if (PressedKey(KeyCode.F5)) ApplyProfile(0);
        else if (PressedKey(KeyCode.F6)) ApplyProfile(1);
        else if (PressedKey(KeyCode.F7)) ApplyProfile(2);

        if (PressedDecreaseSize())
        {
            uiScale = Mathf.Clamp(uiScale - scaleStep, 0.7f, 2.0f);
        }
        else if (PressedIncreaseSize())
        {
            uiScale = Mathf.Clamp(uiScale + scaleStep, 0.7f, 2.0f);
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            cachedText = BuildText();
        }
    }

    private void ApplyProfile(int index)
    {
        if (index < 0 || index >= Profiles.Length) return;
        profileIndex = index;

        InternetProfile profile = Profiles[profileIndex];
        simulatedSendRateHz = profile.SendRateHz;

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.sendRate = profile.SendRateHz;
        }

        KcpTransport kcp = FindKcpTransport();
        if (kcp != null)
        {
            kcp.Interval = profile.KcpIntervalMs;
        }
    }

    private static KcpTransport FindKcpTransport()
    {
        if (Transport.active is KcpTransport directKcp)
        {
            return directKcp;
        }

        if (Transport.active is MultiplexTransport multiplex && multiplex.transports != null)
        {
            foreach (Transport t in multiplex.transports)
            {
                if (t is KcpTransport kcp)
                {
                    return kcp;
                }
            }
        }

        return FindAnyObjectByType<KcpTransport>();
    }

    private static bool PressedDecreaseSize()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        return kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
#endif
    }

    private static bool PressedIncreaseSize()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        return kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus);
#endif
    }

    private static bool PressedKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        switch (key)
        {
            case KeyCode.F3: return kb.f3Key.wasPressedThisFrame;
            case KeyCode.F5: return kb.f5Key.wasPressedThisFrame;
            case KeyCode.F6: return kb.f6Key.wasPressedThisFrame;
            case KeyCode.F7: return kb.f7Key.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }

    private string BuildText()
    {
        builder.Clear();
        builder.AppendLine("<b>REDE</b>   [F3] esconde   [-/+] tamanho");

        if (NetworkClient.active && NetworkServer.active)
        {
            builder.AppendLine("Papel: HOST (servidor + jogador)");
        }
        else if (NetworkServer.active)
        {
            builder.AppendLine("Papel: SERVIDOR DEDICADO");
        }
        else if (NetworkClient.isConnected)
        {
            builder.AppendLine("Papel: CLIENTE");
            double rttMs = NetworkTime.rtt * 1000.0;
            builder.AppendLine($"RTT ate o host: <b>{rttMs:0} ms</b>");
        }
        else if (NetworkClient.active)
        {
            builder.AppendLine("Papel: Conectando...");
        }
        else
        {
            builder.AppendLine("Desconectado");
        }

        InternetProfile current = Profiles[profileIndex];
        KcpTransport kcp = FindKcpTransport();
        uint activeInterval = kcp != null ? kcp.Interval : current.KcpIntervalMs;
        int activeSendRate = NetworkManager.singleton != null ? NetworkManager.singleton.sendRate : simulatedSendRateHz;

        builder.AppendLine($"Simulacao [F5-F7]: <b>{current.Name}</b>");
        builder.AppendLine($"Send rate: {activeSendRate} Hz  |  KCP Interval: {activeInterval} ms");

        if (Transport.active != null)
        {
            builder.Append($"Transporte: {Transport.active.GetType().Name}");
        }

        return builder.ToString();
    }

    private void OnGUI()
    {
        if (!visible) return;

        int fontSize = Mathf.Max(10, Mathf.RoundToInt(13f * uiScale));
        int padX = Mathf.RoundToInt(10f * uiScale);
        int padY = Mathf.RoundToInt(8f * uiScale);

        if (boxStyle == null || lastBuiltFontSize != fontSize)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = fontSize,
                richText = true,
                wordWrap = true,
                padding = new RectOffset(padX, padX, padY, padY)
            };
            lastBuiltFontSize = fontSize;
        }

        float width = 370f * uiScale;
        float height = boxStyle.CalcHeight(new GUIContent(cachedText), width);
        GUI.Box(new Rect(10f, 10f, width, height), cachedText, boxStyle);
    }
}