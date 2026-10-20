# itch.io page copy and upload checklist

Paste-ready text for the Shmupper project page, plus the settings that are easy to get wrong.

---

## Title

    Shmupper

**Short tagline** (shown under the title, 120 char limit)

    The Keep learns from you. Every upgrade you buy makes it stronger.

---

## Description

> You are the last Gunmage, the final heir to an order that married gunpowder to the old
> magic.
>
> The Arcane Keep rebuilds itself from its own rubble every night, and it has swallowed the
> Ember Core. Descend through its shifting halls and take it back.
>
> **But the Keep is watching what you take.** Every upgrade bought at a Wayshrine raises its
> Threat by one, and Threat makes everything that spawns afterwards tougher, meaner and more
> numerous. Descend without buying and the Keep pays you a bounty for your restraint instead.
>
> Getting stronger is not progress. It is a wager.
>
> ---
>
> - A castle that is laid out fresh every run
> - Six fantasy horrors, from imps to a teleporting Lich
> - Four weapons that are half firearm, half spell
> - Arcade scoring: combo chains to x8, plus Swift, Flawless and No-Shrine bonuses
> - Credits, an attract loop, and a TOP 10 board with initial entry

---

## Controls

| | |
|---|---|
| **Move** | W A S D |
| **Look** | Mouse |
| **Fire** | Left mouse (hold) |
| **Switch weapon** | 1 – 4, mouse wheel, or Q |
| **Healing flask** | F or R |
| **Jump** | Space |
| **Insert credit** | C |
| **Start** | Enter |
| **Wayshrine** | Arrows choose, Enter buys, Esc descends |

Gamepad is supported: left stick moves, right stick looks, west button drinks a flask.

---

## Metadata

- **Genre:** Action → Shooter
- **Tags:** `fps`, `shoot-em-up`, `roguelite`, `procedural-generation`, `fantasy`, `dungeon-crawler`, `arcade`, `unity`, `singleplayer`, `retro`
- **Made with:** Unity
- **Platforms:** HTML5 (browser) and Windows
- **Inputs:** Mouse, keyboard, gamepad

---

## Upload checklist

1. **Kind of project: HTML.**
2. Zip the **contents** of `Builds/WebGL` so that `index.html` sits at the **root of the
   zip**, not inside a folder. This is the single most common cause of a build that uploads
   fine and then will not run.
3. Tick **"This file will be played in the browser."**
4. **Embed options:** manually set viewport to **1280 × 720**, and enable **"Fullscreen
   button"**. The game captures the mouse, which behaves far better fullscreen than in the
   page.
5. Add the Windows zip as a second file, flagged as a Windows download. Exclude
   `Shmupper_BackUpThisFolder_ButDontShipItWithYourGame` — those are debug symbols.
6. **Cover image:** export `Assets/Art/Cover/ShmupperCover_itch_630x500.svg` to PNG at
   630 × 500. Opening the SVG in a browser and screenshotting it is enough.
7. Leave **Visibility: Draft** until you have played the uploaded build *in a browser*, then
   switch to Public.

### Re-uploading later

The web form re-uploads the whole archive every time. `butler` is incremental and versioned:

```
butler push Builds/WebGL thedagger994/shmupper:html5
butler push Builds/Windows thedagger994/shmupper:windows
```

---

## Verify in the browser before going public

The browser build is the one that can break in ways the Windows build never will. Check, in
this order:

1. **Is there sound?** Every effect is synthesised at runtime through `AudioClip.Create` and
   `SetData`. If WebGL's audio backend rejects that, the game ships silent and nothing else
   will tell you.
2. **Does the mouse capture** on the first click, and release on Esc?
3. **Set a high score, reload the page, and confirm it is still there.** Persistence routes
   through `PlayerPrefs` on WebGL specifically because a file write to `persistentDataPath`
   does not reliably survive a reload.
4. **Does the text render?** TextMeshPro rasterises the IM Fell English atlas at runtime from
   the linked TTF.
5. **Frame rate with a full wave on screen**, on a mid floor where enemy counts climb.
