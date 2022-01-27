using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.ChartSeries;

namespace DaesungEntCleanOvenDataViewer
{
    public class RenderableSeriesWrapper : DevExpress.Mvvm.BindableBase
    {
        public RenderableSeriesWrapper(string name, string yId, System.Windows.Media.Color stroke)
        {
            var dataSeries = new XyDataSeries<DateTime, double> { SeriesName = name, AcceptsUnsortedData= true };
            this.RenderSeries = new LineRenderableSeriesViewModel {
                DataSeries = dataSeries,
                IsVisible = true,
                AntiAliasing = false,
                Stroke = stroke,
                StrokeThickness = 1,
                YAxisId = yId
            };            
        }
        public IRenderableSeriesViewModel RenderSeries { get; protected set; }
        public string Name { get { return RenderSeries.DataSeries.SeriesName; } }
        public bool IsVisible
        {
            get { return RenderSeries.IsVisible; }
            set
            {
                RenderSeries.IsVisible = value;
                RaisePropertiesChanged("IsVisible");
            }
        }
        public bool IsDataVisible
        {
            get { return false; }
            set
            {
                RaisePropertiesChanged("IsDataVisible");
            }
        }
        public System.Windows.Media.Color Stroke
        {
            get { return (System.Windows.Media.Color)RenderSeries.Stroke; }
            set
            {
                RenderSeries.Stroke = (System.Windows.Media.Color)value;
                RaisePropertiesChanged("Stroke");
            }
        }
        public int StrokeThickness
        {
            get { return RenderSeries.StrokeThickness; }
            set
            {
                RenderSeries.StrokeThickness = value;
                RaisePropertiesChanged("StrokeThickness");
            }
        }
        public XyDataSeries<DateTime, double> DataSeries
        {
            get { return RenderSeries.DataSeries as XyDataSeries<DateTime, double>; }
        }        
    }
}
