using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SciChart.Data.Model;
using SciChart.Charting.Model.DataSeries;
using DaesungEntCleanOven.ViewModel;

namespace DaesungEntCleanOven.View
{
    /// <summary>
    /// RealtimeTrendGraphPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RealtimeTrendGraphPanel : UserControl
    {
        public RealtimeTrendGraphPanel()
        {
            InitializeComponent();

            var First = DateTime.Now;
            var Last = First.Add(TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));

            xAxis1.VisibleRange = new DateRange(First, Last);
            xAxis1.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 10));
            xAxis1.MinorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 50));
            xAxis1.LabelProvider = new CustomDateTimeLabelProvider();
            yaxis11.LabelProvider = new ColoredMultiRowLabelProvider(yaxis11, yaxis12, yaxis13, yaxis14, yaxis15);

            xAxis2.VisibleRange = new DateRange(First, Last);
            xAxis2.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 10));
            xAxis2.MinorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 50));
            xAxis2.LabelProvider = new CustomDateTimeLabelProvider();
            yaxis21.LabelProvider = new ColoredMultiRowLabelProvider(yaxis21, yaxis22, yaxis23, yaxis24);
        }
        void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is Equipment.CleanOven CleanOven)
            {
                if (CleanOven.TrendSeriesGrp1 != null && CleanOven.TrendSeriesGrp1.Count > 0)
                {
                    var Series = CleanOven.TrendSeriesGrp1[0].DataSeries as IXyDataSeries<DateTime, double>;
                    Series.DataSeriesChanged += (s, arg) => {
                        switch (arg.DataSeriesAction)
                        {
                            case DataSeriesAction.Append:
                            case DataSeriesAction.Remove:
                            case DataSeriesAction.Clear:
                                {
                                    if (Series.XValues.Count > G.REALTIME_TREND_CAPACITY * 3600)
                                    {
                                        var Now = Series.XValues[Series.Count - 1];
                                        int Cnt = Series.XValues.Count(o => o < Now - TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));
                                        if (Cnt > 0)
                                        {
                                            scrollbar1.IsEnabled = true;
                                            scrollbar1.Minimum = Cnt * -1;
                                            scrollbar1.Maximum = 0;
                                        }
                                        else
                                        {
                                            scrollbar1.IsEnabled = false;
                                        }
                                    }                                    
                                }
                                break;
                        }
                    };                   
                }
                if (CleanOven.TrendSeriesGrp2 != null && CleanOven.TrendSeriesGrp2.Count > 0)
                {
                    var Series = CleanOven.TrendSeriesGrp2[0].DataSeries as IXyDataSeries<DateTime, double>;
                    Series.DataSeriesChanged += (s, arg) => {
                        switch (arg.DataSeriesAction)
                        {
                            case DataSeriesAction.Append:
                            case DataSeriesAction.Remove:
                            case DataSeriesAction.Clear:
                                {
                                    if (Series.XValues.Count > G.REALTIME_TREND_CAPACITY * 3600)
                                    {
                                        var Now = Series.XValues[Series.Count - 1];
                                        int Cnt = Series.XValues.Count(o => o < Now - TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));
                                        if (Cnt > 0)
                                        {
                                            scrollbar2.IsEnabled = true;
                                            scrollbar2.Minimum = Cnt * -1;
                                            scrollbar2.Maximum = 0;
                                        }
                                        else
                                        {
                                            scrollbar2.IsEnabled = false;
                                        }
                                    }                                    
                                }
                                break;
                        }
                    };
                }
            }
        }
        void Scrollbar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int Offset = (int)e.NewValue;
            G.REALTIME_TREND_ON_SEARCH = Offset != 0;
            var Series = chart1.RenderableSeries[0].DataSeries as IXyDataSeries<DateTime, double>;
            var Now = Series.XValues[Series.Count - 1];
            int Cnt = Series.XValues.Count(o => o < Now - TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));
            if (Cnt > 0)
            {
                if (Series.XValues.Count > 2)
                {
                    var Intv = Series.XValues[1] - Series.XValues[0];
                    var Last = DateTime.Now - new TimeSpan(Intv.Ticks * Math.Abs(Offset));
                    var First = Last - TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY);
                    chart1.XAxis.VisibleRange = new DateRange(First, Last);
                }
            }
        }
        void Scrollbar2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int Offset = (int)e.NewValue;
            G.REALTIME_TREND_ON_SEARCH = Offset != 0;
            var Series = chart2.RenderableSeries[0].DataSeries as IXyDataSeries<DateTime, double>;
            var Now = Series.XValues[Series.Count - 1];
            int Cnt = Series.XValues.Count(o => o < Now - TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));
            if (Cnt > 0)
            {
                if (Series.XValues.Count > 2)
                {
                    var Intv = Series.XValues[1] - Series.XValues[0];
                    var Last = DateTime.Now - new TimeSpan(Intv.Ticks * Math.Abs(Offset));
                    var First = Last - TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY);
                    chart2.XAxis.VisibleRange = new DateRange(First, Last);
                }
            }             
        }
    }
}
