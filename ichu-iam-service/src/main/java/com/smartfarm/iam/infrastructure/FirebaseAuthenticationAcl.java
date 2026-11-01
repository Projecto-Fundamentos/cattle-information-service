package com.smartfarm.iam.infrastructure;
import com.google.firebase.auth.*;
import com.smartfarm.iam.application.*;
import org.springframework.context.annotation.Profile;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import java.time.Instant;
@Component
@Profile("!test & !postgres-test")
public class FirebaseAuthenticationAcl implements IdentityProvider {
    private final FirebaseAuth firebase;
    public FirebaseAuthenticationAcl(FirebaseAuth firebase){this.firebase=firebase;}
    @Override public VerifiedIdentity verify(String token) {
        try {
            // SDK checks signature, issuer, project audience, lifetime AND revocation/disabled user.
            FirebaseToken decoded=firebase.verifyIdToken(token,true);
            if(decoded.getEmail()==null || decoded.getEmail().isBlank()) throw invalid();
            Number issued=(Number)decoded.getClaims().get("iat"), expires=(Number)decoded.getClaims().get("exp");
            if(issued==null || expires==null) throw invalid();
            return new VerifiedIdentity(decoded.getUid(),decoded.getEmail(),decoded.isEmailVerified(),
                Instant.ofEpochSecond(issued.longValue()),Instant.ofEpochSecond(expires.longValue()));
        } catch(FirebaseAuthException ex) {
            var code=ex.getAuthErrorCode();
            if(code==AuthErrorCode.INVALID_ID_TOKEN || code==AuthErrorCode.EXPIRED_ID_TOKEN
                || code==AuthErrorCode.REVOKED_ID_TOKEN || code==AuthErrorCode.USER_DISABLED || code==AuthErrorCode.USER_NOT_FOUND)
                throw invalid();
            throw new ApiException(HttpStatus.SERVICE_UNAVAILABLE,"IDENTITY_PROVIDER_UNAVAILABLE","Identity verification is temporarily unavailable");
        } catch(IllegalArgumentException ex) {throw invalid();}
    }
    private ApiException invalid(){return new ApiException(HttpStatus.UNAUTHORIZED,"INVALID_ID_TOKEN","Invalid or expired Firebase ID token");}
}
