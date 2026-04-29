using System.Collections.Generic;
using System.Linq;

public enum RoundResult
{
    Undecided,
    TeamAlphaWin,
    TeamBetaWin,
    Tie
}

public static class RoundOutcomeResolver
{
    public static RoundResult EliminationWinner(IEnumerable<Health> fighters)
    {
        var alive = fighters
            .Where(h => h != null && h.IsAlive)
            .Select(h => h.GetComponent<TeamMember>())
            .Where(tm => tm != null && tm.Team != CombatTeam.None)
            .Select(tm => tm.Team)
            .ToList();

        if (alive.Count == 0) return RoundResult.Tie;

        var distinct = alive.Distinct().ToList();
        if (distinct.Count > 1) return RoundResult.Undecided;

        return distinct[0] == CombatTeam.Alpha ? RoundResult.TeamAlphaWin : RoundResult.TeamBetaWin;
    }

    public static RoundResult TimerWinner(IEnumerable<Health> fighters)
    {
        var sumAlpha = SumHpAlive(fighters, CombatTeam.Alpha);
        var sumBeta = SumHpAlive(fighters, CombatTeam.Beta);

        if (sumAlpha > sumBeta) return RoundResult.TeamAlphaWin;
        if (sumBeta > sumAlpha) return RoundResult.TeamBetaWin;
        return RoundResult.Tie;
    }

    static float SumHpAlive(IEnumerable<Health> fighters, CombatTeam side)
    {
        return fighters
            .Where(h => h != null && h.IsAlive)
            .Where(h =>
            {
                var tm = h.GetComponent<TeamMember>();
                return tm != null && tm.Team == side;
            })
            .Sum(h => h.CurrentHp);
    }
}
