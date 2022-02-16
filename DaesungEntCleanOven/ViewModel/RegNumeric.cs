using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciChart.Data.Model;
using System.Globalization;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.ChartSeries;
using DaesungEntCleanOven4.Equipment;

namespace DaesungEntCleanOven4.ViewModel
{
    internal class RegNumeric : ItemViewModel<double>
    {
        protected readonly string DecimalFormat;
        protected readonly IDataSeries<DateTime, double> DataSeries;
        public static bool IsContainsUnit { get; set; }
        public RegNumeric(Device.Yokogawa.PLC.YokogawaSequenceEth plc, string Name, string Unit, int Scale, string ReadAddr, string WriteAddr, string Description = "")
            : base(Name, Unit, Scale)
        {
            this.plc = plc;
            this.LatencyQueryItems = (plc as CleanOven).Channel.LatencyQueryItems;
            this.CleanOvenLatencyQueryInterval = (plc as CleanOven).Channel.CleanOvenLatencyQueryInterval;
            this.ReadAddress = ReadAddr;
            this.WriteAddress = WriteAddr;
            this.Description = Description;
            this.DecimalFormat = string.Format("F{0}", (uint)Math.Log10((uint)Model.Scale));
            this.DataSeries = new XyDataSeries<DateTime, double> { SeriesName = Model.Name, AcceptsUnsortedData = true };
            this.DataSeries.FifoCapacity = (int)((G.REALTIME_TREND_CAPACITY + G.REALTIME_TREND_HISTORY) * 3600);
            this.SeriesViewModel = new LineRenderableSeriesViewModel { DataSeries = this.DataSeries, IsVisible = true, AntiAliasing = false };
            this.SeriesViewModel.Stroke = Util.Colors.GetColor();
            this.SeriesViewModel.StrokeThickness = 1;
        }
        public override double Value
        {
            get { return base.Value; }
            set
            {
                if (IsReadOnly)
                {
                    base.Value = value;
                }
                else
                {
                    try
                    {
                        string Response = string.Empty;
                        short Tmp = (short)(value * base.Scale);
                        Response = (string)plc.WriteWord(WriteAddress, Tmp);
                        if (!Response.Contains("OK"))
                            throw new Exception(string.Format("Return Error for WriteWord(),  WriteAddress : {0}, WriteValue : {1}", WriteAddress, Tmp));

                        if (LatencyQueryItems.Contains(this.Name))
                            System.Threading.Thread.Sleep(CleanOvenLatencyQueryInterval);

                        if (!string.IsNullOrEmpty(ReadAddress))
                        {
                            Response = (string)plc.ReadWord(ReadAddress, 1);
                            if (!Response.Contains("OK"))
                                throw new Exception(string.Format("Return Error for ReadWord(),  ReadAddress : {0}, Count : 1", WriteAddress));
                            base.Value = short.Parse(Response.Substring(4, 4), NumberStyles.HexNumber);
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Logger.Dispatch("e", ex.Message);
                    }
                }
            }
        }        
        public override double ScaledValue { get { return base.Value / (int)base.Scale; } }
        public string ReadAddress { get; protected set; }
        public string WriteAddress { get; protected set; }
        public bool IsReadOnly { get { return string.IsNullOrEmpty(WriteAddress) && !string.IsNullOrEmpty(ReadAddress); } }
        public IRenderableSeriesViewModel SeriesViewModel { get; protected set; }
        public IDataSeries<DateTime, double> Series { get { return DataSeries; } }
        public override string FormattedValue
        {
            get
            {
                string Tmp = string.Format("{0:" + DecimalFormat + "}", this.ScaledValue);
                if (IsContainsUnit)
                    Tmp += this.Unit;
                return Tmp;
            }
            set
            {
                if (!IsReadOnly && double.TryParse(value, out double Tmp))
                    this.Value = Tmp;
            }
        }
        public void Reset()
        {
            DataSeries.Clear();
        }
        public void UpdateOnly(double value)
        {
            base.Value = value;
        }

        // 4채널 확장을 위해 어쩔수 없이....ㅠㅠ
        private Device.Yokogawa.PLC.YokogawaSequenceEth plc { get; set; }
        private int CleanOvenLatencyQueryInterval { get; set; }
        private string[] LatencyQueryItems { get; set; }
    }
}
