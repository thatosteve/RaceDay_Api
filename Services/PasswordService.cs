namespace RaceDay.Api.Services
{
    public interface IPasswordService
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    public class PasswordService : IPasswordService
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
        public bool Verify(string password, string hash)
        {
            try { return BCrypt.Net.BCrypt.Verify(password, hash); }
            catch (Exception) { return false; }   // malformed hash (e.g. placeholder seed data) = invalid login, not a 500
        }
    }
}
