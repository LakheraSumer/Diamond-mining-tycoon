using System;

namespace DiamondMine.Runtime
{
    public static class DevelopmentFeatures
    {
        public static bool TestAdsAvailable
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }
    }

    public enum TestAdOutcome
    {
        Completed,
        Cancelled,
        Failed,
        NoFill,
        Timeout
    }

    public enum RewardKind
    {
        None,
        OfflineBonus,
        ProductionBoost
    }

    /// <summary>
    /// Development stand-in for a rewarded ad. It never contacts a network.
    /// A production adapter can replace it later. Rewards are granted by GameSession
    /// only after Completed, and only through the simulation's one-time checks.
    /// </summary>
    public sealed class PendingReward
    {
        public RewardKind Kind;
        public string Token;
        public string GrantId;
        public string Description;
    }

    public interface IRewardedAdProvider
    {
        PendingReward Pending { get; }
        bool TryBegin(PendingReward reward);
        bool Resolve(TestAdOutcome outcome, Func<PendingReward, bool> commitReward);
        void Cancel();
    }

    public sealed class LocalTestRewardedAds : IRewardedAdProvider
    {
        readonly bool enabled;
        public PendingReward Pending { get; private set; }

        public LocalTestRewardedAds(bool enabled) { this.enabled = enabled; }

        public bool TryBegin(PendingReward reward)
        {
            if (!enabled || Pending != null || reward == null || reward.Kind == RewardKind.None) return false;
            Pending = reward;
            return true;
        }

        public bool Resolve(TestAdOutcome outcome, Func<PendingReward, bool> commitReward)
        {
            if (!enabled || Pending == null) return false;
            if (outcome != TestAdOutcome.Completed) { Cancel(); return false; }
            // Keep the offer for an explicit retry if its durable save failed.
            if (!commitReward(Pending)) return false;
            Cancel();
            return true;
        }

        public void Cancel() { Pending = null; }
    }

#if DIAMOND_MINE_ADMOB
    /// <summary>
    /// Reserved compile path. The Google Mobile Ads plugin is not in this project.
    /// Leave DIAMOND_MINE_ADMOB undefined until the owner approves that SDK,
    /// its data collection, and the privacy disclosures.
    /// </summary>
    internal static class AdMobAdapterGuard
    {
        internal const string Status = "The advertising SDK compile flag is set, but no adapter is implemented in this build.";
    }
#endif
}
