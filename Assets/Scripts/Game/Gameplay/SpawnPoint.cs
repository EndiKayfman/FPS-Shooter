using UnityEngine;

public sealed class SpawnPoint : MonoBehaviour
{
    [SerializeField] CombatTeam team;

    public CombatTeam Team => team;

    public void ApplyTo(Transform fighter)
    {
        fighter.SetPositionAndRotation(transform.position + Vector3.up * 0.1f, transform.rotation);
    }
}
