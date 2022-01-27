using Util;

namespace Log
{
    public enum SplitRule
    {
        Day,
        Time,        
        Lines,
    };

    class L
    {
        public L()
        {
            this.Enable = true;
            this.Interval = 1;
            this.EnableSplit = true;
            this.SplitRule = SplitRule.Day;
            this.SplitCount = 86400;
            this.SplitTime = 24;
        }
        public bool Enable { get; set; }
        public double Interval { get; set; }
        public string Storage { get; set; }        
        public bool EnableSplit { get; set; }
        public SplitRule SplitRule { get; set; }
        public int SplitTime { get; set; }
        public int SplitCount { get; set; }
    }
    class Setup : DevExpress.Mvvm.BindableBase
    {
        public static Setup I()
        {
            if (Instance == null)
                Instance = new Setup(new L());
            return Instance;
        }
        static Setup Instance { get; set; }
      
        L Model { get; set; }
        public Setup(L log)
        {
            this.Model = log;
            this.ChangeStorageCommand = new DevExpress.Mvvm.DelegateCommand(ChangeStorage);
            this.SplitRuleOpt = new Options() {
                new Option("Day", SplitRule.Day),
                new Option("Time Intv.", SplitRule.Time),
                new Option("Lines", SplitRule.Lines),
            };
            this.SplitTimeOpt = new int[] { 1, 2, 3, 4, 5, 6, 12, 24 };
        }

        public DevExpress.Mvvm.DelegateCommand ChangeStorageCommand { get; set; }
        public Options SplitRuleOpt { get; set; }
        public int[] SplitTimeOpt { get; set; }
        public bool Enable
        {
            get { return Model.Enable; }
            set
            {
                Model.Enable = value;
                RaisePropertyChanged("Enable");
            }
        }
        public double Interval
        {
            get { return Model.Interval; }
            set
            {
                if (value >= 0.5)
                    Model.Interval = value;
                RaisePropertyChanged("Interval");
            }
        }
        public string Storage
        {
            get { return Model.Storage; }
            set
            {
                Model.Storage = value;
                RaisePropertyChanged("Storage");
            }
        }
        public bool EnableSplit
        {
            get { return Model.EnableSplit; }
            set
            {
                Model.EnableSplit = value;
                RaisePropertyChanged("EnableSplit");
            }
        }
        public SplitRule SplitRule
        {
            get { return Model.SplitRule; }
            set
            {
                Model.SplitRule = value;
                RaisePropertiesChanged("SplitRule");
            }
        }
        public int SplitTime
        {
            get { return Model.SplitTime; }
            set
            {
                Model.SplitTime = value;
                RaisePropertiesChanged("SplitTime");
            }
        }
        public int SplitCount
        {
            get { return Model.SplitCount; }
            set
            {
                Model.SplitCount = value;
                RaisePropertiesChanged("SplitCount");
            }
        }
        protected void ChangeStorage()
        {
            var Dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (Dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.Storage = Dlg.SelectedPath;
        }


        //         public string CsvStorage { get { return Model.CsvStorage; } }
        //         public string BinaryStorage { get { return Model.BinaryStorage; } }
    }    
}
