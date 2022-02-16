using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciChart.Data.Model;
using SciChart.Charting.Visuals.Axes.LabelProviders;

namespace DaesungEntCleanOven4.View
{
    public class CustomDateTimeLabelProvider : DateTimeLabelProvider
    {
        public override string FormatLabel(IComparable dataValue)
        {
            var Time = (DateTime)dataValue;
            return string.Format("{0}.{1:D2}.{2:D2}\r\n{3:D2}:{4:D2}:{5:D2}", Time.Year.ToString().Substring(2, 2), Time.Month, Time.Day, Time.Hour, Time.Minute, Time.Second);
        }
    }
}
