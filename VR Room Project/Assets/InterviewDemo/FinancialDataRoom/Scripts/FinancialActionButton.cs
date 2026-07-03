using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace InterviewDemo.FinancialDataRoom
{
    /// <summary>
    /// Selectable action button with one invocation per press cycle.
    /// Ray, direct, and poke interactors all use the same XRI select path.
    /// </summary>
    public sealed class FinancialActionButton : XRBaseInteractable
    {
        [SerializeField] Transform visual;
        [SerializeField] Renderer[] feedbackRenderers;
        [SerializeField] Vector3 localPressAxis = Vector3.down;
        [SerializeField] float pressDistance = 0.018f;
        [SerializeField] Color hoverTint = new Color(0.35f, 0.85f, 1f, 1f);
        [SerializeField] Color selectedTint = new Color(0.20f, 1f, 0.70f, 1f);
        [SerializeField] UnityEvent pressed;

        MaterialPropertyBlock propertyBlock;
        int baseColorProperty;
        int colorProperty;
        bool initialized;
        Vector3 restLocalPosition;
        Color[] baseColors;
        bool pressLatched;

        public UnityEvent Pressed
        {
            get
            {
                EnsureInitialized();
                return pressed;
            }
        }

        public void Configure(
            Transform visualTransform,
            Collider interactionCollider,
            Renderer[] renderers,
            Vector3 pressAxis,
            float travel)
        {
            EnsureInitialized();

            if (visualTransform == null)
                throw ConfigurationError("visualTransform is required.");

            if (interactionCollider == null)
                throw ConfigurationError("interactionCollider is required.");

            if (renderers == null)
                throw ConfigurationError("renderers cannot be null. Pass an empty array when feedback is not needed.");

            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] == null)
                    throw ConfigurationError($"renderers[{index}] is required.");
            }

            if (colliders == null)
                throw ConfigurationError("the XRI collider collection is unavailable.");

            visual = visualTransform;
            feedbackRenderers = renderers;
            localPressAxis = pressAxis.sqrMagnitude > Mathf.Epsilon
                ? pressAxis.normalized
                : Vector3.down;
            pressDistance = Mathf.Max(0.001f, travel);
            colliders.Clear();
            colliders.Add(interactionCollider);
        }

        protected override void Awake()
        {
            EnsureInitialized();
            base.Awake();

            if (visual == null)
                visual = transform;

            restLocalPosition = visual.localPosition;
            CaptureBaseColors();
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (!pressLatched)
            {
                pressLatched = true;
                visual.localPosition = restLocalPosition + (localPressAxis * pressDistance);
                pressed.Invoke();
                SendHaptic(args.interactorObject, 0.25f, 0.04f);
            }

            RefreshFeedback();
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            ResetVisual();
            base.OnSelectExited(args);
            RefreshFeedback();
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            RefreshFeedback();
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            RefreshFeedback();
        }

        protected override void OnDisable()
        {
            ResetVisual();
            base.OnDisable();
        }

        public void ResetVisual()
        {
            pressLatched = false;
            if (visual != null)
                visual.localPosition = restLocalPosition;
        }

        void CaptureBaseColors()
        {
            EnsureInitialized();

            if (feedbackRenderers == null)
            {
                baseColors = Array.Empty<Color>();
                return;
            }

            baseColors = new Color[feedbackRenderers.Length];
            for (var index = 0; index < feedbackRenderers.Length; index++)
            {
                var renderer = feedbackRenderers[index];
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material != null && material.HasProperty(baseColorProperty))
                    baseColors[index] = material.GetColor(baseColorProperty);
                else if (material != null && material.HasProperty(colorProperty))
                    baseColors[index] = material.GetColor(colorProperty);
                else
                    baseColors[index] = Color.white;
            }
        }

        void RefreshFeedback()
        {
            EnsureInitialized();

            if (feedbackRenderers == null || baseColors == null)
                return;

            for (var index = 0; index < feedbackRenderers.Length; index++)
            {
                var renderer = feedbackRenderers[index];
                if (renderer == null)
                    continue;

                var targetColor = baseColors[index];
                if (isSelected)
                    targetColor = Color.Lerp(targetColor, selectedTint, 0.55f);
                else if (isHovered)
                    targetColor = Color.Lerp(targetColor, hoverTint, 0.35f);

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(baseColorProperty, targetColor);
                propertyBlock.SetColor(colorProperty, targetColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        void EnsureInitialized()
        {
            if (initialized)
                return;

            propertyBlock = new MaterialPropertyBlock();
            baseColorProperty = Shader.PropertyToID("_BaseColor");
            colorProperty = Shader.PropertyToID("_Color");
            pressed ??= new UnityEvent();
            initialized = true;
        }

        InvalidOperationException ConfigurationError(string detail)
        {
            return new InvalidOperationException(
                $"{nameof(FinancialActionButton)} configuration failed on " +
                $"'{gameObject.name}': {detail}");
        }

        static void SendHaptic(IXRSelectInteractor interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor inputInteractor)
                inputInteractor.SendHapticImpulse(amplitude, duration);
        }
    }
}
