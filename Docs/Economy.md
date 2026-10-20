# Economy

Source file: `Assets/_Project/Resources/economy.json`. Changing that file changes the game and the tests together.

## Rules that are not in the JSON

- Mastery multiplies tap, automation, processing, sorting, sales, and cart speed. It does not multiply yield or coins per diamond. A production boost uses the same multiplier and does not stack; watching again refreshes the five minutes.
- Steady coins per second are the minimum of mining, transport, and processing, times yield, then limited by sorting and sales, times coin value. Storage changes how long a stall can last. It is not the steady rate. The buy picker still gives storage a small score so a full vault is worth fixing.
- Prestige mastery gained is the floor of the square root of coins earned since prestige, divided by `prestigeDivisor` (17500). It requires at least 70000 coins since prestige and a hired miner. The preview shows this before anything resets.
- Offline simulation steps by events, capped at eight hours by the caller. The simulator also refuses to advance more than 48 hours in one call.
- Numbers clamp at 1e300. Spending cannot go negative. Non-finite values do not get added.

## Mines

Warmstone is the starter and is transport-limited at one cart and an eight-second one-way trip. Crystal costs 4800 lifetime coins and hauls farther with a richer yield. Deep Core costs 160000 lifetime coins, crushes slowly, and pays more per diamond.

## Measured bot, not a promise

A guided bot plays without ads. The detailed run that printed timestamps, on this economy, was:

| Moment | Time |
| --- | --- |
| First sale | 8.5 s |
| First upgrade | 43.3 s |
| Hired miner | 282 s (about 4.7 min) |
| Crystal Cavern | 836 s (about 13.9 min) |
| Prestige available and taken | 2490 s (about 41.5 min), mastery 2 |
| Deep Core | 4926 s (about 82 min) |
| After 4 hours | about 129 coins/s, about 522k coins, no NaN |

The original sketch asked for a first sale near 30 seconds. Stretching the first trip to hit that number pushed the hire past five minutes, so the trip stayed at 8 seconds. The sale is early on purpose.

The automated suite passed these windows: sale under 30s, upgrade under 75s, automation between 2 and 5 minutes, second mine between 8 and 15 minutes, prestige before 3 hours, third mine before 4 hours, and a positive lifetime-coin minute after automation. Latest current run: 62 passed, 0 failed. See Economy-Results.md for newly retained pacing timestamps; the earlier table above is historical.

The bot is not a player. It buys the best payback it can see, and it does not get stuck watching ads. The mine stays playable with ads unavailable: the hire costs coins, not a view.

## Delivery bonuses

Six finite orders in the economy JSON reward milestones of diamonds already dispatched by a specific mine. Claims do not consume or resell a second copy of stock, never expire, and pay once even across prestige. They are manual local bonuses, not protected server entitlements. The pacing bot does not collect them, so the base mine remains viable without relying on those bonuses or ads.
