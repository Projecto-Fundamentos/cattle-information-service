package com.smartfarm.iam.infrastructure;
import com.smartfarm.iam.domain.Account;
import org.springframework.data.jpa.repository.*;
import jakarta.persistence.LockModeType;
import java.util.*;
public interface AccountRepository extends JpaRepository<Account,UUID> {
    Optional<Account> findByExternalUid(String uid);
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    Optional<Account> findByEmail(String email);
}
