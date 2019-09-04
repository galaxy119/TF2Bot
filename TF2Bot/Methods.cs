using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using QueryMaster;
using QueryMaster.GameServer;
using RconSharp;
using Game = Discord.Game;

namespace TF2Bot
{
	public class Methods
	{
		private readonly Program program;
		private readonly Bot bot;
		private Server server;

		public Methods(Program program, Bot bot)
		{
			this.program = program;
			this.bot = bot;
			
			if (!ushort.TryParse(program.Config.Port, out ushort result))
				throw new ArgumentException("Invalid port!");

			server = ServerQuery.GetServerInstance(EngineType.Source, program.Config.Address, result);
			server.GetControl(program.Config.RconPassword);
		}

		public async Task RunRconCommand(ICommandContext context)
		{
			try
			{
				IGuildUser user = (IGuildUser) context.Message.Author;
				if (user.RoleIds.All(r => r != 592213288729968671))
				{
					await context.Channel.SendMessageAsync("Code 4: Permission Denied.");
					return;
				}
				
				string[] args = context.Message.Content.Split(new[] { " " }, StringSplitOptions.None);

				string commandString = ConvertToString(args.Skip(1).ToArray());
				Rcon rcon = server.Rcon;
				string response = rcon.SendCommand(commandString);
				if (string.IsNullOrEmpty(response))
				{
					await context.Channel.SendMessageAsync("Command accepted, but no response from server was sent. This is *not* an error.");
					return;
				}
				if (response.Length > 2000)
				{
					IEnumerable<string> str = Split(response, 2000);
					foreach (string s in str)
						await context.Channel.SendMessageAsync(s);
					return;
				}

				await context.Channel.SendMessageAsync(response);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				await context.Channel.SendMessageAsync("<@&530252029361127439> Code 0: Command failure!");
			}
		}

		private static IEnumerable<string> Split(string str, int maxChunkSize) 
		{
			for (int i = 0; i < str.Length; i += maxChunkSize) 
				yield return str.Substring(i, Math.Min(maxChunkSize, str.Length-i));
		}

		public async Task DoStatusUpdate(DiscordSocketClient client)
		{
			Console.WriteLine("Initiating Status update.");
			while (true)
			{
				ServerInfo info = server.GetInfo();

				if (info == null)
					await client.SetStatusAsync(UserStatus.DoNotDisturb);
				else if (info.Players == 0)
				{
					await client.SetStatusAsync(UserStatus.Idle);
					await client.SetActivityAsync(new Game("" + info.Players + " / " + info.MaxPlayers));
				}
				else
				{
					await client.SetStatusAsync(UserStatus.Online);
					await client.SetActivityAsync(
						new Game("" + info.Players + " / " + info.MaxPlayers));
				}

				await Task.Delay(3000);
			}
		}

		public async Task DoServerInfo(ICommandContext context)
		{
			ServerInfo info = server.GetInfo();

			EmbedBuilder embed = new EmbedBuilder { Title = program.Config.Hostname };

			embed.AddField("Player Count", info.Players + "/" + info.MaxPlayers);
			embed.AddField("Map", info.Map);
			embed.AddField("Bot Count", info.Bots);
			embed.AddField("Address", program.Config.Address + ":" + program.Config.Port).WithAuthor(context.Client.CurrentUser)
				.WithFooter(f => f.Text = "TF2 Status bot by Joker119").WithColor(Color.Blue).WithCurrentTimestamp();

			await context.Channel.SendMessageAsync(embed:embed.Build());
		}

		private static string ConvertToString(string[] array)
		{
			StringBuilder builder = new StringBuilder();
			if (array.Length == 1)
				return array[0];
			
			foreach (string s in array)
			{
				builder.Append(s);
				builder.Append(" ");
			}

			return builder.ToString();
		}
	}
}