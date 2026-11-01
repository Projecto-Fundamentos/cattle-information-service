package com.smartfarm.iam.interfaces;
import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.validation.constraints.*;
import java.math.BigDecimal;
import java.util.*;
public final class ApiModels {
    private ApiModels(){}
    public record RegisterRequest(
        @NotBlank @Size(max=120) String name,
        @NotBlank @Size(max=100) String region,
        @NotBlank @Size(max=100) String district,
        @NotNull @PositiveOrZero Integer estimatedHeadCount,
        @DecimalMin("-90") @DecimalMax("90") @Digits(integer=3,fraction=6) BigDecimal latitude,
        @DecimalMin("-180") @DecimalMax("180") @Digits(integer=3,fraction=6) BigDecimal longitude
    ) {
        @JsonIgnore @AssertTrue(message="latitude and longitude must be supplied together")
        public boolean isCoordinatePairValid(){return (latitude==null)==(longitude==null);}
    }
    public record LoginRequest(@NotBlank @Size(max=16384) String idToken){}
    public record RanchResponse(UUID id,UUID accountId,String email,String accountStatus,String name,
        String region,String district,int estimatedHeadCount,BigDecimal latitude,BigDecimal longitude){}
    public record MembershipResponse(UUID ranchUnitId,String membershipType){}
    public record IdentityResponse(UUID accountId,String email,String role,String accountStatus,List<MembershipResponse> memberships){}
    public record TokenResponse(String accessToken,String tokenType,long expiresIn,IdentityResponse identity){}
}
