using System.Collections;
using TMPro;
using UnityEngine;

namespace InterviewDemo.FinancialDataRoom
{
    /// <summary>
    /// Coordinates the resettable physical controls, deterministic model, dashboard, and guidance.
    /// Resetting the demo never reloads the scene or moves the XR Origin.
    /// </summary>
    public sealed class FinancialRoomController : MonoBehaviour
    {
        [SerializeField] FinancialDashboardView dashboard;
        [SerializeField] TMP_Text instructionText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] FinancialPhysicalControl riskControl;
        [SerializeField] FinancialPhysicalControl horizonControl;
        [SerializeField] FinancialPhysicalControl scenarioControl;
        [SerializeField] FinancialActionButton runButton;
        [SerializeField] FinancialActionButton resetButton;

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)] float defaultRisk = 0.5f;
        [SerializeField, Range(1, 10)] int defaultHorizonYears = 5;

        float risk;
        int horizonYears;
        bool stressScenario;
        bool movementStepComplete;
        bool riskStepComplete;
        bool horizonStepComplete;
        bool stressStepComplete;
        bool analysisComplete;
        bool analysisStale;
        Coroutine temporaryStatusRoutine;

        public void Configure(
            FinancialDashboardView dashboardView,
            TMP_Text persistentInstructions,
            TMP_Text currentStatus)
        {
            dashboard = dashboardView;
            instructionText = persistentInstructions;
            statusText = currentStatus;
        }

        public void ConfigureControls(
            FinancialPhysicalControl risk,
            FinancialPhysicalControl horizon,
            FinancialPhysicalControl scenario,
            FinancialActionButton run,
            FinancialActionButton reset)
        {
            riskControl = risk;
            horizonControl = horizon;
            scenarioControl = scenario;
            runButton = run;
            resetButton = reset;
        }

        void Awake()
        {
            risk = defaultRisk;
            horizonYears = defaultHorizonYears;
            stressScenario = false;
        }

        void Start()
        {
            if (instructionText != null)
            {
                instructionText.text =
                    "1  Move to the console\n" +
                    "2  Adjust Risk\n" +
                    "3  Adjust Horizon\n" +
                    "4  Enable Market Stress\n" +
                    "5  Run Analysis";
            }

            ApplyStateToControls();
            dashboard?.ClearAnalysisComplete();
            Recalculate();
            RefreshStatus();
            StartCoroutine(CompleteMovementHint());
        }

        public void SetRisk(float normalizedRisk)
        {
            risk = Mathf.Clamp01(normalizedRisk);
            riskStepComplete = true;
            InvalidateCompletedAnalysis();
            Recalculate();
            RefreshStatus();
        }

        public void SetHorizon(float normalizedHorizon)
        {
            horizonYears = 1 + Mathf.RoundToInt(Mathf.Clamp01(normalizedHorizon) * 9f);
            horizonStepComplete = true;
            InvalidateCompletedAnalysis();
            Recalculate();
            RefreshStatus();
        }

        public void SetScenario(float normalizedScenario)
        {
            stressScenario = normalizedScenario >= 0.5f;
            if (stressScenario)
                stressStepComplete = true;

            InvalidateCompletedAnalysis();
            Recalculate();
            RefreshStatus();
        }

        public void RunAnalysis()
        {
            analysisComplete = true;
            analysisStale = false;
            var projection = Recalculate();
            dashboard?.ShowAnalysisComplete(projection, risk, horizonYears, stressScenario);
            RefreshStatus();
        }

        public void ResetDemoState()
        {
            risk = defaultRisk;
            horizonYears = defaultHorizonYears;
            stressScenario = false;
            movementStepComplete = true;
            riskStepComplete = false;
            horizonStepComplete = false;
            stressStepComplete = false;
            analysisComplete = false;
            analysisStale = false;

            ApplyStateToControls();
            runButton?.ResetVisual();
            resetButton?.ResetVisual();
            dashboard?.ClearAnalysisComplete();
            Recalculate();
            ShowTemporaryStatus("PORTFOLIO RESET TO DEFAULTS", 1.5f);
        }

        IEnumerator CompleteMovementHint()
        {
            yield return new WaitForSeconds(4f);
            movementStepComplete = true;
            RefreshStatus();
        }

        FinancialProjectionModel.Projection Recalculate()
        {
            var projection = FinancialProjectionModel.Calculate(risk, horizonYears, stressScenario);
            dashboard?.Render(projection, stressScenario, risk, horizonYears);
            return projection;
        }

        void ApplyStateToControls()
        {
            riskControl?.SetValue(risk, false);
            horizonControl?.SetValue((horizonYears - 1) / 9f, false);
            scenarioControl?.SetValue(stressScenario ? 1f : 0f, false);
        }

        void InvalidateCompletedAnalysis()
        {
            if (!analysisComplete)
                return;

            analysisComplete = false;
            analysisStale = true;
            dashboard?.ClearAnalysisComplete();
        }

        void ShowTemporaryStatus(string message, float duration)
        {
            if (temporaryStatusRoutine != null)
                StopCoroutine(temporaryStatusRoutine);

            temporaryStatusRoutine = StartCoroutine(ShowTemporaryStatusRoutine(message, duration));
        }

        IEnumerator ShowTemporaryStatusRoutine(string message, float duration)
        {
            if (statusText != null)
                statusText.text = message;

            yield return new WaitForSeconds(duration);
            temporaryStatusRoutine = null;
            RefreshStatus();
        }

        void RefreshStatus()
        {
            if (statusText == null || temporaryStatusRoutine != null)
                return;

            if (analysisStale)
                statusText.text = "Inputs changed — run analysis again.";
            else if (analysisComplete)
                statusText.text = "ANALYSIS COMPLETE | Review the highlighted result";
            else if (!movementStepComplete)
                statusText.text = "CURRENT: Move with LEFT stick | Snap turn with RIGHT stick";
            else if (!riskStepComplete)
                statusText.text = "CURRENT: Adjust the Risk knob";
            else if (!horizonStepComplete)
                statusText.text = "CURRENT: Adjust the Investment Horizon slider";
            else if (!stressStepComplete)
                statusText.text = "CURRENT: Move the scenario lever to STRESS";
            else
                statusText.text = "CURRENT: Press RUN ANALYSIS";
        }
    }
}
