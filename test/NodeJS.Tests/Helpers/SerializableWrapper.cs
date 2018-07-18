using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Jering.JavascriptUtils.NodeJS.Tests
{
    public class SerializableWrapper<T> : IXunitSerializable
    {
        private const string VALUE_KEY = "VALUE_KEY";
        public T Value { get; private set; }

        public SerializableWrapper(T target)
        {
            Value = target;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Value = JsonConvert.DeserializeObject<T>(info.GetValue<string>(VALUE_KEY));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(VALUE_KEY, JsonConvert.SerializeObject(Value));
        }
    }
}
