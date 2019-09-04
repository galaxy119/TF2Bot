using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RconSharp;
using RconSharp.Net45;

namespace TF2Bot
{
	public class Bot
	{
		private DiscordSocketClient Client => client ?? (client = new DiscordSocketClient());

		private DiscordSocketClient client;
		private readonly Program program;
		public Methods Functions;
		public INetworkSocket Socket = new RconSocket();

		public Bot(Program program)
		{
			this.program = program;
			Functions = new Methods(program, this);
			InitBot().GetAwaiter().GetResult();
		}

		private async Task InitBot()
		{
			Client.Log += Program.Log;
			Client.MessageReceived += OnMessageReceived;
			await Client.LoginAsync(TokenType.Bot, program.Config.BotToken);
			await Client.StartAsync();
			await Functions.DoStatusUpdate(client);
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

			string[] args = context.Message.Content.Split(new[] { " " }, StringSplitOptions.None);
			args[0] = args[0].Replace(program.Config.BotPrefix, "");

			switch (args[0].ToLower())
			{
				case "ping":
					await context.Channel.SendMessageAsync("Pong!");
					return;
				case "status":
					await Functions.DoServerInfo(context);
					return;
				case "rcon":
					await Functions.RunRconCommand(context);
					return;
			}
		}
	}
}