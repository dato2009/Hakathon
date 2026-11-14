using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Aether
{
    [Serializable]
    public class HealthSample
    {
        public string utc;
        public float heartRate;
        public float sleepHours;
        public float temperature;
        public string mood;
        public bool synthetic;
    }

    [Serializable]
    public class EmergencyEvent
    {
        public string id;
        public string utc;
        public string sender;
        public string senderId;
        public string reason;
        public bool isLocal;
        public List<string> acknowledgedBy = new List<string>();
    }

    [Serializable]
    public class AppState
    {
        public int missionDay = 142;
        public string selectedCrew = "CMDR-01";
        public bool scenarioActive;
        public List<HealthSample> samples = new List<HealthSample>();
        public List<EmergencyEvent> alerts = new List<EmergencyEvent>();
    }

    public class HealthAssessment
    {
        public float heartRateBaseline;
        public float sleepBaseline;
        public float latestHeartRate;
        public float latestSleep;
        public float heartRateDrift;
        public float sleepDrift;
        public bool increasedHeartRate;
        public bool reducedSleep;
        public bool selfReportedStress;
        public string classification;
        public string explanation;
        public string recommendation;
    }

    public static class HealthLogic
    {
        // These are DEMO heuristics for illustrating change detection, not clinical cutoffs.
        public static HealthAssessment Assess(AppState state)
        {
            HealthAssessment result = new HealthAssessment();
            if (state.samples == null || state.samples.Count == 0)
            {
                result.classification = "NO DATA";
                result.explanation = "No measurements have been recorded.";
                result.recommendation = "Record measurements to begin a guided review.";
                return result;
            }
            // The first 7 records are the labeled synthetic reference week.
            int baseCount = Mathf.Min(7, state.samples.Count);
            float totalHR = 0f, totalSleep = 0f;
            for (int i = 0; i < baseCount; i++)
            {
                totalHR += state.samples[i].heartRate;
                totalSleep += state.samples[i].sleepHours;
            }
            result.heartRateBaseline = totalHR / baseCount;
            result.sleepBaseline = totalSleep / baseCount;
            HealthSample latest = state.samples[state.samples.Count - 1];
            result.latestHeartRate = latest.heartRate;
            result.latestSleep = latest.sleepHours;
            result.heartRateDrift = latest.heartRate - result.heartRateBaseline;
            result.sleepDrift = latest.sleepHours - result.sleepBaseline;
            result.increasedHeartRate = result.heartRateDrift >= 8f;
            result.reducedSleep = result.sleepDrift <= -1.5f;
            result.selfReportedStress = latest.mood == "Stressed" || latest.mood == "Very stressed";
            int changes = (result.increasedHeartRate ? 1 : 0) +
                          (result.reducedSleep ? 1 : 0) +
                          (result.selfReportedStress ? 1 : 0);
            result.classification = changes >= 2 ? "REVIEW SUGGESTED" : changes == 1 ? "WATCH TREND" : "NO DEMO FLAG";
            result.explanation = string.Format(CultureInfo.InvariantCulture,
                "Resting HR {0:0} bpm ({1:+0;-0;0} vs personal demo baseline); sleep {2:0.0} h ({3:+0.0;-0.0;0.0} h vs baseline). Self-reported mood: {4}. This is a trend observation, not a diagnosis.",
                latest.heartRate, result.heartRateDrift, latest.sleepHours, result.sleepDrift, latest.mood);
            result.recommendation = changes >= 2
                ? "Verify measurement quality, complete the guided check and consult your crew medical officer under approved mission procedures."
                : changes == 1
                    ? "Consider recent workload, sleep and measurement conditions. Repeat relevant checks under mission protocols."
                    : "Continue routine monitoring. One normal-looking reading does not rule out illness.";
            return result;
        }

        public static bool TryCreateSample(string heartRateText, string sleepText, string temperatureText,
            string mood, out HealthSample sample, out string error)
        {
            sample = null;
            error = "";
            float heartRate, sleep, temp;
            if (!TryNumber(heartRateText, out heartRate) || heartRate < 30 || heartRate > 220)
            {
                error = "Enter a heart rate between 30 and 220 bpm (input validation only).";
                return false;
            }
            if (!TryNumber(sleepText, out sleep) || sleep < 0 || sleep > 24)
            {
                error = "Enter sleep hours between 0 and 24.";
                return false;
            }
            if (!TryNumber(temperatureText, out temp) || temp < 30 || temp > 44)
            {
                error = "Enter a temperature between 30 and 44 °C (input validation only).";
                return false;
            }
            sample = new HealthSample
            {
                utc = DateTime.UtcNow.ToString("o"), heartRate = heartRate,
                sleepHours = sleep, temperature = temp,
                mood = mood, synthetic = false
            };
            return true;
        }

        private static bool TryNumber(string text, out float value)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }

    public static class Storage
    {
        public static string StatePath { get { return Path.Combine(Application.persistentDataPath, "aether_state.json"); } }

        public static AppState Load()
        {
            try
            {
                if (File.Exists(StatePath))
                {
                    AppState saved = JsonUtility.FromJson<AppState>(File.ReadAllText(StatePath));
                    if (saved != null && saved.samples != null && saved.samples.Count > 0)
                    {
                        if (saved.alerts == null) saved.alerts = new List<EmergencyEvent>();
                        return saved;
                    }
                }
            }
            catch (Exception ex) { Debug.LogWarning("AETHER storage load: " + ex.Message); }
            return Fresh();
        }

        public static void Save(AppState state)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(StatePath, JsonUtility.ToJson(state, true));
            }
            catch (Exception ex) { Debug.LogWarning("AETHER storage save: " + ex.Message); }
        }

        public static AppState Fresh()
        {
            AppState state = new AppState();
            float[] heartRates = { 65, 66, 65, 67, 66, 67, 68 };
            float[] sleeps = { 7.5f, 7.4f, 7.6f, 7.3f, 7.4f, 7.2f, 7.4f };
            for (int i = 0; i < heartRates.Length; i++)
            {
                state.samples.Add(new HealthSample
                {
                    utc = DateTime.UtcNow.Date.AddDays(i - 7).ToString("o"),
                    heartRate = heartRates[i], sleepHours = sleeps[i],
                    temperature = 36.6f, mood = "Good", synthetic = true
                });
            }
            return state;
        }

        public static void AddScenario(AppState state)
        {
            if (state.scenarioActive) return;
            float[] hr = { 70, 72, 75, 78, 81 };
            float[] sleep = { 7.1f, 6.5f, 6.0f, 5.5f, 5.0f };
            for (int i = 0; i < hr.Length; i++)
            {
                state.samples.Add(new HealthSample
                {
                    utc = DateTime.UtcNow.Date.AddDays(i + 1).ToString("o"),
                    heartRate = hr[i], sleepHours = sleep[i], temperature = 36.7f,
                    mood = i > 2 ? "Stressed" : "Neutral", synthetic = true
                });
            }
            state.missionDay += 5;
            state.scenarioActive = true;
        }
    }

    public class SpaceflightRisk
    {
        public string name, hazard, effect, indicators, prevention, response, source;
        public SpaceflightRisk(string name, string hazard, string effect, string indicators,
            string prevention, string response, string source)
        {
            this.name = name; this.hazard = hazard; this.effect = effect;
            this.indicators = indicators; this.prevention = prevention;
            this.response = response; this.source = source;
        }
    }

    public static class SpaceflightRisks
    {
        public const string NasaHazards = "https://www.nasa.gov/hrp/hazards/";
        public const string NasaRisks = "https://www.nasa.gov/hrp/risks/";
        public static readonly SpaceflightRisk[] All =
        {
            new SpaceflightRisk("Space radiation", "RADIATION",
                "Ionizing radiation exposure can increase long-term health risk; certain exposure events require special protective procedures.",
                "Actual dosimeter readings, cumulative exposure history, spacecraft radiation alarms, symptoms recorded under approved procedures.",
                "Follow verified spacecraft shielding, dosimetry, exposure minimization and radiation-event shelter procedures.",
                "For an exposure event, follow mission radiation procedures, seek the assigned shielded location, and contact the crew medical officer. Symptoms alone cannot establish radiation dose.", NasaHazards),
            new SpaceflightRisk("Isolation & confinement", "BEHAVIORAL HEALTH",
                "Confinement and separation can affect sleep, stress, fatigue, team dynamics and cognitive performance.",
                "Voluntary mood check-ins, sleep logs, fatigue, cognitive tasks, and confidential wellbeing assessments.",
                "Protect rest and routine, maintain supportive crew communication, and use mission-approved behavioral health resources.",
                "Offer a supportive private check-in. For persistent or escalating concerns, involve trained crew medical/behavioral health support according to procedures.", NasaHazards),
            new SpaceflightRisk("Distance from Earth", "MEDICAL AUTONOMY",
                "Communication delay, limited supplies and the absence of rapid evacuation increase the need for onboard assessment.",
                "Communication state, assessment completion, onboard equipment readiness, symptom reporting and medical handoff status.",
                "Maintain offline medical references, train the crew in approved procedures, and keep local equipment and communications plans ready.",
                "Follow onboard emergency protocols, coordinate with the crew medical officer, and queue a structured report for ground review when communications permit.", NasaHazards),
            new SpaceflightRisk("Altered gravity", "MUSCULOSKELETAL / CARDIOVASCULAR",
                "Microgravity and partial gravity change muscle, bone, cardiovascular adaptation and fluid distribution.",
                "Exercise adherence and performance, functional symptoms, heart rate trends, and validated mission-specific assessments.",
                "Follow prescribed resistance/aerobic exercise, nutrition and monitoring countermeasures.",
                "Report injury, persistent loss of function or concerning symptoms to the crew medical officer and use approved assessments. Heart rate alone cannot measure bone loss.", NasaHazards),
            new SpaceflightRisk("Hostile closed environment", "HABITAT SAFETY",
                "A closed habitat creates atmospheric, microbial, temperature and environmental safety concerns.",
                "Validated habitat atmosphere/air-quality alarms, temperature and environmental sensors, and crew symptom reports.",
                "Maintain life-support monitoring, habitat hygiene, inspection schedules and environmental checklists.",
                "Treat habitat alarms as operational hazards, follow approved environmental and emergency procedures, and notify affected crew and mission leadership.", NasaHazards)
        };
    }
}
