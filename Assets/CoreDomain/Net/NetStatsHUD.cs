using System.Text;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace ArenaFPS
{
    public class NetStatsHUD : MonoBehaviour
    {
        private struct NetworkCondition
        {
            public readonly string Name;
            public readonly int DelayMs;
            public readonly int JitterMs;
            public readonly int LossPercent;

            public NetworkCondition(string name, int delayMs, int jitterMs, int lossPercent)
            {
                Name = name;
                DelayMs = delayMs;
                JitterMs = jitterMs;
                LossPercent = lossPercent;
            }
        }

        private static readonly NetworkCondition[] Conditions =
        {
            new NetworkCondition("Perfeita (sem simulação)", 0, 0, 0),
            new NetworkCondition("Wi-Fi de casa", 30, 10, 1),
            new NetworkCondition("4G instável", 100, 30, 5),
            new NetworkCondition("Rede caótica", 200, 80, 15)
        };

        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private NetworkSimulator simulator;
        [SerializeField] private RuntimeNetStatsMonitor statsMonitor;

        private bool visible;
        private int conditionIndex;
        private bool appliedThisSession;
        private float listeningSince = -1f;
        private float refreshTimer;
        private string cachedText = "";
        private GUIStyle boxStyle;
        private readonly StringBuilder builder = new StringBuilder();

        public string CurrentConditionName => Conditions[conditionIndex].Name;

        private void Awake()
        {
            visible = visibleOnStart;
        }

        private void Start()
        {
            ApplyCondition();
            if (statsMonitor != null) statsMonitor.Visible = false;
        }

        private void Update()
        {
            if (GameInput.Pressed(KeyCode.F3)) visible = !visible;
            if (GameInput.Pressed(KeyCode.F4) && statsMonitor != null) statsMonitor.Visible = !statsMonitor.Visible;

            for (int i = 0; i < Conditions.Length; i++)
            {
                if (GameInput.Pressed(KeyCode.F5 + i))
                {
                    conditionIndex = i;
                    ApplyCondition();
                }
            }

            NetworkManager manager = NetworkManager.Singleton;
            bool listening = manager != null && manager.IsListening;
            if (!listening)
            {
                listeningSince = -1f;
                appliedThisSession = false;
            }
            else
            {
                if (listeningSince < 0f) listeningSince = Time.unscaledTime;
                if (!appliedThisSession && Time.unscaledTime - listeningSince > 0.5f)
                {
                    ApplyCondition();
                    appliedThisSession = true;
                }
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                cachedText = BuildText(manager);
            }
        }

        private void ApplyCondition()
        {
            if (simulator == null) return;
            NetworkCondition condition = Conditions[conditionIndex];
            simulator.ConnectionPreset = NetworkSimulatorPreset.Create(
                condition.Name,
                "Condição de rede simulada",
                condition.DelayMs,
                condition.JitterMs,
                0,
                condition.LossPercent);
        }

        private string BuildText(NetworkManager manager)
        {
            builder.Clear();
            builder.AppendLine("<b>REDE</b>   [F3] esconde");

            if (manager == null)
            {
                builder.AppendLine("NetworkManager ausente");
            }
            else if (!manager.IsListening)
            {
                builder.AppendLine("Desconectado");
            }
            else
            {
                string role = manager.IsHost
                    ? "HOST (servidor + jogador)"
                    : manager.IsServer
                        ? "SERVIDOR DEDICADO (sem jogador)"
                        : "CLIENTE";
                builder.AppendLine($"Papel: {role}");

                UnityTransport transport = manager.NetworkConfig.NetworkTransport as UnityTransport;
                if (transport != null)
                {
                    if (transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
                    {
                        builder.AppendLine("Caminho: via Relay (servidor da Unity no meio)");
                    }
                    else if (manager.IsServer)
                    {
                        string listen = string.IsNullOrEmpty(transport.ConnectionData.ServerListenAddress)
                            ? transport.ConnectionData.Address
                            : transport.ConnectionData.ServerListenAddress;
                        builder.AppendLine($"Caminho: IP direto, escutando em {listen}:{transport.ConnectionData.Port}");
                    }
                    else
                    {
                        builder.AppendLine($"Caminho: IP direto → {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
                    }

                    if (!manager.IsServer)
                    {
                        builder.AppendLine($"RTT até o host: <b>{transport.GetCurrentRtt(NetworkManager.ServerClientId)} ms</b>");
                    }
                    else
                    {
                        foreach (ulong clientId in manager.ConnectedClientsIds)
                        {
                            if (clientId == manager.LocalClientId) continue;
                            builder.AppendLine($"RTT até o jogador {clientId}: {transport.GetCurrentRtt(clientId)} ms");
                        }
                    }
                }

                builder.AppendLine($"Tick rate: {manager.NetworkConfig.TickRate} Hz");
            }

            NetworkCondition condition = Conditions[conditionIndex];
            builder.AppendLine($"Rede simulada [F5–F8]: <b>{condition.Name}</b>");
            if (condition.DelayMs > 0 || condition.LossPercent > 0)
            {
                builder.AppendLine($"  atraso {condition.DelayMs} ms ± {condition.JitterMs} ms · perda {condition.LossPercent}%");
                builder.AppendLine("  (vale na ida E na volta desta máquina)");
            }
            if (simulator == null) builder.AppendLine("  <color=#F8C233>Network Simulator ausente na cena</color>");

            builder.Append("[F4] monitor de banda   [Esc] sair da partida");
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
            float x = statsMonitor != null && statsMonitor.Visible ? 280f : 10f;
            GUI.Box(new Rect(x, 10f, width, height), cachedText, boxStyle);
        }
    }
}