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

namespace DaesungEntCleanOven.View
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
                "온도", "챔버  - OT", "히터 - OT", "차압 챔버", "MFC", "차압 필터", "모터 챔버", "모터 쿨링", "내부 온도 #1", "내부 온도 #2", "내부 온도 #3", "내부 온도 #4"
            };
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
