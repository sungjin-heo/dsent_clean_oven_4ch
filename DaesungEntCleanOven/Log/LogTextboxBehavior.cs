using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Log
{
    class LogTextboxBehavior : DevExpress.Mvvm.UI.Interactivity.Behavior<System.Windows.Controls.TextBox>
    {
        TextBox AssociatedTextbox
        {
            get { return base.AssociatedObject; }
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedTextbox.TextChanged += AssociatedTextbox_TextChanged;
        }
        protected override void OnDetaching()
        {
            AssociatedTextbox.TextChanged -= AssociatedTextbox_TextChanged;
            base.OnDetaching();
        }
        void AssociatedTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AssociatedTextbox.ScrollToEnd();
        }
    }
}
