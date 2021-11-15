﻿using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace Examples2.Scripts.Connect
{
    public class ConnectInfo : MonoBehaviour
    {
        private const string NoName = "---";

        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _playerName;
        [SerializeField] private TMP_Text _localStatus;
        [SerializeField] private TMP_Text _remotePlayer1;
        [SerializeField] private TMP_Text _remotePlayer2;
        [SerializeField] private TMP_Text _remotePlayer3;
        [SerializeField] private TMP_Text _remotePlayer4;

        private readonly List<TMP_Text> _remoteTexts = new List<TMP_Text>();
        private readonly Dictionary<int, TMP_Text> _handleMap = new Dictionary<int, TMP_Text>();

        public void Reset()
        {
            _title.text = string.Empty;
            _playerName.text = NoName;
            _localStatus.text = string.Empty;
            _remotePlayer1.text = string.Empty;
            _remotePlayer2.text = string.Empty;
            _remotePlayer3.text = string.Empty;
            _remotePlayer4.text = string.Empty;
            _remoteTexts.AddRange(new[] { _remotePlayer1, _remotePlayer2, _remotePlayer3, _remotePlayer4 });
        }

        public void ShowPlayer(Player player)
        {
            Debug.Log($"ShowPlayer {player.GetDebugLabel()}");
            Reset();
            UpdatePlayer(player);
        }

        public void HidePlayer()
        {
            Reset();
        }

        public void UpdatePlayer(Player player)
        {
            string FormatTitle(int pp, int an)
            {
                return $"handle {PhotonNetwork.LocalPlayer.ActorNumber}-{pp} {an}";
            }

            var master = player.IsMasterClient ? " [M]" : "";
            var local = player.IsLocal ? " [L]" : "";
            var playerPos = player.GetCustomProperty<byte>(PhotonKeyNames.PlayerPosition, 0);
            var playerName = player.IsLocal ? RichText.Yellow(player.NickName) : player.NickName;
            _title.text = FormatTitle(playerPos, player.ActorNumber);
            _playerName.text = $"{playerName}{master}{local} #{player.ActorNumber}";

            var actors = string.Join(",", PhotonNetwork.CurrentRoom.Players.Keys.OrderBy(x => x));
            _localStatus.text = $"L={PhotonNetwork.LocalPlayer.ActorNumber} pp={playerPos} ({actors})";
        }

        public void UpdatePeers(PlayerHandshakeState state)
        {
            var remoteHandle = state._playerActorNumber;
            if (!_handleMap.TryGetValue(remoteHandle, out var handleText))
            {
                handleText = _remoteTexts[0];
                _handleMap.Add(remoteHandle, handleText);
                _remoteTexts.RemoveAt(0);
            }
            handleText.text =
                $"{state._localActorNumber}-{state._playerPos} {state._playerActorNumber} : o={state._messagesOut} i={state._messagesIn}";
            Debug.Log($"TEXT {remoteHandle}=>{handleText.text}");
        }

        public void RemoveRemoteHandle(short remoteHandle)
        {
            if (!_handleMap.TryGetValue(remoteHandle, out var handleText))
            {
                return;
            }
            _handleMap.Remove(remoteHandle);
            handleText.text = "";
            _remoteTexts.Insert(0, handleText);
        }
    }
}