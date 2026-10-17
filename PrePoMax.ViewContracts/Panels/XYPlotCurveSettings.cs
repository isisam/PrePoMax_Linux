// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System.ComponentModel;
using System.Drawing;

using DynamicTypeDescriptor;

namespace UserControls
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("UserControls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [System.Serializable]
    public class XYPlotCurveSettings : IXYPlotSettings
    {
        // Variables                                                                                                                
        private static readonly float[] _lineWidths = new float[] { 0.5f, 1, 1.5f, 2, 2.5f, 3, 4, 5, 6, 8, 10 };
        private string _name;
        private Color _color;
        private float _lineWidth;
        private XYPlotLineStyleEnum _lineStyle;
        private bool _visible;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("The curve name shown in the plot legend and in the data grid column header.")]
        public string Name { get { return _name; } set { _name = value == null ? "" : value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(0, 10, "Color")]
        [DescriptionAttribute("The color of the curve line.")]
        public Color Color { get { return _color; } set { _color = Color.FromArgb(255, value); } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(1, 10, "Line width")]
        [DescriptionAttribute("The width of the curve line in pixels.")]
        public float LineWidth
        {
            get { return _lineWidth; }
            set
            {
                if (value < 0.5f) _lineWidth = 0.5f;
                else if (value > 10) _lineWidth = 10;
                else _lineWidth = value;
            }
        }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(2, 10, "Line style")]
        [DescriptionAttribute("The style of the curve line.")]
        public XYPlotLineStyleEnum LineStyle { get { return _lineStyle; } set { _lineStyle = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(3, 10, "Visible")]
        [DescriptionAttribute("Draw the curve in the plot.")]
        public bool Visible { get { return _visible; } set { _visible = value; } }


        // Constructors                                                                                                             
        public XYPlotCurveSettings()
        {
            _name = "";
            _color = Color.Black;
            _lineWidth = 2;
            _lineStyle = XYPlotLineStyleEnum.Solid;
            _visible = true;
        }
        public XYPlotCurveSettings(XYPlotCurveSettings settings)
            : this()
        {
            CopyFrom(settings);
        }


        // Methods                                                                                                                  
        public void CopyFrom(XYPlotCurveSettings settings)
        {
            if (settings == null) return;
            //
            _name = settings.Name;
            _color = settings.Color;
            _lineWidth = settings.LineWidth;
            _lineStyle = settings.LineStyle;
            _visible = settings.Visible;
        }




        public void InstallProvider() { PlotSettingsServices.Install(this); }
        public void UpdateVisibility() { PlotSettingsServices.Update(this); }
    }
}
