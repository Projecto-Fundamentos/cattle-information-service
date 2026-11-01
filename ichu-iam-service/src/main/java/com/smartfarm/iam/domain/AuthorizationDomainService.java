package com.smartfarm.iam.domain;
import java.util.Collection;
import java.util.UUID;
/** Domain policy, independent of HTTP, tokens and the identity provider. */
public class AuthorizationDomainService {
    public boolean canAccessRanch(Account account,UUID ranchId,Collection<RanchMembership> memberships) {
        if(!"ACTIVE".equals(account.getStatus())) return false;
        return memberships.stream().anyMatch(m -> m.isActive() && m.getRanchId().equals(ranchId)
            && (("RanchManager".equals(account.getRole()) && "Owner".equals(m.getMembershipType()))
             || ("FieldOperator".equals(account.getRole()) && "Operator".equals(m.getMembershipType()))));
    }
}
