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
using DaesungEntCleanOven.Properties;

namespace DaesungEntCleanOven.View
{
    /// <summary>
    /// PasswordSettingDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PasswordSettingDlg : Window
    {
        public string OldPassword { get; protected set; }
        public string NewPassword { get; protected set; }
        public PasswordSettingDlg()
        {
            InitializeComponent();
        }
        void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = passwordBoxOld.Password;
                if (password != Properties.Resources.AdministratorPassword && password != Settings.Default.UserPassword)
                    throw new Exception("기존 패스워드가 일치하지 않습니다");

                if (passwordBox1.Password != passwordBox2.Password)
                    throw new Exception("신규 패스워드가 일치하지 않습니다");

                this.NewPassword = passwordBox1.Password;
                this.DialogResult = true;
            }
            catch(Exception ex)
            {
                this.DialogResult = false;
                MessageBox.Show(ex.Message, Properties.Resources.AppCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }
    }
}
