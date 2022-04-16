using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using Util;

namespace Log
{
    public class LogEntry : PropertyChangedBase
    {
        public LogEntry() { }
        public LogEntry(string Message)
        {
            this.TimeStamp = DateTime.Now;
            this.Message = Message;
        }
        public DateTime TimeStamp { get; set; }
        public int Index { get; set; }
        public string Message { get; set; }
        public string FormattedTimeStamp => TimeStamp.ToLogTimeFormat();
    }
    public class CollapsibleLogEntry : LogEntry
    {
        public List<LogEntry> Contents { get; set; }
    }
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }));
        }
    }
}
