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


            builder.ComplexProperty(u => u.Results, aggr =>
            {
                aggr.ToJson();

                aggr.Property(r => r.ServiceResultEnum).HasConversion<string>().IsRequired();
                aggr.ComplexCollection(r => r.ServiceScanResult, serviceScanResultList =>
                {
                    serviceScanResultList.Property(serviceScanResult => serviceScanResult.Vendor).HasConversion<string>().IsRequired();
                    serviceScanResultList.Property(serviceScanResult => serviceScanResult.ServiceResult).HasConversion<string>().IsRequired();
                    serviceScanResultList.Property(serviceScanResult => serviceScanResult.Reasons).IsRequired();

                });

            });
            builder.Property(u => u.FlaggedForWrong)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.RowVersion).IsRowVersion();


        }
    }
}
