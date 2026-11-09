namespace RaceDay.Mvc.Extensions
{
    public static class UserSession
    {
        public const string UserIdKey = "UserId";
        public const string RoleKey = "Role";
        public const string NameKey = "FullName";
        public const string ApiCookieKey = "ApiCookie";

        public static bool IsLoggedIn(this ISession s) => s.GetInt32(UserIdKey) != null;
        public static string? GetRole(this ISession s) => s.GetString(RoleKey);

        public static void StartUser(this ISession s, int userId, string role, string fullName)
        {
            s.SetInt32(UserIdKey, userId);
            s.SetString(RoleKey, role);
            s.SetString(NameKey, fullName);
        }
    }
}
