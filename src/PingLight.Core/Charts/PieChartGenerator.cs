using ImageChartsLib;

namespace PingLight.Core.Charts
{
    public static class PieChartGenerator
    {
        private const string RED = "ps0-0,lg,45,ffeb3b,0.2,f44336,1";
        private const string GREEN = "ps0-1,lg,45,8bc34a,0.2,009688,1";
        private const string CHART_TYPE = "p3";

        public static byte[] Generate(int presentPercents, int absentPercents)
        {
            return generatePieChart(presentPercents, absentPercents)
                .toBuffer();
        }

        private static ImageCharts generatePieChart(int presentPercents, int absentPercents)
        {
            return new ImageCharts()
                .cht(CHART_TYPE)
                .chs("500x300")
                .chd($"a:{absentPercents},{presentPercents}")
                .chdl("Відключення світла|Є світло")
                .chl($"{absentPercents}%|{presentPercents}%")
                .chf($"{RED}|{GREEN}")
                .chma("30,30,30,30");
        }
    }
}
