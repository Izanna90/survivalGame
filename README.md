# Survival Game 7702254 Theo Fisch Dewailly

Third-person survival prototype built with Unity. Move with ZQSD, orbit the camera with the mouse, throw grenades, avoid enemy grenades and mines, and interact with the world (chest/door). Enemy AI uses NavMesh to chase and return. Includes ragdoll deaths, health UI, and ambience audio.

## Features

- Player
  - ZQSD camera-relative movement (smooth accel/decel, turning)
  - Mouse orbit camera (yaw/pitch), third-person follow
  - Grenade throwing with upward pitch and collision ignore
  - Footstep loop while moving and throw SFX
  - Health bar HUD (top-left, 2 HP) with pause on death
- Enemy
  - NavMeshAgent chase/return with hysteresis
  - Ballistic grenade throws toward player
  - Ragdoll on death (disables AI/Animator, enables rigidbodies)
- World interactions
  - Chest grants an invisible key
  - Door unlock with key; pauses the game
  - Mines explode on touch (player/enemy)
- Explosions
  - Damage player/enemies, apply explosion force
  - Explosion VFX and SFX
- Audio
  - Global ambience (looping, optional fade-in)
  - Explosion SFX for grenades and mines

## Requirements

- Unity 2021.3+ (LTS recommended)
- Baked NavMesh (Window → AI → Navigation → Bake)
- Tags:
  - Player → "Player"
  - Enemy → "Enemy"

## Installation

1. Open the project in Unity.
2. Bake the NavMesh.
3. Scene setup:
   - Player prefab: CharacterController, MovementInput, PlayerHealth, child Camera.
   - Enemy prefab: NavMeshAgent + Ennemy script.
   - Grenade prefab: Sphere + Rigidbody + Grenade script + explosion VFX/SFX.
   - Mine prefab: Collider (isTrigger) + Mine script + explosion VFX/SFX.
   - Chest with KeyScript; Door with Unlock (optional Animator).
   - AmbienceAudio on an empty GameObject with your ambience clip.
   - HUD Canvas with a Slider named “HealthSlider” or assign it in PlayerHealth.

## Controls (AZERTY/ZQSD)

- Z: Move forward
- S: Move backward
- Q: Strafe left
- D: Strafe right
- Mouse: Orbit camera (yaw/pitch)
- Left Mouse or G: Throw grenade
- E: Interact (open chest / unlock door)

## Scripts Overview

- MovementInput.cs: Player movement (ZQSD), camera orbit, grenade throwing, audio (footsteps/throw)
- Grenade.cs: Impact explosion, damage/force, VFX/SFX
- Mine.cs: Trigger explosion, damage/force, VFX/SFX
- Ennemy.cs: NavMesh chase/return, grenade throws, ragdoll death
- PlayerHealth.cs: 2 HP, HUD slider, pause on death
- KeyScript.cs / Unlock.cs: Chest grants key; door unlock pauses game
- AmbienceAudio.cs: Global ambience with optional fade-in
- UIOverlaySetup.cs: Ensures HUD renders above world elements

## Notes

- Ensure a single AudioListener (usually on the main camera).
- HUD Canvas should be Screen Space Overlay or use UIOverlaySetup with high sorting order.
- Tune forces, cooldowns, and smoothing values in the Inspector.
- Assign all referenced prefabs (grenade, mine, prompts, explosion VFX/SFX) in the Inspector.
