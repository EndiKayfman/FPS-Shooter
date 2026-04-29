using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class FpsMobController : MonoBehaviour
{
    [SerializeField] TeamMember affiliation;
    [SerializeField] HitscanWeapon primary;
    [SerializeField] float maxChaseHoriz = 40f;
    [SerializeField] float fireHorizDistance = 20f;

    NavMeshAgent _agent;
    RoundManager _rounds;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = 3f;
        _rounds = FindFirstObjectByType<RoundManager>();
    }

    public void PrepareForRound()
    {
        if (_agent == null || !_agent.enabled)
            return;

        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
    }

    void Update()
    {
        if (affiliation == null || primary == null || _agent == null)
            return;

        if (_rounds != null && !_rounds.FightsAreLive)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            return;
        }

        var hostile = LocateHostile(out var planarDistance);
        if (hostile == null)
        {
            _agent.isStopped = true;
            return;
        }

        var aimPoint = hostile.position + Vector3.up * 1.1f;
        _agent.isStopped = false;
        _agent.SetDestination(hostile.position);

        var lookDir = (aimPoint - primary.transform.position).normalized;
        if (lookDir.sqrMagnitude > 0.01f)
            primary.transform.rotation = Quaternion.Slerp(primary.transform.rotation, Quaternion.LookRotation(lookDir),
                Time.deltaTime * 18f);

        var yawDir = hostile.position - transform.position;
        yawDir.y = 0f;
        if (yawDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(yawDir.normalized), Time.deltaTime * 12f);

        if (planarDistance <= fireHorizDistance)
            primary.TryManualFire();
    }

    Transform LocateHostile(out float planarDistance)
    {
        planarDistance = float.MaxValue;
        var foeSide = affiliation.Team == CombatTeam.Alpha
            ? CombatTeam.Beta
            : affiliation.Team == CombatTeam.Beta
                ? CombatTeam.Alpha
                : CombatTeam.None;

        if (foeSide == CombatTeam.None)
            return null;

        Transform best = null;

        foreach (var member in FindObjectsByType<TeamMember>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (member.Team != foeSide) continue;

            var hp = member.GetComponent<Health>() ?? member.GetComponentInChildren<Health>();
            if (hp == null || !hp.IsAlive) continue;

            var flat = member.transform.position - transform.position;
            flat.y = 0f;
            var dist = flat.magnitude;
            if (dist > maxChaseHoriz) continue;

            if (dist < planarDistance)
            {
                planarDistance = dist;
                best = member.transform.root;
            }
        }

        return best;
    }
}
