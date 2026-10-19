Dredge in GTA

GUDEV FiveM Mod Jam 2026 - Dafydd (solo)

Trailer: Dredge-Trailer.mp4

About

Halloween fishing horror mod based on Dredge, written in C#. You take a boat out at night and fish, and the longer you're out the more scary stuff happens.

There's a dread meter that goes up slowly while you're at sea. Catching a fish lowers it, missing one raises it, and scare events raise it while they're running. It has four levels (Calm, Uneasy, Fearful, Doom) and each scare has a minimum level it needs. Above ~85% there's a chance of the endgame event, which kills the engine and lights, fades to black, plays a sound and kills the player.

Scares (random, weighted by dread level):

Ghost boat
Distant light
Lights out
Engine cut
Boat jolts/tremors
Radio static

Each scare is built from a few reusable effect classes (prop spawning/movement, world lighting and fog, screen dimming, boat forces), and the endgame reuses them too.

Commands
/start - start the game
/dread <number> - set dread (e.g. /dread 90)
/scare <name> - trigger a scare
/scares - list scares
/endgame - trigger the endgame
/clear - clear active effects

Known issues / todo
Game can only be started with a command
Selling at the docks resets the fish count but doesn't give money yet
Some scares need more tuning
Todo: money, boat upgrades, more fish types, a proper way to start a run
Credits

Music in trailer: "Creepy" by Benjamin Tissot, Bensound, license code 6KFXNLGT5CU5W7FT
