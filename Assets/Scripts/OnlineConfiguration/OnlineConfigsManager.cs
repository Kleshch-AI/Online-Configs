using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Newtonsoft.Json;
using UniRx;
using UnityEditor;
using UnityEngine;
using Utils;
using Debug = Utils.Debug;

namespace OnlineConfiguration
{
    public class OnlineConfigsManager
    {
        public enum ConfigType
        {
            Example
        }
        
        public enum Platform
        {
            Unknown,
            Ios,
            Android,
            NoPlatform,
        }

        public class LoadResult<TServerData> where TServerData : class
        {
            public bool Ok;
            public TServerData Result;
        }

        public interface IConfigLoader
        {
            bool IsLoadingDone { get; }
            Task ProcessLoadConfig(bool isFromServer);
        }

        public class ConfigLoader<TServerData, TOnlineConfig> : IConfigLoader
            where TServerData : class
            where TOnlineConfig : OnlineConfig<TServerData>
        {
            public struct Ctx
            {
                public TOnlineConfig OnlineConfig;
                public ConfigType type;
                public Platform fixedPlatform;
                public BoolReactiveProperty isConfigReady;
            }

            private readonly Ctx _ctx;

            public bool IsLoadingDone { get; private set; }

            public ConfigLoader(Ctx ctx)
            {
                _ctx = ctx;
            }

            public async Task ProcessLoadConfig(bool fromServer)
            {
                IsLoadingDone = false;

                if (!fromServer)
                {
                    _ctx.OnlineConfig.UpdateDataFromPrefs();
                }
                else
                {
                    var data = await LoadConfig<TServerData>(_ctx.type,
                        _ctx.fixedPlatform == Platform.Unknown ? GetPlatformForLoading() : _ctx.fixedPlatform);
                    if (data != null)
                        _ctx.OnlineConfig.UpdateData(data);
                    else
                        Debug.LogWarning($"LoadConfig return null for type: {_ctx.type.ToString()}");
                }

                _ctx.isConfigReady.Value = true;
                IsLoadingDone = true;
            }
        }

        public struct Ctx
        {
            public ExampleOnlineConfig exampleConfig;
        }

        public static bool IsProd = false;

        private const string STORE = "/store";
        private const string LOAD = "/load";

        private const string PLATFORM_IOS = "_ios";
        private const string PLATFORM_ANDROID = "_android";

        private const string EXAMPLE_NAMESPACE = "/example";
        private const string EXAMPLE_NAME = "example-config-ab";

        private const int TIMEOUT = 20000;

        private readonly Ctx _ctx;

        private static string Url => $"URL/MOCK/FOR_{Environment}";
        private static string Environment =>
#if UNITY_EDITOR
            UrlProvider.GetEnvironment(OnlineConfigsManager.IsProd);
#else
            UrlProvider.ENVIRONMENT;
#endif

        public OnlineConfigsManager(Ctx ctx)
        {
            _ctx = ctx;

            _ = UpdateConfigs();
        }

        private async Task UpdateConfigs()
        {
#if UNITY_EDITOR
            return;
#endif

            var hasInternet = HasInternet();

            var configTypes = ConfigType.GetValues(typeof(ConfigType)).Cast<ConfigType>();
            var loaders = new List<IConfigLoader>();
            foreach (var type in configTypes)
            {
                var loader = GetLoader(type);
                if (loader == null)
                {
                    Debug.LogWarning($"UpdateConfigs, loader with type: {type.ToString()} is not found");
                    continue;
                }

                loader.ProcessLoadConfig(hasInternet).DoAsync();
                loaders.Add(loader);
            }

            while (loaders.Any(x => !x.IsLoadingDone))
                await Task.Yield();
        }

        public static async void SaveConfig<TServerData>(ConfigType type, TServerData data, Platform platform)
            where TServerData : class
        {
#if UNITY_EDITOR
            if (!HasInternet())
                return;

            if (data == null)
            {
                Debug.LogError($"[Configs] {type} config data is null!");
                return;
            }

            if (IsProd)
            {
                EditorUtility.DisplayDialog("Ошибка!", "Нельзя сохранить в prod окружение.", "ОК");
                return;
            }

            if (!CheckPlatform(platform))
            {
                EditorUtility.DisplayDialog("Ошибка!", $"Неверно указана платформа: {platform}.", "ОК");
                return;
            }

            var (configPath, configName) = GetConfigPath(type, platform);
            if (configPath == null || configName == null)
            {
                ProcessError($"Invalid path for {type} config!");
                return;
            }

            Debug.Log(
                $"[Configs] Try save {type} config. Url: {Url + STORE + configPath}, name: {configName}, data: \n{JsonConvert.SerializeObject(data)}\nUrl: {Url + STORE + configPath},\nname = {configName}\n");
            var response = new Dictionary<string, object>();
            try
            {
                // ЛОГИКА ОТПРАВКИ НА СЕРВЕР (POST)

                Debug.Log($"[Configs] <color=green>Saved {type} config!</color>");
            }
            catch
            {
                ProcessError();
            }
#endif
        }

        public static async void ListConfigurations()
        {
#if UNITY_EDITOR
            if (!HasInternet())
                return;

            var response = new Dictionary<string, object>();
            try
            {
                // ЛОГИКА ЗАПРОСА НА СЕРВЕР
                Debug.Log("[Configs] List:\n");
                DebugUtils.SplitLog(JsonConvert.SerializeObject(response));
            }
            catch
            {
                ProcessError();
            }
#endif
        }

        public static async Task<T> LoadConfig<T>(ConfigType type, Platform platform) where T : class
        {
            if (!HasInternet())
                return null;

            if (!CheckPlatform(platform))
            {
                ProcessError($"Invalid platform for {type} config: {platform}!");
                return null;
            }

            var (configPath, configName) = GetConfigPath(type, platform);
            if (configPath == null || configName == null)
            {
                ProcessError($"Invalid path for {type} config!");
                return null;
            }

            var url = Url + LOAD + configPath + "/" + configName;
            var serverData = new LoadResult<T>();
            try
            {
                // ЛОГИКА ЗАГРУЗКИ С СЕРВЕРА, ЗАПОЛНЯЕМ serverData
            }
            catch
            {
                ProcessError(message: $"Could not load {type} config.");
            }

            if (serverData.Ok)
            {
                Debug.Log(
                    $"[Configs] <color=green>Loading of {type} config successful!</color>\nUrl: {url}\nData: \n{JsonConvert.SerializeObject(serverData.Result)}");
                return serverData.Result;
            }
            else
            {
                ProcessError($"Loading of {type} config failed!\nUrl: {url}\n");
                return null;
            }
        }

        public static void TEST_SaveConfig<TServerData>(ConfigType type, TServerData data, Platform platform)
            where TServerData : class
        {
#if UNITY_EDITOR
            if (data == null)
            {
                Debug.LogError($"[Configs] {type} config data is null!");
                return;
            }

            if (IsProd)
            {
                EditorUtility.DisplayDialog("Ошибка!", "Нельзя сохранить в prod окружение.", "ОК");
                return;
            }

            if (!CheckPlatform(platform))
            {
                EditorUtility.DisplayDialog("Ошибка!", "Укажите платформу.", "ОК");
                return;
            }

            var (configPath, configName) = GetConfigPath(type, platform);
            if (configPath == null || configName == null)
            {
                ProcessError($"Invalid path for {type} config!");
                return;
            }

            Debug.Log(
                $"[Configs] TEST save {type} config. Url: {Url + STORE + configPath}, name: {configName}, data: \n{JsonConvert.SerializeObject(data)}\nUrl: {Url + STORE + configPath},\nname = {configName}\n");
#endif
        }

        private static bool CheckPlatform(Platform platform)
        {
            return platform != Platform.Unknown;
        }

        private IConfigLoader GetLoader(ConfigType type)
        {
            switch (type)
            {
                case ConfigType.Example: return _ctx.exampleConfig.GetLoader();
                default: return null;
            }
        }


        /// <returns>string path, string name</returns>
        private static (string, string) GetConfigPath(ConfigType type, Platform platform)
        {
            var configNamespace = GetNamespace(type);
            if (string.IsNullOrEmpty(configNamespace))
            {
                Debug.LogError($"[Configs] Namespace for {type} config is not found!");
                return (null, null);
            }

            var platformSuffix = GetPlatform(platform);
            if (platformSuffix == null)
            {
                Debug.LogError($"[Configs] Platform {platform} is not valid!");
                return (null, null);
            }

            var configName = GetName(type);
            if (string.IsNullOrEmpty(configName))
                Debug.LogError($"[Configs] Name for {type} config is not found!");

            return (configNamespace, configName + platformSuffix);
        }

        private static string GetNamespace(ConfigType type)
        {
            return type switch
            {
                ConfigType.Example => EXAMPLE_NAMESPACE,
                _ => null
            };
        }

        private static string GetName(ConfigType type)
        {
            return type switch
            {
                ConfigType.Example => EXAMPLE_NAME,
                _ => null
            };
        }

        private static string GetPlatform(Platform platform)
        {
            return platform switch
            {
                Platform.Android => PLATFORM_ANDROID,
                Platform.Ios => PLATFORM_IOS,
                Platform.NoPlatform => string.Empty,
                _ => null
            };
        }

        private static Platform GetPlatformForLoading()
        {
#if UNITY_ANDROID
            return Platform.Android;
#endif

#if UNITY_IOS || UNITY_IPHONE
            return Platform.Ios;
#endif

            return Platform.Unknown;
        }

        private static bool HasInternet()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        private static void ProcessError(string errorData = null, string message = "")
        {
#if UNITY_EDITOR
            Debug.LogError(errorData);

            var isNotAuthorized = errorData?.Trim() == System.Net.HttpStatusCode.Unauthorized.ToString();
            if (isNotAuthorized)
            {
                OnlineConfigAuthentication.GetInstance().Authenticate();
                return;
            }

            EditorUtility.DisplayDialog("Ошибка!", $"{errorData}", "ОК");
#else
            Debug.Log($"[Configs] {message} Error data: {errorData}");
#endif
        }

        private static async Task AwaitWithTimeout(Task task)
        {
            if (await Task.WhenAny(task, Task.Delay(TIMEOUT)) == task)
                return;

            Debug.Log($"[Configs] <color=red>Operation timeout!</color>");
        }
    }
}