# xr-unity

![Repo Size](https://img.shields.io/github/repo-size/drussell33/xr-unity)
![Last Commit](https://img.shields.io/github/last-commit/drussell33/xr-unity)
![Top Language](https://img.shields.io/github/languages/top/drussell33/xr-unity)

## Overview

`xr-unity` is a Unity XR/VR project organized around a `VR Room Project` workspace. The repository contains a Unity 6 project configured with the Universal Render Pipeline (URP), XR Interaction Toolkit, XR Management, Oculus, and OpenXR packages, along with a primary hotel-room scene and several challenge/sample areas used for XR prototyping and learning.

The project is structured as a self-contained Unity application rather than a multi-tier web system. Its functionality is driven by scenes, XR settings, presets, package dependencies, and editor configuration stored directly in the repository.

## Key Features

- Unity 6 project setup with a dedicated `VR Room Project` workspace
- Primary playable scene: `Assets/Scenes/Basic_Hotel.unity`
- XR-ready configuration using:
  - XR Interaction Toolkit
  - XR Management
  - Oculus XR Plugin
  - OpenXR Plugin
- Universal Render Pipeline configuration for rendering
- Included challenge modules for:
  - Architecture
  - 3D Painting
  - Training
- Included XR Interaction Toolkit sample content
- Project presets for XR interactors and lighting settings
- Unity project settings checked into source control for reproducible editor/project setup
- Visual Studio configuration for managed game development workflow

## Tech Stack

### Backend

- None present in this repository

### Frontend

- Unity 6
- C#
- ShaderLab
- HLSL
- Unity UI (`com.unity.ugui`)

### Database

- None present in this repository

### Tools / Services

- Universal Render Pipeline (`com.unity.render-pipelines.universal`)
- XR Interaction Toolkit (`com.unity.xr.interaction.toolkit`)
- XR Management (`com.unity.xr.management`)
- Oculus XR Plugin (`com.unity.xr.oculus`)
- OpenXR Plugin (`com.unity.xr.openxr`)
- Unity AI Navigation (`com.unity.ai.navigation`)
- ProBuilder (`com.unity.probuilder`)
- Unity Timeline (`com.unity.timeline`)
- Unity Test Framework (`com.unity.test-framework`)
- Visual Studio Managed Game workload

## Architecture Overview

This repository contains a single Unity client application. There is no separate backend service, API layer, or database in the codebase.

At a high level, the project is organized like this:

- **Scenes** define the playable XR environments and prototypes
- **Assets** contain project content, samples, challenge materials, presets, XR configuration assets, and course/library resources
- **Packages** define the Unity package dependencies that provide rendering, XR support, UI, navigation, and editor integrations
- **ProjectSettings** store editor, build, quality, graphics, XR, and package configuration for the Unity project

### Runtime Flow

1. The Unity Editor opens the `VR Room Project`.
2. Unity resolves dependencies from `Packages/manifest.json`.
3. XR configuration is loaded from the project's XR settings assets.
4. The active scene (most notably `Assets/Scenes/Basic_Hotel.unity`) provides the environment and interactive content.
5. XR plugins and interaction presets govern headset/controller support and user interaction behavior.

### Architectural Notes

- This is a **scene-driven Unity application**
- Configuration is primarily **asset-based** and **package-based**
- XR support is managed through Unity's **XR Management** pipeline
- No repository evidence of:
  - DTO layers
  - service layers
  - dependency injection containers
  - API endpoints
  - persistence/database models

## Project Structure

```tree
xr-unity/
├── .gitignore
├── README.md
└── VR Room Project/
    ├── .vsconfig
    ├── Assets/
    │   ├── Challenges/
    │   │   ├── 01_Architecture/
    │   │   ├── 02_3DPainting/
    │   │   └── 03_Training/
    │   ├── Samples/
    │   │   └── XR Interaction Toolkit/
    │   ├── Scenes/
    │   │   └── Basic_Hotel.unity
    │   ├── Settings/
    │   │   ├── Left_NearFarInteractor.preset
    │   │   ├── NearFarInteractor.preset
    │   │   ├── New Lighting Settings.lighting
    │   │   ├── Right_XRDirectInteractor.preset
    │   │   └── Right_XRRayInteractor.preset
    │   ├── TextMesh Pro/
    │   ├── XR/
    │   │   ├── Loaders/
    │   │   ├── Settings/
    │   │   └── XRGeneralSettings.asset
    │   ├── XRI/
    │   │   └── Settings/
    │   ├── _Course Library/
    │   └── Basic_Hotel_Room.unity
    ├── Packages/
    │   ├── manifest.json
    │   └── packages-lock.json
    └── ProjectSettings/
        ├── EditorBuildSettings.asset
        ├── GraphicsSettings.asset
        ├── ProjectSettings.asset
        ├── ProjectVersion.txt
        ├── QualitySettings.asset
        ├── URPProjectSettings.asset
        ├── XRPackageSettings.asset
        └── XRSettings.asset
```

### Important Directories

- **`VR Room Project/Assets`**: Main Unity content folder containing scenes, XR assets, presets, samples, and challenge materials
- **`VR Room Project/Assets/Scenes`**: Contains the main scene currently enabled in build settings
- **`VR Room Project/Assets/Challenges`**: Prototype or exercise content grouped by scenario
- **`VR Room Project/Assets/XR`**: XR general settings and loader-related assets
- **`VR Room Project/Assets/XRI`**: XR Interaction Toolkit configuration assets
- **`VR Room Project/Packages`**: Unity package manifest and lockfile
- **`VR Room Project/ProjectSettings`**: Source-controlled Unity editor and project configuration

## Getting Started

### Prerequisites

- **Unity Editor 6000.3.8f1**
- **Unity Hub**
- **Visual Studio** with the **Managed Game** workload (recommended based on `.vsconfig`)

### Installation

```bash
git clone https://github.com/drussell33/xr-unity.git
cd xr-unity
```

Then open the Unity project folder:

```text
VR Room Project
```

### Dependency Resolution

Unity will restore package dependencies automatically from:

```text
VR Room Project/Packages/manifest.json
```

### Usage

Because this repository is a Unity project, there is no separate backend or frontend startup command.

#### Open the project

1. Open **Unity Hub**
2. Add the repository's `VR Room Project` folder
3. Open it using **Unity Editor 6000.3.8f1**

#### Run the main scene

1. In Unity, open:

```text
Assets/Scenes/Basic_Hotel.unity
```

2. Press **Play** in the Unity Editor

#### Build for XR targets

Open **Build Settings** in Unity and verify scene/build target configuration before creating a player build.

## Roadmap

- [x] Unity project initialized and committed
- [x] Unity 6 editor version pinned in project settings
- [x] URP package configured
- [x] XR Interaction Toolkit integrated
- [x] XR Management integrated
- [x] Oculus XR Plugin included
- [x] OpenXR Plugin included
- [x] Main `Basic_Hotel` scene included
- [x] Challenge content for architecture, 3D painting, and training included
- [x] XR presets and settings assets included
- [ ] Add a richer root-level README with setup screenshots and device notes
- [ ] Document supported XR hardware/runtime targets explicitly
- [ ] Add scene-by-scene walkthroughs
- [ ] Add input/control mapping documentation
- [ ] Add build instructions for standalone and headset deployment
- [ ] Add demo media or gameplay captures
- [ ] Add license metadata if open-source licensing is intended

## Contributing

Contributions should follow a standard GitHub workflow:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Commit with clear messages
5. Push your branch
6. Open a pull request

Before submitting changes:

- Keep Unity project settings consistent
- Avoid committing unnecessary generated artifacts
- Verify scenes and XR settings still open correctly in the target Unity version

## Screenshots / Demo

Screenshots, GIFs, or video walkthroughs can be added here.

Suggested additions:

- Main hotel room scene
- XR interaction examples
- Challenge scenes
- In-editor hierarchy and game view captures

## Contact

- GitHub: [drussell33](https://github.com/drussell33)
