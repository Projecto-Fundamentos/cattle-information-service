package com.smartfarm.iam.domain;
import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;
@Entity
@Table(name = "ranch_units", schema = "iam")
public class RanchUnit {
    @Id private UUID id;
    @Column(name = "owner_id", nullable = false) private UUID ownerId;
    @Column(nullable = false, length = 120) private String name;
    @Column(name="name_key",nullable=false,length=120) private String nameKey;
    @Column(nullable = false, length = 100) private String region;
    @Column(nullable = false, length = 100) private String district;
    @Column(name = "estimated_head_count", nullable = false) private int estimatedHeadCount;
    @Column(precision = 9, scale = 6) private BigDecimal latitude;
    @Column(precision = 9, scale = 6) private BigDecimal longitude;
    @Column(name = "created_at", nullable = false) private Instant createdAt;
    protected RanchUnit() {}
    public RanchUnit(UUID ownerId, String name, String region, String district, int headCount,
                     BigDecimal latitude, BigDecimal longitude) {
        this.id = UUID.randomUUID(); this.ownerId = ownerId; this.name = name.strip(); this.nameKey = name.strip().toLowerCase(java.util.Locale.ROOT);
        this.region = region.strip(); this.district = district.strip(); this.estimatedHeadCount = headCount;
        this.latitude = latitude; this.longitude = longitude; this.createdAt = Instant.now();
    }
    public UUID getId() { return id; }
    public UUID getOwnerId() { return ownerId; }
    public String getName() { return name; }
    public String getRegion() { return region; }
    public String getDistrict() { return district; }
    public int getEstimatedHeadCount() { return estimatedHeadCount; }
    public BigDecimal getLatitude() { return latitude; }
    public BigDecimal getLongitude() { return longitude; }
}
