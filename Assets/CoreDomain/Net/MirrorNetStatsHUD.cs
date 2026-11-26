using System.Text;
using Mirror;
using UnityEngine;

public class MirrorNetStatsHUD : MonoBehaviour
{
    [SerializeField] private bool visibleOnStart = true;

    private bool visible;
    private float refreshTimer;
    private string cachedText = "";
    private GUIStyle boxStyle;
    private readonly StringBuilder builder = new StringBuilder();

    private void Awake()
    {
        visible = visibleOnStart;
    }

    private void Update()
    {
        if (PressedF3())
        {
            visible = !visible;
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            cachedText = BuildText();
        }
    }

    private static bool PressedF3()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.f3Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F3);
#endif
    }

    private string BuildText()
    {
        builder.Clear();
        builder.AppendLine("<b>REDE</b>   [F3] esconde");

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
            builder.AppendLine($"RTT até o host: <b>{rttMs:0} ms</b>");
        }
        else if (NetworkClient.active)
        {
            builder.AppendLine("Papel: Conectando...");
        }
        else
        {
            builder.AppendLine("Desconectado");
        }

        builder.AppendLine($"Send rate: {NetworkServer.sendRate} Hz");
        builder.AppendLine($"Endereço: {NetworkManager.singleton?.networkAddress ?? "-"}");

        if (Transport.active != null)
        {
            builder.Append($"Transporte ativo: {Transport.active.GetType().Name}");
        }

        return builder.ToString();
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                richText = true,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8)
            };
        }

        const float width = 360f;
        float height = boxStyle.CalcHeight(new GUIContent(cachedText), width);
        GUI.Box(new Rect(10f, 10f, width, height), cachedText, boxStyle);
    }
}