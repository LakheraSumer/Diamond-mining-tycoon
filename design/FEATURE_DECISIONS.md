# Original feature decisions

Owner decision, 20 October 2026: ages 5+ with broad family appeal, primarily children. This is a product audience, not a submitted store content rating. No adult-only assumption or neutral age screen is used to weaken the child-directed design.

The supplied APK is Uptodown App Store, not verified Lumber Inc. Reference gameplay is **unknown** throughout this table. Local implementation and automated results are evidence for our game only.

| Feature and supporting evidence | Player benefit / potential frustration | Original adaptation | Complexity | Decision / state |
| --- | --- | --- | --- | --- |
| Staged production; original simulation and conservation tests | Visible cause and effect; queues can confuse young players | Tap/automate excavation → carts → crush/wash/recover → gem finishing → vault → automatic dispatch | Medium | Implemented; core tests passed |
| Transport; cart phase state and capacity checks | Moving carts explain waiting; transport can stall the whole chain | Shared resource-driven cart route with up to eight pooled carts; blocking at processor capacity | Medium | Implemented; core tests passed |
| Automation; `hire` upgrade in economy JSON | Mine continues without repeated tapping; paid-only automation would exclude players | Coin-funded crew unlock in every mine; no ad prerequisite | Low | Implemented; pacing tested without ads |
| Upgrades; geometric costs and previews | Meaningful choices; unclear affordability is frustrating | Buy 1, 10 or maximum; exact shared cost checks, previews and bottleneck text | Medium | Implemented; numeric regressions passed |
| Orders; `orders` config and `OrderService` | Finite, readable goals; deadlines create pressure | Six gem-box delivery bonuses, credited once for diamonds already dispatched; no timers, expiry or second resource sale | Low | Implemented; save, duplicate and prestige tests passed |
| Ore grade and recovery; per-mine yield values | Different mine identities; too many linked sliders obscure choices | One combined recovery yield; Deep Core has a seam upgrade. No geology simulator | Low | Redesign implemented |
| Diamond quality; coin value upgrade | Finishing feels useful; multiple gem currencies confuse beginners | One diamond resource and one coin wallet; quality represented by value rather than random rarity or loot boxes | Low | Redesign implemented |
| Expansion; three palettes and shared mine definitions | New places and bottlenecks; duplicate implementations become brittle | Warmstone Drift, Crystal Cavern, Deep Core; open mines simulate together | Medium | Implemented; unlock and progression tests passed |
| Offline; eight-hour configurable cap and transactional save path | Return to progress; compulsory ads or duplicate grants are harmful | Simulate actual equipment and existing stocks, commit base earnings automatically, then show report | Medium | Implemented; caps, clock anomalies and interruption tests passed |
| Prestige; explicit preview and confirmation | Longer term goal; accidental reset is costly | Optional mastery reset preserving settings, tutorial, claimed goals and delivery bonuses | Medium | Implemented; core tests passed; device confirmation QA not run |
| Ads; owner audience and release compile policy | Optional monetization; ads complicate child privacy and interrupt play | Ad-free version-one release. Local labeled test provider behind editor/development gate | Low now / high for production | Production ads deferred; test-provider cases passed |
| Analytics, accounts, cloud and backend | Potential operations insight; unnecessary data and login friction | None in version one. No valuable protected online operation exists | High | Exclude from current release; future trust design documented |
| Original presentation; source-authored meshes, faceted gems and synthesized audio | Readable mining scene; low visual polish may limit appeal | Original low-poly scene, color palettes and sound cues; no extracted assets | Medium | Source implemented; initial desktop Play succeeded. Full portrait presentation/audio review pending |
| Accessibility; safe areas, settings and explicit close controls | Usable on tablets and touch devices; text volume remains a risk at age five | Portrait adaptive canvas, sound levels, reduced motion, haptics and quality choices | Medium | Implemented; phone/tablet/iPad and child usability QA not run |

No cryptocurrency, gambling, cash-out, purchases, subscriptions, chat, guilds, multiplayer or leaderboards. No forced interstitials or gameplay timers that require a purchase. Industrial details are stylized fiction.

Outstanding design review: a five-year-old may need reading assistance. Validate icon comprehension, text length, motor accessibility and session pacing with supervised family playtests before making learning or age-suitability claims. Final art, tutorial clarity and store name availability remain owner-review items.
