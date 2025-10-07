using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OnlineConfiguration
{
    public class ServerContractResolver : DefaultContractResolver
    {
        private readonly HashSet<string> _ignoreProps;

        public ServerContractResolver()
        {
            _ignoreProps = new HashSet<string>();
            FillIgnoredProperties();
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (_ignoreProps.Contains(member.Name))
            {
                property.ShouldSerialize = _ => false;
                property.ShouldDeserialize = _ => false;
                property.Ignored = true;
            }

            return property;
        }

        private void FillIgnoredProperties()
        {
            // сюда накидываем поля, которые не нужно сохранять на сервер, например:
            //_ignoreProps.Add(nameof(ProfileInfo.Credentials));
        }
    }
}