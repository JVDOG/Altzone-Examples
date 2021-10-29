﻿using Examples.Config.Scripts;
using Examples.Game.Scripts.Battle.Ball;
using Examples.Game.Scripts.Battle.interfaces;
using Examples.Game.Scripts.Battle.Player;
using Examples.Game.Scripts.Battle.Scene;
using Photon.Pun;
using Prg.Scripts.Common.Photon;
using System.Linq;
using UnityEngine;

namespace Examples.Game.Scripts.Battle.SlingShot
{
    /// <summary>
    ///  Puts the ball on the game using "sling shot" method between two team mates in position A and B.
    /// </summary>
    /// <remarks>
    /// Position A is start point of aiming and position B is end point of aiming.<br />
    /// Vector B-A provides direction and relative speed (increase or decrease) to the ball when it is started to the game.<br />
    /// Ball is started from point B.
    /// </remarks>
    public class BallSlingShot : MonoBehaviourPunCallbacks, IBallSlingShot
    {
        private const int msgHideSlingShot = PhotonEventDispatcher.eventCodeBase + 2;

        [Header("Settings"), SerializeField] private int teamIndex;
        [SerializeField] private SpriteRenderer spriteA;
        [SerializeField] private SpriteRenderer spriteB;
        [SerializeField] private LineRenderer line;

        [Header("Live Data"), SerializeField] private Transform followA;
        [SerializeField] private Transform followB;
        [SerializeField] private Vector3 a;
        [SerializeField] private Vector3 b;

        [Header("Debug"), SerializeField] private Vector2 deltaVector;
        [SerializeField] private float _sqrMagnitude;
        [SerializeField] private float _attackForce;

        private IBallControl ballControl;
        private float sqrMinSlingShotDistance;
        private float sqrMaxSlingShotDistance;
        private PhotonEventDispatcher photonEventDispatcher;

        private void Awake()
        {
            Debug.Log("Awake");
            var variables = RuntimeGameConfig.Get().variables;
            sqrMinSlingShotDistance = variables.minSlingShotDistance * variables.minSlingShotDistance;
            sqrMaxSlingShotDistance = variables.maxSlingShotDistance * variables.maxSlingShotDistance;
            photonEventDispatcher = PhotonEventDispatcher.Get();
            photonEventDispatcher.registerEventListener(msgHideSlingShot, data => { onHideSlingShot(); });
        }

        private void sendHideSlingShot()
        {
            photonEventDispatcher.RaiseEvent(msgHideSlingShot, null);
        }

        private void onHideSlingShot()
        {
            gameObject.SetActive(false);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Get all team players ordered by their position so we can align the sling properly from A to B
            var playerActors = FindObjectsOfType<PlayerActor>()
                .Where(x => ((IPlayerActor)x).TeamIndex == teamIndex)
                .OrderBy(x => ((IPlayerActor)x).PlayerPos)
                .ToList();
            if (playerActors.Count == 0)
            {
                Debug.Log($"OnEnable team={teamIndex} playerActors={playerActors.Count}");
                gameObject.SetActive(false); // No players for our team!
                return;
            }
            // Hide ball immediately
            ballControl = BallActor.Get();
            if (PhotonNetwork.IsMasterClient)
            {
                ballControl.hideBall();
            }
            followA = playerActors[0].transform;
            _attackForce = getAttackForce(playerActors[0]);
            if (playerActors.Count == 2)
            {
                followB = playerActors[1].transform;
                _attackForce += getAttackForce(playerActors[1]);
            }
            else
            {
                var teamMatePos = ((IPlayerActor)playerActors[0]).TeamMatePos;
                followB = SceneConfig.Get().playerStartPos[teamMatePos]; // Never moves
            }
            Debug.Log($"OnEnable team={teamIndex} playerActors={playerActors.Count} attackForce={_attackForce}");
            // LineRenderer should be configured ok in Editor - we just move both "ends" on the fly!
            line.positionCount = 2;
        }

        private static float getAttackForce(Component playerActor)
        {
            var player = PhotonView.Get(playerActor).Owner;
            var model = PhotonBattle.getPlayerCharacterModel(player);
            return model.Attack;
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            // When any player leaves, the game is over!
            if (PhotonBattle.isRealPlayer(otherPlayer))
            {
                gameObject.SetActive(false);
            }
        }

        void IBallSlingShot.startBall()
        {
            Debug.Log($"startBall team={teamIndex} sqrMagnitude={_sqrMagnitude} attackForce={_attackForce}");
            var startPosition = b;
            var direction = deltaVector.normalized;
            var multiplier = RuntimeGameConfig.Get().variables.ballMoveSpeedMultiplier;
            var speed = deltaVector.magnitude * multiplier;
            startTheBall(ballControl, startPosition, teamIndex, direction, speed);
            sendHideSlingShot();
        }

        float IBallSlingShot.sqrMagnitude => _sqrMagnitude;

        float IBallSlingShot.attackForce => _attackForce;

        private void Update()
        {
            a = followA.position;
            b = followB.position;

            spriteA.transform.position = a;
            spriteB.transform.position = b;
            line.SetPosition(0, a);
            line.SetPosition(1, b);

            deltaVector = b - a;
            _sqrMagnitude = Mathf.Clamp(deltaVector.sqrMagnitude, sqrMinSlingShotDistance, sqrMaxSlingShotDistance);
        }

        private static void startTheBall(IBallControl ballControl, Vector2 position, int teamIndex, Vector2 direction, float speed)
        {
            ballControl.teleportBall(position, teamIndex);
            ballControl.showBall();
            ballControl.moveBall(direction, speed);
        }

        public static void startTheBall()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            // Get slingshot with largest attack force and start it - LINQ First can throw an exception if list is empty.
            var ballSlingShot = FindObjectsOfType<BallSlingShot>()
                .Cast<IBallSlingShot>()
                .OrderByDescending(x => x.sqrMagnitude * x.attackForce)
                .First();

            // Set player state on the game before ball has been started!
            var teamIndex = ((BallSlingShot)ballSlingShot).teamIndex;
            foreach (var playerActor in PlayerActivator.allPlayerActors)
            {
                if (playerActor.TeamIndex == teamIndex)
                {
                    playerActor.setSpecialMode(); // If we were frozen ball gets stuck inside us :-(
                }
                else
                {
                    playerActor.setNormalMode();
                }
            }
            ballSlingShot.startBall();
        }
    }
}