using System.ComponentModel.DataAnnotations;

namespace Safe_Qr_Backend.Entities
{
    public class User
    {

        public int Id { get; set; }

        public required  string Name { get; set; }

        public required string email { get; set; }

        public required string password { get; set; }

        public bool Enabled { get; set; } = true;

        public uint RowVersion { get; set; }
    }
}
