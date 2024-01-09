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
    public partial class View0 : DXRibbonWindow
    {
        public View0()
        {
            InitializeComponent();
        }
        private void DXRibbonWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is ViewModel.MainViewModel Model)
            {
                Model.DetailViewMoveRequested += Model_DetailViewMoveRequested;
                Model.IntegrateViewReturnRequested += Model_IntegrateViewReturnRequested;
                Model.PropertyChanged += Model_PropertyChanged;
            }
        }
        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedChannel")
            {
                Title = string.Format("N2 클린 오븐 - 대성이앤티 Co. Ltd - {0}채널", (sender as MainViewModel).SelectedChannel.No);
            }
        }
        private void Model_IntegrateViewReturnRequested(object sender, EventArgs e)
        {
            view1.Visibility = Visibility.Hidden;
            view2.Visibility = Visibility.Visible;
            toolBar.Visibility = Visibility.Visible;
            efemStateView.Visibility = Visibility.Hidden;
        //    efemLogView.Visibility = Visibility.Hidden;
            Title = "N2 클린 오븐 - 대성이앤티 Co. Ltd";
        }
        private void Model_DetailViewMoveRequested(object sender, EventArgs e)
        {
            view1.Visibility = Visibility.Visible;
            view2.Visibility = Visibility.Hidden;
            toolBar.Visibility = Visibility.Collapsed;
            efemStateView.Visibility = Visibility.Hidden;
        //    efemLogView.Visibility = Visibility.Hidden;
        }

        private void ShowEfemSystemStateView_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            view1.Visibility = Visibility.Hidden;
            view2.Visibility = Visibility.Hidden;
            toolBar.Visibility = Visibility.Visible;
            efemStateView.Visibility = Visibility.Visible;
        //    efemLogView.Visibility = Visibility.Hidden;
            Title = "N2 클린 오븐 - 대성이앤티 Co. Ltd";
        }
//         private void ShowEfemMessageLogView_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
//         {
//             view1.Visibility = Visibility.Hidden;
//             view2.Visibility = Visibility.Hidden;
//             toolBar.Visibility = Visibility.Visible;
//             efemStateView.Visibility = Visibility.Hidden;
//         //    efemLogView.Visibility = Visibility.Visible;
//             Title = "N2 클린 오븐 - 대성이앤티 Co. Ltd";
//         }
    }
}
