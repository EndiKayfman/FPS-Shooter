using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class FpsMotor : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpHeight = 1.2f;

    CharacterController _cc;
    Vector3 _velocity;
    Vector2 _move;
    bool _jumpRequested;

    public void SetMovementInput(Vector2 planar) => _move = planar;

    public void RequestJump() => _jumpRequested = true;

    void Awake() => _cc = GetComponent<CharacterController>();

    void Update()
    {
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        var forward = transform.forward * _move.y + transform.right * _move.x;
        _cc.Move(forward * (moveSpeed * Time.deltaTime));

        if (_jumpRequested && _cc.isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        _jumpRequested = false;

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }
}
