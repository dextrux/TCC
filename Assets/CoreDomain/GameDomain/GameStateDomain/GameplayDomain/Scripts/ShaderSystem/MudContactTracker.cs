using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MudContactTracker : MonoBehaviour
{
    [SerializeField] private string mudTag = "Mud";
    [SerializeField] private int maxContacts = 12;
    [SerializeField] private ParticleSystem mudSplashEffect;

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Vector4[] contactPoints;
    private int contactCount;

    private static readonly int ContactPointsId = Shader.PropertyToID("_ContactPoints");
    private static readonly int ContactCountId  = Shader.PropertyToID("_ContactCount");

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        contactPoints = new Vector4[maxContacts];
        contactCount = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(mudTag)) return;
        AddContact(collision.GetContact(0).point);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(mudTag)) return;
        AddContact(transform.position);
    }

    void AddContact(Vector3 worldPos)
    {
        // se ja estourou o maximo, sobrescreve a marca mais antiga
        if (contactCount >= maxContacts)
        {
            for (int i = 1; i < maxContacts; i++)
                contactPoints[i - 1] = contactPoints[i];
            contactCount = maxContacts - 1;
        }

        contactPoints[contactCount] = new Vector4(worldPos.x, worldPos.y, worldPos.z, Time.time);
        contactCount++;

        ApplyToRenderers();

        // dispara o splash sempre, em todo contato -- nao so quando estoura o maximo
        if (mudSplashEffect != null)
        {
            mudSplashEffect.transform.position = worldPos;
            mudSplashEffect.Play();
        }
    }

    void ApplyToRenderers()
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetVectorArray(ContactPointsId, contactPoints);
            block.SetInt(ContactCountId, contactCount);
            r.SetPropertyBlock(block);
        }
    }
}