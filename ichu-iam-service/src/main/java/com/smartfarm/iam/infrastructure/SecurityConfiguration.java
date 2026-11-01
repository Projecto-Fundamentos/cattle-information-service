package com.smartfarm.iam.infrastructure;
import com.smartfarm.iam.application.*;
import com.smartfarm.iam.domain.AuthorizationDomainService;
import org.springframework.context.annotation.*;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.oauth2.jwt.*;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;
import org.springframework.security.web.SecurityFilterChain;
import java.util.List;
@Configuration
public class SecurityConfiguration {
    @Bean AuthorizationDomainService authorizationDomainService(){return new AuthorizationDomainService();}
    @Bean JwtDecoder jwtDecoder(IdentityProvider provider) {
        return raw -> {
            try {
                var identity=provider.verify(raw);
                return Jwt.withTokenValue(raw).header("typ","JWT").subject(identity.uid())
                    .issuedAt(identity.issuedAt()).expiresAt(identity.expiresAt())
                    .claim("email",identity.email()).claim("email_verified",identity.emailVerified()).build();
            } catch(ApiException ex) {throw new BadJwtException("Firebase identity verification failed",ex);}
        };
    }
    @Bean SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        var converter=new JwtAuthenticationConverter();
        // Do not grant business roles from externally supplied custom claims: consult local memberships.
        converter.setJwtGrantedAuthoritiesConverter(jwt -> List.of());
        return http.csrf(csrf -> csrf.disable())
            .sessionManagement(s -> s.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
            .authorizeHttpRequests(auth -> auth
                .requestMatchers(HttpMethod.POST,"/api/v1/auth/login").permitAll()
                .requestMatchers("/v3/api-docs/**","/swagger-ui/**","/swagger-ui.html","/error").permitAll()
                .anyRequest().authenticated())
            .oauth2ResourceServer(oauth -> oauth.jwt(jwt -> jwt.jwtAuthenticationConverter(converter))).build();
    }
}
