using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aether
{
    public class AetherApp : MonoBehaviour
    {
        private enum Page { Overview, Health, Risks, Twin, Crew, Assessments }

        private const string Mission = "MARS TRANSIT / CREW HEALTH";
        private readonly string[] crewIds = { "CMDR-01", "MED-02", "SCI-03" };
        private readonly string[] crewNames = { "Alex Morgan", "Jordan Lee", "Sam Rivera" };
        private readonly string[] crewRoles = { "Mission commander", "Crew medical officer", "Flight engineer" };
        private readonly string[] pageNames = { "Overview", "Health Monitor", "Spaceflight Risks", "Health Twin", "Crew Network", "Assessments" };
        private readonly string[] pageDescriptions =
        {
            "Personalized, explainable mission health intelligence",
            "Observe trends, review personal baselines, record new measurements",
            "NASA five-hazard knowledge and practical prevention guidance",
            "Interactive anatomical hologram with selectable medical systems",
            "Best-effort LAN alert demo with human acknowledgment",
            "Guided self-assessment, scenario simulation and handoff"
        };

        private AppState state;
        private AetherNetwork network;
        private Page currentPage;
        private int selectedRisk;
        private int selectedBody;
        private float twinZoom = 1.0f;
        private Canvas canvas;
        private RectTransform navContent;
        private RectTransform pageRoot;
        private RectTransform modalOverlay;
        private RectTransform modalCard;
        private RectTransform toastOverlay;
        private Text toastText;
        private Text pageTitle;
        private Text pageDescription;
        private Text networkText;
        private float nextHello;
        private float toastExpires;
        private bool exiting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<AetherApp>() == null)
                new GameObject("AETHER App Runtime").AddComponent<AetherApp>();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            state = Storage.Load();
            network = new AetherNetwork();
            network.Received += HandlePacket;
            network.Start();
            CreateInterface();
            Navigate(Page.Overview);
            SendHello();
        }

        private void Update()
        {
            if (network != null)
            {
                network.Poll();
                if (Time.realtimeSinceStartup >= nextHello)
                {
                    SendHello();
                    nextHello = Time.realtimeSinceStartup + 5f;
                    if (networkText != null) networkText.text = network.Status;
                }
            }
            if (toastOverlay != null && toastOverlay.gameObject.activeSelf && Time.unscaledTime > toastExpires)
                toastOverlay.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (network != null) { network.Received -= HandlePacket; network.Dispose(); }
            if (!exiting && state != null) Storage.Save(state);
        }

        private void OnApplicationQuit()
        {
            exiting = true;
            if (state != null) Storage.Save(state);
            if (network != null) network.Dispose();
        }

        private void SendHello()
        {
            if (network != null && state != null)
                network.Send("HELLO", state.selectedCrew, CurrentCrewName());
        }

        private string CurrentCrewName()
        {
            for (int i = 0; i < crewIds.Length; i++)
                if (crewIds[i] == state.selectedCrew) return crewNames[i];
            return "Crew member";
        }

        private void CreateInterface()
        {
            if (EventSystem.current == null)
            {
                GameObject input = new GameObject("UI Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(input);
            }
            GameObject canvasObject = new GameObject("AETHER Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.55f;

            RectTransform background = Ui.Panel("Background", canvas.transform, Ui.Background);
            Ui.Stretch(background, 0, 0, 0, 0);

            RectTransform sidebar = Ui.Panel("Sidebar", background, Ui.Sidebar);
            Ui.Anchor(sidebar, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(252, 0));
            Ui.Vertical(sidebar, 15, 11f);
            Ui.Label(sidebar, "AETHER", 32, Ui.Ink, 52, FontStyle.Bold);
            Ui.Label(sidebar, "CREW HEALTH INTELLIGENCE", 11, Ui.Cyan, 30, FontStyle.Bold);
            Ui.Space(sidebar, 13);
            navContent = Ui.Rect("Navigation", sidebar);
            Ui.Vertical(navContent, 0, 7f);
            Ui.Size(navContent, 6 * 51 + 5 * 7);
            Ui.Space(sidebar, 13);
            Ui.Label(sidebar, "MISSION TELEMETRY", 12, Ui.Cyan, 28, FontStyle.Bold);
            Ui.Label(sidebar, "MARS TRANSIT / DAY 142\nSYNTHETIC DATA\nOFFLINE-CAPABLE HEALTH LOGIC", 12,
                Ui.Muted, 83);
            networkText = Ui.Label(sidebar, "LAN DEMO", 10, Ui.Amber, 72);
            Ui.Label(sidebar, "RESEARCH PROTOTYPE\nNOT FOR MEDICAL OR FLIGHT USE", 11,
                Ui.Muted, 55);

            RectTransform header = Ui.Panel("Header", background, Ui.Background);
            Ui.Anchor(header, new Vector2(0, 1), Vector2.one,
                new Vector2(269, -110), new Vector2(-24, -13));
            Ui.Horizontal(header, 9, 16);
            RectTransform leftHeader = Ui.Column(header, 0);
            Ui.Label(leftHeader, "AETHER  /  " + Mission, 12, Ui.Cyan, 25, FontStyle.Bold);
            pageTitle = Ui.Label(leftHeader, "Mission Health Overview", 28, Ui.Ink, 41, FontStyle.Bold);
            pageDescription = Ui.Label(leftHeader, "", 13, Ui.Muted, 28);
            RectTransform rightHeader = Ui.Column(header, 8);
            LayoutElement headerRightLayout = rightHeader.gameObject.AddComponent<LayoutElement>();
            headerRightLayout.preferredWidth = 265;
            headerRightLayout.flexibleWidth = 0;
            Ui.Action(rightHeader, "EMERGENCY SOS", OpenSosModal, Ui.Red, Ui.Background, 48, 17);
            Ui.Label(rightHeader, "CONFIRM BEFORE TRANSMISSION", 10, Ui.Muted, 22, alignment: TextAnchor.MiddleCenter);

            ScrollRect scroll = Ui.Scroll(background, 114, 263);
            pageRoot = scroll.content;

            toastOverlay = Ui.Panel("Notification", background, Ui.Surface);
            Ui.Anchor(toastOverlay, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-480, 24), new Vector2(-24, 105));
            Ui.Vertical(toastOverlay, 12, 2);
            toastText = Ui.Label(toastOverlay, "", 13, Ui.Ink, 65);
            toastOverlay.gameObject.SetActive(false);

            modalOverlay = Ui.Panel("Modal Scrim", background, new Color(0.015f, 0.035f, 0.07f, 0.94f));
            Ui.Stretch(modalOverlay, 0, 0, 0, 0);
            modalCard = Ui.Panel("Modal Content", modalOverlay, Ui.Card);
            Ui.Anchor(modalCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-330, -250), new Vector2(330, 250));
            Ui.Vertical(modalCard, 22, 15);
            modalOverlay.gameObject.SetActive(false);
        }

        private void Navigate(Page page)
        {
            currentPage = page;
            pageTitle.text = page == Page.Overview ? "Mission Health Overview" : pageNames[(int)page];
            pageDescription.text = pageDescriptions[(int)page];
            RenderNavigation();
            RenderPage();
        }

        private void RenderNavigation()
        {
            Ui.Clear(navContent);
            for (int i = 0; i < pageNames.Length; i++)
            {
                int target = i;
                Ui.Action(navContent, "  " + (i + 1) + "    " + pageNames[i],
                    () => Navigate((Page)target), currentPage == (Page)i ? Ui.Surface : Ui.Sidebar,
                    currentPage == (Page)i ? Ui.Cyan : Ui.Muted, 49f);
            }
        }

        private void RenderPage()
        {
            if (pageRoot == null) return;
            Ui.Clear(pageRoot);
            RenderAlertBanner(pageRoot);
            switch (currentPage)
            {
                case Page.Overview: RenderOverview(); break;
                case Page.Health: RenderHealth(); break;
                case Page.Risks: RenderRisks(); break;
                case Page.Twin: RenderTwin(); break;
                case Page.Crew: RenderCrew(); break;
                case Page.Assessments: RenderAssessments(); break;
            }
            LayoutRebuilder.MarkLayoutForRebuild(pageRoot);
        }

        private void RenderAlertBanner(Transform parent)
        {
            EmergencyEvent recent = null;
            for (int i = state.alerts.Count - 1; i >= 0; i--)
            {
                if (!state.alerts[i].acknowledgedBy.Contains(state.selectedCrew))
                {
                    recent = state.alerts[i];
                    break;
                }
            }
            if (recent == null) return;
            EmergencyEvent alert = recent;
            RectTransform banner = Ui.CardPanel(parent, 115, 13);
            Image bg = banner.GetComponent<Image>();
            bg.color = Ui.Hex("4A2537");
            Ui.Label(banner, "ACTIVE CREW EMERGENCY  /  " + alert.sender.ToUpperInvariant(),
                18, Ui.Red, 30, FontStyle.Bold);
            RectTransform row = Ui.Row(banner, 50);
            Ui.Label(row, alert.reason + "  |  " + alert.utc + " UTC", 13, Ui.Ink, 50);
            Ui.Action(row, "ACKNOWLEDGE", () => Acknowledge(alert), Ui.Red, Ui.Background, 48f);
        }

        private void RenderOverview()
        {
            HealthAssessment evaluation = HealthLogic.Assess(state);
            Ui.Label(pageRoot, "LIVE MISSION SNAPSHOT  /  DAY " + state.missionDay, 12, Ui.Cyan, 30, FontStyle.Bold);
            RectTransform row = Ui.Row(pageRoot, 151);
            Metric(row, "RESTING HEART RATE", evaluation.latestHeartRate.ToString("0") + " bpm",
                "Personal baseline " + evaluation.heartRateBaseline.ToString("0.0") + " bpm", Ui.Cyan);
            Metric(row, "SLEEP DURATION", evaluation.latestSleep.ToString("0.0") + " h",
                "Personal baseline " + evaluation.sleepBaseline.ToString("0.0") + " h", Ui.Green);
            Metric(row, "SELF-REPORT", state.samples[state.samples.Count - 1].mood,
                "Latest recorded mood", Ui.Amber);
            Metric(row, "HEALTH REVIEW", evaluation.classification,
                "Illustrative change-detection rules", evaluation.classification == "REVIEW SUGGESTED" ? Ui.Amber : Ui.Green);

            RectTransform chartRow = Ui.Row(pageRoot, 345);
            RectTransform chartCard = Ui.CardPanel(chartRow, 345);
            Ui.Label(chartCard, "RESTING HEART RATE  /  TREND", 15, Ui.Ink, 32, FontStyle.Bold);
            Graph(chartCard, 243, evaluation.heartRateBaseline);
            Ui.Label(chartCard, "Synthetic demo history unless manually entered  |  Amber = reference baseline", 12, Ui.Muted, 23);
            RectTransform missionCard = Ui.CardPanel(chartRow, 345);
            Ui.Label(missionCard, "MISSION INTELLIGENCE", 15, Ui.Ink, 31, FontStyle.Bold);
            Ui.Label(missionCard, "05", 43, Ui.Cyan, 56, FontStyle.Bold);
            Ui.Label(missionCard, "NASA spaceflight hazards", 17, Ui.Ink, 30);
            Ui.Label(missionCard, "Explore measurable indicators, preventive countermeasures, limitations and safe escalation for each hazard.", 14, Ui.Muted, 100);
            Ui.Action(missionCard, "EXPLORE SPACEFLIGHT RISKS", () => Navigate(Page.Risks), Ui.Surface, Ui.Cyan, 45);

            RectTransform infoRow = Ui.Row(pageRoot, 206);
            RectTransform insight = Ui.CardPanel(infoRow, 206);
            Ui.Label(insight, "PRIORITY HEALTH ASSESSMENT", 16, Ui.Ink, 32, FontStyle.Bold);
            Ui.Label(insight, evaluation.explanation, 14, Ui.Muted, 68);
            Ui.Action(insight, "START GUIDED CHECK", () => Navigate(Page.Assessments), Ui.Cyan, Ui.Background, 42);
            RectTransform crew = Ui.CardPanel(infoRow, 206);
            Ui.Label(crew, "CREW SAFETY NETWORK", 16, Ui.Ink, 32, FontStyle.Bold);
            Ui.Label(crew, "Emergency notifications + crew acknowledgments across connected desktop clients on a LAN. No assurance of delivery.", 14,
                Ui.Muted, 70);
            Ui.Action(crew, "OPEN CREW NETWORK", () => Navigate(Page.Crew), Ui.Surface, Ui.Cyan, 43);
            Footer(pageRoot);
        }

        private void Metric(Transform parent, string heading, string number, string note, Color accent)
        {
            RectTransform card = Ui.CardPanel(parent, 151, 14);
            Ui.Label(card, heading, 11, Ui.Muted, 23, FontStyle.Bold);
            Ui.Label(card, number, number.Length > 16 ? 18 : 30, accent, 56, FontStyle.Bold);
            Ui.Label(card, note, 11, Ui.Muted, 28);
        }

        private void Graph(Transform parent, float height, float baseline)
        {
            RectTransform graphRoot = Ui.Panel("Graph", parent, Ui.Hex("0C1D31"));
            Ui.Size(graphRoot, height);
            HeartRateGraph graph = graphRoot.gameObject.AddComponent<HeartRateGraph>();
            graph.Configure(state.samples, baseline);
        }

        private void RenderHealth()
        {
            HealthAssessment evaluation = HealthLogic.Assess(state);
            Ui.Label(pageRoot, "PERSONALIZED MEASUREMENT HISTORY", 12, Ui.Cyan, 29, FontStyle.Bold);
            RectTransform row = Ui.Row(pageRoot, 153);
            Metric(row, "LAST RESTING HR", evaluation.latestHeartRate.ToString("0") + " bpm", "Change: " + evaluation.heartRateDrift.ToString("+0;-0;0") + " bpm", Ui.Cyan);
            Metric(row, "LAST SLEEP", evaluation.latestSleep.ToString("0.0") + " h", "Change: " + evaluation.sleepDrift.ToString("+0.0;-0.0;0.0") + " h", Ui.Green);
            Metric(row, "LAST TEMPERATURE", state.samples[state.samples.Count - 1].temperature.ToString("0.0") + " °C",
                "Measurement only; no diagnosis", Ui.Amber);

            RectTransform recordRow = Ui.Row(pageRoot, 420);
            RectTransform history = Ui.CardPanel(recordRow, 420);
            Ui.Label(history, "HEART RATE AND BASELINE", 16, Ui.Ink, 34, FontStyle.Bold);
            Graph(history, 255, evaluation.heartRateBaseline);
            Ui.Label(history, "This baseline is derived from a labeled synthetic reference week. Demo flags (+8 bpm, -1.5 h) are not clinical cutoffs.",
                12, Ui.Muted, 61);
            RectTransform entry = Ui.CardPanel(recordRow, 420);
            Ui.Label(entry, "RECORD NEW MEASUREMENT", 16, Ui.Ink, 34, FontStyle.Bold);
            RectTransform form = Ui.Row(entry, 73);
            FormField(form, "RESTING HR / BPM", evaluation.latestHeartRate.ToString("0"), out InputField hr);
            FormField(form, "SLEEP / H", evaluation.latestSleep.ToString("0.0", CultureInfo.InvariantCulture), out InputField sleep);
            RectTransform second = Ui.Row(entry, 73);
            FormField(second, "TEMP / °C", state.samples[state.samples.Count - 1].temperature.ToString("0.0", CultureInfo.InvariantCulture), out InputField temperature);
            InputField mood;
            FormField(second, "MOOD (Good / Neutral / Stressed)", "Good", out mood);
            Ui.Label(entry, "Manual measurements are stored locally. Enter Good, Neutral, Stressed or Very stressed for mood.", 11, Ui.Muted, 45);
            Ui.Action(entry, "SAVE MEASUREMENT", () =>
            {
                HealthSample sample;
                string error;
                string normalizedMood = mood.text.Trim();
                if (string.IsNullOrEmpty(normalizedMood)) normalizedMood = "Neutral";
                if (!HealthLogic.TryCreateSample(hr.text, sleep.text, temperature.text, normalizedMood, out sample, out error))
                {
                    Toast(error);
                    return;
                }
                state.samples.Add(sample);
                Storage.Save(state);
                Toast("Measurement saved locally. Trends recomputed.");
                RenderPage();
            }, Ui.Cyan, Ui.Background, 46);
            RectTransform baselineCard = Ui.CardPanel(pageRoot, 155);
            Ui.Label(baselineCard, "EXPLAINABLE HEALTH REVIEW", 16, Ui.Ink, 31, FontStyle.Bold);
            Ui.Label(baselineCard, evaluation.explanation, 14, Ui.Muted, 43);
            Ui.Label(baselineCard, evaluation.recommendation, 13, Ui.Cyan, 43);
            Footer(pageRoot);
        }

        private void FormField(Transform parent, string heading, string initial, out InputField input)
        {
            RectTransform wrapper = Ui.Column(parent, 4);
            Ui.Label(wrapper, heading, 10, Ui.Muted, 22, FontStyle.Bold);
            input = Ui.Input(wrapper, initial);
        }

        private void RenderRisks()
        {
            Ui.Label(pageRoot, "THE FIVE HAZARDS OF HUMAN SPACEFLIGHT", 12, Ui.Cyan, 29, FontStyle.Bold);
            RectTransform intro = Ui.CardPanel(pageRoot, 135);
            Ui.Label(intro, "RISK INTELLIGENCE  /  NASA HUMAN RESEARCH PROGRAM", 16, Ui.Ink, 30, FontStyle.Bold);
            Ui.Label(intro,
                "Explore major spaceflight hazards, observable indicators and potential countermeasures. Educational summaries only. Follow mission-approved medical and emergency procedures for actual care.",
                14, Ui.Muted, 65);
            for (int i = 0; i < SpaceflightRisks.All.Length; i++)
            {
                int index = i;
                SpaceflightRisk risk = SpaceflightRisks.All[i];
                RectTransform riskRow = Ui.Row(pageRoot, 112);
                RectTransform card = Ui.CardPanel(riskRow, 112, 12);
                card.GetComponent<Image>().color = i == selectedRisk ? Ui.Surface : Ui.Card;
                Ui.Label(card, "0" + (i + 1) + "  /  " + risk.name.ToUpperInvariant(), 15, i == selectedRisk ? Ui.Cyan : Ui.Ink, 31, FontStyle.Bold);
                Ui.Label(card, risk.effect, 12, Ui.Muted, 40);
                Ui.Action(riskRow, i == selectedRisk ? "SELECTED" : "VIEW RISK", () => { selectedRisk = index; RenderPage(); },
                    Ui.Surface, Ui.Cyan, 54);
            }
            SpaceflightRisk selected = SpaceflightRisks.All[selectedRisk];
            RectTransform detail = Ui.CardPanel(pageRoot, 540);
            Ui.Label(detail, "SELECTED RISK  /  " + selected.name.ToUpperInvariant(), 18, Ui.Cyan, 39, FontStyle.Bold);
            RiskSection(detail, "WHAT CAN HAPPEN", selected.effect);
            RiskSection(detail, "WHAT TO MEASURE", selected.indicators);
            RiskSection(detail, "PREVENTION AND COUNTERMEASURES", selected.prevention);
            RiskSection(detail, "WHAT TO DO WHEN CONCERNED", selected.response);
            Ui.Action(detail, "OPEN NASA RESEARCH SOURCE", () => Application.OpenURL(selected.source), Ui.Surface, Ui.Cyan, 43);
            Footer(pageRoot);
        }

        private void RiskSection(Transform parent, string heading, string body)
        {
            Ui.Label(parent, heading, 12, Ui.Amber, 21, FontStyle.Bold);
            Ui.Label(parent, body, 13, Ui.Ink, 45);
        }

        private readonly string[] bodyRegions = { "Brain / behavior", "Cardiovascular", "Musculoskeletal", "Respiratory / habitat", "Eyes / vision" };
        private readonly string[] bodyDescriptions =
        {
            "Sleep, mood, fatigue and optional validated cognitive tasks may indicate wellbeing changes. These measures cannot diagnose neurological or psychiatric disease.",
            "Resting heart rate and blood pressure can help monitor cardiovascular trends. Interpret shifts with exercise, hydration, workload and measurement quality.",
            "Exercise completion and functional performance may show changes worth reviewing. Bone density and muscle mass require appropriate validated methods.",
            "Habitat environmental sensors and symptoms inform respiratory and environmental safety reviews. Do not infer air composition from body telemetry.",
            "Vision complaints require appropriate assessment. Spaceflight-associated neuro-ocular syndrome cannot be diagnosed by routine heart-rate or sleep data."
        };


    private void RenderTwin()
    {
        HealthAssessment evaluation = HealthLogic.Assess(state);
        Ui.Label(pageRoot, "AETHER ANATOMICAL HEALTH TWIN  /  EDUCATIONAL INTERACTIVE VISUALIZATION", 12, Ui.Cyan, 32, FontStyle.Bold);

        RectTransform row = Ui.Row(pageRoot, 810, 16f);
        RectTransform figureCard = Ui.CardPanel(row, 810, 15);
        Ui.Label(figureCard, "HUMAN SYSTEMS  /  ANATOMICAL SCAN", 17, Ui.Ink, 32, FontStyle.Bold);
        RectTransform figure = Ui.Panel("Detailed anatomical hologram", figureCard, Ui.Hex("071526"));
        Ui.Size(figure, 668);
        figure.gameObject.AddComponent<RectMask2D>();

        RectTransform scan = Ui.Rect("Interactive hologram artwork", figure);
        scan.anchorMin = new Vector2(.5f, .5f);
        scan.anchorMax = new Vector2(.5f, .5f);
        scan.pivot = new Vector2(.5f, .5f);
        scan.sizeDelta = new Vector2(345, 657);
        scan.anchoredPosition = Vector2.zero;
        scan.localScale = Vector3.one * twinZoom;

        RawImage anatomy = scan.gameObject.AddComponent<RawImage>();
        anatomy.texture = Resources.Load<Texture2D>("Twin/aether_anatomical_hologram");
        anatomy.color = Color.white;
        anatomy.raycastTarget = false;
        if (anatomy.texture == null)
            Debug.LogWarning("AETHER anatomical art is missing: Assets/Resources/Twin/aether_anatomical_hologram.png");

        AnatomicalHotspot(scan, "+", .50f, .925f, 0, "Brain / behavioral health");
        AnatomicalHotspot(scan, "+", .49f, .865f, 4, "Eyes / vision");
        AnatomicalHotspot(scan, "+", .54f, .717f, 1, "Cardiovascular system");
        AnatomicalHotspot(scan, "+", .36f, .738f, 3, "Respiratory system");
        AnatomicalHotspot(scan, "+", .36f, .285f, 2, "Musculoskeletal system");

        RectTransform controls = Ui.Row(figureCard, 39, 8);
        Ui.Action(controls, "ZOOM -", () => { twinZoom = Mathf.Clamp(twinZoom - .12f, .78f, 1.7f); RenderPage(); },
            Ui.Surface, Ui.Cyan, 39, 12);
        Ui.Action(controls, "RESET", () => { twinZoom = 1; selectedBody = 0; RenderPage(); },
            Ui.Surface, Ui.Ink, 39, 12);
        Ui.Action(controls, "ZOOM +", () => { twinZoom = Mathf.Clamp(twinZoom + .12f, .78f, 1.7f); RenderPage(); },
            Ui.Surface, Ui.Cyan, 39, 12);

        RectTransform detail = Ui.CardPanel(row, 810, 19);
        Ui.Label(detail, "SYSTEM EXPLORER", 13, Ui.Cyan, 25, FontStyle.Bold);
        Ui.Label(detail, bodyRegions[selectedBody].ToUpperInvariant(), 22, Ui.Ink, 50, FontStyle.Bold);
        Ui.Label(detail, bodyDescriptions[selectedBody], 14, Ui.Muted, 107);
        Ui.Label(detail, "SELECT AN ANATOMICAL SYSTEM", 12, Ui.Amber, 27, FontStyle.Bold);
        string[] sections = { "BRAIN / BEHAVIOR", "HEART / CIRCULATION", "MUSCLE / BONE", "LUNGS / HABITAT", "EYES / VISION" };
        for (int i = 0; i < sections.Length; i++)
        {
            int region = i;
            Ui.Action(detail, (selectedBody == i ? "●  " : "○  ") + sections[i],
                () => { selectedBody = region; RenderPage(); },
                selectedBody == i ? Ui.Hex("17435A") : Ui.Surface,
                selectedBody == i ? Ui.Cyan : Ui.Ink, 43, 12);
        }
        Ui.Space(detail, 6);
        Ui.Label(detail, "PERSONAL HEALTH OBSERVATIONS", 12, Ui.Amber, 29, FontStyle.Bold);
        Ui.Label(detail, evaluation.explanation, 13, Ui.Ink, 102);
        Ui.Action(detail, "REVIEW MEASUREMENTS", () => Navigate(Page.Health), Ui.Surface, Ui.Cyan, 43);
        Ui.Action(detail, "EXPLORE SPACEFLIGHT HAZARDS", () => Navigate(Page.Risks), Ui.Cyan, Ui.Background, 44);

        RectTransform note = Ui.CardPanel(pageRoot, 148);
        Ui.Label(note, "WHAT YOU ARE SEEING", 16, Ui.Ink, 31, FontStyle.Bold);
        Ui.Label(note,
            "This detailed holographic anatomical reference is an interactive 2D visual asset, based on the user-provided medical-grid illustration. The visible internal organs, skeleton, nerves and glowing particles are illustrative, NOT a live scan or a validated patient-specific 3D model. Click the cyan hotspots or use the system explorer. Only recorded measurements are treated as observations.",
            13, Ui.Muted, 87);
        Footer(pageRoot);
    }

    private void AnatomicalHotspot(Transform scan, string label, float x, float y, int region, string description)
    {
        Button button = Ui.Action(scan, label, () => { selectedBody = region; RenderPage(); },
            selectedBody == region ? Ui.Amber : Ui.Cyan,
            Ui.Background, 35, 23);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x, y);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(35, 35);
        button.gameObject.name = "Hotspot: " + description;
    }

        private void RenderCrew()
        {
            Ui.Label(pageRoot, "CREW-TO-CREW EMERGENCY COMMUNICATION", 12, Ui.Cyan, 32, FontStyle.Bold);
            RectTransform banner = Ui.CardPanel(pageRoot, 145);
            Ui.Label(banner, "CREW NETWORK / LAN DEMONSTRATION", 17, Ui.Ink, 31, FontStyle.Bold);
            Ui.Label(banner,
                "Desktop builds broadcast SOS, presence and acknowledgments over local UDP on port 34862. Broadcast may be blocked; there is NO encryption, authentication or guaranteed delivery. Never use for actual emergencies.",
                13, Ui.Muted, 75);
            RectTransform info = Ui.Row(pageRoot, 165);
            for (int i = 0; i < crewIds.Length; i++)
            {
                int member = i;
                bool local = state.selectedCrew == crewIds[i];
                bool online = network != null && network.IsOnline(crewIds[i]);
                RectTransform card = Ui.CardPanel(info, 165, 12);
                Ui.Label(card, crewNames[i], 16, Ui.Ink, 35, FontStyle.Bold);
                Ui.Label(card, crewRoles[i] + "\n" + (local ? "THIS DEVICE" : online ? "LAN PRESENCE SEEN" : "NO PRESENCE DETECTED"),
                    12, local ? Ui.Green : online ? Ui.Cyan : Ui.Muted, 54);
                Ui.Action(card, local ? "CURRENT PROFILE" : "USE ON THIS DEVICE", () =>
                {
                    state.selectedCrew = crewIds[member];
                    Storage.Save(state);
                    SendHello();
                    Toast("Active demo identity: " + crewNames[member]);
                    RenderPage();
                }, Ui.Surface, Ui.Cyan, 41);
            }
            RectTransform request = Ui.CardPanel(pageRoot, 160);
            Ui.Label(request, "SEND A CREW-WIDE EMERGENCY ALERT", 17, Ui.Red, 35, FontStyle.Bold);
            Ui.Label(request, "The SOS action requires confirmation. It is never triggered automatically by a health trend.",
                13, Ui.Muted, 40);
            Ui.Action(request, "OPEN EMERGENCY SOS CONFIRMATION", OpenSosModal, Ui.Red, Ui.Background, 48);

            Ui.Label(pageRoot, "EMERGENCY ACTIVITY", 16, Ui.Ink, 33, FontStyle.Bold);
            if (state.alerts.Count == 0)
            {
                RectTransform empty = Ui.CardPanel(pageRoot, 98);
                Ui.Label(empty, "No demo emergency events. Send a test SOS from one Unity desktop client and observe it on another connected client.",
                    13, Ui.Muted, 55);
            }
            else
            {
                for (int i = state.alerts.Count - 1; i >= Mathf.Max(0, state.alerts.Count - 12); i--)
                {
                    EmergencyEvent evt = state.alerts[i];
                    RectTransform item = Ui.CardPanel(pageRoot, 127);
                    Ui.Label(item, "SOS   /   " + evt.sender + "   /   " + evt.utc + " UTC", 14, Ui.Red, 31, FontStyle.Bold);
                    Ui.Label(item, evt.reason + "\nAcknowledged profiles: " + evt.acknowledgedBy.Count + " / " + crewIds.Length +
                        " (not a delivery guarantee)", 12, Ui.Ink, 42);
                    if (!evt.acknowledgedBy.Contains(state.selectedCrew))
                        Ui.Action(item, "ACKNOWLEDGE AND BROADCAST RECEIPT", () => Acknowledge(evt), Ui.Surface, Ui.Cyan, 38);
                    else
                        Ui.Label(item, "Acknowledged by this profile", 12, Ui.Green, 31);
                }
            }
            Footer(pageRoot);
        }

        private void OpenSosModal()
        {
            OpenModal();
            Ui.Label(modalCard, "EMERGENCY SOS / CREW ALERT", 22, Ui.Red, 51, FontStyle.Bold);
            Ui.Label(modalCard,
                "This demonstration sends a best-effort message to other running AETHER desktop clients on the same LAN. It does not confirm delivery to every crew member.",
                14, Ui.Ink, 77);
            Ui.Label(modalCard, "REASON / SITUATION", 12, Ui.Muted, 26, FontStyle.Bold);
            InputField reason = Ui.Input(modalCard, "Medical assistance requested", "Briefly describe emergency");
            Ui.Space(modalCard, 16);
            RectTransform actions = Ui.Row(modalCard, 52);
            Ui.Action(actions, "CANCEL", CloseModal, Ui.Surface, Ui.Ink, 52);
            Ui.Action(actions, "CONFIRM SOS", () =>
            {
                string value = reason.text.Trim();
                if (value.Length < 3) { Toast("Enter a short situation description."); return; }
                SendSos(value);
                CloseModal();
            }, Ui.Red, Ui.Background, 52);
        }

        private void SendSos(string reason)
        {
            EmergencyEvent evt = new EmergencyEvent
            {
                id = Guid.NewGuid().ToString("N"), utc = DateTime.UtcNow.ToString("u"),
                sender = CurrentCrewName(), senderId = state.selectedCrew,
                reason = reason.Length > 250 ? reason.Substring(0, 250) : reason,
                isLocal = true
            };
            state.alerts.Add(evt);
            Storage.Save(state);
            network.Send("SOS", state.selectedCrew, evt.sender, evt.reason, evt.id);
            Toast("SOS recorded and LAN broadcast attempted. Ask peers to acknowledge receipt.");
            Navigate(Page.Crew);
        }

        private void HandlePacket(CrewPacket packet)
        {
            if (packet.kind == "SOS")
            {
                foreach (EmergencyEvent existing in state.alerts)
                    if (existing.id == packet.alertId) return;
                EmergencyEvent incoming = new EmergencyEvent
                {
                    id = packet.alertId, utc = packet.utc, sender = packet.sender,
                    senderId = packet.crewId, reason = packet.reason, isLocal = false
                };
                if (string.IsNullOrEmpty(incoming.id)) return;
                state.alerts.Add(incoming);
                Storage.Save(state);
                Toast("INCOMING CREW SOS FROM " + packet.sender.ToUpperInvariant() + ". Open Crew Network.");
                RenderPage();
            }
            else if (packet.kind == "ACK")
            {
                foreach (EmergencyEvent evt in state.alerts)
                {
                    if (evt.id == packet.alertId && !evt.acknowledgedBy.Contains(packet.crewId))
                    {
                        evt.acknowledgedBy.Add(packet.crewId);
                        Storage.Save(state);
                        RenderPage();
                    }
                }
            }
        }

        private void Acknowledge(EmergencyEvent evt)
        {
            if (!evt.acknowledgedBy.Contains(state.selectedCrew)) evt.acknowledgedBy.Add(state.selectedCrew);
            Storage.Save(state);
            network.Send("ACK", state.selectedCrew, CurrentCrewName(), "Acknowledged", evt.id);
            Toast("Acknowledgment recorded and broadcast attempted.");
            RenderPage();
        }

        private void RenderAssessments()
        {
            Ui.Label(pageRoot, "MISSION HEALTH ASSESSMENT WORKFLOW", 12, Ui.Cyan, 30, FontStyle.Bold);
            RectTransform row = Ui.Row(pageRoot, 575);
            RectTransform steps = Ui.CardPanel(row, 575);
            Ui.Label(steps, "GUIDED HEALTH CHECK", 17, Ui.Ink, 35, FontStyle.Bold);
            Step(steps, "01  CHECK IMMEDIATE SAFETY", "If there is an urgent threat, notify the crew and follow approved mission emergency procedures.");
            Step(steps, "02  CONFIRM MEASUREMENT QUALITY", "Verify sensor placement, calibration and recent activity before interpreting data.");
            Step(steps, "03  RECORD OBSERVATIONS", "Record measurements, symptoms, perceived stress and other relevant observations.");
            Step(steps, "04  COMPARE PERSONAL TRENDS", "Review changes with uncertainty and potential environmental and workload context.");
            Step(steps, "05  COORDINATE CARE", "Consult the onboard crew medical officer and Earth-based flight surgeons when protocols indicate.");
            Ui.Action(steps, "OPEN HEALTH RECORD", () => Navigate(Page.Health), Ui.Cyan, Ui.Background, 43);
            RectTransform tools = Ui.CardPanel(row, 575);
            Ui.Label(tools, "MISSION SCENARIO SIMULATOR", 17, Ui.Ink, 35, FontStyle.Bold);
            Ui.Label(tools,
                "Inject a hypothetical 5-day sleep/stress drift and watch the personalized baseline interpretation change. The scenario demonstrates logic, not medical prediction performance.",
                14, Ui.Muted, 100);
            Ui.Action(tools, state.scenarioActive ? "SCENARIO ALREADY ACTIVE" : "INJECT SYNTHETIC HEALTH DRIFT", () =>
            {
                if (state.scenarioActive) { Toast("Reset demo data before simulating again."); return; }
                Storage.AddScenario(state);
                Storage.Save(state);
                Toast("Synthetic health drift inserted; review the health monitor.");
                Navigate(Page.Health);
            }, Ui.Surface, Ui.Cyan, 48);
            Ui.Action(tools, "RESET ALL DEMO DATA", () => OpenConfirmReset(), Ui.Surface, Ui.Amber, 45);
            Ui.Space(tools, 11);
            Ui.Label(tools, "MEDICAL HANDOFF / RESEARCH DEMO", 15, Ui.Ink, 31, FontStyle.Bold);
            Ui.Label(tools, "Export locally recorded measurements and event history as JSON. Includes simulated data labels.",
                13, Ui.Muted, 64);
            Ui.Action(tools, "EXPORT HANDOFF JSON", ExportHandoff, Ui.Cyan, Ui.Background, 48);
            Footer(pageRoot);
        }

        private void Step(Transform parent, string heading, string detail)
        {
            Ui.Label(parent, heading, 13, Ui.Cyan, 24, FontStyle.Bold);
            Ui.Label(parent, detail, 13, Ui.Muted, 51);
        }

        private void OpenConfirmReset()
        {
            OpenModal();
            Ui.Label(modalCard, "RESET ALL LOCAL DEMO DATA?", 21, Ui.Amber, 48, FontStyle.Bold);
            Ui.Label(modalCard, "This removes local measurement history and SOS events, then recreates the synthetic reference week.",
                14, Ui.Muted, 95);
            RectTransform actions = Ui.Row(modalCard, 51);
            Ui.Action(actions, "CANCEL", CloseModal, Ui.Surface, Ui.Ink, 50);
            Ui.Action(actions, "RESET DEMO", () =>
            {
                string current = state.selectedCrew;
                state = Storage.Fresh();
                state.selectedCrew = current;
                Storage.Save(state);
                CloseModal();
                Toast("Synthetic demo dataset restored.");
                RenderPage();
            }, Ui.Amber, Ui.Background, 50);
        }

        private void ExportHandoff()
        {
            try
            {
                string filename = "AETHER_Handoff_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json";
                string path = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllText(path, JsonUtility.ToJson(state, true));
                Toast("Handoff exported to: " + path);
                Debug.Log("AETHER handoff: " + path);
            }
            catch (Exception ex) { Toast("Export error: " + ex.Message); }
        }

        private void OpenModal()
        {
            Ui.Clear(modalCard);
            modalOverlay.gameObject.SetActive(true);
            modalOverlay.SetAsLastSibling();
        }

        private void CloseModal()
        {
            modalOverlay.gameObject.SetActive(false);
        }

        private void Toast(string message)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastOverlay.gameObject.SetActive(true);
            toastOverlay.SetAsLastSibling();
            toastExpires = Time.unscaledTime + 8f;
            Debug.Log("AETHER: " + message);
        }

        private void Footer(Transform parent)
        {
            Ui.Label(parent,
                "AETHER / NASA Space Apps concept / Independent research demo; not NASA-endorsed. Medical content is educational only; not for clinical or operational use. Synthetic data clearly labeled. Sources: nasa.gov/hrp/hazards and nasa.gov/hrp/risks.",
                11, Ui.Muted, 83);
        }
    }
}
