using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace OnlineConfiguration
{
    public enum AbValue
    {
        A,
        B,
        C
    }

    public interface IAbConfig<TData> where TData : class
    {
        AbValue CurrentVariant { get; }
        TData Data { get; }

        void ApplyVariant(AbValue value);
    }

    /// <summary>
    /// Класс для хранения и отображения списка AB вариантов.
    /// </summary>
    /// <typeparam name="TData">данные, попадающие под AB-тестирование.</typeparam>
    [Serializable]
    public class AbData<TData> where TData : class
    {
        [Serializable]
        private class AbSettings
        {
            [HideInInspector] public AbValue Value;
            [FoldoutGroup("@Value")] public TData Data;
        }

        [SerializeField]
        [JsonProperty("Variants")]
        [ListDrawerSettings(DraggableItems = false, HideAddButton = true, OnTitleBarGUI = "DrawAddButton")]
        private List<AbSettings> abVariants = new();

        [JsonIgnore] public IEnumerable<(AbValue, TData)> Variants => abVariants.Select(v => (v.Value, v.Data));

        public TData GetData(AbValue value)
        {
            var index = (int)value;
            return index >= abVariants.Count ? null : abVariants[index].Data;
        }

        public void SetData(AbValue value, TData data)
        {
            var index = (int)value;
            if (index >= abVariants.Count)
                AddData(value, data);
            else
                abVariants[index].Data = data;
        }

        public bool ContainsAbValue(AbValue value) => abVariants.Exists(x => x.Value == value);

        private void AddData(AbValue value, TData data)
        {
            abVariants.Add(new AbSettings { Value = value, Data = data });
        }

        private void DrawAddButton()
        {
#if UNITY_EDITOR
            var variantsCount = abVariants.Count;
            var abValues = Enum.GetValues(typeof(AbValue));
            if (variantsCount >= abValues.Length)
                return;

            var nextValue = (AbValue)abValues.GetValue(variantsCount);
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
            {
                abVariants.Add(new AbSettings
                {
                    Value = nextValue,
                });
            }
#endif
        }
    }

    /// <summary>
    /// Класс, от которого наследуется ab-конфиг. Обращение к текущему варианту через поле Data.
    /// </summary>
    /// <typeparam name="TData">данные, попадающие под AB-тестирование.</typeparam>
    [Serializable]
    public class BaseAbConfig<TData> : SerializedScriptableObject, IAbConfig<TData> where TData : class, new()
    {
        [SerializeField, HideLabel, FoldoutGroup("AbData", Expanded = true)]
        private AbData<TData> abData = new();

        public AbValue CurrentVariant { get; private set; }
        public TData Data => abData.GetData(CurrentVariant);

        public void ApplyVariant(AbValue value)
        {
            if (!abData.ContainsAbValue(value))
            {
                UnityEngine.Debug.LogError(
                    $"[AB] Cannot apply RC settings: config {name} does not contain {value.ToString()} variant!");
                return;
            }

            CurrentVariant = value;
        }
    }

    /// <summary>
    /// Класс, от которого наследуется серверный ab-конфиг.
    /// </summary>
    /// <typeparam name="TAbData">данные, попадающие под AB-тестирование.</typeparam>
    /// <typeparam name="TServerData">данные, которые необходимо сохранять на сервере.</typeparam>
    [Serializable]
    public abstract class BaseOnlineAbConfig<TAbData, TServerData> :
        OnlineConfig<BaseOnlineAbConfig<TAbData, TServerData>.AbServerData>, IAbConfig<TAbData>
        where TAbData : class, new()
        where TServerData : class
    {
        [Serializable]
        public class AbServerData
        {
            public AbData<TServerData> AbData = new();
        }

        [SerializeField, HideLabel, FoldoutGroup("AbData", Expanded = true)]
        private AbData<TAbData> abData = new();

        public AbValue CurrentVariant { get; private set; }
        public TAbData Data => abData.GetData(CurrentVariant);

        public void ApplyVariant(AbValue value)
        {
            if (!abData.ContainsAbValue(value))
            {
                UnityEngine.Debug.LogError(
                    $"[AB] Cannot apply RC settings: config {name} does not contain {value.ToString()} variant!");
                return;
            }

            CurrentVariant = value;
            UnityEngine.Debug.Log($"[AB] Applied RC settings: config {name} set to {value.ToString()} variant!");
        }

        protected override void ApplyServerData(AbServerData data)
        {
            foreach (var (serverAbValue, serverData) in data.AbData.Variants)
            {
                var localData = abData.GetData(serverAbValue);
                ApplyServerToConfigData(serverData, localData);
            }
        }

        protected override AbServerData GetDataToSave()
        {
            var dataToSave = new AbServerData();
            foreach (var (localAbValue, localData) in abData.Variants)
                dataToSave.AbData.SetData(localAbValue, ConvertConfigToServerData(localData));
            return dataToSave;
        }

        protected abstract void ApplyServerToConfigData(TServerData serverData, TAbData updatedLocalData);
        protected abstract TServerData ConvertConfigToServerData(TAbData localData);
    }

    public static class AbValueExtentions
    {
        public static AbValue? ToAbValue(this string str)
        {
            return str switch
            {
                "A" => AbValue.A,
                "B" => AbValue.B,
                "C" => AbValue.C,
                _ => null,
            };
        }
    }
}