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
    /// ParameterRangeSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ParameterRangeSetupDlg : Window
    {
        public ParameterRangeSetupDlg()
        {
            InitializeComponent();
            int[] sdpArr = new int[] { 0, 1, 2, 3 };
            cmbSdp1.ItemsSource = sdpArr;
            cmbSdp2.ItemsSource = sdpArr;
            cmbSdp3.ItemsSource = sdpArr;
            cmbSdp4.ItemsSource = sdpArr;
        }
        void RangeGrpComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox Cmb)
            {
                switch (Cmb.SelectedIndex)
                {
                    case 0:
                        grpBox1.Header = "온도";
                        grpBox2.Header = "챔버 OT";
                        grpBox3.Header = "히터 OT";
                        grpBox4.Header = "차압 챔버";
                        grpBox1.Visibility = Visibility.Visible;
                        grpBox2.Visibility = Visibility.Visible;
                        grpBox3.Visibility = Visibility.Visible;
                        grpBox4.Visibility = Visibility.Visible;
                        tboxRH1.IsEnabled = true;
                        tboxRL1.IsEnabled = true;
                        tboxRH2.IsEnabled = true;
                        tboxRL2.IsEnabled = true;
                        tboxRH3.IsEnabled = true;
                        tboxRL3.IsEnabled = true;
                        break;
                    case 1:
                        grpBox1.Header = "MFC";
                        grpBox2.Header = "차압 필터";
                        grpBox3.Header = "모터 챔버";
                        grpBox4.Header = "";
                        grpBox1.Visibility = Visibility.Visible;
                        grpBox2.Visibility = Visibility.Visible;
                        grpBox3.Visibility = Visibility.Visible;
                        grpBox4.Visibility = Visibility.Hidden;
                        tboxRH1.IsEnabled = false;
                        tboxRL1.IsEnabled = false;
                        tboxRH2.IsEnabled = false;
                        tboxRL2.IsEnabled = false;
                        tboxRH3.IsEnabled = false;
                        tboxRL3.IsEnabled = false;
                        break;
                    case 2:
                        grpBox1.Header = "내부 온도 #1";
                        grpBox2.Header = "내부 온도 #2";
                        grpBox3.Header = "";
                        grpBox4.Header = "";
                        grpBox1.Visibility = Visibility.Visible;
                        grpBox2.Visibility = Visibility.Visible;
                        grpBox3.Visibility = Visibility.Hidden;
                        grpBox4.Visibility = Visibility.Hidden;
                        tboxRH1.IsEnabled = true;
                        tboxRL1.IsEnabled = true;
                        tboxRH2.IsEnabled = true;
                        tboxRL2.IsEnabled = true;
                        tboxRH3.IsEnabled = true;
                        tboxRL3.IsEnabled = true;
                        break;
                }
            }
        }
        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
