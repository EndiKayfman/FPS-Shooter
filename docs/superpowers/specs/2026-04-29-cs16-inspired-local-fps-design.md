# CS 1.6–inspired local FPS — design specification

**Date:** 2026-04-29  
**Status:** Approved  
**Engine:** Unity 6000 (`6000.3.11f1`), URP  

## Purpose

Deliver a playable **local-only** FPS prototype inspired by Counter-Strike 1.6: two teams in **round-based** combat with **friendly and enemy bots**, no multiplayer. Used to validate gunplay, pacing, round flow, and simple AI before considering networking.

## Out of scope (MVP)

- Online / LAN multiplayer, relay, authoritative server  
- Bomb plant / defuse  
- Economy, buy menus, granular CS weapon roster  
- High-fidelity visuals, skeletal mesh swap on weapons (can use placeholders)  
- Competitive anti-cheat, replays  

## Goals (success criteria)

1. Player controlled first-person avatar with mouse look and WASD movement on a **NavMesh-compatible** arena.  
2. **Two teams**, each with bots; player joins one side.  
3. **Rounds** end when one team is eliminated **or** the **round timer** expires.  
4. When the timer expires while both sides have survivors, the round winner is the team whose **living members have the larger sum of remaining HP**. If sums are equal, the round is a **tie** (both teams gain **zero** round points for that outcome).  
5. Match progresses through **until one team reaches a configurable win count** (e.g. first to 4 rounds), with total length bounded by inspector settings (`roundsToWin`, optional `maxRounds`).  
6. Enemy and ally bots pursue and shoot via **NavMesh** with minimal but recognizable combat behavior.

## Core rules

| Rule | MVP behavior |
|------|----------------|
| Spawn | Scripted spawn points tagged per team; reset positions each round |
| Elimination win | Last team standing wins round |
| Timer win | Compare **sum(current HP)** of **alive** fighters per team; higher wins |
| Tie on timer | If sums equal → round tied; neither team earns a round toward match score |
| Buy phase | Skipped — all fighters start each round with the same predefined loadout (implement at least pistol-tier hitscan weapon; rifle can ship in same iteration if trivial to add via shared weapon module) |

## Architecture (recommended)

Separate responsibilities across small MonoBehaviours and plain C# helpers:

1. **Input + movement:** CharacterController-based FPS body; separate look from move; read from Unity Input System (project already ships `InputSystem_Actions`; extend or add a dedicated FPS action map).  
2. **Combat:** Weapons as components driving **Physics.Raycast** (hitscan); damage routed to `Health`; layer masks for hitting players/not environment as needed  
3. **Teams:** Discrete team id (`enum` + component); spawn selection filters by team  
4. **Round / match orchestration:** `RoundManager` (or similarly named singleton-scene object) triggers round start/end, freeze between rounds briefly (optional Short delay), notifies UI, resets health and ammunition, teleports fighters to spawn points  
5. **Bots:** NavMeshAgent + layered state (`Idle/Patrol/Engage`): acquire target within sight/cone, stop and shoot; ally targets must exclude same team  

## Data-driven tuning

Prefer **inspector-exposed floats** early; optional lightweight `ScriptableObject` profiles later for weapon fire rate / damage / round duration without code edits.

## Error handling / edge cases

- **No living members on both teams** due to simultaneous deaths: treat as **tie**.  
- **NavMesh bake missing:** Bots log a warning in editor/console and optionally disable themselves for debug.  

## Testing expectations

- Manual Play Mode: walk, shoot bots, observe round/timer/HP-sum resolution and match end.  
- Optional: isolated pure-C# helpers (e.g. round winner resolver) exposed for Edit Mode tests if time allows—not blocking MVP.

## Resolved decisions (from brainstorming)

| Topic | Decision |
|-------|-----------|
| Network | No — local only iteration 1 |
| Opponents | Simplified allied + enemy bots |
| Match structure | Rounds |
| Teams | Player + allied bots vs enemy bots |
| Timer tie-break | Sum of surviving HP per team vs sum; equality → tie |

## Future (post-MVP hooks)

Networking layer; bomb objective; richer economy; spectator; dedicated server build.
