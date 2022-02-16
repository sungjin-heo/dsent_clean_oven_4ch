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

using DsFourChamberLib.DataComm;
using Mp.Lib.IO;
using System.Xml.Serialization;
using System.IO;
using System.Net.Sockets;

namespace DaesungEntCleanOven4
{
    /// <summary>
    /// Window1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EfemCommDataStatus m = new EfemCommDataStatus();

            byte[] MessageHeader = { 0xFF, 0xFE, 0xEF, 0xFF };

            // conv
            byte[] arr = null;
            using (MemoryStream wms = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(wms))
                {
                    Type t = m.GetType();
                    w.Write(t.Name);

                    XmlSerializer ser = new XmlSerializer(t);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ser.Serialize(ms, m);
                        w.Write(ms.ToArray());
                    }
                    w.Flush();
                    arr = wms.ToArray();
                }
            }
           
            

//             NetworkStream MyStream;
// 
//             // send
//             MyStream.Write(MessageHeader, 0, MessageHeader.Length);
//             MyStream.Write(BitConverter.GetBytes((int)arr.Length), 0, 4);
//             MyStream.Write(arr, 0, arr.Length);
//             MyStream.Flush();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {

        }
    }
}
