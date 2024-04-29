using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ClassicExtended.Functions;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CounterStrikeSharp.API.Modules.Entities;

namespace ClassicExtended;

[MinimumApiVersion(160)]
public partial class ClassicExtended : BasePlugin, IPluginConfig<ClassicExtendedConfig>
{
    public ClassicExtendedConfig Config { get; set; } = new();
    internal static DataBaseService? _dataBaseService;
    private GameFunctions? _functions;

    public override string ModuleName => "Classic Extended";
    public override string ModuleDescription => "Extends classic public server features, adds database and new features.";
    public override string ModuleAuthor => "cFunkz";
    public override string ModuleVersion => "0.0.2";

    public void OnConfigParsed(ClassicExtendedConfig config)
	{
        _dataBaseService = new DataBaseService(config);
        _dataBaseService.TestAndCheckDataBaseTableAsync().GetAwaiter().GetResult();
        _functions = new GameFunctions(config);
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

    }

    [GameEventHandler]
    public HookResult OnFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        try
        {
            if (@event.Userid.IsBot)
            {
                return HookResult.Continue;
            }
            ulong userId = @event.Userid.SteamID;
            string playerName = @event.Userid.PlayerName;

            if (userId != 0)
            {
                Task.Run(async () =>
                {
                    var userData = await SelectUser(userId);
                    if (userData.SteamID == 0)
                    {
                        var newUser = new UserData
                        {
                            SteamID = userId,
                            Name = playerName,
                            LastConnected = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        await UpdateUser(newUser);
                        Logger.LogInformation($"(NEW USER)User {userData.Name} successfully added to database and cache.");
                    }
                    else if (!_userDataCache.ContainsKey(userId))
                    {
                        _userDataCache.TryAdd(userId, userData);
                        Logger.LogInformation($"User {userData.Name} successfully added to cache");
                    }
                    Logger.LogInformation($"User data loaded for player {playerName} with Steam ID {userId} [Playtime: {userData.TotalPlaytime}, K: {userData.Kills}, D: {userData.Deaths}]");
                });
            }
            else
            {
                Logger.LogWarning("Failed to process a user connection due to invalid user ID.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred during OnFullConnect: {ex.Message}");
        }

        return HookResult.Continue;
    }


    [GameEventHandler]
    public HookResult OnDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        try
        {
            if (@event.Userid.IsBot)
            {
                return HookResult.Continue;
            }
            ulong userId = @event.Userid.SteamID;
            string playerName = @event.Userid.PlayerName;

            if (userId != 0)
            {

                Task.Run(async () =>
                {
                    var userData = await SelectUser(userId);
                    Logger.LogInformation($"Loading data for {playerName} with Steam ID {userId} [Playtime: {userData.TotalPlaytime}, K: {userData.Kills}, D: {userData.Deaths}]");

                    long playtime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - userData.LastConnected;
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long playtimeDuration = currentTime - userData.LastConnected;

                    userData.TotalPlaytime += playtimeDuration;
                    userData.LastConnected = currentTime;

                    await UpdateUser(userData);
                    Logger.LogInformation($"Saving data for {playerName} with Steam ID {userId} [Playtime: {userData.TotalPlaytime}, K: {userData.Kills}, D: {userData.Deaths}] successful.");

                    _userDataCache.TryRemove(userId, out _);
                    Logger.LogInformation($"{playerName} successfully removed from cache.");
                });
            }
            else
            {
                Logger.LogWarning("Failed to process a user connection due to invalid user ID.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred during OnFullConnect: {ex.Message}");
        }

        return HookResult.Continue;
    }


    [GameEventHandler]
    public HookResult OnElimination(EventPlayerDeath @event, GameEventInfo info)
    {
        ulong player = @event.Userid.SteamID;
        ulong attacker = @event.Attacker.SteamID;
        bool isBot = @event.Attacker.IsBot;
        ulong assister = @event.Userid.SteamID;
        try
        {
            Task.Run(async () =>
            {
                if (!isBot && player != 0)
                {
                    var userData = await SelectUser(player);
                    userData.Deaths += 1;
                    _userDataCache.TryAdd(player, userData);
                }
                if (!isBot && attacker != 0)
                {
                    var attackerData = await SelectUser(attacker);
                    attackerData.Kills += 1;
                    _userDataCache.TryAdd(attacker, attackerData);
                }
                if (!isBot && assister != 0)
                {
                    var assisterData = await SelectUser(assister);
                    assisterData.Assists += 1;
                    _userDataCache.TryAdd(assister, assisterData);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error on kill/death: {ex.Message}");
        }
        return HookResult.Continue;
    }


    [ConsoleCommand("css_spec", "Become a spectator")]
    public void JoinSpecCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (_functions != null && player != null)
        {
            _functions.ChangeTeam(player, CsTeam.Spectator);
        }
    }

    [ConsoleCommand("css_t", "Become a spectator")]
    public void JoinTCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (_functions != null && player != null)
        {
            _functions.ChangeTeam(player, CsTeam.Terrorist);
        }
    }

    [ConsoleCommand("css_ct", "Become a spectator")]
    public void JoinCTCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (_functions != null && player != null)
        {
            _functions.ChangeTeam(player, CsTeam.CounterTerrorist);
        }
    }

    [ConsoleCommand("rank", "Show player's K/D ratio")]
    public void RankCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        ulong steamID = player.SteamID;

        Task.Run(async () =>
        {
            var userData = await SelectUser(steamID);
            double kdRatio = userData.Deaths == 0 ? userData.Kills : (double)userData.Kills / userData.Deaths;
            string formattedRatio = kdRatio.ToString("0.##");
            if (_functions != null)
            {
                Server.NextFrame(() =>
                {
                    player.PrintToChat(_functions.ReplaceMessageColors("Your K/D ratio is: {YELLOW}[" + formattedRatio + "%]"));
                    player.PrintToChat($"{userData.Kills} Kills");
                    player.PrintToChat($"{userData.Deaths} Deaths");
                    player.PrintToChat($"{userData.Assists} Aassists");
                    return;
                });
            }
        });
    }

    [GameEventHandler]
    public HookResult OnServerOff(EventServerShutdown @event, GameEventInfo info)
    {
        try
        {
            Logger.LogInformation("Starting to push cache to database..");

            Task.Run(async () =>
            {
                foreach (var kvp in _userDataCache)
                {
                    var userData = kvp.Value;
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long playtime = currentTime - userData.LastConnected;
                    userData.TotalPlaytime += playtime;
                    userData.LastConnected = currentTime;
                    await UpdateUser(userData);
                }

                _userDataCache.Clear();
                Logger.LogInformation("User data saved and cache cleared upon server shutdown.");
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred during server pre-shutdown: {ex.Message}");
        }

        return HookResult.Continue;
    }
}
