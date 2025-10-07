using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UniRx;
using UnityEditor;
using UnityEngine;
using Utils;

namespace OnlineConfiguration
{
    public abstract class OnlineConfig<TServerData> : SerializedScriptableObject where TServerData : class
    {
        protected abstract OnlineConfigsManager.ConfigType ConfigType { get; }
        protected abstract OnlineConfigsManager.Platform FixedPlatform { get; }

        [NonSerialized] private BoolReactiveProperty isConfigReady = new(false);
        public BoolReactiveProperty IsConfigReady => isConfigReady;

        protected abstract void ApplyServerData(TServerData data);
        protected abstract TServerData GetDataToSave();

        public OnlineConfigsManager.IConfigLoader GetLoader()
        {
            return new OnlineConfigsManager.ConfigLoader<TServerData, OnlineConfig<TServerData>>(
                new OnlineConfigsManager.ConfigLoader<TServerData, OnlineConfig<TServerData>>.Ctx
                {
                    type = ConfigType,
                    fixedPlatform = FixedPlatform,
                    OnlineConfig = this,
                    isConfigReady = isConfigReady,
                });
        }

        public void UpdateData(TServerData data)
        {
            ApplyServerData(data);
            var serverDataJson = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString($"OnlineConfig:{ConfigType}", serverDataJson);
            UnityEngine.Debug.Log($"[Configs] Saved server data for {ConfigType} config:\n{serverDataJson}");
        }

        public void UpdateDataFromPrefs()
        {
            if (!PlayerPrefs.HasKey($"OnlineConfig:{ConfigType}"))
                return;

            var serverDataJson = PlayerPrefs.GetString($"OnlineConfig:{ConfigType}");
            TServerData data = JsonConvert.DeserializeObject<TServerData>(serverDataJson);
            ApplyServerData(data);
            UnityEngine.Debug.Log($"[Configs] Loaded saved server data for {ConfigType} config:\n{serverDataJson}");
        }

#if UNITY_EDITOR
        [FoldoutGroup("Online Config")]
        [ShowInInspector]
        [PropertyOrder(100)]
        [LabelWidth(50)]
        [Tooltip("Использовать окружение \"prod\" или \"stage\"")]
        public static bool IsProd
        {
            get => OnlineConfigsManager.IsProd;
            set => OnlineConfigsManager.IsProd = value;
        }

        [FoldoutGroup("Online Config")]
        // [ShowIf("@FixedPlatform == OnlineConfigsManager.Platform.Unknown")]
        [PropertyOrder(101)]
        [LabelWidth(50)]
        public OnlineConfigsManager.Platform Platform;

        [FoldoutGroup("Online Config")]
        [Button(ButtonSizes.Large), GUIColor(.5f, 1, .5f)]
        [PropertyOrder(102)]
        [Tooltip("Отправляет данные на сервер.")]
        private void SaveToServer()
        {
            if (!CheckInternet())
                return;

            var confirm = EditorUtility.DisplayDialog("Отправить конфиг?",
                "Это действие перезапишет конфиг на сервере. Продолжить?", "Да", "Нет");
            if (!confirm)
                return;

            OnlineConfigsManager.SaveConfig(ConfigType, GetDataToSave(), GetPlatform());
        }

        [FoldoutGroup("Online Config")]
        [Button(ButtonSizes.Large), GUIColor(1f, .5f, .5f)]
        [PropertyOrder(103)]
        [Tooltip("Получает данные конфига с сервера и перезаписывает!")]
        private async void LoadFromServer()
        {
            if (!CheckInternet())
                return;

            var confirm = EditorUtility.DisplayDialog("Скачать конфиг?",
                "Это действие перезапишет локальный конфиг данными с сервера. Продолжить?", "Да", "Нет");
            if (!confirm)
                return;

            var data = await OnlineConfigsManager.LoadConfig<TServerData>(ConfigType, GetPlatform());
            if (data == null)
                return;

            ApplyServerData(data);
        }

        [FoldoutGroup("Online Config")]
        [Button(ButtonSizes.Large), GUIColor(.4f, .9f, 1f)]
        [PropertyOrder(104)]
        [Tooltip("Выводит в консоль список всех конфигов на сервере.")]
        private void ListConfigs()
        {
            if (!CheckInternet())
                return;

            OnlineConfigsManager.ListConfigurations();
        }

        [FoldoutGroup("Online Config")]
        [Button(ButtonSizes.Large), GUIColor(1f, .9f, .4f)]
        [PropertyOrder(105)]
        [Tooltip("Выводит в консоль данные этого конфига с сервера (не перезаписывает текущее состояние).")]
        private void TEST_LoadFromServer()
        {
            if (!CheckInternet())
                return;

            OnlineConfigsManager.LoadConfig<TServerData>(ConfigType, GetPlatform()).DoAsync();
        }

        [FoldoutGroup("Online Config")]
        [Button(ButtonSizes.Large), GUIColor(1f, .9f, .4f)]
        [PropertyOrder(106)]
        [Tooltip("Выводит в консоль данные этого конфига с клиента (не перезаписывает данные на сервере).")]
        private void TEST_Save()
        {
            OnlineConfigsManager.TEST_SaveConfig(ConfigType, GetDataToSave(), GetPlatform());
        }

        private OnlineConfigsManager.Platform GetPlatform()
        {
            return FixedPlatform != OnlineConfigsManager.Platform.Unknown ? FixedPlatform : Platform;
        }

        private bool CheckInternet()
        {
            var hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
            if (!hasInternet)
                EditorUtility.DisplayDialog("Ошибка!", "Нет подключения к сети!", "ОК");
            return hasInternet;
        }
#endif
    }
}