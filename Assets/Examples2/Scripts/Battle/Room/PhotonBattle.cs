using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Diagnostics;
using UnityEngine;

namespace Examples2.Scripts.Battle.Room
{
    /// <summary>
    /// Convenience class to manage some Photon related stuff in one place.
    /// </summary>
    internal static class PhotonBattle
    {
        private const string PlayerNameKey = "PlayerData.PlayerName";
        private const string PlayerPositionKey = "pp";
        private const string PlayerMainSkillKey = "mk";

        public static string GetLocalPlayerName()
        {
            if (PhotonNetwork.InRoom)
            {
                return PhotonNetwork.NickName;
            }
            var playerName = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = $"Player{1000 * (1 + DateTime.Now.Second % 10) + DateTime.Now.Millisecond:00}";
                PlayerPrefs.SetString(PlayerNameKey, playerName);
            }
            return playerName;
        }

        public static bool IsRealPlayer(Player player)
        {
            var playerPos = player.GetCustomProperty(PlayerPositionKey, -1);
            return playerPos >= 0 && playerPos <= 3;
        }

        public static int GetPlayerPos(Player player)
        {
            return player.GetCustomProperty(PlayerPositionKey, -1);
        }

        [Conditional("UNITY_EDITOR")]
        public static void SetDebugPlayerPos(Player player, int playerPos)
        {
            player.SetCustomProperties(new Hashtable
            {
                { PlayerPositionKey, playerPos }
                //{ playerMainSkillKey, (int)Defence.Deflection }
            });
            Debug.Log($"setDebugPlayerProps {player.GetDebugLabel()}");
        }
    }
}