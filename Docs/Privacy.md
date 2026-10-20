# Privacy and store disclosure draft

20 October 2026. Owner audience: ages 5+, mainly children, with broader family appeal. This is a children-directed product decision, not an assigned store rating or a completed legal/store review. This policy is a draft; the placeholder URL and support contact must be replaced before submission.

## Current implementation

Progress, settings, UTC offline timestamps, one-time local reward/order markers and backups stay in the app's local save directory. Readable corrupt copies may be retained locally for recovery. No account, login, cloud transfer, analytics SDK, advertising SDK, purchase SDK, integrity SDK or reward backend is present. No gameplay upload, tracking prompt, device identifier collection or social/chat feature is implemented. Uninstalling/clearing app data can remove all local recovery copies. Haptics and separate music/effect controls are optional.

Release builds disable development reward offers/grants. Test APKs/editor builds may expose a clearly labeled local reward simulator; it never contacts an ad network. Base offline earnings do not depend on this simulator. The external Store page button was removed. No outward privacy/store URL is opened by the release UI; credits/privacy are readable in-app.

Android no longer forces INTERNET permission. Inspect the actual merged release manifest before declaring final permissions: development builds or engine/package behavior may differ. There is no AD_ID or ATT configuration in the game. Inspect the packaged app and real network behavior before final Data safety answers.

The project iOS privacy manifest declares no tracking and no collected data; file-container timestamp API reason C617.1 covers local file operations. Unity also supplies engine declarations. Review the complete exported/archive privacy report, manifests and linked SDKs, rather than treating the source manifest as a completed Xcode audit.

## Child audience decision

Version-one release is ad-free with no analytics or personal-data service. [Google Play Families requirements](https://support.google.com/googleplay/android-developer/answer/9893335?hl=en) apply when the target includes children; mixed-audience advertising introduces additional screening and certified-SDK obligations. [Apple's review guidelines](https://developer.apple.com/app-store/review/guidelines/) restrict external links and third-party ads/analytics in children's experiences. [FTC guidance](https://www.ftc.gov/business-guidance/resources/complying-coppa-frequently-asked-questions) describes protections for child-directed services collecting personal information. Broad adult appeal does not make a mainly child-directed app an adult-only service.

Do not add ads, analytics, identity, support data collection or attestation identifiers without reviewing actual data flows, child privacy obligations, SDK terms and store disclosures. A parental gate for leaving the app is not equivalent to verifiable parental consent for collecting data. No claim of legal certification is made.

## Draft store worksheet

| Question | Draft answer based on this source | Required confirmation |
| --- | --- | --- |
| Play Data safety: collection/sharing | No developer-operated collection or sharing implemented | Review final binary/engine/SDK traffic and current form definitions |
| Accounts / deletion | No account; reset and device app-data removal manage local data | Test reset/update/uninstall behavior |
| Advertising | No production advertising in release | Inspect release compilation and linked packages |
| Tracking / IDFA / advertising ID | Not implemented or requested | Inspect final manifests and runtime |
| Apple collected-data labels | Candidate: data not collected | Review real Xcode privacy report and final SDK behavior |
| Required-reason APIs | Local file operations plus Unity's engine reasons | Verify archive aggregation and current approved reasons |
| Intended age groups / Kids category | Primarily children ages 5+, family appeal | Owner must choose truthful supported age bands and review category/rating questionnaires |
| Privacy URL / support | Not assigned | Publish an owner-approved policy and monitored contact before submission |

No store form has been submitted. Data safety answers must describe the actual shipped build and any future SDK, not merely this draft.
