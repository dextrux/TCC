using UnityEngine;

public class SanityLight : MonoBehaviour
{
    [Header("Área da Luz")]
    public float outerRadius = 10f;
    public float innerRadius = 4f;

    [Header("Regeneração de Sanidade")]
    public float centerRegeneration = 10f;
    public float edgeRegeneration = 5f;

    public float GetRegenerationRate(Vector3 playerPosition)
    {
        float distance = Vector3.Distance(transform.position, playerPosition);

        
        if (distance <= innerRadius)
        {
            return centerRegeneration;
        }

        
        if (distance <= outerRadius)
        {
            return edgeRegeneration;
        }

        
        return 0f;
    }

    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, outerRadius);

        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}