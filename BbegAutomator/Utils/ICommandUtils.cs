using Discord;
using Discord.WebSocket;

namespace BbegAutomator.Utils;

public interface ICommandUtils
{
    /// <summary>
    /// Returns all slash commands for the bot
    /// </summary>
    /// <returns></returns>
    List<SlashCommandBuilder> GetSlashCommands();

    /// <summary>
    /// Executed the specified command
    /// </summary>
    /// <param name="command">Command to be executed</param>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown if unknown command is used</exception>
    Task<IConfig> ExecuteCommand(SocketCommandBase command);
}