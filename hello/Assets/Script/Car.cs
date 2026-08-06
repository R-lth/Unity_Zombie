using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputComponent))]
[RequireComponent(typeof(Health))]
public class Car : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotateSpeed = 2f;

    private Rigidbody rb;
    private PlayerInputComponent input;
    private float turn;

    private void Awake()
    {
        CharacterEntity.Ensure(gameObject, CharacterRole.Player);
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInputComponent>();
        turn = transform.eulerAngles.y;
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = input != null ? input.MoveInput : Vector2.zero;
        Vector2 lookInput = input != null ? input.LookAt() : Vector2.zero;
        Vector3 localMove = new Vector3(moveInput.x, 0f, moveInput.y);

        if (localMove.sqrMagnitude > 1f)
        {
            localMove.Normalize();
        }

        Vector3 worldMove = rb.rotation * localMove;
        rb.MovePosition(rb.position + worldMove * moveSpeed * Time.fixedDeltaTime);

        turn += lookInput.x * rotateSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.Euler(0f, turn, 0f));
    }
}
