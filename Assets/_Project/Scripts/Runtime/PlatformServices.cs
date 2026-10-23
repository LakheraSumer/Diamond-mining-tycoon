using System;
using UnityEngine;

namespace DiamondMine.Runtime
{
    public interface IGameClock
    {
        long UnixMilliseconds { get; }
    }

    public sealed class SystemGameClock : IGameClock
    {
        public long UnixMilliseconds
        {
            get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        }
    }

    public static class PlatformInfo
    {
        public static bool IsTabletLayout()
        {
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            // A portrait tablet still needs a full-width, readable upgrade page.
            // Pixel density alone must not turn a high-resolution phone into a sidebar layout.
            return width / height >= 1.15f;
        }

        public static bool HapticsSupported()
        {
            return SystemInfo.supportsVibration;
        }

        public static void Pulse(bool enabled)
        {
            if (!enabled || !HapticsSupported())
                return;
            try
            {
                Handheld.Vibrate();
            }
            catch (Exception)
            {
                // A device can report vibration and still reject the call.
            }
        }

        public static string StoreLink()
        {
#if UNITY_IOS
            return Branding.IosStoreUrlPlaceholder;
#else
            return Branding.AndroidStoreUrlPlaceholder;
#endif
        }

        public static bool TryOpenStore(out string message)
        {
            string link = StoreLink();
            if (string.IsNullOrEmpty(link) || link.IndexOf("example.invalid", StringComparison.Ordinal) >= 0)
            {
                message = "The store listing is not published yet.";
                return false;
            }

            Application.OpenURL(link);
            message = "";
            return true;
        }

        public static string NetworkStatus()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.NotReachable:
                    return "Offline. Your mine keeps working. Progress stays on this device.";
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return "Connected. You can also play offline. Progress stays on this device.";
                default:
                    return "Connected. You can also play offline. Progress stays on this device.";
            }
        }
    }

    public static class QualityPreset
    {
        public static void Apply(int quality)
        {
            int level = Mathf.Clamp(quality, 0, 2);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = level >= 2 ? 60 : 30;
            QualitySettings.shadows = level == 0 ? ShadowQuality.Disable : ShadowQuality.All;
            QualitySettings.shadowResolution = level >= 2 ? ShadowResolution.Medium : ShadowResolution.Low;
            QualitySettings.shadowDistance = level >= 2 ? 18f : 8f;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }
    }
}
