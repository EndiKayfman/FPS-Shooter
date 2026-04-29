using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField] float maxHp = 100f;

    float _current;

    public float CurrentHp => _current;
    public float MaxHp => maxHp;
    public bool IsAlive => _current > 0f;

    /// <summary>Invoked whenever HP reaches zero from damage.</summary>
    public event Action<Health> Died;

    void Awake() => ResetToFull();

    public void ApplyDamage(float amount, CombatTeam attackerTeam)
    {
        if (_current <= 0f) return;

        var victimTeam = GetComponent<TeamMember>();
        if (victimTeam != null && victimTeam.Team == attackerTeam) return;

        _current = Mathf.Max(0f, _current - amount);
        if (_current <= 0f)
            Died?.Invoke(this);
    }

    public void ResetToFull() => _current = maxHp;
}
