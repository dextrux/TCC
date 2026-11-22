using System.Collections.Generic;
using UnityEngine;

// Mancha de lama colada numa superficie (criada pela MudBall quando ela bate).
// Aparece com um "pop", fica um tempo, depois seca/encolhe e some.
public class MudSplat : MonoBehaviour
{
    [Tooltip("Maximo de manchas na cena ao mesmo tempo (as mais antigas somem)")]
    public static int MaxSplats = 40;

    private static readonly Queue<MudSplat> alive = new Queue<MudSplat>();
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static Mesh quadMesh;

    private const float PopDuration = 0.08f;

    private Renderer rend;
    private MaterialPropertyBlock block;
    private Vector3 baseScale;
    private float spawnTime;
    private float lifetime;
    private float fadeDuration;

    public static MudSplat Spawn(Vector3 point, Vector3 normal, Vector3 travelDir, float size, float stretch,
                                 Material material, float lifetime, float fadeDuration, Transform parent)
    {
        var go = new GameObject("MudSplat");
        go.AddComponent<MeshFilter>().sharedMesh = QuadMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // eixo Y do quad = direcao em que a bola estava indo, projetada na superficie
        Vector3 along = Vector3.ProjectOnPlane(travelDir, normal);
        if (along.sqrMagnitude < 1e-4f) along = Vector3.ProjectOnPlane(Random.onUnitSphere, normal);
        if (along.sqrMagnitude < 1e-4f) along = Vector3.Cross(normal, Vector3.right);

        go.transform.SetPositionAndRotation(point + normal * 0.01f, Quaternion.LookRotation(-normal, along.normalized));
        go.transform.localScale = new Vector3(size, size * (1f + stretch), 1f);
        if (parent != null) go.transform.SetParent(parent, true);

        var splat = go.AddComponent<MudSplat>();
        splat.Init(lifetime, fadeDuration);
        return splat;
    }

    void Init(float life, float fade)
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        baseScale = transform.localScale;
        spawnTime = Time.time;
        lifetime = Mathf.Max(0.1f, life);
        fadeDuration = Mathf.Clamp(fade, 0.01f, lifetime);

        rend.GetPropertyBlock(block);
        block.SetFloat(SeedId, Random.Range(0f, 100f));
        block.SetFloat(FadeId, 1f);
        rend.SetPropertyBlock(block);

        transform.localScale = baseScale * 0.5f;

        alive.Enqueue(this);
        while (alive.Count > MaxSplats)
        {
            MudSplat old = alive.Dequeue();
            if (old != null) Destroy(old.gameObject);
        }
    }

    void Update()
    {
        float age = Time.time - spawnTime;

        // "pop" do impacto
        if (age < PopDuration)
        {
            float t = age / PopDuration;
            transform.localScale = baseScale * Mathf.Lerp(0.5f, 1f, 1f - (1f - t) * (1f - t));
        }
        else if (transform.localScale != baseScale)
        {
            transform.localScale = baseScale;
        }

        // secando e sumindo
        float fadeStart = lifetime - fadeDuration;
        if (age > fadeStart)
        {
            float fade = 1f - (age - fadeStart) / fadeDuration;
            if (fade <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            rend.GetPropertyBlock(block);
            block.SetFloat(FadeId, fade);
            rend.SetPropertyBlock(block);
        }
    }

    static Mesh QuadMesh
    {
        get
        {
            if (quadMesh != null) return quadMesh;
            quadMesh = new Mesh { name = "MudSplatQuad" };
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f), new Vector3(0.5f,  0.5f, 0f)
            };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quadMesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            quadMesh.triangles = new[] { 0, 3, 1, 3, 0, 2 };
            quadMesh.RecalculateBounds();
            return quadMesh;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() => alive.Clear();
}
