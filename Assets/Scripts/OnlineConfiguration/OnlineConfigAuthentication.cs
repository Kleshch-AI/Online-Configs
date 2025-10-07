#if UNITY_EDITOR

using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Debug = Utils.Debug;

namespace OnlineConfiguration
{
    /// <summary>
    /// Конфиг для авторизованного изменения конфигов.
    /// Хранит токен, необходимый для обращения к серверным конфигам из юнити.
    /// Токен индивидуальный, поэтому OnlineConfigAuthentication.asset должен быть добавлен в .gitignore.
    /// </summary>
    public class OnlineConfigAuthentication : ScriptableObject
    {
        [Title("Токен")]
        [PropertySpace(SpaceBefore = 20)]
        [MultiLineProperty(3)]
        [HideLabel]
        [SerializeField]
        [OnValueChanged("TrimWhitespace")]
        private string token;

        private static OnlineConfigAuthentication _instance;

        [MenuItem("Online Configuration/Online Config Authentication")]
        public static void GetFromAssetsMenu()
        {
            _instance ??= GetExtisting();
            _instance ??= CreateAsset();

            FocusOnAsset();
        }

        public static OnlineConfigAuthentication GetInstance()
        {
            _instance ??= GetExtisting();
            _instance ??= CreateAsset();
            return _instance;
        }

        public string Token
            => token;

        public void Authenticate()
        {
            var option = EditorUtility.DisplayDialogComplex("Вы не авторизованы!",
                "Перейти на страницу авторизации?",
                "Перейти",
                "Отмена",
                "Помощь");

            switch (option)
            {
                // ok
                case 0:
                    OpenWebAuthentication();
                    FocusOnAsset();
                    break;

                // cancel
                case 1:
                    break;

                // alt
                case 2:
                    OpenWebGuide();
                    break;

                default:
                    Debug.LogError("Unrecognized option.");
                    break;
            }
        }

        private static OnlineConfigAuthentication GetExtisting()
        {
            return (OnlineConfigAuthentication)AssetDatabase.LoadAssetAtPath(
                "Assets/OnlineConfigAuthentication.asset", typeof(OnlineConfigAuthentication));
        }

        private static OnlineConfigAuthentication CreateAsset()
        {
            var asset = ScriptableObject.CreateInstance<OnlineConfigAuthentication>();
            AssetDatabase.CreateAsset(asset, "Assets/OnlineConfigAuthentication.asset");
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void FocusOnAsset()
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = _instance;
        }

        [Title("Страница авторизации")]
        [LabelText("Открыть страницу авторизации")]
        [Button(ButtonSizes.Large)]
        private void OpenWebAuthentication()
        {
            // Application.OpenURL($"AUTH_URL");
        }

        [Title("Инструкция по авторизации")]
        [LabelText("Открыть инструкцию")]
        [Button(ButtonSizes.Large)]
        private void OpenWebGuide()
        {
            //Application.OpenURL("GUIDE_URL");
        }

        private void TrimWhitespace()
        {
            token = token.Trim();
        }
    }
}

#endif