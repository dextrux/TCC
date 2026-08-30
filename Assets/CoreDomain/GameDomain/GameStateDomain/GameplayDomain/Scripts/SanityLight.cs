using UnityEngine;

public class SanityLight : MonoBehaviour
{
    [Header("Configurações")]
    public float decreaseRate = 5f;
    public float increaseRate = 10f;

    private bool isInSpotlight = false;
    private PlayerSanity playerSanity;

    void Start()
    {
        playerSanity = GetComponent<PlayerSanity>();
    }

    void Update()
    {
        if (isInSpotlight)
        {
            playerSanity.ChangeSanity(increaseRate * Time.deltaTime);
        }
        else
        {
            playerSanity.ChangeSanity(-decreaseRate * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spotlight"))
        {
            isInSpotlight = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Spotlight"))
        {
            isInSpotlight = false;
        }
    }
}