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
using System.Runtime.Serialization;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    // Mode A: bottom-to-top plies assigned to an existing solid mesh. No UI dependencies.
    [Serializable]
    public class CompositeLayup : NamedClass, IMultiRegion, ISerializable
    {
        public string RegionName { get; set; }
        public RegionTypeEnum RegionType { get; set; }
        public int[] CreationIds { get; set; }
        public Selection CreationData { get; set; }
        public string CoordinateSystemName { get; set; }
        public int StackingAxis { get; set; }
        public bool Symmetric { get; set; }
        public List<CompositePly> Plies { get; set; }
        public double BoundaryTolerance { get; set; }

        public CompositeLayup(string name, string regionName, RegionTypeEnum regionType) : base(name)
        {
            RegionName = regionName;
            RegionType = regionType;
            StackingAxis = 3;
            BoundaryTolerance = 1E-6;
            Plies = new List<CompositePly>();
        }
        public CompositeLayup(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            RegionName = info.GetString("RegionName");
            RegionType = (RegionTypeEnum)info.GetValue("RegionType", typeof(RegionTypeEnum));
            CreationIds = (int[])info.GetValue("CreationIds", typeof(int[]));
            CreationData = (Selection)info.GetValue("CreationData", typeof(Selection));
            CoordinateSystemName = info.GetString("CoordinateSystemName");
            StackingAxis = info.GetInt32("StackingAxis");
            Symmetric = info.GetBoolean("Symmetric");
            BoundaryTolerance = info.GetDouble("BoundaryTolerance");
            Plies = (List<CompositePly>)info.GetValue("Plies", typeof(List<CompositePly>));
        }
        public List<CompositePly> GetExpandedPlies()
        {
            var result = new List<CompositePly>(Plies);
            if (Symmetric)
                for (int i = Plies.Count - 1; i >= 0; i--) result.Add(Plies[i]);
            return result;
        }
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("RegionName", RegionName);
            info.AddValue("RegionType", RegionType, typeof(RegionTypeEnum));
            info.AddValue("CreationIds", CreationIds, typeof(int[]));
            info.AddValue("CreationData", CreationData, typeof(Selection));
            info.AddValue("CoordinateSystemName", CoordinateSystemName);
            info.AddValue("StackingAxis", StackingAxis);
            info.AddValue("Symmetric", Symmetric);
            info.AddValue("BoundaryTolerance", BoundaryTolerance);
            info.AddValue("Plies", Plies, typeof(List<CompositePly>));
        }
    }
}
