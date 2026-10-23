# Save migration to world version 2

24 October 2026.

Save schema version is now 2. Version 0 and 1 saves still load. A save newer than version 2, or a world generator newer than the one in this build, is left untouched.

## What stays

Coins, mastery, lifetime totals, tutorial progress, settings, works upgrades, works buffers, claimed objectives, and delivery orders stay. The three works mines stay the works mines. Opening a starter bunker does not unlock Crystal Cavern or Deep Core.

## Starter bunkers

A new game owns plots 0 through 4 in region 0 exactly once. The flag `starterGrantApplied` is saved. Loading, restoring a backup, or running the grant again does not add another five.

Those plots are hand-authored, not a random roll:

| Plot | Role | Useful reserves |
| --- | --- | --- |
| 0 | Stone and iron | Stone 80, iron ore 40, gravel 40. Also the saved Warmstone Drift area. |
| 1 | Copper | Copper ore 36, stone 24, gravel 16 |
| 2 | First diamond rock | Diamond rock 24, gravel 20, stone 12. Also the saved Crystal Cavern area for the map, without unlocking that works mine. |
| 3 | Silver | Silver ore 20, stone 20, gravel 16 |
| 4 | Mixed jewellery start | Gold ore 10, silver ore 10, diamond rock 12, copper ore 12 |

Plot 2 holds enough diamond rock for five recovered diamonds and one ring, with rock left over.

## Existing saves

If the save has no `adventure` record:

- The five starter plots are granted once.
- If Deep Core was already unlocked, plot 5 is marked owned and no coins are charged. That is the old expansion, not a second starter pack.
- Currency and works upgrades are not reset.
- A short note is stored when the old save had progress.

Reloading that save after it has been written as version 2 does not grant the plots again.

## What is not stored

Generated ground for all 102,400 plots is not stored. The save stores the world seed, generator version, an ownership bit set, and reserve overrides only for bunkers that have been mined. Unmined owned bunkers keep the reserves from that generator version. Generator version 1 stays available so an update does not move those deposits.

## Prestige

Prestige still clears works coins, works ore, works diamonds, and works upgrade levels. It also clears factory machine levels and the hired yard crew. It keeps bunker ownership, mined reserves, pocket goods, cosmetics, claimed yard tasks, and the lifetime count of diamonds first recovered from ore. The preview text says this before the player confirms.

## Rollback

The existing temp-file, backup, and checksum path is unchanged. If the new save fails to write, the live mine is not replaced. A broken primary file still falls back to the backup. A newer file is not overwritten.
