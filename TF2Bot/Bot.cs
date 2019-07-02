using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using QueryMaster;
using QueryMaster.GameServer;
using Game = Discord.Game;

namespace TF2Bot
{
	public class Bot
	{
		private DiscordSocketClient Client => client ?? (client = new DiscordSocketClient());

		private DiscordSocketClient client;
		private readonly Program program;

		public Bot(Program program)
		{
			this.program = program;
			InitBot().GetAwaiter().GetResult();
		}

		private async Task InitBot()
		{
			Client.Log += Program.Log;
			Client.MessageReceived += OnMessageReceived;
			await Client.LoginAsync(TokenType.Bot, program.Config.BotToken);
			await Client.StartAsync();
			await DoStatusUpdate(client);
			await Task.Delay(-1);
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			if (message.Content.StartsWith(program.Config.BotPrefix))
			{
				CommandContext context = new CommandContext(Client, (IUserMessage) message);
				HandleCommands(context);
			}
		}

		private async Task HandleCommands(CommandContext context)
		{
			if (!context.Message.Content.ToLower().StartsWith(program.Config.BotPrefix))
				return;

			if (context.Message.Content.ToLower().Contains("ping"))
			{
				await context.Channel.SendMessageAsync("pong!");
				return;
			}

			if (context.Message.Content.ToLower().Contains("status"))
			{
				await DoServerInfo(context);
				return;
			}
		}

		private async Task DoStatusUpdate(DiscordSocketClient client)
		{
			
			while (true)
			{
				if (!ushort.TryParse(program.Config.Port, out ushort result))
					throw new ArgumentException("Invalid port!");
				
				Server server = ServerQuery.GetServerInstance(EngineType.Source, program.Config.Address, result);
				ServerInfo info = server.GetInfo();

				if (info == null)
					await client.SetStatusAsync(UserStatus.DoNotDisturb);
				else if (info.Players == 0)
					await client.SetStatusAsync(UserStatus.Idle);
				else
				{
					await client.SetStatusAsync(UserStatus.Online);
					await client.SetActivityAsync(
						new Game("" + info.Players + " / " + info.MaxPlayers));
				}

				await Task.Delay(30000);
			}
		}

		private async Task DoServerInfo(ICommandContext context)
		{
			if (!ushort.TryParse(program.Config.Port, out ushort result))
				throw new ArgumentException("Invalid port!");
			
			Server server = ServerQuery.GetServerInstance(EngineType.Source, program.Config.Address, result);
			ServerInfo info = server.GetInfo();

			EmbedBuilder embed = new EmbedBuilder { Title = program.Config.Hostname };

			embed.AddField("Player Count", info.Players + "/" + info.MaxPlayers);
			embed.AddField("Map", info.Map);
			embed.AddField("Bot Count", info.Bots);
			embed.AddField("Address", program.Config.Address + ":" + program.Config.Port).WithAuthor(context.Client.CurrentUser)
				.WithFooter(f => f.Text = "TF2 Status bot by Joker119").WithColor(Color.Blue).WithCurrentTimestamp();

			await context.Channel.SendMessageAsync(embed:embed.Build());
		}
	}
}