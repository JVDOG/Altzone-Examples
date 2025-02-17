using Altzone.Scripts.Config;
using UnityEngine;

namespace GameUi.Scripts.UserSettings
{
    public class UserSettingsController : MonoBehaviour
    {
        [SerializeField] private UserSettingsView _view;

        private void OnEnable()
        {
            var playerData = RuntimeGameConfig.Get().PlayerDataCache;
            Debug.Log($"OnEnable {playerData}");
            _view.PlayerInfo = playerData.GetPlayerInfoLabel();
            if (playerData.ClanId > 0)
            {
                _view.ShowLeaveClanButton();
            }
            else
            {
                _view.ShowJoinClanButton();
            }
        }
    }
}