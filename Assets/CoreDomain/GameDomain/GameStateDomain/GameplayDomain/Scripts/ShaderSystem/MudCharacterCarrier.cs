using UnityEngine;

// Versao do MudCarrier para personagem com CharacterController.
// CharacterController nao dispara OnCollisionEnter, entao o "passo" vem de:
//  - Distance: a cada X metros andando no chao (+ ao aterrissar de um pulo), ou
//  - AnimationEvents: a animacao chama Footstep(0) / Footstep(1) no frame em que o pe toca o chao.
// Em cada passo, um raycast embaixo do pe descobre onde pisou:
//  - tag "Mud"  -> o personagem suja e passa a carregar lama
//  - MudSurface -> deixa uma pegada, cada vez mais fraca, por N passos
[RequireComponent(typeof(CharacterController))]
public class MudCharacterCarrier : MonoBehaviour
{
    public enum StepMode { Distance, AnimationEvents }

    [SerializeField] private string mudTag = "Mud";
    [SerializeField] private StepMode stepMode = StepMode.Distance;

    [Tooltip("Modo Distance: metros andados entre um passo e outro")]
    [SerializeField] private float stepLength = 0.7f;

    [Tooltip("Quantos passos em chao limpo ate a lama acabar")]
    [SerializeField] private int stepsUntilClean = 4;

    [SerializeField] private bool cleanSelfWhileWalking = true;

    [Header("Pes (opcional)")]
    [Tooltip("Se vazio, usa a base da capsula, deslocada pro lado")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [Tooltip("Distancia lateral de cada pe ao centro quando nao ha Transform de pe")]
    [SerializeField] private float footSpacing = 0.15f;

    [Header("Referencias")]
    [Tooltip("MudSurface do proprio personagem. Se vazio, procura neste objeto/filhos")]
    [SerializeField] private MudSurface selfSurface;
    [SerializeField] private ParticleSystem mudSplashEffect;
    [SerializeField] private LayerMask groundLayers = ~0;

    private CharacterController cc;
    private float mudLoad;              // 0 = limpo, 1 = cheio de lama
    private float distanceSinceStep;
    private Vector3 lastPosition;
    private bool wasGrounded;
    private int nextFoot;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (selfSurface == null) selfSurface = GetComponentInChildren<MudSurface>();
        lastPosition = transform.position;
    }

    // LateUpdate: roda depois do script de movimento (que usa Update)
    void LateUpdate()
    {
        bool grounded = cc.isGrounded;

        if (stepMode == StepMode.Distance)
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;

            if (grounded && !wasGrounded)
            {
                // aterrissou de um pulo/queda = conta como passo
                distanceSinceStep = 0f;
                StepWithNextFoot();
            }
            else if (grounded)
            {
                distanceSinceStep += delta.magnitude;
                if (distanceSinceStep >= stepLength)
                {
                    distanceSinceStep = 0f;
                    StepWithNextFoot();
                }
            }
        }

        lastPosition = transform.position;
        wasGrounded = grounded;
    }

    // Animation Event: parametro Int 0 = pe esquerdo, 1 = pe direito
    public void Footstep(int foot)
    {
        if (stepMode == StepMode.AnimationEvents) DoStep(foot);
    }

    void StepWithNextFoot()
    {
        DoStep(nextFoot);
        nextFoot = 1 - nextFoot;
    }

    void DoStep(int foot)
    {
        Vector3 footPos = GetFootPosition(foot);
        if (!FindGround(footPos, out RaycastHit hit)) return;

        if (hit.collider.CompareTag(mudTag))
        {
            mudLoad = 1f;
            if (selfSurface != null)
            {
                selfSurface.SetOpacity(1f);
                selfSurface.AddMark(footPos, 1f);
            }
            if (mudSplashEffect != null)
            {
                mudSplashEffect.transform.position = hit.point;
                mudSplashEffect.Play();
            }
            return;
        }

        if (mudLoad <= 0f) return;

        var surface = hit.collider.GetComponentInParent<MudSurface>();
        if (surface != null && surface != selfSurface)
            surface.AddMark(hit.point, mudLoad);

        mudLoad = Mathf.Max(0f, mudLoad - 1f / Mathf.Max(1, stepsUntilClean));
        if (cleanSelfWhileWalking && selfSurface != null) selfSurface.SetOpacity(mudLoad);
    }

    Vector3 GetFootPosition(int foot)
    {
        Transform t = foot == 0 ? leftFoot : rightFoot;
        if (t != null) return t.position;

        // sem pes: base da capsula, um pouco pra esquerda/direita
        Vector3 bottom = transform.TransformPoint(cc.center)
                       + Vector3.down * (cc.height * 0.5f * transform.lossyScale.y);
        float side = foot == 0 ? -footSpacing : footSpacing;
        return bottom + transform.right * side;
    }

    bool FindGround(Vector3 footPos, out RaycastHit best)
    {
        best = default;
        Vector3 origin = footPos + Vector3.up * 0.5f;
        int n = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 1.0f, groundLayers, QueryTriggerInteraction.Collide);

        float bestDist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            // ignora o proprio personagem
            if (hits[i].collider.transform.IsChildOf(transform)) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                best = hits[i];
                found = true;
            }
        }
        return found;
    }
}
