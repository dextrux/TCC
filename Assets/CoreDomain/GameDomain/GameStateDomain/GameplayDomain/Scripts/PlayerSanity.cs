using UnityEngine;

public class PlayerSanity : MonoBehaviour
{
    [Header("Configurações de Sanidade")]
    public float maxSanity = 100f;
    public float currentSanity;

    [Header("Estado do Player")]
    public bool isCrazy = false;

    [Header("Configurações de Luz")]
    public float decreaseRate = 5f;

    private SanityLight currentLight;

    void Start()
    {
        currentSanity = maxSanity;
    }

    void Update()
    {
        FindClosestLight();

        if (currentLight != null)
        {
            float regeneration = currentLight.GetRegenerationRate(transform.position);

            ChangeSanity(regeneration * Time.deltaTime);
        }
        else
        {
            ChangeSanity(-decreaseRate * Time.deltaTime);
        }

        CheckSanityState();
    }

    private void FindClosestLight()
    {
        SanityLight[] lights = FindObjectsByType<SanityLight>(FindObjectsSortMode.None);

        SanityLight closestLight = null;
        float closestDistance = Mathf.Infinity;

        foreach (SanityLight light in lights)
        {
            float distance = Vector3.Distance(
                transform.position,
                light.transform.position
            );

            if (distance <= light.outerRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestLight = light;
            }
        }

        currentLight = closestLight;
    }

    public void ChangeSanity(float amount)
    {
        currentSanity += amount;

        currentSanity = Mathf.Clamp(
            currentSanity,
            0f,
            maxSanity
        );
    }

    private void CheckSanityState()
    {
        
        if (currentSanity <= 0f && !isCrazy)
        {
            isCrazy = true;
            HandleZeroSanity();
        }

        
        if (currentSanity > 0f && isCrazy)
        {
            isCrazy = false;
        }
    }

    private void HandleZeroSanity()
    {
        Debug.Log("O jogador enlouqueceu!");
    }
}