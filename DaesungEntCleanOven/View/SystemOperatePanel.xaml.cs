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
using DaesungEntCleanOven4.ViewModel;
using Util;

namespace DaesungEntCleanOven4.View
{
    /// <summary>
    /// SystemOperatePanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SystemOperatePanel : UserControl
    {
        private ViewModel.ChannelViewModel prevModel;
        public SystemOperatePanel()
        {
            InitializeComponent();
        }
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (prevModel != null)
            {
                prevModel.CleanOvenChamber.Started -= CleanOvenChamber_Started;
                prevModel.CleanOvenChamber.Stopped -= CleanOvenChamber_Stopped;
            }
            if (this.DataContext is ViewModel.ChannelViewModel Model)
            {
                Model.CleanOvenChamber.Started += CleanOvenChamber_Started;
                Model.CleanOvenChamber.Stopped += CleanOvenChamber_Stopped;
                prevModel = Model;
            }
        }
        private void CleanOvenChamber_Started(object sender, EventArgs e)
        {
            btnStart.Content = "정지";
            btnStart.Glyph = BitmapFrame.Create(new Uri("pack://application:,,,/DevExpress.Images.v17.1;component/Images/Arrows/Stop_16x16.png"));
            btnChangePatter.IsEnabled = false;
            btnPause.IsEnabled = true;
            btnAdvance.IsEnabled = true;
        }
        private void CleanOvenChamber_Stopped(object sender, EventArgs e)
        {
            btnStart.Content = "운전";
            btnStart.Glyph = BitmapFrame.Create(new Uri("pack://application:,,,/DevExpress.Images.v17.1;component/Images/Arrows/Play_16x16.png"));
            btnChangePatter.IsEnabled = true;
            btnPause.IsEnabled = false;
            btnAdvance.IsEnabled = false;
        }
    }
}
