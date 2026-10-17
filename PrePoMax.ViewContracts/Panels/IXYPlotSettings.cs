// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
namespace UserControls
{
    public static class PlotSettingsServices { public static Action<IXYPlotSettings> Install = _ => {}; public static Action<IXYPlotSettings> Update = _ => {}; }
    public interface IXYPlotSettings
    {
        void InstallProvider();
        void UpdateVisibility();
    }
}
