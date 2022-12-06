using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator;

public class CommandUtils
{
	private readonly IServiceProvider _serviceProvider;
	private Config _config;
	private const string UpdateCommandName = "update";
	private const string CreateEventCommandName = "event-create";
	private const string RenameEventCommandName = "event-rename";
	private const string DeleteEventCommandName = "event-delete";
	private const string ListLeaderboardCommandName = "leaderboard-list";
	private const string ListAllLeaderboardsCommandName = "leaderboard-list-all";

	public CommandUtils(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_config = (Config)serviceProvider.GetService(typeof(Config));
	}
	
	/// <summary>
	/// Returns all slash commands for the bot
	/// </summary>
	/// <returns></returns>
	public List<SlashCommandBuilder> GetSlashCommands()
	{
		var updateCommand = new SlashCommandBuilder();
		updateCommand.WithName(UpdateCommandName);
		updateCommand.WithDescription("Updates the bbeg leaderboard for the current event");
		updateCommand.AddOption("event-name", ApplicationCommandOptionType.String, "Name of the event to be updated", isRequired: false);
		var createEventCommand = new SlashCommandBuilder();
		createEventCommand.WithName(CreateEventCommandName);
		createEventCommand.WithDescription("Creates a new event (all updates will update the leaderboard for this event)");
		createEventCommand.AddOption("event-name", ApplicationCommandOptionType.String, "Name of the event that will be created", isRequired: true);
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
		return new List<SlashCommandBuilder> { updateCommand, createEventCommand, listLeaderboardCommand, listAllLeaderboardsCommand, renameEventCommand, deleteEventCommand };
	}

	/// <summary>
	/// Executed the specified command
	/// </summary>
	/// <param name="command">Command to be executed</param>
	/// <returns></returns>
	/// <exception cref="Exception">Thrown if unknown command is used</exception>
	public async Task<Config> ExecuteCommand(SocketCommandBase command)
	{
		var eventUtils = new EventUtils(_serviceProvider);
		var leaderboardUtils = new LeaderboardUtils(_serviceProvider);
		
		//Get the first and second parameter
		var options = ((SocketSlashCommand)command).Data.Options;
		var firstOption = options.FirstOrDefault();
		string firstParameter = firstOption?.Value.ToString();
		var secondOptions = options.Skip(1).FirstOrDefault();
		string secondParameter = secondOptions?.Value.ToString();

		//Executing the command
		string eventName;
		try
		{
			switch (command.CommandName)
			{
				case UpdateCommandName:
					eventName = firstParameter;
					await command.RespondAsync("Leaderboard updating!");
					if (eventName == null) await new LeaderboardUtils(_serviceProvider).UpdateLeaderboard();
					else await new LeaderboardUtils(_serviceProvider).UpdateLeaderboard(eventName);
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
					var leaderboardData = await new LeaderboardUtils(_serviceProvider).LoadLeaderboard(eventName);
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

		return _config;
	}
}