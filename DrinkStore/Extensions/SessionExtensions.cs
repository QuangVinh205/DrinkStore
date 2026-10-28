using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DrinkStore.Extensions
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(
            this ISession session,
            string key,
            T value)
        {
            session.SetString(
                key,
                JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(
            this ISession session,
            string key)
        {
            var data = session.GetString(key);

            if (string.IsNullOrEmpty(data))
                return default;

            return JsonSerializer.Deserialize<T>(data);
        }
    }
}