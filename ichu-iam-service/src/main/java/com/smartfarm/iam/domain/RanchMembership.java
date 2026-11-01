package com.smartfarm.iam.domain;
import jakarta.persistence.*;
import java.time.Instant;
import java.util.UUID;
@Entity
@Table(name="ranch_membership",schema="iam")
public class RanchMembership {
    @Id private UUID id;
    @Column(name="user_account_id",nullable=false) private UUID accountId;
    @Column(name="ranch_id",nullable=false) private UUID ranchId;
    @Column(name="membership_type",nullable=false,length=20) private String membershipType;
    @Column(nullable=false) private boolean active;
    @Column(name="created_at",nullable=false) private Instant createdAt;
    protected RanchMembership() {}
    public RanchMembership(UUID accountId,UUID ranchId,String type) {
        this.id=UUID.randomUUID();this.accountId=accountId;this.ranchId=ranchId;
        this.membershipType=type;this.active=true;this.createdAt=Instant.now();
    }
    public UUID getRanchId(){return ranchId;}
    public String getMembershipType(){return membershipType;}
    public boolean isActive(){return active;}
    public void revoke(){active=false;}
}
