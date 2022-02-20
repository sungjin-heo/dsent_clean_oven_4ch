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
    /// ParameterZoneSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ParameterZoneSetupDlg : Window
    {
        public ParameterZoneSetupDlg()
        {
            InitializeComponent();
            this.cmbZoneParameter.ItemsSource = new string[] {
                 "온도", "챔버  - OT", "히터 - OT", "차압 챔버", "MFC", "차압 필터", "모터 챔버", /*"모터 쿨링", */"내부 온도 #1", "내부 온도 #2",/* "내부 온도 #3", "내부 온도 #4"*/
             };
        }
        private void cmbZoneParameter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbZoneParameter.SelectedIndex >= 4 && cmbZoneParameter.SelectedIndex <= 6)
            {
                tboxFL.IsEnabled = false;
                tboxSC.IsEnabled = false;
                tboxCT.IsEnabled = false;
                tboxBS.IsEnabled = true;
                tboxX1.IsEnabled = false;
                tboxY1.IsEnabled = false;
                tboxX2.IsEnabled = false;
                tboxY2.IsEnabled = false;
                tboxX3.IsEnabled = false;
                tboxY3.IsEnabled = false;
                tboxP.IsEnabled = false;
                tboxI.IsEnabled = false;
                tboxD.IsEnabled = false;
                tboxMR.IsEnabled = false;
                tboxOH.IsEnabled = false;
                tboxOL.IsEnabled = false;
            }
            else
            {
                tboxFL.IsEnabled = true;
                tboxSC.IsEnabled = true;
                tboxCT.IsEnabled = true;
                tboxBS.IsEnabled = true;
                tboxX1.IsEnabled = true;
                tboxY1.IsEnabled = true;
                tboxX2.IsEnabled = true;
                tboxY2.IsEnabled = true;
                tboxX3.IsEnabled = true;
                tboxY3.IsEnabled = true;
                tboxP.IsEnabled = true;
                tboxI.IsEnabled = true;
                tboxD.IsEnabled = true;
                tboxMR.IsEnabled = true;
                tboxOH.IsEnabled = true;
                tboxOL.IsEnabled = true;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
