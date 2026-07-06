using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Data.Configurations

{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {

        public void Configure(EntityTypeBuilder<User> builder) {

            builder.ToTable("User");

            builder.HasKey(u => u.Id);
        
            builder.Property( u => u.Enabled)
                .HasDefaultValue(true);
        }
    }
}
