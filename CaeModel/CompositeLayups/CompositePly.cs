// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class CompositePly : NamedClass, ISerializable
    {
        public string MaterialName { get; set; }
        public double Thickness { get; set; }
        public double AngleDeg { get; set; }
        public int? ElementsThroughThickness { get; set; }

        public CompositePly(string name, string materialName, double thickness, double angleDeg,
                            int? elementsThroughThickness = null) : base(name)
        {
            MaterialName = materialName;
            Thickness = thickness;
            AngleDeg = angleDeg;
            ElementsThroughThickness = elementsThroughThickness;
        }
        public CompositePly(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            MaterialName = info.GetString("MaterialName");
            Thickness = info.GetDouble("Thickness");
            AngleDeg = info.GetDouble("AngleDeg");
            ElementsThroughThickness = (int?)info.GetValue("ElementsThroughThickness", typeof(int?));
        }
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MaterialName", MaterialName);
            info.AddValue("Thickness", Thickness);
            info.AddValue("AngleDeg", AngleDeg);
            info.AddValue("ElementsThroughThickness", ElementsThroughThickness, typeof(int?));
        }
    }
}
