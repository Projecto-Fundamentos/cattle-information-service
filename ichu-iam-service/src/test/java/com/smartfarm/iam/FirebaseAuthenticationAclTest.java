package com.smartfarm.iam;
import com.google.firebase.auth.*;
import com.smartfarm.iam.application.ApiException;
import com.smartfarm.iam.infrastructure.FirebaseAuthenticationAcl;
import org.junit.jupiter.api.Test;
import java.time.Instant;
import java.util.Map;
import static org.mockito.Mockito.*;
import static org.assertj.core.api.Assertions.*;
class FirebaseAuthenticationAclTest {
    @Test void validatesThroughAdminSdkWithRevocationCheck() throws Exception {
        var auth=mock(FirebaseAuth.class);var token=mock(FirebaseToken.class);
        when(auth.verifyIdToken("firebase-token",true)).thenReturn(token);
        when(token.getUid()).thenReturn("uid");when(token.getEmail()).thenReturn("owner@example.com");
        when(token.isEmailVerified()).thenReturn(true);
        when(token.getClaims()).thenReturn(Map.of("iat",Instant.now().getEpochSecond(),"exp",Instant.now().plusSeconds(3600).getEpochSecond()));
        var result=new FirebaseAuthenticationAcl(auth).verify("firebase-token");
        assertThat(result.uid()).isEqualTo("uid");assertThat(result.emailVerified()).isTrue();
        verify(auth).verifyIdToken("firebase-token",true);
    }
    @Test void expiredRevokedDisabledAndInvalidAreDenied() throws Exception {
        for(var code: new AuthErrorCode[]{AuthErrorCode.EXPIRED_ID_TOKEN,AuthErrorCode.REVOKED_ID_TOKEN,AuthErrorCode.USER_DISABLED,AuthErrorCode.INVALID_ID_TOKEN}) {
            var auth=mock(FirebaseAuth.class);var error=mock(FirebaseAuthException.class);
            when(error.getAuthErrorCode()).thenReturn(code);when(auth.verifyIdToken("bad",true)).thenThrow(error);
            assertThatThrownBy(() -> new FirebaseAuthenticationAcl(auth).verify("bad"))
                .isInstanceOfSatisfying(ApiException.class,e -> assertThat(e.status().value()).isEqualTo(401));
        }
    }
    @Test void providerOutageIsNotReportedAsSuccessfulAuthentication() throws Exception {
        var auth=mock(FirebaseAuth.class);var error=mock(FirebaseAuthException.class);
        when(auth.verifyIdToken("token",true)).thenThrow(error);
        assertThatThrownBy(() -> new FirebaseAuthenticationAcl(auth).verify("token"))
            .isInstanceOfSatisfying(ApiException.class,e -> assertThat(e.status().value()).isEqualTo(503));
    }
}
