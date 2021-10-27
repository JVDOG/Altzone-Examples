﻿using Examples.Config.Scripts;
using Examples.Game.Scripts.Battle.interfaces;
using Examples.Game.Scripts.Battle.Room;
using Examples.Game.Scripts.Battle.Scene;
using Photon.Pun;
using System.Linq;
using UnityEngine;

namespace Examples.Game.Scripts.Battle.Player
{
    /// <summary>
    /// Player base class for common player data.
    /// </summary>
    public class PlayerActor : MonoBehaviour, IPlayerActor
    {
        private const int playModeNormal = 0;
        private const int playModeFrozen = 1;
        private const int playModeGhosted = 2;
        private const int playModeSpecial = 3;

        [Header("Settings"), SerializeField] private PlayerShield playerShield;
        [SerializeField] private GameObject playerRotation;
        [SerializeField] private GameObject realPlayer;
        [SerializeField] private GameObject frozenPlayer;
        [SerializeField] private GameObject ghostPlayer;
        [SerializeField] private GameObject localHighlight;
        [SerializeField,Tooltip("Shrink play area to restrict player movement")] private Vector2 playerDimensions;

        [Header("Live Data"), SerializeField] private PlayerActivator activator;
        [SerializeField] private bool _isValidTeam;
        [SerializeField] private PlayerActor _teamMate;
        [SerializeField] private bool _isLocalTeam;
        [SerializeField] private bool _isHomeTeam;
        [SerializeField] private int _playMode;

        int IPlayerActor.PlayerPos => activator.playerPos;
        bool IPlayerActor.IsLocal => activator.isLocal;
        int IPlayerActor.TeamMatePos => activator.teamMatePos;
        int IPlayerActor.TeamIndex => activator.teamIndex;
        int IPlayerActor.OppositeTeam => activator.oppositeTeamIndex;

        bool IPlayerActor.IsLocalTeam
        {
            get
            {
                if (!_isValidTeam) throw new UnityException("team has not been setup yet");
                return _isLocalTeam;
            }
        }

        bool IPlayerActor.IsHomeTeam
        {
            get
            {
                if (!_isValidTeam) throw new UnityException("team has not been setup yet");
                return _isHomeTeam;
            }
        }

        IPlayerActor IPlayerActor.TeamMate
        {
            get
            {
                if (!_isValidTeam) throw new UnityException("team has not been setup yet");
                return _teamMate;
            }
        }

        float IPlayerActor.CurrentSpeed => _Speed;

        private float _Speed;
        private IRestrictedPlayer restrictedPlayer;
        private PhotonView _photonView;

        private void Awake()
        {
            activator = GetComponent<PlayerActivator>();
            _isValidTeam = false;
            _photonView = PhotonView.Get(this);
            var player = _photonView.Owner;
            var model = PhotonBattle.getPlayerCharacterModel(player);
            _Speed = model.Speed;

            // Re-parent and set name
            var sceneConfig = SceneConfig.Get();
            transform.parent = sceneConfig.actorParent.transform;
            name = $"{(player.IsLocal ? "L" : "R")}{activator.playerPos}:{activator.teamIndex}:{player.NickName}";

            setupPlayer(player);
            if (sceneConfig.isCameraRotated)
            {
                // Rotate player to align with camera orientation
                playerRotation.transform.rotation = Quaternion.Euler(0f, 0f, 180f); // Upside down
            }
        }

        public void LateAwakePass1() // Called after all players have been "awaken"
        {
            Debug.Log($"LateAwakePass1 name={name} players={PlayerActivator.allPlayerActors.Count}");
            // Set our team status
            _teamMate = PlayerActivator.allPlayerActors
                .FirstOrDefault(x => x.TeamIndex == activator.teamIndex && x.PlayerPos != activator.playerPos) as PlayerActor;
            _isLocalTeam = activator.isLocal || _teamMate != null && _teamMate.activator.isLocal;
            _isHomeTeam = activator.teamIndex == PlayerActivator.homeTeamIndex;
            _isValidTeam = true;
        }

        public void LateAwakePass2()
        {
            // Enable shields as they require that all players are valid and ready.
            playerShield.enabled = true;
        }

        private void setupPlayer(Photon.Realtime.Player player)
        {
            Debug.Log($"setupPlayer {player.GetDebugLabel()}");
            // Setup input system to move player around - PlayerMovement is required on both ends for RPC!
            var playerMovement = gameObject.AddComponent<PlayerMovement>();
            restrictedPlayer = playerMovement;
            if (player.IsLocal)
            {
                setupLocalPlayer(playerMovement);
            }
            else
            {
                setupRemotePlayer();
            }
        }

        private void setupLocalPlayer(IMovablePlayer movablePlayer)
        {
            var sceneConfig = SceneConfig.Get();
            var playArea = sceneConfig.getPlayArea(activator.playerPos);
            var restrictedArea = playArea.inflate(-playerDimensions); // deflate play area!
            restrictedPlayer.setPlayArea(restrictedArea);

            var playerInput = gameObject.AddComponent<PlayerInput>();
            playerInput.Camera = sceneConfig._camera;
            playerInput.PlayerMovement = movablePlayer;
            if (!Application.isMobilePlatform)
            {
                var keyboardInput = gameObject.AddComponent<PlayerInputKeyboard>();
                keyboardInput.PlayerMovement = movablePlayer;
            }

            localHighlight.SetActive(true);
        }

        private void setupRemotePlayer()
        {
            localHighlight.SetActive(false);
        }

        private void OnEnable()
        {
            Debug.Log($"OnEnable name={name} mode={_playMode}");
        }

        private void OnDisable()
        {
            Debug.Log($"OnDisable name={name} mode={_playMode}");
        }

        void IPlayerActor.setNormalMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(setPlayerPlayModeRpc), RpcTarget.All, playModeNormal);
            }
        }

        void IPlayerActor.setFrozenMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(setPlayerPlayModeRpc), RpcTarget.All, playModeFrozen);
            }
        }

        void IPlayerActor.setGhostedMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(setPlayerPlayModeRpc), RpcTarget.All, playModeGhosted);
            }
        }

        void IPlayerActor.setSpecialMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(setPlayerPlayModeRpc), RpcTarget.All, playModeSpecial);
            }
        }

        void IPlayerActor.headCollision(IBallControl ballControl)
        {
            Debug.Log($"headCollision name={name} mode={_playMode}");
            var oppositeTeam = ((IPlayerActor)this).OppositeTeam;
            ScoreManager.addHeadScore(oppositeTeam);
            ballControl.restartBallFor(this);
        }

        private void _setNormalMode()
        {
            Debug.Log($"setNormalMode name={name} mode={_playMode}");
            realPlayer.SetActive(true);
            frozenPlayer.SetActive(false);
            ghostPlayer.SetActive(false);
            ((IPlayerShield)playerShield).showShield();
            restrictedPlayer.canMove = true;
        }

        private void _setFrozenMode()
        {
            Debug.Log($"setFrozenMode name={name} mode={_playMode}");
            realPlayer.SetActive(false);
            frozenPlayer.SetActive(true);
            ghostPlayer.SetActive(false);
            ((IPlayerShield)playerShield).showShield();
            restrictedPlayer.canMove = false;
        }

        private void _setGhostedMode()
        {
            Debug.Log($"setGhostedMode name={name} mode={_playMode}");
            realPlayer.SetActive(false);
            frozenPlayer.SetActive(false);
            ghostPlayer.SetActive(true);
            ((IPlayerShield)playerShield).ghostShield();
            restrictedPlayer.canMove = true;
        }

        private void _setSpecialMode()
        {
            // This looks and behaves like ghosted mode but could look something else?
            Debug.Log($"setSpecialMode name={name} mode={_playMode}");
            realPlayer.SetActive(false);
            frozenPlayer.SetActive(false);
            ghostPlayer.SetActive(true);
            ((IPlayerShield)playerShield).ghostShield();
            restrictedPlayer.canMove = true;
        }

        [PunRPC]
        private void setPlayerPlayModeRpc(int playMode)
        {
            if (_playMode == playModeSpecial && playMode == playModeFrozen)
            {
                // During ball start we will be set frozen but we can not allow do that
                // because ball might be "inside" us and that is impossible!
                // Special mode is cleared when ball goes to other team's side
                Debug.Log($"setPlayerPlayModeRpc name={name} mode={_playMode} <- {playMode} INTERCEPT");
                _setSpecialMode();
                return;
            }
            _playMode = playMode;
            switch (playMode)
            {
                case playModeNormal:
                    _setNormalMode();
                    return;
                case playModeFrozen:
                    _setFrozenMode();
                    return;
                case playModeGhosted:
                    _setGhostedMode();
                    return;
                case playModeSpecial:
                    _setSpecialMode();
                    return;
                default:
                    throw new UnityException($"unknown play mode: {playMode}");
            }
        }
    }
}