using PingLight.Core.Model;

namespace PingLight.Core.Tests
{
    internal class BlackoutCalculatorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Calculate_TwoEasyChanges_Returns_Correct_Value()
        {
            var from = DateTime.Parse("2023-01-09T00:00:00.0000000Z").ToUniversalTime();
            var till = DateTime.Parse("2023-01-10T00:00:00.0000000Z").ToUniversalTime();

            var changes = new List<Change>
            {
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T09:00:00.0000000Z").ToUniversalTime(), IsLight = false },
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T10:00:00.0000000Z").ToUniversalTime(), IsLight = true },
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T11:00:00.0000000Z").ToUniversalTime(), IsLight = false },
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T13:00:00.0000000Z").ToUniversalTime(), IsLight = true }
            };

            var blackouts = BlackoutCalculator.Calculate(changes, from, till);

            Assert.That(blackouts.Count, Is.EqualTo(2));
            Assert.That(blackouts[0].Hours, Is.EqualTo(1));
            Assert.That(blackouts[1].Hours, Is.EqualTo(2));
        }

        [Test]
        public void Calculate_StartingPartialBlackout_Returns_Correct_Value()
        {
            var from = DateTime.Parse("2023-01-09T00:00:00.0000000Z").ToUniversalTime();
            var till = DateTime.Parse("2023-01-10T00:00:00.0000000Z").ToUniversalTime();

            var changes = new List<Change>
            {
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T09:00:00.0000000Z").ToUniversalTime(), IsLight = true }
            };

            var blackouts = BlackoutCalculator.Calculate(changes, from, till);

            Assert.That(blackouts.Count, Is.EqualTo(1));
            Assert.That(blackouts[0].Hours, Is.EqualTo(9));
        }

        [Test]
        public void Calculate_EndingPartialBlackout_TotalHours_Returns_Correct_Value()
        {
            var from = DateTime.Parse("2023-01-09T00:00:00.0000000Z").ToUniversalTime();
            var till = DateTime.Parse("2023-01-10T00:00:00.0000000Z").ToUniversalTime();

            var changes = new List<Change>
            {
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T00:00:00.0000000Z").ToUniversalTime(), IsLight = false }
            };

            var blackouts = BlackoutCalculator.Calculate(changes, from, till);

            Assert.That(blackouts.Count, Is.EqualTo(1));
            Assert.That(blackouts[0].Hours, Is.EqualTo(0));
            Assert.That(blackouts[0].TotalHours, Is.EqualTo(24));
        }

        [Test]
        public void Calculate_TwoPartialBlackouts_Returns_Correct_Value()
        {
            var from = DateTime.Parse("2023-01-09T00:00:00.0000000Z").ToUniversalTime();
            var till = DateTime.Parse("2023-01-10T00:00:00.0000000Z").ToUniversalTime();

            var changes = new List<Change>
            {
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T09:00:00.0000000Z").ToUniversalTime(), IsLight = true },
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T10:00:00.0000000Z").ToUniversalTime(), IsLight = false },
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T11:00:00.0000000Z").ToUniversalTime(), IsLight = true },
                new Change() { DeviceId = "test", ChangeDate = DateTime.Parse("2023-01-09T12:00:00.0000000Z").ToUniversalTime(), IsLight = false }
            };

            var blackouts = BlackoutCalculator.Calculate(changes, from, till);

            Assert.That(blackouts.Count, Is.EqualTo(3));
            Assert.That(blackouts[0].Hours, Is.EqualTo(9));
            Assert.That(blackouts[1].Hours, Is.EqualTo(1));
            Assert.That(blackouts[2].Hours, Is.EqualTo(12));
        }

        [Test]
        public void Calculate_NoChanges_Returns_Correct_Value()
        {
            var from = DateTime.Parse("2023-01-09T00:00:00.0000000Z").ToUniversalTime();
            var till = DateTime.Parse("2023-01-10T00:00:00.0000000Z").ToUniversalTime();

            var changes = new List<Change>();

            var blackouts = BlackoutCalculator.Calculate(changes, from, till);

            Assert.That(blackouts.Count, Is.EqualTo(0));
        }
    }
}
