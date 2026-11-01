package com.smartfarm.iam.application;
import com.smartfarm.iam.domain.*;
import com.smartfarm.iam.infrastructure.*;
import com.smartfarm.iam.interfaces.ApiModels.*;
import com.smartfarm.iam.application.IdentityProvider.VerifiedIdentity;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.*;
import java.util.*;
@Service
public class IdentityService {
    private static final org.slf4j.Logger LOG=org.slf4j.LoggerFactory.getLogger(IdentityService.class);
    private final AccountRepository accounts;
    private final RanchUnitRepository ranches;
    private final MembershipRepository memberships;
    private final IdentityProvider identities;
    private final AuthorizationDomainService authorization;
    private final ApplicationEventPublisher events;
    public IdentityService(AccountRepository accounts,RanchUnitRepository ranches,MembershipRepository memberships,
        IdentityProvider identities,AuthorizationDomainService authorization,ApplicationEventPublisher events) {
        this.accounts=accounts;this.ranches=ranches;this.memberships=memberships;this.identities=identities;
        this.authorization=authorization;this.events=events;
    }
    @Transactional
    public RanchResponse register(VerifiedIdentity identity,RegisterRequest request) {
        Account account=resolve(identity,true);
        if(!"RanchManager".equals(account.getRole())) throw forbidden("ROLE_NOT_ALLOWED");
        if(ranches.existsByOwnerIdAndNameIgnoreCase(account.getId(),request.name().strip()))
            throw new ApiException(HttpStatus.CONFLICT,"RANCH_ALREADY_EXISTS","Ranch is already registered for this account");
        RanchUnit ranch=ranches.saveAndFlush(new RanchUnit(account.getId(),request.name(),request.region(),request.district(),
            request.estimatedHeadCount(),request.latitude(),request.longitude()));
        memberships.saveAndFlush(new RanchMembership(account.getId(),ranch.getId(),"Owner"));
        events.publishEvent(new RanchUnitRegisteredEvent(ranch.getId(),account.getId()));
        return ranchResponse(ranch,account);
    }
    @Transactional
    public TokenResponse login(LoginRequest request) {
        var identity=identities.verify(request.idToken());
        Account account=resolve(identity,false);
        return new TokenResponse(request.idToken(),"Bearer",Math.max(0,Duration.between(Instant.now(),identity.expiresAt()).getSeconds()),describe(account));
    }
    @Transactional
    public IdentityResponse me(VerifiedIdentity identity){return describe(resolve(identity,false));}
    @Transactional
    public RanchResponse getRanch(VerifiedIdentity identity,UUID ranchId) {
        Account account=resolve(identity,false);
        if(!authorization.canAccessRanch(account,ranchId,memberships.findByAccountId(account.getId()))) {
            LOG.warn("Ranch access denied: accountId={} ranchId={}",account.getId(),ranchId);
            throw forbidden("RANCH_ACCESS_DENIED");
        }
        var ranch=ranches.findById(ranchId).orElseThrow(() -> forbidden("RANCH_ACCESS_DENIED"));
        // Response accountId identifies the owner, not an operator viewing the ranch.
        return ranchResponse(ranch,accounts.findById(ranch.getOwnerId()).orElseThrow());
    }
    private Account resolve(VerifiedIdentity identity,boolean create) {
        String email=identity.email().strip().toLowerCase(Locale.ROOT);
        var byUid=accounts.findByExternalUid(identity.uid());
        Account account;
        if(byUid.isPresent()) account=byUid.get();
        else {
            var byEmail=accounts.findByEmail(email);
            if(byEmail.isPresent()) {
                account=byEmail.get();
                if(account.getExternalUid()!=null) throw new ApiException(HttpStatus.CONFLICT,"IDENTITY_CONFLICT","Email belongs to a different identity");
                if(!identity.emailVerified()) throw forbidden("VERIFIED_EMAIL_REQUIRED");
                // Legacy accounts may be linked only after Firebase proves ownership of the same email.
                account.linkVerifiedIdentity(identity.uid());
            } else {
                if(!create) throw new ApiException(HttpStatus.NOT_FOUND,"ACCOUNT_NOT_REGISTERED","Register a ranch unit first");
                account=new Account(email,identity.uid(),identity.emailVerified());
            }
        }
        if("TEMPORARILY_BLOCKED".equals(account.getStatus())) throw forbidden("ACCOUNT_BLOCKED");
        account.refreshVerification(identity.emailVerified());
        return accounts.saveAndFlush(account);
    }
    private IdentityResponse describe(Account account) {
        var list=memberships.findByAccountId(account.getId()).stream().filter(RanchMembership::isActive)
            .map(m -> new MembershipResponse(m.getRanchId(),m.getMembershipType())).toList();
        return new IdentityResponse(account.getId(),account.getEmail(),account.getRole(),account.getStatus(),list);
    }
    private RanchResponse ranchResponse(RanchUnit ranch,Account account) {
        return new RanchResponse(ranch.getId(),account.getId(),account.getEmail(),account.getStatus(),ranch.getName(),
            ranch.getRegion(),ranch.getDistrict(),ranch.getEstimatedHeadCount(),ranch.getLatitude(),ranch.getLongitude());
    }
    private ApiException forbidden(String code){return new ApiException(HttpStatus.FORBIDDEN,code,"Access is not allowed");}
}
