namespace Login.Rabbit
{
    public class UserUpdateMessage
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
