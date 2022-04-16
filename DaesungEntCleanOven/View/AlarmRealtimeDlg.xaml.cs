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

namespace DaesungEntCleanOven4.View
{
    /// <summary>
    /// RealtimeAlarmDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlarmRealtimeDlg : Window
    {
        public AlarmRealtimeDlg()
        {
            InitializeComponent();
        }
        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is Equipment.CleanOven Chamber)
            {
                switch (Chamber.ChannelNumber)
                {
                    case 1:
                        LampAirPressureLow.IsEnabled = true;
                        LampCoolingPressureLow.IsEnabled = true;
                        LampLeakSensor2.IsEnabled = true;
                        LampSmokeAlarm.IsEnabled = true;
                        TitleAirPressureLow.IsEnabled = true;
                        TitleCoolingPressureLow.IsEnabled = true;
                        TitleLeakSensor2.IsEnabled = true;
                        TitleSmokeAlarm.IsEnabled = true;
                        break;
                    case 2:
                    case 4:
                        LampAirPressureLow.IsEnabled = false;
                        LampCoolingPressureLow.IsEnabled = false;
                        LampLeakSensor2.IsEnabled = false;
                        LampSmokeAlarm.IsEnabled = false;
                        TitleAirPressureLow.IsEnabled = false;
                        TitleCoolingPressureLow.IsEnabled = false;
                        TitleLeakSensor2.IsEnabled = false;
                        TitleSmokeAlarm.IsEnabled = false;
                        break;
                    case 3:
                        LampAirPressureLow.IsEnabled = false;
                        LampCoolingPressureLow.IsEnabled = true;
                        LampLeakSensor2.IsEnabled = true;
                        LampSmokeAlarm.IsEnabled = true;
                        TitleAirPressureLow.IsEnabled = false;
                        TitleCoolingPressureLow.IsEnabled = true;
                        TitleLeakSensor2.IsEnabled = true;
                        TitleSmokeAlarm.IsEnabled = true;
                        break;
                }
            }
        }
    }
}
