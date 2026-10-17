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
using DynamicTypeDescriptor;


namespace UserControls
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("UserControls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [System.Serializable]
    public class XYPlotSettings : IXYPlotSettings
    {
        // Variables
        public const string DefaultXAxisTitle = "X axis";
        public const string DefaultYAxisTitle = "Y axis";
        private static readonly float[] _fontSizes = new float[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 };
        private string _xAxisTitle;
        private bool _xAxisAutoScale;
        private double _xAxisMin;
        private double _xAxisMax;
        private string _yAxisTitle;
        private bool _yAxisAutoScale;
        private double _yAxisMin;
        private double _yAxisMax;
        private bool _showLegend;
        private XYPlotLegendPositionEnum _legendPosition;
        private bool _showPointSymbols;
        private XYPlotFontFamilyEnum _fontFamily;
        private float _fontSize;


        // Properties
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(0, 10, "Title")]
        [DescriptionAttribute("The title shown below the X axis.")]
        public string XAxisTitle { get { return _xAxisTitle; } set { _xAxisTitle = value == null ? "" : value; } }
        //
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(1, 10, "Auto scale")]
        [DescriptionAttribute("Compute the X axis range from the curve data.")]
        public bool XAxisAutoScale
        {
            get { return _xAxisAutoScale; }
            set
            {
                _xAxisAutoScale = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(2, 10, "Minimum")]
        [DescriptionAttribute("The minimum value of the X axis.")]
        public double XAxisMin { get { return _xAxisMin; } set { _xAxisMin = value; } }
        //
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(3, 10, "Maximum")]
        [DescriptionAttribute("The maximum value of the X axis.")]
        public double XAxisMax { get { return _xAxisMax; } set { _xAxisMax = value; } }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(0, 10, "Title")]
        [DescriptionAttribute("The title shown left of the Y axis.")]
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value == null ? "" : value; } }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(1, 10, "Auto scale")]
        [DescriptionAttribute("Compute the Y axis range from the curve data.")]
        public bool YAxisAutoScale
        {
            get { return _yAxisAutoScale; }
            set
            {
                _yAxisAutoScale = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(2, 10, "Minimum")]
        [DescriptionAttribute("The minimum value of the Y axis.")]
        public double YAxisMin { get { return _yAxisMin; } set { _yAxisMin = value; } }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(3, 10, "Maximum")]
        [DescriptionAttribute("The maximum value of the Y axis.")]
        public double YAxisMax { get { return _yAxisMax; } set { _yAxisMax = value; } }
        //
        [CategoryAttribute("Legend")]
        [OrderedDisplayName(0, 10, "Show legend")]
        [DescriptionAttribute("Show the curve names in the plot legend.")]
        public bool ShowLegend
        {
            get { return _showLegend; }
            set
            {
                _showLegend = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Legend")]
        [OrderedDisplayName(1, 10, "Position")]
        [DescriptionAttribute("The position of the legend inside or around the plot.")]
        public XYPlotLegendPositionEnum LegendPosition { get { return _legendPosition; } set { _legendPosition = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(0, 10, "Show point symbols")]
        [DescriptionAttribute("Draw a symbol at each curve data point.")]
        public bool ShowPointSymbols { get { return _showPointSymbols; } set { _showPointSymbols = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(1, 10, "Font")]
        [DescriptionAttribute("The font of the legend, the axis titles and the axis labels.")]
        public XYPlotFontFamilyEnum FontFamily { get { return _fontFamily; } set { _fontFamily = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(2, 10, "Font size")]
        [DescriptionAttribute("The font size of the legend, the axis titles and the axis labels.")]
        public float FontSize
        {
            get { return _fontSize; }
            set
            {
                if (value < 6) _fontSize = 6;
                else if (value > 30) _fontSize = 30;
                else _fontSize = value;
            }
        }


        // Constructors
        public XYPlotSettings()
        {
            _xAxisTitle = DefaultXAxisTitle;
            _xAxisAutoScale = true;
            _xAxisMin = 0;
            _xAxisMax = 1;
            _yAxisTitle = DefaultYAxisTitle;
            _yAxisAutoScale = true;
            _yAxisMin = 0;
            _yAxisMax = 1;
            _showLegend = true;
            _legendPosition = XYPlotLegendPositionEnum.TopCenter;
            _showPointSymbols = false;
            _fontFamily = XYPlotFontFamilyEnum.SegoeUI;
            _fontSize = 16;
        }
        public XYPlotSettings(XYPlotSettings settings)
            : this()
        {
            CopyFrom(settings);
        }


        // Methods
        public void CopyFrom(XYPlotSettings settings)
        {
            if (settings == null) return;
            //
            _xAxisTitle = settings.XAxisTitle;
            _xAxisAutoScale = settings.XAxisAutoScale;
            _xAxisMin = settings.XAxisMin;
            _xAxisMax = settings.XAxisMax;
            _yAxisTitle = settings.YAxisTitle;
            _yAxisAutoScale = settings.YAxisAutoScale;
            _yAxisMin = settings.YAxisMin;
            _yAxisMax = settings.YAxisMax;
            _showLegend = settings.ShowLegend;
            _legendPosition = settings.LegendPosition;
            _showPointSymbols = settings.ShowPointSymbols;
            _fontFamily = settings.FontFamily;
            _fontSize = settings.FontSize;
        }
        public string GetFontFamilyName()
        {
            switch (_fontFamily)
            {
                case XYPlotFontFamilyEnum.Arial: return "Arial";
                case XYPlotFontFamilyEnum.Calibri: return "Calibri";
                case XYPlotFontFamilyEnum.Verdana: return "Verdana";
                case XYPlotFontFamilyEnum.Tahoma: return "Tahoma";
                case XYPlotFontFamilyEnum.TimesNewRoman: return "Times New Roman";
                case XYPlotFontFamilyEnum.Cambria: return "Cambria";
                case XYPlotFontFamilyEnum.Consolas: return "Consolas";
                default: return "Segoe UI";
            }
        }
        public void SetFontFamilyName(string fontFamilyName)
        {
            switch (fontFamilyName)
            {
                case "Arial": _fontFamily = XYPlotFontFamilyEnum.Arial; break;
                case "Calibri": _fontFamily = XYPlotFontFamilyEnum.Calibri; break;
                case "Verdana": _fontFamily = XYPlotFontFamilyEnum.Verdana; break;
                case "Tahoma": _fontFamily = XYPlotFontFamilyEnum.Tahoma; break;
                case "Times New Roman": _fontFamily = XYPlotFontFamilyEnum.TimesNewRoman; break;
                case "Cambria": _fontFamily = XYPlotFontFamilyEnum.Cambria; break;
                case "Consolas": _fontFamily = XYPlotFontFamilyEnum.Consolas; break;
                default: _fontFamily = XYPlotFontFamilyEnum.SegoeUI; break;
            }
        }




        public void InstallProvider() { PlotSettingsServices.Install(this); }
        public void UpdateVisibility() { PlotSettingsServices.Update(this); }
    }
}
