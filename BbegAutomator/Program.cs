using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BbegAutomator;

public class Program
{
	//TODO: crash the program if an exception occurs 
	private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

	private IHost _host;
	private static readonly DiscordSocketClient Client = new();
	private static Config _config;

	private async Task MainAsync()
	{
		_config = await Config.GetConfig();

		//Setting up dependency injection
		_host = Host
			.CreateDefaultBuilder()
			.ConfigureServices((_, services) =>
				services
					.AddSingleton(Client)
					.AddSingleton<IDiscordClient>(Client)
					.AddSingleton(_config)).Build();

		Client.Log += Log;

		Client.SlashCommandExecuted += SlashCommandHandlerAsync;
		Client.Ready += InitCommandsAsync;

		await Client.LoginAsync(TokenType.Bot, _config.BotToken);
		await Client.StartAsync();

		// Block this task until the program is closed.
		await Task.Delay(-1);
	}

	public static async Task Log(LogMessage msg)
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
			foreach (var command in new CommandUtils(_host.Services).GetSlashCommands())
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
		_config = await new CommandUtils(_host.Services).ExecuteCommand(command);
}