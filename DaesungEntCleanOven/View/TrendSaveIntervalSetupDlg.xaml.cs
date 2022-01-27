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
    /// TrendSaveIntervalSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TrendSaveIntervalSetupDlg : Window
    {
        public int SaveInterval { get; protected set; }
        public TrendSaveIntervalSetupDlg(int Intv)
        {
            InitializeComponent();
            this.tboxSaveInterval.Text = Intv.ToString();
        }
        void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tboxSaveInterval.Text, out int Tmp))
            {
                this.SaveInterval = Tmp;
                this.DialogResult = true;
            }
            Close();
        }
    }
}
