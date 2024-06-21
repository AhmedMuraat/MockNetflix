namespace Login.Rabbit
{
    public class UserCreatedEvent
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public DateTime AccountCreated { get; set; }
        public int? RoleId { get; set; }
    }
}
