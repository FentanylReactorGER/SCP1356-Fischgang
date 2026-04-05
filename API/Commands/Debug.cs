using System;
using CommandSystem;
using Exiled.API.Features;
using SCP1356Main.API.Extensions;
using SCP1356Main.API.Schematic.Start;
using UnityEngine;

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
            plyr.Teleport(GetChamberSetuped.GetWorldData(Plugin.Singleton.GetChamberSetuped.SCP1356Chamber, new Vector3(0, 5, 0), true));
            response = "Logged hello!";
            return true;
        }
    }
}