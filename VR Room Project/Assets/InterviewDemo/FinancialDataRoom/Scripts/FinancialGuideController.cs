using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace InterviewDemo.FinancialDataRoom
{
    /// <summary>
    /// Runs the non-blocking Portfolio Guide subtitles, optional narration, and speaking pulse.
    /// </summary>
    public sealed class FinancialGuideController : MonoBehaviour
    {
        public enum GuideCue
        {
            Welcome,
            Risk,
            Horizon,
            Stress,
            AnalysisComplete,
        }

        public const string WelcomeCopy =
            "Welcome to the Portfolio Stress Lab. Move to the console to explore how risk, " +
            "time horizon, and market conditions affect a hypothetical portfolio.";
        public const string RiskCopy =
            "Risk shifts this illustrative portfolio between equities, bonds, and cash. " +
            "Higher equity exposure raises both expected return and volatility.";
        public const string HorizonCopy =
            "Investment horizon controls how much time the portfolio has to compound or " +
            "recover from a simulated market decline.";
        public const string StressCopy =
            "Market Stress adds an early drawdown and partial recovery. Compare it with the " +
            "baseline forecast to understand the downside tradeoff.";
        public const string AnalysisCompleteCopy =
            "Analysis complete. Compare the forecast lines, projected value, and drawdown to " +
            "discuss the tradeoff between growth potential and risk. These results are " +
            "illustrative only.";

        [Header("Presentation")]
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] Transform speakingRing;
        [SerializeField, Range(0.01f, 0.12f)] float pulseAmount = 0.065f;
        [SerializeField, Min(0.5f)] float pulseSpeed = 3.5f;

        [Header("Optional Narration")]
        [SerializeField] AudioSource narrationSource;
        [SerializeField] AudioClip welcomeClip;
        [SerializeField] AudioClip riskClip;
        [SerializeField] AudioClip horizonClip;
        [SerializeField] AudioClip stressClip;
        [SerializeField] AudioClip analysisCompleteClip;

        bool[] autoPlayed;
        GuideCue currentCue;
        Coroutine speakingRoutine;
        Vector3 speakingRingBaseScale;
        bool initialized;

        public GuideCue CurrentCue => currentCue;

        public void Configure(
            TMP_Text subtitle,
            Transform pulseRing,
            AudioSource audioSource,
            AudioClip welcome,
            AudioClip risk,
            AudioClip horizon,
            AudioClip stress,
            AudioClip analysisComplete)
        {
            EnsureInitialized();

            if (subtitle == null)
                throw ConfigurationError("subtitle is required.");
            if (pulseRing == null)
                throw ConfigurationError("speaking ring is required.");
            if (audioSource == null)
                throw ConfigurationError("narration AudioSource is required.");

            subtitleText = subtitle;
            speakingRing = pulseRing;
            narrationSource = audioSource;
            welcomeClip = welcome;
            riskClip = risk;
            horizonClip = horizon;
            stressClip = stress;
            analysisCompleteClip = analysisComplete;
            speakingRingBaseScale = speakingRing.localScale;
        }

        void Awake()
        {
            EnsureInitialized();
            if (speakingRing != null)
                speakingRingBaseScale = speakingRing.localScale;
        }

        public void ResetGuide()
        {
            EnsureInitialized();
            StopActiveCue();
            Array.Clear(autoPlayed, 0, autoPlayed.Length);
            currentCue = GuideCue.Welcome;
            PresentCurrentCueOnce();
        }

        public void NotifyRiskInteraction()
        {
            SetRelevantCue(GuideCue.Risk);
        }

        public void NotifyHorizonInteraction()
        {
            SetRelevantCue(GuideCue.Horizon);
        }

        public void NotifyStressInteraction()
        {
            SetRelevantCue(GuideCue.Stress);
        }

        public void NotifyAnalysisComplete()
        {
            SetRelevantCue(GuideCue.AnalysisComplete);
        }

        public void ReplayCurrentCue()
        {
            EnsureInitialized();
            PlayCue(currentCue);
        }

        void SetRelevantCue(GuideCue cue)
        {
            EnsureInitialized();
            currentCue = cue;
            PresentCurrentCueOnce();
        }

        void PresentCurrentCueOnce()
        {
            var index = (int)currentCue;
            if (autoPlayed[index])
                return;

            autoPlayed[index] = true;
            PlayCue(currentCue);
        }

        void PlayCue(GuideCue cue)
        {
            StopActiveCue();

            var copy = GetCopy(cue);
            if (subtitleText != null)
                subtitleText.text = copy;

            var clip = GetClip(cue);
            if (narrationSource != null)
            {
                narrationSource.clip = clip;
                if (clip != null)
                    narrationSource.Play();
            }

            var duration = clip != null ? clip.length : EstimateReadingDuration(copy);
            speakingRoutine = StartCoroutine(PulseSpeakingRing(duration));
        }

        IEnumerator PulseSpeakingRing(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (speakingRing != null)
                {
                    var pulse = 1f +
                        (pulseAmount * (0.5f + (0.5f * Mathf.Sin(elapsed * pulseSpeed))));
                    speakingRing.localScale = speakingRingBaseScale * pulse;
                }

                yield return null;
            }

            if (speakingRing != null)
                speakingRing.localScale = speakingRingBaseScale;
            speakingRoutine = null;
        }

        void StopActiveCue()
        {
            if (speakingRoutine != null)
            {
                StopCoroutine(speakingRoutine);
                speakingRoutine = null;
            }

            if (narrationSource != null)
                narrationSource.Stop();
            if (speakingRing != null)
                speakingRing.localScale = speakingRingBaseScale;
        }

        void OnDisable()
        {
            StopActiveCue();
        }

        void EnsureInitialized()
        {
            if (initialized)
                return;

            autoPlayed = new bool[Enum.GetValues(typeof(GuideCue)).Length];
            initialized = true;
        }

        InvalidOperationException ConfigurationError(string detail)
        {
            return new InvalidOperationException(
                $"{nameof(FinancialGuideController)} configuration failed on " +
                $"'{gameObject.name}': {detail}");
        }

        AudioClip GetClip(GuideCue cue)
        {
            switch (cue)
            {
                case GuideCue.Welcome:
                    return welcomeClip;
                case GuideCue.Risk:
                    return riskClip;
                case GuideCue.Horizon:
                    return horizonClip;
                case GuideCue.Stress:
                    return stressClip;
                case GuideCue.AnalysisComplete:
                    return analysisCompleteClip;
                default:
                    return null;
            }
        }

        static string GetCopy(GuideCue cue)
        {
            switch (cue)
            {
                case GuideCue.Welcome:
                    return WelcomeCopy;
                case GuideCue.Risk:
                    return RiskCopy;
                case GuideCue.Horizon:
                    return HorizonCopy;
                case GuideCue.Stress:
                    return StressCopy;
                case GuideCue.AnalysisComplete:
                    return AnalysisCompleteCopy;
                default:
                    return string.Empty;
            }
        }

        static float EstimateReadingDuration(string copy)
        {
            if (string.IsNullOrWhiteSpace(copy))
                return 4f;

            var wordCount = copy.Split(
                new[] { ' ', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            return Mathf.Clamp(1f + (wordCount / 2.8f), 4f, 12f);
        }
    }
}
