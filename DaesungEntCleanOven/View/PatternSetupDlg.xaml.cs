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
    /// PatternSetupDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatternSetupDlg : Window
    {
        public PatternSetupDlg()
        {
            InitializeComponent();
            this.cmbConditionUsage.ItemsSource = new string[] { "미사용", "사용" };
            this.cmbExhaustValveOpen.ItemsSource = new string[] { "OFF", "ON" };
        }
        void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }
        void SegmentTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // SEG.TIME은 MAX 999.59 로 제한.
            if (sender is TextBox tbox)
            {
                var Token = tbox.Text.Split('.');
                if (Token.Length == 1)
                {
                    if (Token[0].Length > 3)
                        tbox.Text = Token[0].Substring(0, 3);
                }
                else if (Token.Length == 2)
                {
                    string Hour = Token[0];
                    string Minute = Token[1];
                    if (Hour.Length > 3)
                        Hour = Hour.Substring(0, 3);

                    if (int.TryParse(Minute, out int Tmp))
                    {
                        if (Tmp > 59)
                            Minute = "59";
                    }
                    tbox.Text = string.Format("{0}.{1}", Hour, Minute);
                }
                tbox.CaretIndex = tbox.Text.Length;
            }
        }
    }
}
