package com.smartfarm.iam.domain;
import java.util.UUID;
public record RanchUnitRegisteredEvent(UUID ranchUnitId, UUID ownerId) {}
