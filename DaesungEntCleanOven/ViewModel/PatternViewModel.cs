using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.IO;
using System.Windows.Media;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.ChartSeries;
using DaesungEntCleanOven.Properties;
using DaesungEntCleanOven.Model;

namespace DaesungEntCleanOven.ViewModel
{
    class PatternViewModel : DevExpress.Mvvm.BindableBase
    {
        public PatternViewModel(Model.Pattern patt)
        {
            this.OpenPatternSelectDlgCommand = new DevExpress.Mvvm.DelegateCommand(OpenPatternSelectDlg, CanOpenPatternSelectDlg);
            this.OpenEditorDlgCommand = new DevExpress.Mvvm.DelegateCommand<object>(OpenEditorDlg, CanOpenEditorDlg);
            this.SelectSegmentGrp1Command = new DevExpress.Mvvm.DelegateCommand(SelectSegmentGrp1, CanSelectSegmentGrp1);
            this.SelectSegmentGrp2Command = new DevExpress.Mvvm.DelegateCommand(SelectSegmentGrp2, CanSelectSegmentGrp2);
            this.OpenTimeSignalSetupDlgCommand = new DevExpress.Mvvm.DelegateCommand<object>(OpenTimeSignalSetupDlg, CanOpenTimeSignalSetupDlg);
            this.ResetCommand = new DevExpress.Mvvm.DelegateCommand(Reset, CanReset);
            this.SaveCommand = new DevExpress.Mvvm.DelegateCommand(Save, CanSave);

            this.Model = patt ?? throw new ArgumentNullException("Model.Pattern");
            this.Segments = new List<SegmentViewModel>();
            for(int i = 0; i < patt.SegmentCount; i++)
                Segments.Add(new SegmentViewModel(patt.Segments[i]));

            string[] yIds = new string[] { "y1", "y2", "y3", "y4", "y5" };            
            string[] sNames = new string[] { "Temperature", "Difference Chamber", "Motor Chamber", "Motor Cooling", "MFC" };
            System.Windows.Media.Color[] sColor = new Color[] { Colors.Red, Colors.Lime, Colors.Aqua, Colors.Yellow, Colors.Magenta };
            this.Group1Series = new List<IRenderableSeriesViewModel>();
            for (int i = 0; i < yIds.Length; i++)
            {
                XyDataSeries<int, double> series = new XyDataSeries<int, double> { SeriesName = sNames[i], AcceptsUnsortedData = true };
                var Ln = new LineRenderableSeriesViewModel() {
                    YAxisId = yIds[i],
                    DataSeries = series,
                    Stroke = sColor[i],
                    StrokeThickness = 1,
                    AntiAliasing = false,
                    IsVisible = true,
                    IsDigitalLine = i > 0
                };
                Group1Series.Add(Ln);
            }
            this.Group2Series = new List<IRenderableSeriesViewModel>();
            for (int i = 0; i < yIds.Length; i++)
            {
                XyDataSeries<int, double> series = new XyDataSeries<int, double> { SeriesName = sNames[i], AcceptsUnsortedData = true };
                var Ln = new LineRenderableSeriesViewModel() {
                    YAxisId = yIds[i],
                    DataSeries = series,
                    Stroke = sColor[i],
                    StrokeThickness = 1,
                    AntiAliasing = false,
                    IsVisible = true,
                    IsDigitalLine = i > 0
                };
                Group2Series.Add(Ln);
            }

            UpdateSegmentChart();
        }

        public Model.Pattern Model { get; protected set; }
        public List<SegmentViewModel> Segments { get; protected set; }
        public int No { get { return Model.No; } }
        public string Name
        {
            get { return Model.Name; }
            set
            {
                Model.Name = value;
                RaisePropertyChanged("Name");
            }
        }
        public string Description { get { return Model.Description; } }
        public int StartConditionUsage
        {
            get { return Model.StartConditionUsage; }
            set
            {
                Model.StartConditionUsage = value;
                RaisePropertiesChanged("StartConditionUsage");
            }
        }
        public double DifferencePressChamberSv
        {
            get { return Model.DifferencePressChamberSv; }
            set
            {
                Model.DifferencePressChamberSv = value;
                RaisePropertiesChanged("DifferencePressChamberSv");
            }
        }
        public double DifferencePressChamberInitMv
        {
            get { return Model.DifferencePressChamberInitMv; }
            set
            {
                Model.DifferencePressChamberInitMv = value;
                RaisePropertiesChanged("DifferencePressChamberInitMv");
            }
        }
        public double DifferencePressChamberManualCtlTime
        {
            get { return Model.DifferencePressChamberManualCtlTime; }
            set
            {
                Model.DifferencePressChamberManualCtlTime = value;
                RaisePropertiesChanged("DifferencePressChamberManualCtlTime");
            }
        }
        public double MotorChamberSv
        {
            get { return Model.MotorChamberSv; }
            set
            {
                Model.MotorChamberSv = value;
                RaisePropertiesChanged("MotorChamberSv");
            }
        }
        public double O2TargetValue
        {
            get { return Model.O2TargetValue; }
            set
            {
                Model.O2TargetValue = value;
                RaisePropertiesChanged("O2TargetValue");
            }
        }
        public double O2TargetReachCheckupTime
        {
            get { return Model.O2TargetReachCheckupTime; }
            set
            {
                Model.O2TargetReachCheckupTime = value;
                RaisePropertiesChanged("O2TargetReachCheckupTime");
            }
        }
        public double MFCSv
        {
            get { return Model.MFCSv; }
            set
            {
                Model.MFCSv = value;
                RaisePropertiesChanged("MFCSv");
            }
        }
        public int ExhaustValveOpenSetting
        {
            get { return Model.ExhaustValveOpenSetting; }
            set
            {
                Model.ExhaustValveOpenSetting = value;
                RaisePropertiesChanged("ExhaustValveOpenSetting");
            }
        }
        public double WaitTemperatureAfterClose
        {
            get { return Model.WaitTemperatureAfterClose; }
            set
            {
                Model.WaitTemperatureAfterClose = value;
                RaisePropertiesChanged("WaitTemperatureAfterClose");
            }
        }
        public List<SegmentViewModel> SelectedSegmentGrp { get; protected set; }
        public List<IRenderableSeriesViewModel> Group1Series { get; protected set; }
        public List<IRenderableSeriesViewModel> Group2Series { get; protected set; }
        public event EventHandler PatternChanged;
        public DevExpress.Mvvm.DelegateCommand OpenPatternSelectDlgCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand<object> OpenEditorDlgCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand SelectSegmentGrp1Command { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand SelectSegmentGrp2Command { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand<object> OpenTimeSignalSetupDlgCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand ResetCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand SaveCommand { get; protected set; }
        public void Load(Model.Pattern model)
        {
            this.Model = model;
            this.Segments.Clear();
            for (int i = 0; i < model.SegmentCount; i++)
                Segments.Add(new SegmentViewModel(model.Segments[i]));
            UpdateSegmentChart();
            var Properties = typeof(PatternViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        protected void OpenPatternSelectDlg()
        {
            var Dlg = new View.PatternSelectDlg() { DataContext = G.PatternMetaDatas };
            if ((bool)Dlg.ShowDialog())
            {
                if (Dlg.SelectedMetaData is Model.PatternMetadata metaData)
                    G.SelectEditPattern(metaData.No);
            }
        }
        protected bool CanOpenPatternSelectDlg()
        {
            // 패턴 선택은 항상 가능하도록....
            return true;
        }
        protected void OpenEditorDlg(object parameter)
        {
            if (parameter is string Id)
            {
                var Copy = this.Clone();
                if (Id == "1")
                    Copy.SelectSegmentGrp1();
                else if (Id == "2")
                    Copy.SelectSegmentGrp2();

                var Dlg = new View.PatternSetupDlg() { DataContext = Copy };
                if ((bool)Dlg.ShowDialog())
                {
                    // 일단 저장은 하고 PLC로 보내는 것은 변경된 경우만 전송.... 
                    // -> 수정 : 편집기에서는 저장만 하도록. 편집된 패턴을 선택하는 것은 메인화면의 패턴선택메뉴를 통해.
                    if (!Copy.Equals(this))
                    {
                        var qDlg = new View.Question("패턴 정보가 변경되었습니다.\r\n변경된 내용을 저장하시겠습니까?");
                        if ((bool)qDlg.ShowDialog())
                        {
                            Copy.Save();
                            Load(Copy.Model);                          
                            PatternChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                 }
            }
        }
        protected bool CanOpenEditorDlg(object parameter)
        {
            return true;
        }
        protected void SelectSegmentGrp1()
        {
            this.SelectedSegmentGrp = Segments.GetRange(0, 15);
            RaisePropertiesChanged("SelectedSegmentGrp");
        }
        protected bool CanSelectSegmentGrp1()
        {
            return SelectedSegmentGrp != null && SelectedSegmentGrp[0].No == 16;
        }
        protected void SelectSegmentGrp2()
        {
            this.SelectedSegmentGrp = Segments.GetRange(15, 15);
            RaisePropertiesChanged("SelectedSegmentGrp");
        }
        protected bool CanSelectSegmentGrp2()
        {
            return SelectedSegmentGrp != null && SelectedSegmentGrp[0].No == 1;
        }
        protected void OpenTimeSignalSetupDlg(object parameter)
        {
            if (parameter is SegmentViewModel Seg)
            {
                var Copy = (SegmentViewModel)Seg.Clone();
                var Dlg = new View.PatternTimeSignalSetupDlg() { DataContext = Copy };
                if ((bool)Dlg.ShowDialog())
                {
                    Seg.DeviationAlarmUsed = Copy.DeviationAlarmUsed;
                    Seg.N2InputValve = Copy.N2InputValve;
                    Seg.CoerciveExhaustValve = Copy.CoerciveExhaustValve;
                    Seg.OxygenAnalyzerValve = Copy.OxygenAnalyzerValve;
                    Seg.CoolingWaterValve = Copy.CoolingWaterValve;
                    Seg.HeaterCutoffUsed = Copy.HeaterCutoffUsed;
                    Seg.CoolingFanUsed = Copy.CoolingFanUsed;
                    Seg.CoolingChamberUsed = Copy.CoolingChamberUsed;
                }
            }
        }
        protected bool CanOpenTimeSignalSetupDlg(object parameter)
        {
            return true;
        }
        protected void Reset()
        {
            var qDlg = new View.Question("패턴 정보를 리셋하시겠습니까?");
            if (!(bool)qDlg.ShowDialog())
                return;

            Model.Reset();
            Segments.ForEach(Seg => Seg.Reset());
            G.PatternMetaDatas[this.No - 1].Name = this.Name;
            G.PatternMetaDatas[this.No - 1].IsAssigned = false;
            G.PatternMetaDatas[this.No - 1].RegisteredScanCode = "";
            G.SavePatternMetaData();
            var Properties = typeof(PatternViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        protected bool CanReset()
        {
            return true;
        }
        protected void Save()
        {
            try
            {
                string path = Path.Combine(G.PatternStorageDir, string.Format("{0:D3}.xml", this.No));
                Pattern.SaveTo(this.Model, path);
                G.PatternMetaDatas[this.No - 1].Name = this.Name;
                G.PatternMetaDatas[this.No - 1].IsAssigned = true;
                G.SavePatternMetaData();
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Save pattern config : {0}", ex.Message);
            }
        }
        protected bool CanSave()
        {
            return true;
        }
        public void UpdateSegmentChart()
        {
            foreach (var Ln in Group1Series)
                Ln.DataSeries.Clear();
            foreach (var Ln in Group2Series)
                Ln.DataSeries.Clear();

            var qry1 = (from Seg in Segments select Seg.Temperature).ToList();
            var qry2 = (from Seg in Segments select Seg.DifferencePressureChamber).ToList();
            var qry3 = (from Seg in Segments select Seg.MotorChamber).ToList();
            var qry4 = (from Seg in Segments select Seg.MotorCooling).ToList();
            var qry5 = (from Seg in Segments select Seg.MFC).ToList();

            List<double> yValues;
            int[] xValues = new int[16];
            for (int i = 0; i < xValues.Length; i++)
                xValues[i] = i;
            
            yValues = qry1.GetRange(0, 15);
            yValues.Insert(0, 0);
            (Group1Series[0].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry2.GetRange(0, 15);
            yValues.Add(yValues[14]);
            (Group1Series[1].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry3.GetRange(0, 15);
            yValues.Add(yValues[14]);
            (Group1Series[2].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry4.GetRange(0, 15);
            yValues.Add(yValues[14]);
            (Group1Series[3].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry5.GetRange(0, 15);
            yValues.Add(yValues[14]);
            (Group1Series[4].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);

            yValues = qry1.GetRange(15, 15);
            yValues.Insert(0, 0);
            (Group2Series[0].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry2.GetRange(15, 15);
            yValues.Add(yValues[14]);
            (Group2Series[1].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry3.GetRange(15, 15);
            yValues.Add(yValues[14]);
            (Group2Series[2].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry4.GetRange(15, 15);
            yValues.Add(yValues[14]);
            (Group2Series[3].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);
            yValues = qry5.GetRange(15, 15);
            yValues.Add(yValues[14]);
            (Group2Series[4].DataSeries as XyDataSeries<int, double>).Append(xValues, yValues);

            var Properties = typeof(PatternViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        public PatternViewModel Clone()
        {
            return new PatternViewModel((Model.Pattern)this.Model.Clone());
        }        
        public override bool Equals(object obj)
        {
            return ((PatternViewModel)obj).Model.Equals(this.Model);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
