using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Book_Store.Helpers
{
    public static class SessionExtensions
    {
        // Lưu object vào session
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            session.SetString(key, json);
        }

        // Lấy object từ session (an toàn với nullable)
        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return string.IsNullOrEmpty(json)
                ? default
                : JsonConvert.DeserializeObject<T>(json);
        }
    }
}
