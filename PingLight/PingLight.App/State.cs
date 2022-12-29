namespace PingLight.App
{
    internal class State
    {
        public DateTime LastChangeTime { get; set; }
        public bool IsLight { get; set; }

        public State() {
            LastChangeTime = DateTime.UtcNow;
            IsLight = true;
        }
    }
}
