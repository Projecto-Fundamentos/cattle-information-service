package com.smartfarm.iam.infrastructure;
import com.smartfarm.iam.domain.RanchUnit;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.*;
public interface RanchUnitRepository extends JpaRepository<RanchUnit,UUID> {
    boolean existsByOwnerIdAndNameIgnoreCase(UUID ownerId,String name);
}
