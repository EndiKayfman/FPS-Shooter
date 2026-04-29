# CS-inspired local FPS (Unity + bots + rounds) — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable local-only two-team round FPS in the existing Unity 6000 URP project (`FPS Shooter`), with NavMesh bots, timer + HP-sum tie-break rules, and a simple HUD.

**Architecture:** Modular MonoBehaviours under `Assets/Scripts/` for movement, combat, teams, orchestration (`RoundManager`), NavMesh-backed enemy/ally bots, UI overlay. Existing `Assets/InputSystem_Actions.inputactions` supplies Move/Look/Attack/Jump; generate C# wrapper from that asset before wiring FPS code.

**Tech stack:** Unity 6000 (`6000.3.11f1`), URP, Input System (`com.unity.inputsystem`), Physics hitscan raycasts (`Physics.Raycast`), Unity AI Navigation (`NavMeshAgent`). No external packages unless already in manifest.

---

## File map

| Responsibility | Paths |
|----------------|-------|
| Team enum + marker | `Assets/Scripts/Game/Teams/CombatTeam.cs` |
| Damageable HP | `Assets/Scripts/Game/Combat/Health.cs`, `Assets/Scripts/Game/Combat/TeamMember.cs` |
| Resolver (pure logic) | `Assets/Scripts/Game/Rounds/RoundOutcomeResolver.cs` |
| Round/Match orchestration | `Assets/Scripts/Game/Rounds/RoundManager.cs` |
| Spawns | `Assets/Scripts/Game/Gameplay/SpawnPoint.cs` |
| Player FPS control | `Assets/Scripts/Game/Fps/FpsMotor.cs`, `Assets/Scripts/Game/Fps/FpsLook.cs`, `Assets/Scripts/Game/Fps/PlayerInteractor.cs` |
| Weapon | `Assets/Scripts/Game/Fps/HitscanWeapon.cs` |
| Bots | `Assets/Scripts/Game/Ai/FpsMobController.cs` |
| HUD | `Assets/Scripts/Game/UI/HudController.cs` |

---

### Task 1: Input wiring + FPS movement + look

**Files:**

- Create: `Assets/Scripts/Game/Fps/FpsMotor.cs`
- Create: `Assets/Scripts/Game/Fps/FpsLook.cs`
- Create: `Assets/Scripts/Game/Fps/PlayerInteractor.cs`
- Modify: enable **Generate C# Class** on `Assets/InputSystem_Actions.inputactions` (Inspector) → creates `InputSystem_Actions.cs` beside the asset

- [ ] **Step 1: Generate Input System C# wrapper**

In Unity Editor: select `InputSystem_Actions.inputactions` → enable **Generate C# Class**, Apply. Confirm `InputSystem_Actions.cs` exists next to the asset.

- [ ] **Step 2: Add `FpsMotor.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FpsMotor : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpHeight = 1.2f;

    CharacterController _cc;
    Vector2 _move;
    bool _jumpPressed;
    Vector3 _velocity;

    public void OnMove(InputValue value) => _move = value.Get<Vector2>();
    public void OnJump(InputValue value) => _jumpPressed = value.isPressed;

    void Awake() => _cc = GetComponent<CharacterController>();

    void Update()
    {
        if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        var forward = transform.forward * _move.y + transform.right * _move.x;
        _cc.Move(forward * (moveSpeed * Time.deltaTime));
        if (_jumpPressed && _cc.isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }
}
```

- [ ] **Step 3: Add `FpsLook.cs`** (Yaw on player root body, pitch on camera pivot)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FpsLook : MonoBehaviour
{
    [SerializeField] Transform yawRoot;
    [SerializeField] Transform pitchPivot;
    [SerializeField] float sensitivity = 0.06f;

    float _pitch;

    public void OnLook(InputValue value)
    {
        var delta = value.Get<Vector2>();
        yawRoot.Rotate(0f, delta.x * sensitivity * 100f * Time.deltaTime, 0f);
        _pitch -= delta.y * sensitivity * 100f * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);
        pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
```

Assign `yawRoot` = player capsule transform, `pitchPivot` = child empty holding the camera.

- [ ] **Step 4: Add `PlayerInteractor.cs`** for `PlayerInput` bootstrapping

```csharp
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerInteractor : MonoBehaviour
{
}
```

Configure `PlayerInput` with **Invoke Unity Events** (or unity events on actions): hook `Move`/`Look`/`Jump` to `FpsMotor` / `FpsLook`, `Attack` to `HitscanWeapon` added in Task 2. Prefer **Actions Asset** referencing generated `InputSystem_Actions`.

- [ ] **Step 5: Manual verify**

Enter Play Mode on a flat plane + CharacterController capsule: WASD/strafe, mouse rotates view, Space jumps.

**Expected:** smooth grounded movement; no drift when idle.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Game/Fps Assets/InputSystem_Actions.inputactions Assets/InputSystem_Actions.cs
git commit -m "feat(fps): input and first-person motor"
```

---

### Task 2: Teams, health, hitscan damage

**Files:**

- Create: `Assets/Scripts/Game/Teams/CombatTeam.cs`
- Create: `Assets/Scripts/Game/Combat/TeamMember.cs`
- Create: `Assets/Scripts/Game/Combat/Health.cs`
- Create: `Assets/Scripts/Game/Fps/HitscanWeapon.cs`

- [ ] **Step 1: `CombatTeam.cs`**

```csharp
public enum CombatTeam { None = 0, Alpha = 1, Beta = 2 }
```

- [ ] **Step 2: `TeamMember.cs`**

```csharp
using UnityEngine;

public sealed class TeamMember : MonoBehaviour
{
    [SerializeField] CombatTeam team;

    public CombatTeam Team => team;

    public void SetTeam(CombatTeam t) => team = t;
}
```

- [ ] **Step 3: `Health.cs`**

```csharp
using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField] float maxHp = 100f;
    float _current;

    public float CurrentHp => _current;
    public float MaxHp => maxHp;
    public bool IsAlive => _current > 0f;
    public event Action<Health> Died;

    void Awake() => _current = maxHp;

    public void ApplyDamage(float amount, CombatTeam attackerTeam)
    {
        if (_current <= 0f) return;
        var victim = GetComponent<TeamMember>();
        if (victim != null && victim.Team == attackerTeam) return;

        _current = Mathf.Max(0f, _current - amount);
        if (_current <= 0f) Died?.Invoke(this);
    }

    public void ResetToFull() => _current = maxHp;
}
```

- [ ] **Step 4: `HitscanWeapon.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HitscanWeapon : MonoBehaviour
{
    [SerializeField] float damage = 25f;
    [SerializeField] float range = 80f;
    [SerializeField] float fireCooldown = 0.15f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] TeamMember wielderTeam;

    float _nextShot;

    public bool TryManualFire()
    {
        if (Time.time < _nextShot) return false;
        FireOnce();
        return true;
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TryManualFire();
    }

    void FireOnce()
    {
        _nextShot = Time.time + fireCooldown;
        var origin = transform.position;
        var dir = transform.forward;
        if (!Physics.Raycast(origin, dir, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
            return;
        var hp = hit.collider.GetComponentInParent<Health>();
        if (hp == null) return;
        hp.ApplyDamage(damage, wielderTeam.Team);
    }
}
```

Inspector: Camera child carries weapon; assign `TeamMember`; set `hitMask` to exclude own colliders if ray hits self — use **`Ignore Raycast` layer on player capsule** child collider workaround or shrink origin forward offset.

- [ ] **Step 5: Manual verify**

Place second capsule with opposing team and Health: repeated shots drain HP until death; friendly fire off when teams match.

**Expected:** ~4 shots lethality at 25 dmg / 100 HP.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Game
git commit -m "feat(combat): teams health hitscan"
```

---

### Task 3: Round outcome resolver

**Files:**

- Create: `Assets/Scripts/Game/Rounds/RoundOutcomeResolver.cs`

- [ ] **Step 1: Implement resolver**

```csharp
using System.Collections.Generic;
using System.Linq;

public enum RoundResult { Undecided, TeamAlphaWin, TeamBetaWin, Tie }

public static class RoundOutcomeResolver
{
    public static RoundResult EliminationWinner(IEnumerable<Health> fighters)
    {
        var alive = fighters
            .Where(h => h != null && h.IsAlive)
            .Select(h => h.GetComponent<TeamMember>())
            .Where(tm => tm != null)
            .Select(tm => tm.Team)
            .Where(t => t != CombatTeam.None)
            .ToList();

        if (alive.Count == 0) return RoundResult.Tie;

        var distinct = alive.Distinct().ToList();
        if (distinct.Count == 1)
            return distinct[0] == CombatTeam.Alpha ? RoundResult.TeamAlphaWin : RoundResult.TeamBetaWin;

        return RoundResult.Undecided;
    }

    public static RoundResult TimerWinner(IEnumerable<Health> fighters)
    {
        float sumAlpha = SumHp(fighters, CombatTeam.Alpha);
        float sumBeta = SumHp(fighters, CombatTeam.Beta);
        if (sumAlpha > sumBeta) return RoundResult.TeamAlphaWin;
        if (sumBeta > sumAlpha) return RoundResult.TeamBetaWin;
        return RoundResult.Tie;
    }

    static float SumHp(IEnumerable<Health> fighters, CombatTeam side)
    {
        return fighters
            .Where(h => h != null && h.IsAlive)
            .Where(h => h.GetComponent<TeamMember>() != null && h.GetComponent<TeamMember>().Team == side)
            .Sum(h => h.CurrentHp);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Game/Rounds/RoundOutcomeResolver.cs
git commit -m "feat(rounds): outcome resolver"
```

---

### Task 4: Spawns + RoundManager + HUD

**Files:**

- Create: `Assets/Scripts/Game/Gameplay/SpawnPoint.cs`
- Create: `Assets/Scripts/Game/Rounds/RoundManager.cs`
- Create: `Assets/Scripts/Game/UI/HudController.cs`

- [ ] **Step 1: `SpawnPoint.cs`**

```csharp
using UnityEngine;

public sealed class SpawnPoint : MonoBehaviour
{
    [SerializeField] CombatTeam team;

    public CombatTeam Team => team;
    public void Apply(Transform fighter)
    {
        fighter.SetPositionAndRotation(transform.position + Vector3.up * 0.1f, transform.rotation);
    }
}
```

- [ ] **Step 2–4: Implement `RoundManager`** (single scene singleton)

Behaviors required (no stubs):

1. Serialized: `HudController hud`, `SpawnPoint[] spawns`, `Health[] fighters` or auto-find tagged `Actor` roots, `float roundDurationSeconds = 115f`, `float interRoundDelay = 5f`, `int roundsToWin = 4`, `CombatTeam humanTeam`.

2. On `Start()` / first frame: shuffle spawn cycle per team, place each fighter, refill `Health.ResetToFull`, reset weapon cooldown timestamps if tracked.

3. Each `Update()` while Fighting: decrement timer; subscribe once to each `Health.Died` → when `EliminationWinner` != `Undecided`, finalize round scoring.

4. Between rounds pause input or freeze agents (freeze optional MVP: teleport + reset suffices).

5. Scoring rule: elimination win adds 1 round point to winner; timer uses `TimerWinner`; `Tie` adds **0** points to either team **and** increments internal tie counter (optional HUD).

6. Match stops when either team score ≥ `roundsToWin` — invoke `HudController.ShowGameOver(team)`.

`HudController` exposes `ShowRoundBanner(string)`, `SetTimer(float)`, `SetScores(int alpha, int beta)`, `AnnounceTie()`, `AnnounceMatchWinner(CombatTeam)`.

- [ ] **Step 5: Manual verify**

Elimination path and simulated timer expiry (`roundDurationSeconds` set to **5** for QA) produces correct scorer.

**Expected:** scores and announcements match resolver output.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts
git commit -m "feat(rounds): spawns manager hud"
```

---

### Task 5: NavMesh bots

**Files:**

- Create: `Assets/Scripts/Game/Ai/FpsMobController.cs`

- [ ] **Step 1: Ensure package** — verify `manifest.json` includes `com.unity.ai.navigation`; add via Package Manager if missing.

- [ ] **Step 2: Scene** — plane + obstacles; **`NavMesh Surface`** bake (AI Navigation overlay).

- [ ] **Step 3: `FpsMobController`** — Requires `NavMeshAgent`, optional `CapsuleCollider` + `Rigidbody.isKinematic` off for agents (prefer pure NavMeshAgent). Each frame acquire nearest hostile `TeamMember` in `searchRadius`; if found, `agent.SetDestination(target.position)`. When planar distance `< shootRange`, call weapon `TryManualFire()`. Aim: orient weapon transform `LookAt` target chest.

**Expected:** bots move toward targets and chips damage until elimination path triggers.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Game/Ai
git commit -m "feat(ai): navmesh bot shooter"
```

---

### Task 6: Arena scene + layering

Duplicate `Assets/Scenes/SampleScene.unity` → `ArenaBots.unity`.

- Layers: dedicate `Actors` layer; Player/Bot meshes on Actors; set **`HitscanWeapon.hitMask`** so rays do not hit the shooter’s collider (e.g. Physics exclude query or shorter layer mask excluding self layer).

- Populate minimal Alpha roster (player + 1 ally bot) vs Beta bots (≥2 enemies).

Manual QA:

1. Full elimination round resolves score.  
2. Timer round with asymmetric HP remnants awards correct team via sum rule.

---

### Task 7: Package verification

Inspect `FPS Shooter/Packages/manifest.json` → ensure **`com.unity.inputsystem`**, **`com.unity.ai.navigation`** listed.

---

## Spec coverage checklist

| Requirement | Tasks |
|-------------|-------|
| Local only | Implicit — no networking code |
| Two teams allies + bots | Task 4 setup + Task 5 |
| Elimination rounds | Tasks 3–4 |
| Timer + summed HP survivors | Resolver + RoundManager Timer branch |
| Tie = neither gets round point | RoundManager Tie branch Task 4 |
| CS-like feel (minimal) | FpsMotor + Weapon cadence Task 1–2 |

---

## Execution handoff

**Plan complete and saved to** `FPS Shooter/docs/superpowers/plans/2026-04-29-cs16-inspired-local-fps.md`.

**Execution options:**

1. **Subagent-driven (recommended)** — dispatch a worker per Task block, compile after merges.  
2. **Inline execution** — carry out tasks sequentially in one session using executing-plans-style checkpoints.

**Which approach do you want for implementation work?**
