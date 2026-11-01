package com.smartfarm.iam.domain;
import jakarta.persistence.*;
import java.time.Instant;
import java.util.UUID;
@Entity
@Table(name="accounts",schema="iam")
public class Account {
    @Id private UUID id;
    @Column(nullable=false,unique=true,length=254) private String email;
    // Retained only for migration. Never used for authentication; cleared on verified linking.
    @Column(name="password_hash",length=100) private String passwordHash;
    @Column(name="external_uid",unique=true,length=128) private String externalUid;
    @Column(nullable=false,length=20) private String role;
    @Column(nullable=false,length=32) private String status;
    @Column(name="created_at",nullable=false) private Instant createdAt;
    protected Account() {}
    public Account(String email,String externalUid,boolean verified) {
        this.id=UUID.randomUUID();this.email=email;this.externalUid=externalUid;
        this.role="RanchManager";this.status=verified?"ACTIVE":"PENDING_VERIFICATION";this.createdAt=Instant.now();
    }
    public void linkVerifiedIdentity(String uid) {
        if(externalUid!=null && !externalUid.equals(uid)) throw new IllegalStateException("Account already linked");
        externalUid=uid;passwordHash=null;refreshVerification(true);
    }
    public void refreshVerification(boolean verified) {
        if(!"TEMPORARILY_BLOCKED".equals(status)) status=verified?"ACTIVE":"PENDING_VERIFICATION";
    }
    public UUID getId(){return id;}
    public String getEmail(){return email;}
    public String getExternalUid(){return externalUid;}
    public String getRole(){return role;}
    public String getStatus(){return status;}
}
