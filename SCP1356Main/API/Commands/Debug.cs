using System;
using CommandSystem;
using Exiled.API.Features;
using SCP1356Main.API.Extensions;

namespace SCP1356Main.API.Commands
{

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class HelloCommand : ICommand
    {
        public string Command => "SigmaTest";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "Logs hello to the server.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Log.Info("hello"); // This logs to server console + logs file
            Player plyr = Player.Get(sender);
            plyr.Position.PlayAudioAt(Plugin.Singleton.Config.SoundEncounter, 20, 4);
            response = "Logged hello!";
            return true;
        }
    }
}