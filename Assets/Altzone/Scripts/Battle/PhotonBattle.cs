﻿using System;
using System.Diagnostics;
using System.Linq;
using Altzone.Scripts.Model;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Altzone.Scripts.Battle
{
    public static class PhotonBattle
    {
        private const string PlayerPrefsPlayerNameKey = "PlayerData.PlayerName";

        public const string PlayerPositionKey = "pp";
        public const string PlayerMainSkillKey = "mk";

        public const int PlayerPositionGuest = 0;
        public const int PlayerPosition1 = 1;
        public const int PlayerPosition2 = 2;
        public const int PlayerPosition3 = 3;
        public const int PlayerPosition4 = 4;
        public const int PlayerPositionSpectator = 11;

        public const string TeamBlueKey = "tb";
        public const string TeamRedKey = "tr";

        public const int NoTeamValue = 0;
        public const int TeamBlueValue = 1;
        public const int TeamRedValue = 2;

        public const int StartPlayingEvent = 123;

        public static bool IsRealPlayer(Player player)
        {
            var playerPos = player.GetCustomProperty(PlayerPositionKey, PlayerPositionGuest);
            return playerPos >= PlayerPosition1 && playerPos <= PlayerPosition4;
        }

        public static int CountRealPlayers()
        {
            return PhotonNetwork.CurrentRoom.Players.Values.Where(IsRealPlayer).Count();
        }

        public static int GetPlayerPos(Player player)
        {
            return player.GetCustomProperty(PlayerPositionKey, PlayerPositionGuest);
        }

        public static string GetLocalPlayerName()
        {
            if (PhotonNetwork.InRoom)
            {
                return PhotonNetwork.NickName;
            }
            // TODO: this part need to be refactored to use the store system in the game when it is implemented.
            var playerName = PlayerPrefs.GetString(PlayerPrefsPlayerNameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = $"Player{1000 * (1 + DateTime.Now.Second % 10) + DateTime.Now.Millisecond:00}";
                PlayerPrefs.SetString(PlayerPrefsPlayerNameKey, playerName);
            }
            return playerName;
        }

        public static int GetPlayerIndex(int playerPos)
        {
            Assert.IsTrue(playerPos >= PlayerPosition1 && playerPos <= PlayerPosition4,
                "playerPos >= PlayerPosition1 && playerPos <= PlayerPosition4");
            return playerPos - 1;
        }

        public static int GetTeamNumber(int playerPos)
        {
            switch (playerPos)
            {
                case PlayerPosition1:
                case PlayerPosition2:
                    return TeamBlueValue;
                case PlayerPosition3:
                case PlayerPosition4:
                    return TeamRedValue;
                default:
                    return NoTeamValue;
            }
        }

        public static int GetOppositeTeamNumber(int teamNumber)
        {
            return teamNumber == TeamBlueValue ? TeamRedValue : TeamBlueValue;
        }

        public static int GetTeamIndex(int teamNumber)
        {
            Assert.IsTrue(teamNumber >= TeamBlueValue && teamNumber <= TeamRedValue,
                "teamNumber >= TeamBlueValue && teamNumber <= TeamRedValue");
            return teamNumber == TeamBlueValue ? 0 : 1;
        }

        public static int GetOppositeTeamIndex(int teamIndex)
        {
            return teamIndex == 0 ? 1 : 0;
        }

        public static int GetTeamMatePos(int playerPos)
        {
            switch (playerPos)
            {
                case PlayerPosition1:
                    return PlayerPosition2;
                case PlayerPosition2:
                    return PlayerPosition1;
                case PlayerPosition3:
                    return PlayerPosition4;
                case PlayerPosition4:
                    return PlayerPosition3;
                default:
                    throw new UnityException($"invalid player pos: {playerPos}");
            }
        }

        public static CharacterModel GetPlayerCharacterModel(Player player)
        {
            var skillId = player.GetCustomProperty(PlayerMainSkillKey, -1);
            var character = Storefront.Get().GetCharacterModel(skillId);
            return character;
        }

        [Conditional("UNITY_EDITOR")]
        public static void SetDebugPlayerProps(Player player, int playerPos, int playerMainSkill = -1)
        {
            Assert.IsTrue(playerPos >= PlayerPosition1 && playerPos <= PlayerPosition4,
                "playerPos >= PlayerPosition1 && playerPos <= PlayerPosition4");
            if (playerMainSkill < (int)Defence.Desensitisation || playerMainSkill > (int)Defence.Confluence)
            {
                // Fastest movement skill to have for testing.
                playerMainSkill = (int)Defence.Deflection;
            }
            player.SetCustomProperties(new Hashtable
            {
                { PlayerPositionKey, playerPos },
                { PlayerMainSkillKey, playerMainSkill }
            });
            Debug.LogWarning($"setDebugPlayerProps {player.GetDebugLabel()}");
        }
    }
}