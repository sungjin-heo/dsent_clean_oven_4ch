using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Core.Extensions;
using SciChart.Drawing.Common;

namespace DaesungEntCleanOvenDataViewer
{
    class LogPointMarker : BasePointMarker
    {
        private const float TextSize = 10f;
        private const double TextIndent = 3f;
        private IList<IPointMetadata> __DataPointMetadata;
        private IList<int> __DataPointIndexes;
        private readonly TextBlock __TextBlock;

        public LogPointMarker()
        {
            __DataPointIndexes = new List<int>();
            __TextBlock = new TextBlock { FontSize = TextSize };
            SetCurrentValue(PointMarkerBatchStrategyProperty, new DefaultPointMarkerBatchStrategy());
        }
        public override void BeginBatch(IRenderContext2D context, Color? strokeColor, Color? fillColor)
        {
            __DataPointMetadata = __DataPointMetadata ?? RenderableSeries.DataSeries.Metadata;
            __DataPointIndexes = new List<int>();
            base.BeginBatch(context, strokeColor, fillColor);
        }
        public override void MoveTo(IRenderContext2D context, double x, double y, int index)
        {
            if (IsInBounds(x, y))
                __DataPointIndexes.Add(index);
            base.MoveTo(context, x, y, index);
        }
        public override void Draw(IRenderContext2D context, IEnumerable<Point> centers)
        {
            var markerLocations = centers.ToArray();
            for (int i = 0; i < markerLocations.Length; ++i)
            {
                if (__DataPointMetadata[__DataPointIndexes[i]] is LogPointMetaData metadata)
                {
                    var center = markerLocations[i];
                    string value = string.Format("{0:F1}", metadata.Value);
                    __TextBlock.Text = value;
                    __TextBlock.MeasureArrange();

                    var xPos = center.X - __TextBlock.DesiredSize.Width / 2;
                    xPos = xPos < 0 ? TextIndent : xPos;
                    var marginalRightPos = context.ViewportSize.Width - __TextBlock.DesiredSize.Width - TextIndent;
                    xPos = xPos > marginalRightPos ? marginalRightPos : xPos;

                    var yPos = center.Y;
                    var yOffset = __TextBlock.DesiredSize.Height - TextIndent;
                    yPos += yOffset;

                     var textRect = new Rect(xPos, yPos, __TextBlock.DesiredSize.Width, __TextBlock.DesiredSize.Height);
                    context.DrawText(textRect, metadata.Color, TextSize, value, FontFamily, FontWeight, FontStyle);
                }                
            }
        }
    }
}
