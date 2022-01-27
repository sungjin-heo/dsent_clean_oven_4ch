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

namespace DaesungEntCleanOvenDataViewer.View
{
    /// <summary>
    /// ParameterSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ParameterSetupDlg : Window
    {
        public ParameterSetupDlg()
        {
            InitializeComponent();
        }
        void VisibleCheckChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox Chk)
            {
                if (gridSeries.ItemsSource is List<FastLineRenderableSeriesEx> Series)
                {
                    foreach (var S in Series)
                        S.IsVisible = (bool)Chk.IsChecked;
                }
            }
        }
        void DataVisibleCheckChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox Chk)
            {
                if (gridSeries.ItemsSource is List<FastLineRenderableSeriesEx> Series)
                {
                    foreach (var S in Series)
                        S.VisiblePtrValue = (bool)Chk.IsChecked;
                }
            }
        }
        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
