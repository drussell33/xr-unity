# VR Financial Data Room — Portfolio Stress Lab

An interview-ready Meta Quest 3 vertical slice built on the project's existing
OpenXR and XR Interaction Toolkit foundation. The experience is deterministic,
offline, illustrative only, and isolated from existing scenes and project-wide
settings.

## Architecture and ownership

- `FinancialProjectionModel` calculates both baseline and market-stress paths.
- `FinancialRoomController` owns current input, guided-flow, completion, stale,
  and state-only reset behavior.
- `FinancialDashboardView` renders KPIs, allocation, scenario context, and both
  forecast lines.
- `FinancialPhysicalControl` and `FinancialActionButton` provide reusable
  ray/direct/poke interaction, visual feedback, and haptics.
- `FinancialGuideController` owns one-time onboarding cues, subtitles, optional
  spatial narration, Replay Guide behavior, and its lightweight speaking pulse.
- `FinancialDataRoomSceneBuilder` is authoritative for all scene hierarchy,
  references, XR setup, layout, generated environment objects, and the guide.

Do not hand-edit the generated scene as the primary implementation. Rebuild it
through the menu command after changing the builder.

## Generate the scene

1. Open the Unity project and wait for compilation.
2. Save any unrelated open scene changes manually.
3. Select **Tools > Financial Data Room > Build Demo Scene**.
4. Unity replaces only:
   `Assets/InterviewDemo/FinancialDataRoom/Scenes/VR_Financial_Data_Room.unity`
5. Confirm the generated scene is open and the Console has no new errors.

The builder does not modify Build Settings, XR settings, packages, source
prefabs, `Basic_Hotel`, or other existing scenes.

## Controls

- **Left thumbstick:** head-relative smooth movement
- **Right thumbstick:** 45-degree snap turn
- **Teleport:** retained from the reused XR Origin
- **Risk knob:** adjusts portfolio risk and allocation
- **Investment Horizon slider:** selects 1–10 years
- **Base / Stress lever:** selects the active scenario
- **Run Analysis:** records the current completed result
- **Replay Guide:** replays the most relevant current Portfolio Guide cue
- **Reset:** restores portfolio state, controls, guidance, and dashboard without
  moving or rotating the XR Origin

Ray, direct, and poke interaction remain enabled. Controls and buttons provide
hover/select tint, press or movement feedback, and brief controller haptics.
The XR Interaction Simulator is tagged `EditorOnly`.

## Portfolio Guide

The scene builder creates a static, gender-neutral human-like guide from eight
Unity primitives at `(2.15, 0, 4.85)`, beside the dashboard and outside the
central walking path. Its subtitle panel faces the normal console standing
position. Guide geometry and subtitles use Ignore Raycast, have no colliders,
cast no shadows, and use no particles, skinned meshes, animation rigs, or
dynamic lights. Only the separate Replay Guide button is interactable.

The project-owned `FinancialGuide_Hologram` material uses URP/Unlit in opaque,
high-opacity mode to avoid transparent sorting artifacts on Quest. The guide
uses at most eight primitive renderers, one shared material, one small
world-space Canvas, one AudioSource, and no idle per-frame update. Only its base
ring pulses while a cue is active.

Automatic cues play once per session:

- **Welcome:** "Welcome to the Portfolio Stress Lab. Move to the console to
  explore how risk, time horizon, and market conditions affect a hypothetical
  portfolio."
- **Risk:** "Risk shifts this illustrative portfolio between equities, bonds,
  and cash. Higher equity exposure raises both expected return and volatility."
- **Horizon:** "Investment horizon controls how much time the portfolio has to
  compound or recover from a simulated market decline."
- **Stress:** "Market Stress adds an early drawdown and partial recovery.
  Compare it with the baseline forecast to understand the downside tradeoff."
- **Analysis Complete:** "Analysis complete. Compare the forecast lines,
  projected value, and drawdown to discuss the tradeoff between growth
  potential and risk. These results are illustrative only."

Risk, Horizon, and Stress cues are emitted only while their XRI controls are
actively selected. Initialization, builder configuration, programmatic
`SetValue(..., false)`, and Reset remain silent. Reset clears cue history and
returns the guide to Welcome without moving the player.

Optional narration files:

- `Audio/PortfolioGuide_Welcome.wav`
- `Audio/PortfolioGuide_Risk.wav`
- `Audio/PortfolioGuide_Horizon.wav`
- `Audio/PortfolioGuide_MarketStress.wav`
- `Audio/PortfolioGuide_AnalysisComplete.wav`

These files are intentionally not included. The builder wires any matching clip
that exists and safely leaves missing slots null. Subtitles and speaking-ring
emphasis continue to work without audio; no placeholder sound is played.

## Deterministic model assumptions

- Starting value: **$1,000,000**
- Risk maps Equity from 25–80%, Cash from 20–5%, and Bonds to the remainder.
- Baseline annual assumptions are 8% Equity, 4% Bonds, and 2% Cash.
- Volatility uses fixed illustrative asset-class assumptions.
- Both baseline and stress forecasts contain exactly 24 points.
- Market Stress applies a risk-scaled early drawdown followed by a deterministic
  partial recovery.
- The active scenario changes the KPI result while both comparison paths remain
  visible.
- Inputs use no randomness, networking, external market data, persistence, or
  analytics.

All displayed financial content is illustrative only and is not financial
advice.

## Generated environment

- Playable floor: 19m × 13m, with a separate invisible collision/teleport floor.
- The Room_Modern prefab is used only as a style/material source and is disabled
  after the builder verifies separate floor, table, dashboard backing, and
  lighting replacements.
- Four generated perimeter walls use the project-owned texture-free
  `FinancialRoom_Wall_Neutral` material.
- Wall shadows are disabled locally to keep the large Quest surfaces stable
  without changing global URP or quality settings.

## 60-second interview walkthrough

1. Move to the console and mention the reused OpenXR/XRI locomotion foundation.
2. Turn Risk and show allocation, return, volatility, drawdown, and both paths
   update deterministically.
3. Change Horizon and explain that it changes compounding, not allocation.
4. Enable Market Stress and point out the early shock and partial recovery while
   the baseline remains visible.
5. Press Run Analysis and review the completed terminal result.
6. Change an input to demonstrate the
   **Inputs changed — run analysis again.** state.
7. Press Reset and show that portfolio state resets without teleporting the
   player.

## Editor validation

1. Clear the Console and rebuild the scene from the menu.
2. Confirm the player starts inside the room facing the dashboard.
3. Confirm the four wall renderers use only `FinancialRoom_Wall_Neutral`.
4. Verify smooth movement, 45-degree snap turn, teleport, rays, direct, and poke.
5. Exercise all three controls plus Run and Reset.
6. Confirm both chart lines share one scale and remain readable.
7. Complete an analysis, change an input, rerun, then reset.
8. Confirm Reset never moves the XR Origin.
9. From the console, confirm the guide and subtitles do not obscure the
   dashboard and Replay Guide is reachable by ray.
10. Trigger each cue once, repeat each interaction to confirm no automatic
    repetition, and verify Replay follows the latest relevant cue.
11. With no narration files present, confirm subtitles and the speaking pulse
    work without Console errors.

## Quest 3 validation

1. Preserve the existing Android/OpenXR, ARM64, and IL2CPP configuration.
2. Add or preserve the scene in Build Settings manually; the builder will not.
3. Build and Run only after reviewing generated files.
4. From approximately 0.5m, 5m, and 10–12m, look and move parallel to each wall.
   Pass only if the surface has no crawling texture, flicker, bands, or corner
   z-fighting.
5. Run the full interaction flow with ray and direct interaction.
6. Confirm dashboard readability, Run, stale-input messaging, and state-only
   Reset.
7. Confirm guide subtitle readability and hologram stability from the console.
8. Test Replay Guide by ray, then approach it for direct and poke interaction.
9. Confirm Reset restores Welcome without changing player position.
10. Complete a stable five-minute headset play test.

## Known limitations

- Values are illustrative and not calibrated to live markets.
- No networking, persistence, analytics, synthesized narration, localization,
  or live data. Prerecorded narration clips are optional and project-local.
- The chart intentionally uses lightweight LineRenderers without interactive
  axes or inspection.
- Build Settings and Android builds remain manual review steps.
