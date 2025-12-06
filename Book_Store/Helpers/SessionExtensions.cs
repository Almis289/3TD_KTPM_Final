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

        // LƯU DANH SÁCH PRODUCT ĐƯỢC CHỌN
        public static List<int> GetSelectedProducts(this ISession session)
        {
            return session.GetObject<List<int>>("SelectedProducts") ?? new List<int>();
        }

        public static void SaveSelectedProducts(this ISession session, List<int> ids)
        {
            session.SetObject("SelectedProducts", ids);
        }
    }
}
