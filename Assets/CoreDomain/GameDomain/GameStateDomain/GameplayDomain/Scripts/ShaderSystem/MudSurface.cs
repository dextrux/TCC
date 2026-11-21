using UnityEngine;

// Vai em QUALQUER objeto que pode MOSTRAR lama: o cubo/pe, e o chao limpo que vai receber as pegadas.
// Guarda as marcas em espaco local, entao a lama acompanha o objeto quando ele se move.
public class MudSurface : MonoBehaviour
{
    public const int MaxContacts = 12; // tem que bater com MAX_CONTACTS do shader

    private Renderer[] renderers;
    private MaterialPropertyBlock block;

    private readonly Vector4[] points = new Vector4[MaxContacts]; // xyz = local (deste transform), w = tempo
    private readonly Vector4[] parms  = new Vector4[MaxContacts]; // x = forca
    private readonly Vector4[] pointsPerRenderer = new Vector4[MaxContacts];
    private int count;
    private float opacity = 1f;

    private static readonly int ContactPointsId = Shader.PropertyToID("_ContactPoints");
    private static readonly int ContactParamsId = Shader.PropertyToID("_ContactParams");
    private static readonly int ContactCountId  = Shader.PropertyToID("_ContactCount");
    private static readonly int MudOpacityId    = Shader.PropertyToID("_MudOpacity");

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        Apply();
    }

    public void AddMark(Vector3 worldPos, float strength)
    {
        // se ja estourou o maximo, sobrescreve a marca mais antiga
        if (count >= MaxContacts)
        {
            for (int i = 1; i < MaxContacts; i++)
            {
                points[i - 1] = points[i];
                parms[i - 1]  = parms[i];
            }
            count = MaxContacts - 1;
        }

        Vector3 local = transform.InverseTransformPoint(worldPos);
        // Time.timeSinceLevelLoad e o mesmo relogio do _Time.y do shader
        points[count] = new Vector4(local.x, local.y, local.z, Time.timeSinceLevelLoad);
        parms[count]  = new Vector4(Mathf.Clamp01(strength), 0f, 0f, 0f);
        count++;

        Apply();
    }

    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
        Apply();
    }

    void Apply()
    {
        foreach (var r in renderers)
        {
            // cada renderer filho tem seu proprio espaco local: converte os pontos pra ele
            for (int i = 0; i < MaxContacts; i++)
            {
                Vector3 world = transform.TransformPoint(points[i]);
                Vector3 local = r.transform.InverseTransformPoint(world);
                pointsPerRenderer[i] = new Vector4(local.x, local.y, local.z, points[i].w);
            }

            r.GetPropertyBlock(block);
            block.SetVectorArray(ContactPointsId, pointsPerRenderer);
            block.SetVectorArray(ContactParamsId, parms);
            block.SetFloat(ContactCountId, count);
            block.SetFloat(MudOpacityId, opacity);
            r.SetPropertyBlock(block);
        }
    }
}
