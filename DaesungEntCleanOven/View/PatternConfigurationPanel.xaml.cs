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
using SciChart.Charting.Model.DataSeries;

namespace DaesungEntCleanOven4.View
{
    /// <summary>
    /// PatternConfigurationPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatternConfigurationPanel : UserControl
    {
        public PatternConfigurationPanel()
        {
            InitializeComponent();
            yaxis11.LabelProvider = new ColoredMultiRowLabelProvider(yaxis11, yaxis12, yaxis13, yaxis14, yaxis15);
            yaxis21.LabelProvider = new ColoredMultiRowLabelProvider(yaxis21, yaxis22, yaxis23, yaxis24, yaxis25);
        }
    }
}
