using Examples.Config.Scripts;
using Examples.Game.Scripts.Battle.interfaces;
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
            PlayerActivator.allPlayerActors.Clear();
            PlayerActivator.homeTeamIndex = -1;
            PlayerActivator.localTeamIndex = -1;
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
            if (teamIndex != 1)
            {
                return;
            }
            var sceneConfig = SceneConfig.Get();
            var features = RuntimeGameConfig.Get().features;
            if (features.isRotateGameCamera)
            {
                // Rotate game camera for upper team
                sceneConfig.rotateGameCamera(upsideDown: true);
            }
            if (features.isLocalPLayerOnTeamBlue)
            {
                sceneConfig.rotateBackground(upsideDown: true);
                // Separate sprites for each team gameplay area - these might not be visible in final game
                if (sceneConfig.upperTeamSprite.enabled && sceneConfig.lowerTeamSprite.enabled)
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
            var wait = new WaitForSeconds(0.1f);
            // Wait for players so that everybody can know (find) each other if required!
            while (playerActors.Count != playerCount && PhotonNetwork.InRoom)
            {
                Debug.Log($"setupAllPlayers playerCount={playerCount} playerActors={playerActors.Count} wait");
                yield return wait;
                playerActors = FindObjectsOfType<PlayerActor>().ToList();
            }
            // All player have been instantiated in the scene, wait until they are in known state
            for (; PhotonNetwork.InRoom;)
            {
                if (checkPlayerActors(playerActors) == playerCount)
                {
                    break;
                }
                yield return null;
            }
            if (!PhotonNetwork.InRoom)
            {
                yield break;
            }
            // TODO: this need more work to make it better and easier to understand
            // Save current player actor list for easy access later!
            PlayerActivator.allPlayerActors.AddRange(playerActors);
            Debug.Log($"setupAllPlayers playerCount={playerCount} allPlayerActors={PlayerActivator.allPlayerActors.Count} ready");
            // Now we can activate all players safely with two passes over them!
            foreach (var playerActor in playerActors)
            {
                if (!PhotonNetwork.InRoom)
                {
                    yield break;
                }
                playerActor.LateAwakePass1();
                ((IPlayerActor)playerActor).setGhostedMode();
            }
            foreach (var playerActor in playerActors)
            {
                if (!PhotonNetwork.InRoom)
                {
                    yield break;
                }
                playerActor.LateAwakePass2();
            }
            continueToNextStage();
        }

        private static int checkPlayerActors(List<PlayerActor> playerActors)
        {
            var activeCount = 0;
            foreach (var playerActor in playerActors)
            {
                var activator = playerActor.GetComponent<PlayerActivator>();
                if (activator.isAwake)
                {
                    activeCount += 1;
                }
            }
            Debug.Log($"checkPlayerActors activeCount={activeCount}");
            return activeCount;
        }
    }
}