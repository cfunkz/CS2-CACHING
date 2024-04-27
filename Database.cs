using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ClassicExtended
{
    public class UserData()
    {
        public ulong SteamID { get; set; }
        public string? Name { get; set; }
        public long Points { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Headshots { get; set; }
        public long TotalPlaytime { get; set; }
        public long LastConnected { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }


    public partial class ClassicExtended
    {
        // SQL Statements for the Users table
        private const string InsertNewSQL = @"
            INSERT INTO users (SteamID, Name, Points, Kills, Deaths, Assists, Headshots, TotalPlaytime, LastConnected) 
            VALUES (@SteamID, @Name, @Points, @Kills, @Deaths, @Assists, @Headshots, @TotalPlaytime, @LastConnected)
            ON DUPLICATE KEY UPDATE 
            Name = VALUES(Name), LastConnected = VALUES(LastConnected);";

        private const string GetPlayerSQL = @"
            SELECT Name, Points, Kills, Deaths, Assists, Headshots, TotalPlaytime, LastConnected FROM users WHERE SteamID = @SteamID;";

        private const string UpdateUserSQL = @"
            INSERT INTO users (SteamID, Name, Points, Kills, Deaths, Assists, Headshots, TotalPlaytime, LastConnected) 
            VALUES (@SteamID, @Name, @Points, @Kills, @Deaths, @Assists, @Headshots, @TotalPlaytime, @LastConnected)
            ON DUPLICATE KEY UPDATE 
            Name = VALUES(Name), 
            Points = VALUES(Points), 
            Kills = VALUES(Kills), 
            Deaths = VALUES(Deaths), 
            Assists = VALUES(Assists), 
            Headshots = VALUES(Headshots), 
            TotalPlaytime = VALUES(TotalPlaytime), 
            LastConnected = VALUES(LastConnected);";

        private readonly ConcurrentDictionary<ulong, UserData> _userDataCache = new ConcurrentDictionary<ulong, UserData>();

        private async Task<UserData> SelectUser(ulong steamID)
        {
            try
            {
                if (_userDataCache.TryGetValue(steamID, out UserData? cachedUserData))
                {
                    Logger.LogInformation($"Pushing data from cache for {cachedUserData.Name} {cachedUserData.SteamID} {cachedUserData.TotalPlaytime} {cachedUserData.LastConnected} successful.");
                    return cachedUserData;
                }
                else if (_dataBaseService != null)
                {
                    var result = await _dataBaseService.ExecuteSqlAsync<(string Name, long Points, int Kills, int Deaths, int Assists, int Headshots, long TotalPlaytime, long LastConnected)>(GetPlayerSQL, new { SteamID = steamID });

                    if (result != default)
                    {
                        var newUser = new UserData
                        {
                            SteamID = steamID,
                            Name = result.Name,
                            Points = result.Points,
                            Kills = result.Kills,
                            Deaths = result.Deaths,
                            Assists = result.Assists,
                            Headshots = result.Headshots,
                            TotalPlaytime = result.TotalPlaytime,
                            LastConnected = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        _userDataCache.TryAdd(steamID, newUser);
                        Logger.LogInformation($"Transfering data from Database to Cache for {newUser.Name} {newUser.SteamID} {newUser.TotalPlaytime} {newUser.LastConnected} successful.");
                        return newUser;
                    }
                    else
                    {
                        Logger.LogInformation($"No data found for user with Steam ID {steamID} in the database.");
                    }
                }
                else
                {
                    Logger.LogError("Database service is not initialized.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to retrieve user data: {ex.Message}");
            }
            return new UserData();
        }

        private async Task UpdateUser(UserData userData)
        {
            try
            {
                if (_dataBaseService != null)
                {
                    await _dataBaseService.ExecuteSqlAsync<int>(UpdateUserSQL, userData);
                    Logger.LogInformation($"User data in Database updated for {userData.Name}. ");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update user data in Database: {ex.Message}");
            }
        }
    }
}

