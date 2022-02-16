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

namespace DaesungEntCleanOven4.ViewModel
{
    class MainViewModel : DevExpress.Mvvm.ViewModelBase
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
            this.AlarmResetCommand = new DevExpress.Mvvm.DelegateCommand(AlarmReset);
            this.BuzzerStopCommand = new DevExpress.Mvvm.DelegateCommand(BuzzerStop);

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
        public MpMessageClient CimClient { get; private set; }
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

                for (int i = 0; i < 4; i++)
                {
                    ChannelViewModel Ch = new ChannelViewModel(i + 1);
                    Ch.DetailViewMoveRequested += Ch_DetailViewMoveRequested;
                    Ch.IntegrateViewReturnRequested += Ch_IntegrateViewReturnRequested;
                    Channels.Add(Ch);
                }

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
                CimClient.ClientConnectionChangedEvent += CimClient_ClientConnectionChangedEvent;
                CimClient.MessageReceiveEvent += CimClient_MessageReceiveEvent;
                CimClient.ReceiveErrorEvent += CimClient_ReceiveErrorEvent;
            }
            catch (Exception ex)
            {
                SplashScreenService.SetSplashScreenState("System Initialize Failed.");
            }
            finally
            {
                SplashScreenService.HideSplashScreen();
                RaisePropertiesChanged("Channels");
            }
        }
        private void OnClose()
        {
            Channels.ForEach(o => o.Dispose());
        }
        private void OpenComm()
        {
            Channels.ForEach(o => o.OpenComm());
        }
        private bool CanOpenComm()
        {
            return Channels.Any(o => !o.IsConnected);
        }
        private void CloseComm()
        {
//             View.Question Q = new View.Question("");
//             if (!(bool)Q.ShowDialog())
//                 return;
            Channels.ForEach(o => o.CloseComm());
        }
        private bool CanCloseComm()
        {
            return Channels.All(o => o.IsConnected);
        }
        private void MoveToChannelView(object parameter)
        {
            int Ch = int.Parse(parameter as string);
            this.SelectedChannel = Channels[Ch - 1];
            RaisePropertiesChanged("SelectedChannel");
            DetailViewMoveRequested?.Invoke(this, EventArgs.Empty);       
        }
        private void AlarmReset()
        {

        }
        private void BuzzerStop()
        {

        }
        private void Ch_DetailViewMoveRequested(object sender, EventArgs e)
        {
            this.SelectedChannel = sender as ViewModel.ChannelViewModel;
            RaisePropertiesChanged("SelectedChannel");
            DetailViewMoveRequested?.Invoke(this, EventArgs.Empty);
        }
        private void Ch_IntegrateViewReturnRequested(object sender, EventArgs e)
        {
            IntegrateViewReturnRequested?.Invoke(this, EventArgs.Empty);
        }
        private void CimClient_ClientConnectionChangedEvent(MpMessageClient arg1, bool arg2)
        {
            Log.Logger.Dispatch("i", "Robostar.CIM Connection State Changed : {0}", arg2 ? "Connected" : "DisConnected");
        }
        private void CimClient_MessageReceiveEvent(object m)
        {
            Type Ty = m.GetType();
            if (Ty == typeof(EfemCommDataStatus))
            {

            }
        }
        private void CimClient_ReceiveErrorEvent(MpMessageClient arg1, string arg2)
        {
            Log.Logger.Dispatch("e", "Robostar.CIM Error Occured : {0}", arg2);
        }
    }
}