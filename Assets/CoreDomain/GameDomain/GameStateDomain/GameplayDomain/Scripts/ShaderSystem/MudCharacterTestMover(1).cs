using UnityEngine;

// So pra testar: WASD/setas andam, Espaco pula. Movimento simples com CharacterController.
// Usa o Input antigo (funciona com Active Input Handling = Both).
[RequireComponent(typeof(CharacterController))]
public class MudCharacterTestMover : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -20f;

    private CharacterController cc;
    private float verticalVelocity;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(move); // vira pra onde anda (pes esquerdo/direito fazem sentido)

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        cc.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}
