// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalDepvar : CalculixKeyword
    {
        private int _numberOfStateVariables;
        public CalDepvar(int numberOfStateVariables) { _numberOfStateVariables = numberOfStateVariables; }
        public override string GetKeywordString() { return "*DEPVAR" + Environment.NewLine; }
        public override string GetDataString() { return _numberOfStateVariables + Environment.NewLine; }
    }
}
