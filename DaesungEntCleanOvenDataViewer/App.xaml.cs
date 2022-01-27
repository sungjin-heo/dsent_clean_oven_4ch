using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.Core;
using SciChart.Charting.Visuals;

namespace DaesungEntCleanOvenDataViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            SciChartSurface.SetRuntimeLicenseKey("p3bNNejkpJqXC+BJl9IDpd5Furg4uS/jsxoEhT2sU5aoSs3ub2WHNFaO9577WbVozb+UiwBI/sgDjw4K1SaqEZniaet97yPhEfHYbx1XhyQUtesdbrD8AEF9Rbn8vx+Uk744E+guNMmVWqkGDS0wnXVXCrzQYIWU8vc3wB0bdeNDp5QTnS93N1RhI9vNK+lis1cJGJ+G+u1G33waKTSzOZeaynKQ4ZdnYi/q4voCsA80tO2aB1el2oxL2W+SYpopFmtXcnbdoGVZhuu2DaXaAufo8BdFUfOIL+B4SOJpWMXJovGaDPpNgVPtuaLTUXEHkQmHZ/avPJCIJikzAtOUpzuhYV6OU49z9+1CNS9siQKXeBAuDRArFsHoTrdcniYlAJx9A8BSg6PNz0LEGNS/tQ5HT+vLRbktgcuKdWnTx96hfPWtiF6ri2O57LdckPhDm9H20jXLsB34VqRr2FgzSgXwli3KH++PSx/YmMztfmOcQf4Rsx4=");
        }
        private void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
        }
    }
}
