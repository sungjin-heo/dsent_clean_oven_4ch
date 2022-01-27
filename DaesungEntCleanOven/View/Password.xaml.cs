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
    /// PasswordDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Password : Window
    {
        public string UserPassword { get; protected set; }
        public Password()
        {
            InitializeComponent();
            passwordBox.Focus();
        }
        void SimpleButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserPassword = passwordBox.Password;
            this.DialogResult = true;
            Close();
        }
    }
}
