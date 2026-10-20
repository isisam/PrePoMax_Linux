# PrePoMax-RCM features

Version: 0.1.0-rc1g8-win.1 (RC). Author: S.Y. Su. Original upstream credits and licences are retained.

## What it does

Prepare finite element models, assign materials and loads, run CalculiX, and inspect results. Includes Traditional Chinese / English UI and composite layup views: 3D fibre directions, colours per ply, single-ply display and exploded view. Bundled stock CalculiX 2.21 is selected automatically at `Solver\ccx.exe`, using Spooles by default. Includes spring, beam and laminate examples.

## Differences from upstream

Adds the Chinese UI and composite visualisation features listed above. This student variant uses the public stock solver and a per-user installer with automatic solver setup.

## Limitations

Release candidate: the owner's manual 150% DPI check is still pending. Layup views help inspect the model but do not validate its physics.
No private fatigue or custom material solver extensions are supplied. Supported analyses depend on CalculiX 2.21 and your input model. Teaching examples are not engineering certification.

## Requirements

Windows 10/11 x64. .NET Framework 4.8 and an OpenGL-capable graphics driver. Recommended: 8 GB RAM and 2 GB free disk space. Larger jobs need more resources. Use a writable work folder; simple ASCII work paths are recommended for legacy native tools.

## Install

Download `PrePoMax-RCM_Setup_0.1.0-rc1g8-win.1.exe` from [GitHub Releases](https://github.com/isisam/PrePoMax_Linux/releases) → double-click → Next → Install → Finish. No administrator password is required. Default folder: `%LOCALAPPDATA%\Programs\PrePoMax-RCM`.

Launch from the Desktop or Start menu. The solver path is already set.

Silent installation: `PrePoMax-RCM_Setup_0.1.0-rc1g8-win.1.exe /S`. Optional `/D=full path` must be the last argument and is unquoted, e.g. `/S /D=C:\Student Apps\PrePoMax-RCM`.

## Uninstall

Use Windows Settings → Apps → Installed apps, or run `Uninstall.exe` in the installation folder (`/S` for silent removal). Removes shipped files, shortcuts and registration. Save edited examples outside the install folder. Your separately saved models and results remain.

## SmartScreen: unknown publisher

The installer is unsigned. Verify the GitHub release source and SHA256SUMS. If SmartScreen appears, choose More info → Run anyway. If school policy blocks execution, contact school IT. Do not disable antivirus or change security settings.
