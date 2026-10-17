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
using System.Text;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalUserMaterial : CalculixKeyword
    {
        private UserMaterial _material;
        public CalUserMaterial(UserMaterial material) { _material = material; }
        public override string GetKeywordString()
        {
            return "*USER MATERIAL, CONSTANTS=" + _material.Constants.Count + Environment.NewLine;
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _material.Constants.Count; i++)
            {
                sb.Append(_material.Constants[i].Value.Value.ToString("R", CultureInfo.InvariantCulture));
                if (i < _material.Constants.Count - 1) sb.Append(",");
                if ((i + 1) % 8 == 0 || i == _material.Constants.Count - 1) sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
