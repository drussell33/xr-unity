using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace InterviewDemo.FinancialDataRoom
{
    /// <summary>
    /// Resettable, project-owned interaction behavior for the demo's knob, slider, and lever visuals.
    /// XRI selection makes the same control usable by ray, direct, and poke interactors.
    /// </summary>
    public sealed class FinancialPhysicalControl : XRBaseInteractable
    {
        public enum ControlMode
        {
            Knob,
            Slider,
            Lever,
        }

        [Serializable]
        public sealed class FloatEvent : UnityEvent<float>
        {
        }

        [SerializeField] ControlMode mode;
        [SerializeField] Transform visual;
        [SerializeField] Transform sliderStart;
        [SerializeField] Transform sliderEnd;
        [SerializeField] Renderer[] feedbackRenderers;
        [SerializeField, Range(0f, 1f)] float defaultValue = 0.5f;
        [SerializeField] float minimumKnobAngle = -90f;
        [SerializeField] float maximumKnobAngle = 90f;
        [SerializeField, Min(0.1f)] float knobTurnSensitivity = 1.35f;
        [SerializeField] Color hoverTint = new Color(0.35f, 0.85f, 1f, 1f);
        [SerializeField] Color selectedTint = new Color(0.20f, 1f, 0.70f, 1f);
        [SerializeField] FloatEvent valueChanged;

        MaterialPropertyBlock propertyBlock;
        int baseColorProperty;
        int colorProperty;
        bool initialized;
        Color[] baseColors;
        Transform activeAttach;
        Quaternion baseVisualRotation;
        Quaternion selectionStartRotation;
        Vector3 selectionStartPosition;
        Vector3 selectionAxis;
        Vector3 selectionReferenceDirection;
        Vector3 selectionStartKnobDirection;
        bool hasSelectionStartKnobDirection;
        float selectionStartValue;
        float value;

        public float Value => value;
        public FloatEvent ValueChanged
        {
            get
            {
                EnsureInitialized();
                return valueChanged;
            }
        }

        public void Configure(
            ControlMode controlMode,
            Transform visualTransform,
            Collider interactionCollider,
            Renderer[] renderers,
            float initialValue,
            Transform start = null,
            Transform end = null,
            float knobSensitivity = 1.35f)
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

            if (controlMode == ControlMode.Slider && start == null)
                throw ConfigurationError("sliderStart is required for Slider mode.");

            if (controlMode == ControlMode.Slider && end == null)
                throw ConfigurationError("sliderEnd is required for Slider mode.");

            if (colliders == null)
                throw ConfigurationError("the XRI collider collection is unavailable.");

            mode = controlMode;
            visual = visualTransform;
            sliderStart = start;
            sliderEnd = end;
            feedbackRenderers = renderers;
            defaultValue = Mathf.Clamp01(initialValue);
            knobTurnSensitivity = Mathf.Max(0.1f, knobSensitivity);
            value = defaultValue;
            colliders.Clear();
            colliders.Add(interactionCollider);
        }

        protected override void Awake()
        {
            EnsureInitialized();
            base.Awake();

            if (visual == null)
                visual = transform;

            baseVisualRotation = visual.localRotation;
            CaptureBaseColors();
            SetValue(defaultValue, false);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            activeAttach = args.interactorObject.GetAttachTransform(this);
            selectionStartValue = value;
            selectionStartPosition = activeAttach.position;
            selectionStartRotation = activeAttach.rotation;
            selectionAxis = transform.TransformDirection(Vector3.up);
            selectionReferenceDirection = visual.TransformDirection(Vector3.forward);
            var knobDirection = Vector3.ProjectOnPlane(
                activeAttach.position - visual.position,
                selectionAxis);
            hasSelectionStartKnobDirection = knobDirection.sqrMagnitude > 0.0004f;
            selectionStartKnobDirection = hasSelectionStartKnobDirection
                ? knobDirection.normalized
                : Vector3.zero;

            if (mode == ControlMode.Lever)
                SetValue(value >= 0.5f ? 0f : 1f, true);

            SendHaptic(args.interactorObject, 0.15f, 0.025f);
            RefreshFeedback();
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            activeAttach = null;
            hasSelectionStartKnobDirection = false;
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

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic ||
                activeAttach == null ||
                mode == ControlMode.Lever)
            {
                return;
            }

            if (mode == ControlMode.Slider)
            {
                UpdateSliderFromInteractor();
                return;
            }

            UpdateKnobFromInteractor();
        }

        public void ResetToDefault(bool notify = false)
        {
            SetValue(defaultValue, notify);
        }

        public void SetValue(float normalizedValue, bool notify)
        {
            var nextValue = Mathf.Clamp01(normalizedValue);
            if (mode == ControlMode.Lever)
                nextValue = nextValue >= 0.5f ? 1f : 0f;

            var changed = !Mathf.Approximately(value, nextValue);
            value = nextValue;
            ApplyVisual();

            if (notify && changed)
                valueChanged.Invoke(value);
        }

        void UpdateSliderFromInteractor()
        {
            if (sliderStart == null || sliderEnd == null)
                return;

            var travel = sliderEnd.position - sliderStart.position;
            var length = travel.magnitude;
            if (length <= Mathf.Epsilon)
                return;

            var interactorDelta = activeAttach.position - selectionStartPosition;
            var normalizedDelta = Vector3.Dot(interactorDelta, travel / length) / length;
            SetValue(selectionStartValue + normalizedDelta, true);
        }

        void UpdateKnobFromInteractor()
        {
            var rotationDelta = activeAttach.rotation * Quaternion.Inverse(selectionStartRotation);
            var rotatedReference = rotationDelta * selectionReferenceDirection;
            var rotationAngle = Vector3.SignedAngle(
                selectionReferenceDirection,
                rotatedReference,
                selectionAxis);

            var angleDelta = rotationAngle;
            if (hasSelectionStartKnobDirection)
            {
                var currentDirection = Vector3.ProjectOnPlane(
                    activeAttach.position - visual.position,
                    selectionAxis);
                if (currentDirection.sqrMagnitude > 0.0004f)
                {
                    var positionAngle = Vector3.SignedAngle(
                        selectionStartKnobDirection,
                        currentDirection.normalized,
                        selectionAxis);
                    if (Mathf.Abs(positionAngle) > Mathf.Abs(rotationAngle))
                        angleDelta = positionAngle;
                }
            }

            var angleRange = Mathf.Max(maximumKnobAngle - minimumKnobAngle, 1f);
            SetValue(
                selectionStartValue + ((angleDelta * knobTurnSensitivity) / angleRange),
                true);
        }

        void ApplyVisual()
        {
            if (visual == null)
                return;

            switch (mode)
            {
                case ControlMode.Knob:
                    var angle = Mathf.Lerp(minimumKnobAngle, maximumKnobAngle, value);
                    visual.localRotation =
                        baseVisualRotation * Quaternion.AngleAxis(angle, Vector3.up);
                    break;

                case ControlMode.Slider:
                    if (sliderStart != null && sliderEnd != null)
                        visual.position = Vector3.Lerp(sliderStart.position, sliderEnd.position, value);
                    break;

                case ControlMode.Lever:
                    var leverAngle = value >= 0.5f ? 180f : 0f;
                    visual.localRotation =
                        baseVisualRotation * Quaternion.AngleAxis(leverAngle, Vector3.forward);
                    break;
            }
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
            valueChanged ??= new FloatEvent();
            initialized = true;
        }

        InvalidOperationException ConfigurationError(string detail)
        {
            return new InvalidOperationException(
                $"{nameof(FinancialPhysicalControl)} configuration failed on " +
                $"'{gameObject.name}': {detail}");
        }

        static void SendHaptic(IXRSelectInteractor interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor inputInteractor)
                inputInteractor.SendHapticImpulse(amplitude, duration);
        }
    }
}
