using System.Runtime.CompilerServices;
using System.Drawing.Drawing2D;
using DynamicTypeDescriptor;
using ZedGraph;

namespace UserControls
{
    public static class XYPlotSettingsExtensions
    {
        public static void Initialize()
        {
            PlotSettingsServices.Install = s =>
            {
                if (s is XYPlotSettings p)
                    InstallProvider(p);
                else if (s is XYPlotCurveSettings c)
                    InstallProvider(c);
            };
            PlotSettingsServices.Update = s =>
            {
                if (s is XYPlotSettings p)
                    UpdateVisibility(p);
                else if (s is XYPlotCurveSettings c)
                    UpdateVisibility(c);
            };
        }

        private static readonly ConditionalWeakTable<object, DynamicCustomTypeDescriptor> descriptors = new ConditionalWeakTable<object, DynamicCustomTypeDescriptor>();
        public static LegendPos GetLegendPos(this XYPlotSettings settings)
        {
            switch (settings.LegendPosition)
            {
                case XYPlotLegendPositionEnum.Top:
                    return LegendPos.Top;
                case XYPlotLegendPositionEnum.TopFlushLeft:
                    return LegendPos.TopFlushLeft;
                case XYPlotLegendPositionEnum.BottomCenter:
                    return LegendPos.BottomCenter;
                case XYPlotLegendPositionEnum.Bottom:
                    return LegendPos.Bottom;
                case XYPlotLegendPositionEnum.BottomFlushLeft:
                    return LegendPos.BottomFlushLeft;
                case XYPlotLegendPositionEnum.Left:
                    return LegendPos.Left;
                case XYPlotLegendPositionEnum.Right:
                    return LegendPos.Right;
                case XYPlotLegendPositionEnum.InsideTopLeft:
                    return LegendPos.InsideTopLeft;
                case XYPlotLegendPositionEnum.InsideTopRight:
                    return LegendPos.InsideTopRight;
                case XYPlotLegendPositionEnum.InsideBotLeft:
                    return LegendPos.InsideBotLeft;
                case XYPlotLegendPositionEnum.InsideBotRight:
                    return LegendPos.InsideBotRight;
                default:
                    return LegendPos.TopCenter;
            }
        }

        public static void SetLegendPos(this XYPlotSettings settings, LegendPos legendPos)
        {
            switch (legendPos)
            {
                case LegendPos.Top:
                    settings.LegendPosition = XYPlotLegendPositionEnum.Top;
                    break;
                case LegendPos.TopFlushLeft:
                    settings.LegendPosition = XYPlotLegendPositionEnum.TopFlushLeft;
                    break;
                case LegendPos.BottomCenter:
                    settings.LegendPosition = XYPlotLegendPositionEnum.BottomCenter;
                    break;
                case LegendPos.Bottom:
                    settings.LegendPosition = XYPlotLegendPositionEnum.Bottom;
                    break;
                case LegendPos.BottomFlushLeft:
                    settings.LegendPosition = XYPlotLegendPositionEnum.BottomFlushLeft;
                    break;
                case LegendPos.Left:
                    settings.LegendPosition = XYPlotLegendPositionEnum.Left;
                    break;
                case LegendPos.Right:
                    settings.LegendPosition = XYPlotLegendPositionEnum.Right;
                    break;
                case LegendPos.InsideTopLeft:
                    settings.LegendPosition = XYPlotLegendPositionEnum.InsideTopLeft;
                    break;
                case LegendPos.InsideTopRight:
                    settings.LegendPosition = XYPlotLegendPositionEnum.InsideTopRight;
                    break;
                case LegendPos.InsideBotLeft:
                    settings.LegendPosition = XYPlotLegendPositionEnum.InsideBotLeft;
                    break;
                case LegendPos.InsideBotRight:
                    settings.LegendPosition = XYPlotLegendPositionEnum.InsideBotRight;
                    break;
                default:
                    settings.LegendPosition = XYPlotLegendPositionEnum.TopCenter;
                    break;
            }
        }

        public static void InstallProvider(this XYPlotSettings settings)
        {
            var _dctd = ProviderInstaller.Install(settings);
            descriptors.Remove(settings);
            descriptors.Add(settings, _dctd);
            //
            _dctd.RenameBooleanProperty(nameof(settings.XAxisAutoScale), "Yes", "No");
            _dctd.RenameBooleanProperty(nameof(settings.YAxisAutoScale), "Yes", "No");
            _dctd.RenameBooleanProperty(nameof(settings.ShowLegend), "Yes", "No");
            _dctd.RenameBooleanProperty(nameof(settings.ShowPointSymbols), "Yes", "No");
            //
            XYPlotSettingsTools.PopulateNumericProperty(_dctd, nameof(settings.FontSize), new float[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 });
        }

        public static void UpdateVisibility(this XYPlotSettings settings)
        {
            DynamicCustomTypeDescriptor _dctd;
            if (!descriptors.TryGetValue(settings, out _dctd))
                return;
            //
            _dctd.GetProperty(nameof(settings.XAxisMin)).SetIsBrowsable(!settings.XAxisAutoScale);
            _dctd.GetProperty(nameof(settings.XAxisMax)).SetIsBrowsable(!settings.XAxisAutoScale);
            _dctd.GetProperty(nameof(settings.YAxisMin)).SetIsBrowsable(!settings.YAxisAutoScale);
            _dctd.GetProperty(nameof(settings.YAxisMax)).SetIsBrowsable(!settings.YAxisAutoScale);
            //
            _dctd.GetProperty(nameof(settings.LegendPosition)).SetIsBrowsable(settings.ShowLegend);
        }

        public static DashStyle GetDashStyle(this XYPlotCurveSettings settings)
        {
            switch (settings.LineStyle)
            {
                case XYPlotLineStyleEnum.Dash:
                    return DashStyle.Dash;
                case XYPlotLineStyleEnum.Dot:
                    return DashStyle.Dot;
                case XYPlotLineStyleEnum.DashDot:
                    return DashStyle.DashDot;
                case XYPlotLineStyleEnum.DashDotDot:
                    return DashStyle.DashDotDot;
                default:
                    return DashStyle.Solid;
            }
        }

        public static void SetDashStyle(this XYPlotCurveSettings settings, DashStyle dashStyle)
        {
            switch (dashStyle)
            {
                case DashStyle.Dash:
                    settings.LineStyle = XYPlotLineStyleEnum.Dash;
                    break;
                case DashStyle.Dot:
                    settings.LineStyle = XYPlotLineStyleEnum.Dot;
                    break;
                case DashStyle.DashDot:
                    settings.LineStyle = XYPlotLineStyleEnum.DashDot;
                    break;
                case DashStyle.DashDotDot:
                    settings.LineStyle = XYPlotLineStyleEnum.DashDotDot;
                    break;
                default:
                    settings.LineStyle = XYPlotLineStyleEnum.Solid;
                    break;
            }
        }

        public static void InstallProvider(this XYPlotCurveSettings settings)
        {
            var _dctd = ProviderInstaller.Install(settings);
            descriptors.Remove(settings);
            descriptors.Add(settings, _dctd);
            //
            _dctd.RenameBooleanProperty(nameof(settings.Visible), "Yes", "No");
            //
            XYPlotSettingsTools.PopulateNumericProperty(_dctd, nameof(settings.LineWidth), new float[] { 0.5f, 1, 1.5f, 2, 2.5f, 3, 4, 5, 6, 8, 10 });
        }

        public static void UpdateVisibility(this XYPlotCurveSettings settings)
        {
        }
    }
}
