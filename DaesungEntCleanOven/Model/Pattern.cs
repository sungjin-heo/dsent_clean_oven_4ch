using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using DaesungEntCleanOven4.ViewModel;

namespace DaesungEntCleanOven4.Model
{
    internal class Pattern : ICloneable
    {
        public static Model.Pattern LoadFrom(string path)
        {
            XmlDocument Xd = new XmlDocument();
            Xd.Load(path);

            XmlNode root = Xd.SelectSingleNode("Pattern");
            var pattern = new Pattern(false)
            {
                No = int.Parse(root.Attributes["No"].Value),
                Name = root.Attributes["Name"].Value,
                StartConditionUsage = int.Parse(root.Attributes["StartConditionUsage"].Value),
                DifferencePressChamberSv = double.Parse(root.Attributes["DifferencePressChamberSv"].Value),
                DifferencePressChamberInitMv = double.Parse(root.Attributes["DifferencePressChamberInitMv"].Value),
                DifferencePressChamberManualCtlTime = double.Parse(root.Attributes["DifferencePressChamberManualCtlTime"].Value),
                MotorChamberSv = double.Parse(root.Attributes["MotorChamberSv"].Value),
                O2TargetValue = double.Parse(root.Attributes["O2TargetValue"].Value),
                O2TargetReachCheckupTime = double.Parse(root.Attributes["O2TargetReachCheckupTime"].Value),
                MFCSv = double.Parse(root.Attributes["MFCSv"].Value),
                ExhaustValveOpenSetting = int.Parse(root.Attributes["ExhaustValveOpenSetting"].Value),
                WaitTemperatureAfterClose = double.Parse(root.Attributes["WaitTemperatureAfterClose"].Value)
            };

            XmlNodeList pNodes = Xd.SelectNodes("Pattern/Segments/Segment");
            foreach (XmlElement pNode in pNodes)
            {
                var Seg = new Segment(pattern);
                if (pNode.HasAttributes)
                {
                    Seg.No = int.Parse(pNode.Attributes["No"].Value);
                    Seg.Temperature = double.Parse(pNode.Attributes["Temperature"].Value);
                    Seg.DifferencePressureChamber = double.Parse(pNode.Attributes["DifferencePressureChamber"].Value);
                    Seg.MotorChamber = double.Parse(pNode.Attributes["MotorChamber"].Value);
                    Seg.MotorCooling = double.Parse(pNode.Attributes["MotorCooling"].Value);
                    Seg.MFC = double.Parse(pNode.Attributes["MFC"].Value);
                    Seg.Duration = double.Parse(pNode.Attributes["Duration"].Value);
                    Seg.DeviationAlarmUsed = int.Parse(pNode.Attributes["DeviationAlarmUsed"].Value);
                    Seg.N2InputValve = int.Parse(pNode.Attributes["N2InputValve"].Value);
                    Seg.CoerciveExhaustValve = int.Parse(pNode.Attributes["CoerciveExhaustValve"].Value);
                    Seg.OxygenAnalyzerValve = int.Parse(pNode.Attributes["OxygenAnalyzerValve"].Value);
                    Seg.CoolingWaterValve = int.Parse(pNode.Attributes["CoolingWaterValve"].Value);
                    Seg.HeaterCutoffUsed = int.Parse(pNode.Attributes["HeaterCutoffUsed"].Value);
                    Seg.CoolingFanUsed = int.Parse(pNode.Attributes["CoolingFanUsed"].Value);
                    Seg.CoolingChamberUsed = int.Parse(pNode.Attributes["CoolingChamberUsed"].Value);
                }
                pattern.Segments.Add(Seg);
            }
            return pattern;
        }
        public static void SaveTo(Model.Pattern Pattern, string path)
        {
            XDocument xDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null));
            XElement xRoot = new XElement("Pattern");
            xRoot.SetAttributeValue("No", Pattern.No.ToString());
            xRoot.SetAttributeValue("Name", Pattern.Name);
            xRoot.SetAttributeValue("StartConditionUsage", Pattern.StartConditionUsage);
            xRoot.SetAttributeValue("DifferencePressChamberSv", Pattern.DifferencePressChamberSv);
            xRoot.SetAttributeValue("DifferencePressChamberInitMv", Pattern.DifferencePressChamberInitMv);
            xRoot.SetAttributeValue("DifferencePressChamberManualCtlTime", Pattern.DifferencePressChamberManualCtlTime);
            xRoot.SetAttributeValue("MotorChamberSv", Pattern.MotorChamberSv);
            xRoot.SetAttributeValue("O2TargetValue", Pattern.O2TargetValue);
            xRoot.SetAttributeValue("O2TargetReachCheckupTime", Pattern.O2TargetReachCheckupTime);
            xRoot.SetAttributeValue("MFCSv", Pattern.MFCSv);
            xRoot.SetAttributeValue("ExhaustValveOpenSetting", Pattern.ExhaustValveOpenSetting);
            xRoot.SetAttributeValue("WaitTemperatureAfterClose", Pattern.WaitTemperatureAfterClose);

            XElement xSegs = new XElement("Segments");
            xRoot.Add(xSegs);
            foreach (var Seg in Pattern.Segments)
            {
                XElement xSeg = new XElement("Segment");
                xSeg.SetAttributeValue("No", Seg.No.ToString());
                xSeg.SetAttributeValue("Temperature", Seg.Temperature.ToString());
                xSeg.SetAttributeValue("DifferencePressureChamber", Seg.DifferencePressureChamber.ToString());
                xSeg.SetAttributeValue("MotorChamber", Seg.MotorChamber.ToString());
                xSeg.SetAttributeValue("MotorCooling", Seg.MotorCooling.ToString());
                xSeg.SetAttributeValue("MFC", Seg.MFC.ToString());
                xSeg.SetAttributeValue("Duration", Seg.Duration.ToString("F2"));
                xSeg.SetAttributeValue("DeviationAlarmUsed", Seg.DeviationAlarmUsed);
                xSeg.SetAttributeValue("N2InputValve", Seg.N2InputValve);
                xSeg.SetAttributeValue("CoerciveExhaustValve", Seg.CoerciveExhaustValve);
                xSeg.SetAttributeValue("OxygenAnalyzerValve", Seg.OxygenAnalyzerValve);
                xSeg.SetAttributeValue("CoolingWaterValve", Seg.CoolingWaterValve);
                xSeg.SetAttributeValue("HeaterCutoffUsed", Seg.HeaterCutoffUsed);
                xSeg.SetAttributeValue("CoolingFanUsed", Seg.CoolingFanUsed);
                xSeg.SetAttributeValue("CoolingChamberUsed", Seg.CoolingChamberUsed);
                xSegs.Add(xSeg);
            }
            xDoc.Add(xRoot);
            xDoc.Save(path);
        }
        public readonly int MAX_SEGMENT_CNT = 30;

        public Pattern(bool autoSegCreate = true)
        {
            this.Segments = new List<Segment>();
            if (autoSegCreate)
            {
                for (int i = 0; i < MAX_SEGMENT_CNT; i++)
                    Segments.Add(new Segment(this) { No = i + 1 });
            }
            this.Name = "NO-NAME";
            this.WaitTemperatureAfterClose = 50.0;
        }
        public List<Segment> Segments { get; private set; }
        public int SegmentCount => Segments.Count;
        public int No { get; set; }
        public string Name { get; set; }
        public string Description => string.Format("패턴 번호 : {0}, 패턴 명 : {1}", No, Name);
        public int StartConditionUsage { get; set; }
        public double DifferencePressChamberSv { get; set; }
        public double DifferencePressChamberInitMv { get; set; }
        public double DifferencePressChamberManualCtlTime { get; set; }
        public double MotorChamberSv { get; set; }
        public double O2TargetValue { get; set; }
        public double O2TargetReachCheckupTime { get; set; }
        public double MFCSv { get; set; }
        public int ExhaustValveOpenSetting { get; set; }
        public double WaitTemperatureAfterClose { get; set; }
        public object Clone()
        {
            Pattern pattern = new Pattern(false) 
            {
                No = this.No,
                Name = this.Name
            };
            pattern.StartConditionUsage = StartConditionUsage;
            pattern.DifferencePressChamberSv = DifferencePressChamberSv;
            pattern.DifferencePressChamberInitMv = DifferencePressChamberInitMv;
            pattern.DifferencePressChamberManualCtlTime = DifferencePressChamberManualCtlTime;
            pattern.MotorChamberSv = MotorChamberSv;
            pattern.O2TargetValue = O2TargetValue;
            pattern.O2TargetReachCheckupTime = O2TargetReachCheckupTime;
            pattern.MFCSv = MFCSv;
            pattern.ExhaustValveOpenSetting = ExhaustValveOpenSetting;
            pattern.WaitTemperatureAfterClose = WaitTemperatureAfterClose;
            foreach (Segment Seg in Segments)
                pattern.Segments.Add((Segment)Seg.Clone());
            return pattern;
        }
        public override bool Equals(object obj)
        {
            if (obj is Pattern y)
            {
                if (ReferenceEquals(this, y))
                    return true;

                if (Name == y.Name
                    && StartConditionUsage == y.StartConditionUsage
                    && DifferencePressChamberSv == y.DifferencePressChamberSv
                    && DifferencePressChamberInitMv == y.DifferencePressChamberInitMv
                    && DifferencePressChamberManualCtlTime == y.DifferencePressChamberManualCtlTime
                    && MotorChamberSv == y.MotorChamberSv
                    && O2TargetValue == y.O2TargetValue
                    && O2TargetReachCheckupTime == y.O2TargetReachCheckupTime
                    && MFCSv == y.MFCSv
                    && ExhaustValveOpenSetting == y.ExhaustValveOpenSetting
                    && WaitTemperatureAfterClose == y.WaitTemperatureAfterClose)
                    return Segments.SequenceEqual(((Pattern)obj).Segments, new PattenCompare());
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public void Reset()
        {
            Name = "NO-NAME";
            StartConditionUsage = 0;
            DifferencePressChamberSv = 0;
            DifferencePressChamberInitMv = 0;
            DifferencePressChamberManualCtlTime = 0;
            MotorChamberSv = 0;
            O2TargetValue = 0;
            O2TargetReachCheckupTime = 0;
            MFCSv = 0;
            ExhaustValveOpenSetting = 0;
            WaitTemperatureAfterClose = 0;
            Segments.ForEach(Seg => Seg.Reset());
        }
        public void DebugPrint()
        {
            Debug.WriteLine(Description);
            Segments.ForEach(Seg => Seg.DebugPrint());
        }
    }
}
