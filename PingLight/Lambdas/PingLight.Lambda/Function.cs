using Amazon.Lambda.Core;
using PingLight.Core;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.CamelCaseLambdaJsonSerializer))]

namespace PingLight.Lambda;

public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// 

    private static string Host = "46.30.167.101";
    private static string Token = "";
    private static string ChannelId = "";

    public async Task<string> FunctionHandler(JsonObject input, ILambdaContext context)
    {
        var success = PingTools.Ping(Host);
        var bot = new ChatBot(Token);
        var timespan = TimeSpan.FromHours(4);
        var message = success ? bot.GetLightOnMessage(timespan) : bot.GetLightOffMessage(timespan);

        await bot.Post(message, ChannelId);

        return message;
    }
}
