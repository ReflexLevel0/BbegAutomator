using BbegAutomator.Leaderboard;
using BbegAutomator.Utils;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BbegAutomator;

public class Program
{
	private IHost _host;
	private IConfig _config;
	
	//TODO: crash the program if an exception occurs 
	private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

	private async Task MainAsync()
	{
		_config = await new Config().LoadFromFile();
		var client = new DiscordSocketClient();
		
		//Setting up dependency injection
		_host = Host
			.CreateDefaultBuilder()
			.ConfigureServices((_, services) =>
				services
					.AddSingleton<DiscordSocketClient>(client)
					.AddSingleton<IConfig>(_config)
					.AddSingleton<ILeaderboardUtils>(sp => new LeaderboardUtils(sp))
					.AddSingleton<ICommandUtils>(sp => new CommandUtils(sp))
					.AddSingleton<IDiscordClientUtils>(sp => new DiscordClientUtils(sp))
					.AddSingleton<IEventUtils>(sp => new EventUtils(sp))
					.AddScoped<ILeaderboard>(sp => new BbegAutomator.Leaderboard.Leaderboard(sp))
				).Build();
		await _host.StartAsync();
		
		client.Log += Log;
		client.SlashCommandExecuted += SlashCommandHandlerAsync;
		client.Ready += InitCommandsAsync;
		await client.LoginAsync(TokenType.Bot, _config.BotToken);
		await client.StartAsync();

		// Block this task until the program is closed.
    while(true){
      Console.WriteLine("\nPress Ctrl+C or ESC to shut down the program");
      var key = Console.ReadKey();
      if((key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control) || key.Key == ConsoleKey.Escape) {
        break;
      }
    }

    await client.StopAsync();
	}

	public async Task Log(LogMessage msg)
	{
		var client = _host.Services.GetRequiredService<DiscordSocketClient>();
		if (client.LoginState == LoginState.LoggedIn)
		{
			foreach (ulong id in _config.LoggingIds)
			{
				var user = await client.GetUserAsync(id);
				await user.SendMessageAsync(msg.Message);
			}
		}

		Console.WriteLine(msg.ToString());
	}

	private async Task InitCommandsAsync()
	{
		//Setting up app commands
		var client = _host.Services.GetRequiredService<DiscordSocketClient>();
		var guild = client.GetGuild(_config.GuildId);
		try
		{
			foreach (var command in _host.Services.GetRequiredService<ICommandUtils>().GetSlashCommands())
			{
				await guild.CreateApplicationCommandAsync(command.Build());
			}
		}
		catch (HttpException exception)
		{
			Console.WriteLine(exception.Message);
		}
	}

	private async Task SlashCommandHandlerAsync(SocketCommandBase command) =>
		_config = await _host.Services.GetRequiredService<ICommandUtils>().ExecuteCommand(command);
}
