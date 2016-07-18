using System.IO;

namespace Augury.Base
{
    public abstract class StatelessClassSerializationBase<T> : ISerializer<T>
        where T : class, new()
    {
        public T Deserialize(Stream stream)
        {
            return new T();
        }

        public void Serialize(Stream stream, T data)
        {
        }
    }
}
