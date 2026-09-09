# SHMUPPER — The Arcane Keep

A first-person shoot 'em up in the old arena-shooter tradition, set in a medieval castle that
generates itself fresh every time you descend.

---

## 1. Description and main objective

**The pitch.** You are the last Gunmage — the final graduate of an order that learned to load
gunpowder with the old magic. The Arcane Keep is a castle that tears itself down and rebuilds
itself every night from the bones of the last one, and it has swallowed the Ember Core, the relic
the kingdom runs on. Nobody has mapped the Keep because nobody can: the floor plan is different
every time the doors open.

**The objective.** Descend nine floors and take the Ember Core back. Each floor is a procedurally
generated castle level: clear every wave of its garrison, follow your compass to the Wayshrine,
decide what to buy, and go deeper. Clear floor nine — guarded by The Pale Archivist — and the run
is won. Die before that and you leave your initials on the wall instead.

**The hook — the bargain.** The Keep learns from you. Every upgrade tier you buy at a Wayshrine
raises the Keep's **Threat** by one, and Threat makes every enemy that spawns afterwards tougher,
faster, angrier and more numerous. Getting stronger is not free progress; it is a wager. Walk past
the shrine with your purse full and the castle stays merciful — and pays you a bonus for the
insult.

That single rule is what the whole game is built around: the player is always choosing between
power now and difficulty later, and the score rewards both answers differently.

---

## 2. The loop

```
      ┌──────────────────────────────────────────────────────┐
      │                                                      │
  generate floor ──► clear waves ──► shrine lights up        │
                          ▲               │                  │
                          │               ▼                  │
                     enemies scale    spend essence           │
                     with Threat      (or refuse it)          │
                          ▲               │                  │
                          └───────────────┴──────► descend ───┘
```

* A floor holds 2–5 waves depending on depth.
* Waves trickle in from spawn points weighted away from the player and out of line of sight.
* Every third floor ends with a boss: **The Pale Archivist**, a Lich — so floors 3, 6 and 9.
* Clearing floor 9 claims the Ember Core and wins the run, paying 25,000 plus four times whatever
  essence went unspent — a final reward for having refused the bargain.
* Death ends the run. Either way the score is banked to the TOP 10.

---

## 3. Controls

| Input | Action |
|---|---|
| `W A S D` | Move (Quake-style ground/air acceleration — strafe-jumping works) |
| `Mouse` | Look |
| `Space` | Jump |
| `Left Mouse` | Fire (hold; every weapon is automatic) |
| `1` – `4`, `Mouse Wheel`, `Q` | Switch weapon |
| `F` or `R` | Drink a healing flask |
| `Esc` | Release / recapture the mouse |
| `C` or `5` | Insert credit (front end) |
| `Enter` | Start / confirm |

Gamepad is supported for movement, look, jump and flask.

---

## 4. Arsenal

Magic and gunsmithing in the same object. Each weapon owns a distinct range band.

| Weapon | Role | Damage | Rate | Ammo | Unlocks |
|---|---|---|---|---|---|
| **Spellslinger** | Hitscan revolver, the reliable answer | 24 | 4.6/s | Infinite | Floor 1 |
| **Emberlance** | Nine-pellet hand cannon, close range | 11 × 9 | 1.35/s | 36 | Floor 1 |
| **Arcanoflux** | Storm in a barrel, chews crowds | 8.5 | 11/s | 260 | Floor 2 |
| **Runeblaster** | Explosive siegework, 5.5m splash | 95 | 0.95/s | 18 | Floor 3 |

Ammo drops from kills and always feeds whichever unlocked weapon is emptiest.

---

## 5. Bestiary

Each enemy is built from primitives with one signature colour, one signature shape and one glowing
tell, so it can be identified at speed in the dark.

| Enemy | Read | Behaviour | Points |
|---|---|---|---|
| **Imp** | Small green sphere, two horns | Fast swarmer, leaps the last few metres | 100 |
| **Hollow Knight** | Floating helm over an empty gorget, blue | Armoured; telegraphs a lunging slash | 250 |
| **Wizard** | Purple cone, orbiting orb | Holds range, fires tracking bolts, blinks away when you close | 400 |
| **Baby Dragon** | The only winged thing, orange, lit throat | Circles overhead, spits fire, commits to dives | 500 |
| **Gargoyle** | Heavy amber stone, folded wings | Slow; area-denial ground slam, hurls stone at range | 750 |
| **The Pale Archivist** | Lich — robe cone, orbiting crown | Radial volleys, summons, channelled beam, blinks at each quarter health | 5,000 |

---

## 6. Scoring

**Chain multiplier.** Kills within 3.2 seconds of each other chain.

| Chain | 3 | 6 | 10 | 15 | 20 | 30 | 45 |
|---|---|---|---|---|---|---|---|
| Multiplier | ×2 | ×3 | ×4 | ×5 | ×6 | ×7 | ×8 |

**Bonuses.**

| Bonus | Award | Condition |
|---|---|---|
| Depth | 500 × floor per wave | Clearing a wave |
| No-Shrine | 2,000 × floor | Descend without buying anything |
| Flawless | 1,500 × floor | Clear a floor without taking damage |
| Swift | up to 3,000 | Beat the floor's par time |

Essence — the shrine currency — is earned separately at roughly an eighth of base kill value, so
chaining raises score without inflating purchasing power.

---

## 7. Upgrades (the Wayshrine)

Every purchase costs essence **and** one point of Threat.

| Upgrade | Effect | Tiers |
|---|---|---|
| Gunmage Vitality | +25 max health, fully restored | 6 |
| Sharpened Runes | +18% weapon damage | 6 |
| Rapid Sigils | +12% fire rate | 5 |
| Quicksilver Boots | +9% move speed, higher jump | 5 |
| Rune Flask | +1 healing flask slot | 4 |
| Deep Reserves | +40% ammo capacity, refilled | 4 |
| Soul Siphon | Heal 3 HP per kill | 4 |
| Warding Plate | −11% damage taken | 5 |

**Enemy scaling.** With `T` = Threat and `F` = floor:

```
health  ×= (1 + 0.17·T) · (1 + 0.22·(F−1))
damage  ×= (1 + 0.10·T) · (1 + 0.14·(F−1))
speed   ×= (1 + 0.028·T) · (1 + 0.035·(F−1))   capped at ×1.75
count   += T/2 + (F−1)
```

---

## 8. Presentation

**Graphics.** Every visual in the game is built from Unity primitives — boxes, spheres, cones and
quads — with flat colours and emissive accents. The shapes are defined once as code, in
`EnemyBuilder.cs` and `WeaponView.cs`, and `Shmupper > Forge Assets` bakes that code into real
prefabs with persisted meshes and materials, which is what the game instantiates. Castle geometry
is welded into
8×8-cell chunks and lit by baking torchlight into vertex colours, drawn with a custom URP shader.
That gives the blotchy, hand-placed lighting of a late-90s arena shooter, sidesteps the per-object
realtime light limit entirely, and keeps a whole floor down to a few dozen draw calls.

**Audio.** Every sound is synthesised at boot from oscillators and a noise source. No audio files.

**Front end.** A six-page attract loop over a rotating diorama of the full bestiary: title and
story, how to play, the bargain, scoring, bonuses, and the TOP 10. Credits are inserted with `C`,
the counter sits bottom-left, and the record is displayed on screen at all times — in the front
end and in the HUD during play.

---

## 9. Requirements checklist

| # | Requirement | Where it lives |
|---|---|---|
| 1 | "Shmup" style — lots of shooting and enemies | Up to 26 concurrent enemies, six types, four automatic weapons |
| 2 | Game story — clear description and objective | Sections 1–2; retold on attract pages 1 and 3 |
| 3 | Fluid gameplay, easy to learn objectives | Compass objective marker, wave announcements, `Hud.cs` |
| 4 | Simple yet distinctive shapes | `EnemyBuilder.cs`, `Shapes.cs`, `WeaponView.cs` |
| 5 | Cover art | `Assets/Art/Cover/ShmupperCover.svg` |
| 6 | Credits button with counter and Start | `Frontend.cs` — `C` inserts, counter bottom-left, Enter starts |
| 7 | Attract screen — how to play, scores, bonuses | `Frontend.cs` six-page loop + `AttractStage.cs` diorama |
| 8 | High scores — TOP 10 | `SaveSystem.cs`, attract page 6, arcade initial entry |
| 9 | Records — highest score on screen | Front-end header and in-game HUD, both live |

---

## 10. Running it

Open the project in Unity 6.6 (6000.6.0f1) and run **`Shmupper > Forge Assets and Build Scene`**
once. That bakes the bestiary and the arsenal into prefabs under `Assets/Prefabs`, writes the
registry to `Assets/Resources/GameContent.asset`, and saves a play scene to
`Assets/Scenes/Shmupper.unity`. Open that scene and press Play.

The forge is safe to re-run at any time: prefabs are overwritten in place so existing references
survive, and materials are shared by content hash rather than duplicated. Editing a forged prefab
by hand is the intended workflow — enemy health, speed, damage and score are serialized on the
prefab, so tuning them is an Inspector edit rather than a code change. Re-running the forge will
overwrite those hand edits, since it re-stamps from the table in `EnemyContext.cs`.

Every prefab lookup is allowed to fail. On a fresh clone where the forge has never run, the game
falls back to building each enemy, weapon and level prop from primitives at runtime exactly as
before, so pressing Play on any scene still works.

Scores are written to `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Shmupper/shmupper_scores.json`.
