namespace PingLight.Lambda
{
    internal class Config
    {
        public DateTime TimeOfLastChange { get; set; }
        public bool IsLight { get; set; }

        public Config(DateTime time, bool isLight)
        {
            TimeOfLastChange = time;
            IsLight = isLight;
        }
    }
}
