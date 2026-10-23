using System;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    public sealed partial class GameUi
    {
        static readonly string[] StageOrder =
        {
            Stages.Mining, Stages.Transport, Stages.Processing, Stages.Sorting,
            Stages.Storage, Stages.Sales, Stages.Automation
        };

        void RefreshUpgrades(bool force)
        {
            if (upgradeHost == null || session == null || !session.Ready || !upgradeHost.gameObject.activeSelf)
                return;
            MineDefinition def = session.Config.FindMine(session.State.ActiveMineId);
            MineState mine = session.State.FindMine(session.State.ActiveMineId);
            if (def == null || mine == null)
                return;

            int signature = session.State.TutorialStep * 1000 + (session.State.TutorialComplete ? 1 : 0);
            for (int i = 0; i < def.Upgrades.Count; i++)
                signature = signature * 13 + mine.Level(def.Upgrades[i].Id);
            if (force || upgradeMine != def.Id || signature != upgradeSignature)
            {
                upgradeMine = def.Id;
                upgradeSignature = signature;
                RebuildUpgradeRows(def);
            }
            RelabelUpgrades(def, mine);
        }

        void RebuildUpgradeRows(MineDefinition def)
        {
            UiFactory.Clear(upgradeContent);
            upgradeRows.Clear();
            Text heading = UiFactory.Label(upgradeContent, "Upgrades · " + def.Name, 36, UiTheme.Coin, TextAnchor.MiddleLeft);
            Preferred(heading, 72f);
            if (!tablet)
            {
                Button close = UiFactory.Press(upgradeContent, "Close", Pop, UiTheme.Button, 96f);
                UiFactory.Caption(close).fontSize = 30;
            }

            for (int s = 0; s < StageOrder.Length; s++)
            {
                for (int i = 0; i < def.Upgrades.Count; i++)
                {
                    UpgradeDefinition upgrade = def.Upgrades[i];
                    if (upgrade.Stage != StageOrder[s])
                        continue;
                    var row = new UpgradeRow();
                    row.Id = upgrade.Id;
                    RectTransform card = UiFactory.Panel("Row", upgradeContent, UiTheme.Ink);
                    PreferredRect(card, 286f);
                    VerticalLayoutGroup column = card.gameObject.AddComponent<VerticalLayoutGroup>();
                    column.padding = new RectOffset(16, 16, 12, 12);
                    column.spacing = 8f;
                    column.childControlHeight = true;
                    column.childControlWidth = true;
                    column.childForceExpandHeight = false;
                    column.childForceExpandWidth = true;
                    row.Title = UiFactory.Label(card, upgrade.Name, 32, UiTheme.Paper, TextAnchor.MiddleLeft);
                    Preferred(row.Title, 44f);
                    row.Body = UiFactory.Label(card, upgrade.Description, 28, UiTheme.Muted, TextAnchor.MiddleLeft);
                    Preferred(row.Body, 82f);
                    RectTransform actions = UiFactory.Rect("Actions", card);
                    PreferredRect(actions, 108f);
                    HorizontalLayoutGroup rowLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
                    rowLayout.spacing = 8f;
                    rowLayout.childControlWidth = true;
                    rowLayout.childForceExpandWidth = true;
                    rowLayout.childControlHeight = true;
                    rowLayout.childForceExpandHeight = true;
                    string id = upgrade.Id;
                    row.One = UiFactory.Press(actions, "Buy", delegate { Buy(id, 1); }, UiTheme.ButtonOn, 96f);
                    row.Ten = UiFactory.Press(actions, "Buy 10", delegate { Buy(id, 10); }, UiTheme.Button, 96f);
                    row.Max = UiFactory.Press(actions, "Max", delegate { Buy(id, 0); }, UiTheme.Button, 96f);
                    row.OneText = UiFactory.Caption(row.One);
                    row.TenText = UiFactory.Caption(row.Ten);
                    row.MaxText = UiFactory.Caption(row.Max);
                    row.OneText.fontSize = 28;
                    row.TenText.fontSize = 28;
                    row.MaxText.fontSize = 28;
                    upgradeRows.Add(row);
                }
            }
        }

        void RelabelUpgrades(MineDefinition def, MineState mine)
        {
            string marked = session.State.TutorialStep == 4 || session.State.TutorialStep == 5
                ? session.State.MarkedStage
                : "";
            for (int i = 0; i < upgradeRows.Count; i++)
            {
                UpgradeRow row = upgradeRows[i];
                UpgradeDefinition upgrade = def.FindUpgrade(row.Id);
                if (upgrade == null)
                    continue;
                int level = mine.Level(upgrade.Id);
                bool slow = !string.IsNullOrEmpty(marked) && upgrade.Stage == marked;
                row.Title.text = (slow ? "SLOW · " : "") + upgrade.Name + "   " + level + "/" + upgrade.MaxLevel;
                PurchasePreview one = UpgradeService.Preview(session.State, session.Config, def.Id, upgrade.Id, 1);
                row.Body.text = string.IsNullOrEmpty(one.Summary) ? upgrade.Description : one.Summary;
                bool buyOne = one.Affordable && one.Count == 1;
                row.One.interactable = buyOne;
                row.OneText.text = one.Cost > 0d ? "Buy  " + NumericFormat.Format(one.Cost) : ShortDeny(one.DenyReason);

                int room = upgrade.MaxLevel - level;
                int affordable = UpgradeService.MaxAffordable(session.State.Coins, level, upgrade.MaxLevel, upgrade.BaseCost, upgrade.CostGrowth);
                bool ten = room >= 10 && affordable >= 10;
                PurchasePreview tenPreview = ten
                    ? UpgradeService.Preview(session.State, session.Config, def.Id, upgrade.Id, 10)
                    : null;
                bool buyTen = tenPreview != null && tenPreview.Affordable && tenPreview.Count == 10;
                row.Ten.interactable = buyTen;
                row.TenText.text = buyTen ? "Buy 10  " + NumericFormat.Format(tenPreview.Cost) : "Buy 10";

                PurchasePreview max = UpgradeService.Preview(session.State, session.Config, def.Id, upgrade.Id, 0);
                bool buyMax = max.Affordable && max.Count >= 1;
                row.Max.interactable = buyMax;
                row.MaxText.text = buyMax ? "Max " + max.Count + "  " + NumericFormat.Format(max.Cost) : "Max";
            }
        }

        void Buy(string id, int count)
        {
            if (gameSound != null)
                gameSound.PlayUi(session);
            if (!session.TryBuy(id, count))
                ShowToast("That purchase did not go through.");
            else
                ShowToast("Upgrade bought.");
            RefreshUpgrades(false);
        }

        static string ShortDeny(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return "Buy";
            if (reason.IndexOf("Fully") >= 0)
                return "Maxed";
            if (reason.IndexOf("Requires") >= 0)
                return reason;
            return "Buy";
        }

        void Header(RectTransform card, string title)
        {
            Text label = UiFactory.Label(card, title, 40, UiTheme.Coin, TextAnchor.MiddleLeft);
            Place(label.rectTransform, 0.04f, 0.88f, 0.62f, 0.98f);
            Button close = UiFactory.Press(card, "Close", Pop, UiTheme.Button, 96f);
            RectTransform rect = close.GetComponent<RectTransform>();
            Place(rect, 0.66f, 0.86f, 0.96f, 0.98f);
            LayoutElement layout = close.GetComponent<LayoutElement>();
            if (layout != null)
                Destroy(layout);
        }

        void BuildGoals(RectTransform card)
        {
            Header(card, "Goals");
            RectTransform content;
            RectTransform scroll = UiFactory.Scroll(card, out content);
            Place(scroll, 0.02f, 0.02f, 0.98f, 0.86f);
            if (!session.Ready)
                return;
            UiFactory.Press(content, "Orders", delegate { Push(Modal.Orders, true); }, UiTheme.ButtonOn, 96f);
            for (int i = 0; i < session.Config.Objectives.Count; i++)
            {
                ObjectiveDefinition objective = session.Config.Objectives[i];
                bool claimed = session.State.ClaimedObjectives.Contains(objective.Id);
                bool met = ObjectiveService.IsMet(session.State, session.Config, objective);
                string mark = claimed ? "Done" : met ? "Ready" : "Now";
                if (!claimed && !met)
                {
                    ObjectiveDefinition current = ObjectiveService.Current(session.State, session.Config);
                    mark = current != null && current.Id == objective.Id ? "Now" : "Later";
                }
                Text line = UiFactory.Label(content,
                    mark + " · " + objective.Title + "\n" + objective.Detail + "\n" + ObjectiveProgress(objective) +
                    (objective.Reward > 0d ? "    Reward " + NumericFormat.Format(objective.Reward) + " coins" : ""),
                    28, claimed ? UiTheme.Muted : UiTheme.Paper, TextAnchor.MiddleLeft);
                Preferred(line, 150f);
            }
        }

        void BuildMines(RectTransform card)
        {
            Header(card, "Mines");
            RectTransform content;
            RectTransform scroll = UiFactory.Scroll(card, out content);
            Place(scroll, 0.02f, 0.02f, 0.98f, 0.86f);
            Text note = UiFactory.Label(content, "Open mines keep working while you look at another one. Lifetime coins: " +
                NumericFormat.Format(session.State.LifetimeCoins) + ". Mastery " + session.State.Mastery.ToString("0"),
                28, UiTheme.Muted, TextAnchor.MiddleLeft);
            Preferred(note, 110f);
            for (int i = 0; i < session.Config.Mines.Count; i++)
            {
                MineDefinition def = session.Config.Mines[i];
                MineState mine = session.State.FindMine(def.Id);
                bool open = mine != null && mine.Unlocked;
                bool viewing = session.State.ActiveMineId == def.Id;
                Text title = UiFactory.Label(content, def.Name + (viewing ? "   · viewing" : ""), 34, UiTheme.Diamond, TextAnchor.MiddleLeft);
                Preferred(title, 52f);
                Text body = UiFactory.Label(content, def.Description, 28, UiTheme.Paper, TextAnchor.MiddleLeft);
                Preferred(body, 80f);
                string id = def.Id;
                if (open)
                {
                    Button view = UiFactory.Press(content, viewing ? "Viewing this mine" : "View this mine", delegate
                    {
                        if (session.TrySelectMine(id))
                        {
                            upgradeMine = "";
                            Pop();
                        }
                    }, UiTheme.ButtonOn, 100f);
                }
                else
                {
                    string reason;
                    bool can = MineUnlocks.CanUnlock(session.State, session.Config, id, out reason);
                    Button openButton = UiFactory.Press(content, can ? "Open this mine" : reason, delegate
                    {
                        if (session.TryUnlock(id))
                        {
                            ShowToast(def.Name + " is open. The other mines keep running.");
                            upgradeMine = "";
                            Pop();
                        }
                        else
                            ShowToast("That mine is not open yet.");
                    }, UiTheme.ButtonOn, 100f);
                    openButton.interactable = can;
                }
            }
        }

        void BuildPrestige(RectTransform card)
        {
            Header(card, "Prestige");
            prestigeBody = UiFactory.Label(card, "", 30, UiTheme.Paper, TextAnchor.UpperLeft);
            Place(prestigeBody.rectTransform, 0.05f, 0.38f, 0.95f, 0.86f);
            Button arm = UiFactory.Press(card, "I understand the reset", delegate
            {
                prestigeArmed = true;
                FillPrestige();
            }, UiTheme.Button, 100f);
            RectTransform armRect = arm.GetComponent<RectTransform>();
            Destroy(arm.GetComponent<LayoutElement>());
            Place(armRect, 0.05f, 0.22f, 0.95f, 0.34f);
            prestigeConfirm = UiFactory.Press(card, "Prestige now", delegate
            {
                if (!prestigeArmed)
                    return;
                if (session.TryPrestige())
                {
                    prestigeArmed = false;
                    upgradeMine = "";
                    ShowToast("Mastery increased. The mines start again.");
                    Pop();
                }
                else
                    ShowToast("Prestige is not available.");
            }, UiTheme.Danger, 110f);
            Destroy(prestigeConfirm.GetComponent<LayoutElement>());
            Place(prestigeConfirm.GetComponent<RectTransform>(), 0.05f, 0.06f, 0.95f, 0.18f);
            FillPrestige();
        }

        void FillPrestige()
        {
            if (prestigeBody == null || !session.Ready)
                return;
            PrestigePreview preview = PrestigeService.Preview(session.State, session.Config);
            prestigeBody.text = preview.Summary + (string.IsNullOrEmpty(preview.DenyReason) ? "" : "\n\n" + preview.DenyReason);
            if (prestigeConfirm != null)
                prestigeConfirm.interactable = prestigeArmed && preview.Available;
        }

        void BuildSettings(RectTransform card)
        {
            Header(card, "Settings");
            RectTransform content;
            RectTransform scroll = UiFactory.Scroll(card, out content);
            Place(scroll, 0.02f, 0.02f, 0.98f, 0.86f);
            SettingsState settings = session.State.Settings;
            AddStepper(content, "Music", settings.Music, delegate (double value)
            {
                session.UpdateSettings(value, session.State.Settings.Sfx, session.State.Settings.Haptics, session.State.Settings.ReducedMotion, session.State.Settings.Quality);
            });
            AddStepper(content, "Sound effects", settings.Sfx, delegate (double value)
            {
                session.UpdateSettings(session.State.Settings.Music, value, session.State.Settings.Haptics, session.State.Settings.ReducedMotion, session.State.Settings.Quality);
            });

            AddToggle(content, "Haptics", settings.Haptics, delegate (bool value)
            {
                session.UpdateSettings(session.State.Settings.Music, session.State.Settings.Sfx, value, session.State.Settings.ReducedMotion, session.State.Settings.Quality);
                if (value) PlatformInfo.Pulse(true);
            });
            AddToggle(content, "Reduced motion", settings.ReducedMotion, delegate (bool value)
            {
                session.UpdateSettings(session.State.Settings.Music, session.State.Settings.Sfx, session.State.Settings.Haptics, value, session.State.Settings.Quality);
            });
            string qualityName = settings.Quality <= 0 ? "Low" : settings.Quality == 1 ? "Medium" : "High";
            Button quality = UiFactory.Press(content, "Graphics: " + qualityName, delegate
            {
                int next = (session.State.Settings.Quality + 1) % 3;
                session.UpdateSettings(session.State.Settings.Music, session.State.Settings.Sfx, session.State.Settings.Haptics, session.State.Settings.ReducedMotion, next);
                built = Modal.NotRendered;
                SyncModal();
            }, UiTheme.Button, 100f);

            Text network = UiFactory.Label(content, PlatformInfo.NetworkStatus(), 26, UiTheme.Muted, TextAnchor.MiddleLeft);
            Preferred(network, 90f);
            string boost = session.BoostOfferLabel();
            if (!string.IsNullOrEmpty(boost))
            {
                Button ad = UiFactory.Press(content, boost, delegate
                {
                    session.BeginBoostAd();
                    if (session.PendingReward != null)
                        Push(Modal.Ad, true);
                }, UiTheme.ButtonOn, 110f);
            }
            if (session.CanOfferOfflineBonus())
            {
                Button bonus = UiFactory.Press(content, session.OfflineBonusLabel(), delegate
                {
                    session.BeginOfflineAd();
                    if (session.PendingReward != null)
                        Push(Modal.Ad, true);
                }, UiTheme.Button, 110f);
            }
            UiFactory.Press(content, "Credits", delegate { Push(Modal.Credits, true); }, UiTheme.Button, 96f);
            UiFactory.Press(content, "Privacy", delegate { Push(Modal.Privacy, true); }, UiTheme.Button, 96f);
            UiFactory.Press(content, "Reset progress", delegate { Push(Modal.Reset, true); }, UiTheme.Danger, 100f);
            Text version = UiFactory.Label(content, Branding.WorkingTitle + "  " + Branding.Version + " (" + Branding.BuildNumber + ")",
                24, UiTheme.Muted, TextAnchor.MiddleLeft);
            Preferred(version, 120f);
        }

        void AddStepper(Transform parent, string caption, double value, Action<double> changed)
        {
            int percent = Mathf.RoundToInt((float)value * 100f);
            Text label = UiFactory.Label(parent, caption + "  " + percent + "%", 28, UiTheme.Paper, TextAnchor.MiddleLeft);
            Preferred(label, 40f);
            RectTransform row = UiFactory.Rect("Step", parent);
            PreferredRect(row, 96f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            UiFactory.Press(row, "Lower", delegate
            {
                changed(Math.Max(0d, value - 0.1d));
                built = Modal.NotRendered;
                SyncModal();
            }, UiTheme.Button, 96f);
            UiFactory.Press(row, "Higher", delegate
            {
                changed(Math.Min(1d, value + 0.1d));
                built = Modal.NotRendered;
                SyncModal();
            }, UiTheme.Button, 96f);
        }

        void AddToggle(Transform parent, string caption, bool value, UnityEngine.Events.UnityAction<bool> changed)
        {
            Button button = UiFactory.Press(parent, caption + ": " + (value ? "On" : "Off"), null, value ? UiTheme.ButtonOn : UiTheme.Button, 96f);
            button.onClick.AddListener(delegate
            {
                bool next = !value;
                changed(next);
                built = Modal.NotRendered;
                SyncModal();
            });
        }

        void BuildInfo(RectTransform card, string title, string body)
        {
            Header(card, title);
            RectTransform content;
            RectTransform scroll = UiFactory.Scroll(card, out content);
            Place(scroll, 0.03f, 0.03f, 0.97f, 0.84f);
            Text text = UiFactory.Label(content, body, 28, UiTheme.Paper, TextAnchor.UpperLeft);
            var size = text.gameObject.AddComponent<LayoutElement>();
            size.minHeight = 700f;
        }

        void BuildOffline(RectTransform card)
        {
            Header(card, "Away earnings");
            OfflinePlan plan = session.OfflineReport;
            string body;
            if (plan == null)
                body = "There is no offline report to show. Coins already added to the mine stay there.";
            else
            {
                body = "Added " + NumericFormat.Format(plan.Coins) + " coins from " +
                    Mathf.RoundToInt((float)plan.SecondsSimulated / 60f) + " minutes of offline work.";
                if (plan.Capped)
                    body += "\n\nThe away time was longer than the " +
                        Mathf.RoundToInt((float)session.Config.OfflineCapSeconds / 3600f) + " hour cap, so earnings stop there.";
                if (plan.Anomaly == "backward")
                    body += "\n\nThe device clock moved backward. No offline earnings were added.";
                else if (plan.Anomaly == "implausible_forward")
                    body += "\n\nThe device clock jumped by a very long time. Earnings were still limited to the cap. This is not a server check.";
                else if (plan.Anomaly == "invalid")
                    body += "\n\nThe elapsed time was not a usable number. Nothing was added.";
                body += "\n\nThese coins are already in your balance. Continue does not add them again.";
            }
            RectTransform offlineContent;
            RectTransform offlineScroll = UiFactory.Scroll(card, out offlineContent);
            Place(offlineScroll, 0.03f, 0.28f, 0.97f, 0.86f);
            Text text = UiFactory.Label(offlineContent, body, 30, UiTheme.Paper, TextAnchor.UpperLeft);
            if (session.CanOfferOfflineBonus())
            {
                Button watch = UiFactory.Press(card, session.OfflineBonusLabel(), delegate
                {
                    session.BeginOfflineAd();
                    if (session.PendingReward != null)
                        Push(Modal.Ad, true);
                }, UiTheme.ButtonOn, 110f);
                Destroy(watch.GetComponent<LayoutElement>());
                Place(watch.GetComponent<RectTransform>(), 0.05f, 0.14f, 0.95f, 0.26f);
            }
        }

        void BuildAd(RectTransform card)
        {
            if (!DevelopmentFeatures.TestAdsAvailable)
            {
                BuildInfo(card, "Mine", "Keep playing. This game has no advertisements.");
                return;
            }
            Header(card, "Development test ad");
            PendingReward pending = session.PendingReward;
            string reward = pending == null ? "This offer is no longer available." : pending.Description;
            Text text = UiFactory.Label(card,
                "This is a development test, not a real advertisement. It does not contact a network and it does not ask to track you.\n\n" +
                reward + "\n\nOnly Complete grants the reward. Cancel, Fail, No fill, and Timeout grant nothing.",
                28, UiTheme.Paper, TextAnchor.UpperLeft);
            Place(text.rectTransform, 0.05f, 0.42f, 0.95f, 0.86f);
            AddAdButton(card, "Complete", TestAdOutcome.Completed, 0.05f, 0.28f, 0.48f, 0.4f);
            AddAdButton(card, "Cancel", TestAdOutcome.Cancelled, 0.52f, 0.28f, 0.95f, 0.4f);
            AddAdButton(card, "Fail", TestAdOutcome.Failed, 0.05f, 0.16f, 0.32f, 0.26f);
            AddAdButton(card, "No fill", TestAdOutcome.NoFill, 0.34f, 0.16f, 0.64f, 0.26f);
            AddAdButton(card, "Timeout", TestAdOutcome.Timeout, 0.66f, 0.16f, 0.95f, 0.26f);
        }

        void AddAdButton(RectTransform card, string label, TestAdOutcome outcome, float x0, float y0, float x1, float y1)
        {
            Button button = UiFactory.Press(card, label, delegate
            {
                if (outcome == TestAdOutcome.Completed)
                {
                    bool granted = session.ResolveTestAd(outcome);
                    if (!granted)
                    {
                        ShowToast("The reward was not granted. You can try Complete again.");
                        return;
                    }
                    ShowToast("Reward added.");
                }
                else
                {
                    session.ResolveTestAd(outcome);
                    ShowToast("No reward.");
                }
                Pop();
            }, outcome == TestAdOutcome.Completed ? UiTheme.ButtonOn : UiTheme.Button, 90f);
            Destroy(button.GetComponent<LayoutElement>());
            Place(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            UiFactory.Caption(button).fontSize = 26;
        }

        void BuildQuit(RectTransform card)
        {
            Text text = UiFactory.Label(card, "Leave the mine?\n\nProgress is saved on this device when you leave.", 34, UiTheme.Paper, TextAnchor.UpperLeft);
            Place(text.rectTransform, 0.06f, 0.4f, 0.94f, 0.9f);
            Button stay = UiFactory.Press(card, "Stay", Pop, UiTheme.ButtonOn, 110f);
            Destroy(stay.GetComponent<LayoutElement>());
            Place(stay.GetComponent<RectTransform>(), 0.06f, 0.2f, 0.94f, 0.34f);
            Button leave = UiFactory.Press(card, "Leave", delegate { GameSession.Quit(); }, UiTheme.Danger, 110f);
            Destroy(leave.GetComponent<LayoutElement>());
            Place(leave.GetComponent<RectTransform>(), 0.06f, 0.06f, 0.94f, 0.18f);
        }

        void BuildNewer(RectTransform card)
        {
            Text text = UiFactory.Label(card,
                "This save was written by a newer version.\n\nIt was not overwritten. You can leave, or start a separate mine and keep the newer file on this device.",
                32, UiTheme.Paper, TextAnchor.UpperLeft);
            Place(text.rectTransform, 0.06f, 0.4f, 0.94f, 0.92f);
            Button leave = UiFactory.Press(card, "Leave", delegate { GameSession.Quit(); }, UiTheme.Button, 110f);
            Destroy(leave.GetComponent<LayoutElement>());
            Place(leave.GetComponent<RectTransform>(), 0.06f, 0.22f, 0.94f, 0.36f);
            Button fresh = UiFactory.Press(card, freshArmed ? "Confirm new mine" : "Start a new mine", delegate
            {
                if (!freshArmed)
                {
                    freshArmed = true;
                    built = Modal.NotRendered;
                    SyncModal();
                    return;
                }
                if (session.StartFreshBesideNewerSave())
                {
                    freshArmed = false;
                    stack.Clear();
                    built = Modal.NotRendered;
                    upgradeMine = "";
                    SyncModal();
                    PlaceUpgradeHost();
                    ShowToast("A new mine was started. The newer save file was kept.");
                }
            }, UiTheme.Danger, 110f);
            Destroy(fresh.GetComponent<LayoutElement>());
            Place(fresh.GetComponent<RectTransform>(), 0.06f, 0.06f, 0.94f, 0.2f);
        }

        void BuildReset(RectTransform card)
        {
            Text text = UiFactory.Label(card,
                resetArmed
                    ? "Confirm reset. Coins, diamonds, upgrades, and mastery on this device will be deleted. This does not affect any other device."
                    : "Reset progress on this device? This asks a second time before anything is deleted.",
                32, UiTheme.Paper, TextAnchor.UpperLeft);
            Place(text.rectTransform, 0.06f, 0.4f, 0.94f, 0.92f);
            Button cancel = UiFactory.Press(card, "Cancel", delegate { resetArmed = false; Pop(); }, UiTheme.ButtonOn, 110f);
            Destroy(cancel.GetComponent<LayoutElement>());
            Place(cancel.GetComponent<RectTransform>(), 0.06f, 0.22f, 0.94f, 0.36f);
            Button confirm = UiFactory.Press(card, resetArmed ? "Delete progress" : "Continue", delegate
            {
                if (!resetArmed)
                {
                    resetArmed = true;
                    built = Modal.NotRendered;
                    SyncModal();
                    return;
                }
                if (session.ResetProgress())
                {
                    resetArmed = false;
                    stack.Clear();
                    built = Modal.NotRendered;
                    upgradeMine = "";
                    SyncModal();
                    PlaceUpgradeHost();
                    ShowToast("Progress on this device was reset.");
                }
            }, UiTheme.Danger, 110f);
            Destroy(confirm.GetComponent<LayoutElement>());
            Place(confirm.GetComponent<RectTransform>(), 0.06f, 0.06f, 0.94f, 0.2f);
        }

        static void Preferred(Text text, float height)
        {
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        static void PreferredRect(RectTransform rect, float height)
        {
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }
    }
}
