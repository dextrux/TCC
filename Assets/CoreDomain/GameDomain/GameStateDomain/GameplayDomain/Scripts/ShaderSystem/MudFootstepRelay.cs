using UnityEngine;

// Animation Events so chamam metodos no MESMO objeto do Animator.
// Se o Animator estiver num filho (o modelo), coloque este script nele
// e os eventos Footstep(0/1) sao repassados pro MudCharacterCarrier no pai.
public class MudFootstepRelay : MonoBehaviour
{
    private MudCharacterCarrier carrier;

    void Awake() => carrier = GetComponentInParent<MudCharacterCarrier>();

    public void Footstep(int foot)
    {
        if (carrier != null) carrier.Footstep(foot);
    }
}
