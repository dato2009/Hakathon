#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Aether
{
    public static class AetherSmokeTests
    {
        [MenuItem("AETHER/Run Health Logic Smoke Tests")]
        public static void Run()
        {
            AppState stable = Storage.Fresh();
            HealthAssessment baseline = HealthLogic.Assess(stable);
            if (baseline.classification != "NO DEMO FLAG")
                throw new System.Exception("Stable scenario should not produce a demo flag.");
            Storage.AddScenario(stable);
            HealthAssessment drift = HealthLogic.Assess(stable);
            if (drift.classification != "REVIEW SUGGESTED")
                throw new System.Exception("Synthetic drift scenario should request review.");
            HealthSample sample;
            string error;
            if (HealthLogic.TryCreateSample("invalid", "7.4", "36.6", "Good", out sample, out error))
                throw new System.Exception("Invalid heart rate should fail input validation.");
            if (!HealthLogic.TryCreateSample("71", "7.4", "36.6", "Good", out sample, out error))
                throw new System.Exception("Valid sample was rejected: " + error);
            Debug.Log("AETHER smoke tests PASSED: baseline, scenario, invalid input, valid input.");
            EditorUtility.DisplayDialog("AETHER Smoke Tests", "All four health logic checks passed.", "OK");
        }
    }
}
#endif
