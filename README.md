# AETHER — Unity astronaut health & crew safety demo

A functional Unity uGUI prototype inspired by the NASA Space Apps 2026 challenge, **Create Health Monitoring Software for Astronauts on Space Missions**.

**Status: source project, not a compiled executable.** The source was assembled without a Unity Editor available in this environment; open it in Unity to compile and play-test. Report any Unity-version-specific compile errors rather than treating the current implementation as flight-ready.

## Start in Unity

1. Install **Unity Hub** and a **Unity 2022.3 LTS** editor (or a newer compatible editor), with **Windows Build Support** if you want Windows executables.
2. Extract `AETHER-Unity.zip`.
3. In Unity Hub, choose **Add project from disk** and select the extracted **AETHER-Unity** folder.
4. Open `Assets/Scenes/AETHER.unity`.
5. Press **Play**. The application creates its Canvas/UI at runtime from `Assets/Scripts/AetherApp.cs`.
6. Target **1600 × 900** in the Game view for the intended desktop layout.

If Unity prompts to upgrade the project version, accept the upgrade. Unity UI (`com.unity.ugui`) is included in `Packages/manifest.json`.

## Implemented features

- Dark mission-control dashboard and mission-context overview.
- Manual heart rate, sleep, body temperature and mood entry, persisted as JSON.
- Individual demo reference-week baseline and interpretable trend flags (not clinical thresholds).
- Interactive Unity-generated heart rate line chart.
- **Risk intelligence:** NASA's five spaceflight hazards, impacts, relevant measurements, countermeasures, recommended escalation, and source links.
- **Upgraded Anatomical Twin:** integrated high-detail, user-provided holographic anatomy scan with five clickable organ-system hotspots, zoom controls, system explorer and monitoring explanations. This is an interactive **2D image-based illustration**, not a 3D anatomical mesh or real scan.
- **Crew emergency network:** local role selection; confirmation before SOS; LAN UDP broadcast SOS and receipt acknowledgments; persistent event log; active alert banners.
- 5-day synthetic sleep/stress scenario simulator.
- Guided self-assessment and JSON handoff export.
- Editor smoke tests: `AETHER > Run Health Logic Smoke Tests`.

## Two central innovations

### 1 — Spaceflight Risk Intelligence

Open **Spaceflight Risks**. Select one of the five hazards. Each includes:

- What could happen
- What can realistically be measured
- Potential preventive countermeasures
- What to do when concerned
- A direct NASA source link

This is **educational decision support, not a diagnostic or treatment protocol**. No medication instructions or medical cutoffs are invented.

### 2 — Crew Emergency Network (LAN demo)

Open **Crew Network**. Each installation chooses the demo astronaut profile representing its operator (Commander / Medical Officer / Engineer). To test:

1. Build a Windows or macOS/Linux desktop player from **File > Build Settings**.
2. Run AETHER on **two or more computers on the same local network**. Configure firewall/network rules to permit UDP broadcasts on port **34862**.
3. On each device, select a **different crew profile** and leave AETHER running.
4. On one device, press **EMERGENCY SOS** and confirm the reason.
5. Other running clients should display a visible active emergency alert. Select **ACKNOWLEDGE** on the receiving client.
6. The original sender should see the acknowledgment count update if UDP reaches them.

The application also operates offline in a **single-device demonstration**, recording its own alerts locally. LAN UDP is **best-effort**, so missed alerts and acknowledgments are possible; the user interface must not be interpreted as proof every crew member received a message. Some Wi-Fi networks block broadcasts, some firewalls require explicit permission, and clients on different networks cannot see each other without additional infrastructure.

**Security and reliability limitations:** There is NO authentication, encryption, guaranteed delivery, medical-grade escalation, or store-and-forward relay. A LAN peer can spoof demo packets. The implementation is NOT suitable for real emergency communication, astronaut safety systems, or medical deployment. Production plans must include authenticated crew identities, encrypted messaging, receipt/retry/timeout logic, redundant transport and tested onboard network integration.

**WebGL:** Browser players do not support the demo UDP transport. Local UI, health tracking and simulated on-device SOS logs remain available.

## Detailed anatomical twin (new)

Open **Health Twin** in the left navigation. The full-body holographic image is embedded at `Assets/Resources/Twin/aether_anatomical_hologram.png` and appears automatically in Unity. It was adapted from the anatomical reference image supplied by the user. Click any glowing `+` marker, or select a system on the right, for the risk/indicator explanation. Zoom + / Zoom − controls scale the image and hotspots together; RESET restores the initial view.

This visual is a **high-detail interactive 2D medical illustration**, *not* an imported 3D FBX/glTF model, not a diagnostic scan, and not physiological simulation. For genuine orbitable 3D medical anatomy in a later release, a suitably licensed and accurate rigged model and its validated system segmentation are needed. The reference art remains independent of the app's numeric monitoring data.

## How the demo assessment works

- The first seven synthetic readings form a labeled *reference week*.
- The latest reading is compared to its mean resting heart rate and sleep hours.
- Demonstration heuristics indicate a notable shift at **+8 bpm**, **−1.5 hours sleep**, and/or a stressed self-report.
- With two or more such observations, the UI suggests review, **not a clinical diagnosis or automatic SOS**.
- No validated space medicine alert limits or probability estimates are claimed.

## Where data is stored

Application data is saved as `aether_state.json` under `Application.persistentDataPath`. The Assessments page can export another JSON handoff. Runtime logs show the exported path. Use **RESET ALL DEMO DATA** to restore the synthetic reference week.

## Key source files

- `Assets/Scripts/AetherApp.cs` — UI, navigation, forms, SOS workflow, guided review.
- `Assets/Scripts/AetherUI.cs` — reusable uGUI components, color system and generated heart rate chart.
- `Assets/Scripts/AetherData.cs` — models, demo baseline logic, NASA hazard reference content, local storage.
- `Assets/Scripts/AetherNetwork.cs` — demonstration LAN UDP broadcast, discovery, receipt acknowledgement.
- `Assets/Editor/AetherSmokeTests.cs` — basic algorithm tests in the Editor menu.

## Research references

- [NASA — Five Hazards of Human Spaceflight](https://www.nasa.gov/hrp/hazards/)
- [NASA — Risks of Human Spaceflight](https://www.nasa.gov/hrp/risks/)
- [NASA — Ongoing Human Research Program research](https://www.nasa.gov/hrp/ongoing-hrp-research/)
- [CSA — Monitoring Radiation for Astronaut Health](https://www.asc-csa.gc.ca/eng/news/articles/2026/2026-02-26-monitoring-radiation-astronaut-health.asp)

This demo is an independent hackathon project, **not affiliated with or endorsed by NASA**.

## Next engineering upgrades

1. Authenticated encrypted crew messaging with explicit delivery and acknowledgment state.
2. Reliable store-and-forward to tolerate outages and cross-network connectivity.
3. Real hardware adapter layer (sensors, dosimetry) with provenance/quality assurance.
4. Human-factors testing of alarms, comprehension, accessibility and stress workflows.
5. Reviewed mission medical protocols and expert validation, rather than speculative treatments.
6. Validated spaceflight datasets and prospective testing before any operational usage.
