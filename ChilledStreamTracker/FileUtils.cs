using System.Text;
using Newtonsoft.Json.Linq;

namespace ChilledStreamTracker;

public class FileUtils
{
    public static async Task WriteFile(string kdInfo, TimeSpan timeAlive, TimeSpan timeDead)
    {
        var sb = new StringBuilder();
        sb.AppendLine(kdInfo);
        var aliveString =
            $"Alive for {timeAlive:mm\\:ss}, Dead for {timeDead:mm\\:ss}";
        sb.AppendLine(aliveString);
        var stats = sb.ToString();
        try
        {
            await File.WriteAllTextAsync(@"C:\stream\chilledKD.txt", stats);
        }
        catch
        {
            // ignored - could be currently being read for updating in stream
        }
    }

    public static string ReadOpenAIKey()
    {
        var jsonObj = JToken.Parse(File.ReadAllText(Environment.CurrentDirectory + "\\key.json"));
        return (string)jsonObj["key"];
    }
}