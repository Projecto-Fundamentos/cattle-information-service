package com.smartfarm.iam.infrastructure;
import com.smartfarm.iam.domain.RanchMembership;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.*;
public interface MembershipRepository extends JpaRepository<RanchMembership,UUID> {
    List<RanchMembership> findByAccountId(UUID accountId);
}
