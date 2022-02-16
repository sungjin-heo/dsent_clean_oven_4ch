using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven4.Model;

namespace DaesungEntCleanOven4.ViewModel
{
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T Value { get; protected set; }
        public ValueChangedEventArgs(T value)
        {
            this.Value = value;
        }
    }

    public abstract class ItemViewModel<T> : DevExpress.Mvvm.BindableBase
    {
        protected Item<T> Model { get; set; }
        public ItemViewModel(Item<T> Model)
        {
            this.Model = Model ?? throw new ArgumentNullException("Item<T>");
        }
        public ItemViewModel(string Name)
            : this(Name, null, null)
        {
        }
        public ItemViewModel(string Name, string Unit)
            : this(Name, Unit, null)
        {
        }
        public ItemViewModel(string Name, string Unit, int? Scale)
        {
            this.Model = new Item<T>(Name, Unit, Scale);
        }
        public string Name
        {
            get { return Model.Name; }
            set
            {
                Model.Name = value;
                RaisePropertiesChanged("Name");
            }
        }
        public virtual T Value
        {
            get { return Model.Value; }
            set
            {
                Model.Value = value;
                RaisePropertiesChanged("Value", "ScaledValue", "FormattedValue");
                ValueChanged?.Invoke(this, new ValueChangedEventArgs<T>(value));
            }
        }
        public virtual T ScaledValue { get; }
        public int? Scale
        {
            get { return Model.Scale; }
            set
            {
                Model.Scale = value;
                RaisePropertiesChanged("Scale", "FormattedValue");
            }
        }
        public string Unit
        {
            get { return Model.Unit; }
            set
            {
                Model.Unit = value;
                RaisePropertiesChanged("Unit");
            }
        }
        public string Description
        {
            get { return Model.Description; }
            set
            {
                Model.Description = value;
                RaisePropertiesChanged("Description");
            }
        }        
        public abstract string FormattedValue { get; set; }
        public event EventHandler<ValueChangedEventArgs<T>> ValueChanged;
    }
}
