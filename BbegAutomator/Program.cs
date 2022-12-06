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
	private const string UpdateCommandName = "update";
	private const string CreateEventCommandName = "event-create";
	private const string RenameEventCommandName = "event-rename";
	private const string DeleteEventCommandName = "event-delete";
	private const string ListLeaderboardCommandName = "leaderboard-list";
	private const string ListAllLeaderboardsCommandName = "leaderboard-list-all";

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
		var updateCommand = new SlashCommandBuilder();
		updateCommand.WithName(UpdateCommandName);
		updateCommand.WithDescription("Updates the bbeg leaderboard for the current event");
		var createEventCommand = new SlashCommandBuilder();
		createEventCommand.WithName(CreateEventCommandName);
		createEventCommand.WithDescription("Creates a new event (all updates will update the leaderboard for this event)");
		createEventCommand.AddOption("name", ApplicationCommandOptionType.String, "Name of the event that will be created", isRequired: true);
		var renameEventCommand = new SlashCommandBuilder();
		renameEventCommand.WithName(RenameEventCommandName);
		renameEventCommand.WithDescription("Renames an event to a new name");
		renameEventCommand.AddOption("old-event-name", ApplicationCommandOptionType.String, "Name of the event", isRequired: true);
		renameEventCommand.AddOption("new-event-name", ApplicationCommandOptionType.String, "New name of the event", isRequired: true);
		var deleteEventCommand = new SlashCommandBuilder();
		deleteEventCommand.WithName(DeleteEventCommandName);
		deleteEventCommand.WithDescription("Deletes an event with the specified name");
		deleteEventCommand.AddOption("event-name", ApplicationCommandOptionType.String, "Name of the event to be deleted", isRequired: true);
		var listLeaderboardCommand = new SlashCommandBuilder();
		listLeaderboardCommand.WithName(ListLeaderboardCommandName);
		listLeaderboardCommand.WithDescription("Prints out a leaderboard for the specified event");
		listLeaderboardCommand.AddOption("event-name", ApplicationCommandOptionType.String, "Name of the event to be printer out", isRequired: true);
		var listAllLeaderboardsCommand = new SlashCommandBuilder();
		listAllLeaderboardsCommand.WithName(ListAllLeaderboardsCommandName);
		listAllLeaderboardsCommand.WithDescription("Prints out all leaderboards");
		SlashCommandBuilder[] commands = { updateCommand, createEventCommand, listLeaderboardCommand, listAllLeaderboardsCommand, renameEventCommand, deleteEventCommand };
		try
		{
			foreach (var command in commands)
			{
				await guild.CreateApplicationCommandAsync(command.Build());
			}
		}
		catch (HttpException exception)
		{
			Console.WriteLine(exception.Message);
		}
	}

	private async Task SlashCommandHandlerAsync(SocketCommandBase command)
	{
		var options = ((SocketSlashCommand)command).Data.Options;
		var firstOption = options.FirstOrDefault();
		string firstParameter = firstOption?.Value.ToString();
		var secondOptions = options.Skip(1).FirstOrDefault();
		string secondParameter = secondOptions?.Value.ToString();

		var eventUtils = new EventUtils(_host.Services);
		var leaderboardUtils = new LeaderboardUtils(_host.Services);

		string eventName;
		try
		{
			switch (command.CommandName)
			{
				case UpdateCommandName:
					await command.RespondAsync("Leaderboard updating!");
					await new LeaderboardUtils(_host.Services).UpdateLeaderboard();
					await command.ModifyOriginalResponseAsync(a => a.Content = "Leaderboard updated!");
					break;
				case CreateEventCommandName:
					eventName = firstParameter;
					await command.RespondAsync($"Creating event \"{eventName}\"");
					_config = await eventUtils.CreateEvent(eventName);
					await command.ModifyOriginalResponseAsync(a => a.Content = $"Created event \"{eventName}\"");
					break;
				case RenameEventCommandName:
					eventName = firstParameter;
					string newEventName = secondParameter;
					await command.RespondAsync($"Renaming event \"{eventName}\" to \"{newEventName}\"");
					_config = await eventUtils.RenameEvent(eventName, newEventName);
					await command.ModifyOriginalResponseAsync(a => a.Content = $"Renamed event \"{eventName}\" to \"{newEventName}\"");
					break;
				case DeleteEventCommandName:
					eventName = firstParameter;
					await command.RespondAsync($"Deleting event \"{eventName}\"");
					await eventUtils.DeleteEvent(eventName);
					await command.ModifyOriginalResponseAsync(a => a.Content = $"Deleted event \"{eventName}\"");
					break;
				case ListLeaderboardCommandName:
					eventName = firstParameter;
					var leaderboardData = await new LeaderboardUtils(_host.Services).LoadLeaderboard(eventName);
					string message = await leaderboardData.Leaderboard.ToStringWithUsernames();
					await command.RespondAsync(message);
					break;
				case ListAllLeaderboardsCommandName:
					var leaderboards = await leaderboardUtils.LoadAllLeaderboards();
					var builder = new StringBuilder(2048);
					if (leaderboards.Count == 0) await command.RespondAsync("No events exist!");
					else
					{
						foreach (var leaderboard in leaderboards)
						{
							builder.AppendLine(await leaderboard.Leaderboard.ToStringWithUsernames());
						}

						await command.RespondAsync(builder.ToString());
					}

					break;
				default:
					throw new Exception("Command not handled!");
			}
		}
		catch (Exception ex) when (ex is EventDoesntExistException or EventAlreadyExistsException or EventNameNullException)
		{
			if (command.HasResponded) await command.ModifyOriginalResponseAsync(a => a.Content = ex.Message);
			else await command.RespondAsync(ex.Message);
		}
	}
}