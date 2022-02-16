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
    /// PatternTimeSignalSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatternTimeSignalSetupDlg : Window
    {
        public PatternTimeSignalSetupDlg()
        {
            InitializeComponent();
            string[] Opt = new string[] { "OFF", "ON" };
            cmb1.ItemsSource = Opt;
            cmb2.ItemsSource = Opt;
            cmb3.ItemsSource = Opt;
            cmb4.ItemsSource = Opt;
            cmb5.ItemsSource = Opt;
            cmb6.ItemsSource = Opt;
            cmb7.ItemsSource = Opt;
            cmb8.ItemsSource = Opt;
        }
        void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }
    }
}
