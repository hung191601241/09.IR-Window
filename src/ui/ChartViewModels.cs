using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace VisionInspection
{
    class BarChartViewModel
    {
        public PlotModel MyModel { get; private set; }

        public BarChartViewModel(String title, List<double> values, List<string> axeLabels) {
            this.MyModel = new PlotModel { Title = title };

            var barItems = new List<BarItem>();
            double maxVal = 0;
            foreach (var v in values) {
                barItems.Add(new BarItem(v));
                if (v > maxVal) {
                    maxVal = v;
                }
            }

            var barSeries = new BarSeries
            {
                ItemsSource = barItems,
                LabelMargin = 5,
                LabelPlacement = LabelPlacement.Outside,
                TextColor = OxyColors.Blue,
                LabelFormatString = "{0}",
            };

            barSeries.FillColor = OxyColors.SkyBlue;

            MyModel.Series.Add(barSeries);

            MyModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                TickStyle = TickStyle.None,
                ItemsSource = axeLabels,
                IsZoomEnabled = false,
                IsPanEnabled = false
            }); ;

            var X = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Maximum = maxVal * 1.1,
                IsZoomEnabled = false,
                IsPanEnabled = false
            };
            MyModel.Axes.Add(X);
        }
    }

    class LineChartViewModel
    {
        public PlotModel MyModel { get; set; }

        public LineChartViewModel(String xAxisName, String yAxisName, List<double> values, List<string> xAxisLabels) {
            this.MyModel = new PlotModel();

            var dataPoints = new List<DataPoint>();
            double maxVal = 0;
            double minVal = double.MaxValue;
            for (int i = 0; i < values.Count; i++) {
                var v = values[i];
                dataPoints.Add(new DataPoint(i, v));
                if (v > maxVal) {
                    maxVal = v;
                }
                if (v < minVal) {
                    minVal = v;
                }
            }
            var lineSeries = new LineSeries
            {
                ItemsSource = dataPoints,
                MarkerFill = OxyColors.Red,
                MarkerType = MarkerType.Circle
            };
            MyModel.Series.Add(lineSeries);

            MyModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = xAxisName,
                TickStyle = TickStyle.None,
                ItemsSource = xAxisLabels,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });

            var Y = new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = yAxisName,
                Minimum = minVal * 0.9,
                Maximum = maxVal * 1.1,
                IsZoomEnabled = false,
                IsPanEnabled = false
            };
            MyModel.Axes.Add(Y);
        }
    }
}
