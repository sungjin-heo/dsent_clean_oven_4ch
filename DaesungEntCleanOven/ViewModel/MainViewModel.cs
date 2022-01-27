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
using DaesungEntCleanOven.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DevExpress.Mvvm;
using Util;
using Microsoft.Win32;

namespace DaesungEntCleanOven.ViewModel
{
    enum ViewType
    {
        None,
        SystemOperateStatus,
        PatternConfiguration,
        RealtimeTrendGraph,
    }

    class MainViewModel : DevExpress.Mvvm.ViewModelBase
    {
        protected System.Timers.Timer SystemTimer;
        protected System.Diagnostics.Stopwatch MonitorTimeWatch;
        protected System.Timers.Timer LogRemoveTimer;

        public MainViewModel()
        {
            this.AppCaption = Resources.AppCaption;
            this.AppVersion = string.Format("v{0}", Resources.AppVersion);
            this.InitializedCommand = new DevExpress.Mvvm.DelegateCommand<object>(Initialized);
            this.QuitCommand = new DevExpress.Mvvm.DelegateCommand<CancelEventArgs>(Quit);
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

            this.CleanOvenChamber = G.CleanOven;
            this.CleanOvenChamber.ConnectionChanged += (s, e) => {
                DispatcherService.BeginInvoke(() => {
                    switch (CleanOvenChamber.ConnectState)
                    {
                        case Comm.ConnectionState.Closed:
                        case Comm.ConnectionState.Connecting:
                            OpenEmptyView();
                            break;
                        case Comm.ConnectionState.Connected:
                            OpenOperationView();
                            break;
                        case Comm.ConnectionState.Retry:
                            {
                                if (this.NotifyDlg == null && !CleanOvenCommErrorUserVerify)
                                {
                                    this.NotifyDlg = new View.Question("PLC 통신이 끊어져 재 연결을 시도합니다...");
                                    if ((bool)this.NotifyDlg.ShowDialog())
                                        CleanOvenCommErrorUserVerify = true;
                                    this.NotifyDlg = null;
                                }
                            }
                            OpenEmptyView();
                            break;
                    }
                });
            };
            this.CleanOvenChamber.Started += (s, e) => { MonitorTimeWatch.Restart(); };
            this.CleanOvenChamber.Stopped += (s, e) => { MonitorTimeWatch.Stop(); };
            this.Analyzer = G.O2Analyzer;
            this.Analyzer.MonitorDataUpdated += Analyzer_MonitorDataUpdated;
            this.Analyzer.Connected += (s, e) => { this.CleanOvenChamber.O2AnalyzerConnected(); };
            this.Analyzer.DisConnected += (s, e) =>
            {
                this.CleanOvenChamber.O2AnalyzerDisConnected();
                DispatcherService.BeginInvoke(() => {
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
            this.PatternForEdit = G.PatternForEdit;

            MonitorTimeWatch = new System.Diagnostics.Stopwatch();
            SystemTimer = new System.Timers.Timer() { Interval = 1000 };
            SystemTimer.Elapsed += (s, e) => {
                this.SystemTime = e.SignalTime.ToSystemTimeFormat();
                this.MonitorElapsedTime = MonitorTimeWatch.Elapsed.ToElapaseTimeFormat();
                RaisePropertiesChanged("SystemTime", "MonitorElapsedTime");
            };
            SystemTimer.Start();

            LogRemoveTimer = new System.Timers.Timer();
            LogRemoveTimer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
            LogRemoveTimer.Elapsed += (s, e) =>
            {
                try
                {
                    DateTime Today = DateTime.Now;
                    string[] files = System.IO.Directory.GetFiles(@"D:\DAESUNG-ENT\CLEAN_OVEN\LOG\SYS");
                    foreach (string f in files)
                    {
                        string fName = f.Substring(f.LastIndexOf('\\') + 1, f.Length - f.LastIndexOf('\\') - 1);
                        string[] Tokens = fName.Split('-');
                        if (Tokens.Length == 4)
                        {
                            DateTime Tmp = new DateTime(int.Parse(Tokens[1]), int.Parse(Tokens[2]), int.Parse(Tokens[3]));
                            if ((Today - Tmp).TotalDays > 60)
                                System.IO.File.Delete(f);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Logger.Dispatch("e", "Exception is Occured in LogRemover Handler : " + ex.Message);
                }
            };
            LogRemoveTimer.Start();
            
            //SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;            
        }

        Microsoft.Win32.PowerModes SystemPowerMode;

        protected void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            this.SystemPowerMode = e.Mode;
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    {
                        Log.Logger.Dispatch("i", "SystemPowerMode Suspend...");
                        if (CleanOvenChamber != null)
                            CleanOvenChamber.StopMonitor();
                        if (Analyzer != null)
                            Analyzer.StopMonitor();
                    }
                    break;

                case PowerModes.Resume:
                    {
                        Log.Logger.Dispatch("i", "SystemPowerMode Resume...");
                        if (CleanOvenChamber != null)
                            CleanOvenChamber.StartMonitor();
                        if (Analyzer != null)
                            Analyzer.StartMonitor();
                    }
                    break;

                case PowerModes.StatusChange:
                    break;
            }
        }

        public bool CleanOvenCommErrorUserVerify;
        public string AppCaption { get; protected set; }
        public string AppVersion { get; protected set; }
        public string SystemTime { get; protected set; }
        public string MonitorElapsedTime { get; protected set; }
        public ViewType ViewType { get; protected set; }
        public ISplashScreenService SplashScreenService { get { return GetService<ISplashScreenService>(); } }       
        public IDispatcherService DispatcherService { get { return GetService<IDispatcherService>(); } }
        public DevExpress.Mvvm.DelegateCommand<object> InitializedCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand<CancelEventArgs> QuitCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenCommCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand CloseCommCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenOperationViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenPatternConfigurationViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenManualControlViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenIoListViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenRealtimeTrendViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenLogDataViewerCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenCurrentAlarmViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenAlarmHistoryViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenBarcodeScanViewCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenSensorRangeSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenSensorParameterSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenAutoTuneSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenAlertParameterSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenTrendSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenDifferenceChammberInitSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenChangePaswordCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenPLCCommSetupCommand { get; protected set; }
        public DevExpress.Mvvm.DelegateCommand OpenAnalyzerCommSetupCommand { get; protected set; }
        public System.Windows.Window NotifyDlg;
        public Equipment.CleanOven CleanOvenChamber { get; protected set; }             // REF. in "G"
        public Equipment.O2Analyzer Analyzer { get; protected set; }                    // REF. in "G"
        public PatternViewModel PatternForEdit { get; protected set; }                  // REF. in "G.PatternForRun"

        protected void Initialized(object e)
        {
            OnInit();
        }
        protected void Quit(CancelEventArgs e)
        {
            var Dlg = new View.Question("프로그램을 종료하시겠습니까?");
            if ((bool)Dlg.ShowDialog())
            {
                OnClose();
                App.Current.Shutdown();
            }
            else
            {
                e.Cancel = true;
            }
        }
        protected void OnInit()
        {
            try
            {
                SplashScreenService.ShowSplashScreen();
                SplashScreenService.SetSplashScreenState("Initializing ...");
                System.Threading.Thread.Sleep(1000);

                SplashScreenService.SetSplashScreenState("Create System Instances...");
                System.Threading.Thread.Sleep(1000);

                SplashScreenService.SetSplashScreenState("Create System Directories...");
                if (!Directory.Exists(G.AlarmStorageDir))
                    Directory.CreateDirectory(G.AlarmStorageDir);
                if (!Directory.Exists(G.BinaryLogStorageDir))
                    Directory.CreateDirectory(G.BinaryLogStorageDir);
                if (!Directory.Exists(G.CsvLogStorageDir))
                    Directory.CreateDirectory(G.CsvLogStorageDir);
                if (!Directory.Exists(G.PatternStorageDir))
                    Directory.CreateDirectory(G.PatternStorageDir);
            }
            catch (Exception ex)
            {
                SplashScreenService.SetSplashScreenState("System Initialize Failed.");
            }
            finally
            {
                SplashScreenService.HideSplashScreen();
            }
        }
        protected void OnClose()
        {
            if (CanCloseComm())
                CloseComm();
        }
        protected async void OpenComm()
        {
            View.ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", "통신 연결 및 데이터 조회 중...");
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
                            Log.Logger.Dispatch("i", "CleanOvenChamber Connection Timeout");
                        CleanOvenChamber.ConnectionChanged -= OnConnectionChanged;
                    }
                    if (Analyzer != null && !Analyzer.IsOpen)
                    {
                        if (Analyzer.Open())
                            Analyzer.StartMonitor();
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Dispatch("e", "Exception is Occured while to OpenComm() : " + ex.Message);
                }
            });
            View.ProgressWindow.CloseWindow();
        }
        protected bool CanOpenComm()
        {
            return ((CleanOvenChamber != null && !CleanOvenChamber.IsConnected) && (Analyzer != null && !Analyzer.IsOpen));
        }
        protected async void CloseComm()
        {
            var qDlg = new View.Question("통신 연결을 해제하시겠습니까?");
            if (!(bool)qDlg.ShowDialog())
                return;

            View.ProgressWindow.ShowWindow("대성ENT - N2 CLEAN OVEN", "통신 연결 해제 중...");
            await Task.Run(() => {
                if (CleanOvenChamber != null && CleanOvenChamber.IsConnected)
                    CleanOvenChamber.DisConnect();
                if (Analyzer != null && Analyzer.IsOpen)
                    Analyzer.Close();
            });
            View.ProgressWindow.CloseWindow();
        }
        protected bool CanCloseComm()
        {
            return ((CleanOvenChamber != null && CleanOvenChamber.IsConnected) || (Analyzer != null && Analyzer.IsOpen));
        }
        protected void OpenOperationView()
        {
            this.ViewType = ViewType.SystemOperateStatus;
            RaisePropertiesChanged("ViewType");
        }
        protected bool CanOpenOperationView()
        {
            return true;
        }
        protected void OpenEmptyView()
        {
            ViewType = ViewType.None;
            RaisePropertiesChanged("ViewType");
        }
        protected void OpenPatternConfigurationView()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            this.ViewType = ViewType.PatternConfiguration;
            RaisePropertiesChanged("ViewType");
        }
        protected bool CanOpenPatternConfigurationView()
        {
            return true;
        }
        protected void OpenBarcodeScanView()
        {
            var Dlg = new View.PatternSelectDlgBarcode();
            if ((bool)Dlg.ShowDialog())
            {
                if(Dlg.PatternNo != null)
                {
                    int No = (int)Dlg.PatternNo;
                    G.SelectRunningPattern(No);
                }
            }
        }
        protected bool CanOpenBarcodeScanView()
        {
            return CleanOvenChamber.IsConnected && !CleanOvenChamber.IsRunning;
        }
        protected void OpenManualControlView()
        {
            var Dlg = new View.ManualControlDlg() { DataContext = CleanOvenChamber };
            Dlg.ShowDialog();
        }
        protected bool CanOpenManualControlView()
        {
            return CleanOvenChamber.IsConnected && !CleanOvenChamber.IsRunning;
        }
        protected void OpenIoListView()
        {
            var Dlg = new View.IoListDlg() { DataContext = CleanOvenChamber };
            Dlg.ShowDialog();
        }
        protected bool CanOpenIoListView()
        {
            return true;
        }
        protected void OpenRealtimeTrendView()
        {
            this.ViewType = ViewType.RealtimeTrendGraph;
            RaisePropertiesChanged("ViewType");
        }
        protected bool CanOpenRealtimeTrendView()
        {
            return true;
        }
        protected void OpenLogDataViewer()
        {
            if (!System.IO.File.Exists(@".\DaesungEntCleanOvenDataViewer.exe"))
            {
                var qDlg = new View.Question("Log Viewer App is not Exist");
                qDlg.ShowDialog();
                return;
            }
            System.Diagnostics.Process ps = new System.Diagnostics.Process();
            ps.StartInfo.FileName = "DaesungEntCleanOvenDataViewer.exe";
            ps.StartInfo.WorkingDirectory = @".\";
            ps.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
            ps.Start();
        }
        protected bool CanOpenLogDataViewer()
        {
            return true;
        }
        protected void OpenCurrentAlarmView()
        {
            if (CleanOvenChamber.AlarmDlg != null)
                return;

            var Dlg = new View.AlarmRealtimeDlg() { DataContext = CleanOvenChamber };
            Dlg.ShowDialog();
        }
        protected bool CanOpenCurrentAlarmView()
        {
            return true;
        }
        protected void OpenAlarmHistoryView()
        {
            var Dlg = new View.AlarmHistoryDlg() { DataContext = CleanOvenChamber };
            Dlg.ShowDialog();
        }
        protected bool CanOpenAlarmHistoryView()
        {
            return true;
        }
        protected void OpenSensorRangeSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }                

            var Dlg = new View.ParameterRangeSetupDlg() { DataContext = CleanOvenChamber };
            CleanOvenChamber.WriteWord("D8900", 1);
            Dlg.ShowDialog();
            CleanOvenChamber.WriteWord("D8900", 0);
        }
        protected bool CanOpenSensorRangeSetup()
        {
            return CleanOvenChamber.IsConnected && !CleanOvenChamber.IsRunning;
        }
        protected void OpenSensorParameterSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            var Dlg = new View.ParameterZoneSetupDlg() { DataContext = CleanOvenChamber };
            CleanOvenChamber.WriteWord("D8901", 1);
            Dlg.ShowDialog();
            CleanOvenChamber.WriteWord("D8901", 0);
        }
        protected bool CanOpenSensorParameterSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        protected void OpenAutoTuneSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            var Dlg = new View.AutoTuneDlg() { DataContext = CleanOvenChamber };
            Dlg.ShowDialog();
        }
        protected bool CanOpenAutoTuneSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        protected void OpenAlertParameterSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            var Dlg = new View.AlarmParameterSetupDlg() { DataContext = CleanOvenChamber };
            Dlg.ShowDialog();
        }
        protected bool CanOpenAlertParameterSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        protected void OpenTrendSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            var Dlg = new View.TrendSaveIntervalSetupDlg(G.BinaryLogSaveInterval);
            if((bool)Dlg.ShowDialog())
            {
                G.BinaryLogSaveInterval = Dlg.SaveInterval;
                G.SaveSystemConfig();
            }
        }
        protected bool CanOpenTrendSetup()
        {
            return true;
        }
        protected void OpenDifferenceChammberInitSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            var Dlg = new View.DifferenceChamberInitSetupDlg() { DataContext = CleanOvenChamber };
            CleanOvenChamber.WriteWord("D8902", 1);
            Dlg.ShowDialog();
            CleanOvenChamber.WriteWord("D8902", 0);
        }
        protected bool CanOpenDifferenceChammberInitSetup()
        {
            return CleanOvenChamber.IsConnected;
        }
        protected void OpenChangePasword()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }

            var Dlg = new View.PasswordSettingDlg();
            if ((bool)Dlg.ShowDialog())
            {
                Settings.Default.UserPassword = Dlg.NewPassword;
                Settings.Default.Save();
            }
        }
        protected bool CanOpenChangePasword()
        {
            return true;
        }
        protected void OpenPLCCommSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }
        }
        protected bool CanOpenPLCCommSetup()
        {
            return true;
        }
        protected void OpenAnalyzerCommSetup()
        {
            var pwDlg = new View.Password();
            if (!(bool)pwDlg.ShowDialog())
                return;

            string Password = pwDlg.UserPassword.ToUpper();
            if (Password != Resources.AdministratorPassword && Password != Settings.Default.UserPassword)
            {
                var qDlg = new View.Question("패스워드 불일치");
                qDlg.ShowDialog();
                return;
            }
        }
        protected bool CanOpenAnalyzerCommSetup()
        {
            return true;
        }
        protected void Analyzer_MonitorDataUpdated(object sender, EventArgs e)
        {
            CleanOvenChamber.UpdateAnalyzerTemperature(Analyzer.SensorTemperature);
            CleanOvenChamber.UpdateAnalyzerEmf(Analyzer.SensorEMF);
            CleanOvenChamber.UpdateAnalyzerConcentrationPpm(Analyzer.O2ConcentrationPpm);
        }
    }
}
