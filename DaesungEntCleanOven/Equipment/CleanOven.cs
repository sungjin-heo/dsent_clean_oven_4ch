using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Diagnostics;

using DevExpress.Mvvm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SciChart.Data.Model;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.ChartSeries;
using DaesungEntCleanOven4.Properties;
using DaesungEntCleanOven4.View;
using DaesungEntCleanOven4.ViewModel;
using Device.Yokogawa.PLC;
using Util;
using Comm;

namespace DaesungEntCleanOven4.Equipment
{
    // D99 번지 
    // 0 : STOP
    // 1 : 수동운전
    // 5 : 패턴운전 (개시조건사용)
    // 7 : 패턴운전 (개시조건미사용)

    class CleanOven : Device.Yokogawa.PLC.YokogawaSequenceEth
    {
        private object SyncKey = new object();
        private System.IO.StreamWriter CsvWriter;
        private System.IO.BinaryWriter BinaryDataWriter;
        private System.Threading.Timer LogWriterTimer;
        private int __SelectedZoneParameterGrpIndex;
        private int __SelectedZoneParameterIndex;
        private bool __ManualCtrl;
        private double __AutoTuningCache;

        public CleanOven(string Ip, int Port, ChannelViewModel Ch)
           : base(Ip, Port, null)
        {
            this.Channel = Ch ?? throw new ArgumentNullException("CleanOven's ChannelViewModel");
            this.RunCtlCommand = new DevExpress.Mvvm.DelegateCommand(RunCtl, CanRunCtl);
            this.ChangePatternCommand = new DevExpress.Mvvm.DelegateCommand(ChangePattern, CanChangePattern);
            this.HoldCommand = new DevExpress.Mvvm.DelegateCommand(Hold, CanHold);
            this.AdvancePatternCommand = new DevExpress.Mvvm.DelegateCommand(AdvancePattern, CanAdvancePattern);
            this.ClearAlarmHistoryCommand = new DevExpress.Mvvm.DelegateCommand(ClearAlarmHistory, CanClearAlarmHistory);
            this.AutoTuneCommand = new DevExpress.Mvvm.DelegateCommand(AutoTune, CanAutoTune);
            this.TurnOnPowerCommand = new DevExpress.Mvvm.DelegateCommand(TurnOnPower, CanTurnOnPower);
            this.TurnOffPowerCommand = new DevExpress.Mvvm.DelegateCommand(TurnOffPower, CanTurnOffPower);
            this.OpenDoorCommand = new DevExpress.Mvvm.DelegateCommand(OpenDoor, CanOpenDoor);
            this.CloseDoorCommand = new DevExpress.Mvvm.DelegateCommand(CloseDoor, CanCloseDoor);

            if (!ConstructRegister(Ch.No))
            {
                throw new Exception("CleanOven Configuration Error.");
            }
            this.SelectedZoneParameterGrpIndex = 0;
            this.SelectedZoneParameterIndex = 0;
        }
        public DevExpress.Mvvm.DelegateCommand RunCtlCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand ChangePatternCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand HoldCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand AdvancePatternCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand ClearAlarmHistoryCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand AutoTuneCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand TurnOnPowerCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand TurnOffPowerCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenDoorCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand CloseDoorCommand { get; private set; }

        public bool IsInitialized { get; private set; }
        public ChannelViewModel Channel { get; private set; }
        public int ChannelNumber => Channel.No;

        // ORIGINAL DATA SOURCES...
        public List<RegNumeric> NumericValues { get; private set; }
        public List<RegRelay> Relays { get; private set; }
        public List<ZoneParameters> Parameters { get; private set; }
        public Pattern PatternConf { get; private set; }
        public List<RegRelay> IoX { get; private set; }
        public List<RegRelay> IoY { get; private set; }
        public List<RegNumeric> AlertSettings { get; private set; }

        // DATA REFERENCES FOR ORIGINAL DATA SOURCES..
        public List<RegNumeric> CsvLoggingNumerics { get; private set; }
        public List<RegNumeric> BinaryLoggingNumerics { get; private set; }
        public List<RegNumeric> TrendSeriesGrp1Numerics { get; private set; }
        public List<RegNumeric> TrendSeriesGrp2Numerics { get; private set; }
        public List<IRenderableSeriesViewModel> TrendSeriesGrp1 { get; private set; }
        public List<IRenderableSeriesViewModel> TrendSeriesGrp2 { get; private set; }
        public List<ZoneParameters> SelectedZoneParameterGrp { get; private set; }
        public ZoneParameters SelectedZoneParameter { get; private set; }
        public List<Alarm> Alarms { get; private set; }
        public ObservableCollection<AlarmDescript> AlarmHistory { get; private set; }
        public System.Windows.Window AlarmDlg;

        public bool IsInitRunning => Relays[3].Value;
        public bool IsRunning { get; private set; }
        public bool IsStopping => Relays[5].Value;
        public bool IsStop => !Relays[0].Value;
        public bool IsFixRun => Relays[7].Value;
        public bool IsHold => Relays[1].Value;
        public bool IsAutoTune => __AutoTuningCache == 1;
        public bool IsDoorOpenAvailable => Relays[24].Value;
        public bool IsDoorOpenUnavailable => !Relays[24].Value;
        public bool IsDoorClosed { get; private set; }      //=> IoX[24].Value;
        public bool IsDoorOpen { get; private set; }        // => IoX[25].Value;
        public bool IsAlarmState => Relays[9].Value;
        public bool IsReady => IsStop && !IsAlarmState;
        public bool IsExternalAborted { get; private set; }     // 외부에서 정지한 경우.
        public int CurrentSequenceNo => (int)NumericValues[5].Value;
        public int TotalSequenceCount => (int)NumericValues[6].Value;

        public string TrendDataSaveName { get; private set; }
        public int UsePatternNo => Channel.PatternForRun.No;
        public string FormattedCleanOvenStatus
        {
            get
            {
                if (Relays[1].Value)
                    return string.Format("CH.{0} - TEMPERATURE : PROG. HOLD", ChannelNumber);
                else if (Relays[3].Value)
                    return string.Format("CH.{0} - TEMPERATURE : PROG. PRE-RUN", ChannelNumber);
                else if (Relays[4].Value)
                    return string.Format("CH.{0} - TEMPERATUR.E : PROG. RUN", ChannelNumber);
                else if (Relays[5].Value)
                    return string.Format("CH.{0} - TEMPERATURE : PROG. CLOSING", ChannelNumber);
                else if (Relays[6].Value)
                    return string.Format("CH.{0} - TEMPERATURE : PROG. STOP", ChannelNumber);
                else if (Relays[7].Value)
                    return string.Format("CH.{0} - TEMPERATURE : FIX. RUN", ChannelNumber);
                else if (NumericValues[39].Value == 1)
                    return string.Format("CH.{0} - TEMPERATURE : AUTO-TUNE", ChannelNumber);
                else if (NumericValues[42].Value == 0)
                    return string.Format("CH.{0} - TEMPERATURE : STOP", ChannelNumber);

                return string.Format("CH.{0} - TEMPERATURE : UNKNOWN", ChannelNumber);
            }
        }
        public string FormattedPatten => string.Format("{0} : {1}", NumericValues[4].Value, Channel.PatternForRun.Name);
        public string FormattedSegment => string.Format("{0} / {1}", NumericValues[5].Value, NumericValues[6].Value);
        public string FormattedSegmentTime
        {
            get
            {
                // 엑셀 문서상에 할당된 값의 위치가 바뀐것 같음.
                // 문서 : D8014(세그시간), D8012(남은시간)
                // 실제 : D8012(세그시간), D8014(남은시간)
                int Hour, Minute;

                // 남은시간
                Hour = Math.DivRem((int)NumericValues[8].ScaledValue, 60, out Minute);
                string remTime = string.Format("{0}h {1:d2}m", Hour, Minute);

                // 세그먼트 설정 시간.
                Hour = Math.DivRem((int)NumericValues[7].ScaledValue, 60, out Minute);
                string segTime = string.Format("{0}h {1:d2}m", Hour, Minute);

                return string.Format("{0} / {1}", remTime, segTime);
            }
        }
        public string FormattedTotalRunTime
        {
            get
            {
                int Hour = Math.DivRem((int)NumericValues[9].Value, 3600, out int Tmp);
                int Minute = Math.DivRem(Tmp, 60, out int Sec);
                return string.Format("{0}h {1}m {2}s ", Hour, Minute, Sec);
            }
        }
        public string FormattedCloseTime
        {
            get
            {
                if (!IsRunning)
                    return "--.--.-- --:--";
                
                int CurrSeg = (int)NumericValues[5].Value;
                int SegTimes = 0;
                foreach (SegmentViewModel Seg in Channel.PatternForRun.Segments)
                {
                    if (Seg.No == CurrSeg)
                    {
                        SegTimes += (int)NumericValues[8].ScaledValue;
                    }
                    else if (Seg.No > CurrSeg)
                    {
                        SegTimes += Seg.ConvertedDurationTime;
                    }
                }
                DateTime Now = DateTime.Now;
                DateTime endTime = Now.AddMinutes(SegTimes);
                return string.Format("{0:D4}.{1:D2}.{2:D2} {3:D2}:{4:D2}", endTime.Year, endTime.Month, endTime.Day, endTime.Hour, endTime.Minute);
            }
        }        

        public double TotalSegmentTime => Channel.PatternForRun.Segments.Sum(o => o.ConvertedDurationTime);  // 단위 : 분.
        public double TotalSegmentElapsedTime => NumericValues[9].Value / 60;
        public double CurrentSegmentElapsedTime => CurrentSegmentDuration - NumericValues[8].ScaledValue;
        public double CurrentSegmentDuration => NumericValues[7].ScaledValue;

        public int SelectedZoneParameterGrpIndex
        {
            get { return __SelectedZoneParameterGrpIndex; }
            set
            {
                if (value >= 0 && value < 3)
                {
                    this.SelectedZoneParameterGrp = Parameters.GetRange(value * 4, 4);
                    __SelectedZoneParameterGrpIndex = value;
                }
                RaisePropertiesChanged("SelectedZoneParameterGrpIndex", "SelectedZoneParameterGrp");
            }
        }
        public int SelectedZoneParameterIndex
        {
            get { return __SelectedZoneParameterIndex; }
            set
            {
                // 파라미터 중, MFC, 차압챔버, 모터챔버, 모터쿨링 아이템들은 UI상에서 선택 불가하도록 변경되어서... -> 원복.
                if (value >= 0 && value <= 6)
                {
                    this.SelectedZoneParameter = Parameters[value];
                    __SelectedZoneParameterIndex = value;
                }
                else if (value >= 7 && value <= 8)
                {
                    this.SelectedZoneParameter = Parameters[value + 1];
                    __SelectedZoneParameterIndex = value;
                }
// 
//                 if (value >= 0 && value <= 11)
//                 {
//                     this.SelectedZoneParameter = Parameters[value];
//                     __SelectedZoneParameterIndex = value;
//                 }   
                RaisePropertiesChanged("SelectedZoneParameterIndex", "SelectedZoneParameter");
            }
        }
        public bool ManualCtrl
        {
            get { return __ManualCtrl; }
            set
            {
                if (value == __ManualCtrl) 
                    return;

                if (value)
                {
                    Question Q = new Question("수동운전을 시작하시겠습니까?");
                    if (!(bool)Q.ShowDialog())
                    {
                        return;
                    }

                    if (!IoY[7].Value)
                    {
                        Q = new Question("OVEN POWER MC가 OFF 상태입니다.");
                        _ = Q.ShowDialog();
                        return;
                    }
                    if (Monitor.TryEnter(SyncKey, 3000))
                    {
                        try
                        {
                            NumericValues[42].Value = 1;
                            __ManualCtrl = value;
                        }
                        finally
                        {
                            Monitor.Exit(SyncKey);
                        }
                    }
                }
                else
                {
                    Question Q = new Question("수동운전을 정지하시겠습니까?");
                    if (!(bool)Q.ShowDialog())
                    {
                        return;
                    }

                    if (Monitor.TryEnter(SyncKey, 3000))
                    {
                        try
                        {
                            // N2 BYPASS VALVE : B접점
                            NumericValues[42].Value = 0;
                            Thread.Sleep(500);
                            List<RegRelay> relays = new List<RegRelay> { Relays[14], Relays[15], Relays[16], Relays[17], Relays[18], Relays[19], Relays[20], Relays[21], Relays[124] };
                            string[] w_addr = (from rel in relays select rel.WriteAddress).ToArray();
                            _ = WriteRandomBit(w_addr, new bool[] { false, true, false, false, false, false, false, false, false });
                            Thread.Sleep(500);

                            string[] r_addr = (from rel in relays select rel.ReadAddress).ToArray();
                            string Response = (string)ReadRandomBit(r_addr);
                            if (!Response.Contains("OK") || (Response.Length - 4 != r_addr.Length))
                                throw new Exception("Fail to Update Manual Control Relay Values");
                            for (int i = 0; i < relays.Count; i++)
                                relays[i].UpdateOnly(Response[4 + i] == '1');

                            RaisePropertiesChanged(
                                "N2InputValve",
                                "N2BypassValve",
                                "ForceExhaustValve",
                                "O2AnalyzerValve",
                                "CoolingWaterValve",
                                "MainHeaterValve",
                                "CoolingFanValve",
                                "MotorChamberValve",
                                "EvaValve"
                                );
                        }
                        finally
                        {
                            Monitor.Exit(SyncKey);
                        }
                    }
                    __ManualCtrl = value;
                }
                
                RaisePropertiesChanged("ManualCtrl");
            }
        }
        public bool N2InputValve
        {
            get { return Relays[14].Value; }
            set
            {
                string Message = string.Format("[수동운전] N2 투입 밸브를 {0} 하겠습니까?", value ? "ON" : "OFF");
                Question Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[14].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public bool N2BypassValve
        {
            get { return Relays[15].Value; }
            set
            {
                string Message = string.Format("[수동운전] N2 바이패스 밸브를 {0} 하겠습니까?", value ? "ON" : "OFF");
                Question Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[15].Value = !value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public bool ForceExhaustValve
        {
            get { return Relays[16].Value; }
            set
            {
                string Message = string.Format("[수동운전] 강제배기밸브 잠금을 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[16].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public bool O2AnalyzerValve
        {
            get { return Relays[17].Value; }
            set
            {
                string Message = string.Format("[수동운전] 산소분석기 밸브를 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[17].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public bool CoolingWaterValve
        {
            get { return Relays[18].Value; }
            set
            {
                string Message = string.Format("[수동운전] 냉각수 밸브를 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[18].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public bool MainHeaterValve
        {
            get { return Relays[19].Value; }
            set
            {
                string Message = string.Format("[수동운전] 메인 히터를 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[19].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public bool CoolingFanValve
        {
            get { return Relays[20].Value; }
            set
            {
                string Message = string.Format("[수동운전] 냉각팬을 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[20].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }                
            }
        }
        public bool MotorChamberValve
        {
            get { return Relays[21].Value; }
            set
            {
                string Message = string.Format("[수동운전] 모터 챔버를 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[21].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }                
            }
        }
        public bool EvaValve
        {
            get { return Relays[124].Value; }
            set
            {
                string Message = string.Format("[수동운전] EVA 밸브를 {0} 하겠습니까?", value ? "ON" : "OFF");
                var Dlg = new Question(Message);
                if (!(bool)Dlg.ShowDialog())
                    return;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        Relays[124].Value = value;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
        }
        public string TemperatureDifferenceAlertNotUsedTitle
        {
            get
            {
                // TS #1로 고정.
                return "온도 편차 경보 #1 미사용";
                //int nSeg = (int)NumericValues[5].Value;
                //return string.Format("온도 편차 경보 #{0} 미사용", nSeg);
            }
        }
        public bool TemperatureDifferenceAlertNotUsed
        {
            get
            {
                if (!IsRunning)
                    return false;

                int nSeg = (int)NumericValues[5].Value;
                if (nSeg > 0)
                    return Channel.PatternForRun.Segments[nSeg - 1].DeviationAlarmUsed == 1;

                return false;
            }
        }
        public double AnalyzerO2Temperature { get; private set; }
        public double AnalyzerO2Emf { get; private set; }
        public double AnalyzerO2Ppm { get; private set; }

        public event EventHandler PatternReloadRequested;
        public event EventHandler AlarmOccured;
        public event EventHandler DoorOpenCompleted;
        public event EventHandler DoorCloseCompleted;
        public event EventHandler ProcessStarted;
        public event EventHandler ProcessCompleted;
        public event EventHandler ProcessAborted;

        private T[] GetChunk<T>(T[] Src, int Offset, int Cnt, ref int Index)
        {
            T[] Tmp = null;
            if (Offset + Cnt < Src.Length)
            {
                Tmp = new T[Cnt];
                Array.Copy(Src, Offset, Tmp, 0, Tmp.Length);
                Index = Offset + Cnt;
            }
            else
            {
                Tmp = new T[Src.Length - Offset];
                Array.Copy(Src, Offset, Tmp, 0, Tmp.Length);
                Index = Offset + Tmp.Length;
            }
            return Tmp;
        }
        private bool ConstructRegister(int Ch)
        {
            try
            {
                this.NumericValues = new List<RegNumeric>();
                this.Relays = new List<RegRelay>();
                this.Parameters = new List<ZoneParameters>();
                this.PatternConf = new Pattern();
                this.IoX = new List<RegRelay>();
                this.IoY = new List<RegRelay>();
                this.AlertSettings = new List<RegNumeric>();
                this.Alarms = new List<Alarm>();
                this.CsvLoggingNumerics = new List<RegNumeric>();
                this.BinaryLoggingNumerics = new List<RegNumeric>();
                this.AlarmHistory = new ObservableCollection<AlarmDescript>();
                this.AlarmHistory.CollectionChanged += AlarmHistory_CollectionChanged;

                JToken jsonData;

                // 1. NUMERIC DATA.
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\numerics.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                JToken[] Items = jsonData["numerics"].ToArray();
                foreach (JToken Item in Items)
                {
                    RegNumeric regNum = new RegNumeric(this, (string)Item["name"], (string)Item["unit"], (int)Item["scale"],
                        (string)Item["r.addr"], (string)Item["w.addr"]);
                    NumericValues.Add(regNum);
                }

                // 2. RELAY.
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\relay.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                Items = jsonData["relays"].ToArray();
                foreach (JToken Item in Items)
                {
                    string rAddr = (string)Item["r.addr"];
                    if (!string.IsNullOrEmpty(rAddr) && rAddr.Contains('.'))
                    {
                        string[] Token = rAddr.Split('.');
                        if (Token.Length == 2)
                        {
                            RegRelay regRel = new RegRelay(this, (string)Item["name"], Token[0], (string)Item["w.addr"]);
                            regRel.Offset = int.Parse(Token[1]);
                            Relays.Add(regRel);
                        }
                    }
                    else
                    {
                        RegRelay regRel = new RegRelay(this, (string)Item["name"], (string)Item["r.addr"], (string)Item["w.addr"]);
                        Relays.Add(regRel);
                    }
                }

                // 3. ZONE PARAMETERS.
                string[] zoneNames = new string[] { "temperature", "chamber_ot", "heater_ot", "different_pressure_chamber",
                    "mfc", "different_pressure_filter", "motor_chamber", "motor_cooling", "inner_temp_1", "inner_temp_2", "inner_temp_3", "inner_temp_4" };
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\parameter.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                foreach (string zoneName in zoneNames)
                {
                    Items = jsonData[zoneName].ToArray();
                    ZoneParameters zoneParameter = new ZoneParameters() { Name = zoneName };
                    foreach (JToken Item in Items)
                    {
                        RegNumeric regNum = new RegNumeric(this, (string)Item["name"], "", (int)Item["scale"], (string)Item["r.addr"], (string)Item["w.addr"]);
                        zoneParameter.Items.Add(regNum);
                    }
                    Parameters.Add(zoneParameter);
                }

                // 4. PATTERN DATA.
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\pattern.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                Items = jsonData["conditions"].ToArray();
                foreach (JToken Item in Items)
                {
                    RegNumeric regNum = new RegNumeric(this, (string)Item["name"], "", (int)Item["scale"],
                       (string)Item["r.addr"], (string)Item["w.addr"]);
                    PatternConf.Conditions.Add(regNum);
                }
                for (int i = 0; i < 30; i++)
                {
                    Segment Seg = new Segment(i);
                    string segName = string.Format("SEG{0:D2}", i + 1);
                    Items = jsonData["segments"][segName].ToArray();
                    foreach (JToken Item in Items)
                    {
                        RegNumeric regNum = new RegNumeric(this, (string)Item["name"], "", (int)Item["scale"],
                            (string)Item["r.addr"], (string)Item["w.addr"]);
                        Seg.Add(regNum);
                    }
                    PatternConf.Add(Seg);
                }

                // 5. IO-X.
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\io_x.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                Items = jsonData["io_x"].ToArray();
                foreach (JToken Item in Items)
                {
                    RegRelay regRel = new RegRelay(this, (string)Item["name"], (string)Item["r.addr"], (int)Item["offset"]) { Description = (string)Item["descript"] };
                    IoX.Add(regRel);
                }

                // 5. IO-Y.
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\io_y.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                Items = jsonData["io_y"].ToArray();
                foreach (JToken Item in Items)
                {
                    RegRelay regRel = new RegRelay(this, (string)Item["name"], (string)Item["r.addr"], (int)Item["offset"]) { Description = (string)Item["descript"] };
                    IoY.Add(regRel);
                }

                // 6. ALERT SETUP PARAMETERS.
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\alert_param_setup.json", Ch)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        jsonData = JToken.ReadFrom(Jr);
                    }
                }
                Items = jsonData["alert_param_setup"].ToArray();
                foreach (JToken Item in Items)
                {
                    RegNumeric regNum = new RegNumeric(this, (string)Item["name"], "", (int)Item["scale"], (string)Item["r.addr"], (string)Item["w.addr"]);
                    AlertSettings.Add(regNum);
                }

                // COPY LOG ITEM REFERENCE...    
                int[] Index = new int[] { 0, 1, 3, 11, 14, 15, /*18,*/ 21, 23, 24, /*25, 26,*/ 27, 28, 29, 30, 31, 12, 13 };
                for (int i = 0; i < Index.Length; i++)
                    this.CsvLoggingNumerics.Add(this.NumericValues[Index[i]]);

                Index = new int[] { 0, 1, 3, 11, 14, 15, /*18,*/ 21, 23, 24, /*25, 26,*/ 27, 28, 29, 30, 31 };
                for (int i = 0; i < Index.Length; i++)
                    this.BinaryLoggingNumerics.Add(this.NumericValues[Index[i]]);

                for (int i = 25; i < 74; i++)
                {
                    Alarm alarm = new Alarm(Relays[i].Name, Relays[i], Relays[i + 49]);
                    alarm.AlarmOccured += (s, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.AlarmHistory.Add((s as Alarm).GetDescript());
                            if (AlarmDlg == null)
                            {
                                AlarmDlg = new View.AlarmRealtimeDlg() { DataContext = this };
                                AlarmDlg.Title = string.Format("채널.{0} - 경보 상태 창", Channel.No);
                                AlarmDlg.Closed += (snd, arg) => { AlarmDlg = null; };
                                AlarmDlg.Show();
                            }
                            AlarmOccured?.Invoke(this, EventArgs.Empty);
                        });
                    };
                    alarm.AlarmCleared += (s, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.AlarmHistory.Add((s as Alarm).GetDescript());
                        });
                    };
                    Alarms.Add(alarm);
                }

                // COPY REALTIME TREND ITEM REF.
                Index = new int[] { 0, 1, 3, 11, 14, 15, /*18,*/ 21 };
                string[] yIds = new string[] { "y1", "y1", "y2", "y3", "y3", "y4", /*"y4",*/ "y5" };
                System.Windows.Media.Color[] sColor = new Color[] {
                    System.Windows.Media.Colors.Red, System.Windows.Media.Colors.Blue, System.Windows.Media.Colors.Orange, System.Windows.Media.Colors.Lime,
                    System.Windows.Media.Colors.White, System.Windows.Media.Colors.Aqua, /*System.Windows.Media.Colors.Yellow, */System.Windows.Media.Colors.Magenta
                };
                TrendSeriesGrp1 = new List<IRenderableSeriesViewModel>();
                TrendSeriesGrp1Numerics = new List<RegNumeric>();
                for (int i = 0; i < Index.Length; i++)
                {
                    RegNumeric regNum = NumericValues[Index[i]];
                    TrendSeriesGrp1Numerics.Add(regNum);
                    IRenderableSeriesViewModel Ln = regNum.SeriesViewModel;
                    Ln.YAxisId = yIds[i];
                    Ln.Stroke = sColor[i];
                    TrendSeriesGrp1.Add(Ln);
                }

                Index = new int[] { 23, 24, /*25, 26,*/ 27, 28, 29, 30, 31 };
                yIds = new string[] { "y1", "y1", /*"y1", "y1",*/ "y1", "y1", "y2", "y3", "y4" };
                sColor = new Color[] {
                    System.Windows.Media.Colors.Red, System.Windows.Media.Colors.Blue,/* System.Windows.Media.Colors.Orange, System.Windows.Media.Colors.Lime,*/
                    System.Windows.Media.Colors.White, System.Windows.Media.Colors.Aqua, System.Windows.Media.Colors.Yellow, System.Windows.Media.Colors.Magenta, System.Windows.Media.Colors.Green
                };
                TrendSeriesGrp2 = new List<IRenderableSeriesViewModel>();
                TrendSeriesGrp2Numerics = new List<RegNumeric>();
                for (int i = 0; i < Index.Length; i++)
                {
                    RegNumeric regNum = NumericValues[Index[i]];
                    TrendSeriesGrp2Numerics.Add(regNum);
                    IRenderableSeriesViewModel Ln = regNum.SeriesViewModel;
                    Ln.YAxisId = yIds[i];
                    Ln.Stroke = sColor[i];
                    TrendSeriesGrp2.Add(Ln);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to \"OnInit()\" : " + ex.Message);
            }
            return false;
        }
        private void AlarmHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (this)
            {
                if (e.NewItems != null)
                {
                    foreach (var Item in e.NewItems)
                    {
                        AlarmDescript Alarm = Item as AlarmDescript;
                        if (Alarm != null)
                        {
                            DateTime Now = DateTime.Now;
                            string Name = string.Format("ALARM_LOG - {0:D4}{1:D2}{2:D2}.csv", Now.Year, Now.Month, Now.Day);
                            string Path = System.IO.Path.Combine(Channel.AlarmStorageDir, Name);
                            using (var Sw = new System.IO.StreamWriter(Path, true))
                            {
                                Sw.WriteLine(Alarm.ToString());
                            }
                        }
                    }
                }
            }
        }
        protected virtual void OnMonitorDataUpdated()
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                // 운전 상태 업데이트.
                bool curr_run_state = Relays[0].Value;
                if (curr_run_state)
                {
                    if (!IsRunning)
                    {
                        ProcessStarted?.Invoke(this, EventArgs.Empty);
                        IsExternalAborted = false;
                    }
                }
                else
                {
                    if (IsRunning)
                    {
                        CloseLOG();
                        if (IsExternalAborted)      // 사용자에 의한 정지.
                        {
                            ProcessAborted?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            ProcessCompleted?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
                IsRunning = Relays[0].Value;  // IsRunning은 운전상태에 대한 캐쉬.

                // OVEN POWER MC가 OFF 상태에선 수동 제어 불가.
                if (!IoY[7].Value)
                {
                    if (ManualCtrl)
                    {
                        ManualCtrl = false;
                    }
                }

                // AUTOTUING이 완료되면, TEMPERATURE P,I,D 갱신.
                double curr_autotune_state = NumericValues[39].Value;
                if (__AutoTuningCache != curr_autotune_state)
                {
                    if (__AutoTuningCache == 1 && curr_autotune_state == 0)
                    {
                        // AUTOTUNE DONE. UPDATE TEMPERATURE ZONE P,I,D
                        if (Monitor.TryEnter(SyncKey, 3000))
                        {
                            try
                            {
                                List<RegNumeric> list = Parameters[0].Items.GetRange(15, 3);
                                string[] r_addr = (from reg in list select reg.ReadAddress).ToArray();
                                string Response = (string)ReadRandomWord(r_addr);
                                if (!Response.Contains("OK") || (Response.Length - 4 != r_addr.Length * 4))
                                    throw new Exception("Fail to Update Temperature P,I,D Values");
                                for (int i = 0; i < r_addr.Length; i++)
                                    list[i].UpdateOnly(short.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Dispatch("e", ex.Message);
                            }
                            finally
                            {
                                Monitor.Exit(SyncKey);
                            }
                        }                        
                    }
                    __AutoTuningCache = curr_autotune_state;
                }

                try
                {
                    IXyDataSeries<DateTime, double> Series1 = TrendSeriesGrp1[0].DataSeries as IXyDataSeries<DateTime, double>;
                    IXyDataSeries<DateTime, double> Series2 = TrendSeriesGrp2[0].DataSeries as IXyDataSeries<DateTime, double>;
                    if (Series1.HasValues)
                    {
                        DateTime Now = DateTime.Now;
                        TimeSpan ElapsedTime = Now - Series1.XValues[0];
                        if (ElapsedTime.TotalMinutes > G.REALTIME_TREND_CAPACITY * 60.0 && !G.REALTIME_TREND_ON_SEARCH)
                        {
                            // CHANGE VISIBLE RANGE.
                            DateTime First = Now.Subtract(TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));
                            DateTime Last = Now;
                            SciChart.Charting.Visuals.Axes.DateTimeAxis xAxis = Series1.ParentSurface.XAxis as SciChart.Charting.Visuals.Axes.DateTimeAxis;
                            xAxis.VisibleRange = new DateRange(First, Last);
                            xAxis = Series2.ParentSurface.XAxis as SciChart.Charting.Visuals.Axes.DateTimeAxis;
                            xAxis.VisibleRange = new DateRange(First, Last);
                        }
                    }

                    DateTime Time = DateTime.Now;
                    for (int i = 0; i < TrendSeriesGrp1Numerics.Count; i++)
                    {
                        IXyDataSeries<DateTime, double> dataSeries = (TrendSeriesGrp1[i].DataSeries as IXyDataSeries<DateTime, double>);
                        dataSeries.Append(Time, TrendSeriesGrp1Numerics[i].ScaledValue);
                    }
                    for (int i = 0; i < TrendSeriesGrp2Numerics.Count; i++)
                    {
                        IXyDataSeries<DateTime, double> dataSeries = (TrendSeriesGrp2[i].DataSeries as IXyDataSeries<DateTime, double>);
                        dataSeries.Append(Time, TrendSeriesGrp2Numerics[i].ScaledValue);
                    }
                }
                catch (Exception ex)
                {
                    __Tracer.TraceError("Exception is Occured in OnMonitorDataUpdated() : " + ex.Message);
                }

                // 도어 클로즈 이벤트.
                if (IoX[24].Value)      // 현재 도어 Close 상태.
                {
                    if (!IsDoorClosed)
                    {
                        DoorCloseCompleted?.Invoke(this, EventArgs.Empty);
                    }
                }
                IsDoorClosed = IoX[24].Value;

                // 도어 오픈 이벤트
                if (IoX[25].Value)      // 현재 도어 Open 상태.
                {
                    if (!IsDoorOpen)
                    {
                        DoorOpenCompleted?.Invoke(this, EventArgs.Empty);
                    }
                }
                IsDoorOpen = IoX[25].Value;

                RaisePropertiesChanged(
                    "FormattedCleanOvenStatus", 
                    "FormattedPatten",
                    "FormattedSegment",
                    "FormattedSegmentTime",
                    "FormattedTotalRunTime", 
                    "FormattedCloseTime",
                    "IsRunning", 
                    "IsHold", 
                    "IsFixRun",
                    "IsAutoTune", 
                    "TrendDataSaveName", 
                    "UsePatternNo", 
                    "TemperatureDifferenceAlertNotUsed"
                    );
            });
        }

        private CancellationTokenSource CancelTokenSource;
        private System.Threading.AutoResetEvent Waitor = new AutoResetEvent(false);
        public async void StartMonitor()
        {
            try
            {
                if (!IsConnected)
                    throw new Exception("YokogawaSequence Session Closed.");

                CancelTokenSource = new CancellationTokenSource();
                CancellationToken Token = CancelTokenSource.Token;
                await Task.Factory.StartNew(MonitorFunc, Token, TaskCreationOptions.LongRunning);
                CancelTokenSource.Dispose();
                CancelTokenSource = null;
                Waitor.Set();
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured whil to StartMonitor() : {0}", ex.Message);
            }
        }
        public void StopMonitor()
        {
            if (CancelTokenSource != null)
            {
                CancelTokenSource.Cancel();
                Waitor.WaitOne(1000);
            }
        }
        protected virtual void MonitorFunc(object State)
        {
            try
            {
                CancellationToken Token = (CancellationToken)State;
                while (!Token.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        // 소요시간 : 약 0.02 sec
                        UpdateNumericRo();
                        UpdateRelayRo();
                        UpdateIoX();
                        UpdateIoY();
                        OnMonitorDataUpdated();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Dispatch("i", "Exception is Occured while to Monitor CleanOvenChamber : " + ex.Message);
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("i", "CleanOven Monitor Aborted : " + ex.Message);
            }
        }
        private bool UpdateNumeric()
        {
            try
            {
                var Items = (from regNum in NumericValues
                             where !string.IsNullOrEmpty(regNum.ReadAddress)
                             select regNum).ToArray();

                var query = (from item in Items select item.ReadAddress).ToArray();

                int Offset = 0;
                int Cnt = 32;
                int Index = 0;
                int j = 0;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        do
                        {
                            var qryAddress = GetChunk(query, Offset, Cnt, ref Index);
                            string Response = (string)ReadRandomWord(qryAddress);
                            if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length * 4))
                                throw new Exception("Fail to Update Numeric Values");

                            for (int i = 0; i < qryAddress.Length; i++)
                                Items[j++].UpdateOnly(short.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));
                            Offset = Index;

                        } while (Offset < query.Length);

                        return true;
                    }
                    catch(Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateNumeric : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch(Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateNumeric : " + ex.Message);
            }
            return false;
        }
        private bool UpdateNumericRo()
        {
            try
            {
                var roNumerics = NumericValues.GetRange(0, 10);
                roNumerics.AddRange(NumericValues.GetRange(11, 21));
                roNumerics.Add(NumericValues[39]);
                roNumerics.Add(NumericValues[43]);
                var roAddress = (from regNum in roNumerics select regNum.ReadAddress).ToArray();

                int Offset = 0;
                int Cnt = 32;
                int Index = 0;
                int j = 0;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        do
                        {
                            var qryAddress = GetChunk(roAddress, Offset, Cnt, ref Index);
                            string Response = (string)ReadRandomWord(qryAddress);
                            if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length * 4))
                                throw new Exception("Fail to Update Readonly Numeric Values");

                            for (int i = 0; i < qryAddress.Length; i++)
                                roNumerics[j++].UpdateOnly(short.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));
                            Offset = Index;
                            Thread.Sleep(10);

                        } while (Offset < roAddress.Length);

                        return true;
                    }
                    catch(Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateNumericRO : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch (Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateNumericRO : " + ex.Message);
            }
            return false;
        }
        private bool UpdateRelay()
        {
            try
            {
                var Items = (from relay in Relays
                             where !string.IsNullOrEmpty(relay.ReadAddress)
                             select relay).ToArray();

                var query = (from item in Items select item.ReadAddress).ToArray();

                int Offset = 0;
                int Cnt = 32;
                int Index = 0;
                int j = 0;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        do
                        {
                            var qryAddress = GetChunk(query, Offset, Cnt, ref Index);
                            string Response = (string)ReadRandomBit(qryAddress);
                            if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length))
                                throw new Exception("Fail to Update Relay Values");

                            for (int i = 0; i < qryAddress.Length; i++)
                                Items[j++].UpdateOnly((Response[4 + i] == '1'));
                            Offset = Index;

                        } while (Offset < query.Length);

                        return true;
                    }
                    catch(Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateRelay : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch(Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateRelay : " + ex.Message);                
            }
            return false;
        }
        private bool UpdateRelayRo()
        {
            try
            {
                var roRelays = (from relay in Relays where relay.IsReadOnly select relay).ToArray();
                var roAddress = (from relay in roRelays select relay.ReadAddress).ToArray();

                int Offset = 0;
                int Cnt = 32;
                int Index = 0;
                int j = 0;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        do
                        {
                            var qryAddress = GetChunk(roAddress, Offset, Cnt, ref Index);
                            string Response = (string)ReadRandomBit(qryAddress);
                            if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length))
                                throw new Exception("Fail to Update Readonly Relay Values");

                            for (int i = 0; i < qryAddress.Length; i++)
                                roRelays[j++].UpdateOnly((Response[4 + i] == '1'));
                            Offset = Index;
                            Thread.Sleep(10);

                        } while (Offset < roAddress.Length);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateRelayRO : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch (Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateRelayRO : " + ex.Message);
            }
            return false;
        }
        private bool UpdateParameters()
        {
            try
            {
                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        foreach (ZoneParameters paramZone in Parameters)
                        {
                            string[] qryAddress = (from param in paramZone.Items
                                                   where !string.IsNullOrEmpty(param.ReadAddress)
                                                   select param.ReadAddress).ToArray();

                            string Response = (string)ReadRandomWord(qryAddress);
                            if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length * 4))
                                throw new Exception("Fail to Update Parameter Values");

                            for (int i = 0; i < qryAddress.Length; i++)
                                paramZone.Items[i].UpdateOnly(short.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateParameters : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch(Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateParameters : " + ex.Message);
            }
            return false;
        }
        private bool UpdateIoX()
        {
            try
            {
                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        string[] qryAddress = IoX.Select(o => o.ReadAddress).Distinct().ToArray();

                        string Response = (string)ReadRandomWord(qryAddress);
                        if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length * 4))
                            throw new Exception("Fail to Update IO-X Values");

                        Dictionary<string, ushort> Cache = new Dictionary<string, ushort>();
                        for (int i = 0; i < qryAddress.Length; i++)
                            Cache.Add(qryAddress[i], ushort.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));

                        foreach (RegRelay Io in IoX)
                            Io.Value = (Cache[Io.ReadAddress] & (0x01 << Io.Offset)) != 0;

                        Thread.Sleep(10);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateIoX : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch(Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateIoX : " + ex.Message);
            }
            return false;
        }
        private bool UpdateIoY()
        {
            try
            {
                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        string[] qryAddress = IoY.Select(o => o.ReadAddress).Distinct().ToArray();

                        string Response = (string)ReadRandomWord(qryAddress);
                        if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length * 4))
                            throw new Exception("Fail to Update IO-Y Values");

                        Dictionary<string, short> Cache = new Dictionary<string, short>();
                        for (int i = 0; i < qryAddress.Length; i++)
                            Cache.Add(qryAddress[i], short.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));

                        foreach (RegRelay Io in IoY)
                            Io.Value = (Cache[Io.ReadAddress] & (0x01 << Io.Offset)) != 0;

                        Thread.Sleep(10);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateIoY : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch (Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateIoY : " + ex.Message);
            }
            return false;
        }
        private bool UpdateAlertSetupParameter()
        {
            try
            {
                var Items = (from regNum in AlertSettings
                             where !string.IsNullOrEmpty(regNum.ReadAddress)
                             select regNum).ToArray();

                var query = (from item in Items select item.ReadAddress).ToArray();

                int Offset = 0;
                int Cnt = 32;
                int Index = 0;
                int j = 0;

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        do
                        {
                            var qryAddress = GetChunk(query, Offset, Cnt, ref Index);
                            string Response = (string)ReadRandomWord(qryAddress);
                            if (!Response.Contains("OK") || (Response.Length - 4 != qryAddress.Length * 4))
                                throw new Exception("Fail to Update Alert Setup Parameter Values");

                            for (int i = 0; i < qryAddress.Length; i++)
                                Items[j++].UpdateOnly(short.Parse(Response.Substring((i + 1) * 4, 4), NumberStyles.HexNumber));
                            Offset = Index;

                        } while (Offset < query.Length);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        __Tracer.TraceError("Exception is Occured while to UpdateAlertSetupParameter : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch (Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to UpdateAlertSetupParameter : " + ex.Message);
            }
            return false;
        }
        private bool UpdatePattern()
        {
            try
            {
                PatternViewModel model = Channel.PatternForRun;

                string[] condAddr = (from cond in PatternConf.Conditions
                                     select cond.WriteAddress).ToArray();

                int?[] condScale = (from cond in PatternConf.Conditions
                                    select cond.Scale).ToArray();

                short[] condValues = new short[condAddr.Length];
                condValues[0] = (short)(condScale[0] * model.StartConditionUsage);
                condValues[1] = (short)(condScale[1] * model.DifferencePressChamberSv);
                condValues[2] = (short)(condScale[2] * model.DifferencePressChamberInitMv);
                condValues[3] = (short)(condScale[3] * model.DifferencePressChamberManualCtlTime);
                condValues[4] = (short)(condScale[4] * model.MotorChamberSv);
                condValues[5] = (short)(condScale[5] * model.O2TargetValue);
                condValues[6] = (short)(condScale[6] * model.O2TargetReachCheckupTime);
                condValues[7] = (short)(condScale[7] * model.MFCSv);
                condValues[8] = (short)(condScale[8] * model.ExhaustValveOpenSetting);
                condValues[9] = (short)(condScale[9] * model.WaitTemperatureAfterClose);

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        NumericValues[4].Value = model.No;
                        _ = WriteRandomWord(condAddr, condValues);

                        string[] segAddr = new string[7];
                        short[] segValues = new short[7];
                        for (int i = 0; i < model.Segments.Count; i++)      // 30
                        {
                            var Seg = PatternConf[i];
                            for (int j = 0; j < segAddr.Length; j++)
                                segAddr[j] = Seg[j].WriteAddress;

                            segValues[0] = (short)(Seg[0].Scale * model.Segments[i].Temperature);
                            segValues[1] = (short)(Seg[1].Scale * model.Segments[i].DifferencePressureChamber);
                            segValues[2] = (short)(Seg[2].Scale * model.Segments[i].MotorChamber);
                            segValues[3] = (short)(Seg[3].Scale * model.Segments[i].MotorCooling);
                            segValues[4] = (short)(Seg[4].Scale * model.Segments[i].MFC);
                            segValues[5] = (short)(Seg[5].Scale * model.Segments[i].ConvertedDurationTime);
                            segValues[6] = (short)(Seg[6].Scale * model.Segments[i].TimeSignalValue);
                            _ = WriteRandomWord(segAddr, segValues);
                        }
                        Relays[123].Value = true;

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Dispatch("e", "Exception is Occured while to Transfer pattern configuration : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Transfer pattern configuration : " + ex.Message);
            }
            return false;
        }
        private void RunCtl()
        {
            if (IsRunning)
            {
                Question Q = new View.Question("프로그램 운전을 정지하겠습니까?");
                if (!(bool)Q.ShowDialog())
                {
                    return;
                }

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        this.NumericValues[42].Value = 0;
                        this.Relays[1].Value = false;       // 운전정지시 HOLD상태 해제.
                        this.IsExternalAborted = true;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
                CloseLOG();
            }
            else
            {
                string Message = string.Format("패턴 번호 : {0}\r\n패턴 명칭 : {1}\r\n프로그램 운전을 시작하겠습니까?", Channel.PatternForRun.No, Channel.PatternForRun.Name);
                View.Question Q = new View.Question(Message);
                if (!(bool)Q.ShowDialog())
                {
                    return;
                }

                //                 if (Channel.PatternForRun.No == Channel.PatternForEdit.No)
                //                 {
                //                     if (!Channel.PatternForRun.Equals(Channel.PatternForEdit))
                //                     {
                //                         Channel.PatternForRun.Load(Channel.PatternForEdit.Model);
                //                         TransferPatternNoMsg(Channel.PatternForRun);
                //                     }
                //                 }
                PatternReloadRequested?.Invoke(this, EventArgs.Empty);

                // CURRETN : STOP STATE, SO START...
                DateTime First = DateTime.Now;
                DateTime Last = First.Add(TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));

                SciChart.Charting.Visuals.Axes.DateTimeAxis xAxis = TrendSeriesGrp1[0].DataSeries.ParentSurface.XAxis as SciChart.Charting.Visuals.Axes.DateTimeAxis;
                xAxis.VisibleRange = new DateRange(First, Last);
                xAxis.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 10));
                xAxis.MinorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 50));
                foreach (IRenderableSeriesViewModel Series in TrendSeriesGrp1)
                    Series.DataSeries.Clear();

                xAxis = TrendSeriesGrp2[0].DataSeries.ParentSurface.XAxis as SciChart.Charting.Visuals.Axes.DateTimeAxis;
                xAxis.VisibleRange = new DateRange(First, Last);
                xAxis.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 10));
                xAxis.MinorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 50));
                foreach (IRenderableSeriesViewModel Series in TrendSeriesGrp2)
                    Series.DataSeries.Clear();

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        this.NumericValues[42].Value = (Channel.PatternForRun.StartConditionUsage == 1) ? 5 : 7;
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
                __ManualCtrl = false;
                RaisePropertiesChanged("ManualCtrl");
                StartLOG();
            }
        }
        private bool CanRunCtl()
        {
            return IsConnected/* && !IsHold;*/;
        }
        private void ChangePattern()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword;
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            View.PatternSelectDlg Dlg = new View.PatternSelectDlg() { DataContext = Channel };
            if ((bool)Dlg.ShowDialog())
            {
                if (Dlg.SelectedMetaData is Model.PatternMetadata metaData)
                    Channel.SelectRunningPattern(metaData.No);
            }
        }
        private bool CanChangePattern()
        {
            return IsConnected;/* && !IsRunning;*/
        }
        private void Hold()
        {
            bool State = this.IsHold;
            string Message = (State) ? "현재 세그먼트 HOLD를 해제 하겠습니까?" : "현재 세그먼트를 HOLD 하겠습니까?";
            View.Question Q = new View.Question(Message);
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[1].Value = !State;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanHold()
        {
            return IsConnected/* && IsRunning;*/;
        }
        private void AdvancePattern()
        {
            View.Question Q = new View.Question("다음 세그먼트로 진행 하겠습니까?");
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[2].Value = true;
                    this.Relays[1].Value = false;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanAdvancePattern()
        {
            return IsConnected /*&& IsRunning;*/;
        }
        private void ClearAlarmHistory()
        {
            var Q = new View.Question("알람 히스토리를 클리어 하시겠습니까?");
            if ((bool)Q.ShowDialog())
                this.AlarmHistory.Clear();
        }
        private bool CanClearAlarmHistory()
        {
            return this.AlarmHistory.Count > 0;
        }
        private void AutoTune()
        {
            var Message = IsAutoTune ? "오토튜닝을 정지하겠습니까?" : "오토튜닝을 시작하겠습니까?";
            View.Question Q = new View.Question(Message);
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    if (IsAutoTune)
                        this.Relays[23].Value = true;
                    else
                        this.Relays[22].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanAutoTune()
        {
            return IsConnected;
        }
        private void TurnOnPower()
        {
            View.Question Q = new View.Question("파워 \"ON\" 하시겠습니까?");
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[125].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanTurnOnPower()
        {
            return IsConnected/* && IoY[18].Value*/;
        }
        private void TurnOffPower()
        {
            View.Question Q = new View.Question("파워 \"OFF\" 하시겠습니까?");
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[126].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanTurnOffPower()
        {
            return IsConnected/* && IoY[17].Value*/;
        }
        private void OpenDoor()
        {
            View.Question Q = new View.Question("도어를 \"OPEN\" 하시겠습니까?");
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[10].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanOpenDoor()
        {
            return IsConnected/* && IoY[20].Value*/;
        }
        private void CloseDoor()
        {
            View.Question Q = new View.Question("도어를 \"CLOSE\" 하시겠습니까?");
            if (!(bool)Q.ShowDialog())
                return;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[11].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        private bool CanCloseDoor()
        {
            return IsConnected/* && IoY[19].Value*/;
        }
        private void StartLOG()
        {
            CloseLOG();

            // MAKE HEADER.
            string[] Header = new string[] {
                "DATE",
                "TIME",
                "온도_PV(℃)",
                "온도_SV(℃)",
                "온도_MV(%)",
                "차압챔버(mmH2O)",
                "차압필터(mmH2O)",
                "모터챔버(Hz)",
                //"쿨링챔버(Hz)",
                "MFC(ℓ/min)",
                "내부온도-1(℃)",
                "내부온도-2(℃)",
                //"내부온도-3(℃)",
                //"내부온도-4(℃)",
                "챔버_OT(℃)",
                "히터_OT(℃)",
                "O2_온도(℃)",
                "O2_EMF(mV)","" +
                "O2_ppm(ppm)",
                "차압챔버_SV(mmH2O)",
                "차압챔버_MV(%)"
            };

            // 1. CSV LOG.
            StringBuilder Sb = new StringBuilder();
            for (int i = 0; i < Header.Length; i++)
                _ = Sb.AppendFormat("{0,15},", Header[i]);

            DateTime Time = DateTime.Now;
            string Name = string.Format("{0}_{1:D3}_{2}.csv", Helper.ToLogNameFormat(Time), Channel.PatternForRun.No, Channel.PatternForRun.Name);
            string Path = System.IO.Path.Combine(Channel.CsvLogStorageDir, Name);
            CsvWriter = new System.IO.StreamWriter(File.Open(Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), Encoding.Default);
            CsvWriter.WriteLine(Sb.ToString().TrimEnd(','));

            // 2. BINARY DATA LOG.
            Name = string.Format("CH.{0} - {1}_{2:D3}_{3}.dat", Channel.No, Helper.ToLogNameFormat(Time), Channel.PatternForRun.No, Channel.PatternForRun.Name);
            this.TrendDataSaveName = Name;
            Path = System.IO.Path.Combine(Channel.BinaryLogStorageDir, Name);
            BinaryDataWriter = new System.IO.BinaryWriter(File.Open(Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
            var Tmp = Encoding.ASCII.GetBytes("DAESUNG-ENT.4CH.N2-CLEANOVEN.V1");
            byte[] Buf = new byte[50];
            Array.Copy(Tmp, 0, Buf, 0, Tmp.Length);
            BinaryDataWriter.Write(Buf);

            LogWriterTimer = new System.Threading.Timer(OnLogTimerCallback, null, 5000, (int)(Channel.BinaryLogSaveInterval * 1000));
        }
        private void CloseLOG()
        {
            if (LogWriterTimer != null)
            {
                LogWriterTimer.Dispose();
                LogWriterTimer = null;
            }
            if (CsvWriter != null)
            {
                CsvWriter.Flush();
                CsvWriter.Close();
                CsvWriter = null;
            }
            if (BinaryDataWriter != null)
            {
                BinaryDataWriter.Flush();
                BinaryDataWriter.Close();
                BinaryDataWriter = null;
            }
        }
        private void OnLogTimerCallback(object state)
        {
            try
            {
                DateTime Time = DateTime.Now;

                // 1. CSV WRITE.
                string[] Token = Helper.ToLogTimeFormat(Time).Split(' ');
                var Sb = new StringBuilder();
                Sb.AppendFormat("{0,15},{1,15},", Token[0], Token[1]);
                foreach (var regNum in this.CsvLoggingNumerics)
                    Sb.AppendFormat("{0,15},", regNum.FormattedValue);
                if (CsvWriter != null)
                    CsvWriter.WriteLine(Sb.ToString().TrimEnd(','));
                CsvWriter.Flush();

                // 2.BINARY WRITE.
                BinaryDataWriter.Write(BitConverter.GetBytes(Time.Ticks));
                foreach (var regNum in BinaryLoggingNumerics)
                    BinaryDataWriter.Write(BitConverter.GetBytes((short)regNum.Value));  // 스케일링되기전 값으로 저장.
                BinaryDataWriter.Flush();
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured in OnLogTimerCallback : {0}", ex.Message);
            }
        }

        public void Initialize()
        {
            try
            {
                UpdateNumeric();
                UpdateRelay();
                UpdateParameters();
                UpdateIoX();
                UpdateIoY();
                UpdateAlertSetupParameter();
                UpdatePattern();
                IsInitialized = true;
            }
            catch (Exception ex) 
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Initialize CleanOven : " + ex.Message);
            }
        }
        public override void DisConnect()
        {
            // PC와 통신연결이 해제되어도 정상동작해야 함...
//             if (IsConnected)
//                 this.NumericValues[42].Value = 0;
            CloseLOG();
            StopMonitor();
            base.DisConnect();
        }
        public async void TransferPattern(PatternViewModel model)
        {
            if (!IsConnected || IsRunning)
                return;

            ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", string.Format("채널.{0}, 패턴 데이터 전송 중...", ChannelNumber));
            Task<bool> T = Task.Run(() =>
            {
                bool ret = false;
                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        string[] condAddr = (from cond in PatternConf.Conditions
                                        select cond.WriteAddress).ToArray();

                        int?[] condScale = (from cond in PatternConf.Conditions
                                         select cond.Scale).ToArray();

                        short[] condValues = new short[condAddr.Length];
                        condValues[0] = (short)(condScale[0] * model.StartConditionUsage);
                        condValues[1] = (short)(condScale[1] * model.DifferencePressChamberSv);
                        condValues[2] = (short)(condScale[2] * model.DifferencePressChamberInitMv);
                        condValues[3] = (short)(condScale[3] * model.DifferencePressChamberManualCtlTime);
                        condValues[4] = (short)(condScale[4] * model.MotorChamberSv);
                        condValues[5] = (short)(condScale[5] * model.O2TargetValue);
                        condValues[6] = (short)(condScale[6] * model.O2TargetReachCheckupTime);
                        condValues[7] = (short)(condScale[7] * model.MFCSv);
                        condValues[8] = (short)(condScale[8] * model.ExhaustValveOpenSetting);
                        condValues[9] = (short)(condScale[9] * model.WaitTemperatureAfterClose);

                        NumericValues[4].Value = model.No;
                        _ = WriteRandomWord(condAddr, condValues);

                        string[] segAddr = new string[7];
                        short[] segValues = new short[7];
                        for (int i = 0; i < model.Segments.Count; i++)      // 30
                        {
                            Segment Seg = PatternConf[i];
                            for (int j = 0; j < segAddr.Length; j++)
                                segAddr[j] = Seg[j].WriteAddress;

                            segValues[0] = (short)(Seg[0].Scale * model.Segments[i].Temperature);
                            segValues[1] = (short)(Seg[1].Scale * model.Segments[i].DifferencePressureChamber);
                            segValues[2] = (short)(Seg[2].Scale * model.Segments[i].MotorChamber);
                            segValues[3] = (short)(Seg[3].Scale * model.Segments[i].MotorCooling);
                            segValues[4] = (short)(Seg[4].Scale * model.Segments[i].MFC);
                            segValues[5] = (short)(Seg[5].Scale * model.Segments[i].ConvertedDurationTime);
                            segValues[6] = (short)(Seg[6].Scale * model.Segments[i].TimeSignalValue);
                            _ = WriteRandomWord(segAddr, segValues);
                        }
                        Relays[123].Value = true;
                        ret = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Dispatch("e", "Exception is Occured while to Transfer pattern configuration : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
                return ret;
            });

            bool Res = await T;
            ProgressWindow.CloseWindow();
            string Msg = Res ? "패턴 데이터 전송 완료" : "패턴 데이터 전송 실패";
            Question Q = new View.Question(Msg);
            _ = Q.ShowDialog();
        }
        public async void TransferPatternNoMsg(PatternViewModel model)
        {
            if (!IsConnected || IsRunning)
                return;
            _ = await Task<bool>.Run(() =>
              {
                  bool ret = false;
                  if (Monitor.TryEnter(SyncKey, 3000))
                  {

                      try
                      {
                          string[] condAddr = (from cond in PatternConf.Conditions
                                          select cond.WriteAddress).ToArray();

                          int?[] condScale = (from cond in PatternConf.Conditions
                                           select cond.Scale).ToArray();

                          short[] condValues = new short[condAddr.Length];
                          condValues[0] = (short)(condScale[0] * model.StartConditionUsage);
                          condValues[1] = (short)(condScale[1] * model.DifferencePressChamberSv);
                          condValues[2] = (short)(condScale[2] * model.DifferencePressChamberInitMv);
                          condValues[3] = (short)(condScale[3] * model.DifferencePressChamberManualCtlTime);
                          condValues[4] = (short)(condScale[4] * model.MotorChamberSv);
                          condValues[5] = (short)(condScale[5] * model.O2TargetValue);
                          condValues[6] = (short)(condScale[6] * model.O2TargetReachCheckupTime);
                          condValues[7] = (short)(condScale[7] * model.MFCSv);
                          condValues[8] = (short)(condScale[8] * model.ExhaustValveOpenSetting);
                          condValues[9] = (short)(condScale[9] * model.WaitTemperatureAfterClose);

                          NumericValues[4].Value = model.No;
                          WriteRandomWord(condAddr, condValues);

                          string[] segAddr = new string[7];
                          short[] segValues = new short[7];
                          for (int i = 0; i < model.Segments.Count; i++)      // 30
                        {
                              Segment Seg = PatternConf[i];
                              for (int j = 0; j < segAddr.Length; j++)
                                  segAddr[j] = Seg[j].WriteAddress;

                              segValues[0] = (short)(Seg[0].Scale * model.Segments[i].Temperature);
                              segValues[1] = (short)(Seg[1].Scale * model.Segments[i].DifferencePressureChamber);
                              segValues[2] = (short)(Seg[2].Scale * model.Segments[i].MotorChamber);
                              segValues[3] = (short)(Seg[3].Scale * model.Segments[i].MotorCooling);
                              segValues[4] = (short)(Seg[4].Scale * model.Segments[i].MFC);
                              segValues[5] = (short)(Seg[5].Scale * model.Segments[i].ConvertedDurationTime);
                              segValues[6] = (short)(Seg[6].Scale * model.Segments[i].TimeSignalValue);
                              WriteRandomWord(segAddr, segValues);
                          }
                          Relays[123].Value = true;
                          ret = true;
                      }
                      catch (Exception ex)
                      {
                          Log.Logger.Dispatch("e", "Exception is Occured while to Transfer pattern configuration : " + ex.Message);
                      }
                      finally
                      {
                          Monitor.Exit(SyncKey);
                      }
                  }
                  return ret;
              });
        }
        public async void TransferPatternWaitTemperatureAfterClose(PatternViewModel model)
        {
            if (!IsConnected)
                return;

            ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", string.Format("채널.{0}, 패턴 데이터 전송 중...", ChannelNumber));
            Task<bool> T = Task.Run(() =>
            {
                bool ret = false;
                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        string addr = PatternConf.Conditions[9].WriteAddress;
                        int Scale = (int)PatternConf.Conditions[9].Scale;
                        short value = (short)(model.WaitTemperatureAfterClose * Scale);
                        _ = WriteWord(addr, value);
                        ret = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Dispatch("e", "Exception is Occured while to Transfer pattern configuration : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
                return ret;
            });

            bool Res = await T;
            ProgressWindow.CloseWindow();
            string Msg = Res ? "패턴 데이터 전송 완료" : "패턴 데이터 전송 실패";
            View.Question Q = new View.Question(Msg);
            _ = Q.ShowDialog();
        }
        public void UpdateAnalyzerTemperature(double value)
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    NumericValues[29].Value = value;
                    this.AnalyzerO2Temperature = value;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void UpdateAnalyzerEmf(double value)
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    NumericValues[30].Value = value;
                    this.AnalyzerO2Emf = value;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void UpdateAnalyzerConcentrationPpm(double value)
        {
            if (value > 1000.0)
                value = 1000.0;

            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    NumericValues[31].Value = value;
                    this.AnalyzerO2Ppm = value;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void O2AnalyzerConnectStateUpdate(bool State)
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    NumericValues[44].Value = State ? 0 : 1;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void BuzzerStop()
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[127].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void AlarmReset()
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[128].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }

        // EFEM 연동시 확인 메세지 상자 없이 동작하기 위해서...
        public void OpenDoorNoMsg()
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[10].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void CloseDoorNoMsg()
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.Relays[11].Value = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
        }
        public void StartBake()
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                DateTime First = DateTime.Now;
                DateTime Last = First.Add(TimeSpan.FromHours(G.REALTIME_TREND_CAPACITY));

                if (TrendSeriesGrp1[0].DataSeries.ParentSurface != null)
                {
                    SciChart.Charting.Visuals.Axes.DateTimeAxis xAxis = TrendSeriesGrp1[0].DataSeries.ParentSurface.XAxis as SciChart.Charting.Visuals.Axes.DateTimeAxis;
                    xAxis.VisibleRange = new DateRange(First, Last);
                    xAxis.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 10));
                    xAxis.MinorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 50));
                }
                foreach (IRenderableSeriesViewModel Series in TrendSeriesGrp1)
                {
                    Series.DataSeries.Clear();
                }

                if (TrendSeriesGrp2[0].DataSeries.ParentSurface != null)
                {
                    SciChart.Charting.Visuals.Axes.DateTimeAxis xAxis = TrendSeriesGrp2[0].DataSeries.ParentSurface.XAxis as SciChart.Charting.Visuals.Axes.DateTimeAxis;
                    xAxis.VisibleRange = new DateRange(First, Last);
                    xAxis.MajorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 10));
                    xAxis.MinorDelta = new TimeSpan((long)((Last.Ticks - First.Ticks) / 50));
                }                  
                foreach (IRenderableSeriesViewModel Series in TrendSeriesGrp2)
                {
                    Series.DataSeries.Clear();
                }

                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        this.NumericValues[42].Value = (Channel.PatternForRun.StartConditionUsage == 1) ? 5 : 7;
                    }
                    finally
                    { 
                        Monitor.Exit(SyncKey);
                    }
                }

                __ManualCtrl = false;
                RaisePropertiesChanged("ManualCtrl");
                StartLOG();
            });
        }
        public void AbortBake()
        {
            if (Monitor.TryEnter(SyncKey, 3000))
            {
                try
                {
                    this.NumericValues[42].Value = 0;
                    this.Relays[1].Value = false;       // 운전정지시 HOLD상태 해제.
                    this.IsExternalAborted = true;
                }
                finally
                {
                    Monitor.Exit(SyncKey);
                }
            }
            CloseLOG();
        }
    }
}
