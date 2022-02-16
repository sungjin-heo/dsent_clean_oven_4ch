using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace DaesungEntCleanOven4.Model
{
    internal class Segment : ICloneable
    {
        public Segment(Model.Pattern pattern)
        {
            this.Pattern = pattern ?? throw new ArgumentNullException("Pattern");
            this.DeviationAlarmUsed = 0;
            this.N2InputValve = 1;
            this.CoerciveExhaustValve = 0;
            this.OxygenAnalyzerValve = 0;
            this.CoolingWaterValve = 1;
            this.HeaterCutoffUsed = 0;
            this.CoolingFanUsed = 0;
            this.CoolingChamberUsed = 0;
        }
        public Model.Pattern Pattern { get; private set; }
        public int No { get; set; }
        public string Name => string.Format("SEG #{0:d2}", No);
        public double Temperature { get; set; }
        public double DifferencePressureChamber { get; set; }
        public double MotorChamber { get; set; }
        public double MotorCooling { get; set; }
        public double MFC { get; set; }
        public double Duration { get; set; }
        public int TimeSignalValue
        {
            get
            {
                int value = 0;
                if (DeviationAlarmUsed == 1)
                    value |= 0x01;
                if (N2InputValve == 1)
                    value |= 0x02;
                if (CoerciveExhaustValve == 1)
                    value |= 0x04;
                if (OxygenAnalyzerValve == 1)
                    value |= 0x08;
                if (CoolingWaterValve == 1)
                    value |= 0x10;
                if (HeaterCutoffUsed == 1)
                    value |= 0x20;
                if (CoolingFanUsed == 1)
                    value |= 0x40;
                if (CoolingChamberUsed == 1)
                    value |= 0x80;
                return value;
            }
        }
        public int DeviationAlarmUsed { get; set; }
        public int N2InputValve { get; set; }
        public int CoerciveExhaustValve { get; set; }
        public int OxygenAnalyzerValve { get; set; }
        public int CoolingWaterValve { get; set; }
        public int HeaterCutoffUsed { get; set; }
        public int CoolingFanUsed { get; set; }
        public int CoolingChamberUsed { get; set; }
        public void Reset()
        {
            this.Temperature = 0;
            this.DifferencePressureChamber = 0;
            this.MotorChamber = 0;
            this.MotorCooling = 0;
            this.MFC = 0;
            this.Duration = 0;
            this.DeviationAlarmUsed = 0;
            this.N2InputValve = 0;
            this.CoerciveExhaustValve = 0;
            this.OxygenAnalyzerValve = 0;
            this.CoolingWaterValve = 0;
            this.HeaterCutoffUsed = 0;
            this.CoolingFanUsed = 0;
            this.CoolingChamberUsed = 0;
        }
        public object Clone()
        {
            return new Segment(this.Pattern)
            {
                No = this.No,
                Temperature = this.Temperature,
                DifferencePressureChamber = this.DifferencePressureChamber,
                MotorChamber = this.MotorChamber,
                MotorCooling = this.MotorCooling,
                MFC = this.MFC,
                Duration = this.Duration,
                DeviationAlarmUsed = this.DeviationAlarmUsed,
                N2InputValve = this.N2InputValve,
                CoerciveExhaustValve = this.CoerciveExhaustValve,
                OxygenAnalyzerValve = this.OxygenAnalyzerValve,
                CoolingWaterValve = this.CoolingWaterValve,
                HeaterCutoffUsed = this.HeaterCutoffUsed,
                CoolingFanUsed = this.CoolingFanUsed,
                CoolingChamberUsed = this.CoolingChamberUsed
            };
        }
        public override bool Equals(object obj)
        {
            if (obj is Segment y)
            {
                if (ReferenceEquals(this, y)) return true;
                if (No == y.No
                    && Name == y.Name
                    && Temperature == y.Temperature
                    && DifferencePressureChamber == y.DifferencePressureChamber
                    && MotorChamber == y.MotorChamber
                    && MotorCooling == y.MotorCooling
                    && MFC == y.MFC
                    && Duration == y.Duration
                    && TimeSignalValue == y.TimeSignalValue)
                    return true;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
        public void DebugPrint()
        {
            Debug.WriteLine("------------------------------------------------------");
            var Properties = typeof(Segment).GetProperties();
            Properties.ToList().ForEach(p => Debug.WriteLine(string.Format("{0} : {1}", p.Name, p.GetValue(this))));
        }
    }
}
