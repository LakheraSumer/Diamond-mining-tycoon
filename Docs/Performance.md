# Performance report

Status: actual Unity/player/device profiling **not run**. There is no measured Android or iOS FPS, frame time, GPU time, memory, battery, thermal, loading or session-growth result.

The Windows .NET automated suite completes its simulation tests in approximately three seconds. This includes deterministic simulated early/middle/four-hour progression; it is a core-rule test duration, not real-time game speed or a mobile performance benchmark. Retained timestamps and per-test durations are in tests/results/simulation.trx.

Source-level provisions: one authoritative simulation, a fixed eight-cart visual pool, reused scene geometry/materials, capped catch-up work, HUD refresh around 10 Hz, original low-poly meshes, unlit shader, bounded save input and few audio voices. Quality requests 30 FPS on low/medium and 60 on high; these are targets only. Shadow settings on an unlit scene are not proof of measured visual quality or GPU benefit.

After builds exist, agree at least one modest Android phone, an Android tablet, iPhone and iPad. Record exact device/OS/build/hash. Profile fresh start, all three mines, upgrades/scrolling, eight-hour catch-up, pause/resume and at least a 30-minute session. Capture Unity Profiler CPU/GC and memory, platform GPU tools, Android/Apple energy and thermal evidence, and startup time. Check 16 KB Android builds separately. Fix memory growth, hot/stalled sessions, UI rebuilding spikes and loading faults before release. Retain before/after measurements for any performance claim.
