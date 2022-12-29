using PingLight.Core;
using System.Text.Json;

namespace PingLight.App
{
    internal static class StateManager
    {
        private static string fileName = "state.json";

        public static State LoadOrCreateState()
        {
            try
            {
                if (File.Exists(fileName))
                {
                    var jsonString = File.ReadAllText(fileName);
                    var state = JsonSerializer.Deserialize<State>(jsonString) ?? new State();

                    return state;
                }
                else
                {
                    var state = new State();
                    string jsonString = JsonSerializer.Serialize(state);
                    File.WriteAllText(fileName, jsonString);

                    return state;
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(ex);

                return new State();
            }
        }

        public static void SaveState(State state)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(state);
                File.WriteAllText(fileName, jsonString);
            }
            catch (Exception ex)
            {
                Logging.LogError(ex);
            }
        }
    }
}
