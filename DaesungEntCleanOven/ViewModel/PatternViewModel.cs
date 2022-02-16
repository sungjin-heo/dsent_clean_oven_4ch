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
using DaesungEntCleanOven4.Properties;
using DaesungEntCleanOven4.Model;

namespace DaesungEntCleanOven4.ViewModel
{
    internal class PatternViewModel : DevExpress.Mvvm.BindableBase
    {
        public PatternViewModel(ChannelViewModel Ch, Model.Pattern pattern)
        {
            this.Channel = Ch ?? throw new ArgumentNullException("ChannelViewModel");
            this.Model = pattern ?? throw new ArgumentNullException("Model.Pattern");
            this.Segments = new List<SegmentViewModel>();
            for (int i = 0; i < pattern.SegmentCount; i++)
                Segments.Add(new SegmentViewModel(pattern.Segments[i]));

            this.OpenEditorDlgCommand = new DevExpress.Mvvm.DelegateCommand<object>(OpenEditorDlg, CanOpenEditorDlg);
            this.OpenPatternSelectDlgCommand = new DevExpress.Mvvm.DelegateCommand(OpenPatternSelectDlg, CanOpenPatternSelectDlg);
            this.SelectSegmentGrp1Command = new DevExpress.Mvvm.DelegateCommand(SelectSegmentGrp1, CanSelectSegmentGrp1);
            this.SelectSegmentGrp2Command = new DevExpress.Mvvm.DelegateCommand(SelectSegmentGrp2, CanSelectSegmentGrp2);
            this.OpenTimeSignalSetupDlgCommand = new DevExpress.Mvvm.DelegateCommand<object>(OpenTimeSignalSetupDlg, CanOpenTimeSignalSetupDlg);
            this.ResetCommand = new DevExpress.Mvvm.DelegateCommand(Reset, CanReset);
            this.SaveCommand = new DevExpress.Mvvm.DelegateCommand(Save, CanSave);

            string[] yIds = new string[] { "y1", "y2", "y3", "y4", "y5" };
            string[] sNames = new string[] { "Temperature", "Difference Chamber", "Motor Chamber", "Motor Cooling", "MFC" };
            System.Windows.Media.Color[] sColor = new Color[] { Colors.Red, Colors.Lime, Colors.Aqua, Colors.Yellow, Colors.Magenta };

            this.Group1Series = new List<IRenderableSeriesViewModel>();
            for (int i = 0; i < yIds.Length; i++)
            {
                XyDataSeries<int, double> series = new XyDataSeries<int, double> 
                { 
                    SeriesName = sNames[i], 
                    AcceptsUnsortedData = true 
                };

                LineRenderableSeriesViewModel Ln = new LineRenderableSeriesViewModel()
                {
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
                XyDataSeries<int, double> series = new XyDataSeries<int, double> 
                { 
                    SeriesName = sNames[i], 
                    AcceptsUnsortedData = true 
                };

                LineRenderableSeriesViewModel Ln = new LineRenderableSeriesViewModel() 
                {
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

        public ChannelViewModel Channel { get; private set; }
        public Model.Pattern Model { get; private set; }
        public List<SegmentViewModel> Segments { get; private set; }
        public int No => Model.No;
        public string Name
        {
            get => Model.Name;
            set
            {
                Model.Name = value;
                RaisePropertyChanged("Name");
            }
        }
        public string Description => Model.Description;
        public int StartConditionUsage
        {
            get => Model.StartConditionUsage;
            set
            {
                Model.StartConditionUsage = value;
                RaisePropertiesChanged("StartConditionUsage");
            }
        }
        public double DifferencePressChamberSv
        {
            get => Model.DifferencePressChamberSv;
            set
            {
                Model.DifferencePressChamberSv = value;
                RaisePropertiesChanged("DifferencePressChamberSv");
            }
        }
        public double DifferencePressChamberInitMv
        {
            get => Model.DifferencePressChamberInitMv;
            set
            {
                Model.DifferencePressChamberInitMv = value;
                RaisePropertiesChanged("DifferencePressChamberInitMv");
            }
        }
        public double DifferencePressChamberManualCtlTime
        {
            get => Model.DifferencePressChamberManualCtlTime;
            set
            {
                Model.DifferencePressChamberManualCtlTime = value;
                RaisePropertiesChanged("DifferencePressChamberManualCtlTime");
            }
        }
        public double MotorChamberSv
        {
            get => Model.MotorChamberSv;
            set
            {
                Model.MotorChamberSv = value;
                RaisePropertiesChanged("MotorChamberSv");
            }
        }
        public double O2TargetValue
        {
            get => Model.O2TargetValue;
            set
            {
                Model.O2TargetValue = value;
                RaisePropertiesChanged("O2TargetValue");
            }
        }
        public double O2TargetReachCheckupTime
        {
            get => Model.O2TargetReachCheckupTime;
            set
            {
                Model.O2TargetReachCheckupTime = value;
                RaisePropertiesChanged("O2TargetReachCheckupTime");
            }
        }
        public double MFCSv
        {
            get => Model.MFCSv;
            set
            {
                Model.MFCSv = value;
                RaisePropertiesChanged("MFCSv");
            }
        }
        public int ExhaustValveOpenSetting
        {
            get => Model.ExhaustValveOpenSetting;
            set
            {
                Model.ExhaustValveOpenSetting = value;
                RaisePropertiesChanged("ExhaustValveOpenSetting");
            }
        }
        public double WaitTemperatureAfterClose
        {
            get => Model.WaitTemperatureAfterClose;
            set
            {
                Model.WaitTemperatureAfterClose = value;
                RaisePropertiesChanged("WaitTemperatureAfterClose");
            }
        }
        public List<SegmentViewModel> SelectedSegmentGrp { get; private set; }
        public List<IRenderableSeriesViewModel> Group1Series { get; private set; }
        public List<IRenderableSeriesViewModel> Group2Series { get; private set; }
        public event EventHandler PatternChanged;

        public DevExpress.Mvvm.DelegateCommand OpenPatternSelectDlgCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand<object> OpenEditorDlgCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand SelectSegmentGrp1Command { get; private set; }
        public DevExpress.Mvvm.DelegateCommand SelectSegmentGrp2Command { get; private set; }
        public DevExpress.Mvvm.DelegateCommand<object> OpenTimeSignalSetupDlgCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand ResetCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand SaveCommand { get; private set; }
        public void Load(Model.Pattern model)
        {
            this.Model = model;
            this.Segments.Clear();
            for (int i = 0; i < model.SegmentCount; i++)
                Segments.Add(new SegmentViewModel(model.Segments[i]));
            UpdateSegmentChart();
            System.Reflection.PropertyInfo[] Properties = typeof(PatternViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        private void OpenEditorDlg(object grpId)
        {
            if (grpId is string Id)
            {
                PatternViewModel Copy = this.Clone();
                if (Id == "1")
                    Copy.SelectSegmentGrp1();
                else if (Id == "2")
                    Copy.SelectSegmentGrp2();

                View.PatternSetupDlg Dlg = new View.PatternSetupDlg() { DataContext = Copy };
                if ((bool)Dlg.ShowDialog())
                {
                    // 일단 저장은 하고 PLC로 보내는 것은 변경된 경우만 전송.... 
                    // -> 수정 : 편집기에서는 저장만 하도록. 편집된 패턴을 선택하는 것은 메인화면의 패턴선택메뉴를 통해.
                    if (!Copy.Equals(this))
                    {
                        View.Question qDlg = new View.Question("패턴 정보가 변경되었습니다.\r\n변경된 내용을 저장하시겠습니까?");
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
        private bool CanOpenEditorDlg(object parameter)
        {
            return true;
        }
        private void OpenPatternSelectDlg()
        {
            View.PatternSelectDlg Dlg = new View.PatternSelectDlg() { DataContext = Channel.PatternMetaDatas };
            if ((bool)Dlg.ShowDialog())
            {
                if (Dlg.SelectedMetaData is Model.PatternMetadata metaData)
                    Channel.SelectEditPattern(metaData.No);
            }
        }
        private bool CanOpenPatternSelectDlg()
        {
            // 패턴 선택은 항상 가능하도록....
            return true;
        }    
        private void SelectSegmentGrp1()
        {
            this.SelectedSegmentGrp = Segments.GetRange(0, 15);
            RaisePropertiesChanged("SelectedSegmentGrp");
        }
        private bool CanSelectSegmentGrp1()
        {
            return SelectedSegmentGrp != null && SelectedSegmentGrp[0].No == 16;
        }
        private void SelectSegmentGrp2()
        {
            this.SelectedSegmentGrp = Segments.GetRange(15, 15);
            RaisePropertiesChanged("SelectedSegmentGrp");
        }
        private bool CanSelectSegmentGrp2()
        {
            return SelectedSegmentGrp != null && SelectedSegmentGrp[0].No == 1;
        }
        private void OpenTimeSignalSetupDlg(object parameter)
        {
            if (parameter is SegmentViewModel Seg)
            {
                SegmentViewModel Copy = (SegmentViewModel)Seg.Clone();
                View.PatternTimeSignalSetupDlg Dlg = new View.PatternTimeSignalSetupDlg() { DataContext = Copy };
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
        private bool CanOpenTimeSignalSetupDlg(object parameter)
        {
            return true;
        }
        private void Reset()
        {
            View.Question qDlg = new View.Question("패턴 정보를 리셋하시겠습니까?");
            if (!(bool)qDlg.ShowDialog())
                return;

            Model.Reset();
            Segments.ForEach(Seg => Seg.Reset());
            Channel.PatternMetaDatas[No - 1].Name = this.Name;
            Channel.PatternMetaDatas[No - 1].IsAssigned = false;
            Channel.PatternMetaDatas[No - 1].RegisteredScanCode = "";
            Channel.SavePatternMetaData();
            System.Reflection.PropertyInfo[] Properties = typeof(PatternViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        private bool CanReset()
        {
            return true;
        }
        private void Save()
        {
            try
            {
                string path = Path.Combine(Channel.PatternStorageDir, string.Format("{0:D3}.xml", this.No));
                Pattern.SaveTo(this.Model, path);
                Channel.PatternMetaDatas[this.No - 1].Name = this.Name;
                Channel.PatternMetaDatas[this.No - 1].IsAssigned = true;
                Channel.SavePatternMetaData();
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Save pattern config : {0}", ex.Message);
            }
        }
        private bool CanSave()
        {
            return true;
        }
        public void UpdateSegmentChart()
        {
            foreach (IRenderableSeriesViewModel Ln in Group1Series)
                Ln.DataSeries.Clear();
            foreach (IRenderableSeriesViewModel Ln in Group2Series)
                Ln.DataSeries.Clear();

            List<double> qry1 = (from Seg in Segments select Seg.Temperature).ToList();
            List<double> qry2 = (from Seg in Segments select Seg.DifferencePressureChamber).ToList();
            List<double> qry3 = (from Seg in Segments select Seg.MotorChamber).ToList();
            List<double> qry4 = (from Seg in Segments select Seg.MotorCooling).ToList();
            List<double> qry5 = (from Seg in Segments select Seg.MFC).ToList();

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

            System.Reflection.PropertyInfo[] Properties = typeof(PatternViewModel).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
        public PatternViewModel Clone()
        {
            return new PatternViewModel(Channel, (Model.Pattern)Model.Clone());
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
