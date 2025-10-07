using System;
using System.Collections.Generic;
using System.Linq;
using OnlineConfiguration;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// Пример конфига для ивента с разными периодами активностями и наградами за очки.
    /// Часть данных идёт в AB-тесты, а часть – нет.
    /// </summary>
    [CreateAssetMenu(fileName = "Example Online Config", menuName = "Online Configuration/Example Online Config")]
    [TypeInfoBox("<color=green>Зелёным</color> цветом выделены данные, сохраняемые на сервер")]
    public class ExampleOnlineConfig : BaseOnlineAbConfig<ExampleOnlineConfig.AbData, ExampleOnlineConfig.ServerData>
    {
        public class ServerData
        {
            public int UnlockLevelIndex;
            public List<ServerPeriodData> ActivityPeriods;
        }

        public struct ServerPeriodData
        {
            public ServerWeekAndTime start;
            public ServerWeekAndTime end;
        }

        public struct ServerWeekAndTime
        {
            public int day;
            public TimeSpan ts;
        }
        
        public enum RewardType
        {
            Small,
            Big,
            Mega
        }

        [Serializable]
        public class Reward
        {
            public RewardType type;
            public int count;
        }

        [Serializable]
        public class RewardData
        {
            [TableColumnWidth(100, false)] public int points;
            [TableList] public List<Reward> rewards;
        }

        [Serializable]
        public struct IconData
        {
            public int Points;
            [PreviewField] public Sprite Sprite;
        }

        [Serializable]
        public class AbData
        {
            [SuffixLabel("on win", Overlay = true), GUIColor(.5f, 1, .5f)]
            public int unlockLevelIndex = 0;

            [TableList, GUIColor(.5f, 1, .5f)] public List<ActivityPeriod> activityPeriods = new();
            [TableList] public List<RewardData> rewards = new();
        }

        [SerializeField, TableList] private List<IconData> iconsByPoints = new();

        public int MaxPoints => Data.rewards.Max(x => x.points);

        public bool IsLocked(int lvl) => lvl < Data.unlockLevelIndex;

        public bool IsActive(DateTime dateTime)
        {
            foreach (var period in Data.activityPeriods)
                if (period.IsActive(dateTime))
                    return true;

            return false;
        }

        public ActivityPeriod GetActivePeriod(DateTime dateTime)
        {
            foreach (var period in Data.activityPeriods)
                if (period.IsActive(dateTime))
                    return period;

            return null;
        }

        public Sprite GetSpriteByPoints(int points)
        {
            foreach (var icon in iconsByPoints)
                if (icon.Points == points)
                    return icon.Sprite;

            return null;
        }

        #region server-ab-config

        protected override OnlineConfigsManager.ConfigType ConfigType
            => OnlineConfigsManager.ConfigType.Example;

        protected override OnlineConfigsManager.Platform FixedPlatform
            => OnlineConfigsManager.Platform.Unknown;

        protected override void ApplyServerToConfigData(ServerData serverData, AbData updatedLocalData)
        {
            var periods = new List<ActivityPeriod>();
            foreach (var p in serverData.ActivityPeriods)
            {
                var start = new ActivityPeriod.WeekAndTime
                {
                    DayOfWeek = (DayOfWeek)p.start.day,
                    Time = new ActivityPeriod.TimeOfDay(p.start.ts)
                };

                var end = new ActivityPeriod.WeekAndTime
                {
                    DayOfWeek = (DayOfWeek)p.end.day,
                    Time = new ActivityPeriod.TimeOfDay(p.end.ts)
                };

                periods.Add(new ActivityPeriod
                {
                    start = start,
                    end = end
                });
            }

            updatedLocalData.unlockLevelIndex = serverData.UnlockLevelIndex;
            updatedLocalData.activityPeriods = periods;
        }

        protected override ServerData ConvertConfigToServerData(AbData localData)
        {
            var periods = new List<ServerPeriodData>();
            foreach (var p in localData.activityPeriods)
            {
                var start = new ServerWeekAndTime
                {
                    day = (int)p.start.DayOfWeek,
                    ts = p.start.Time.GetTimeSpanValue()
                };

                var end = new ServerWeekAndTime
                {
                    day = (int)p.end.DayOfWeek,
                    ts = p.end.Time.GetTimeSpanValue()
                };

                periods.Add(new ServerPeriodData
                {
                    start = start,
                    end = end
                });
            }

            return new ServerData
            {
                UnlockLevelIndex = localData.unlockLevelIndex,
                ActivityPeriods = periods,
            };
        }

        #endregion
    }
}