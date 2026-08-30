using UnityEngine;

public class PlayerSanity : MonoBehaviour
{
    [Header("Configurações de Sanidade")]
    public float maxSanity = 100f;
    public float currentSanity;

    void Start()
    {
        currentSanity = maxSanity;
    }

    public void ChangeSanity(float amount)
    {
        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        if (currentSanity <= 0f)
        {
            HandleZeroSanity();
        }
    }

    private void HandleZeroSanity()
    {
        Debug.Log("O jogador enlouqueceu!");
    }
}