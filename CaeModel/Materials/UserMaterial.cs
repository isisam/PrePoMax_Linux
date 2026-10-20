// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class UserMaterialConstant
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EquationContainer Value { get; set; }

        public UserMaterialConstant(string name, double value, string description = "")
        {
            Name = name;
            Description = description;
            Value = new EquationContainer(typeof(StringDoubleConverter), value);
        }
    }

    [Serializable]
    public class UserMaterial : MaterialProperty, ISerializable
    {
        private List<UserMaterialConstant> _constants;
        private int _numberOfStateVariables;

        public List<UserMaterialConstant> Constants { get { return _constants; } }
        public int NumberOfStateVariables
        {
            get { return _numberOfStateVariables; }
            set
            {
                if (value < 0) throw new CaeException("DEPVAR must not be negative.");
                _numberOfStateVariables = value;
            }
        }

        public UserMaterial(double[] constants, int numberOfStateVariables = 0)
        {
            _constants = new List<UserMaterialConstant>();
            NumberOfStateVariables = numberOfStateVariables;
            if (constants != null)
                for (int i = 0; i < constants.Length; i++)
                    _constants.Add(new UserMaterialConstant("C" + (i + 1), constants[i], ""));
        }
        public UserMaterial(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _constants = new List<UserMaterialConstant>();
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_constants": _constants = (List<UserMaterialConstant>)entry.Value; break;
                    case "_numberOfStateVariables": _numberOfStateVariables = (int)entry.Value; break;
                }
            }
        }
        public override void CheckEquations()
        {
            if (_constants == null || _constants.Count == 0)
                throw new CaeException("At least one user material constant is required.");
            if (_numberOfStateVariables < 0) throw new CaeException("DEPVAR must not be negative.");
            foreach (UserMaterialConstant constant in _constants)
            {
                if (constant == null || constant.Value == null) throw new CaeException("A user material constant is missing.");
                constant.Value.CheckEquation();
                if (double.IsNaN(constant.Value.Value) || double.IsInfinity(constant.Value.Value))
                    throw new CaeException("User material constants must be finite.");
            }
        }
        public void CheckMaterialName(string name) { CheckEquations(); }
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_constants", _constants, typeof(List<UserMaterialConstant>));
            info.AddValue("_numberOfStateVariables", _numberOfStateVariables);
        }
    }
}
