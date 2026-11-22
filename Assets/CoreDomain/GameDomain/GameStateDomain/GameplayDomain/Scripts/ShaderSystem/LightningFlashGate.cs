using UnityEngine;

// Coloque no GameObject do relampago (o objeto com o material Custom/Lightning).
// - Enquanto houver relampago ativo na cena: _LightningActive = 1 e o timing do material
//   do relampago e copiado para o pos-processamento (flash sempre sincronizado).
// - Sem relampago (objeto desativado, renderer desligado, cena sem relampago): _LightningActive = 0, sem flash.
// Com varios relampagos ativos, o flash segue o primeiro que foi ativado.
public class LightningFlashGate : MonoBehaviour
{
    [Tooltip("Exige que o Renderer esteja ligado, nao so o GameObject ativo")]
    [SerializeField] private bool requireEnabledRenderer = true;

    [Tooltip("So conta se o relampago estiver aparecendo em alguma camera (fora da tela = sem flash)")]
    [SerializeField] private bool requireVisibleOnCamera = false;

    // globais lidas pelo Hidden/LightningFlashPost
    private static readonly int ActiveId         = Shader.PropertyToID("_LightningActive");
    private static readonly int GIntervalId      = Shader.PropertyToID("_LightningStrikeInterval");
    private static readonly int GRandomId        = Shader.PropertyToID("_LightningIntervalRandom");
    private static readonly int GDurationId      = Shader.PropertyToID("_LightningFlashDuration");
    private static readonly int GDoubleId        = Shader.PropertyToID("_LightningDoubleFlashChance");
    private static readonly int GSeedId          = Shader.PropertyToID("_LightningSeed");

    // propriedades do material Custom/Lightning
    private static readonly int MIntervalId      = Shader.PropertyToID("_StrikeInterval");
    private static readonly int MRandomId        = Shader.PropertyToID("_IntervalRandom");
    private static readonly int MDurationId      = Shader.PropertyToID("_FlashDuration");
    private static readonly int MDoubleId        = Shader.PropertyToID("_DoubleFlashChance");
    private static readonly int MSeedId          = Shader.PropertyToID("_Seed");

    private static int activeCount;
    private static LightningFlashGate leader;   // o relampago que comanda o timing do flash

    private Renderer rend;
    private bool counted;

    void Awake() => rend = GetComponentInChildren<Renderer>();

    void OnEnable()  => Refresh();
    void OnDisable() => SetCounted(false);

    void Update()
    {
        Refresh();
        if (leader == this) PushTiming(); // atualiza se voce mexer no material em Play
    }

    void Refresh()
    {
        bool on = isActiveAndEnabled && rend != null;
        if (on && requireEnabledRenderer) on = rend.enabled;
        if (on && requireVisibleOnCamera) on = rend.isVisible;
        SetCounted(on);
    }

    void SetCounted(bool on)
    {
        if (on == counted) return;
        counted = on;
        activeCount += on ? 1 : -1;

        if (on && leader == null) { leader = this; PushTiming(); }
        if (!on && leader == this) leader = FindNewLeader();

        Shader.SetGlobalFloat(ActiveId, activeCount > 0 ? 1f : 0f);
    }

    void PushTiming()
    {
        Material m = rend != null ? rend.sharedMaterial : null;
        if (m == null) return;

        Shader.SetGlobalFloat(GIntervalId, m.GetFloat(MIntervalId));
        Shader.SetGlobalFloat(GRandomId,   m.GetFloat(MRandomId));
        Shader.SetGlobalFloat(GDurationId, m.GetFloat(MDurationId));
        Shader.SetGlobalFloat(GDoubleId,   m.GetFloat(MDoubleId));
        Shader.SetGlobalFloat(GSeedId,     m.GetFloat(MSeedId));
    }

    static LightningFlashGate FindNewLeader()
    {
        foreach (var g in FindObjectsByType<LightningFlashGate>(FindObjectsSortMode.None))
        {
            if (g.counted)
            {
                g.PushTiming();
                return g;
            }
        }
        return null;
    }

    // comeca desligado ao entrar em Play / trocar de cena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        activeCount = 0;
        leader = null;
        Shader.SetGlobalFloat(ActiveId, 0f);
    }
}
