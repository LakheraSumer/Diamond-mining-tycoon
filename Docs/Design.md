# Game design

Working title: Diamond Mine Tycoon. English only. One player. The mine runs without a server.

## Fantasy and pace

The player runs a small underground mine. Rock becomes ore, ore moves, a machine crushes it, diamonds are sorted and stored, then sold for coins. Coins buy better equipment and, later, miners who work without tapping. Sessions can be a minute long. Progress also continues while the app is closed, up to eight hours, and only for work the mine can actually do.

## Production chain

Mine rock, transport ore, process ore, sort diamonds, sell diamonds, reinvest coins.

Each stage has a capacity. Mining fills the face. Carts move ore. The crusher turns ore into rough diamonds at the mine's yield. Sorting fills the vault. Sales turn vault diamonds into coins. A full buffer stops the stage above it. The slowest rate sets the steady income. The interface says which stage that is, with the word SLOW plus a sentence, not color by itself.

Animations show the simulation. A cart's position does not create ore. A sound does not grant coins.

## Three mines

They share one set of rules and one save. Only unlocked mines produce. All unlocked mines produce together; the one on screen is just the one you are looking at.

- Warmstone Drift starts open. Short tunnel, carts set the pace.
- Crystal Cavern opens with lifetime coins. Richer yield, longer haul.
- Deep Core opens much later. Slow crusher, small vault, higher coin value.

## Currencies

Coins buy upgrades. Diamonds are the goods moving through the mine, not a second wallet. Mastery is the prestige reward: it speeds production and carts. It does not raise the coin value of a diamond. There is no fourth currency.

## Upgrades

Buy one, buy ten, or buy the most you can afford. Buy ten stays off unless ten levels fit and the whole price is affordable. The row shows the effect before and after. Prices grow from the economy file.

## Tutorial

The prompts are actions: tap, watch the cart arrive, see a sale, buy something, notice the slow stage, improve it, hire a miner, then read the next goal. If a step is already done, it is skipped. Closing the app does not restart a finished step. The slow-stage step waits until that line has been on screen for about two seconds, so a finished earlier action does not skip the explanation the player has not seen.

## Objectives, offline, prestige

Goals pay their coin reward once. Offline earnings use the saved mine and the device clock, with an eight-hour cap. Existing ore can still be processed without a miner. New ore offline requires a hired miner. A backward clock grants nothing. A huge forward jump still grants only the cap. The base amount is saved before the report is shown. Continue does not add it again. Editor/development builds have an optional labeled local test ad that can add half again after Complete, once per grant. Release builds offer no ads.

Prestige shows the mastery change and what resets, then asks for a second confirmation. It clears works coins, works stocks, works upgrade levels, factory machine levels, and the hired yard crew. It keeps mastery, lifetime coins, settings, finished tutorial state, claimed goals, used ad tokens, bunker ownership, pocket goods, cosmetics, and the count of diamonds first recovered from ore.

## Yard

The yard is a third-person walk around the factory, the shop, and the first bunkers. Mining inside a bunker needs ownership. Walking past a locked door does not open a purchase. The player chooses a cosmetic miner. Four of the twelve crew looks are free. The others unlock from ordinary play. Clothes do not change prices.

Region 0 has 1,024 plots and the world has 100 regions, 102,400 addresses. Only nearby doors are drawn. The rest are generated from the world seed when a map page is opened.

Diamond rock is crushed, washed, and sorted. Four pieces of rock become one rough diamond plus gravel. Cutting and polishing do not create extra diamonds. Silver ore becomes a silver bar before it can be used in a ring. Ten jewellery recipes are in the bench. The shop buys goods at fixed coin prices. Customer orders are a short scripted list.

There is no live ranking server. The practice total on the device is labeled as practice.

## What this version leaves out

Multiplayer, chat, trading, a public map of other players, subscriptions, a required login, and in-app purchases. The release is ad-free. Only editor/development builds have a labeled local reward test. A production leaderboard is not enabled.

## Family audience and orders

Owner decision: primarily children ages 5+, with broad family appeal. The current UI uses a larger 720x1280 reference and scrollable information. Six gem-box delivery contracts under Goals pay finite one-time bonuses for diamonds already dispatched, with no deadlines or extra stock consumption. Claimed contracts survive prestige. Actual child readability and touch QA remain not run.
