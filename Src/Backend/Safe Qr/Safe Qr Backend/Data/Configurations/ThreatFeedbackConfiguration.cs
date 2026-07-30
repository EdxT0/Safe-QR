using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Data.Configurations
{
    public class ThreatFeedbackConfiguration : IEntityTypeConfiguration<ThreatFeedback>
    {
        public void Configure(EntityTypeBuilder<ThreatFeedback> builder)
        {
            builder.ToTable("ThreatFeedback");
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Payload).IsRequired();
            builder.Property(f => f.PayloadType).IsRequired();
            builder.Property(f => f.ReportedRiskLevel).HasConversion<string>().IsRequired();
            builder.Property(f => f.Comment);

            builder.HasIndex(f => f.UserId);

            // SetNull (not Cascade): feedback is valuable tuning data even after the
            // reporting account is deleted, so keep the row and just drop the attribution.
            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ComplexProperty(f => f.SystemClassification, aggr =>
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

            builder.Property(f => f.CreatedAt).IsRequired().HasDefaultValueSql("now()");

            builder.Property(f => f.RowVersion).IsRowVersion();
        }
    }
}
