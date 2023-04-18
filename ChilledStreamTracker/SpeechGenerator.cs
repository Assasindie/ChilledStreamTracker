using System.Speech.Synthesis;
using OpenAI;

namespace ChilledStreamTracker;

public class SpeechGenerator
{
    private readonly OpenAIClient _openAiClient;

    private readonly SpeechSynthesizer _speech;

    public SpeechGenerator()
    {
        _speech = new SpeechSynthesizer()
        {
            //trying to not lose my ears 
#if DEBUG
            Volume = 25
#else
            Volume = 75
#endif
        };

        _openAiClient = new OpenAIClient(FileUtils.ReadOpenAIKey());
    }

    public async Task GenerateAndSpeak(string text)
    {
        _speech.SpeakAsync(await GenerateResponse("Write a complete sentence of csgo commentary of " + text));
    }

    async Task<string> GenerateResponse(string input)
    {
        var res = await _openAiClient.CompletionsEndpoint.CreateCompletionAsync(input, maxTokens: 250);
        Console.WriteLine($"{DateTime.Now} Input {input} \n AI response : {res.Completions[0]}");
        return res.Completions[0];
    }
}