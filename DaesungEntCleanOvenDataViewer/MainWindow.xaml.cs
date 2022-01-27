using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;

using DevExpress.Xpf.Core;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {
        protected readonly string BINARY_FILE_HEADER = "DAESUNG-ENT.N2-CLEANOVEN.V1";
        protected readonly int SCREEN_PTR_MAX = 1000;
        protected DateTime[] xSourceValues; // TOTAL X VALUES.
        protected Dictionary<int, double[]> ySourceValues = new Dictionary<int, double[]>();    // TOTAL Y VALUES.
        protected int[] Scales = new int[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 1, 1, 1 };
        protected int OutputDataOffset = 0;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            var F = DateTime.Now;
            var L = new DateTime(F.Ticks + (TimeSpan.FromSeconds(10).Ticks * SCREEN_PTR_MAX));
            xAxis.VisibleRange = new DateRange(F, L);
            xAxis.MajorDelta = new TimeSpan((long)((L.Ticks - F.Ticks) / 20));
            xAxis.MinorDelta = new TimeSpan((long)((L.Ticks - F.Ticks) / 100));
            xAxis.LabelProvider = new CustomDateTimeLabelProvider();
            yaxis1.LabelProvider = new ColoredMultiRowLabelProvider(yaxis1, yaxis2, yaxis3, yaxis4, yaxis5, yaxis6, yaxis7, yaxis8);

            string[] sNames = new string[] {
                "온도_PV(℃)",
                "온도_SV(℃)",
                "온도_MV(%)",
                "차압챔버(mmH2O)",
                "차압필터(mmH2O)",
                "모터챔버(Hz)",
                "쿨링챔버(Hz)",
                "MFC(ℓ/min)",
                "내부온도-1(℃)",
                "내부온도-2(℃)",
                "내부온도-3(℃)",
                "내부온도-4(℃)",
                "챔버_OT(℃)",
                "히터_OT(℃)",
                "O2_온도(℃)",
                "O2_EMF(mV)",
                "O2_ppm(ppm)"
            };
            string[] yIds = new string[] {
                "y1",
                "y1",
                "y2",
                "y3",
                "y3",
                "y4",
                "y4",
                "y5",
                "y1",
                "y1",
                "y1",
                "y1",
                "y1",
                "y1",
                "y6",
                "y7",
                "y8"
            };
            Color[] sColor = new System.Windows.Media.Color[] {
                Colors.Red,         //
                Colors.Blue,
                Colors.Lime,        //
                Colors.Aqua,        //
                Colors.White,
                Colors.Yellow,      //
                Colors.LightBlue,
                Colors.Magenta,     //
                Colors.Purple,
                Colors.Brown,
                Colors.Beige,
                Colors.Teal,
                Colors.SaddleBrown,
                Colors.Plum,
                Colors.RoyalBlue,   //
                Colors.Green,       //
                Colors.Pink,        //
                };
            this.LineRenderableSeries = new List<FastLineRenderableSeriesEx>();
            for (int i = 0; i < sNames.Length; i++)
            {
                var ln = new FastLineRenderableSeriesEx();
                var dataSeries = new XyDataSeries<DateTime, double> { SeriesName = sNames[i], AcceptsUnsortedData = true };
                ln.DataSeries = dataSeries;
                ln.YAxisId = yIds[i];
                ln.Stroke = sColor[i];
                ln.IsVisible = true;
                ln.AntiAliasing = false;
                ln.StrokeThickness = 1;
                chartTrend.RenderableSeries.Add(ln);
                LineRenderableSeries.Add(ln);
//                 ln.PropertyChanged += (s, e) => {
//                     if (e.PropertyName == "LineColor")
//                     {
//                         if (s is FastLineRenderableSeriesEx Sr)
//                         {
//                             switch (Sr.DataSeries.SeriesName)
//                             {
//                                 case "온도_PV(℃)":
//                                     yaxis1.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "온도_MV(%)":
//                                     yaxis2.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "차압챔버(mmH2O)":
//                                     yaxis3.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "모터챔버(Hz)":
//                                     yaxis4.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "MFC(ℓ/min)":
//                                     yaxis5.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "O2_온도(℃)":
//                                     yaxis6.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "O2_EMF(mV)":
//                                     yaxis7.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                                 case "O2_ppm(ppm)":
//                                     yaxis8.TickTextBrush = new SolidColorBrush(Sr.LineColor);
//                                     break;
//                             }
//                         }
 //                   }
 //               };
            }

            this.TrendChartParameter = new ChartParameter(this.chartTrend);
            this.TrendChartParameter.PropertyChanged += (s, e) => {
                if (xSourceValues != null)
                {
                    if (e.PropertyName == "ZoomRatio")
                    {
                        if (OutputDataOffset < xSourceValues.Length)
                        {
                            var First = xSourceValues[OutputDataOffset];
                            var Last = new DateTime(First.Ticks + (long)(((xSourceValues[1].Ticks - xSourceValues[0].Ticks) * SCREEN_PTR_MAX) * TrendChartParameter.ZoomRatio));
                            xAxis.VisibleRange = new DateRange(First, Last);
                        }
                    }
                }
            };
        }
        public List<FastLineRenderableSeriesEx> LineRenderableSeries { get; protected set; }
        public ChartParameter TrendChartParameter { get; protected set; }
        protected void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog Ofd = new OpenFileDialog())
            {
                Ofd.InitialDirectory = @"D:\DAESUNG-ENT\CLEAN_OVEN\LOG\GRAPH\";
                Ofd.Filter = "Data files (*.dat)|*.dat|All files (*.*)|*.*";
                Ofd.FilterIndex = 1;
                Ofd.RestoreDirectory = true;
                if (Ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.tboxFileName.Text = Ofd.FileName;
                    OpenFile(Ofd.FileName);
                }
            }
        }
        protected async void OpenFile(string path)
        {
            View.ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", "로그 데이터 로드 중...");
           
            var T = Task<bool>.Run(() => {

                try
                {
                    var Fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var tmpBuf = new byte[Fs.Length];
                    int ptrCnt = (int)((Fs.Length - 50) / 42);
                    if (ptrCnt < 2)
                        throw new Exception("Ptr.Count is Invalid");

                    using (var Br = new System.IO.BinaryReader(Fs))
                    {
                        if (Br.Read(tmpBuf, 0, tmpBuf.Length) != tmpBuf.Length)
                            throw new Exception("Fail to Read a Binary Data to Internal Buffer");
                    }

                    int Index = Array.FindIndex(tmpBuf, 0, b => b == 0x0);
                    if (Encoding.ASCII.GetString(tmpBuf, 0, Index) != BINARY_FILE_HEADER)
                        throw new Exception("BinaryFile Header is Invalid");

                    xSourceValues = new DateTime[ptrCnt];
                    for (int i = 0; i < 17; i++)
                        ySourceValues[i] = new double[ptrCnt];

                    for (int i = 0; i < ptrCnt; i++)
                    {
                        long Ticks = BitConverter.ToInt64(tmpBuf, 50 + (i * 42));
                        xSourceValues[i] = new DateTime(Ticks, DateTimeKind.Local);
                        for (int j = 0; j < LineRenderableSeries.Count; j++)
                        {
                            var value = BitConverter.ToInt16(tmpBuf, 50 + (i * 42) + (8 + j * 2)) / Scales[j];
                            ySourceValues[j][i] = value;
                        }
                    }
                    return true;
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exceptio is Occured while to Open Chart Binary Log File : " + ex.Message);
                }
                return false;
            });

            var Res = await T;
            View.ProgressWindow.CloseWindow();

            if (Res)
            {
                OutputDataOffset = 0;
                var First = xSourceValues[0];
                var Last = new DateTime(First.Ticks + (long)(((xSourceValues[1].Ticks - First.Ticks) * SCREEN_PTR_MAX)));
                xAxis.VisibleRange = new DateRange(First, Last);
                xAxis.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 20));
                scrollbar.IsEnabled = (xSourceValues.Length > SCREEN_PTR_MAX);
                if (xSourceValues.Length > SCREEN_PTR_MAX)
                    scrollbar.Maximum = xSourceValues.Length;
                TrendChartParameter.ZoomRatio = 1.0;

                long majorTicks = xAxis.MajorDelta.Ticks;
                for (int i = 0; i < LineRenderableSeries.Count; i++)
                {
                    long sumTicks = 0;
                    var logMetaData = new List<LogPointMetaData>();
                    for (int j = 0; j < ySourceValues[i].Length; j++)
                    {
                        if (j == 0)
                        {
                            logMetaData.Add(new LogPointMetaData(ySourceValues[i][j], LineRenderableSeries[i].Stroke));
                        }
                        else
                        {
                            sumTicks += (xSourceValues[j].Ticks - xSourceValues[j - 1].Ticks);
                            if (sumTicks >= majorTicks)
                            {
                                logMetaData.Add(new LogPointMetaData(ySourceValues[i][j], LineRenderableSeries[i].Stroke));
                                sumTicks = 0;
                            }
                            else
                            {
                                logMetaData.Add(null);
                            }
                        }
                    }
                    var dataSeries = LineRenderableSeries[i].DataSeries as XyDataSeries<DateTime, double>;
                    dataSeries.Clear();
                    dataSeries.Append(xSourceValues, ySourceValues[i], logMetaData);
                }
            }

            var qDlg = new View.Question(Res ? "차트 데이터 로드 완료" : "차트 데이터 로드 실패.");
            qDlg.ShowDialog();
        }
        protected void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int Offset = (int)e.NewValue;
            if (Offset != OutputDataOffset)
            {
                if (Offset < xSourceValues.Length)
                {
                    var First = xSourceValues[Offset];
                    var Last = new DateTime(First.Ticks + (long)(((xSourceValues[1].Ticks - xSourceValues[0].Ticks) * SCREEN_PTR_MAX) * TrendChartParameter.ZoomRatio));
                    xAxis.VisibleRange = new DateRange(First, Last);
                }
                OutputDataOffset = Offset;
            }
        }       
        protected void OpenChartSetupButton_Click(object sender, RoutedEventArgs e)
        {
            var Dlg = new View.ParameterSetupDlg();
            Dlg.DataContext = this;
            Dlg.ShowDialog();
        }
    }

    public class FastLineRenderableSeriesEx : FastLineRenderableSeries, INotifyPropertyChanged
    {
        public bool VisiblePtrValue
        {
            get { return base.PointMarker != null; }
            set
            {
                if (value)
                    base.PointMarker = new LogPointMarker();
                else
                    base.PointMarker = null;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VisiblePtrValue"));
            }
        }
        public Color LineColor
        {
            get { return base.Stroke; }
            set
            {
                base.Stroke = value;
                // DataSeries.MetaData를 IList<LogPointMetaData> 직접 캐스팅 시 null.
                foreach (var md in base.DataSeries.Metadata)
                {
                    if (md != null && md is LogPointMetaData lpd)
                        lpd.Color = value;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LineColor"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
