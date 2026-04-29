using UnityEngine;

public sealed class TeamMember : MonoBehaviour
{
    [SerializeField] CombatTeam team;

    public CombatTeam Team => team;

    public void SetTeam(CombatTeam t) => team = t;
}
