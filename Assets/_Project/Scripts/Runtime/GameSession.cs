using System;
using System.IO;
using DiamondMine.Simulation;
using UnityEngine;

namespace DiamondMine.Runtime
{
    /// <summary>
    /// Owns the one live GameState. Buttons call these methods; they do not change balances themselves.
    /// Animations read the state after ProductionSimulator advances it.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public const float AutosaveSeconds = 20f;

        public EconomyConfig Config { get; private set; }
        public GameState State { get; private set; }
        public bool Ready { get; private set; }
        public bool NewerSaveBlocked { get; private set; }
        public bool OfflinePending { get { return offlinePending; } }
        public string StatusMessage { get; private set; }
        public string SaveError { get; private set; }
        public OfflinePlan OfflineReport { get; private set; }
        public bool OfflineReportOpen { get; private set; }
        public PendingReward PendingReward { get { return ads == null ? null : ads.Pending; } }

        IGameClock clock;
        SaveGameStore store;
        IRewardedAdProvider ads;
        string savePath;
        bool suspended;
        bool offlinePending;
        float saveTimer;
        float bottleneckVisible;
        float objectiveVisible;
        bool dirtySave;

        public void Configure(IGameClock gameClock)
        {
            clock = gameClock ?? new SystemGameClock();
        }

        void Awake()
        {
            if (clock == null)
                clock = new SystemGameClock();
            Boot();
        }

        public void Boot()
        {
            Ready = false;
            NewerSaveBlocked = false;
            StatusMessage = "";
            SaveError = "";
            OfflineReport = null;
            OfflineReportOpen = false;
            ads = new LocalTestRewardedAds(DevelopmentFeatures.TestAdsAvailable);

            TextAsset asset = Resources.Load<TextAsset>("economy");
            if (asset == null)
            {
                StatusMessage = "The economy configuration is missing from the build.";
                return;
            }

            try
            {
                Config = EconomyLoader.Load(asset.text);
            }
            catch (Exception ex)
            {
                StatusMessage = "The economy configuration could not be read. " + ex.Message;
                return;
            }

            savePath = Path.Combine(Application.persistentDataPath, "diamond-mine-save.json");
#if UNITY_EDITOR
            string reviewRoot = Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT");
            if (!string.IsNullOrEmpty(reviewRoot))
                savePath = Path.Combine(reviewRoot, "diamond-mine-save.json");
#endif
            store = new SaveGameStore(new SystemSaveIO(), savePath);
            LoadOutcome loaded = store.Load(Config);
            if (loaded.Status == LoadStatus.NewerVersion)
            {
                NewerSaveBlocked = true;
                StatusMessage = string.IsNullOrEmpty(loaded.Message)
                    ? "This save was written by a newer version of the game. It was left untouched."
                    : loaded.Message;
                return;
            }

            State = loaded.State ?? GameFactory.NewGame(Config);
            State.Version = SaveCodec.CurrentVersion;
            if (!string.IsNullOrEmpty(loaded.Message) && loaded.Status != LoadStatus.Ok && loaded.Status != LoadStatus.NewGame)
                StatusMessage = loaded.Message;

            EnsureActiveMine();
            TutorialService.Reconcile(State, Config);
            ApplyOfflineOnLaunch();
            QualityPreset.Apply(State.Settings.Quality);
            Ready = true;
            StampAndSave();
        }

        void Update()
        {
            if (!Ready || suspended || State == null)
                return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.5f);
            if (offlinePending)
            {
                saveTimer += dt;
                if (saveTimer >= 2f)
                {
                    saveTimer = 0f;
                    ApplyGapSinceStamp(true);
                }
                return;
            }
            if (dt <= 0f)
                return;

            try
            {
                ProductionSimulator.Advance(State, Config, dt);
            }
            catch (Exception ex)
            {
                Ready = false;
                StatusMessage = "The mine simulation stopped. " + ex.Message;
                return;
            }

            int claimed = ObjectiveService.ClaimCompleted(State, Config);
            int stepBefore = State.TutorialStep;
            RememberMarkedStage();
            TutorialService.Reconcile(State, Config);
            if (claimed > 0 || State.TutorialStep != stepBefore)
                dirtySave = true;

            saveTimer += dt;
            if (dirtySave || saveTimer >= AutosaveSeconds)
            {
                saveTimer = 0f;
                dirtySave = false;
                StampAndSave();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (!Ready)
                return;
            if (paused)
                Suspend();
            else
                Resume();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!Ready)
                return;
            if (!focused)
                Suspend();
            else
                Resume();
        }

        void OnApplicationQuit()
        {
            if (!Ready || NewerSaveBlocked)
                return;
            Suspend();
        }

        void Suspend()
        {
            if (suspended)
                return;
            suspended = true;
            AudioListener.pause = true;
            StampAndSave();
        }

        void Resume()
        {
            if (!suspended)
                return;
            suspended = false;
            AudioListener.pause = false;
            ApplyGapSinceStamp(showReport: true);
        }

        public void NotifyBottleneckVisible(float dt, bool covered)
        {
            if (!Ready || State.TutorialStep != 4 || covered)
            {
                bottleneckVisible = 0f;
                return;
            }

            bottleneckVisible += dt;
            if (bottleneckVisible < 2f)
                return;
            TutorialService.NotifyBottleneckShown(State);
            TutorialService.Reconcile(State, Config);
            dirtySave = true;
        }

        public void NotifyObjectiveVisible(float dt, bool covered)
        {
            if (!Ready || State.TutorialStep < 7 || State.TutorialComplete || covered)
            {
                objectiveVisible = 0f;
                return;
            }

            objectiveVisible += dt;
            if (objectiveVisible < 1.5f)
                return;
            TutorialService.NotifyObjectiveShown(State);
            TutorialService.Reconcile(State, Config);
            dirtySave = true;
        }

        public void OpenObjectives()
        {
            if (!Ready)
                return;
            TutorialService.NotifyObjectiveShown(State);
            TutorialService.Reconcile(State, Config);
        }

        public bool TryTap()
        {
            if (!CanPlay())
                return false;
            bool mined = ProductionSimulator.TryTap(State, Config, State.ActiveMineId);
            if (!mined)
                return false;
            TutorialService.Reconcile(State, Config);
            PlatformInfo.Pulse(State.Settings.Haptics);
            return true;
        }

        public bool TryBuy(string upgradeId, int count)
        {
            if (!CanPlay() || string.IsNullOrEmpty(upgradeId))
                return false;
            PurchasePreview preview = UpgradeService.Preview(State, Config, State.ActiveMineId, upgradeId, count);
            if (count == 10)
            {
                MineDefinition def = Config.FindMine(State.ActiveMineId);
                UpgradeDefinition upgrade = def != null ? def.FindUpgrade(upgradeId) : null;
                MineState mine = State.FindMine(State.ActiveMineId);
                if (upgrade == null || mine == null)
                    return false;
                int room = upgrade.MaxLevel - mine.Level(upgrade.Id);
                int affordable = UpgradeService.MaxAffordable(State.Coins, mine.Level(upgrade.Id), upgrade.MaxLevel, upgrade.BaseCost, upgrade.CostGrowth);
                if (room < 10 || affordable < 10 || !preview.Affordable || preview.Count != 10)
                    return false;
            }
            else if (!preview.Affordable || preview.Count < 1)
            {
                return false;
            }

            bool saved = Commit(delegate (GameState clone)
            {
                UpgradeService.TryBuy(clone, Config, State.ActiveMineId, upgradeId, count);
                ObjectiveService.ClaimCompleted(clone, Config);
                TutorialService.Reconcile(clone, Config);
            });
            if (saved)
                PlatformInfo.Pulse(State.Settings.Haptics);
            return saved;
        }

        public bool TryUnlock(string mineId)
        {
            if (!CanPlay())
                return false;
            string reason;
            if (!MineUnlocks.CanUnlock(State, Config, mineId, out reason))
                return false;
            return Commit(delegate (GameState clone)
            {
                MineUnlocks.TryUnlock(clone, Config, mineId);
                clone.ActiveMineId = mineId;
                ObjectiveService.ClaimCompleted(clone, Config);
                TutorialService.Reconcile(clone, Config);
            });
        }

        public bool TrySelectMine(string mineId)
        {
            if (!CanPlay())
                return false;
            MineState mine = State.FindMine(mineId);
            if (mine == null || !mine.Unlocked || State.ActiveMineId == mineId)
                return mine != null && mine.Unlocked;
            State.ActiveMineId = mineId;
            StampAndSave();
            return string.IsNullOrEmpty(SaveError);
        }

        public bool TryPrestige()
        {
            if (!CanPlay())
                return false;
            if (!PrestigeService.Preview(State, Config).Available)
                return false;
            return Commit(delegate (GameState clone)
            {
                PrestigeService.TryPrestige(clone, Config);
                ObjectiveService.ClaimCompleted(clone, Config);
                TutorialService.Reconcile(clone, Config);
            });
        }

        public bool TryClaimOrder(string orderId)
        {
            if (!CanPlay()) return false;
            foreach (OrderDefinition order in Config.Orders)
            {
                if (order.Id != orderId || !OrderService.CanClaim(State, order)) continue;
                return Commit(delegate (GameState clone) { OrderService.TryClaim(clone, Config, orderId); });
            }
            return false;
        }

        public void UpdateSettings(double music, double sfx, bool haptics, bool reducedMotion, int quality)
        {
            if (!CanPlay())
                return;
            State.Settings.Music = Clamp01(music);
            State.Settings.Sfx = Clamp01(sfx);
            State.Settings.Haptics = haptics;
            State.Settings.ReducedMotion = reducedMotion;
            State.Settings.Quality = Mathf.Clamp(quality, 0, 2);
            QualityPreset.Apply(State.Settings.Quality);
            dirtySave = true;
        }

        public void DismissOfflineReport()
        {
            OfflineReportOpen = false;
        }

        public bool CanOfferOfflineBonus()
        {
            return DevelopmentFeatures.TestAdsAvailable && Ready
                && State.LastOfflineBaseCoins > 0d
                && !State.LastOfflineBonusClaimed
                && !string.IsNullOrEmpty(State.LastOfflineGrantId);
        }

        public string OfflineBonusLabel()
        {
            if (!CanOfferOfflineBonus())
                return "";
            double bonus = State.LastOfflineBaseCoins * Config.OfflineAdBonusFraction;
            return "Test ad: +" + NumericFormat.Format(bonus) + " extra offline coins";
        }

        public string BoostOfferLabel()
        {
            if (!DevelopmentFeatures.TestAdsAvailable || !Ready || State.LifetimeCoins <= 0d)
                return "";
            return "Test ad: " + Config.BoostMultiplier.ToString("0") + "x production for " +
                Mathf.RoundToInt((float)Config.BoostDurationSeconds / 60f) + " min";
        }

        public void BeginOfflineAd()
        {
            if (!CanPlay() || PendingReward != null || !CanOfferOfflineBonus())
                return;
            double bonus = State.LastOfflineBaseCoins * Config.OfflineAdBonusFraction;
            ads.TryBegin(new PendingReward
            {
                Kind = RewardKind.OfflineBonus,
                GrantId = State.LastOfflineGrantId,
                Token = "",
                Description = "Extra offline coins: " + NumericFormat.Format(bonus) + ". The coins already added stay either way."
            });
        }

        public void BeginBoostAd()
        {
            if (!DevelopmentFeatures.TestAdsAvailable || !CanPlay() || PendingReward != null || State.LifetimeCoins <= 0d)
                return;
            ads.TryBegin(new PendingReward
            {
                Kind = RewardKind.ProductionBoost,
                GrantId = "",
                Token = "boost-" + Guid.NewGuid().ToString("N"),
                Description = Config.BoostMultiplier.ToString("0") + "x mining, hauling, processing, sorting, and sales for " +
                    Mathf.RoundToInt((float)Config.BoostDurationSeconds / 60f) + " minutes. Watching again refreshes the timer. It does not stack."
            });
        }

        public bool ResolveTestAd(TestAdOutcome outcome)
        {
            if (!DevelopmentFeatures.TestAdsAvailable || !CanPlay())
                return false;
            return ads.Resolve(outcome, CommitAdReward);
        }

        bool CommitAdReward(PendingReward pending)
        {
            GameState probe = State.Clone();
            bool canGrant = pending.Kind == RewardKind.OfflineBonus
                ? AdRewards.TryGrantOfflineBonus(probe, Config, pending.GrantId)
                : pending.Kind == RewardKind.ProductionBoost && AdRewards.TryGrantBoost(probe, Config, pending.Token);
            if (!canGrant)
                return false;

            bool granted = Commit(delegate (GameState clone)
            {
                if (pending.Kind == RewardKind.OfflineBonus)
                    AdRewards.TryGrantOfflineBonus(clone, Config, pending.GrantId);
                else
                    AdRewards.TryGrantBoost(clone, Config, pending.Token);
            });
            return granted;
        }

        public void CancelPendingAd()
        {
            if (ads != null) ads.Cancel();
        }

        public bool ResetProgress()
        {
            if (store == null || Config == null)
                return false;
            try
            {
                MoveAside(savePath);
                MoveAside(savePath + ".bak");
                if (File.Exists(savePath + ".tmp"))
                    File.Delete(savePath + ".tmp");
            }
            catch (Exception ex)
            {
                SaveError = "Progress was not reset. " + ex.Message;
                return false;
            }

            NewerSaveBlocked = false;
            store = new SaveGameStore(new SystemSaveIO(), savePath);
            State = GameFactory.NewGame(Config);
            Ready = true;
            OfflineReport = null;
            OfflineReportOpen = false;
            if (ads != null) ads.Cancel();
            StatusMessage = "Progress on this device was reset.";
            QualityPreset.Apply(State.Settings.Quality);
            return StampAndSave();
        }

        public bool StartFreshBesideNewerSave()
        {
            if (!NewerSaveBlocked)
                return false;
            try
            {
                string stamp = clock.UnixMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                MoveAside(savePath, savePath + ".held-newer-" + stamp);
                MoveAside(savePath + ".bak", savePath + ".held-newer-" + stamp + ".bak");
            }
            catch (Exception ex)
            {
                SaveError = "The newer save was left in place. " + ex.Message;
                return false;
            }

            NewerSaveBlocked = false;
            store = new SaveGameStore(new SystemSaveIO(), savePath);
            State = GameFactory.NewGame(Config);
            Ready = true;
            StatusMessage = "A new mine was started. The newer save file was kept beside it.";
            return StampAndSave();
        }

        public static void Quit()
        {
            Application.Quit();
        }

        void ApplyOfflineOnLaunch()
        {
            if (State.LastActiveUnixMs <= 0)
                return;
            ApplyGapSinceStamp(showReport: true);
        }

        void ApplyGapSinceStamp(bool showReport)
        {
            if (State == null || State.LastActiveUnixMs <= 0)
            {
                offlinePending = false;
                return;
            }
            double elapsed = (clock.UnixMilliseconds - State.LastActiveUnixMs) / 1000d;
            if (elapsed > -1d && elapsed < 1d)
            {
                offlinePending = false;
                return;
            }

            OfflinePlan applied = null;
            bool saved = Commit(delegate (GameState clone)
            {
                string previousGrant = clone.LastOfflineGrantId;
                double previousBase = clone.LastOfflineBaseCoins;
                bool previousClaimed = clone.LastOfflineBonusClaimed;
                applied = OfflineEarnings.Apply(clone, Config, elapsed);
                bool keepUnclaimedBonus = previousBase > 0d
                    && !previousClaimed
                    && applied.Coins < 1d
                    && string.IsNullOrEmpty(applied.Anomaly)
                    && !applied.Capped;
                if (keepUnclaimedBonus)
                {
                    clone.LastOfflineGrantId = previousGrant;
                    clone.LastOfflineBaseCoins = previousBase;
                    clone.LastOfflineBonusClaimed = false;
                }
                ObjectiveService.ClaimCompleted(clone, Config);
                TutorialService.Reconcile(clone, Config);
            });
            if (!saved)
            {
                offlinePending = true;
                return;
            }

            offlinePending = false;
            OfflineReport = applied;
            bool interesting = applied != null && (
                applied.SecondsSimulated >= 15d ||
                applied.Capped ||
                !string.IsNullOrEmpty(applied.Anomaly));
            OfflineReportOpen = showReport && interesting;
        }

        void RememberMarkedStage()
        {
            if (State.TutorialStep != 4 && State.TutorialStep != 5)
                return;
            if (!string.IsNullOrEmpty(State.MarkedStage))
                return;
            MineState mine = State.FindMine(State.ActiveMineId);
            MineDefinition def = Config.FindMine(State.ActiveMineId);
            if (mine == null || def == null)
                return;
            MineRates rates = MineStats.Compute(def, mine, Config, State.Mastery, State.BoostRemainingSeconds > 0d);
            StageDiagnosis diagnosis = BottleneckService.Diagnose(mine, rates);
            State.MarkedStage = diagnosis.Stage;
            State.MarkedStageLevels = UpgradeService.StageLevels(def, mine, diagnosis.Stage);
        }

        void EnsureActiveMine()
        {
            MineState active = State.FindMine(State.ActiveMineId);
            if (active != null && active.Unlocked)
                return;
            for (int i = 0; i < State.Mines.Count; i++)
            {
                if (!State.Mines[i].Unlocked)
                    continue;
                State.ActiveMineId = State.Mines[i].Id;
                return;
            }
        }

        bool Commit(Action<GameState> mutation)
        {
            long now = clock.UnixMilliseconds;
            string error;
            bool ok = SaveGameStore.Commit(State, store, delegate (GameState clone)
            {
                mutation(clone);
                clone.LastActiveUnixMs = now;
            }, out error);
            if (!ok)
            {
                SaveError = "Progress could not be saved. " + error;
                return false;
            }

            SaveError = "";
            saveTimer = 0f;
            dirtySave = false;
            EnsureActiveMine();
            return true;
        }

        bool StampAndSave()
        {
            if (!Ready || State == null || store == null || offlinePending)
                return false;
            GameState snapshot = State.Clone();
            snapshot.LastActiveUnixMs = clock.UnixMilliseconds;
            SaveOutcome outcome = store.Save(snapshot);
            if (!outcome.Success)
            {
                SaveError = "Progress could not be saved. " + outcome.Error;
                return false;
            }

            SaveError = "";
            State.LastActiveUnixMs = snapshot.LastActiveUnixMs;
            dirtySave = false;
            saveTimer = 0f;
            return true;
        }

        static void MoveAside(string path)
        {
            MoveAside(path, path + ".retired-" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        static void MoveAside(string path, string destination)
        {
            if (!File.Exists(path))
                return;
            if (File.Exists(destination))
                File.Delete(destination);
            File.Move(path, destination);
        }

        bool CanPlay()
        {
            return Ready && !offlinePending && !NewerSaveBlocked;
        }

        static double Clamp01(double value)
        {
            if (double.IsNaN(value) || value < 0d)
                return 0d;
            if (value > 1d)
                return 1d;
            return value;
        }
    }
}
