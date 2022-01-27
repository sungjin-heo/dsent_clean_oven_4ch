using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven.ViewModel;
using Util;

namespace DaesungEntCleanOven.Equipment
{
    class Alarm : DevExpress.Mvvm.BindableBase
    {
        public Alarm(string Name, params RegRelay[] relayRef)
        {
            this.RelayRef = relayRef.ToArray();
            this.Description = Name.Replace(" L1", "").Replace(" L2", "");
            foreach (var relay in RelayRef)
                relay.ValueChanged += Relay_ValueChanged;
        }
        void Relay_ValueChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (e.Value)
            {
                if (!this.State)
                {
                    this.State = true;
                    this.RaiseTime = DateTime.Now;
                    AlarmOccured?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                if (this.State)
                {
                    if (this.RelayRef.All(o => o.Value == false))
                    {
                        this.State = false;
                        this.RaiseTime = DateTime.Now;
                        AlarmCleared?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            RaisePropertiesChanged("State");
        }
        protected RegRelay[] RelayRef { get; set; }
        public DateTime RaiseTime { get; set; }
        public bool State { get; set; }
        public string Description { get; set; }
        public event EventHandler AlarmOccured;
        public event EventHandler AlarmCleared;
        public AlarmDescript GetDescript()
        {
            return new AlarmDescript() { Time = RaiseTime.ToLogTimeFormat(), State = this.State ? "<ON>" : "<OFF>", Description = this.Description };
        }
    }

    class AlarmDescript
    {
        public string Time { get; set; }
        public string State { get; set; }
        public string Description { get; set; }
        public override string ToString()
        {
            return string.Format("{0},{1},{2}", Time, State, Description);
        }
    }
}
