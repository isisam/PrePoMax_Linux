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
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ViewDistributionFromFile : ViewMappedDistribution
    {
        // Variables                                                                                                                
        private DistributionFromFile _distributionFromFile;


        // Properties                                                                                                               
        [CategoryAttribute("Source")]
        [OrderedDisplayName(0, 10, "File name")]
        [DescriptionAttribute("Select the file from which the distribution will be imported.")]
        [Editor("CaeGlobals.FilteredFileNameEditor, CaeGlobals", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Id(1, 2)]
        public string FileName
        {
            get { return _distributionFromFile.FileName; }
            set { _distributionFromFile.FileName = value; }
        }
        //
        [CategoryAttribute("Interpolation")]
        [OrderedDisplayName(0, 10, "Interpolator")]
        [DescriptionAttribute("Select the interpolation type. The Gauss interpolation uses the kernel equation: exp(-(r/R)²), " +
                              "while the Shepard interpolation uses the kernel equation: 1/r². R is the interpolator radius and " +
                              "r is the distance to the neighbouring point.")]
        [Id(1, 3)]
        public CaeResults.CloudInterpolatorEnum Interpolator
        {
            get { return _distributionFromFile.InterpolatorType; }
            set
            {
                _distributionFromFile.InterpolatorType = value;
                UpdateVisibility();
            }
        }
        [CategoryAttribute("Interpolation")]
        [OrderedDisplayName(1, 10, "Interpolator radius")]
        [DescriptionAttribute("Set the value of the interpolator kernel radius.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 3)]
        public EquationString InterpolatorRadius
        {
            get { return _distributionFromFile.InterpolatorRadius.Equation; }
            set { _distributionFromFile.InterpolatorRadius.Equation = value; }
        }


        // Constructors                                                                                                             
        public ViewDistributionFromFile(DistributionFromFile distributionFromFile)
            : base(distributionFromFile)
        {
            _distributionFromFile = distributionFromFile;
            //
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            UpdateVisibility();
        }


        // Methods
        public override Distribution GetBase()
        {
            return _distributionFromFile;
        }
        public void PopulateDropDownLists(string[] coordinateSystemNames)
        {
            base.PopulateCoordinateSystemNames(coordinateSystemNames);
        }
        private void UpdateVisibility()
        {
            bool visible = _distributionFromFile.InterpolatorType != CaeResults.CloudInterpolatorEnum.ClosestPoint;
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(InterpolatorRadius)).SetIsBrowsable(visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(InterpolatorRadius)).SetIsBrowsable(visible);
        }
        public void UpdateFileBrowserDialog()
        {
            FileEditorFilter.Filter = "Text files|*.txt|All files|*.*";
        }
    }
}
