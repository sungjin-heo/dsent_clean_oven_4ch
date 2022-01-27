using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.Core;
using System.Threading;
using SciChart.Charting.Visuals;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;


namespace DaesungEntCleanOven
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Mutex Instance;
        static string InstanceId = "890d2f7c-3f3a-4e32-9b0a-398e98f87bb7";

        public App()
        {
            SciChartSurface.SetRuntimeLicenseKey("p3bNNejkpJqXC+BJl9IDpd5Furg4uS/jsxoEhT2sU5aoSs3ub2WHNFaO9577WbVozb+UiwBI/sgDjw4K1SaqEZniaet97yPhEfHYbx1XhyQUtesdbrD8AEF9Rbn8vx+Uk744E+guNMmVWqkGDS0wnXVXCrzQYIWU8vc3wB0bdeNDp5QTnS93N1RhI9vNK+lis1cJGJ+G+u1G33waKTSzOZeaynKQ4ZdnYi/q4voCsA80tO2aB1el2oxL2W+SYpopFmtXcnbdoGVZhuu2DaXaAufo8BdFUfOIL+B4SOJpWMXJovGaDPpNgVPtuaLTUXEHkQmHZ/avPJCIJikzAtOUpzuhYV6OU49z9+1CNS9siQKXeBAuDRArFsHoTrdcniYlAJx9A8BSg6PNz0LEGNS/tQ5HT+vLRbktgcuKdWnTx96hfPWtiF6ri2O57LdckPhDm9H20jXLsB34VqRr2FgzSgXwli3KH++PSx/YmMztfmOcQf4Rsx4=");
            XmlConfigurator.Configure(new System.IO.FileInfo("log4net.xml"));
        }
        void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            if (IsApplicationRunning())
            {
                string title = "대성ENT - N2 CLEAN OVEN";
                MessageBox.Show(string.Format("\"{0}\" 이미 실행 중입니다...", title), title, MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
        }
        bool IsApplicationRunning()
        {
            try
            {
                Instance = Mutex.OpenExisting(InstanceId);
                return true;
            }
            catch (WaitHandleCannotBeOpenedException ex)
            {
                Instance = new Mutex(false, InstanceId);
            }
            return false;
        }
        void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
        }
    }
}
