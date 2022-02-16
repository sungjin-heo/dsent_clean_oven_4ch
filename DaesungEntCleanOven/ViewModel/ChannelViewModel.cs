using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using Microsoft.Win32;

using DevExpress.Mvvm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Json;
using Util;
using DaesungEntCleanOven4.Properties;
using DaesungEntCleanOven4.Model;

namespace DaesungEntCleanOven4.ViewModel
{
    enum ViewType
    {
        None,
        SystemOperateStatus,
        PatternConfiguration,
        RealtimeTrendGraph,
    }

    internal class ChannelViewModel : DevExpress.Mvvm.ViewModelBase, IDisposable
    {
        public ChannelViewModel(int Ch)
        {
            this.No = Ch;
            this.OpenCommCommand = new DevExpress.Mvvm.DelegateCommand(OpenComm, CanOpenComm);
            this.CloseCommCommand = new DevExpress.Mvvm.DelegateCommand(CloseComm, CanCloseComm);
            this.OpenOperationViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenOperationView, CanOpenOperationView);
            this.OpenPatternConfigurationViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenPatternConfigurationView, CanOpenPatternConfigurationView);
            this.OpenManualControlViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenManualControlView, CanOpenManualControlView);
            this.OpenIoListViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenIoListView, CanOpenIoListView);
            this.OpenRealtimeTrendViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenRealtimeTrendView, CanOpenRealtimeTrendView);
            this.OpenLogDataViewerCommand = new DevExpress.Mvvm.DelegateCommand(OpenLogDataViewer, CanOpenLogDataViewer);
            this.OpenCurrentAlarmViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenCurrentAlarmView, CanOpenCurrentAlarmView);
            this.OpenAlarmHistoryViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenAlarmHistoryView, CanOpenAlarmHistoryView);
            this.OpenBarcodeScanViewCommand = new DevExpress.Mvvm.DelegateCommand(OpenBarcodeScanView, CanOpenBarcodeScanView);
            this.OpenSensorRangeSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenSensorRangeSetup, CanOpenSensorRangeSetup);
            this.OpenSensorParameterSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenSensorParameterSetup, CanOpenSensorParameterSetup);
            this.OpenAutoTuneSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenAutoTuneSetup, CanOpenAutoTuneSetup);
            this.OpenAlertParameterSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenAlertParameterSetup, CanOpenAlertParameterSetup);
            this.OpenTrendSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenTrendSetup, CanOpenTrendSetup);
            this.OpenDifferenceChammberInitSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenDifferenceChammberInitSetup, CanOpenDifferenceChammberInitSetup);
            this.OpenChangePaswordCommand = new DevExpress.Mvvm.DelegateCommand(OpenChangePasword, CanOpenChangePasword);
            this.OpenPLCCommSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenPLCCommSetup, CanOpenPLCCommSetup);
            this.OpenAnalyzerCommSetupCommand = new DevExpress.Mvvm.DelegateCommand(OpenAnalyzerCommSetup, CanOpenAnalyzerCommSetup);
            this.MoveToDetailViewCommand = new DevExpress.Mvvm.DelegateCommand(MoveToDetailView);
            this.BackToIntegrateViewCommand = new DevExpress.Mvvm.DelegateCommand(BackToIntegrateView);

            // 설정 정보 로딩.
            JToken json;
            using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\system.json", Ch)))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    json = JToken.ReadFrom(Jr);
                }
            }
            CleanOvenIpAddr = (string)json["device"]["clean_oven_chamber"]["addr"];
            CleanOvenTcp = (int)json["device"]["clean_oven_chamber"]["port"];
            CleanOvenLatencyQueryInterval = (int)json["device"]["clean_oven_chamber"]["latency_query_interval"];
            CleanOvenLatencyQueryItems = (string)json["device"]["clean_oven_chamber"]["latency_query_items"];
            LatencyQueryItems = CleanOvenLatencyQueryItems.Split(',');
            AnalyzerId = (byte)json["device"]["analyzer"]["id"];
            AnalyzerPort = (string)json["device"]["analyzer"]["port"];
            AnalyzerBaudrate = (int)json["device"]["analyzer"]["baud_rate"];
            PatternStorageDir = (string)json["pattern"]["dir"];
            LastestSelectedPatternNo = (int)json["pattern"]["last_selected_pattern"];
            CsvLogStorageDir = (string)json["log"]["csv"]["dir"];
            CsvLogSaveInterval = (int)json["log"]["csv"]["intv"];
            BinaryLogStorageDir = (string)json["log"]["binary"]["dir"];
            BinaryLogSaveInterval = (int)json["log"]["binary"]["intv"];
            AlarmStorageDir = (string)json["log"]["alarm"]["dir"];

            // 채널 데이터 저장을 위한 디렉토리 생성.
            if (!Directory.Exists(PatternStorageDir))
                Directory.CreateDirectory(PatternStorageDir);
            if (!Directory.Exists(CsvLogStorageDir))
                Directory.CreateDirectory(CsvLogStorageDir);
            if (!Directory.Exists(BinaryLogStorageDir))
                Directory.CreateDirectory(BinaryLogStorageDir);
            if (!Directory.Exists(AlarmStorageDir))
                Directory.CreateDirectory(AlarmStorageDir);

            // 디바이스 통신 연결...
            this.CleanOvenChamber = new Equipment.CleanOven(CleanOvenIpAddr, CleanOvenTcp, this);
            this.CleanOvenChamber.ConnectionChanged += (s, e) => {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (CleanOvenChamber.ConnectState)
                    {
                        case Comm.ConnectionState.Closed:
                        case Comm.ConnectionState.Connecting:
                            this.CleanOvenChamber.StopMonitor();
                            OpenEmptyView();
                            break;
                        case Comm.ConnectionState.Connected:
                            {
                                if (!CleanOvenChamber.IsInitialized)
                                    CleanOvenChamber.Initialize();
                                CleanOvenChamber.StartMonitor();
                            }
                            OpenOperationView();
                            break;
                        case Comm.ConnectionState.Retry:
                            {
                                if (NotifyDlg == null && !CleanOvenCommErrorUserVerify)
                                {
                                    NotifyDlg = new View.Question(string.Format("채널.{0} - PLC 통신이 끊어져 재 연결을 시도합니다...", Ch));
                                    if ((bool)NotifyDlg.ShowDialog())
                                        CleanOvenCommErrorUserVerify = true;
                                    NotifyDlg = null;
                                }
                            }
                            OpenEmptyView();
                            break;
                    }
                });
            };
            this.CleanOvenChamber.Started += (s, e) => { MonitorTimeWatch.Restart(); };
            this.CleanOvenChamber.Stopped += (s, e) => { MonitorTimeWatch.Stop(); };

            this.Analyzer = new Equipment.O2Analyzer(AnalyzerPort, AnalyzerBaudrate, AnalyzerId);
            this.Analyzer.MonitorDataUpdated += Analyzer_MonitorDataUpdated;
            this.Analyzer.Connected += (s, e) => { this.CleanOvenChamber.O2AnalyzerConnected(); };
            this.Analyzer.DisConnected += (s, e) =>
            {
                this.CleanOvenChamber.O2AnalyzerDisConnected();
                Application.Current.Dispatcher.Invoke(() => {
                    this.Analyzer.Close();
                    Thread.Sleep(3000);
                    int TryCnt = 5;
                    do
                    {
                        if (this.Analyzer.Open())
                        {
                            this.Analyzer.StartMonitor();
                            break;
                        }
                        TryCnt--;
                        Thread.Sleep(3000);

                    } while (TryCnt >= 0);
                });
            };

            // 패턴 모델 로딩...
            Model.Pattern model;
            string path = Path.Combine(PatternStorageDir, string.Format("{0:D3}.xml", LastestSelectedPatternNo));
            if (System.IO.File.Exists(path))
                model = Model.Pattern.LoadFrom(path);
            else
                model = new Model.Pattern() { No = LastestSelectedPatternNo };

            this.PatternForRun = new PatternViewModel(this, model);
            this.PatternForEdit = new PatternViewModel(this, model);
            PatternForEdit.PatternChanged += (s, e) =>
            {
                if (PatternForEdit.No == PatternForRun.No)
                {
                    if (CleanOvenChamber.IsConnected)
                    {
                        if (!CleanOvenChamber.IsRunning)
                        {
                            PatternForRun.Load(PatternForEdit.Model);
                            CleanOvenChamber.TransferPattern(PatternForEdit);
                        }
                        else
                        {
                            if (PatternForEdit.WaitTemperatureAfterClose != PatternForRun.WaitTemperatureAfterClose)
                            {
                                CleanOvenChamber.TransferPatternWaitTemperatureAfterClose(PatternForEdit);
                            }
                        }
                    }
                }
            };

            // 패턴 메타데이터 로딩...
            using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\pattern_meta.json", Ch)))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    json = JToken.ReadFrom(Jr);
                }
            }
            this.PatternMetaDatas = new List<PatternMetadata>();
            JToken[] metaDatas = json["pattern_metadata"].ToArray();
            foreach (JToken Data in metaDatas)
            {
                PatternMetaDatas.Add(new PatternMetadata() {
                    No = (int)Data["no"],
                    Name = (string)Data["name"],
                    IsAssigned = (int)Data["assigned"] == 1,
                    Description = (string)Data["description"],
                    RegisteredScanCode = (string)Data["registered_scancode"]
                });
            }

            MonitorTimeWatch = new System.Diagnostics.Stopwatch();
            SystemTimer = new System.Timers.Timer() { Interval = 1000 };
            SystemTimer.Elapsed += (s, e) => {
                this.MonitorElapsedTime = MonitorTimeWatch.Elapsed.ToElapaseTimeFormat();
                RaisePropertiesChanged("MonitorElapsedTime");
            };
            SystemTimer.Start();
        }

        private System.Timers.Timer SystemTimer;
        private System.Diagnostics.Stopwatch MonitorTimeWatch;
        public string CleanOvenIpAddr { get; private set; }
        public int CleanOvenTcp { get; private set; }
        public int CleanOvenLatencyQueryInterval { get; private set; }
        public string CleanOvenLatencyQueryItems { get; private set; }
        public string[] LatencyQueryItems { get; private set; }
        public byte AnalyzerId { get; private set; }
        public string AnalyzerPort { get; private set; }
        public int AnalyzerBaudrate { get; private set; }
        public string PatternStorageDir { get; private set; }
        public int LastestSelectedPatternNo { get; private set; }
        public string CsvLogStorageDir { get; private set; }
        public int CsvLogSaveInterval { get; private set; }
        public string BinaryLogStorageDir { get; private set; }
        public int BinaryLogSaveInterval { get; private set; }
        public string AlarmStorageDir { get; private set; }

        public bool CleanOvenCommErrorUserVerify;
        public string MonitorElapsedTime { get; private set; }
        public int No { get; private set; }
        public DaesungEntCleanOven4.ViewModel.ViewType ViewType { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenCommCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand CloseCommCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenOperationViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenPatternConfigurationViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenManualControlViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenIoListViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenRealtimeTrendViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenLogDataViewerCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenCurrentAlarmViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenAlarmHistoryViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenBarcodeScanViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenSensorRangeSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenSensorParameterSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenAutoTuneSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenAlertParameterSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenTrendSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenDifferenceChammberInitSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenChangePaswordCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenPLCCommSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenAnalyzerCommSetupCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand MoveToDetailViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand BackToIntegrateViewCommand { get; private set; }

        public System.Windows.Window NotifyDlg;
        public bool IsConnected => CleanOvenChamber.IsConnected/* && Analyzer.IsOpen*/;
        public Equipment.CleanOven CleanOvenChamber { get; private set; }
        public Equipment.O2Analyzer Analyzer { get; private set; }
        public PatternViewModel PatternForEdit { get; private set; }
        public PatternViewModel PatternForRun { get; private set; }
        public List<Model.PatternMetadata> PatternMetaDatas { get; private set; }
        public event EventHandler DetailViewMoveRequested;
        public event EventHandler IntegrateViewReturnRequested;

        public void Dispose()
        {
            if (CleanOvenChamber != null && CleanOvenChamber.IsConnected)
                CleanOvenChamber.DisConnect();
            if (Analyzer != null && Analyzer.IsOpen)
                Analyzer.Close();
        }
        public async void OpenComm()
        {
           // View.ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", string.Format("채널.{0} - 통신 연결 및 데이터 조회 중...", No));
            await Task.Run(() => {

                try
                {
                    if (CleanOvenChamber != null && !CleanOvenChamber.IsConnected)
                    {
                        ManualResetEvent Waitor = new ManualResetEvent(false);
                        void OnConnectionChanged(object sender, EventArgs e)
                        {
                            if (CleanOvenChamber.IsConnected)
                            {
                                CleanOvenCommErrorUserVerify = false;
                                Waitor.Set();
                            }
                        }
                        CleanOvenChamber.ConnectionChanged += OnConnectionChanged;
                        CleanOvenChamber.Connect();
                        if (!Waitor.WaitOne(3000))
                            Log.Logger.Dispatch("i", "채널.{0} - CleanOvenChamber Connection Timeout", No);
                        CleanOvenChamber.ConnectionChanged -= OnConnectionChanged;
                    }

                    if (Analyzer != null && !Analyzer.IsOpen)
                    {
                        if (Analyzer.Open())
                            Analyzer.StartMonitor();
                    }

                    RaisePropertiesChanged("IsConnected");
                }
                catch (Exception ex)
                {
                    Log.Logger.Dispatch("e", "채널.{0} - Exception is Occured while to OpenComm() : {1}", No, ex.Message);
                }
            });

           // View.ProgressWindow.CloseWindow();
        }
        private bool CanOpenComm()
        {
            return (CleanOvenChamber != null && !CleanOvenChamber.IsConnected) || (Analyzer != null && !Analyzer.IsOpen);
        }
        public async void CloseComm()
        {
            //View.Question Q = new View.Question(string.Format("채널.{0} - 통신 연결을 해제하시겠습니까?", No));
//             if (!(bool)Q.ShowDialog())
//                 return;

//            View.ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", string.Format("채널.{0} - 통신 연결 해제 중...", No));
            await Task.Run(() =>
            {
                if (CleanOvenChamber != null && CleanOvenChamber.IsConnected)
                    CleanOvenChamber.DisConnect();
                if (Analyzer != null && Analyzer.IsOpen)
                    Analyzer.Close();
            });
//            View.ProgressWindow.CloseWindow();
        }
        private bool CanCloseComm()
        {
            return (CleanOvenChamber != null && CleanOvenChamber.IsConnected) || (Analyzer != null && Analyzer.IsOpen);
        }
        private void OpenOperationView()
        {
            this.ViewType = ViewType.SystemOperateStatus;
            RaisePropertiesChanged("ViewType");
        }
        private bool CanOpenOperationView()
        {
            return true;
        }
        private void OpenEmptyView()
        {
            ViewType = ViewType.None;
            RaisePropertiesChanged("ViewType");
        }
        private void OpenPatternConfigurationView()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                Q.ShowDialog();
                return;
            }

            this.ViewType = ViewType.PatternConfiguration;
            RaisePropertiesChanged("ViewType");
        }
        private bool CanOpenPatternConfigurationView()
        {
            return true;
        }
        private void OpenBarcodeScanView()
        {
            View.PatternSelectDlgBarcode Wind = new View.PatternSelectDlgBarcode() { DataContext = this };
            if ((bool)Wind.ShowDialog())
            {
                if (Wind.PatternNo != null)
                {
                    int No = (int)Wind.PatternNo;
                    SelectRunningPattern(No);
                }
            }
        }
        private bool CanOpenBarcodeScanView()
        {
            return CleanOvenChamber.IsConnected && !CleanOvenChamber.IsRunning;
        }
        private void OpenManualControlView()
        {
            View.ManualControlDlg Dlg = new View.ManualControlDlg() { DataContext = CleanOvenChamber };
            _ = Dlg.ShowDialog();
        }
        private bool CanOpenManualControlView()
        {
            return CleanOvenChamber.IsConnected && !CleanOvenChamber.IsRunning;
        }
        private void OpenIoListView()
        {
            View.IoListDlg Dlg = new View.IoListDlg() { DataContext = CleanOvenChamber };
            _ = Dlg.ShowDialog();
        }
        private bool CanOpenIoListView()
        {
            return true;
        }
        private void OpenRealtimeTrendView()
        {
            this.ViewType = ViewType.RealtimeTrendGraph;
            RaisePropertiesChanged("ViewType");
        }
        private bool CanOpenRealtimeTrendView()
        {
            return true;
        }
        private void OpenLogDataViewer()
        {
            if (!System.IO.File.Exists(@".\DaesungEntCleanOvenDataViewer.exe"))
            {
                View.Question Q = new View.Question("로드 데이터 뷰어 프로그램이 설치되어 있지 않습니다.");
                _ = Q.ShowDialog();
                return;
            }
            System.Diagnostics.Process ps = new System.Diagnostics.Process();
            ps.StartInfo.FileName = "DaesungEntCleanOvenDataViewer.exe";
            ps.StartInfo.WorkingDirectory = this.BinaryLogStorageDir;
            ps.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
            _ = ps.Start();
        }
        private bool CanOpenLogDataViewer()
        {
            return true;
        }
        private void OpenCurrentAlarmView()
        {
            if (CleanOvenChamber.AlarmDlg != null)
                return;

            View.AlarmRealtimeDlg Dlg = new View.AlarmRealtimeDlg() { DataContext = CleanOvenChamber };
            _ = Dlg.ShowDialog();
        }
        private bool CanOpenCurrentAlarmView()
        {
            return true;
        }
        private void OpenAlarmHistoryView()
        {
            View.AlarmHistoryDlg Dlg = new View.AlarmHistoryDlg() { DataContext = CleanOvenChamber };
            _ = Dlg.ShowDialog();
        }
        private bool CanOpenAlarmHistoryView()
        {
            return true;
        }
        private void OpenSensorRangeSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                Q.ShowDialog();
                return;
            }

            View.ParameterRangeSetupDlg Dlg = new View.ParameterRangeSetupDlg() { DataContext = CleanOvenChamber };
            _ = CleanOvenChamber.WriteWord("D8900", 1);
            Dlg.ShowDialog();
            _ = CleanOvenChamber.WriteWord("D8900", 0);
        }
        private bool CanOpenSensorRangeSetup()
        {
            return CleanOvenChamber.IsConnected && !CleanOvenChamber.IsRunning;
        }
        private void OpenSensorParameterSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }

            View.ParameterZoneSetupDlg Dlg = new View.ParameterZoneSetupDlg() { DataContext = CleanOvenChamber };
            _ = CleanOvenChamber.WriteWord("D8901", 1);
            Dlg.ShowDialog();
            _ = CleanOvenChamber.WriteWord("D8901", 0);
        }
        private bool CanOpenSensorParameterSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        private void OpenAutoTuneSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }

            View.AutoTuneDlg Dlg = new View.AutoTuneDlg() { DataContext = CleanOvenChamber };
            _ = Dlg.ShowDialog();
        }
        private bool CanOpenAutoTuneSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        private void OpenAlertParameterSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }

            View.AlarmParameterSetupDlg Dlg = new View.AlarmParameterSetupDlg() { DataContext = CleanOvenChamber };
            _ = Dlg.ShowDialog();
        }
        private bool CanOpenAlertParameterSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        private void OpenTrendSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }

            View.TrendSaveIntervalSetupDlg Dlg = new View.TrendSaveIntervalSetupDlg(BinaryLogSaveInterval);
            if((bool)Dlg.ShowDialog())
            {
                this.BinaryLogSaveInterval = Dlg.SaveInterval;
                SaveSystemConfig();
            }
        }
        private bool CanOpenTrendSetup()
        {
            return true;
        }
        private void OpenDifferenceChammberInitSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }

            View.DifferenceChamberInitSetupDlg Dlg = new View.DifferenceChamberInitSetupDlg() { DataContext = CleanOvenChamber };
            _ = CleanOvenChamber.WriteWord("D8902", 1);
            Dlg.ShowDialog();
            _ = CleanOvenChamber.WriteWord("D8902", 0);
        }
        private bool CanOpenDifferenceChammberInitSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        private void OpenChangePasword()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }

            View.PasswordSettingDlg Dlg = new View.PasswordSettingDlg();
            if ((bool)Dlg.ShowDialog())
            {
                Settings.Default.UserPassword = Dlg.NewPassword;
                Settings.Default.Save();
            }
        }
        private bool CanOpenChangePasword()
        {
            return true;
        }
        private void OpenPLCCommSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }
        }
        private bool CanOpenPLCCommSetup()
        {
            return true;
        }
        private void OpenAnalyzerCommSetup()
        {
            View.Password pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                View.Question Q = new View.Question("패스워드 불일치");
                _ = Q.ShowDialog();
                return;
            }
        }
        private bool CanOpenAnalyzerCommSetup()
        {
            return true;
        }
        private void MoveToDetailView()
        {
            DetailViewMoveRequested?.Invoke(this, EventArgs.Empty);
        }
        private void BackToIntegrateView()
        {
            IntegrateViewReturnRequested?.Invoke(this, EventArgs.Empty);
        }
        private void Analyzer_MonitorDataUpdated(object sender, EventArgs e)
        {
            CleanOvenChamber.UpdateAnalyzerTemperature(Analyzer.SensorTemperature);
            CleanOvenChamber.UpdateAnalyzerEmf(Analyzer.SensorEMF);
            CleanOvenChamber.UpdateAnalyzerConcentrationPpm(Analyzer.O2ConcentrationPpm);
        }
        private void SaveSystemConfig()
        {
            try
            {
                // 설정 정보 저장.
                JToken json;
                using (StreamReader Sr = System.IO.File.OpenText(string.Format(@".\conf_{0}\system.json", this.No)))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        json = JToken.ReadFrom(Jr);
                    }
                }
                json["pattern"]["dir"] = PatternStorageDir;
                json["pattern"]["last_selected_pattern"] = LastestSelectedPatternNo;
                json["log"]["csv"]["dir"] = CsvLogStorageDir;
                json["log"]["csv"]["intv"] = CsvLogSaveInterval;
                json["log"]["binary"]["dir"] = BinaryLogStorageDir;
                json["log"]["binary"]["intv"] = BinaryLogSaveInterval;
                json["log"]["alarm"]["dir"] = AlarmStorageDir;
                File.WriteAllText(string.Format(@".\conf_{0}\system.json", this.No), json.ToString());
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Save system config : " + ex.Message);
            }
        }

        public void SelectRunningPattern(int No)
        {
            try
            {
                if (No == PatternForRun.No)
                    return;

                string Message = string.Format("현재 패턴번호 : {0}\r\n변경 패턴번호 : {1}\r\n패턴을 변경 하겠습니까?", PatternForRun.No, No);
                View.Question Q = new View.Question(Message);
                if (!(bool)Q.ShowDialog())
                    return;

                Model.Pattern pattern;
                string path = Path.Combine(this.PatternStorageDir, string.Format("{0:D3}.xml", No));
                if (File.Exists(path))
                    pattern = Model.Pattern.LoadFrom(path);
                else
                    pattern = new Model.Pattern(true) { No = No };

                if (pattern != null)
                {
                    PatternForRun.Load(pattern);
                    PatternForEdit.Load(pattern);
                    LastestSelectedPatternNo = No;
                    SaveSystemConfig();
                    CleanOvenChamber.TransferPattern(PatternForRun);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Load pattern config : " + ex.Message);
            }
        }
        public void SelectEditPattern(int No)
        {
            try
            {
                if (No == PatternForEdit.No)
                    return;

                string Message = string.Format("현재 패턴번호 : {0}\r\n변경 패턴번호 : {1}\r\n패턴을 변경 하겠습니까?", PatternForEdit.No, No);
                View.Question qDlg = new View.Question(Message);
                if (!(bool)qDlg.ShowDialog())
                    return;

                Model.Pattern pattern;
                string path = Path.Combine(PatternStorageDir, string.Format("{0:D3}.xml", No));
                if (File.Exists(path))
                    pattern = Model.Pattern.LoadFrom(path);
                else
                    pattern = new Model.Pattern(true) { No = No };

                if (pattern != null)
                {
                    PatternForEdit.Load(pattern);
                    PatternMetaDatas[No - 1].Name = PatternForEdit.Name;
                    PatternMetaDatas[No - 1].IsAssigned = true;
                    SavePatternMetaData();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Load Pattern Config : " + ex.Message);
            }
        }
        public void SavePatternMetaData()
        {
            try
            {
                JsonObjectCollection root = new JsonObjectCollection();
                JsonArrayCollection jacServer = new JsonArrayCollection("pattern_metadata");
                foreach (PatternMetadata metaData in PatternMetaDatas)
                {
                    JsonObjectCollection joc = new JsonObjectCollection {
                                 new JsonNumericValue("no", metaData.No),
                                 new JsonStringValue("name", metaData.Name),
                                 new JsonNumericValue("assigned", metaData.IsAssigned ? 1 : 0),
                                 new JsonStringValue("description", metaData.Description),
                                 new JsonStringValue("registered_scancode", metaData.RegisteredScanCode)
                             };
                    jacServer.Add(joc);
                    metaData.Invalidate();
                }
                root.Add(jacServer);
                string strRoot = root.ToString();
                File.WriteAllText(string.Format(@".\conf_{0}\pattern_meta.json", No), strRoot);
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Save pattern metadata : " + ex.Message);
            }
        }
    }
}
