# gamedev-test-repo

## Project Overview
This is a solo-developed 2D arcade-style beat ’em up game built using **Godot 4 (C#)**.

The goal of this project is to:
- Build a classic **side-scrolling brawler** (Final Fight / Streets of Rage style)
- Use a **code-first, AI-assisted development workflow**
- Keep systems simple, explicit, and readable
- Avoid heavy editor-only logic or opaque configuration

This repository is intended to be worked on collaboratively with an AI coding agent (Claude Code / VS Code).

---

## High-Level Game Design
- **Genre**: 2D arcade beat ’em up
- **Perspective**: Side-on camera with vertical movement for depth (Y-axis lanes) 
- **Platform**: Windows (initially)
- **Graphics**: Sprite-based (no 3D models for this project)

### Core Gameplay Pillars
- Tight, readable combat
- Simple but expressive enemy behaviours
- Authorable encounters (enemy waves, camera locks)
- Deterministic, debuggable rules (hitstun, knockback, invulnerability)

---

## Technical Stack
- **Engine**: Godot 4.x (Mono / C# build)
- **Language**: C# (.NET)
- **IDE**: VS Code (with Claude Code)
- **Version Control**: GitHub (private repo)

---

## Development Philosophy (Important for AI Agents)
When modifying or adding code, follow these rules:

1. **Prefer code over editor configuration**
   - Core rules, state machines, and behaviours should live in C#
   - Godot scenes should primarily define structure and visuals

2. **Keep scripts small and focused**
   - One responsibility per script
   - Avoid “god classes”

3. **Be data-driven where possible**
   - Enemy stats, wave definitions, and tuning values should be loadable from data files (e.g. JSON or Godot Resources)
   - Do NOT hardcode gameplay numbers unless explicitly instructed

4. **Readable > clever**
   - Explicit logic is preferred over abstraction
   - This is a solo project; maintainability matters more than elegance

5. **Avoid premature complexity**
   - No advanced pathfinding unless explicitly requested
   - Steering, lane logic, and simple rules are preferred

---

## Planned Architecture (Initial)
Expected top-level folders (may evolve):
/Game
/Core // bootstrap, game state, scene flow
/Entities // player, enemies, pickups
/Combat // hitboxes, hurtboxes, damage, knockback
/AI // enemy behaviour logic
/Spawning // enemy waves, triggers, camera locks
/UI // HUD, debug overlays
/Data // JSON or resource-based game data

AI agents should respect this structure and extend it carefully.

---

## Initial Milestone (First Playable Slice)
The first concrete goal of the project is:

- A test scene where:
  - The player can move and punch
  - A single enemy can approach, take damage, and be defeated
  - Hitstun and knockback are visible
  - Everything is deterministic and debuggable

No menus, no polish, no sound required at this stage.

---

## How AI Agents Should Work in This Repo
When making changes:
- Prefer **incremental, testable steps**
- Explain assumptions in comments if needed
- Do NOT add assets or large dependencies unless explicitly instructed
- Do NOT auto-run terminal commands without user approval
- Ask for clarification if a design decision is ambiguous

---

### Movement & Combat Depth Model
- Movement is 2D with continuous X/Y axes.
- The Y axis represents “depth” (Final Fight / Streets of Rage style), not discrete lanes.
- Attacks are intended to connect only when attacker and target are within a small Y-distance (“depth tolerance”).
- Depth tolerance rules may be layered on after basic combat is working.

---

## Status

### Completed — Initial Combat Sandbox
- **Player**: WASD movement, J/Space to punch, HP system, hitstun + knockback on hit, dies at 0 HP
- **Enemy**: walks toward player, melee attack with range check + depth tolerance, hitstun + knockback, dies at 0 HP
- **Combat**: signal-based hitbox/hurtbox system (`Hitbox.cs`), handles close-range hits correctly via deferred overlap check, dedup prevents multi-hit per swing
- **Physics**: frame-rate-independent knockback decay (`Mathf.Pow(decay, delta * 60)`)
- **Debug overlay**: live HP and state readout for both characters
- **Build script**: `play.sh` in repo root — builds and launches in one command

### Completed — AI Slot System (step 1 + 2)
- **AIManager** (`Game/AI/AIManager.cs`): autoload singleton; owns 8 named slots around the player (`FrontAttack`, `RearAttack`, 4 diagonals, 2 wide flanks); slot world positions recalculate every frame relative to player facing; greedy slot assignment runs every ~500ms on a jittered timer using a 4-term cost function (travel time, slot preference, stickiness, tier penalty)
- **Slot tiers**: attack slots are always preferred over diagonal and wide slots via a tiered cost weight (`AIConstants.CostWeightTier`)
- **PathPlanner** (`Game/AI/PathPlanner.cs`): pure-geometry static utility; Liang-Barsky segment/rect intersection; extremal-corner selection for CW/CCW detour routes; handles start-inside-obstacle (nearest edge exit) and boundary-touch edge cases
- **Zone of Control**: player ZoC rectangle (configurable half-extents) acts as a routing obstacle; enemies navigate around it via 1–2 waypoints; within `DirectApproachRadius` of their slot they go direct
- **Enemy slot navigation**: each enemy registers with AIManager on spawn; receives a live slot target (updated every frame, not just on assignment ticks) and a waypoint list refreshed each assignment cycle; slot preference (`Front`/`Rear`/`None`) is an inspector-exposed export
- **Debug visualisation**: ZoC rect, slot circles (green/yellow/grey), enemy waypoint paths, all drawn by AIManager's `_Draw()` with `DebugDraw` export flag
- **Combat decoupling**: `CollisionMask = 0` on both player and enemy — characters no longer physically push each other; all combat interaction is through the Area2D hitbox/hurtbox system

### Next Steps (not yet started)
- Camera scrolling to follow player
- Multiple enemies in scene (stress-test slot assignment)
- Additional attack types (kick, jump kick)
- Stun/grab mechanics
- Weapon pickup system
- Health bar UI (replace debug text)
- Sprite-based graphics (replace coloured rectangles)