// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    public static class PythonCompositeLayupEmitter
    {
        public static void Emit(PythonJournalWriter writer, string oldName, CompositeLayup layup)
        {
            var arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(layup.Name));
            arguments.Add(PythonRegionEmitter.Emit(writer, layup.RegionName, layup.RegionType, layup.CreationData));
            var plies = layup.Plies.Select(p => PythonJournalWriter.Join("pmx.create.composite_ply",
                PythonValueFormatter.Text(p.Name), PythonValueFormatter.Text(p.MaterialName),
                PythonValueFormatter.Number(p.Thickness), PythonValueFormatter.Number(p.AngleDeg),
                PythonValueFormatter.Format(p.ElementsThroughThickness)));
            arguments.Add("[" + string.Join(", ", plies) + "]");
            arguments.Add("coordinate_system_name=" + PythonValueFormatter.Text(layup.CoordinateSystemName));
            arguments.Add("stacking_axis=" + layup.StackingAxis);
            arguments.Add("symmetric=" + PythonValueFormatter.Format(layup.Symmetric));
            arguments.Add("boundary_tolerance=" + PythonValueFormatter.Number(layup.BoundaryTolerance));
            arguments.Add("active=" + PythonValueFormatter.Format(layup.Active));
            writer.Statement(writer.Call("pmx.model.composite_layups", oldName == null ? "add" : "replace", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
    }
}
