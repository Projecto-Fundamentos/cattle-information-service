package com.smartfarm.iam.interfaces;
import com.smartfarm.iam.application.IdentityService;
import com.smartfarm.iam.application.IdentityProvider.VerifiedIdentity;
import com.smartfarm.iam.interfaces.ApiModels.*;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import jakarta.validation.Valid;
import org.springframework.http.*;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.*;
import java.util.UUID;
@RestController
@RequestMapping("/api/v1")
public class IdentityController {
    private final IdentityService service;
    public IdentityController(IdentityService service){this.service=service;}
    @PostMapping("/ranch-units")
    @Operation(summary="Register a local account, ranch and owner membership using a Firebase identity",security=@SecurityRequirement(name="bearerAuth"))
    public ResponseEntity<RanchResponse> register(@AuthenticationPrincipal Jwt jwt,@Valid @RequestBody RegisterRequest request) {
        return ResponseEntity.status(HttpStatus.CREATED).body(service.register(identity(jwt),request));
    }
    @PostMapping("/auth/login")
    @Operation(summary="Validate a Firebase ID token and return the registered account and memberships")
    public ResponseEntity<TokenResponse> login(@Valid @RequestBody LoginRequest request) {
        return ResponseEntity.ok().cacheControl(CacheControl.noStore()).body(service.login(request));
    }
    @GetMapping("/auth/me")
    @Operation(summary="Get current account and active memberships",security=@SecurityRequirement(name="bearerAuth"))
    public IdentityResponse me(@AuthenticationPrincipal Jwt jwt){return service.me(identity(jwt));}
    @GetMapping("/ranch-units/{id}")
    @Operation(summary="Read a ranch after checking current role and membership",security=@SecurityRequirement(name="bearerAuth"))
    public RanchResponse ranch(@AuthenticationPrincipal Jwt jwt,@PathVariable UUID id){return service.getRanch(identity(jwt),id);}
    private VerifiedIdentity identity(Jwt jwt) {
        return new VerifiedIdentity(jwt.getSubject(),jwt.getClaimAsString("email"),Boolean.TRUE.equals(jwt.getClaimAsBoolean("email_verified")),jwt.getIssuedAt(),jwt.getExpiresAt());
    }
}
