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
using System.ComponentModel;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ViewUserMaterial : ViewMaterialProperty
    {
        private List<UserMaterialDataPoint> _points;
        private UserMaterial _material;
        public override string Name { get { return _material.Fatczm ? "Fatigue CZM (FATCZM)" : "User Material"; } }
        [Browsable(false)]
        public List<UserMaterialDataPoint> DataPoints { get { return _points; } }
        [Category("User material")]
        [DisplayName("State variables (DEPVAR)")]
        [Description("Number of state variables. FATCZM needs at least 6; initiation needs at least 8.")]
        public int NumberOfStateVariables
        {
            get { return _material.NumberOfStateVariables; }
            set { _material.NumberOfStateVariables = value; }
        }
        [Category("User material")]
        [DisplayName("FATCZM preset")]
        [Description("Edit the ordered constants on the Data points tab. Preset defaults use N, mm, MPa; convert manually for other units.")]
        public bool Fatczm { get { return _material.Fatczm; } }
        public ViewUserMaterial(UserMaterial material)
        {
            _material = material;
            _points = new List<UserMaterialDataPoint>();
            foreach (UserMaterialConstant constant in material.Constants) _points.Add(new UserMaterialDataPoint(constant));
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }
        public override MaterialProperty GetBase()
        {
            _material.Constants.Clear();
            foreach (UserMaterialDataPoint point in _points) _material.Constants.Add(point.GetBase());
            return _material;
        }
    }
}
