using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven4.Model;

namespace DaesungEntCleanOven4.ViewModel
{
    class SegmentViewModel : DevExpress.Mvvm.BindableBase, ICloneable
    {
        public static readonly string UNKNOWN = "-----";

        public SegmentViewModel(Segment Model)
        {
            this.Model = Model ?? throw new ArgumentNullException("Model.Segment");
        }
        public int PatternNo { get { return Model.Pattern.No; } }
        public Segment Model { get; protected set; }
        public int No
        {
            get { return Model.No; }
        }
        public string Name { get { return Model.Name; } }
        public double Temperature
        {
            get { return Model.Temperature; }
            set
            {
                Model.Temperature = value;
                RaisePropertiesChanged("Temperature");
            }
        }
        public double DifferencePressureChamber
        {
            get { return Model.DifferencePressureChamber; }
            set
            {
                Model.DifferencePressureChamber = value;
                RaisePropertiesChanged("DifferencePressureChamber");
            }
        }
        public double MotorChamber
        {
            get { return Model.MotorChamber; }
            set
            {
                Model.MotorChamber = value;
                RaisePropertiesChanged("MotorChamber");
            }
        }
        public double MotorCooling
        {
            get { return Model.MotorCooling; }
            set
            {
                Model.MotorCooling = value;
                RaisePropertiesChanged("MotorCooling");
            }
        }
        public double MFC
        {
            get { return Model.MFC; }
            set
            {
                Model.MFC = value;
                RaisePropertiesChanged("MFC");
            }
        }
        public double Duration
        {
            get { return Model.Duration; }
            set
            {
                Model.Duration = value;
                RaisePropertiesChanged("Duration", "FormattedDurationTime");
            }
        }
        public int TimeSignalValue { get { return Model.TimeSignalValue; } }
        public int DeviationAlarmUsed
        {
            get { return Model.DeviationAlarmUsed; }
            set
            {
                Model.DeviationAlarmUsed = value;
                RaisePropertiesChanged("DeviationAlarmUsed", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int N2InputValve
        {
            get { return Model.N2InputValve; }
            set
            {
                Model.N2InputValve = value;
                RaisePropertiesChanged("N2InputValve", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int CoerciveExhaustValve
        {
            get { return Model.CoerciveExhaustValve; }
            set
            {
                Model.CoerciveExhaustValve = value;
                RaisePropertiesChanged("CoerciveExhaustValve", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int OxygenAnalyzerValve
        {
            get { return Model.OxygenAnalyzerValve; }
            set
            {
                Model.OxygenAnalyzerValve = value;
                RaisePropertiesChanged("OxygenAnalyzerValve", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int CoolingWaterValve
        {
            get { return Model.CoolingWaterValve; }
            set
            {
                Model.CoolingWaterValve = value;
                RaisePropertiesChanged("CoolingWaterValve", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int HeaterCutoffUsed
        {
            get { return Model.HeaterCutoffUsed; }
            set
            {
                Model.HeaterCutoffUsed = value;
                RaisePropertiesChanged("HeaterCutoffUsed", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int CoolingFanUsed
        {
            get { return Model.CoolingFanUsed; }
            set
            {
                Model.CoolingFanUsed = value;
                RaisePropertiesChanged("CoolingFanUsed", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int CoolingChamberUsed
        {
            get { return Model.CoolingChamberUsed; }
            set
            {
                Model.CoolingChamberUsed = value;
                RaisePropertiesChanged("CoolingChamberUsed", "FormattedTimeSignal", "FormattedTimeSignalGrp1", "FormattedTimeSignalGrp2");
            }
        }
        public int ConvertedDurationTime
        {
            get
            {
                // 단위 : 분.
                string Tmp = string.Format("{0:F2}", this.Duration);
                string[] Token = Tmp.Split('.');
                return (int.Parse(Token[0]) * 60) + int.Parse(Token[1]);
            }
        }
        public string FormattedDurationTime
        {
            get
            {
                string Tmp = string.Format("{0:F2}", this.Duration);
                var Token = Tmp.Split('.');
                if (Token.Length != 2) return UNKNOWN;
                return string.Format("{0}h {1:d2}m", Token[0], Token[1]);
            }
        }
        public string FormattedTimeSignal { get { return FormattedTimeSignalGrp1 + " " + FormattedTimeSignalGrp2; } }
        public string FormattedTimeSignalGrp1
        {
            get
            {
                return string.Format("{0}{1}{2}{3}{4}", DeviationAlarmUsed, N2InputValve, CoerciveExhaustValve, OxygenAnalyzerValve, CoolingWaterValve);
            }
        }
        public string FormattedTimeSignalGrp2
        {
            get
            {
                return string.Format("{0}{1}{2}00", HeaterCutoffUsed, CoolingFanUsed, CoolingChamberUsed);
            }
        }
        public void Reset()
        {
            Model.Reset();
            var Properties = typeof(SegmentViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        public object Clone()
        {
            return new SegmentViewModel((Segment)Model.Clone());
        }
    }
}
