using System;
using System.Linq;
using Altzone.Scripts.Battle;
using Examples2.Scripts.Battle.Ball;
using Examples2.Scripts.Battle.interfaces;
using Examples2.Scripts.Battle.Room;
using Photon.Pun;
using Prg.Scripts.Common.PubSub;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Examples2.Scripts.Battle.Players
{
    [RequireComponent(typeof(PhotonView))]
    internal class PlayerActor : MonoBehaviour, IPlayerActor
    {
        public const int PlayModeNormal = 0;
        public const int PlayModeFrozen = 1;
        public const int PlayModeGhosted = 2;

        [Serializable]
        internal class PlayerState
        {
            public int _currentMode;
            public Transform _transform;
            public int _playerPos;
            public int _teamNumber;
            public PlayerActor _teamMate;
        }

        [Header("Settings"), SerializeField] private Transform _uiContentRoot;
        [SerializeField] private SpriteRenderer _highlightSprite;
        [SerializeField] private SpriteRenderer _stateSprite;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private PlayerShield _playerShield;

        [Header("Live Data"), SerializeField] private PlayerState _state;
        [SerializeField] private bool _isReParentOnDestroy;
        [SerializeField] private Transform _alternateParent;
        [SerializeField] private bool _hasPlayerShield;

        [Header("Debug"), SerializeField] private TextMeshPro _playerInfo;

        private PhotonView _photonView;

        private void Awake()
        {
            _photonView = PhotonView.Get(this);
            var player = _photonView.Owner;
            _state._currentMode = PlayModeNormal;
            _state._transform = GetComponent<Transform>();
            _state._playerPos = PhotonBattle.GetPlayerPos(player);
            _state._teamNumber = PhotonBattle.GetTeamNumber(_state._playerPos);
            var prefix = $"{(player.IsLocal ? "L" : "R")}{_state._playerPos}:{_state._teamNumber}";
            name = $"{prefix}:{player.NickName}";
            _playerInfo = GetComponentInChildren<TextMeshPro>();
            _playerInfo.text = _state._playerPos.ToString("N0");
            Debug.Log($"Awake {name}");
            this.Subscribe<BallManager.ActiveTeamEvent>(OnActiveTeamEvent);
            if (_photonView.IsMine)
            {
                _highlightSprite.color = Color.yellow;
            }
            _isReParentOnDestroy = _uiContentRoot != null;
            if (_isReParentOnDestroy)
            {
                _alternateParent = PlayerInstantiate.DetachedPlayerTransform;
            }
            _hasPlayerShield = _playerShield != null;
            if (_hasPlayerShield)
            {
                _playerShield.SetShieldMode(PlayModeGhosted);
                _playerShield.SetShieldSide(_state._teamNumber);
                _playerShield.SetShieldRotation(0);
            }
        }

        private void OnEnable()
        {
            var players = FindObjectsOfType<PlayerActor>();
            Debug.Log($"OnEnable {name} IsMine {_photonView.IsMine} IsMaster {_photonView.Owner.IsMasterClient} players {players.Length}");
            _state._teamMate = players
                .FirstOrDefault(x => x._state._teamNumber == _state._teamNumber && x._state._playerPos != _state._playerPos);
            gameObject.AddComponent<LocalPlayer>();
            ((IPlayerActor)this).SetNormalMode();
        }

        private void OnDestroy()
        {
            Debug.Log($"OnDestroy {name} re-parent {_isReParentOnDestroy}");
            this.Unsubscribe();
            if (_photonView.IsMine)
            {
                return;
            }
            if (_isReParentOnDestroy)
            {
                // We must disable ourself because at least Ball assumes that we are alive and all components are present.
                _collider.enabled = false;
                _highlightSprite.enabled = false;
                _stateSprite.color = Color.grey;
                _uiContentRoot.transform.parent = _alternateParent;
            }
        }

        #region External events

        private void OnActiveTeamEvent(BallManager.ActiveTeamEvent data)
        {
            if (data.TeamIndex == _state._teamNumber)
            {
                // Ghosted -> Frozen is not allowed
                if (_state._currentMode != PlayModeNormal)
                {
                    return;
                }
                ((IPlayerActor)this).SetFrozenMode();
            }
            else
            {
                ((IPlayerActor)this).SetNormalMode();
            }
        }

        #endregion

        #region IPlayerActor

        Transform IPlayerActor.Transform => _state._transform;

        int IPlayerActor.PlayerPos => _state._playerPos;

        int IPlayerActor.TeamNumber => _state._teamNumber;

        IPlayerActor IPlayerActor.TeamMate => _state._teamMate;

        void IPlayerActor.HeadCollision()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(SetPlayerPlayModeRpc), RpcTarget.All, PlayModeGhosted);
            }
        }

        void IPlayerActor.ShieldCollision()
        {
            // NOP - until game features are implemented
        }

        void IPlayerActor.SetNormalMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(SetPlayerPlayModeRpc), RpcTarget.All, PlayModeNormal);
            }
        }

        void IPlayerActor.SetFrozenMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(SetPlayerPlayModeRpc), RpcTarget.All, PlayModeFrozen);
            }
        }

        void IPlayerActor.SetGhostedMode()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC(nameof(SetPlayerPlayModeRpc), RpcTarget.All, PlayModeGhosted);
            }
        }

        [PunRPC]
        private void SetPlayerPlayModeRpc(int playMode)
        {
            Assert.IsTrue(playMode >= PlayModeNormal && playMode <= PlayModeGhosted,
                "playMode >= PlayModeNormal && playMode <= PlayModeGhosted");
            _state._currentMode = playMode;
            if (_hasPlayerShield)
            {
                _playerShield.SetShieldMode(playMode);
            }
            switch (playMode)
            {
                case PlayModeNormal:
                    _collider.enabled = true;
                    _stateSprite.color = Color.blue;
                    return;
                case PlayModeFrozen:
                    _collider.enabled = true;
                    _stateSprite.color = Color.magenta;
                    return;
                case PlayModeGhosted:
                    _collider.enabled = false;
                    _stateSprite.color = Color.grey;
                    return;
            }
        }

        #endregion
    }
}