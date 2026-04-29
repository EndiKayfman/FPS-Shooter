using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FpsInputDriver : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;
    [SerializeField] FpsMotor motor;
    [SerializeField] FpsLook look;
    [SerializeField] HitscanWeapon weapon;

    InputActionMap _playerMap;
    InputAction _move;
    InputAction _look;
    InputAction _jump;
    InputAction _attack;

    RoundManager _rounds;

    void Awake()
    {
        _rounds = FindFirstObjectByType<RoundManager>();

        if (actions == null)
        {
            Debug.LogError($"{nameof(FpsInputDriver)}: assign InputActionAsset ({nameof(actions)}).");
            return;
        }

        _playerMap = actions.FindActionMap("Player");
        _move = _playerMap.FindAction("Move");
        _look = _playerMap.FindAction("Look");
        _jump = _playerMap.FindAction("Jump");
        _attack = _playerMap.FindAction("Attack");
    }

    void OnEnable()
    {
        EnableActionsSafe();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        _playerMap?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EnableActionsSafe()
    {
        _playerMap?.Enable();
    }

    void Update()
    {
        if (_move == null || motor == null)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        {
            motor.SetMovementInput(Vector2.zero);
            return;
        }

        motor.SetMovementInput(_move.ReadValue<Vector2>());

        if (look != null && _look != null)
            look.ConsumeLook(_look.ReadValue<Vector2>());

        if (_jump != null && _jump.WasPressedThisFrame())
            motor.RequestJump();

        if (weapon != null && _attack != null && _attack.WasPressedThisFrame())
            weapon.TryManualFire();
    }
}
