using Examples.Config.Scripts;
using Examples.Game.Scripts.Battle.Player;
using Examples.Game.Scripts.Battle.Scene;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Examples.Game.Scripts.Battle.Room
{
    /// <summary>
    /// Setup arena for Battle gameplay.
    /// </summary>
    /// <remarks>
    /// Wait that all players has been instantiated properly.
    /// </remarks>
    public class RoomSetup : MonoBehaviour
    {
        [Header("Settings"), SerializeField] private GameObject[] objectsToManage;

        [Header("Live Data"), SerializeField] private List<PlayerActor> playerActors;

        private void Awake()
        {
            Debug.Log($"Awake: {PhotonNetwork.NetworkClientState}");
            prepareCurrentStage();
        }

        private void OnEnable()
        {
            setupLocalPlayer();
            StartCoroutine(setupAllPlayers());
        }

        private void prepareCurrentStage()
        {
            // Disable game objects until this room stage is ready
            Array.ForEach(objectsToManage, x => x.SetActive(false));
        }

        private void continueToNextStage()
        {
            enabled = false;
            // Enable game objects when this room stage is ready to play
            Array.ForEach(objectsToManage, x => x.SetActive(true));
        }

        private void setupLocalPlayer()
        {
            var player = PhotonNetwork.LocalPlayer;
            PhotonBattle.getPlayerProperties(player, out var playerPos, out var teamIndex);
            Debug.Log($"OnEnable pos={playerPos} team={teamIndex} {player.GetDebugLabel()}");
            var sceneConfig = SceneConfig.Get();
            var features = RuntimeGameConfig.Get().features;
            if (features.isRotateGameCamera)
            {
                if (teamIndex == 1)
                {
                    // Rotate game camera for upper team
                    var _camera = sceneConfig._camera;
                    var cameraTransform = _camera.transform;
                    cameraTransform.rotation = Quaternion.Euler(0f, 0f, 180f); // Upside down
                }
            }
            if (features.isLocalPLayerOnTeamBlue)
            {
                if (teamIndex == 1)
                {
                    // c# swap via deconstruction
                    (sceneConfig.upperTeamSprite.color, sceneConfig.lowerTeamSprite.color) =
                        (sceneConfig.lowerTeamSprite.color, sceneConfig.upperTeamSprite.color);
                }
            }
        }

        private IEnumerator setupAllPlayers()
        {
            var playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            playerActors = FindObjectsOfType<PlayerActor>().ToList();
            while (playerActors.Count != playerCount && PhotonNetwork.InRoom)
            {
                if (!PhotonNetwork.InRoom)
                {
                    yield break;
                }
                Debug.Log($"setupAllPlayers playerCount={playerCount} playerActors={playerActors.Count} wait");
                yield return null;
                playerActors = FindObjectsOfType<PlayerActor>().ToList();
            }
            // Wait for players so that everybody can know (find) each other if required!
            for (;;)
            {
                if (!PhotonNetwork.InRoom)
                {
                    yield break;
                }
                var activeCount = activateActors(playerActors);
                if (activeCount == playerCount)
                {
                    break;
                }
                yield return null;
            }
            // Save current player actor list for easy access later!
            PlayerActivator.allPlayerActors.AddRange(playerActors);
            Debug.Log($"setupAllPlayers playerCount={playerCount} allPlayerActors={PlayerActivator.allPlayerActors.Count} ready");
            // Now we can activate all players safely!
            foreach (var playerActor in playerActors)
            {
                if (!PhotonNetwork.InRoom)
                {
                    yield break;
                }
                playerActor.LateAwake();
                ((IPlayerActor)playerActor).setGhostedMode();
            }
            continueToNextStage();
        }

        private static int activateActors(List<PlayerActor> playerActors)
        {
            var countReady = 0;
            foreach (var playerActor in playerActors)
            {
                var activator = playerActor.GetComponent<PlayerActivator>();
                if (activator.isAwake)
                {
                    countReady += 1;
                }
            }
            Debug.Log($"activateActors countReady={countReady}");
            return countReady;
        }
    }
}