using Login.Models;

namespace Login.Models
{
    public class UserWithToken: User
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public UserWithToken(User user)
        {
            this.Id = user.Id;
            this.Email = user.Email;
            this.Username = user.Username;
            this.RefreshTokens = user.RefreshTokens;
            this.Role = user.Role;

        }
    }
}
