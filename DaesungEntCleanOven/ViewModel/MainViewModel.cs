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
using DaesungEntCleanOven4.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DevExpress.Mvvm;
using Util;
using Microsoft.Win32;
using DsFourChamberLib.DataComm;
using Mp.Lib.IO;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace DaesungEntCleanOven4.ViewModel
{
    internal class MainViewModel : DevExpress.Mvvm.ViewModelBase
    {
        public MainViewModel()
        {
            this.AppCaption = Resources.AppCaption;
            this.AppVersion = string.Format("v{0}", Resources.AppVersion);
            this.Channels = new List<ChannelViewModel>();
            this.InitializedCommand = new DevExpress.Mvvm.DelegateCommand<object>(Initialized);
            this.QuitCommand = new DevExpress.Mvvm.DelegateCommand<CancelEventArgs>(Quit);
            this.OpenCommCommand = new DevExpress.Mvvm.DelegateCommand(OpenComm, CanOpenComm);
            this.CloseCommCommand = new DevExpress.Mvvm.DelegateCommand(CloseComm, CanCloseComm);
            this.MoveToChannelViewCommand = new DelegateCommand<object>(MoveToChannelView);
            this.AlarmResetCommand = new DevExpress.Mvvm.DelegateCommand(AlarmReset, CanAlarmReset);
            this.BuzzerStopCommand = new DevExpress.Mvvm.DelegateCommand(BuzzerStop, CanBuzzerStop);
            this.MoveToIntegratedViewCommand = new DelegateCommand(MoveToIntegratedView, CanMoveToIntegratedView);

            SystemTimer = new System.Timers.Timer() { Interval = 1000 };
            SystemTimer.Elapsed += (s, e) => {
                this.SystemTime = e.SignalTime.ToSystemTimeFormat();
                RaisePropertiesChanged("SystemTime");
            };
            SystemTimer.Start();

#if false
            LogRemoveTimer = new System.Timers.Timer();
            LogRemoveTimer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
            LogRemoveTimer.Elapsed += (s, e) =>
            {
                try
                {
                    DateTime Today = DateTime.Now;
                    string[] files = System.IO.Directory.GetFiles(@"D:\APP\DAESUNG-ENT\CLEAN_OVEN\LOG");
                    foreach (string f in files)
                    {
                        string fName = f.Substring(f.LastIndexOf('\\') + 1, f.Length - f.LastIndexOf('\\') - 1);
                        string[] Tokens = fName.Split('-');
                        if (Tokens.Length == 4)
                        {
                            DateTime Tmp = new DateTime(int.Parse(Tokens[1]), int.Parse(Tokens[2]), int.Parse(Tokens[3]));
                            if ((Today - Tmp).TotalDays > 30)
                            {
                                System.IO.File.Delete(f);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Dispatch("e", "Exception is Occured in LogRemover Handler : " + ex.Message);
                }
            };
            LogRemoveTimer.Start(); 
#endif
        }

        private System.Timers.Timer SystemTimer;
        private System.Timers.Timer LogRemoveTimer;
        private System.Threading.Tasks.Task MpMessageProcTask;
        private System.Threading.CancellationTokenSource MpMessageProcTaskCancelSource;
        private System.Timers.Timer CommOpenTimer;

        public ISplashScreenService SplashScreenService => GetService<ISplashScreenService>();
        public IDispatcherService DispatcherService => GetService<IDispatcherService>();
        public DevExpress.Mvvm.DelegateCommand<object> InitializedCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand<CancelEventArgs> QuitCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenCommCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand CloseCommCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand<object> MoveToChannelViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand AlarmResetCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand BuzzerStopCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand MoveToIntegratedViewCommand { get; private set; }

        public string AppCaption { get; private set; }
        public string AppVersion { get; private set; }
        public string SystemTime { get; private set; }
        public List<ChannelViewModel> Channels { get; private set; }
        public ChannelViewModel SelectedChannel { get; private set; }
        public Equipment.O2Analyzer Analyzer { get; private set; }
        public MpMessageClient CimClient { get; private set; }
        public ConcurrentQueue<object> MpMessageQueue { get; private set; }
        public Robostar.ViewModel.EfemServer EfemServer { get; private set; }
        public event EventHandler DetailViewMoveRequested;
        public event EventHandler IntegrateViewReturnRequested;

        private void Initialized(object e)
        {
            OnInit();
        }
        private void Quit(CancelEventArgs e)
        {
            View.Question Q = new View.Question("프로그램을 종료하시겠습니까?");
            if ((bool)Q.ShowDialog())
            {
                Log.Logger.Dispatch("i", "사용자에 의한 시스템 정지...");
                OnClose();
                App.Current.Shutdown();
            }
            else
            {
                e.Cancel = true;
            }
        }
        private void OnInit()
        {
            try
            {
                SplashScreenService.ShowSplashScreen();
                SplashScreenService.SetSplashScreenState("Initializing ...");
                System.Threading.Thread.Sleep(1000);

                SplashScreenService.SetSplashScreenState("Create System Instances...");
                System.Threading.Thread.Sleep(1000);

                // 4채널 챔버 모델 생성.
                for (int i = 0; i < 4; i++)
                {
                    ChannelViewModel Ch = new ChannelViewModel(i + 1);
                    Ch.DetailViewMoveRequested += Ch_DetailViewMoveRequested;
                    Ch.IntegrateViewReturnRequested += Ch_IntegrateViewReturnRequested;
                    Ch.PatternMetaDataChanged += Ch_PatternMetaDataChanged;
                    Ch.AlarmOccured += Ch_AlarmOccured;
                    Ch.DoorOpenCompleted += Ch_DoorOpenCompleted;
                    Ch.DoorCloseCompleted += Ch_DoorCloseCompleted;
                    Ch.ProcessStarted += Ch_ProcessStarted;
                    Ch.ProcessCompleted += Ch_ProcessCompleted;
                    Ch.ProcessAborted += Ch_ProcessAborted;
                    Channels.Add(Ch);
                }

                // EFEM 서버 연동 클라이언트 생성
                JToken json;
                using (StreamReader Sr = System.IO.File.OpenText(@".\config.json"))
                {
                    using (JsonTextReader Jr = new JsonTextReader(Sr))
                    {
                        json = JToken.ReadFrom(Jr);
                    }
                }
                string Ip = (string)json["device"]["efem"]["addr"];
                int Port = (int)json["device"]["efem"]["port"];
                CimClient = new MpMessageClient(Ip, Port);
                MpMessageQueue = new ConcurrentQueue<object>();
                Ds4CommHelper.RegisterDataObject(CimClient.MessageTypes);
                CimClient.ClientConnectionChangedEvent += CimClient_ClientConnectionChangedEvent;
                CimClient.MessageReceiveEvent += CimClient_MessageReceiveEvent;
                CimClient.ReceiveErrorEvent += CimClient_ReceiveErrorEvent;
              //  CimClient.Start();

                // EFEM 서버의 상태 값을 출력하기 위한 모델.
                this.EfemServer = new Robostar.ViewModel.EfemServer();
                EfemServer.ServerIpAddress = Ip;
                EfemServer.ServerTcpPort = Port;

                // EFEM Mp 메세지 처리 태스트 시작.
                if (MpMessageProcTaskCancelSource == null)
                {
                    MpMessageProcTaskCancelSource = new CancellationTokenSource();
                }
                MpMessageProcTask = Task.Factory.StartNew(() => MpMessageHandlerFunc(MpMessageProcTaskCancelSource.Token),
                    MpMessageProcTaskCancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                // O2 뷴석기.  RS485, 2W 멀티드랍통신(디바이스 아이디 : 1 ~ 4)
                string COM = (string)json["device"]["analyzer"]["port"];
                int Baudrate = (int)json["device"]["analyzer"]["baud_rate"];
                string[] portNames = System.IO.Ports.SerialPort.GetPortNames();
                if (!portNames.Contains(COM))
                {
                    throw new Exception(string.Format("시스템에 \"{0}\" 포트가 존재하지 않습니다.", COM));
                }
                this.Analyzer = new Equipment.O2Analyzer(COM, Baudrate, 1);
                this.Analyzer.MeasureDataUpdated += Analyzer_MeasureDataUpdated;
                this.Analyzer.ConnectionStateChanged += (s, e) =>
                {
                    Channels[e.DeviceId].UpdateAnalyzerConnectionState(e.ConnectState);
                };
                this.Analyzer.Connected += (s, e) => { Analyzer.StartMonitor(); };

                Log.Logger.Dispatch("i", "Create Communication Open Timer...");
                CommOpenTimer = new System.Timers.Timer();
                CommOpenTimer.Interval = 5000;
                CommOpenTimer.Elapsed += (s, e) =>
                {
                    Log.Logger.Dispatch("i", "Communication Open Timer Callback Called.");
                    CommOpenTimer.Enabled = false;
                    Application.Current.Dispatcher.Invoke(() => OpenComm());
                };
                CommOpenTimer.Start();
            }
            catch (Exception ex)
            {
                SplashScreenService.SetSplashScreenState("System Initialize Failed : " + ex.Message);
                Thread.Sleep(3000);
            }
            finally
            {
                SplashScreenService.HideSplashScreen();
                RaisePropertiesChanged("Channels", "EfemServer");
            }
        }
        private void OnClose()
        {
            if (Analyzer != null && Analyzer.IsOpen)
            {
                Analyzer.Close();
            }
            Channels.ForEach(o => o.Dispose());

            // EFEM 연동 클라이언트 정지.
            if (CimClient != null && CimClient.IsConnected)
            {
                CimClient.Stop();
            }

            // Mp 메세지 처리 태스크 정지.
            if (MpMessageProcTaskCancelSource != null)
            {
                MpMessageProcTaskCancelSource.Cancel();
                if (MpMessageProcTask != null)
                {
                    if (MpMessageProcTask.Wait(1000))
                        MpMessageProcTask = null;
                }
                MpMessageProcTaskCancelSource = null;
            }
        }
        private async void OpenComm()
        {
            try
            {
                View.ProgressWindow.ShowWindow("대성ENT - 4CH. N2 CLEAN OVEN", "통신 연결 및 데이터 조회 중...");
                await Task.Run(() =>
                {
                    if (Analyzer != null && !Analyzer.IsOpen)
                    {
                        _ = Analyzer.Open();
                    }
                    Channels.ForEach(o => o.OpenComm());
                    if (CimClient != null)
                    {
                        CimClient.Start();
                    }
                });
            }
            finally
            {
                View.ProgressWindow.CloseWindow();
            }
        }
        private bool CanOpenComm()
        {
            return Channels.Any(o => !o.IsConnected);
        }
        private async void CloseComm()
        {
            try
            {
                View.Question Q = new View.Question("통신 연결을 해제 하시겠습니까?");
                if (!(bool)Q.ShowDialog())
                {
                    return;
                }
                Log.Logger.Dispatch("i", "사용자에 의한 통신 연결 해제...");

                View.ProgressWindow.ShowWindow("대성ENT - 4CH. N2 CLEAN OVEN", "모니터링 정지 및 통신 해제 중...");
                await Task.Run(() => 
                {
                    if (CimClient != null)
                    {
                        CimClient.Stop();
                    }
                    if (Analyzer != null && Analyzer.IsOpen)
                    {
                        Analyzer.Close();
                    }                    
                    Channels.ForEach(o => o.UpdateAnalyzerConnectionState(false));
                    Channels.ForEach(o => o.CloseComm());
                });
            }
            finally
            {
                View.ProgressWindow.CloseWindow();
            }          
        }
        private bool CanCloseComm()
        {
            return Channels.Any(o => o.IsConnected);
        }
        private void MoveToChannelView(object parameter)
        {
            int Ch = int.Parse(parameter as string);
            this.SelectedChannel = Channels[Ch - 1];
            DetailViewMoveRequested?.Invoke(this, EventArgs.Empty);
            RaisePropertiesChanged("SelectedChannel");
        }
        private void AlarmReset()
        {
            if (Channels != null && Channels.Count > 0)
            {
                ChannelViewModel Chan = Channels.FirstOrDefault(o => o.IsConnected);
                if (Chan != null)
                {
                    Chan.CleanOvenChamber.AlarmReset();
                }
            }
        }
        private bool CanAlarmReset()
        {
            if (Channels != null && Channels.Count > 0)
                return Channels.FirstOrDefault(o => o.IsConnected) != null;
            return false;
        }
        private void BuzzerStop()
        {
            if (Channels != null && Channels.Count > 0 && Channels[0].IsConnected)
            {
                ChannelViewModel Chan = Channels.FirstOrDefault(o => o.IsConnected);
                if (Chan != null)
                    Chan.CleanOvenChamber.BuzzerStop();
            }
        }
        private bool CanBuzzerStop()
        {
            if (Channels != null && Channels.Count > 0)
                return Channels.FirstOrDefault(o => o.IsConnected) != null;
            return false;
        }
        private void MoveToIntegratedView()
        {
            IntegrateViewReturnRequested?.Invoke(this, EventArgs.Empty);
        }
        private bool CanMoveToIntegratedView()
        {
            return true;
        }
        private void Ch_DetailViewMoveRequested(object sender, EventArgs e)
        {
            this.SelectedChannel = sender as ViewModel.ChannelViewModel;
            DetailViewMoveRequested?.Invoke(this, EventArgs.Empty);
            RaisePropertiesChanged("SelectedChannel");
        }
        private void Ch_IntegrateViewReturnRequested(object sender, EventArgs e)
        {
            IntegrateViewReturnRequested?.Invoke(this, EventArgs.Empty);
        }
        private void Ch_PatternMetaDataChanged(object sender, EventArgs e)
        {
            if (sender is ChannelViewModel Ch)
            {
                foreach (ChannelViewModel Channel in Channels)
                {
                    if (Channel != Ch)
                    {
                        Channel.LoadPatternMetaData();
                    }
                }
            }
        }
        private void Analyzer_MeasureDataUpdated(object sender, Equipment.MeasureDataUpdateEventArgs e)
        {
            try
            {
                ChannelViewModel Channel = Channels[e.DeviceId];
                if (Channel != null)
                {
                    Channel.CleanOvenChamber.UpdateAnalyzerTemperature(e.SensorTemperature);
                    Channel.CleanOvenChamber.UpdateAnalyzerEmf(e.SensorEMF);
                    Channel.CleanOvenChamber.UpdateAnalyzerConcentrationPpm(e.O2ConcentrationPpm);
                }
            }
            catch(Exception ex)
            {
                Log.Logger.Dispatch("e", "Analyzer_MeasureDataUpdated() Exception : " + ex.Message);
            }
        }
        private void CimClient_ClientConnectionChangedEvent(MpMessageClient arg1, bool arg2)
        {
            EfemServer.IsServerConnected = arg2;
            Log.Logger.Dispatch("i", "Robostar.CIM Connection State Changed : {0}", arg2 ? "Connected" : "DisConnected");
        }
        private void CimClient_MessageReceiveEvent(object m)
        {
            if (MpMessageQueue != null && m != null)
            {
                MpMessageQueue.Enqueue(m);
            }
        }
        private void CimClient_ReceiveErrorEvent(MpMessageClient arg1, string arg2)
        {
            Log.Logger.Dispatch("e", "Robostar.CIM Error Occured : {0}", arg2);
        }

        private void MpMessageHandlerFunc(CancellationToken Token)
        {
            try
            {
                Token.ThrowIfCancellationRequested();
                while (!Token.IsCancellationRequested)
                {
                    if (MpMessageQueue.TryDequeue(out object m))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (m.GetType() == typeof(EfemCommDataStatus))
                            {
                                EfemCommDataStatus Msg = m as EfemCommDataStatus;
                                if (Msg.IsEFEM && EfemServer != null)
                                {
                                    EfemServer.Update(Msg);
                                }
                            }
                            else if (m.GetType() == typeof(EfemCommDataSupplyPanel))
                            {
                                EfemCommDataSupplyPanel Msg = m as EfemCommDataSupplyPanel;
                                if (Msg.IsEFEM && EfemServer != null)
                                {
                                    EfemServer.Update(Msg);
                                }
                            }
                            else if (m.GetType() == typeof(EfemRequest))
                            {
                                // 요청 메세지를 받을 경우, 챔버는 반드시 EFEM 서버측으로 응답 메세지를 보내야 한다.
                                EfemRequest Msg = m as EfemRequest;
                                if (!Msg.IsEFEM)
                                {
                                    return;
                                }
                                    
                                switch (Msg.Req)
                                {
                                    case eEfemMessage.None:
                                        break;

                                    case eEfemMessage.ReqResetAlarm:
                                        {
                                            // 알람 해제 요청, EFEM에서는 채널 아이디가 넘어오나 알람해제는 공통.
                                            // 4채널 중 PLC가 연결된 설비만 알람 리셋을 한다.
                                            AlarmReset();
                                            BakeOvenResponse Reply = new BakeOvenResponse()
                                            {
                                                IsEFEM = false,
                                                MessageSeqNo = Msg.MessageSeqNo
                                            };
                                            Reply.Req = Msg.Req;
                                            Reply.Ack = eBakeOvenAck.OK;
                                            bool v = CimClient.Send(Reply);
                                        }
                                        break;

                                    case eEfemMessage.ReqRecipeList:			// 레시피 리스트 요청
                                        {
                                            // 설비가 가지고 있는 전체 레시피 리스트를 보내야 하는지, 아님, 요청된 레시피 NO에 해당하는 값만 보내면 되는지?
                                            // 레시피는 채널에 상관없이 공통으로 사용한다.
                                           
#if SEND_ALL_RECIPE
                                            // #1. 전체 레시피를 보내는 경우.
                                            BakeOvenRecipeList Reply = new BakeOvenRecipeList
                                            {
                                                IsEFEM = false,
                                                MessageSeqNo = Msg.MessageSeqNo
                                            };

                                            string dir = @"D:\APP\DAESUNG-ENT\CLEAN_OVEN\PATTERN";
                                            for (int i = 0; i < 100; i++)
                                            {
                                                string path = Path.Combine(dir, string.Format("{0:D3}.xml", i + 1));
                                                if (File.Exists(path))
                                                {
                                                    Model.Pattern pattern = Model.Pattern.LoadFrom(path);

                                                    BakeOvenRecipe recipe = new BakeOvenRecipe
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo,
                                                        RecipeNo = pattern.No,
                                                        RecipeName = pattern.Name,
                                                        Segments = new BakeOvenSegmentData[pattern.SegmentCount]
                                                    };

                                                    int k = 0;
                                                    foreach (Model.Segment Seg in pattern.Segments)
                                                    {
                                                        // 네패스 요청에 의해 Duration이 0이 아닌, 실제 사용되는 세그먼트만 전송하도록 변경.
                                                        if (Seg.Duration == 0)
                                                        {
                                                            break;
                                                        }
                                                        BakeOvenSegmentData d = new BakeOvenSegmentData
                                                        {
                                                            SegmentNo = Seg.No,
                                                            Temp = Seg.Temperature,
                                                            ChmPress = Seg.DifferencePressureChamber,
                                                            MotorChm = Seg.MotorChamber,
                                                            MotorCooling = Seg.MotorCooling,
                                                            MFC = Seg.MFC,
                                                            Time = Seg.Duration,
                                                            TS1 = Seg.TimeSignalValue
                                                        };
                                                        recipe.Segments[k++] = d;
                                                    }
                                                    Reply.Recipes.Add(recipe);
                                                }
                                            }
                                            bool v = CimClient.Send(Reply);

#endif

#if SEND_REQ_RECIPE

                                            // #2. 요청된 레시피만 보내는 경우.
                                            string recipe_name = Msg.RecipeName;
                                            int receipe_no = Msg.RecipeNo;
                                                                       
                                            string dir = @"D:\APP\DAESUNG-ENT\CLEAN_OVEN\PATTERN";
                                            string path = Path.Combine(dir, string.Format("{0:D3}.xml", receipe_no));
                                            if (File.Exists(path))
                                            {
                                                Model.Pattern pattern;
                                                pattern = Model.Pattern.LoadFrom(path);

                                                BakeOvenRecipe Reply = new BakeOvenRecipe
                                                {
                                                    IsEFEM = false,
                                                    MessageSeqNo = Msg.MessageSeqNo,
                                                    RecipeNo = pattern.No,
                                                    RecipeName = pattern.Name,
                                                    Segments = new BakeOvenSegmentData[pattern.SegmentCount]
                                                };

                                                int k = 0;
                                                foreach (Model.Segment Seg in pattern.Segments)
                                                {
                                                    BakeOvenSegmentData d = new BakeOvenSegmentData
                                                    {
                                                        SegmentNo = Seg.No,
                                                        Temp = Seg.Temperature,
                                                        ChmPress = Seg.DifferencePressureChamber,
                                                        MotorChm = Seg.MotorChamber,
                                                        MotorCooling = Seg.MotorCooling,
                                                        MFC = Seg.MFC,
                                                        Time = Seg.Duration,
                                                        TS1 = Seg.TimeSignalValue
                                                    };
                                                    Reply.Segments[k++] = d;
                                                }
                                                CimClient.Send(Reply);
                                            }
                                            else
                                            {
                                                // 패턴 파일이 없을 때...
                                                BakeOvenRecipe Reply = new BakeOvenRecipe
                                                {
                                                    IsEFEM = false,
                                                    RecipeNo = receipe_no,
                                                    RecipeName = recipe_name,
                                                    DebugMessage = "No Recipe"
                                                };
                                                bool v = CimClient.Send(Reply);
                                            }
#endif
                                        }
                                        break;

                                    case eEfemMessage.ReqStatus:				// 상태정보 요청
                                        {
                                            ChannelViewModel xCh = Channels.Find(o => o.IsConnected == false);
                                            if (xCh != null)
                                            {
                                                BakeOvenResponse reponse = new BakeOvenResponse()
                                                {
                                                    IsEFEM = false,
                                                    MessageSeqNo = Msg.MessageSeqNo
                                                };
                                                reponse.Req = Msg.Req;
                                                reponse.Ack = eBakeOvenAck.NG;
                                                reponse.ErrorCode = string.Format("PLC.#{0} Disconnected", xCh.No);
                                                _ = CimClient.Send(reponse);
                                                return;
                                            }

                                            BakeOvenCommDataStatus Reply = new BakeOvenCommDataStatus()
                                            {
                                                IsEFEM = false,
                                                MessageSeqNo = Msg.MessageSeqNo
                                            };
                                            // 설비가 채널별로 동작함으로 응답 메세지에서 아래 전역 파라미터들은 로보스타와 협의하여 채널 단으로 이동.
                                            // 채널단에 있던 IsRun은 중복되어 삭제, IsPrepared는 의미가 모호하여 삭제.

                                            // #1 챔버 데이터
                                            ChannelViewModel Ch = Channels[0];
                                            Equipment.CleanOven Chamber = Ch.CleanOvenChamber;
                                            List<RegNumeric> Values = Chamber.NumericValues;
                                            BakeOvenCommDataChamberStatusData d = new BakeOvenCommDataChamberStatusData
                                            {
                                                // d.IsReady = ;            // IsReady는 설비가 정지되어 있고 알람이 없는 상태
                                                // d.IsPrepared = ;         // 삭제.

                                                IsInitOperation = Chamber.IsInitRunning,
                                                IsRun = Chamber.IsRunning,
                                                IsStopping =Chamber.IsStopping,
                                                IsStop = Chamber.IsStop,
                                                IsAutoTune = Chamber.IsAutoTune,
                                                IsReady = Chamber.IsStop && !Chamber.IsAlarmState,
                                                IsAlarm = Chamber.IsAlarmState,
                                                IsDoorOpenAvailable = Chamber.IsDoorOpenAvailable,
                                                IsDoorOpen = Chamber.IsDoorOpen,
                                                IsDoorClosed = Chamber.IsDoorClosed,
                                                RecipeName = Ch.PatternForRun.Name,
                                                SequenceTotal = Chamber.TotalSequenceCount,
                                                SequenceNo = Chamber.CurrentSequenceNo,
                                                ProcessStartTime = Chamber.TotalSegmentElapsedTime,              // 시간 단위 => 분?
                                                ProcessTotalTime = Chamber.TotalSegmentTime,
                                                SequenceStartTime = Chamber.CurrentSegmentElapsedTime,
                                                SequenceToatlTime = Chamber.CurrentSegmentDuration,
                                                TempPV = Values[0].ScaledValue,
                                                TempSV = Values[1].ScaledValue,
                                                TempMV = Values[3].ScaledValue,
                                                ChmPressPV = Values[11].ScaledValue,
                                                ChmPressSV = Values[12].ScaledValue,
                                                ChmPressMV = Values[13].ScaledValue,
                                                ChmOverTemp = Values[27].ScaledValue,
                                                HeaterOverTemp = Values[28].ScaledValue,
                                                ChmFilterPV = Values[14].ScaledValue,
                                                MotorChmPV = Values[15].ScaledValue,
                                                MotorChmSV = Values[16].ScaledValue,
                                                MotorChmMV = Values[17].ScaledValue,
                                                MfcSV = Values[21].ScaledValue,
                                                MfcPV = Values[22].ScaledValue,
                                                MfcMV = Values[43].ScaledValue,
                                                InsideTemp1 = Values[23].ScaledValue,
                                                InsideTemp2 = Values[24].ScaledValue,
                                                O2Temp = Chamber.AnalyzerO2Temperature,
                                                O2EMF = Chamber.AnalyzerO2Emf,
                                                O2PPM = Chamber.AnalyzerO2Ppm
                                            };
                                            bool[] bits = (from alarm in Chamber.Alarms select alarm.State).ToArray();
                                            for (int i = 0; i < bits.Length; i++)
                                            {
                                                d.AlarmBits[i] = bits[i];
                                            }
                                            Reply.Chamber1 = d;

                                            // #2 챔버 데이터
                                            Ch = Channels[1];
                                            Chamber = Ch.CleanOvenChamber;
                                            Values = Chamber.NumericValues;
                                            d = new BakeOvenCommDataChamberStatusData
                                            {                                              
                                                IsInitOperation = Chamber.IsInitRunning,
                                                IsRun = Chamber.IsRunning,
                                                IsStopping = Chamber.IsStopping,
                                                IsStop = Chamber.IsStop,
                                                IsAutoTune = Chamber.IsAutoTune,
                                                IsReady = Chamber.IsStop && !Chamber.IsAlarmState,
                                                IsAlarm = Chamber.IsAlarmState,
                                                IsDoorOpenAvailable = Chamber.IsDoorOpenAvailable,
                                                IsDoorOpen = Chamber.IsDoorOpen,
                                                IsDoorClosed = Chamber.IsDoorClosed,
                                                RecipeName = Ch.PatternForRun.Name,
                                                SequenceTotal = Chamber.TotalSequenceCount,
                                                SequenceNo = Chamber.CurrentSequenceNo,
                                                ProcessStartTime = Chamber.TotalSegmentElapsedTime,              // 시간 단위 => 분?
                                                ProcessTotalTime = Chamber.TotalSegmentTime,
                                                SequenceStartTime = Chamber.CurrentSegmentElapsedTime,
                                                SequenceToatlTime = Chamber.CurrentSegmentDuration,
                                                TempPV = Values[0].ScaledValue,
                                                TempSV = Values[1].ScaledValue,
                                                TempMV = Values[3].ScaledValue,
                                                ChmPressPV = Values[11].ScaledValue,
                                                ChmPressSV = Values[12].ScaledValue,
                                                ChmPressMV = Values[13].ScaledValue,
                                                ChmOverTemp = Values[27].ScaledValue,
                                                HeaterOverTemp = Values[28].ScaledValue,
                                                ChmFilterPV = Values[14].ScaledValue,
                                                MotorChmPV = Values[15].ScaledValue,
                                                MotorChmSV = Values[16].ScaledValue,
                                                MotorChmMV = Values[17].ScaledValue,
                                                MfcSV = Values[21].ScaledValue,
                                                MfcPV = Values[22].ScaledValue,
                                                MfcMV = Values[43].ScaledValue,
                                                InsideTemp1 = Values[23].ScaledValue,
                                                InsideTemp2 = Values[24].ScaledValue,
                                                O2Temp = Chamber.AnalyzerO2Temperature,
                                                O2EMF = Chamber.AnalyzerO2Emf,
                                                O2PPM = Chamber.AnalyzerO2Ppm
                                            };
                                            bits = (from alarm in Chamber.Alarms select alarm.State).ToArray();
                                            for (int i = 0; i < bits.Length; i++)
                                            {
                                                d.AlarmBits[i] = bits[i];
                                            }
                                            Reply.Chamber2 = d;

                                            // #3 챔버 데이터
                                            Ch = Channels[2];
                                            Chamber = Ch.CleanOvenChamber;
                                            Values = Chamber.NumericValues;
                                            d = new BakeOvenCommDataChamberStatusData
                                            {
                                                IsInitOperation = Chamber.IsInitRunning,
                                                IsRun = Chamber.IsRunning,
                                                IsStopping = Chamber.IsStopping,
                                                IsStop = Chamber.IsStop,
                                                IsAutoTune = Chamber.IsAutoTune,
                                                IsReady = Chamber.IsStop && !Chamber.IsAlarmState,
                                                IsAlarm = Chamber.IsAlarmState,
                                                IsDoorOpenAvailable = Chamber.IsDoorOpenAvailable,
                                                IsDoorOpen = Chamber.IsDoorOpen,
                                                IsDoorClosed = Chamber.IsDoorClosed,
                                                RecipeName = Ch.PatternForRun.Name,
                                                SequenceTotal = Chamber.TotalSequenceCount,
                                                SequenceNo = Chamber.CurrentSequenceNo,
                                                ProcessStartTime = Chamber.TotalSegmentElapsedTime,              // 시간 단위 => 분?
                                                ProcessTotalTime = Chamber.TotalSegmentTime,
                                                SequenceStartTime = Chamber.CurrentSegmentElapsedTime,
                                                SequenceToatlTime = Chamber.CurrentSegmentDuration,
                                                TempPV = Values[0].ScaledValue,
                                                TempSV = Values[1].ScaledValue,
                                                TempMV = Values[3].ScaledValue,
                                                ChmPressPV = Values[11].ScaledValue,
                                                ChmPressSV = Values[12].ScaledValue,
                                                ChmPressMV = Values[13].ScaledValue,
                                                ChmOverTemp = Values[27].ScaledValue,
                                                HeaterOverTemp = Values[28].ScaledValue,
                                                ChmFilterPV = Values[14].ScaledValue,
                                                MotorChmPV = Values[15].ScaledValue,
                                                MotorChmSV = Values[16].ScaledValue,
                                                MotorChmMV = Values[17].ScaledValue,
                                                MfcSV = Values[21].ScaledValue,
                                                MfcPV = Values[22].ScaledValue,
                                                MfcMV = Values[43].ScaledValue,
                                                InsideTemp1 = Values[23].ScaledValue,
                                                InsideTemp2 = Values[24].ScaledValue,
                                                O2Temp = Chamber.AnalyzerO2Temperature,
                                                O2EMF = Chamber.AnalyzerO2Emf,
                                                O2PPM = Chamber.AnalyzerO2Ppm
                                            };
                                            bits = (from alarm in Chamber.Alarms select alarm.State).ToArray();
                                            for (int i = 0; i < bits.Length; i++)
                                            {
                                                d.AlarmBits[i] = bits[i];
                                            }
                                            Reply.Chamber3 = d;

                                            // #4 챔버 데이터
                                            Ch = Channels[3];
                                            Chamber = Ch.CleanOvenChamber;
                                            Values = Chamber.NumericValues;
                                            d = new BakeOvenCommDataChamberStatusData
                                            {
                                                IsInitOperation = Chamber.IsInitRunning,
                                                IsRun = Chamber.IsRunning,
                                                IsStopping = Chamber.IsStopping,
                                                IsStop = Chamber.IsStop,
                                                IsAutoTune = Chamber.IsAutoTune,
                                                IsReady = Chamber.IsStop && !Chamber.IsAlarmState,
                                                IsAlarm = Chamber.IsAlarmState,
                                                IsDoorOpenAvailable = Chamber.IsDoorOpenAvailable,
                                                IsDoorOpen = Chamber.IsDoorOpen,
                                                IsDoorClosed = Chamber.IsDoorClosed,
                                                RecipeName = Ch.PatternForRun.Name,
                                                SequenceTotal = Chamber.TotalSequenceCount,
                                                SequenceNo = Chamber.CurrentSequenceNo,
                                                ProcessStartTime = Chamber.TotalSegmentElapsedTime,              // 시간 단위 => 분?
                                                ProcessTotalTime = Chamber.TotalSegmentTime,
                                                SequenceStartTime = Chamber.CurrentSegmentElapsedTime,
                                                SequenceToatlTime = Chamber.CurrentSegmentDuration,
                                                TempPV = Values[0].ScaledValue,
                                                TempSV = Values[1].ScaledValue,
                                                TempMV = Values[3].ScaledValue,
                                                ChmPressPV = Values[11].ScaledValue,
                                                ChmPressSV = Values[12].ScaledValue,
                                                ChmPressMV = Values[13].ScaledValue,
                                                ChmOverTemp = Values[27].ScaledValue,
                                                HeaterOverTemp = Values[28].ScaledValue,
                                                ChmFilterPV = Values[14].ScaledValue,
                                                MotorChmPV = Values[15].ScaledValue,
                                                MotorChmSV = Values[16].ScaledValue,
                                                MotorChmMV = Values[17].ScaledValue,
                                                MfcSV = Values[21].ScaledValue,
                                                MfcPV = Values[22].ScaledValue,
                                                MfcMV = Values[43].ScaledValue,
                                                InsideTemp1 = Values[23].ScaledValue,
                                                InsideTemp2 = Values[24].ScaledValue,
                                                O2Temp = Chamber.AnalyzerO2Temperature,
                                                O2EMF = Chamber.AnalyzerO2Emf,
                                                O2PPM = Chamber.AnalyzerO2Ppm
                                            };
                                            bits = (from alarm in Chamber.Alarms select alarm.State).ToArray();
                                            for (int i = 0; i < bits.Length; i++)
                                            {
                                                d.AlarmBits[i] = bits[i];
                                            }
                                            Reply.Chamber4 = d;

                                            bool v = CimClient.Send(Reply);
                                        }
                                        break;

                                    case eEfemMessage.PreCheckChamberReady:	    // 챔버 작업가능한지 요청 => IsReady 상태에 대한 요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (!Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    BakeOvenResponse Reply = new BakeOvenResponse()
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo
                                                    };
                                                    Reply.ChamberNo = Ch.No;
                                                    Reply.Req = Msg.Req;
                                                    Reply.Ack = eBakeOvenAck.NG;
                                                    Reply.ErrorCode = "PLC Disconnected";
                                                    _ = CimClient.Send(Reply);
                                                }
                                                else
                                                {
                                                    if (Ch.CleanOvenChamber.IsStop && !Ch.CleanOvenChamber.IsAlarmState)
                                                    {
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.ChamberNo = Ch.No;
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.OK;
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                    else if (!Ch.CleanOvenChamber.IsStop)
                                                    {
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.ChamberNo = Ch.No;
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.NG;
                                                        Reply.ErrorCode = "Not Stop";
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                    else if (Ch.CleanOvenChamber.IsAlarmState)
                                                    {
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.ChamberNo = Ch.No;
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.NG;
                                                        Reply.ErrorCode = "Alarm State";
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                    else
                                                    {
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.ChamberNo = Ch.No;
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.NG;
                                                        Reply.ErrorCode = "Unknown";
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    case eEfemMessage.ReqOpenChamberDoor:       // 챔버 도어 열기요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (!Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    BakeOvenResponse Reply = new BakeOvenResponse()
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo
                                                    };
                                                    Reply.ChamberNo = Ch.No;
                                                    Reply.Req = Msg.Req;
                                                    Reply.Ack = eBakeOvenAck.NG;
                                                    Reply.ErrorCode = "PLC Disconnected";
                                                    _ = CimClient.Send(Reply);
                                                }
                                                else
                                                {
                                                    if (Ch.CleanOvenChamber.IsDoorOpenAvailable)
                                                    {
                                                        Ch.CleanOvenChamber.OpenDoorNoMsg();
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.OK;
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                    else
                                                    {
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.NG;
                                                        Reply.ErrorCode = "DoorOpen Not Available";
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                }                                               
                                            }
                                        }
                                        break;

                                    case eEfemMessage.ReqCloseChamberDoor:      // 챔버 도어 닫기요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (!Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    BakeOvenResponse Reply = new BakeOvenResponse()
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo
                                                    };
                                                    Reply.ChamberNo = Ch.No;
                                                    Reply.Req = Msg.Req;
                                                    Reply.Ack = eBakeOvenAck.NG;
                                                    Reply.ErrorCode = "PLC Disconnected";
                                                    _ = CimClient.Send(Reply);
                                                }
                                                else
                                                {
                                                    if (Ch.CleanOvenChamber.IsDoorClosed)
                                                    {

                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.NG;
                                                        Reply.ErrorCode = "Already Closed";
                                                        bool v = CimClient.Send(Reply);
                                                    }
                                                    else
                                                    {
                                                        Ch.CleanOvenChamber.CloseDoorNoMsg();
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.OK;
                                                        bool v = CimClient.Send(Reply);
                                                    }                                               
                                                }
                                            }
                                        }
                                        break;

                                    case eEfemMessage.ReqPrepareBakeProcess:    // 작업 준비 요청                                   
                                    case eEfemMessage.ReqStartTransfer:		    // 패널 이송준비 요청                                      
                                    case eEfemMessage.ReqCompleteTransfer:	    // 패널 이송완료 요청
                                        {
                                            // 별다른 액션이 필요없음.
                                            BakeOvenResponse Reply = new BakeOvenResponse()
                                            {
                                                IsEFEM = false,
                                                MessageSeqNo = Msg.MessageSeqNo
                                            };
                                            Reply.Req = Msg.Req;
                                            Reply.Ack = eBakeOvenAck.OK;
                                            bool v = CimClient.Send(Reply);
                                        }
                                        break;

                                    case eEfemMessage.ReqStartBakeProcess:	    // bake 프로세스 시작 요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (!Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    BakeOvenResponse Reply = new BakeOvenResponse()
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo
                                                    };
                                                    Reply.ChamberNo = Ch.No;
                                                    Reply.Req = Msg.Req;
                                                    Reply.Ack = eBakeOvenAck.NG;
                                                    Reply.ErrorCode = "PLC Disconnected";
                                                    bool v = CimClient.Send(Reply);
                                                  
                                                }
                                                else
                                                {
                                                    if (!Ch.CleanOvenChamber.IsReady)
                                                    {
                                                        BakeOvenResponse Reply = new BakeOvenResponse()
                                                        {
                                                            IsEFEM = false,
                                                            MessageSeqNo = Msg.MessageSeqNo
                                                        };
                                                        Reply.ChamberNo = Ch.No;
                                                        Reply.Req = Msg.Req;
                                                        Reply.Ack = eBakeOvenAck.NG;
                                                        Reply.ErrorCode = "Not Ready";
                                                        _ = CimClient.Send(Reply);
                                                    }
                                                    else
                                                    {
                                                        string dir = @"D:\APP\DAESUNG-ENT\CLEAN_OVEN\PATTERN";
                                                        string path = Path.Combine(dir, string.Format("{0:D3}.xml", Msg.RecipeNo));
                                                        if (!File.Exists(path))
                                                        {
                                                            BakeOvenResponse Reply = new BakeOvenResponse()
                                                            {
                                                                IsEFEM = false,
                                                                MessageSeqNo = Msg.MessageSeqNo
                                                            };
                                                            Reply.ChamberNo = Ch.No;
                                                            Reply.Req = Msg.Req;
                                                            Reply.Ack = eBakeOvenAck.NG;
                                                            Reply.ErrorCode = "No Recipe File";
                                                            _ = CimClient.Send(Reply);
                                                        }
                                                        else
                                                        {
                                                            Ch.SelectRunningPatternNoMsg(Msg.RecipeNo);
                                                            Thread.Sleep(3000);
                                                            Ch.CleanOvenChamber.StartBake();
                                                            BakeOvenResponse Reply = new BakeOvenResponse()
                                                            {
                                                                IsEFEM = false,
                                                                MessageSeqNo = Msg.MessageSeqNo
                                                            };
                                                            Reply.ChamberNo = Ch.No;
                                                            Reply.Req = Msg.Req;
                                                            Reply.Ack = eBakeOvenAck.OK;
                                                            bool v = CimClient.Send(Reply);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    case eEfemMessage.ReqAbortBakeProcess:      // bake 프로세스 중단 요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (!Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    BakeOvenResponse Reply = new BakeOvenResponse()
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo
                                                    };
                                                    Reply.ChamberNo = Ch.No;
                                                    Reply.Req = Msg.Req;
                                                    Reply.Ack = eBakeOvenAck.NG;
                                                    Reply.ErrorCode = "PLC DisConnected.";
                                                    bool v = CimClient.Send(Reply);
                                                }
                                                else
                                                {
                                                    Ch.CleanOvenChamber.AbortBake();
                                                    BakeOvenResponse Reply = new BakeOvenResponse()
                                                    {
                                                        IsEFEM = false,
                                                        MessageSeqNo = Msg.MessageSeqNo
                                                    };
                                                    Reply.ChamberNo = Ch.No;
                                                    Reply.Req = Msg.Req;
                                                    Reply.Ack = eBakeOvenAck.OK;
                                                    bool v = CimClient.Send(Reply);
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                            else if (m.GetType() == typeof(EfemEvent))
                            {
                                EfemEvent Msg = m as EfemEvent;
                                if (!Msg.IsEFEM)
                                {
                                    return;
                                }

                                // EFEM에서 보내오는 아래 이벤트 메세지는 별도 처리할 내용이 없다.
                                switch (Msg.EventID)
                                {
                                    case eEfemEvent.StartPutPanelEvent:         // 패널 공급 시작.
                                        break;
                                    case eEfemEvent.FinishPutPanelEvent:        // 패널 공급 완료.
                                        break;
                                    case eEfemEvent.StopPutPanelEvent:          // 패널 공급 중단.
                                        break;
                                    case eEfemEvent.StartGetPanelEvent:         // 패널 배출 시작.
                                        break;
                                    case eEfemEvent.FinishGetPanelEvent:        // 패널 배출 완료.
                                        break;
                                    case eEfemEvent.StopGetPanelEvent:          // 패널 배출 중단.
                                        break;
                                }
                            }                        
                        });
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "CoordinateMessageHandlerFunc() exceptoin : {0}", ex.Message);
            }
        }
        private void Ch_AlarmOccured(object sender, EventArgs e)
        {
            try
            {
                if (sender is ChannelViewModel Ch)
                {
                    if (CimClient != null && CimClient.IsConnected)
                    {
                        BakeOvenEventData Msg = new BakeOvenEventData();
                        Msg.IsEFEM = false;
                        Msg.ChamberNo = Ch.No;
                        Msg.EventID = eOvenEvent.Alarm;
                        _ = CimClient.Send(Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is occured to process Ch_AlarmOccured() : " + ex.Message);
            }
        }
        private void Ch_DoorOpenCompleted(object sender, EventArgs e)
        {
            try
            {
                if (sender is ChannelViewModel Ch)
                {
                    if (CimClient != null && CimClient.IsConnected)
                    {
                        BakeOvenEventData Msg = new BakeOvenEventData();
                        Msg.IsEFEM = false;
                        Msg.ChamberNo = Ch.No;
                        Msg.EventID = eOvenEvent.DoorOpenComplete;
                        _ = CimClient.Send(Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is occured to process Ch_DoorOpenCompleted() : " + ex.Message);
            }
        }
        private void Ch_DoorCloseCompleted(object sender, EventArgs e)
        {
            try
            {
                if (sender is ChannelViewModel Ch)
                {
                    if (CimClient != null && CimClient.IsConnected)
                    {
                        BakeOvenEventData Msg = new BakeOvenEventData();
                        Msg.IsEFEM = false;
                        Msg.ChamberNo = Ch.No;
                        Msg.EventID = eOvenEvent.DoorCloseComplete;
                        _ = CimClient.Send(Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is occured to process Ch_DoorCloseCompleted() : " + ex.Message);
            }
        }
        private void Ch_ProcessStarted(object sender, EventArgs e)
        {
            try
            {
                if (sender is ChannelViewModel Ch)
                {
                    if (CimClient != null && CimClient.IsConnected)
                    {
                        BakeOvenEventData Msg = new BakeOvenEventData();
                        Msg.IsEFEM = false;
                        Msg.ChamberNo = Ch.No;
                        Msg.EventID = eOvenEvent.ProcessStart;
                        _ = CimClient.Send(Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is occured to process Ch_ProcessStarted() : " + ex.Message);
            }
        }
        private void Ch_ProcessCompleted(object sender, EventArgs e)
        {
            try
            {
                if (sender is ChannelViewModel Ch)
                {
                    if (CimClient != null && CimClient.IsConnected)
                    {
                        BakeOvenEventData Msg = new BakeOvenEventData();
                        Msg.IsEFEM = false;
                        Msg.ChamberNo = Ch.No;
                        Msg.EventID = eOvenEvent.ProcessComplete;
                        _ = CimClient.Send(Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is occured to process Ch_ProcessCompleted() : " + ex.Message);
            }
        }
        private void Ch_ProcessAborted(object sender, EventArgs e)
        {
            try
            {
                if (sender is ChannelViewModel Ch)
                {
                    if (CimClient != null && CimClient.IsConnected)
                    {
                        BakeOvenEventData Msg = new BakeOvenEventData();
                        Msg.IsEFEM = false;
                        Msg.ChamberNo = Ch.No;
                        Msg.EventID = eOvenEvent.ProcessAbort;
                        _ = CimClient.Send(Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is occured to process Ch_ProcessAborted() : " + ex.Message);
            }
        }
    }
}