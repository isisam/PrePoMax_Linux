// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Globalization;
using System.Linq;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalOrientation : CalculixKeyword
    {
        private readonly string _name;
        private readonly double[] _a;
        private readonly double[] _b;
        private readonly double _angle;

        public CalOrientation(ExpandedCompositePly ply)
        {
            _name = ply.OrientationName;
            _a = ply.A;
            _b = ply.B;
            _angle = ply.AngleDeg;
        }
        public override string GetKeywordString()
        {
            return "*ORIENTATION, NAME=" + _name + ", SYSTEM=RECTANGULAR" + Environment.NewLine;
        }
        public override string GetDataString()
        {
            // CalculiX 2.21 reads only the first 20 characters of a numeric field.
            return string.Join(",", _a.Concat(_b).Select(Number)) + Environment.NewLine +
                   "3, " + Number(_angle) + Environment.NewLine;
        }
        private static string Number(double value) { return value.ToString("G12", CultureInfo.InvariantCulture); }
    }
}
