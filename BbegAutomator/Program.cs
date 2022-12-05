﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace BbegAutomator
{
	public class Program
	{
		//TODO: crash the program if an exception occurs 
		private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

		private IHost _host;
		private static readonly DiscordSocketClient Client = new();
		private static Config _config;
		private const string UpdateCommandName = "update";
		private const string CreateEventCommandName = "event-create";
		private const string RenameCommandName = "event-rename";
		private const string ListLeaderboardCommandName = "leaderboard-list";
		private const string ListAllLeaderboardsCommandName = "leaderboard-list-all";

		private async Task MainAsync()
		{
			_config = await Config.GetConfigAsync();

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
				renameEventCommand.WithName(RenameCommandName);
				renameEventCommand.WithDescription("Renames an event to a new name");
				renameEventCommand.AddOption("old-event-name", ApplicationCommandOptionType.String, "Name of the event", isRequired: true);
				renameEventCommand.AddOption("new-event-name", ApplicationCommandOptionType.String, "New name of the event", isRequired: true);
				var listLeaderboardCommand = new SlashCommandBuilder();
				listLeaderboardCommand.WithName(ListLeaderboardCommandName);
				listLeaderboardCommand.WithDescription("Prints out a leaderboard for the specified event");
				listLeaderboardCommand.AddOption("event-name", ApplicationCommandOptionType.String, "Name of the event to be printer out", isRequired: true);
				var listAllLeaderboardsCommand = new SlashCommandBuilder();
				listAllLeaderboardsCommand.WithName(ListAllLeaderboardsCommandName);
				listAllLeaderboardsCommand.WithDescription("Prints out all leaderboards");
				SlashCommandBuilder[] commands = { updateCommand, createEventCommand, listLeaderboardCommand, listAllLeaderboardsCommand, renameEventCommand };
				try
				{
					foreach (var command in commands)
					{
						await guild.CreateApplicationCommandAsync(command.Build());
					}
				}
				catch (ApplicationCommandException exception)
				{
					Console.WriteLine(exception.Message);
				}
			}
		}

		private async Task SlashCommandHandlerAsync(SocketCommandBase command)
		{
			var options = ((SocketSlashCommand)command).Data.Options;
			var firstOption = options.FirstOrDefault();
			string firstParameter = firstOption?.Value.ToString();
			var secondOptions = options.Skip(1).FirstOrDefault();
			string secondParameter = secondOptions?.Value.ToString();

			string eventName;
			switch (command.CommandName)
			{
				case UpdateCommandName:
					await command.RespondAsync("Leaderboard updating!");
					await Leaderboard.UpdateLeaderboardAsync(_host.Services);
					await command.ModifyOriginalResponseAsync(a => a.Content = "Leaderboard updated!");
					break;
				case CreateEventCommandName:
					eventName = firstParameter;
					await command.RespondAsync($"Creating event \"{eventName}\"");
					try
					{
						_config = await EventHandler.CreateEventAsync(eventName, _host.Services);
						await command.ModifyOriginalResponseAsync(a => a.Content = $"Created event \"{eventName}\"");
					}
					catch (EventAlreadyExistsException)
					{
						await command.ModifyOriginalResponseAsync(a => a.Content = $"Event \"{eventName}\" already exists!");
					}
					break;
				case RenameCommandName:
					eventName = firstParameter;
					string newEventName = secondParameter;
					await command.RespondAsync($"Renaming event \"{eventName}\" to \"{newEventName}\"");
					try
					{
						_config = await EventHandler.RenameEventAsync(eventName, newEventName, _host.Services);
						await command.ModifyOriginalResponseAsync(a => a.Content = $"Renamed event \"{eventName}\" to \"{newEventName}\"");
					}
					catch(EventDoesntExistException e)
					{
						await command.ModifyOriginalResponseAsync(a => a.Content = e.Message);
					}
					break;
				case ListLeaderboardCommandName:
					eventName = firstParameter;
					var leaderboardData = await LeaderboardParser.LoadLeaderboardAsync(eventName, _host.Services);
					if (leaderboardData == null)
					{
						await command.RespondAsync("Leaderboard not found!");
					}
					else
					{
						string message = await leaderboardData.Leaderboard.ToStringWithUsernamesAsync();
						await command.RespondAsync(message);
					}
					break;
				case ListAllLeaderboardsCommandName:
					var leaderboards = await LeaderboardParser.LoadAllLeaderboardsAsync(_host.Services);
					var builder = new StringBuilder(2048);
					foreach (var leaderboard in leaderboards)
					{
						builder.AppendLine(await leaderboard.Leaderboard.ToStringWithUsernamesAsync());
					}
					await command.RespondAsync(builder.ToString());
					break;
				default:
					throw new Exception("Command not handled!");
			}
		}
	}
}