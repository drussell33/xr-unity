using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InterviewDemo.FinancialDataRoom
{
    /// <summary>
    /// Presentation-only view for the Portfolio Stress Lab dashboard.
    /// </summary>
    public sealed class FinancialDashboardView : MonoBehaviour
    {
        [Header("KPI Values")]
        [SerializeField] TMP_Text portfolioValueText;
        [SerializeField] TMP_Text expectedReturnText;
        [SerializeField] TMP_Text volatilityText;
        [SerializeField] TMP_Text maximumDrawdownText;

        [Header("Allocation")]
        [SerializeField] TMP_Text equityAllocationText;
        [SerializeField] TMP_Text bondAllocationText;
        [SerializeField] TMP_Text cashAllocationText;

        [Header("Scenario")]
        [SerializeField] TMP_Text scenarioText;
        [SerializeField] Image scenarioChipBackground;
        [SerializeField] TMP_Text riskHorizonText;
        [SerializeField] TMP_Text scenarioExplanationText;

        [Header("Forecast")]
        [SerializeField] TMP_Text baselineLegendText;
        [SerializeField] TMP_Text stressLegendText;
        [SerializeField] LineRenderer baselineForecastLine;
        [SerializeField] LineRenderer stressForecastLine;
        [SerializeField] float chartWidth = 1.35f;
        [SerializeField] float chartHeight = 0.32f;
        [SerializeField] Color baseScenarioColor = new Color(0.20f, 0.90f, 0.65f);
        [SerializeField] Color stressScenarioColor = new Color(1f, 0.35f, 0.30f);

        [Header("Analysis Completion")]
        [SerializeField] GameObject completionPanel;
        [SerializeField] TMP_Text completionText;

        static readonly Color BaseChipColor = new Color(0.06f, 0.30f, 0.24f, 0.98f);
        static readonly Color StressChipColor = new Color(0.42f, 0.10f, 0.10f, 0.98f);

        public void Configure(
            TMP_Text portfolioValue,
            TMP_Text expectedReturn,
            TMP_Text volatility,
            TMP_Text maximumDrawdown,
            TMP_Text equityAllocation,
            TMP_Text bondAllocation,
            TMP_Text cashAllocation,
            TMP_Text scenario,
            Image scenarioBackground,
            TMP_Text riskHorizon,
            TMP_Text scenarioExplanation,
            TMP_Text baselineLegend,
            TMP_Text stressLegend,
            LineRenderer baselineLine,
            LineRenderer stressLine,
            GameObject analysisCompletionPanel,
            TMP_Text analysisCompletionText)
        {
            portfolioValueText = portfolioValue;
            expectedReturnText = expectedReturn;
            volatilityText = volatility;
            maximumDrawdownText = maximumDrawdown;
            equityAllocationText = equityAllocation;
            bondAllocationText = bondAllocation;
            cashAllocationText = cashAllocation;
            scenarioText = scenario;
            scenarioChipBackground = scenarioBackground;
            riskHorizonText = riskHorizon;
            scenarioExplanationText = scenarioExplanation;
            baselineLegendText = baselineLegend;
            stressLegendText = stressLegend;
            baselineForecastLine = baselineLine;
            stressForecastLine = stressLine;
            completionPanel = analysisCompletionPanel;
            completionText = analysisCompletionText;
        }

        public void Render(
            FinancialProjectionModel.Projection projection,
            bool stressScenario,
            float risk,
            int horizonYears)
        {
            if (projection == null)
                return;

            SetText(portfolioValueText, projection.portfolioValue.ToString("$#,##0"));
            SetText(expectedReturnText, projection.expectedReturn.ToString("P1"));
            SetText(volatilityText, projection.volatility.ToString("P1"));
            SetText(maximumDrawdownText, projection.maximumDrawdown.ToString("P1"));
            SetText(equityAllocationText, $"Equity  {projection.equityAllocation:P0}");
            SetText(bondAllocationText, $"Bonds  {projection.bondAllocation:P0}");
            SetText(cashAllocationText, $"Cash  {projection.cashAllocation:P0}");
            SetText(scenarioText, projection.scenarioCategory);
            SetText(
                riskHorizonText,
                $"{projection.riskCategory}  |  Risk {risk:P0}  |  Horizon {horizonYears}Y");
            SetText(scenarioExplanationText, projection.scenarioExplanation);
            SetText(
                baselineLegendText,
                $"BASE  {projection.baselineTerminalValue:$#,##0}");
            SetText(
                stressLegendText,
                $"STRESS  {projection.stressTerminalValue:$#,##0}");

            if (scenarioChipBackground != null)
                scenarioChipBackground.color = stressScenario ? StressChipColor : BaseChipColor;

            RenderForecastComparison(
                projection.baselineForecastValues,
                projection.stressForecastValues,
                stressScenario);
        }

        public void ShowAnalysisComplete(
            FinancialProjectionModel.Projection projection,
            float risk,
            int horizonYears,
            bool stressScenario)
        {
            if (completionPanel != null)
                completionPanel.SetActive(true);

            if (completionText == null || projection == null)
                return;

            var difference =
                projection.stressTerminalValue - projection.baselineTerminalValue;
            var differenceText =
                (difference >= 0f ? "+" : "-") + Mathf.Abs(difference).ToString("$#,##0");
            completionText.text =
                $"ANALYSIS COMPLETE — {projection.scenarioCategory}\n" +
                $"{projection.riskCategory} | Risk {risk:P0} | {horizonYears}Y | " +
                $"Terminal {projection.portfolioValue:$#,##0}\n" +
                $"Stress vs Base  {differenceText}";
        }

        public void ClearAnalysisComplete()
        {
            if (completionPanel != null)
                completionPanel.SetActive(false);

            if (completionText != null)
                completionText.text = string.Empty;
        }

        void RenderForecastComparison(
            float[] baselineValues,
            float[] stressValues,
            bool stressScenario)
        {
            if (baselineForecastLine == null ||
                stressForecastLine == null ||
                baselineValues == null ||
                stressValues == null ||
                baselineValues.Length < 2 ||
                stressValues.Length < 2)
            {
                return;
            }

            var minimum = Mathf.Min(baselineValues[0], stressValues[0]);
            var maximum = Mathf.Max(baselineValues[0], stressValues[0]);
            AccumulateRange(baselineValues, ref minimum, ref maximum);
            AccumulateRange(stressValues, ref minimum, ref maximum);
            var range = Mathf.Max(maximum - minimum, 1f);

            SetForecastLine(
                baselineForecastLine,
                baselineValues,
                minimum,
                range,
                baseScenarioColor,
                !stressScenario);
            SetForecastLine(
                stressForecastLine,
                stressValues,
                minimum,
                range,
                stressScenarioColor,
                stressScenario);
        }

        void SetForecastLine(
            LineRenderer line,
            float[] values,
            float minimum,
            float range,
            Color color,
            bool active)
        {
            var positions = new Vector3[values.Length];
            for (var index = 0; index < values.Length; index++)
            {
                var horizontal = index / (float)(values.Length - 1);
                var vertical = (values[index] - minimum) / range;
                positions[index] = new Vector3(
                    horizontal * chartWidth,
                    vertical * chartHeight,
                    0f);
            }

            color.a = active ? 1f : 0.58f;
            line.widthMultiplier = active ? 0.022f : 0.012f;
            line.positionCount = positions.Length;
            line.startColor = color;
            line.endColor = color;
            line.SetPositions(positions);
        }

        static void AccumulateRange(float[] values, ref float minimum, ref float maximum)
        {
            for (var index = 1; index < values.Length; index++)
            {
                minimum = Mathf.Min(minimum, values[index]);
                maximum = Mathf.Max(maximum, values[index]);
            }
        }

        static void SetText(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value;
        }
    }
}
