﻿using System.Collections;
using Altzone.Scripts.Config;
using Prg.Scripts.Common.Unity.Window;
using Prg.Scripts.Common.Unity.Window.ScriptableObjects;
using UnityEngine;

namespace GameUi.Scripts.UiLoader
{
    public class UiLoader : MonoBehaviour
    {
        [SerializeField] private WindowDef _windowMainMenu;
        [SerializeField] private WindowDef _windowFirstTime;
        [SerializeField] private float _demoLoadDelay;

        private bool _isServicesLoaded;

        private void OnEnable()
        {
            // Simulate that we load some services before can continue to the game main menu (or first time process).
            Debug.Log($"OnEnable _isServicesLoaded {_isServicesLoaded}");
            var playerData = RuntimeGameConfig.Get().PlayerDataCache;
            Debug.Log(playerData.ToString());
            var nextWindowToLoad = playerData.IsTosAccepted && playerData.HasPlayerName
                ? _windowMainMenu
                : _windowFirstTime;
            if (!_isServicesLoaded)
            {
                _isServicesLoaded = true;
                StartCoroutine(SpinAndWait(_demoLoadDelay, nextWindowToLoad));
                return;
            }
            StartCoroutine(LoadNextWindow(nextWindowToLoad));
        }

        private static IEnumerator SpinAndWait(float delay, WindowDef windowDef)
        {
            var wait = new WaitForSeconds(delay);
            yield return wait;
            WindowManager.Get().ShowWindow(windowDef);
        }

        private static IEnumerator LoadNextWindow(WindowDef windowDef)
        {
            yield return null;
            WindowManager.Get().ShowWindow(windowDef);
        }
    }
}