package com.smartfarm.iam.application;
import java.time.Instant;
/** Anti-corruption boundary: application code does not depend on Firebase SDK classes. */
public interface IdentityProvider {
    VerifiedIdentity verify(String idToken);
    record VerifiedIdentity(String uid,String email,boolean emailVerified,Instant issuedAt,Instant expiresAt) {}
}
