using ImageChartsLib;
using System.Text;

namespace PingLight.Core.Charts
{
    public static class BarChartGenerator
    {
        private const string BLUE = "0077CC";
        private const string BAR_CHART_TYPE = "bvs";
        private const int MINS_IN_DAY = 1440;

        public static byte[] Generate(List<TimeSpan> blackouts, DateTime from, DateTime till)
        {
            return generateBarChart(blackouts, from, till)
                .toBuffer();
        }

        private static ImageCharts generateBarChart(List<TimeSpan> dailyBlackouts, DateTime from, DateTime till)
        {
            var data = dailyBlackouts.Select(b => b.TotalMinutes);

            return new ImageCharts()
                .cht(BAR_CHART_TYPE)
                .chtt("Наявність світла")
                .chs("500x300")
                .chd($"a:{string.Join(",", data)}") //data
                .chma("10,30,30,10")
                .chco(BLUE)
                .chxt("x,y") //which axis should have labels
                .chxr($"1,0,{MINS_IN_DAY}") //ranges
                .chxl($"{getXLabels(from, till)}{getYLabels()}") //axis labels
                .chxs("0,s"); //skip some labels on X axis
        }

        //private static string getXLabels(List<TimeSpan> blackouts)
        //{
        //    var sb = new StringBuilder("0:|");

        //    for (int i = 1; i <= blackouts.Count; i++)
        //    {
        //        sb.Append($"{i}|");
        //    }

        //    return sb.ToString();
        //}

        private static string getXLabels(DateTime from, DateTime till)
        {
            var sb = new StringBuilder("0:|");

            var current = from;

            while (current < till)
            {
                sb.Append($"{current.Day}|");
                current = current.AddDays(1);
            }

            return sb.ToString();
        }

        private static string getYLabels() => "1:|0|6|12|18|24";
    }
}
