using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LOD_CM_CLI
{
    public static class SerializationUtils<T>
    {
        public static async Task Serialize(T obj, string path)
        {
            var json = JsonConvert.SerializeObject(obj);
            await File.WriteAllTextAsync(path, json);
        }

        public static async Task<T> Deserialize(string path)
        {
            var content = await File.ReadAllTextAsync(path);
            var obj = JsonConvert.DeserializeObject<T>(content);
            return obj; 
        }
    }
}