﻿using System;
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
    /// PatternSelectDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatternSelectDlg : Window
    {
        public Model.PatternMetadata SelectedMetaData { get; protected set; }
        public PatternSelectDlg()
        {
            InitializeComponent();
        }
        void UnregisterBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (gridPatternMetaDatas.SelectedItem is Model.PatternMetadata metaData)
            {
                if (string.IsNullOrEmpty(metaData.RegisteredScanCode))
                    return;

                var qDlg = new View.Question(string.Format("바코드 : {0}\r\n등록 패턴 번호 : {1}\r\n등록 해제 합니다.", metaData.RegisteredScanCode, metaData.No));
                if ((bool)qDlg.ShowDialog())
                {
                    metaData.RegisteredScanCode = null;
                    G.SavePatternMetaData();
                    Close();
                }
            }
        }
        void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.SelectedMetaData = gridPatternMetaDatas.SelectedItem as Model.PatternMetadata;
            Close();
        }
    }
}
