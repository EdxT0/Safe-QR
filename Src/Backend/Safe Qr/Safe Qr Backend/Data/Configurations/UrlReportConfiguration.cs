using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Data.Configurations
{
    public class UrlReportConfiguration : IEntityTypeConfiguration<UrlReport>
    {

        public void Configure(EntityTypeBuilder<UrlReport> builder)
        {
            builder.ToTable("UrlReport");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Url).IsRequired();

            builder.HasIndex(u => u.Url).IsUnique();


            builder.ComplexCollection(u => u.Results, b =>
            {
                b.ToJson();

                b.Property(r => r.vendor).IsRequired();
                b.Property(r => r.serviceResultVerdict).HasConversion<string>().IsRequired();
                b.Property(r => r.reasons).IsRequired();
            });



            builder.Property(u => u.FlaggedForWrong)
                .IsRequired()
                .HasDefaultValue(false);
        }
    }
}
