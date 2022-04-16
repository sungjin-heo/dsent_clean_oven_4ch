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
    /// AlertParameterSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlarmParameterSetupDlg : Window
    {
        public AlarmParameterSetupDlg()
        {
            InitializeComponent();
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is Equipment.CleanOven Chamber)
            {
                switch (Chamber.ChannelNumber)
                {
                    case 1:                        
                        TitleAirPressureLow.IsEnabled = true;
                        TitleCoolingPressureLow.IsEnabled = true;
                        TitleLeakSensor2.IsEnabled = true;
                        TitleSmokeAlarm.IsEnabled = true;
                        LevelAirPressureLow.IsEnabled = true;
                        LevelCoolingPressureLow.IsEnabled = true;
                        LevelLeakSensor2.IsEnabled = true;
                        LevelSmokeAlarm.IsEnabled = true;
                        DelayAirPressureLow.IsEnabled = true;
                        DelayCoolingPressureLow.IsEnabled = true;
                        DelayLeakSensor2.IsEnabled = true;
                        DelaySmokeAlarm.IsEnabled = true;
                        break;
                    case 2:
                    case 4:
                        TitleAirPressureLow.IsEnabled = false;
                        TitleCoolingPressureLow.IsEnabled = false;
                        TitleLeakSensor2.IsEnabled = false;
                        TitleSmokeAlarm.IsEnabled = false;
                        LevelAirPressureLow.IsEnabled = false;
                        LevelCoolingPressureLow.IsEnabled = false;
                        LevelLeakSensor2.IsEnabled = false;
                        LevelSmokeAlarm.IsEnabled = false;
                        DelayAirPressureLow.IsEnabled = false;
                        DelayCoolingPressureLow.IsEnabled = false;
                        DelayLeakSensor2.IsEnabled = false;
                        DelaySmokeAlarm.IsEnabled = false;
                        break;
                    case 3:
                        TitleAirPressureLow.IsEnabled = false;
                        TitleCoolingPressureLow.IsEnabled = true;
                        TitleLeakSensor2.IsEnabled = true;
                        TitleSmokeAlarm.IsEnabled = true;
                        LevelAirPressureLow.IsEnabled = false;
                        LevelCoolingPressureLow.IsEnabled = true;
                        LevelLeakSensor2.IsEnabled = true;
                        LevelSmokeAlarm.IsEnabled = true;
                        DelayAirPressureLow.IsEnabled = false;
                        DelayCoolingPressureLow.IsEnabled = true;
                        DelayLeakSensor2.IsEnabled = true;
                        DelaySmokeAlarm.IsEnabled = true;
                        break;
                }
            }
        }
    }
}
