namespace Userdata.Rabbit
{
    public class UserUpdateRequest
    {
        public int UserInfoId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}