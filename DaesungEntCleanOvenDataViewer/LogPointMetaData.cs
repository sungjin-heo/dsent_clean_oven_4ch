using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SciChart.Data.Model;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Axes.LabelProviders;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.PaletteProviders;

namespace DaesungEntCleanOvenDataViewer
{
    class LogPointMetaData : IPointMetadata
    {
        public LogPointMetaData(double value, System.Windows.Media.Color color)
        {
            this.Value = value;
            this.Color = color;
        }
        public bool IsSelected { get; set; }
        public double Value { get; set; }
        public System.Windows.Media.Color Color { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
