using System.Collections.Generic;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    public sealed partial class GameUi : MonoBehaviour
    {
#if UNITY_EDITOR
        // Pixel-space inset simulation used only by the isolated editor review.
        public static Rect? ReviewSafeAreaOverride;
#endif
        enum Modal
        {
            NotRendered = -1,
            None = 0,
            Upgrades,
            Goals,
            Orders,
            Mines,
            Prestige,
            Settings,
            Credits,
            Privacy,
            Offline,
            Ad,
            Quit,
            Newer,
            Reset
        }

        sealed class UpgradeRow
        {
            public string Id;
            public Text Title;
            public Text Body;
            public Button One;
            public Button Ten;
            public Button Max;
            public Text OneText;
            public Text TenText;
            public Text MaxText;
        }

        GameSession session;
        GameAudio gameSound;
        MinePresenter world;
        YardControls yardControls;
        bool yardOpen;
        RectTransform safe;
        RectTransform modalHost;
        RectTransform upgradeHost;
        RectTransform upgradeContent;
        Text coinsText;
        Text mineNameText;
        Text incomeText;
        Text objectiveText;
        Text bottleneckText;
        Text metersText;
        Text bannerText;
        Text toastText;
        Button tapButton;
        Text tapLabel;
        RectTransform navRoot;
        readonly List<Modal> stack = new List<Modal>();
        readonly List<UpgradeRow> upgradeRows = new List<UpgradeRow>();
        Modal built = Modal.NotRendered;
        bool tablet;
        bool layoutReady;
        int screenW = -1;
        int screenH = -1;
        Rect safeRect;
        float hudTimer;
        float toastTimer;
        string toast = "";
        bool resetArmed;
        bool freshArmed;
        bool prestigeArmed;
        bool offlinePushed;
        string upgradeMine = "";
        int upgradeSignature = -1;
        Text prestigeBody;
        Button prestigeConfirm;

        public void AttachYard(YardControls controls)
        {
            yardControls = controls;
        }

        public void SetYardOpen(bool open)
        {
            yardOpen = open;
            if (world != null)
                world.SetRendering(!open);
        }

        public void Bind(GameSession gameSession, GameAudio gameAudio, MinePresenter presenter)
        {
            session = gameSession;
            gameSound = gameAudio;
            world = presenter;
            BuildChrome();
            ApplyLayout(true);
            RefreshHud();
        }

        void Update()
        {
            if (session == null)
                return;
            HandleBack();
            ApplyLayout(false);
            if (session.NewerSaveBlocked)
            {
                if (stack.Count == 0 || (stack[stack.Count - 1] != Modal.Newer && stack[stack.Count - 1] != Modal.Quit))
                {
                    stack.Clear();
                    Push(Modal.Newer, false);
                }
            }
            else if (session.OfflineReportOpen && !offlinePushed)
            {
                offlinePushed = true;
                Push(Modal.Offline, false);
            }
            if (!session.OfflineReportOpen)
                offlinePushed = false;

            bool covered = stack.Count > 0;
            float dt = Time.unscaledDeltaTime;
            session.NotifyBottleneckVisible(dt, covered);
            session.NotifyObjectiveVisible(dt, covered);

            if (world != null && session.Ready && !yardOpen)
                world.Present(session);
            hudTimer += dt;
            if (hudTimer >= 0.1f)
            {
                hudTimer = 0f;
                RefreshHud();
                RefreshUpgrades(false);
                RefreshDynamicModal();
            }

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                if (toastTimer <= 0f && toastText != null)
                    toastText.text = "";
            }
        }

        void BuildChrome()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0f;

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }

            safe = UiFactory.Rect("Safe", canvasObject.transform);
            coinsText = UiFactory.Label(safe, "", 52, UiTheme.Coin, TextAnchor.MiddleLeft);
            mineNameText = UiFactory.Label(safe, "", 28, UiTheme.Diamond, TextAnchor.MiddleRight);
            incomeText = UiFactory.Label(safe, "", 30, UiTheme.Paper, TextAnchor.MiddleLeft);
            objectiveText = UiFactory.Label(safe, "", 32, UiTheme.Paper, TextAnchor.MiddleLeft);
            bottleneckText = UiFactory.Label(safe, "", 30, UiTheme.Paper, TextAnchor.MiddleLeft);
            metersText = UiFactory.Label(safe, "", 26, UiTheme.Diamond, TextAnchor.MiddleLeft);
            bannerText = UiFactory.Label(safe, "", 28, UiTheme.Paper, TextAnchor.MiddleLeft);
            bannerText.resizeTextForBestFit = true;
            bannerText.resizeTextMinSize = 20;
            bannerText.resizeTextMaxSize = 28;
            toastText = UiFactory.Label(safe, "", 30, UiTheme.Coin, TextAnchor.MiddleCenter);

            tapButton = UiFactory.Press(safe, "Tap the rock", OnTap, UiTheme.ButtonOn, 140f);
            tapLabel = UiFactory.Caption(tapButton);
            var tapLayout = tapButton.GetComponent<LayoutElement>();
            if (tapLayout != null)
                Destroy(tapLayout);

            navRoot = UiFactory.Panel("Nav", safe, UiTheme.Ink);
            BuildNav();

            upgradeHost = UiFactory.Panel("Upgrades", safe, UiTheme.Panel);
            RectTransform scroll = UiFactory.Scroll(upgradeHost, out upgradeContent);
            UiFactory.Stretch(scroll);

            modalHost = UiFactory.Panel("Modal", safe, new Color(0.05f, 0.03f, 0.02f, 0.72f));
            UiFactory.Stretch(modalHost);
            modalHost.gameObject.SetActive(false);
            upgradeHost.gameObject.SetActive(false);
        }

        void BuildNav()
        {
            string[] labels = { "Yard", "Upgrades", "Goals", "Mines", "Prestige", "Settings" };
            Modal[] modals = { Modal.None, Modal.Upgrades, Modal.Goals, Modal.Mines, Modal.Prestige, Modal.Settings };
            for (int i = 0; i < labels.Length; i++)
            {
                Modal modal = modals[i];
                Button button = UiFactory.Press(navRoot, labels[i], delegate
                {
                    if (modal == Modal.None)
                    {
                        if (yardControls != null)
                            yardControls.Show();
                        return;
                    }
                    if (modal == Modal.Upgrades && tablet)
                        return;
                    Push(modal, true);
                }, UiTheme.Button, 120f);
                RectTransform rect = button.GetComponent<RectTransform>();
                float column = i / (float)labels.Length;
                float next = (i + 1) / (float)labels.Length;
                rect.anchorMin = new Vector2(column, 0.08f);
                rect.anchorMax = new Vector2(next - 0.01f, 0.92f);
                rect.offsetMin = new Vector2(3f, 0f);
                rect.offsetMax = new Vector2(-3f, 0f);
                LayoutElement layout = button.GetComponent<LayoutElement>();
                if (layout != null)
                    Destroy(layout);
                Text caption = UiFactory.Caption(button);
                caption.fontSize = 24;
                caption.horizontalOverflow = HorizontalWrapMode.Overflow;
                caption.resizeTextForBestFit = true;
                caption.resizeTextMinSize = 14;
                caption.resizeTextMaxSize = 24;
            }
        }

        void ApplyLayout(bool force)
        {
            Rect area = Screen.safeArea;
#if UNITY_EDITOR
            if (ReviewSafeAreaOverride.HasValue) area = ReviewSafeAreaOverride.Value;
#endif
            bool nowTablet = PlatformInfo.IsTabletLayout();
            if (!force && layoutReady && screenW == Screen.width && screenH == Screen.height && safeRect == area && tablet == nowTablet)
                return;
            layoutReady = true;
            screenW = Screen.width;
            screenH = Screen.height;
            safeRect = area;
            tablet = nowTablet;

            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Mathf.Max(1, Screen.width);
            min.y /= Mathf.Max(1, Screen.height);
            max.x /= Mathf.Max(1, Screen.width);
            max.y /= Mathf.Max(1, Screen.height);
            safe.anchorMin = min;
            safe.anchorMax = max;
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;

            float textRight = tablet ? 0.58f : 0.96f;
            Place(coinsText.rectTransform, 0.04f, 0.9f, tablet ? 0.3f : 0.55f, 0.99f);
            Place(mineNameText.rectTransform, tablet ? 0.31f : 0.57f, 0.9f, textRight, 0.99f);
            Place(incomeText.rectTransform, 0.04f, 0.845f, textRight, 0.9f);
            Place(objectiveText.rectTransform, 0.04f, 0.78f, textRight, 0.845f);
            Place(bottleneckText.rectTransform, 0.04f, 0.7f, textRight, 0.78f);
            Place(metersText.rectTransform, 0.04f, 0.63f, textRight, 0.7f);
            Place(bannerText.rectTransform, 0.04f, 0.58f, textRight, 0.64f);
            Place(toastText.rectTransform, 0.08f, 0.52f, tablet ? 0.56f : 0.92f, 0.58f);

            RectTransform tapRect = tapButton.GetComponent<RectTransform>();
            if (tablet)
            {
                tapRect.anchorMin = new Vector2(0.04f, 0.12f);
                tapRect.anchorMax = new Vector2(0.34f, 0.2f);
            }
            else
            {
                tapRect.anchorMin = new Vector2(0.06f, 0.15f);
                tapRect.anchorMax = new Vector2(0.94f, 0.24f);
            }
            tapRect.offsetMin = Vector2.zero;
            tapRect.offsetMax = Vector2.zero;

            navRoot.anchorMin = new Vector2(0f, 0f);
            navRoot.anchorMax = new Vector2(1f, tablet ? 0.09f : 0.13f);
            navRoot.offsetMin = Vector2.zero;
            navRoot.offsetMax = Vector2.zero;

            if (world != null)
            {
                Rect mineArea = tablet
                    ? new Rect(0f, 0.1f, 0.58f, 0.8f)
                    : new Rect(0f, 0.25f, 1f, 0.38f);
                world.SetViewport(new Rect(min.x + mineArea.x * (max.x - min.x), min.y + mineArea.y * (max.y - min.y), mineArea.width * (max.x - min.x), mineArea.height * (max.y - min.y)));
            }
            PlaceUpgradeHost();
        }

        static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void PlaceUpgradeHost()
        {
            bool show = tablet || (stack.Count > 0 && stack[stack.Count - 1] == Modal.Upgrades);
            upgradeHost.gameObject.SetActive(show && session != null && session.Ready);
            if (!show)
                return;
            if (tablet)
            {
                upgradeHost.anchorMin = new Vector2(0.6f, 0.1f);
                upgradeHost.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                upgradeHost.anchorMin = new Vector2(0.03f, 0.14f);
                upgradeHost.anchorMax = new Vector2(0.97f, 0.88f);
            }
            upgradeHost.offsetMin = Vector2.zero;
            upgradeHost.offsetMax = Vector2.zero;
        }

        void RefreshHud()
        {
            if (!session.Ready || session.State == null)
            {
                coinsText.text = Branding.WorkingTitle;
                mineNameText.text = "";
                incomeText.text = "";
                objectiveText.text = session.NewerSaveBlocked ? "" : (string.IsNullOrEmpty(session.StatusMessage) ? "Opening the mine..." : session.StatusMessage);
                bottleneckText.text = "";
                metersText.text = "";
                bannerText.text = session.SaveError ?? "";
                if (tapButton != null)
                    tapButton.gameObject.SetActive(false);
                return;
            }

            GameState state = session.State;
            MineState mine = state.FindMine(state.ActiveMineId);
            MineDefinition def = session.Config.FindMine(state.ActiveMineId);
            tapButton.gameObject.SetActive(!session.OfflinePending && !yardOpen);
            if (yardOpen && session.State.Adventure != null)
            {
                coinsText.text = NumericFormat.Format(state.Coins) + " coins";
                CharacterDef person = JourneyCatalog.FindCharacter(session.State.Adventure.SelectedCharacter);
                mineNameText.text = person == null ? "Choose a miner" : person.Name;
                incomeText.text = "Yard";
                objectiveText.text = AdventureRules.NextHint(session.State);
                bottleneckText.text = "";
                metersText.text = "";
                bannerText.text = session.ActionMessage ?? "";
                return;
            }
            coinsText.text = NumericFormat.Format(state.Coins) + " coins";
            if (state.BoostRemainingSeconds > 0.5d)
                coinsText.text += "   " + session.Config.BoostMultiplier.ToString("0") + "x " + Mathf.CeilToInt((float)state.BoostRemainingSeconds) + "s";

            bool automated = false;
            if (mine != null && def != null)
            {
                MineRates rates = MineStats.Compute(def, mine, session.Config, state.Mastery, state.BoostRemainingSeconds > 0d);
                automated = rates.AutoUnlocked;
                mineNameText.text = def.Name;
                double income = MineStats.SustainedIncome(state, session.Config, true);
                int open = 0;
                for (int i = 0; i < state.Mines.Count; i++)
                    if (state.Mines[i].Unlocked) open++;
                incomeText.text = "Income " + NumericFormat.FormatRate(income) + (automated ? "" : " while tapping");
                if (open > 1)
                    incomeText.text += "   · all open mines";
                StageDiagnosis diagnosis = BottleneckService.Diagnose(mine, rates);
                bool idle = diagnosis.Reason.StartsWith("Tap") || diagnosis.Reason.IndexOf("idle") >= 0;
                string marked = state.TutorialStep == 4 || state.TutorialStep == 5 ? state.MarkedStage : "";
                string prefix = idle ? "" : "SLOW · ";
                if (!string.IsNullOrEmpty(marked) && marked == diagnosis.Stage)
                    prefix = "SLOW · ";
                bottleneckText.text = prefix + diagnosis.Title + ". " + diagnosis.Reason;
                metersText.text =
                    "Ore " + NumericFormat.Format(mine.OreAtFace) + "/" + NumericFormat.Format(rates.MineBuffer) + " · Carts " + mine.Carts.Count +
                    " · Crusher " + NumericFormat.Format(mine.ProcessInput) + "/" + NumericFormat.Format(rates.ProcessBuffer) +
                    "\nGems " + NumericFormat.Format(mine.Rough) + "/" + NumericFormat.Format(rates.RoughBuffer) + " · Vault " + NumericFormat.Format(mine.Storage) + "/" + NumericFormat.Format(rates.StorageCap);
            }

            if (!state.TutorialComplete)
                objectiveText.text = TutorialService.Prompt(state, session.Config);
            else
            {
                ObjectiveDefinition current = ObjectiveService.Current(state, session.Config);
                objectiveText.text = current == null ? "Objectives complete." : "Goal: " + current.Title + "  " + ObjectiveProgress(current);
            }

            tapLabel.text = automated ? "Tap (optional)" : "Tap the rock";
            string banner = "";
            if (session.OfflinePending)
                banner = "Save problem: offline earnings are waiting for a successful save. " + session.SaveError;
            else if (!string.IsNullOrEmpty(session.SaveError))
                banner = "Save problem: " + session.SaveError;
            else if (!string.IsNullOrEmpty(session.StatusMessage))
                banner = session.StatusMessage;
            bannerText.text = banner;
            if (gameSound != null)
                gameSound.Observe(session);
        }

        void OnTap()
        {
            if (session.TryTap() && gameSound != null)
                gameSound.PlayUi(session);
        }

        void HandleBack()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
            if (yardControls != null && yardOpen && yardControls.TryConsumeBack())
                return;
            if (stack.Count == 0)
            {
                Push(Modal.Quit, true);
                return;
            }
            Modal top = stack[stack.Count - 1];
            if (top == Modal.Newer)
            {
                Push(Modal.Quit, true);
                return;
            }
            Pop();
        }

        void Push(Modal modal, bool sound)
        {
            if (!session.Ready && modal != Modal.Newer && modal != Modal.Quit && modal != Modal.Credits && modal != Modal.Privacy)
                return;
            if (stack.Count > 0 && stack[stack.Count - 1] == modal)
                return;
            if (modal == Modal.Settings)
                resetArmed = false;
            if (modal == Modal.Reset)
                resetArmed = false;
            if (modal == Modal.Prestige)
                prestigeArmed = false;
            if (modal == Modal.Newer)
                freshArmed = false;
            stack.Add(modal);
            if (sound && gameSound != null)
                gameSound.PlayUi(session);
            if (modal == Modal.Goals)
                session.OpenObjectives();
            built = Modal.NotRendered;
            SyncModal();
            PlaceUpgradeHost();
        }

        void Pop()
        {
            if (stack.Count == 0)
                return;
            Modal closed = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (closed == Modal.Offline)
                session.DismissOfflineReport();
            if (closed == Modal.Ad)
                session.CancelPendingAd();
            if (closed == Modal.Reset)
                resetArmed = false;
            built = Modal.NotRendered;
            SyncModal();
            PlaceUpgradeHost();
        }

        void SyncModal()
        {
            Modal top = stack.Count == 0 ? Modal.None : stack[stack.Count - 1];
            if (top == Modal.Upgrades)
                top = Modal.None;
            if (top == built)
                return;
            built = top;
            UiFactory.Clear(modalHost);
            prestigeBody = null;
            orderRows.Clear();
            prestigeConfirm = null;
            modalHost.gameObject.SetActive(top != Modal.None);
            if (top == Modal.None)
                return;
            RectTransform card = UiFactory.Panel("Card", modalHost, UiTheme.Panel);
            bool wide = tablet;
            card.anchorMin = new Vector2(wide ? 0.18f : 0.04f, wide ? 0.12f : 0.16f);
            card.anchorMax = new Vector2(wide ? 0.82f : 0.96f, wide ? 0.9f : 0.9f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            switch (top)
            {
                case Modal.Goals: BuildGoals(card); break;
                case Modal.Orders: BuildOrders(card); break;
                case Modal.Mines: BuildMines(card); break;
                case Modal.Prestige: BuildPrestige(card); break;
                case Modal.Settings: BuildSettings(card); break;
                case Modal.Credits: BuildInfo(card, "Credits", InfoCopy.Credits); break;
                case Modal.Privacy: BuildInfo(card, "Privacy", InfoCopy.Privacy); break;
                case Modal.Offline: BuildOffline(card); break;
                case Modal.Ad: BuildAd(card); break;
                case Modal.Quit: BuildQuit(card); break;
                case Modal.Newer: BuildNewer(card); break;
                case Modal.Reset: BuildReset(card); break;
            }
        }

        void RefreshDynamicModal()
        {
            if (built == Modal.Orders) RefreshOrders();
            if (built == Modal.Prestige && prestigeBody != null)
                FillPrestige();
            int signature = StructureSignature();
            if (signature == structureSignature)
                return;
            structureSignature = signature;
            if (built == Modal.Goals || built == Modal.Mines)
            {
                built = Modal.NotRendered;
                SyncModal();
            }
        }

        int structureSignature = int.MinValue;

        int StructureSignature()
        {
            if (session == null || !session.Ready || session.State == null)
                return 0;
            int signature = session.State.ClaimedObjectives.Count * 31 + session.State.PrestigeCount;
            for (int i = 0; i < session.State.Mines.Count; i++)
                signature = signature * 2 + (session.State.Mines[i].Unlocked ? 1 : 0);
            return signature;
        }

        void ShowToast(string message)
        {
            toast = message;
            toastTimer = 2.4f;
            if (toastText != null)
                toastText.text = message;
        }

        string ObjectiveProgress(ObjectiveDefinition objective)
        {
            GameState state = session.State;
            double current = 0d;
            switch (objective.Type)
            {
                case "oreMined":
                    for (int i = 0; i < state.Mines.Count; i++) current += state.Mines[i].OreMined;
                    break;
                case "diamondsSold":
                    for (int i = 0; i < state.Mines.Count; i++) current += state.Mines[i].DiamondsSold;
                    break;
                case "lifetimeCoins": current = state.LifetimeCoins; break;
                case "upgradesBought": current = UpgradeService.TotalLevels(state); break;
                case "prestigeCount": current = state.PrestigeCount; break;
                case "upgradeLevel":
                    MineState mine = state.FindMine(objective.MineId);
                    current = mine == null ? 0d : mine.Level(objective.UpgradeId);
                    break;
                default:
                    return ObjectiveService.IsMet(state, session.Config, objective) ? "Ready" : "";
            }
            return NumericFormat.Format(current) + " / " + NumericFormat.Format(objective.Target);
        }
    }
}
