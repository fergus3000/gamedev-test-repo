# gamedev-test-repo

## Project Overview
This is a solo-developed 2D arcade-style beat ’em up game built using **Godot 4 (C#)**.

The goal of this project is to:
- Build a classic **side-scrolling brawler** (Final Fight / Streets of Rage style)
- Use a **code-first, AI-assisted development workflow**
- Keep systems simple, explicit, and readable
- Avoid heavy editor-only logic or opaque configuration

This repository is intended to be worked on collaboratively with an AI coding agent (Cursor).

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
- **IDE**: Cursor (VS Code-compatible)
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
- Repository initialized
- Godot project not yet created
- README defines intent and constraints

Next step: initialize Godot 4 C# project and create first combat sandbox scene.