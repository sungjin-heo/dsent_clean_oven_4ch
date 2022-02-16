using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Ribbon;
using DaesungEntCleanOven4.ViewModel;

namespace DaesungEntCleanOven4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXRibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        void DXRibbonWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is ViewModel.ChannelViewModel Model) {
                Model.PropertyChanged += (s, arg) => {
                    switch (arg.PropertyName)
                    {
                        case "ViewType":
                            {
                                switch (Model.ViewType)
                                {
                                    case ViewType.None:
                                        viewSystemOperateStatus.Visibility = Visibility.Hidden;
                                        viewPatternConfiguration.Visibility = Visibility.Hidden;
                                        viewRealtimeTrendGraph.Visibility = Visibility.Hidden;
                                        viewNone.Visibility = Visibility.Visible;
                                        break;
                                    case ViewType.SystemOperateStatus:
                                        viewSystemOperateStatus.Visibility = Visibility.Visible;
                                        viewPatternConfiguration.Visibility = Visibility.Hidden;
                                        viewRealtimeTrendGraph.Visibility = Visibility.Hidden;
                                        viewNone.Visibility = Visibility.Hidden;
                                        break;
                                    case ViewType.PatternConfiguration:
                                        viewSystemOperateStatus.Visibility = Visibility.Hidden;
                                        viewPatternConfiguration.Visibility = Visibility.Visible;
                                        viewRealtimeTrendGraph.Visibility = Visibility.Hidden;
                                        viewNone.Visibility = Visibility.Hidden;
                                        break;
                                    case ViewType.RealtimeTrendGraph:
                                        viewSystemOperateStatus.Visibility = Visibility.Hidden;
                                        viewPatternConfiguration.Visibility = Visibility.Hidden;
                                        viewRealtimeTrendGraph.Visibility = Visibility.Visible;
                                        viewNone.Visibility = Visibility.Hidden;
                                        break;
                                }
                            }
                            break;
                    }
                };
            }
        }
    }
}
