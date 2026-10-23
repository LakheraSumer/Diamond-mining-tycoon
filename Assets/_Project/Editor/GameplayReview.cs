using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiamondMine.Editor
{
    // These checks run real MonoBehaviours, Unity UI event routing and the disk store.
    // They are editor integration checks, not physical-device/touch or performance results.
    public static class GameplayReview
    {
        [Serializable] public sealed class Result { public string check; public string status; public string detail; }
        [Serializable] public sealed class Report { public string unity; public string version; public string saveRoot; public string scope; public List<Result> results = new List<Result>(); }
        sealed class Clock : IGameClock { public long value; public long UnixMilliseconds { get { return value; } } }
        sealed class DeniedIo : ISaveIO
        {
            public bool Exists(string p) { return false; }
            public string ReadAllText(string p) { throw new IOException("QA injected read failure"); }
            public void WriteAllText(string p,string text) { throw new IOException("QA injected write failure"); }
            public void ReplaceWithBackup(string a,string b,string c) { throw new IOException("QA injected replace failure"); }
            public void Delete(string p) { }
        }
        static Report report;
        static GameSession session;
        static Clock clock;
        static string destinationPath;
        static int oldPrestige;
        static double oldCoins;
        static int coveredTaps;
        static string firstOrder;
        static double resetMastery;
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Run(string destination)
        {
            destinationPath = destination;
            report = new Report { unity = Application.unityVersion,version = Branding.Version, saveRoot = Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"), scope = "Editor integration; synthetic UI pointer events; later progression uses explicitly credited QA fixture coins. No device results." };
            try
            {
                session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
                Check("Isolated save configured",!string.IsNullOrEmpty(report.saveRoot) && report.saveRoot.Contains("qa-saves"));
                Check("Fresh state",session.Ready && session.State.Coins == 0 && session.State.Mines.Count == 3);
                clock = new Clock { value = session.State.LastActiveUnixMs };
                session.Configure(clock);
                session.Boot();
                MinePresenter world = UnityEngine.Object.FindAnyObjectByType<MinePresenter>();
                Check("Camera independent of scenery",world.View.transform != world.transform && world.transform.localPosition == Vector3.zero);
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(world.View);
                Check("Mine machinery inside camera frustum",world.GetComponentsInChildren<Renderer>().Where(r => new[] { "Face", "CrusherBase", "SalesDesk", "Head" }.Contains(r.name)).All(r => GeometryUtility.TestPlanesAABB(planes,r.bounds)));
                Check("Full screen background clear",world.GetComponentsInChildren<Camera>().Any(c=>c.rect == new Rect(0,0,1,1) && c.cullingMask == 0 && c.clearFlags == CameraClearFlags.SolidColor));

                int taps = session.State.FindMine("warmstone").TapCount;
                Click(Button("Tap the rock"));
                Check("Manual mining through UI",session.State.FindMine("warmstone").TapCount == taps + 1);
                Advance(100,true);
                Check("Transport processing and first sale",session.State.FindMine("warmstone").TripsStarted > 0 && session.State.FindMine("warmstone").OreProcessed > 0 && session.State.Coins > 0);
                Check("Buy one persists",session.TryBuy("bin",1) && Reload().FindMine("warmstone").Level("bin") == 1);
                session.NotifyBottleneckVisible(2.2f,false);

                // Development-only fixture credit. It is never compiled into a player assembly.
                Wallet.Credit(session.State,10000000);
                Check("Hire automation",session.TryBuy("hire",1));
                Check("Buy ten persists",session.TryBuy("training",10) && Reload().FindMine("warmstone").Level("training") == 10);
                Check("Buy maximum persists",session.TryBuy("crusher",0) && Reload().FindMine("warmstone").Level("crusher") == session.Config.FindMine("warmstone").FindUpgrade("crusher").MaxLevel);
                session.OpenObjectives();
                Check("Tutorial completion",session.State.TutorialComplete);
                Advance(300,false);
                Check("Objective claims paid once",session.State.ClaimedObjectives.Count > 0 && ObjectiveService.ClaimCompleted(session.State,session.Config) == 0);
                OrderDefinition order = session.Config.Orders.First(o=>o.MineId == "warmstone");
                firstOrder = order.Id;
                Advance(3600,false);
                Check("Order claim persists and rejects duplicate",session.TryClaimOrder(order.Id) && !session.TryClaimOrder(order.Id) && Reload().ClaimedOrders.Contains(order.Id));
                Check("Second mine unlock persists",session.TryUnlock("crystal") && Reload().FindMine("crystal").Unlocked);
                Check("Third mine unlock persists",session.TryUnlock("deepcore") && Reload().FindMine("deepcore").Unlocked);
                Check("Select mine persists",session.TrySelectMine("crystal") && Reload().ActiveMineId == "crystal");
                Check("Second mine automation",session.TryBuy("hire",1));
                double warm = session.State.FindMine("warmstone").DiamondsSold;
                double crystal = session.State.FindMine("crystal").DiamondsSold;
                Advance(120,false);
                Check("Other mines produce while switching",session.State.FindMine("warmstone").DiamondsSold > warm && session.State.FindMine("crystal").DiamondsSold > crystal);

                object originalStore = Field("store").GetValue(session);
                Field("store").SetValue(session,new SaveGameStore(new DeniedIo(),Path.Combine(report.saveRoot,"denied.json")));
                Check("Failed mine switch does not change live state",!session.TrySelectMine("warmstone") && session.State.ActiveMineId == "crystal");
                double music = session.State.Settings.Music;
                session.UpdateSettings(0.1,0.2,false,true,0);
                Check("Failed settings save does not change live state",session.State.Settings.Music == music);
                Field("store").SetValue(session,originalStore);
                session.UpdateSettings(0.2,0.3,false,true,0);
                GameState saved = Reload();
                Check("Settings persist immediately",saved.Settings.Music == 0.2 && saved.Settings.Sfx == 0.3 && saved.Settings.ReducedMotion && !saved.Settings.Haptics && saved.Settings.Quality == 0);
                world.Present(session);
                Check("Reduced motion stops machinery swing",world.transform.Find("CrusherJaw").localRotation == Quaternion.identity);
                session.Boot();
                Check("Reload retains upgrades orders and settings",session.State.FindMine("warmstone").Level("training") == 10 && session.State.ClaimedOrders.Contains(order.Id) && session.State.Settings.ReducedMotion);
                clock.value = session.State.LastActiveUnixMs + 120000;
                double previousCoins = session.State.Coins;
                session.Boot();
                Check("Offline earnings committed before report",session.OfflineReport != null && session.OfflineReport.Coins > 0 && session.State.Coins > previousCoins && Reload().Coins == session.State.Coins);
                double settled = session.State.Coins;
                session.Boot();
                Check("Offline reload does not duplicate coins",session.State.Coins == settled);
                session.DismissOfflineReport();
            }
            catch (Exception ex) { report.results.Add(new Result { check="Review aborted",status="failed",detail=ex.ToString() }); }
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination,JsonUtility.ToJson(report,true));
            int failed = report.results.Count(r=>r.status == "failed");
            Debug.Log("Editor integration review: " + (report.results.Count-failed) + " passed, " + failed + " failed. " + destination);
            if (failed > 0) throw new InvalidOperationException("Editor integration review failed; inspect " + destination);
        }

        static FieldInfo Field(string name) { return typeof(GameSession).GetField(name,Hidden); }
        static Button TapButton() { return UnityEngine.Object.FindAnyObjectByType<GameUi>().GetComponentsInChildren<Button>().First(b=>UiFactory.Caption(b).text.StartsWith("Tap")); }
        public static void BeginModalChecks()
        {
            PortraitReview.ClosePanels();
            Click(Button("Settings"));
            coveredTaps=session.State.FindMine(session.State.ActiveMineId).TapCount;
        }
        public static void CheckModalAndClose()
        {
            Button tap=TapButton();
            GameObject hit=Hit(Point(tap));
            Check("Modal blocks pointer to mining control",hit != null && hit.GetComponentInParent<Button>() != tap);
            PointerClick(Point(tap));
            Check("Covered UI click does not mine",session.State.FindMine(session.State.ActiveMineId).TapCount == coveredTaps);
            Click(Button("Close"));
        }
        public static void CheckClosedAndOpenPrestige()
        {
            Button tap=TapButton();
            Check("Closed modal releases pointer",Hit(Point(tap)).GetComponentInParent<Button>() == tap);
            oldPrestige=session.State.PrestigeCount;
            Click(Button("Prestige"));
        }
        public static void CheckPrestigeAndCancel()
        {
            Check("Prestige requires confirmation",!Button("Prestige now").interactable && session.State.PrestigeCount == oldPrestige);
            oldCoins=session.State.Coins;
            Click(Button("Close"));
        }
        public static void CheckCancelAndReopen()
        {
            Check("Prestige cancel preserves run",session.State.PrestigeCount == oldPrestige && session.State.Coins >= oldCoins);
            Click(Button("Prestige"));
        }
        public static void ArmPrestige() { Click(Button("I understand the reset")); }
        public static void ConfirmPrestige()
        {
            Check("Prestige confirmation enabled after arming",Button("Prestige now").interactable);
            Click(Button("Prestige now"));
        }
        public static void CheckPrestigeSaved()
        {
            Check("Prestige committed and permanent order retained",session.State.PrestigeCount == oldPrestige + 1 && Reload().PrestigeCount == oldPrestige + 1 && session.State.ClaimedOrders.Contains(firstOrder) && session.State.Settings.ReducedMotion);
        }
        public static void StartResetReview(string destination)
        {
            destinationPath=destination;
            session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            report=new Report {unity=Application.unityVersion,version=Branding.Version,saveRoot=Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT"),scope="Focused editor UI reset/back regression, synthetic pointer input, isolated QA fixture"};
            Check("Reset review uses isolated save",!string.IsNullOrEmpty(report.saveRoot) && report.saveRoot.Contains("qa-saves"));
            Wallet.Credit(session.State,5000);
            session.State.Mastery=3;
            resetMastery=3;
            session.UpdateSettings(0.2,0.3,false,true,0);
            PortraitReview.ClosePanels();
            Click(Button("Settings"));
        }
        public static void ScrollSettingsBottom()
        {
            Canvas.ForceUpdateCanvases();
            foreach(ScrollRect scroll in UnityEngine.Object.FindAnyObjectByType<GameUi>().GetComponentsInChildren<ScrollRect>()) scroll.verticalNormalizedPosition=0;
        }
        public static void OpenReset() { Click(Button("Reset progress")); }
        public static void ArmReset()
        {
            Check("Reset starts with first confirmation",UnityEngine.Object.FindAnyObjectByType<GameUi>().GetComponentsInChildren<Button>().Any(b=>UiFactory.Caption(b).text == "Continue"));
            Click(Button("Continue"));
        }
        public static void BackFromReset()
        {
            Check("Reset continuation arms second confirmation",Button("Delete progress").interactable);
            PortraitReview.CloseTopPanel();
        }
        public static void CheckResetReopened()
        {
            Check("Back navigation clears reset arming",UnityEngine.Object.FindAnyObjectByType<GameUi>().GetComponentsInChildren<Button>().Any(b=>UiFactory.Caption(b).text == "Continue"));
            Click(Button("Cancel"));
            Check("Cancelled reset preserves saved progress",session.State.Mastery == resetMastery && Reload().Mastery == resetMastery && session.State.Coins >= 5000);
        }
        public static void PrepareLateGame()
        {
            Wallet.Credit(session.State,10000000);
            session.TrySelectMine("warmstone");
            session.TryBuy("hire",1);
            foreach(string id in new[] {"crystal","deepcore"})
            {
                session.TryUnlock(id);
                session.TrySelectMine(id);
                session.TryBuy("hire",1);
            }
            Advance(600,false);
            session.TrySelectMine("warmstone");
            GameState restored=Reload();
            Check("Saturated three-mine save remains reloadable",restored.Mastery == session.State.Mastery && restored.Mines.Count(m=>m.Unlocked) == 3 && restored.FindMine("deepcore").Carts.All(c=>c.Load <= MineStats.Compute(session.Config.FindMine("deepcore"),restored.FindMine("deepcore"),session.Config,restored.Mastery,false).CartCapacity+1e-6));
        }
        public static void PrepareOffline()
        {
            session.UpdateSettings(0.2,0.3,false,true,0);
            clock.value = session.State.LastActiveUnixMs + 300000;
            session.Boot();
            Check("Late-game offline reload retains mines and earnings",session.OfflineReport != null && session.OfflineReport.Coins > 0 && session.State.Mines.Count(m=>m.Unlocked) == 3 && Reload().Mastery == session.State.Mastery);
        }
        public static void CompleteReport()
        {
            if(report == null)return;
            File.WriteAllText(destinationPath,JsonUtility.ToJson(report,true));
            Debug.Log("Final editor integration: " + report.results.Count(r=>r.status == "passed") + " passed, " + report.results.Count(r=>r.status == "failed") + " failed. " + destinationPath);
        }
        static void Check(string name,bool success)
        {
            report.results.Add(new Result {check=name,status=success ? "passed" : "failed",detail="Real editor runtime / isolated QA save"});
            if (!string.IsNullOrEmpty(destinationPath)) File.WriteAllText(destinationPath,JsonUtility.ToJson(report,true));
            if (!success) throw new InvalidOperationException(name);
        }
        static GameState Reload() { return new SaveGameStore(new SystemSaveIO(),Path.Combine(report.saveRoot,"diamond-mine-save.json")).Load(session.Config).State; }
        static void Advance(int seconds,bool tap)
        {
            for(int i=0;i<seconds*2;i++)
            {
                if(tap) session.TryTap();
                ProductionSimulator.Advance(session.State,session.Config,0.5);
                clock.value += 500;
            }
            typeof(GameSession).GetMethod("Update",Hidden).Invoke(session,null);
        }
        static Button Button(string caption)
        {
            return UnityEngine.Object.FindAnyObjectByType<GameUi>().GetComponentsInChildren<Button>().First(b=>UiFactory.Caption(b).text == caption);
        }
        static Vector2 Point(Button button) { Canvas.ForceUpdateCanvases(); return RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center)); }
        static GameObject Hit(Vector2 point)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=point},hits);
            return hits.Count == 0 ? null : hits[0].gameObject;
        }
        static void Click(Button button) { if(!button.interactable)throw new InvalidOperationException("Disabled button: " + UiFactory.Caption(button).text); PointerClick(Point(button)); Canvas.ForceUpdateCanvases(); }
        static void PointerClick(Vector2 point)
        {
            GameObject target=Hit(point);
            if(target==null)throw new InvalidOperationException("No UI raycast target at " + point);
            var pointer=new PointerEventData(EventSystem.current) {position=point,button=PointerEventData.InputButton.Left};
            ExecuteEvents.ExecuteHierarchy(target,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
