using Examples2.Scripts.Battle.Factory;
using Examples2.Scripts.Battle.interfaces;
using Examples2.Scripts.Battle.Photon;
using Photon.Pun;
using Prg.Scripts.Common.PubSub;
using UnityEngine;

namespace Examples2.Scripts.Battle.Room
{
    /// <summary>
    /// Manages room gameplay state from start to game over.
    /// </summary>
    internal class RoomManager : MonoBehaviour
    {
        [Header("Live Data"), SerializeField] private int _requiredActorCount;
        [SerializeField] private int _currentActorCount;
        [SerializeField] private bool _isMaster;
        [SerializeField] private bool _isWaitForActors;
        [SerializeField] private bool _isWaitForCountdown;

        private IPlayerManager _playerManager;

        private void Awake()
        {
            _requiredActorCount = 1 + PhotonBattle.CountRealPlayers();
            _currentActorCount = 0;
            _isMaster = PhotonNetwork.IsMasterClient;
            _isWaitForActors = true;
            _isWaitForCountdown = false;
            Debug.Log($"Awake required {_requiredActorCount} master {_isMaster}");
            this.Subscribe<ActorReportEvent>(OnActorReportEvent);
        }

        private void OnDestroy()
        {
            this.Unsubscribe();
        }

        private void OnActorReportEvent(ActorReportEvent data)
        {
            _currentActorCount += 1;
            Debug.Log(
                $"OnActorReportEvent component {data.ComponentTypeId} required {_requiredActorCount} current {_currentActorCount} master {_isMaster}");
            if (_currentActorCount == _requiredActorCount)
            {
                _isWaitForActors = false;
                _isWaitForCountdown = true;
                _playerManager = Context.GetPlayerManager;
                _playerManager.StartCountdown();
            }
        }

        internal class ActorReportEvent
        {
            public readonly int ComponentTypeId;

            public ActorReportEvent(int componentTypeId)
            {
                ComponentTypeId = componentTypeId;
            }
        }
    }
}