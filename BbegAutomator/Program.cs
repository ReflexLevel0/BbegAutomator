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
	private readonly DiscordSocketClient Client = new();
	private IConfig _config;
	
	//TODO: crash the program if an exception occurs 
	private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

	private async Task MainAsync()
	{
		_config = await new Config().LoadFromFile();

		//Setting up dependency injection
		_host = Host
			.CreateDefaultBuilder()
			.ConfigureServices((_, services) =>
				services
					.AddSingleton(Client)
					.AddSingleton<IDiscordClient>(Client)
					.AddSingleton<IConfig>(_config)
					.AddSingleton<ILeaderboardUtils>(sp => new LeaderboardUtils(sp))
					.AddSingleton<ICommandUtils>(sp => new CommandUtils(sp))
					.AddSingleton<IDiscordClientUtils>(sp => new DiscordClientUtils(sp))
					.AddSingleton<IEventUtils>(sp => new EventUtils(sp))
					.AddScoped<ILeaderboard>(sp => new BbegAutomator.Leaderboard.Leaderboard(sp))
				).Build();
		await _host.StartAsync();
		
		Client.Log += Log;
		Client.SlashCommandExecuted += SlashCommandHandlerAsync;
		Client.Ready += InitCommandsAsync;
		await Client.LoginAsync(TokenType.Bot, _config.BotToken);
		await Client.StartAsync();
		
		// Block this task until the program is closed.
		await Task.Delay(-1);
	}

	public async Task Log(LogMessage msg)
	{
		if (Client.LoginState == LoginState.LoggedIn)
		{
			foreach (ulong id in _config.LoggingIds)
			{
				var user = await Client.GetUserAsync(id);
				await user.SendMessageAsync(msg.Message);
			}
		}

		Console.WriteLine(msg.ToString());
	}

	private async Task InitCommandsAsync()
	{
		//Setting up app commands
		var guild = Client.GetGuild(_config.GuildId);
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