using System.ComponentModel.DataAnnotations;

namespace Safe_Qr_Backend.Entities
{
    public class User
    {
        
        public int Id { get; set; }

        public required  string Name { get; set; }

        public required string Email { get; set; }

        public required string HashedPassword { get; set; }
        public required UserRoleEnum Role { get; set; }

        public bool Enabled { get; set; } = true;

        public uint RowVersion { get; set; }
    }
}
