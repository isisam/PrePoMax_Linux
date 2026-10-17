// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class UserMaterialDataPoint
    {
        private UserMaterialConstant _constant;
        [DisplayName("Constant name")]
        public string Name { get { return _constant.Name; } set { _constant.Name = value; } }
        [DisplayName("Value / equation")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString ValueEq { get { return _constant.Value.Equation; } set { _constant.Value.Equation = value; } }
        [DisplayName("Unit / description")]
        public string Description { get { return _constant.Description; } set { _constant.Description = value; } }

        public UserMaterialDataPoint() : this(new UserMaterialConstant("C", 0)) { }
        public UserMaterialDataPoint(UserMaterialConstant constant) { _constant = constant; }
        public UserMaterialConstant GetBase() { return _constant; }
    }
}
