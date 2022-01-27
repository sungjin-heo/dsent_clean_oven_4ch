using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Axes.LabelProviders;
using SciChart.Core.Extensions;

namespace DaesungEntCleanOven.View
{
    class ColoredMultiRowLabelProvider : NumericLabelProvider
    {
        protected AxisCore[] Axes;
        public ColoredMultiRowAxisLabelViewModel Model { get; protected set; }
        public ColoredMultiRowLabelProvider(params AxisCore[] axes)
        {
            this.Axes = axes;
            this.Model = new ColoredMultiRowAxisLabelViewModel(axes);
        }
        public override ITickLabelViewModel CreateDataContext(System.IComparable dataValue)
        {
            return UpdateDataContext(Model, dataValue);
        }
        public override ITickLabelViewModel UpdateDataContext(ITickLabelViewModel labelDataContext, System.IComparable dataValue)
        {
            return (ColoredMultiRowAxisLabelViewModel)labelDataContext;
        }
    }

    class ColoredMultiRowAxisLabelViewModel : NumericTickLabelViewModel
    {
        public ColoredMultiRowAxisLabelViewModel(params AxisCore[] axes)
        {
            this.AxisLabels = new List<ColoredAxisLabel>();
            foreach (var axis in axes)
                this.AxisLabels.Add(new ColoredAxisLabel(axis));
        }
        public List<ColoredAxisLabel> AxisLabels { get; protected set; }
    }

    class ColoredAxisLabel : DefaultTickLabelViewModel
    {
        protected AxisCore Axis;
        protected int Index;
        protected readonly int Cnt;
        public ColoredAxisLabel(AxisCore axis)
        {
            this.Axis = axis;
            this.Cnt = (int)(Math.Abs(Axis.VisibleRange.Max.ToDouble() - Axis.VisibleRange.Min.ToDouble()) / Axis.MajorDelta.ToDouble()) + 1;
        }
        public System.Windows.Media.Brush Color { get { return Axis.TickTextBrush; } }
        public string Label
        {
            get
            {
                double Max = Axis.VisibleRange.Max.ToDouble();
                double Min = Axis.VisibleRange.Min.ToDouble();
                double Delta = Axis.MajorDelta.ToDouble();
                string Tmp = string.Format("{0}", Min + (Index++ * Delta));
                Index %= Cnt;
                return Tmp;
            }
        }
    }
}
