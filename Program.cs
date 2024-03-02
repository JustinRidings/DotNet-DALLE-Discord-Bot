using Discord;
using Discord.WebSocket;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Discord.Net;
using Newtonsoft.Json;

public class Function1
{
    private static DiscordSocketClient _client = default!;
    private static OpenAIService _openAiService = default!;

    private static async Task Main()
    {
        _client = new DiscordSocketClient();

        _openAiService = new OpenAIService(new OpenAiOptions()
        {
            // Your OpenAI API Key
            ApiKey = ""
        });

        // Your Discord bot token
        string botToken = "";

        _client.Ready += ClientReady;

        _client.SlashCommandExecuted += DrawCommandHandler;

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();
        Console.ReadLine();
    }

    private static async Task ClientReady()
    {
        // Let's do our global command
        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("draw");
        globalCommand.WithDescription("This command generates a DALLE image using the provided message text.");
        globalCommand.AddOption(name: "text", ApplicationCommandOptionType.String, "Add your command", isRequired: true);

        try
        {

            // With global commands we don't need the guild.
            await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch (HttpException exception)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(json);
        }
    }

    private static async Task DrawCommandHandler(SocketSlashCommand command)
    {
        var optionText = (string)command.Data.Options.First().Value;

        Task t1 = Task.Run(() => command.DeferAsync());
        Task<List<Embed>> t2 = Task.Run(() => QueryDalle(optionText));
        await Task.WhenAll(t1, t2);
        if(t2.IsCompletedSuccessfully)
        {
            var embeds = t2.Result;

            if (embeds[0] != null && embeds[1] != null)
            {
                await command.ModifyOriginalResponseAsync(x => x.Embeds = embeds.ToArray());
            }
            else
            {
                await command.RespondAsync("Something went wrong, please try again.");
            }
        }
    }

    private static async Task<List<Embed>> QueryDalle(string message)
    {
        var embedList = new List<Embed>();

        if (!string.IsNullOrEmpty(message))
        {
            var imageResult = await _openAiService.Image.CreateImage(new ImageCreateRequest
            {
                Prompt = message,
                N = 2,
                Size = StaticValues.ImageStatics.Size.Size1024,
                ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url,
                User = "" // Ideally you want to pass-through the ID of the user who ran the command
                          // for auditing purposes
            });

            if (imageResult.Successful)
            {
                foreach(var result in imageResult.Results)
                {
                    var emb = new EmbedBuilder()
                    .WithImageUrl(result.Url).Build();

                    embedList.Add(emb);
                }
                return embedList;
            }
            else
            {
                return embedList;
            }
        }
        return embedList;
    }
}