namespace DiamondMine.Runtime
{
    public static class InfoCopy
    {
        public static readonly string Credits =
            "Diamond Mine Tycoon\n" +
            "Version " + Branding.Version + " (" + Branding.BuildNumber + ")\n\n" +
            "This build is original work: the simulation, interface, low-poly shapes, and sounds were made for this project. " +
            "Shapes are generated in code. Sounds are synthesized in code. No paid asset-store packs are included.\n\n" +
            "Engine: Unity, used under the owner's Unity license. The Unity splash screen follows that license.\n\n" +
            "Publisher: " + Branding.CompanyPlaceholder + "\n" +
            "Support contact is not assigned yet (" + Branding.SupportContactPlaceholder + " is a placeholder, not a monitored mailbox).";

        public static readonly string Privacy =
            "Privacy\n\n" +
            "This build stores your mine only on this device. Progress does not move between Android and iOS, and there is no account to sign in to. " +
            "Uninstalling the app, or clearing its storage, deletes that local save. A backup of the last good save is kept beside it on the same device.\n\n" +
            "The game does not include an analytics SDK, a crash-reporting SDK, or an advertising SDK. " +
            "The release game has no advertisements, accounts, chat, or real-money purchases. " +
            (DevelopmentFeatures.TestAdsAvailable ? "Development reward tests are local simulations; they never contact an ad network. " : "") + "\n\n" +
            "Nothing in this build is sold for real money. Diamonds and coins are fictional game resources.\n\n" +
            "Offline earnings use the device clock and the saved mine. That can be wrong if the clock changes, and it is not a server-checked anti-cheat system.\n\n" +
            "A public privacy-policy page is not published yet. The address " + Branding.PrivacyPolicyUrlPlaceholder + " is a placeholder.\n\n" +
            "If a later build adds a real ad network, analytics, or cloud saves, this notice has to be rewritten to match what that build actually collects, and the store forms have to be filled in again.";
    }
}
