namespace PingLight.AggregateChanges.Lambda
{
    internal class Change
    {
        public string DeviceId { get; set; }

        public DateTime ChangeDate { get; set; }

        public bool IsLight { get; set; }
    }
}
