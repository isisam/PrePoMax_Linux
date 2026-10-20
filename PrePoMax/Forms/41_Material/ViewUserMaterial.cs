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
    public class ViewUserMaterial : ViewMaterialProperty
    {
        private List<UserMaterialDataPoint> _points;
        private UserMaterial _material;
        public override string Name { get { return "User Material"; } }
        [Browsable(false)]
        public List<UserMaterialDataPoint> DataPoints { get { return _points; } }
        [Category("User material")]
        [DisplayName("State variables (DEPVAR)")]
        [Description("Number of state variables for the user material.")]
        public int NumberOfStateVariables
        {
            get { return _material.NumberOfStateVariables; }
            set { _material.NumberOfStateVariables = value; }
        }
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
