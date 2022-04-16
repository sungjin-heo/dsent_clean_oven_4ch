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

            SystemTimer = new System.Timers.Timer() { Interval = 1000 };
            SystemTimer.Elapsed += (s, e) => {
                this.SystemTime = e.SignalTime.ToSystemTimeFormat();
                RaisePropertiesChanged("SystemTime");
            };
            SystemTimer.Start();

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
                            if ((Today - Tmp).TotalDays > 60)
                                System.IO.File.Delete(f);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Dispatch("e", "Exception is Occured in LogRemover Handler : " + ex.Message);
                }
            };
            LogRemoveTimer.Start();
        }

        private System.Timers.Timer SystemTimer;
        private System.Timers.Timer LogRemoveTimer;
        private System.Threading.Tasks.Task MpMessageProcTask;
        private System.Threading.CancellationTokenSource MpMessageProcTaskCancelSource;

        public ISplashScreenService SplashScreenService => GetService<ISplashScreenService>();
        public IDispatcherService DispatcherService => GetService<IDispatcherService>();
        public DevExpress.Mvvm.DelegateCommand<object> InitializedCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand<CancelEventArgs> QuitCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand OpenCommCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand CloseCommCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand<object> MoveToChannelViewCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand AlarmResetCommand { get; private set; }
        public DevExpress.Mvvm.DelegateCommand BuzzerStopCommand { get; private set; }
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
                CimClient.Start();

                // EFE 서버의 상태 값을 출력하기 위한 모델.
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
                this.Analyzer.ConnectionStateChanged += (s, e) => {
                    Channels[e.DeviceId].UpdateAnalyzerConnectionState(e.ConnectState);
                };
                this.Analyzer.Connected += (s, e) => { Analyzer.StartMonitor(); };

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
                Analyzer.Close();
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
                    Channels.ForEach(o => o.OpenComm());
                    if (Analyzer != null && !Analyzer.IsOpen)
                        _ = Analyzer.Open();
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
                View.ProgressWindow.ShowWindow("대성ENT - 4CH. N2 CLEAN OVEN", "모니터링 정지 및 통신 해제 중...");
                await Task.Run(() => {
                    if (Analyzer != null && Analyzer.IsOpen)
                        Analyzer.Close();
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
                    Chan.CleanOvenChamber.AlarmReset();
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
                        Channel.LoadPatternMetaData();
                }
            }
        }
        private void Analyzer_MeasureDataUpdated(object sender, Equipment.MeasureDataUpdateEventArgs e)
        {
            ChannelViewModel Channel = Channels[e.DeviceId];
            if (Channel != null)
            {
                Channel.CleanOvenChamber.UpdateAnalyzerTemperature(e.SensorTemperature);
                Channel.CleanOvenChamber.UpdateAnalyzerEmf(e.SensorEMF);
                Channel.CleanOvenChamber.UpdateAnalyzerConcentrationPpm(e.O2ConcentrationPpm);
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

                            if (m.GetType() == typeof(CommonData))
                            {

                            }

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
                                // 챔버는 반드시 EFEM 서버측으로 응답 메세지를 보내야 한다.
                                EfemRequest Msg = m as EfemRequest;
                                if (!Msg.IsEFEM)
                                    return;

                                switch (Msg.Req)
                                {
                                    case eEfemMessage.None:
                                        break;

                                    case eEfemMessage.ReqResetAlarm:            // 알람 해제 요청, EFEM에서는 채널 아이디가 넘어오나 알람해제는 공통.
                                        {
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
                                            // # 시뮬레이션 프로그램과 연동시 레시피 리스트를 전송할 경우 에러발생. 단일 레피시 전송 시 OK.
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
                                                    Model.Pattern pattern;
                                                    pattern = Model.Pattern.LoadFrom(path);

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
                                            BakeOvenCommDataStatus Reply = new BakeOvenCommDataStatus()
                                            {
                                                IsEFEM = false,
                                                MessageSeqNo = Msg.MessageSeqNo
                                            };

                                            // 각 채널별로 동작함으로 응답 메세지의 아래 전역 파라미터는 의미가 없어 보임...
                                            // Reply.IsInitOperation = ;
                                            // Reply.IsRun = ;
                                            // Reply.IsStopping =;
                                            // Reply.IsStop = ;
                                            // Reply.IsAutoTune = 

                                            // #1 챔버 데이터
                                            ChannelViewModel Ch = Channels[0];
                                            Equipment.CleanOven Chamber = Ch.CleanOvenChamber;
                                            List<RegNumeric> Values = Chamber.NumericValues;
                                            BakeOvenCommDataChamberStatusData d = new BakeOvenCommDataChamberStatusData
                                            {
                                                // d.IsReady = ;            // Ready와 Prepared 상태는 어떤값을 기준으로 판단?
                                                // d.IsPrepared = ;
                                                //AlarmBits = (from alarm in Chamber.Alarms select alarm.State).ToArray(),
                                                IsRun = Chamber.IsRunning,
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
                                                // d.IsReady = ;            // Ready와 Prepared 상태는 어떤값을 기준으로 판단?
                                                // d.IsPrepared = ;
                                                //AlarmBits = (from alarm in Chamber.Alarms select alarm.State).ToArray(),
                                                IsRun = Chamber.IsRunning,
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
                                                // d.IsReady = ;            // Ready와 Prepared 상태는 어떤값을 기준으로 판단?
                                                // d.IsPrepared = ;
                                                //AlarmBits = (from alarm in Chamber.Alarms select alarm.State).ToArray(),
                                                IsRun = Chamber.IsRunning,
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

                                            // #3 챔버 데이터
                                            Ch = Channels[3];
                                            Chamber = Ch.CleanOvenChamber;
                                            Values = Chamber.NumericValues;
                                            d = new BakeOvenCommDataChamberStatusData
                                            {
                                                // d.IsReady = ;            // Ready와 Prepared 상태는 어떤값을 기준으로 판단?
                                                // d.IsPrepared = ;
                                                //AlarmBits = (from alarm in Chamber.Alarms select alarm.State).ToArray(),
                                                IsRun = Chamber.IsRunning,
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

                                    case eEfemMessage.PreCheckChamberReady:	    // 챔버 작업가능한지 요청
                                        {

                                        }
                                        break;

                                    case eEfemMessage.ReqOpenChamberDoor:       // 챔버 도어 열기요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                Ch.CleanOvenChamber.OpenDoorNoVerify();
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
                                        break;

                                    case eEfemMessage.ReqCloseChamberDoor:      // 챔버 도어 닫기요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                Ch.CleanOvenChamber.CloseDoorNoVerify();
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
                                        break;

                                    case eEfemMessage.ReqPrepareBakeProcess:    // 작업 준비 요청
                                        {

                                        }
                                        break;

                                    case eEfemMessage.ReqStartTransfer:		    // 패널 이송준비 요청
                                        {

                                        }
                                        break;

                                    case eEfemMessage.ReqCompleteTransfer:	    // 패널 이송완료 요청
                                        {

                                        }
                                        break;

                                    case eEfemMessage.ReqStartBakeProcess:	    // bake 프로세스 시작 요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    Ch.CleanOvenChamber.StartBake();
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
                                                    Reply.ErrorCode = "Chamber Controller DisConnected.";
                                                    bool v = CimClient.Send(Reply);
                                                }                                               
                                            }
                                        }
                                        break;

                                    case eEfemMessage.ReqAbortBakeProcess:      // bake 프로세스 중단 요청
                                        {
                                            if (Msg.ChamberNo >= 1 && Msg.ChamberNo <= 4)
                                            {
                                                ChannelViewModel Ch = Channels[Msg.ChamberNo - 1];
                                                if (Ch.CleanOvenChamber.IsConnected)
                                                {
                                                    Ch.CleanOvenChamber.AbortBake();
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
                                                    Reply.ErrorCode = "Chamber Controller DisConnected.";
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

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "CoordinateMessageHandlerFunc() exceptoin : {0}", ex.Message);
            }
        }
        private void Ch_AlarmOccured(object sender, EventArgs e)
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
        private void Ch_DoorOpenCompleted(object sender, EventArgs e)
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
        private void Ch_DoorCloseCompleted(object sender, EventArgs e)
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
        private void Ch_ProcessStarted(object sender, EventArgs e)
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
        private void Ch_ProcessCompleted(object sender, EventArgs e)
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
        private void Ch_ProcessAborted(object sender, EventArgs e)
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
    }
}