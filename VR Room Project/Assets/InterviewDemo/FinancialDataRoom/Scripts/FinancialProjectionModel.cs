using System;
using UnityEngine;

namespace InterviewDemo.FinancialDataRoom
{
    /// <summary>
    /// Deterministic illustrative portfolio model for the interview demo.
    /// It deliberately has no network, persistence, market-data, or random-number dependencies.
    /// </summary>
    public static class FinancialProjectionModel
    {
        public const float StartingPortfolioValue = 1_000_000f;
        public const int ForecastPointCount = 24;

        [Serializable]
        public sealed class Projection
        {
            public float portfolioValue;
            public float baselineTerminalValue;
            public float stressTerminalValue;
            public float expectedReturn;
            public float baselineExpectedReturn;
            public float stressExpectedReturn;
            public float volatility;
            public float maximumDrawdown;
            public float baselineMaximumDrawdown;
            public float stressMaximumDrawdown;
            public float equityAllocation;
            public float bondAllocation;
            public float cashAllocation;
            public float[] baselineForecastValues;
            public float[] stressForecastValues;
            public string riskCategory;
            public string scenarioCategory;
            public string scenarioExplanation;
        }

        public static Projection Calculate(float risk, int horizonYears, bool stressScenario)
        {
            risk = Mathf.Clamp01(risk);
            horizonYears = Mathf.Clamp(horizonYears, 1, 10);

            var equity = Mathf.Lerp(0.25f, 0.80f, risk);
            var cash = Mathf.Lerp(0.20f, 0.05f, risk);
            var bonds = 1f - equity - cash;

            var baselineReturn =
                (equity * 0.08f) +
                (bonds * 0.04f) +
                (cash * 0.02f);
            var stressReturn = baselineReturn - Mathf.Lerp(0.015f, 0.045f, risk);

            var volatility = Mathf.Sqrt(
                Mathf.Pow(equity * 0.18f, 2f) +
                Mathf.Pow(bonds * 0.06f, 2f) +
                Mathf.Pow(cash * 0.01f, 2f));

            var baselineDrawdown = Mathf.Lerp(0.04f, 0.18f, risk);
            var stressDrawdown = Mathf.Lerp(0.12f, 0.42f, risk);
            var baselineForecast = BuildBaselineForecast(baselineReturn, horizonYears);
            var stressForecast = BuildStressForecast(
                stressReturn,
                stressDrawdown,
                horizonYears);

            var baselineTerminal = baselineForecast[baselineForecast.Length - 1];
            var stressTerminal = stressForecast[stressForecast.Length - 1];
            var riskCategory = GetRiskCategory(risk);

            return new Projection
            {
                portfolioValue = stressScenario ? stressTerminal : baselineTerminal,
                baselineTerminalValue = baselineTerminal,
                stressTerminalValue = stressTerminal,
                expectedReturn = stressScenario ? stressReturn : baselineReturn,
                baselineExpectedReturn = baselineReturn,
                stressExpectedReturn = stressReturn,
                volatility = volatility,
                maximumDrawdown = stressScenario ? stressDrawdown : baselineDrawdown,
                baselineMaximumDrawdown = baselineDrawdown,
                stressMaximumDrawdown = stressDrawdown,
                equityAllocation = equity,
                bondAllocation = bonds,
                cashAllocation = cash,
                baselineForecastValues = baselineForecast,
                stressForecastValues = stressForecast,
                riskCategory = riskCategory,
                scenarioCategory = stressScenario ? "MARKET STRESS" : "BASE CASE",
                scenarioExplanation = BuildScenarioExplanation(
                    riskCategory,
                    horizonYears,
                    stressScenario,
                    stressDrawdown),
            };
        }

        static float[] BuildBaselineForecast(float annualReturn, int horizonYears)
        {
            var forecast = new float[ForecastPointCount];
            for (var index = 0; index < forecast.Length; index++)
            {
                var progress = index / (float)(forecast.Length - 1);
                var elapsedYears = progress * horizonYears;
                forecast[index] =
                    StartingPortfolioValue * Mathf.Pow(1f + annualReturn, elapsedYears);
            }

            return forecast;
        }

        static float[] BuildStressForecast(
            float annualReturn,
            float maximumDrawdown,
            int horizonYears)
        {
            var forecast = new float[ForecastPointCount];
            const float shockStart = 0.08f;
            const float shockBottom = 0.26f;
            const float residualDrawdownShare = 0.28f;

            for (var index = 0; index < forecast.Length; index++)
            {
                var progress = index / (float)(forecast.Length - 1);
                var elapsedYears = progress * horizonYears;
                var compoundedValue =
                    StartingPortfolioValue * Mathf.Pow(1f + annualReturn, elapsedYears);

                float drawdown;
                if (progress <= shockStart)
                {
                    drawdown = 0f;
                }
                else if (progress <= shockBottom)
                {
                    var shockProgress =
                        (progress - shockStart) / (shockBottom - shockStart);
                    drawdown = Mathf.Lerp(0f, maximumDrawdown, shockProgress);
                }
                else
                {
                    var recoveryProgress =
                        (progress - shockBottom) / (1f - shockBottom);
                    drawdown = Mathf.Lerp(
                        maximumDrawdown,
                        maximumDrawdown * residualDrawdownShare,
                        recoveryProgress);
                }

                forecast[index] = compoundedValue * (1f - drawdown);
            }

            return forecast;
        }

        static string GetRiskCategory(float risk)
        {
            if (risk < 0.34f)
                return "CONSERVATIVE";

            return risk < 0.67f ? "BALANCED" : "GROWTH";
        }

        static string BuildScenarioExplanation(
            string riskCategory,
            int horizonYears,
            bool stressScenario,
            float stressDrawdown)
        {
            if (!stressScenario)
            {
                return $"{riskCategory} allocation compounds steadily across " +
                    $"{horizonYears} year{(horizonYears == 1 ? string.Empty : "s")} " +
                    "using fixed illustrative asset-class assumptions.";
            }

            return $"MARKET STRESS applies an early {stressDrawdown:P0} drawdown to the " +
                $"{riskCategory} allocation, followed by a deterministic partial recovery.";
        }
    }
}
