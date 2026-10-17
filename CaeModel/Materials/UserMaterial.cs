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
        private bool _fatczm;

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
        public bool Fatczm { get { return _fatczm; } }

        public UserMaterial(double[] constants, int numberOfStateVariables = 0, bool fatczm = false)
        {
            _constants = new List<UserMaterialConstant>();
            _fatczm = fatczm;
            NumberOfStateVariables = numberOfStateVariables;
            if (constants != null)
                for (int i = 0; i < constants.Length; i++)
                    _constants.Add(new UserMaterialConstant(fatczm && i < FatczmNames.Length ? FatczmNames[i] : "C" + (i + 1),
                                                           constants[i], fatczm && i < FatczmDescriptions.Length ?
                                                           FatczmDescriptions[i] : ""));
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
                    case "_fatczm": _fatczm = (bool)entry.Value; break;
                }
            }
        }
        // IM7 ENF energy-driver example, N/mm/MPa; gen_inp.py (10 significant figures).
        public static readonly string[] FatczmNames = {
            "tIC", "tIIC", "GIC", "GIIC", "Dint", "tc", "aI", "bI", "gI", "aII", "bII", "gII",
            "dDmax", "first fatigue step", "normal axis", "dNmax", "compression penalty", "dDrel",
            "pre-crack normal behaviour", "damage variable", "ainc", "binc", "deta", "initiation clock", "F", "mixed-mode rule"
        };
        public static readonly string[] FatczmDescriptions = {
            "Mode I strength [MPa]", "Mode II strength [MPa]", "Mode I toughness [N/mm]", "Mode II toughness [N/mm]",
            "Elastic-branch end damage (0 < Dint < 1); pre-crack: 0.999999", "Layer thickness [mm], equal to element thickness",
            "Mode I alpha [1/cycle]", "Mode I beta [-]", "Mode I gamma [-]", "Mode II alpha [1/cycle]",
            "Mode II beta [-]", "Mode II gamma [-]", "Maximum damage increment per jump [-]", "First fatigue step (integer)",
            "Global normal axis: 1=X, 2=Y, 3=Z", "Maximum cycles per jump (0=unlimited)",
            "Compression penalty [N/mm^3] (0=automatic)", "Maximum relative damage increment (0=off)",
            "Insert: 1=bonded normal, 0=compression contact only", "0=Hong D, 1=Camanho-Davila d",
            "Initiation alpha [1/cycle] (0=off; positive needs DEPVAR >= 8)", "Initiation exponent [-]",
            "Maximum initiation increment (0=solver default 0.05)", "0=local Miner clock, 1=global pocket clock",
            "0=traction, 1=energy driver", "0=Hong Eq.13, 1=log-linear displacement share, 2=log-linear energy share"
        };
        public static UserMaterial CreateFatczm(bool preCrack = false)
        {
            return new UserMaterial(new double[] {
                60, 90, 0.2399, 0.739, preCrack ? 0.999999 : 0.001, 0.001, 1, 6.75,
                -1, 1, 5.25, -1, 0.03, 2, 2, 0, 7503126.303, 0.5, 1, 0, 0, 0, 0, 0, 1, 1
            }, 6, true);
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
            if (_fatczm) CheckFatczm();
        }
        public void CheckMaterialName(string name)
        {
            CheckEquations();
            if (_fatczm && !name.StartsWith("USERFATCZM", StringComparison.Ordinal))
                throw new CaeException("The FATCZM material name must start with USERFATCZM. Prefix the name with USERFATCZM.");
            if (name.StartsWith("USERFATCZM", StringComparison.Ordinal))
            {
                CheckFatczm();
                if (name.StartsWith("USERFATCZMPC", StringComparison.Ordinal) && _constants[4].Value.Value != 0.999999)
                    throw new CaeException("USERFATCZMPC is a pre-crack material: set constant 5 (Dint) to 0.999999.");
            }
        }
        private void CheckFatczm()
        {
            if ((_fatczm && _constants.Count != 26) || _constants.Count < 19 || _constants.Count % 8 == 0)
                throw new CaeException("The FATCZM preset requires 26 constants. Generic FATCZM needs at least 19; " +
                                       "the count must not be a multiple of 8 in CalculiX 2.21 (append a dummy zero).");
            if (_numberOfStateVariables < 6) throw new CaeException("FATCZM requires DEPVAR >= 6.");
            double[] c = _constants.Select(item => item.Value.Value).ToArray();
            for (int i = 0; i < 6; i++)
                if (c[i] <= 0) throw new CaeException("FATCZM constants 1-6 must be positive.");
            if (c[4] >= 1) throw new CaeException("FATCZM Dint must be less than 1.");
            // The UMAT evaluates log(alpha) in the log-linear mixed-mode law.
            if (c[6] <= 0 || c[9] <= 0 || c[12] <= 0)
                throw new CaeException("FATCZM alpha I, alpha II and dDmax must be positive.");
            CheckInteger(c[13], 1, int.MaxValue, "first fatigue step");
            CheckInteger(c[14], 1, 3, "normal axis");
            for (int i = 15; i <= 17; i++)
                if (c[i] < 0) throw new CaeException("FATCZM constants 16-18 must not be negative.");
            CheckInteger(c[18], 0, 1, "pre-crack normal behaviour");
            if (c.Length > 19) CheckInteger(c[19], 0, 1, "damage variable");
            if (c.Length > 20 && c[20] < 0) throw new CaeException("Initiation alpha must not be negative.");
            if (c.Length > 20 && c[20] > 0 && (c.Length < 24 || _numberOfStateVariables < 8))
                throw new CaeException("FATCZM initiation needs constants 21-24 and DEPVAR >= 8.");
            if (c.Length > 23) CheckInteger(c[23], 0, 1, "initiation clock");
            if (c.Length > 24) CheckInteger(c[24], 0, 1, "F driver");
            if (c.Length > 25) CheckInteger(c[25], 0, 2, "mixed-mode rule");
        }
        private static void CheckInteger(double value, int min, int max, string name)
        {
            if (value != Math.Truncate(value) || value < min || value > max)
                throw new CaeException("FATCZM " + name + " must be an integer in [" + min + ", " + max + "].");
        }
        public static void CheckSharedFatczmConstants(IEnumerable<Material> materials)
        {
            UserMaterial reference = null;
            foreach (Material material in materials)
            {
                if (!material.Active || !material.Name.StartsWith("USERFATCZM", StringComparison.Ordinal)) continue;
                foreach (UserMaterial um in material.Properties.OfType<UserMaterial>())
                {
                    if (um.Constants.Count < 19) continue;
                    if (reference == null) reference = um;
                    foreach (int id in new int[] { 12, 13, 17 })
                        if (reference.Constants[id].Value.Value != um.Constants[id].Value.Value)
                            throw new CaeException("FATCZM uses one global cycle counter: constants 13, 14 and 18 " +
                                                   "must agree in all active USERFATCZM materials.");
                }
            }
        }
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_constants", _constants, typeof(List<UserMaterialConstant>));
            info.AddValue("_numberOfStateVariables", _numberOfStateVariables);
            info.AddValue("_fatczm", _fatczm);
        }
    }
}
