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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DaesungEntCleanOven.ViewModel;
using Util;

namespace DaesungEntCleanOven.View
{
    /// <summary>
    /// SystemOperatePanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SystemOperatePanel : UserControl
    {
        public SystemOperatePanel()
        {
            InitializeComponent();

            G.CleanOven.Started += (s, e) => {
                btnStart.Content = "정지";
                btnStart.Glyph = BitmapFrame.Create(new Uri("pack://application:,,,/DevExpress.Images.v17.1;component/Images/Arrows/Stop_16x16.png"));
                btnChangePatter.IsEnabled = false;
                btnPause.IsEnabled = true;
                btnAdvance.IsEnabled = true;
//                 MouseButtonEventArgs arg = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
//                 arg.RoutedEvent = TextBox.MouseLeftButtonUpEvent;
//                 sysDiagram.RaiseEvent(arg);
            };

            G.CleanOven.Stopped += (s, e) => {
                btnStart.Content = "운전";
                btnStart.Glyph = BitmapFrame.Create(new Uri("pack://application:,,,/DevExpress.Images.v17.1;component/Images/Arrows/Play_16x16.png"));
                btnChangePatter.IsEnabled = true;
                btnPause.IsEnabled = false;
                btnAdvance.IsEnabled = false;
//                 MouseButtonEventArgs arg = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
//                 arg.RoutedEvent = TextBox.MouseLeftButtonUpEvent;
//                 sysDiagram.RaiseEvent(arg);
            };
        }
    }
}
