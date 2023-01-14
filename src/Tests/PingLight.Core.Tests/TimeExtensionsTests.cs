namespace PingLight.Core.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TestCase(0, ExpectedResult = "")]
        [TestCase(1, ExpectedResult = "1 дня")]
        [TestCase(2, ExpectedResult = "2 днів")]
        [TestCase(3, ExpectedResult = "3 днів")]
        [TestCase(10, ExpectedResult = "10 днів")]
        [TestCase(11, ExpectedResult = "11 днів")]
        [TestCase(13, ExpectedResult = "13 днів")]
        [TestCase(14, ExpectedResult = "14 днів")]
        [TestCase(15, ExpectedResult = "15 днів")]
        [TestCase(21, ExpectedResult = "21 дня")]
        [TestCase(22, ExpectedResult = "22 днів")]
        public string GetDays_Returns_Correct_Value(int days)
        {
            var timespan = TimeSpan.FromDays(days);

            return timespan.getDays();
        }

        [TestCase(0, ExpectedResult = "")]
        [TestCase(1, ExpectedResult = "1 години")]
        [TestCase(2, ExpectedResult = "2 годин")]
        [TestCase(3, ExpectedResult = "3 годин")]
        [TestCase(10, ExpectedResult = "10 годин")]
        [TestCase(11, ExpectedResult = "11 годин")]
        [TestCase(13, ExpectedResult = "13 годин")]
        [TestCase(14, ExpectedResult = "14 годин")]
        [TestCase(15, ExpectedResult = "15 годин")]
        [TestCase(21, ExpectedResult = "21 години")]
        [TestCase(22, ExpectedResult = "22 годин")]
        public string GetHours_Returns_Correct_Value(int hours)
        {
            var timespan = TimeSpan.FromHours(hours);

            return timespan.getHours();
        }

        [TestCase(0, ExpectedResult = "")]
        [TestCase(1, ExpectedResult = "1 хвилини")]
        [TestCase(2, ExpectedResult = "2 хвилин")]
        [TestCase(3, ExpectedResult = "3 хвилин")]
        [TestCase(10, ExpectedResult = "10 хвилин")]
        [TestCase(11, ExpectedResult = "11 хвилин")]
        [TestCase(13, ExpectedResult = "13 хвилин")]
        [TestCase(14, ExpectedResult = "14 хвилин")]
        [TestCase(15, ExpectedResult = "15 хвилин")]
        [TestCase(21, ExpectedResult = "21 хвилини")]
        [TestCase(22, ExpectedResult = "22 хвилин")]
        public string GetMinutes_Returns_Correct_Value(int minutes)
        {
            var timespan = TimeSpan.FromMinutes(minutes);

            return timespan.getMinutes();
        }

        [TestCase(0, ExpectedResult = "")]
        [TestCase(1, ExpectedResult = "1 раз")]
        [TestCase(2, ExpectedResult = "2 рази")]
        [TestCase(3, ExpectedResult = "3 рази")]
        [TestCase(4, ExpectedResult = "4 рази")]
        [TestCase(5, ExpectedResult = "5 разів")]
        [TestCase(10, ExpectedResult = "10 разів")]
        [TestCase(11, ExpectedResult = "11 разів")]
        [TestCase(13, ExpectedResult = "13 разів")]
        [TestCase(14, ExpectedResult = "14 разів")]
        [TestCase(15, ExpectedResult = "15 разів")]
        [TestCase(21, ExpectedResult = "21 раз")]
        [TestCase(22, ExpectedResult = "22 рази")]
        public string GetTimes_Returns_Correct_Value(int times)
        {
            return times.getTimes();
        }
    }
}