using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace DaesungEntCleanOvenDataViewer.View
{
    /// <summary>
    /// Interaction logic for NotifyWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(string Title, string Message)
        {
            InitializeComponent();
            this.txtTitle.Text = Title;
            this.txtMessage.Text = Message;
        }
        public System.Windows.Media.SolidColorBrush BackgroundColor
        {
            set { background.Background = value; }
        }
        static ProgressWindow Progressor { get; set; }
        public static void ShowWindow(string Title, string Message)
        {
            CloseWindow();
            Progressor = new ProgressWindow(Title, Message);
            Progressor.Show();
        }
        public static void CloseWindow()
        {
            if (Progressor != null)
            {
                if(Progressor.Dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    Progressor.Close();
                }
                else
                {
                    Progressor.Dispatcher.Invoke(() => {
                        Progressor.Close();
                    });
                }
                Progressor = null;
            }
        }
    }
}
