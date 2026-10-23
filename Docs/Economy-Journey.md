# Yard economy timings

24 October 2026. These are rule timings from `JourneyCatalog` and the automated journey test. They are not a timed session with a child.

The careful scripted path, ignoring walking and reading:

| Step | Rule time |
| --- | --- |
| First stone in hand | First swing, no machine wait |
| Four pieces of diamond rock | One extra 0.35 second tool rest |
| First rough diamond | Crush 1.6 seconds, wash 1.6, sort 1.8, about 5.7 seconds after the rock is stored |
| First polished diamond | Cut 1.4 and polish 1.4 after that |
| First ring | Silver refine 1.6 and bench 2.0 after a silver bar and a bright diamond are ready |
| Sixth bunker | Ring sells for 120 coins. Plot 5 costs 80 while no extra bunkers are owned |

The automated test runs each machine batch for 3 seconds instead of the minimum. Even with that padding, the action clock was still under 20 seconds when the ring craft had started. After reload, the ring was granted once. Selling it left 40 coins after the bunker purchase.

Selling all 24 pieces of starter diamond rock pays 144 coins, which also buys plot 5. Crafting pays better per stone. Selling raw goods still works.

A player with no deposit and no coins can sweep the path for 8 stone every 15 seconds and sell it for 1 coin each. That does not use an advertisement.

Polish grade is 1 at machine level 0 and stops at 4. Grade 5 is not produced.

Plot price is `80 + 25 * square root(paid bunkers) + 15 * region`. It is not an exponential ladder. Owning every plot is not required.

Offline crew mining uses the same deposit counters as play. An eight-hour advance on the stone bunker stopped at the 80 stone reserve and did not add more after save and reload.
