using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Data.Configurations
{
    public class ScanHistoryConfiguration : IEntityTypeConfiguration<ScanHistory>
    {
        public void Configure(EntityTypeBuilder<ScanHistory> builder)
        {
            builder.ToTable("ScanHistory");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Payload).IsRequired();
            builder.Property(s => s.PayloadType).IsRequired();

            builder.HasIndex(s => s.UserId);

            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ComplexProperty(s => s.Results, aggr =>
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

            builder.Property(s => s.ScannedAt).IsRequired().HasDefaultValueSql("now()");

            builder.Property(s => s.RowVersion).IsRowVersion();
        }
    }
}
