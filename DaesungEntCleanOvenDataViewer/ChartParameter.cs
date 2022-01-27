using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciChart.Core.Extensions;

namespace DaesungEntCleanOvenDataViewer
{
    public class ChartParameter : DevExpress.Mvvm.BindableBase
    {
        protected double __ZoomRatio = 1.0;
        protected SciChart.Charting.Visuals.SciChartSurface ChartSurface;
        public ChartParameter(SciChart.Charting.Visuals.SciChartSurface chartSurface)
        {
            this.ChartSurface = chartSurface;
        }
        public double ZoomRatio
        {
            get { return __ZoomRatio; }
            set
            {
                __ZoomRatio = value;
                RaisePropertiesChanged("ZoomRatio");
            }
        }
        public double TemperatureScaleLow
        {
            get { return (double)ChartSurface.YAxes[0].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(TemperatureScaleHigh - value) / 4;
                ChartSurface.YAxes[0].MajorDelta = delta;
                ChartSurface.YAxes[0].MinorDelta = delta / 5;
                ChartSurface.YAxes[0].VisibleRange.Min = value;
                RaisePropertiesChanged("TemperatureScaleLow");
            }
        }
        public double TemperatureScaleHigh
        {
            get { return (double)ChartSurface.YAxes[0].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - TemperatureScaleLow) / 4;
                ChartSurface.YAxes[0].MajorDelta = delta;
                ChartSurface.YAxes[0].MinorDelta = delta / 5;
                ChartSurface.YAxes[0].VisibleRange.Max = value;
                RaisePropertiesChanged("TemperatureScaleHigh");
            }
        }
        public double PercentageScaleLow
        {
            get { return (double)ChartSurface.YAxes[1].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(PercentageScaleHigh - value) / 4;
                ChartSurface.YAxes[1].MajorDelta = delta;
                ChartSurface.YAxes[1].MinorDelta = delta / 5;
                ChartSurface.YAxes[1].VisibleRange.Min = value;
                RaisePropertiesChanged("PercentageScaleLow");
            }
        }
        public double PercentageScaleHigh
        {
            get { return (double)ChartSurface.YAxes[1].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - PercentageScaleLow) / 4;
                ChartSurface.YAxes[1].MajorDelta = delta;
                ChartSurface.YAxes[1].MinorDelta = delta / 5;
                ChartSurface.YAxes[1].VisibleRange.Max = value;
                RaisePropertiesChanged("PercentageScaleHigh");
            }
        }
        public double DifferencePressureScaleLow
        {
            get { return (double)ChartSurface.YAxes[2].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(DifferencePressureScaleHigh - value) / 4;
                ChartSurface.YAxes[2].MajorDelta = delta;
                ChartSurface.YAxes[2].MinorDelta = delta / 5;
                ChartSurface.YAxes[2].VisibleRange.Min = value;
                RaisePropertiesChanged("DifferencePressureScaleLow");
            }
        }
        public double DifferencePressureScaleHigh
        {
            get { return (double)ChartSurface.YAxes[2].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - DifferencePressureScaleLow) / 4;
                ChartSurface.YAxes[2].MajorDelta = delta;
                ChartSurface.YAxes[2].MinorDelta = delta / 5;
                ChartSurface.YAxes[2].VisibleRange.Max = value;
                RaisePropertiesChanged("DifferencePressureScaleHigh");
            }
        }
        public double FrequencyScaleLow
        {
            get { return (double)ChartSurface.YAxes[3].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(FrequencyScaleHigh - value) / 4;
                ChartSurface.YAxes[3].MajorDelta = delta;
                ChartSurface.YAxes[3].MinorDelta = delta / 5;
                ChartSurface.YAxes[3].VisibleRange.Min = value;
                RaisePropertiesChanged("FrequencyScaleLow");
            }
        }
        public double FrequencyScaleHigh
        {
            get { return (double)ChartSurface.YAxes[3].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - FrequencyScaleLow) / 4;
                ChartSurface.YAxes[3].MajorDelta = delta;
                ChartSurface.YAxes[3].MinorDelta = delta / 5;
                ChartSurface.YAxes[3].VisibleRange.Max = value;
                RaisePropertiesChanged("FrequencyScaleHigh");
            }
        }
        public double MfcScaleLow
        {
            get { return (double)ChartSurface.YAxes[4].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(MfcScaleHigh - value) / 4;
                ChartSurface.YAxes[4].MajorDelta = delta;
                ChartSurface.YAxes[4].MinorDelta = delta / 5;
                ChartSurface.YAxes[4].VisibleRange.Min = value;
                RaisePropertiesChanged("MfcScaleLow");
            }
        }
        public double MfcScaleHigh
        {
            get { return (double)ChartSurface.YAxes[4].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - MfcScaleLow) / 4;
                ChartSurface.YAxes[4].MajorDelta = delta;
                ChartSurface.YAxes[4].MinorDelta = delta / 5;
                ChartSurface.YAxes[4].VisibleRange.Max = value;
                RaisePropertiesChanged("MfcScaleHigh");
            }
        }
        public double O2TemperatureScaleLow
        {
            get { return (double)ChartSurface.YAxes[5].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(O2TemperatureScaleHigh - value) / 4;
                ChartSurface.YAxes[5].MajorDelta = delta;
                ChartSurface.YAxes[5].MinorDelta = delta / 5;
                ChartSurface.YAxes[5].VisibleRange.Min = value;
                RaisePropertiesChanged("O2TemperatureScaleLow");
            }
        }
        public double O2TemperatureScaleHigh
        {
            get { return (double)ChartSurface.YAxes[5].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - O2TemperatureScaleLow) / 4;
                ChartSurface.YAxes[5].MajorDelta = delta;
                ChartSurface.YAxes[5].MinorDelta = delta / 5;
                ChartSurface.YAxes[5].VisibleRange.Max = value;
                RaisePropertiesChanged("O2TemperatureScaleHigh");
            }
        }
        public double O2EmfScaleLow
        {
            get { return (double)ChartSurface.YAxes[6].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(O2EmfScaleHigh - value) / 4;
                ChartSurface.YAxes[6].MajorDelta = delta;
                ChartSurface.YAxes[6].MinorDelta = delta / 5;
                ChartSurface.YAxes[6].VisibleRange.Min = value;
                RaisePropertiesChanged("O2EmfScaleLow");
            }
        }
        public double O2EmfScaleHigh
        {
            get { return (double)ChartSurface.YAxes[6].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - O2EmfScaleLow) / 4;
                ChartSurface.YAxes[6].MajorDelta = delta;
                ChartSurface.YAxes[6].MinorDelta = delta / 5;
                ChartSurface.YAxes[6].VisibleRange.Max = value;
                RaisePropertiesChanged("O2EmfScaleHigh");
            }
        }
        public double O2ppmScaleLow
        {
            get { return (double)ChartSurface.YAxes[7].VisibleRange.Min; }
            set
            {
                double delta = Math.Abs(O2ppmScaleHigh - value) / 4;
                ChartSurface.YAxes[7].MajorDelta = delta;
                ChartSurface.YAxes[7].MinorDelta = delta / 5;
                ChartSurface.YAxes[7].VisibleRange.Min = value;
                RaisePropertiesChanged("O2ppmScaleLow");
            }
        }
        public double O2ppmScaleHigh
        {
            get { return (double)ChartSurface.YAxes[7].VisibleRange.Max; }
            set
            {
                double delta = Math.Abs(value - O2ppmScaleLow) / 4;
                ChartSurface.YAxes[7].MajorDelta = delta;
                ChartSurface.YAxes[7].MinorDelta = delta / 5;
                ChartSurface.YAxes[7].VisibleRange.Max = value;
                RaisePropertiesChanged("O2ppmScaleHigh");
            }
        }     
    }
}
