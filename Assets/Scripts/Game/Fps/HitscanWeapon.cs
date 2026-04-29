using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HitscanWeapon : MonoBehaviour
{
    [SerializeField] float damage = 25f;
    [SerializeField] float range = 80f;
    [SerializeField] float fireCooldown = 0.15f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] TeamMember wielderTeam;
    [SerializeField] Transform rayOriginOverride;
    [SerializeField] List<Collider> ignoreCollider;

    readonly RaycastHit[] _hitsScratch = new RaycastHit[8];

    float _nextShotAllowed;

    public bool TryManualFire()
    {
        if (Time.time < _nextShotAllowed) return false;
        FireOnce();
        return true;
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TryManualFire();
    }

    public void ResetCooldown() => _nextShotAllowed = 0f;

    void FireOnce()
    {
        _nextShotAllowed = Time.time + fireCooldown;

        if (wielderTeam == null)
            return;

        var origin = rayOriginOverride != null ? rayOriginOverride.position : transform.position;
        var dir = transform.forward;
        var count = Physics.RaycastNonAlloc(origin, dir, _hitsScratch, range, hitMask, QueryTriggerInteraction.Ignore);

        Health best = null;
        var bestDist = float.MaxValue;
        var rootSelf = wielderTeam.transform.root;

        for (var i = 0; i < count; i++)
        {
            var h = _hitsScratch[i];
            if (IsIgnoredCollider(h.collider)) continue;

            var health = h.collider.GetComponentInParent<Health>();
            if (health == null) continue;

            if (health.transform.root == rootSelf) continue;

            var d = h.distance;
            if (d >= bestDist) continue;
            bestDist = d;
            best = health;
        }

        if (best == null) return;

        var attacker = wielderTeam.Team;
        best.ApplyDamage(damage, attacker);
    }

    bool IsIgnoredCollider(Collider c)
    {
        if (ignoreCollider == null) return false;
        for (var i = 0; i < ignoreCollider.Count; i++)
        {
            if (ignoreCollider[i] != null && ignoreCollider[i] == c)
                return true;
        }

        return false;
    }
}
