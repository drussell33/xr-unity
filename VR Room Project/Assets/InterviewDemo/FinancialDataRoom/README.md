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
- `FinancialDataRoomSceneBuilder` is authoritative for all scene hierarchy,
  references, XR setup, layout, and generated environment objects.

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
- **Reset:** restores portfolio state, controls, guidance, and dashboard without
  moving or rotating the XR Origin

Ray, direct, and poke interaction remain enabled. Controls and buttons provide
hover/select tint, press or movement feedback, and brief controller haptics.
The XR Interaction Simulator is tagged `EditorOnly`.

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
7. Complete a stable five-minute headset play test.

## Known limitations

- Values are illustrative and not calibrated to live markets.
- No networking, persistence, analytics, audio, localization, or live data.
- The chart intentionally uses lightweight LineRenderers without interactive
  axes or inspection.
- Build Settings and Android builds remain manual review steps.
