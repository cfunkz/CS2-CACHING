using ClassicExtended.Functions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicExtended.Functions
{
    public class GameFunctions(ClassicExtendedConfig Config)
    {

        public bool ChangeTeam(CCSPlayerController? caller, CsTeam moveTo)
        {
            if (caller == null) return false;

            if (IsInSpec(caller) && moveTo == CsTeam.Spectator)
            {
                caller.PrintToChat(ReplaceMessageColors($"[{Config.ChatPrefix}{ChatColors.Default}] {ChatColors.Red}You're a spectator already."));
                return false;
            }

            if (!IsUnassigned(caller) && moveTo != CsTeam.Spectator)
            {
                caller.PrintToChat(ReplaceMessageColors($"[{Config.ChatPrefix}{ChatColors.Default}] {ChatColors.Red}You can't use this command outside spectator mode."));
                return false;
            }

            NotifyTeamChange(caller, moveTo);
            caller.ChangeTeam(moveTo);

            return true;
        }

        private void NotifyTeamChange(CCSPlayerController player, CsTeam team)
        {
            string name = player.PlayerName ?? "Unknown";
            char teamColor = ChatColors.White;
            if (team != CsTeam.Spectator) teamColor = team == CsTeam.Terrorist ? ChatColors.Red : ChatColors.Blue;
            string teamMsg = $"[{Config.ChatPrefix}{ChatColors.Default}] {ChatColors.Default}Moving {ChatColors.Green}{name} {ChatColors.Default}to {teamColor}{team}.";

            Server.PrintToChatAll(ReplaceMessageColors(teamMsg));
        }

        private bool IsUnassigned(CCSPlayerController? caller)
        {
            if (caller == null) return false;
            return caller.TeamNum != (int)CsTeam.Terrorist && caller.TeamNum != (int)CsTeam.CounterTerrorist;
        }

        private bool IsInSpec(CCSPlayerController? caller)
        {
            if (caller == null) return false;
            return caller.TeamNum == (int)CsTeam.Spectator;
        }


        public string ReplaceMessageTags(string Message, string PlayerName, string userIP, string SteamPlID)
        {
            var replacedMessage = Message
                                        .Replace("{NEXTLINE}", "\u2029")
                                        .Replace("{MAP}", NativeAPI.GetMapName())
                                        .Replace("{TIME}", DateTime.Now.ToString("HH:mm:ss"))
                                        .Replace("{DATE}", DateTime.Now.ToString("dd.MM.yyyy"))
                                        .Replace("{SERVERNAME}", ConVar.Find("hostname")!.StringValue)
                                        .Replace("{IP}", ConVar.Find("ip")!.StringValue)
                                        .Replace("{PORT}", ConVar.Find("hostport")!.GetPrimitiveValue<int>().ToString())
                                        .Replace("{MAXPLAYERS}", Server.MaxPlayers.ToString())
                                        .Replace("{PLAYERS}", Utilities.GetPlayers().Count.ToString())
                                        .Replace("{PLAYERNAME}", PlayerName.ToString())
                                        .Replace("{IPUSER}", userIP)
                                        .Replace("{STEAMID}", SteamPlID);

            replacedMessage = ReplaceMessageColors(replacedMessage);

            return replacedMessage;
        }

        // THANKS TO Klayeryt
        public string ReplaceMessageColors(string input)
        {
            string[] ColorAlphabet = { "{GREEN}", "{BLUE}", "{RED}", "{SILVER}", "{MAGENTA}", "{GOLD}", "{DEFAULT}", "{LIGHTBLUE}", "{LIGHTPURPLE}", "{LIGHTRED}", "{LIGHTYELLOW}", "{YELLOW}", "{GREY}", "{LIME}", "{OLIVE}", "{ORANGE}", "{DARKRED}", "{DARKBLUE}", "{BLUEGREY}", "{PURPLE}" };
            string[] ColorChar = { $"{ChatColors.Green}", $"{ChatColors.Blue}", $"{ChatColors.Red}", $"{ChatColors.Silver}", $"{ChatColors.Magenta}", $"{ChatColors.Gold}", $"{ChatColors.Default}", $"{ChatColors.LightBlue}", $"{ChatColors.LightPurple}", $"{ChatColors.LightRed}", $"{ChatColors.LightYellow}", $"{ChatColors.Yellow}", $"{ChatColors.Grey}", $"{ChatColors.Lime}", $"{ChatColors.Olive}", $"{ChatColors.Orange}", $"{ChatColors.DarkRed}", $"{ChatColors.DarkBlue}", $"{ChatColors.BlueGrey}", $"{ChatColors.Purple}" };

            for (int z = 0; z < ColorAlphabet.Length; z++)
                input = input.Replace(ColorAlphabet[z], ColorChar[z]);


            return input;
        }
    }
}
