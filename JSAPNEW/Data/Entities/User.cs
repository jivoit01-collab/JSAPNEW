namespace JSAPNEW.Data.Entities
{
     public class User
    {
        
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string LoginUser { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

    }
}
