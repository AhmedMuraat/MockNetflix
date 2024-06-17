namespace Subscribe.Models
{
    public class User
    {
        public int UserInfoId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime AccountCreated { get; set; }
    }

}
