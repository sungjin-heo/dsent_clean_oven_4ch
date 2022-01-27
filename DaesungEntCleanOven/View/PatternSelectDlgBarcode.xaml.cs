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
using DaesungEntCleanOven.ViewModel;

namespace DaesungEntCleanOven.View
{
    /// <summary>
    /// PatternSelectDlgBarcode.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatternSelectDlgBarcode : Window
    {
        public int? PatternNo { get; protected set; }
        public PatternSelectDlgBarcode()
        {
            InitializeComponent();
            tboxScanCode.Focus();
            tboxScanCode.SelectAll();
        }
        void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            this.PatternNo = null;

            string ScanCode = tboxScanCode.Text;
            if (!string.IsNullOrEmpty(ScanCode))
            {
                var query = (from metaData in G.PatternMetaDatas
                             where metaData.Name == ScanCode
                             select metaData).ToArray();

                if (query == null || query.Length == 0)
                    tboxSearchResult.Text = "현재 바코드에 등록된 패턴을 찾을 수 없습니다.";
                else
                {
                    tboxSearchResult.Text = string.Format("패턴 번호 : {0}, 패턴 명 : {1}", query[0].No, query[0].Name);
                    this.PatternNo = query[0].No;
                }
            }
        }
        void ApplyPatternButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DataContext = false;
            Close();
        }
    }
}
