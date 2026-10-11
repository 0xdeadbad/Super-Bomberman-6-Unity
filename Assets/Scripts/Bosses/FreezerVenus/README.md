# Freezer Venus

The `FreezerVenus` prefab is referenced by `World3HallStageSevenSequence` in
`Stage_World3Hall`. Stage 3-8 is a hall encounter, not a separate scene. After
Pretty Bomber rises in her death pose, the hall waits for `FreezerVenusIntro`
before restoring player movement, colliders and bomb placement. During absorption, Pretty Bomber renders in front of Venus and the beam, blinks progressively faster, fades out and becomes nearly white with a pale yellow tone through a URP 2D sprite shader.

The boss descends with closed eyes, extends and widens a light cone, lifts and
fades Pretty Bomber, retracts the cone, opens her eyes and smiles. All new timers
and movement respect pause. The transparent yellow beam is a Point-filtered 16 PPU pixel texture from the red crown gem to the original portal floor, with a rounded elliptical base and a brighter pixel rim to give the cone depth. Descent stops one tile higher and starts shortly after Pretty emerges.

Combat starts with 10 HP in Normal and 12 HP in Hard/Hardcore campaign encounters. Hard/Hardcore also multiplies movement speed by 1.5 (2.1 to 3.15 units/second), shortening travel between attacks; casting and recovery timings stay the same. Boss Rush does not apply these campaign modifiers. A hit grants 0.85 seconds
of invulnerability. She flies in eight directions over pillars and bombs between attacks, choosing upper-half destinations on four of every five trips and periodically approaching the closest living player. Destinations exclude indestructible and destructible tiles so she stops on attackable floor; flight still crosses obstacles and bombs. Her hitbox is at her feet, aligned with a black pixel shadow that grows in integer pixel steps during descent and blinks together with her body on death. All positions snap to 1/16 world units while movement retains fractional progress internally. Casting starts immediately upon reaching a destination. Ice preparation distributes the ten configured Ice Cast frames evenly across 1.1 seconds (0.11 seconds each); after firing, she stays still for another 1.1 seconds, holding DollCast4 during firing and for 0.94 seconds after it, then showing DollCast0 for the final 0.16 seconds of recovery. Attacks:

- A 5-unit/second tornado born at the boss's ground tile. Its visual sits half a tile above the ground collider. It waits one second, then follows tile centers toward the nearest living player. Breadth-first routes avoid missing ground, indestructibles, destructibles and bombs. Turns pause for 0.18 seconds; a bomb appearing in an active segment makes it pause and return to the previous center before rerouting. It expires after six active seconds or an explosion.
- Six downward ice shards at 7.6 units/second, rotating 90 degrees every 0.25 seconds. Ice uses the SunMask star's centered radius-0.3 circular trigger and passes through pillars, bombs and explosions.
- Four invocations, born simultaneously in the nearest four clear columns after the summon effects. Each uses FreezeVenusInvocation.png at native 16 PPU, walks exactly one tile down with frames 1,2,1,3, then stops for one jump sequence: 4,5,6,7,8,7,6,5,4 followed by the three 32x32 frames in the second row. The sequence runs once in 0.2 seconds. The invocation then holds the third second-row sprite and immediately advances down at a constant 6.6 units/second (3x its initial walking speed), without acceleration. Explosions hold the ninth first-row sprite for five 0.1-second frames, then play the shared SmallEnemiesDie finish used by Funya. Bomb or wall contact disables damage immediately and plays Summon_1..5 at 0.1 seconds per frame. On bomb contact, invocations also kick downward through Bomb.StartKick with the same blocking masks and parameters used by BombKickAbility.

Ice, IceCast and SummonCast each use a configurable 3x one-shot gain, multiplied by the player SFX setting.

Ice flies across interior pillars; grounded attacks obey the grid. All attacks expire at the arena boundary or their lifetime limit and are removed on boss death/disable. Player hits use the existing mount
damage path. Defeat lasts six seconds plus the final explosion lifetime. Each explosion plays once, uses the encounter's pause-aware clock and is destroyed at the end. It uses the existing boss explosion sprites and `BossEndStageSequence`,
records the virtual stage `Stage_3-8`, and follows the current campaign ending
through `GameManager`, without introducing a separate save mechanism.

## Authoring

`Tools > Bosses > Rebuild Freezer Venus` imports the supplied sheet and rebuilds
the prefab. Sprite IDs remain stable on repeat imports. Import settings are
16 PPU, Point filtering, no mipmaps, Default compression None and no platform
overrides. The original PNG is copied intact; sprite rectangles exclude the
sheet's annotations. `Wire Freezer Venus To Open Hall` updates the reference in
the open World3Hall scene and saves it.

The prefab exposes descent, light and absorption durations, movement speed, sprite sequences and audio. Shared damage, explosion and
victory audio come from the SunMask prefab. Intro timing is independent of the
music's length.

## Research and adaptation

Requested references:

- https://bomberman.fandom.com/wiki/Freezer_Venus
- https://bomberman.fandom.com/pt-br/wiki/Freezer_Venus
- https://www.youtube.com/watch?v=u-q3SatZVh0

Those URLs could not be retrieved by the research tool during implementation;
the video was not viewed. The accessible behavior description at
https://game.bad-person.net/gamenote/superbom3/superbom3_boss5.html describes
pursuit, destructible homing tornadoes lasting about six seconds, six downward
ice shots and dolls that kick bombs. The supplied sprite sheet also contains
the corresponding casting and tornado frames.

This implementation adapts those mechanics to this project's hall: an explicit
three-attack cycle makes every pattern available, and the small copies use
reduced native-pixel boss sprites from the supplied sheet. The requested Pretty Bomber
absorption and 10 HP are intentional project-specific choices, not claims of
frame-perfect reproduction of Super Bomberman 3.

## Validation

Four NUnit checks cover routes around pillars, sealed corridors, the nearest four open lanes and blockers above the spawn point. They were invoked directly through Unity MCP and passed. The current Unity Test Framework runner returned zero discovered tests for this predefined assembly. Live Play Mode also verifies the intro pixel texture, crown origin, foot hitbox and shadow.

## Hard/Hardcore Queen Oyobee phase

Below half of the initial 12 HP (5 HP or less), Venus cancels her active attack,
clears its projectiles, displays the supplied transformation sprite and becomes
Queen Oyobee without recovering health. Transformation grants temporary external
invulnerability and uses pause-aware timing. The red low-health tint is replaced
by this phase in Hard/Hardcore; Normal and Boss Rush retain Venus behavior.

Queen no longer pauses or switches to a hurt pose on damage and receives three seconds of hit invulnerability. She moves 40% faster than Hard Venus (4.41 units/second). Her cycle uses one ice cast followed by two crown throws, with shorter preparation and recovery.

Transformation lasts 2.2 pause-aware seconds: an icy pixel ritual ring, accelerating flashes, a cold tint pulse and the Queen music precede the reveal. A single light-blue circular wave expands from the Queen across the arena at 11 units/second, rasterized at 16 PPU with a bright crest, cyan body and trailing frost pixels. Contact freezes every living active player for 3.2 seconds, bypassing temporary health/stun protection for this encounter event only; health immunity itself is preserved. Existing planted bombs retain their normal fuses and explosion damage; freezing provides no explosion immunity.

Each ordinary ice cast releases twelve snowflakes: six arena hazards and six pursuing shots. The hazards select six distinct ground destinations without indestructible/destructible tiles, depart at 0.11-second intervals after the 0.45-second orbit and move continuously at 1.6–2.25 units/second. Each stops only upon reaching its own destination and remains hazardous for seven seconds after arriving, fading during the last second. They are not consumed by player contact. The six pursuing shots start at evenly spaced angles around the Queen, launch toward distributed living-player targets at 4.5 units/second and steer at at most 20 degrees/second. If pursuit would require a turn greater than 55 degrees, they permanently continue straight. They expire after five flight seconds or after leaving the arena once they have entered its floor bounds. Ordinary snow freezes for two seconds and respects existing protection; 1.5 seconds of protection follows thawing. All projectile and boss positions snap to 1/16 units while retaining fractional logical movement, so reaching a tile never teleports the snowflake.

The crown uses five waypoint patterns with mirrored variants (triangle, diamond, rectangle, crossing sweep and zigzag). Waypoints use clear arena floor and every path ends at the launch head position. The base route expands by sqrt(2) in both axes to double its nominal area, constrained to arena floor near boundaries. Flight duration is calculated from the original route at 19 units/second (minimum 0.7 seconds) and is preserved after expansion, increasing speed automatically. A 0.72-second preparation shows the exact path as a subtle gold line with dark pixel accents, rasterized at native 16 PPU with Point filtering. It stays visible through flight and is removed on return, defeat or disable. Collision sweeps prevent tunneling and each throw damages a player at most once using the existing health/mount path. Queen retains remaining HP and the existing ending sequence; Normal and Boss Rush do not use this phase.

The supplied MP3 is assigned to the prefab and replaces the current music at the
start of transformation. `Tools > Audio > Calibrate Queen Oyobee Music` measures
only that clip against Robot Amusement Park Loop at volume 0.4, using the same
representative RMS calculation as Calibrate Stage Musics. The full calibration
command includes the Queen clip. `Tools > Bosses > Configure Queen Oyobee
Transformation` imports and wires the phase while preserving other prefab values;
Rebuild Freezer Venus also configures it. Imported sheets use 16 PPU, Point,
Default compression None and no platform overrides. The original Queen sheet is
preserved; a separate working sheet removes its magenta/orange background.

## Queen attack revision checks

Nine encounter Edit Mode checks passed by direct NUnit-method invocation through Unity MCP, including distinct clear snow destinations, limited steering, expanded crown geometry and wave arrival. In Play Mode, the six ground snowflakes reached separate destinations at approximately 2.08–4.76 seconds without teleporting; every sampled collider/render position stayed on the 16 PPU grid. A crown retained the exact original flight duration after expansion. A six-player runtime fixture verified that the wave froze all six even with health immunity and freeze protection enabled, while preserving health immunity. The light-blue wave was visually inspected in the hall; a full multiplayer balance run was not performed.
