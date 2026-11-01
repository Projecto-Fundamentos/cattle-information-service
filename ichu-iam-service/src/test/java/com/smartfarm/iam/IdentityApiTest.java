package com.smartfarm.iam;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartfarm.iam.application.*;
import com.smartfarm.iam.application.IdentityProvider.VerifiedIdentity;
import com.smartfarm.iam.infrastructure.*;
import org.junit.jupiter.api.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.*;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.test.context.TestPropertySource;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;
import java.time.Instant;
import java.util.*;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;
@SpringBootTest
@AutoConfigureMockMvc
@TestPropertySource(properties="spring.profiles.default=test")
class IdentityApiTest {
    @Autowired MockMvc mvc;
    @Autowired ObjectMapper json;
    @Autowired AccountRepository accounts;
    @Autowired RanchUnitRepository ranches;
    @Autowired MembershipRepository memberships;
    @Autowired JdbcTemplate jdbc;
    @MockitoBean IdentityProvider identities;
    @BeforeEach void clean(){
        memberships.deleteAll();ranches.deleteAll();accounts.deleteAll();reset(identities);
        when(identities.verify("alice-token")).thenReturn(identity("alice","alice@example.com",true));
        when(identities.verify("bob-token")).thenReturn(identity("bob","bob@example.com",true));
        when(identities.verify("pending-token")).thenReturn(identity("pending","pending@example.com",false));
        when(identities.verify("invalid")).thenThrow(new ApiException(HttpStatus.UNAUTHORIZED,"INVALID_ID_TOKEN","Invalid or expired Firebase ID token"));
    }
    VerifiedIdentity identity(String uid,String email,boolean verified){
        return new VerifiedIdentity(uid,email,verified,Instant.now().minusSeconds(10),Instant.now().plusSeconds(3500));
    }
    Map<String,Object> registration(){return new HashMap<>(Map.of("name","Fundo San Jose","region","Apurimac",
        "district","Andahuaylas","estimatedHeadCount",120,"latitude",-13.6556,"longitude",-73.3878));}
    String register(String token,Map<String,Object> body,int expected) throws Exception {
        return mvc.perform(post("/api/v1/ranch-units").header("Authorization","Bearer "+token)
            .contentType(MediaType.APPLICATION_JSON).content(json.writeValueAsBytes(body)))
            .andExpect(status().is(expected)).andReturn().getResponse().getContentAsString();
    }
    String ranchId(String token) throws Exception{return json.readTree(register(token,registration(),201)).get("id").asText();}
    @Test void registrationCreatesFirebaseLinkedAccountRanchAndMembership() throws Exception {
        var result=json.readTree(register("alice-token",registration(),201));
        assertThat(accounts.findByExternalUid("alice")).isPresent();
        assertThat(result.get("accountStatus").asText()).isEqualTo("ACTIVE");
        assertThat(memberships.count()).isEqualTo(1);
        assertThat(jdbc.queryForObject("select count(*) from iam.accounts where password_hash is not null",Integer.class)).isZero();
    }
    @Test void registrationRequiresFirebaseAuthentication() throws Exception {
        mvc.perform(post("/api/v1/ranch-units").contentType(MediaType.APPLICATION_JSON).content(json.writeValueAsBytes(registration())))
            .andExpect(status().isUnauthorized());
        assertThat(accounts.count()).isZero();
    }
    @Test void duplicateRanchConflictsButSecondRanchForSameAccountWorks() throws Exception {
        ranchId("alice-token");
        assertThat(register("alice-token",registration(),409)).contains("RANCH_ALREADY_EXISTS");
        var body=registration();body.put("name","Second ranch");register("alice-token",body,201);
        assertThat(accounts.count()).isEqualTo(1);assertThat(memberships.count()).isEqualTo(2);
    }
    @Test void loginReturnsSameFirebaseTokenAndMemberships() throws Exception {
        ranchId("alice-token");
        mvc.perform(post("/api/v1/auth/login").contentType(MediaType.APPLICATION_JSON).content(json.writeValueAsBytes(java.util.Map.of("idToken","alice-token"))))
            .andExpect(status().isOk()).andExpect(header().string("Cache-Control","no-store"))
            .andExpect(jsonPath("accessToken").value("alice-token"))
            .andExpect(jsonPath("identity.role").value("RanchManager"))
            .andExpect(jsonPath("identity.memberships[0].membershipType").value("Owner"));
    }
    @Test void unregisteredIdentityCannotLogin() throws Exception {
        mvc.perform(post("/api/v1/auth/login").contentType(MediaType.APPLICATION_JSON).content(json.writeValueAsBytes(java.util.Map.of("idToken","alice-token"))))
            .andExpect(status().isNotFound()).andExpect(jsonPath("code").value("ACCOUNT_NOT_REGISTERED"));
    }
    @Test void ownerCanReadOwnRanchButCannotReadAnotherRanch() throws Exception {
        String own=ranchId("alice-token"),other=ranchId("bob-token");
        mvc.perform(get("/api/v1/ranch-units/"+own).header("Authorization","Bearer alice-token")).andExpect(status().isOk());
        mvc.perform(get("/api/v1/ranch-units/"+other).header("Authorization","Bearer alice-token"))
            .andExpect(status().isForbidden()).andExpect(jsonPath("code").value("RANCH_ACCESS_DENIED"));
    }
    @Test void revocationTakesEffectWithoutWaitingForTokenExpiration() throws Exception {
        String id=ranchId("alice-token");var membership=memberships.findAll().getFirst();membership.revoke();memberships.saveAndFlush(membership);
        mvc.perform(get("/api/v1/ranch-units/"+id).header("Authorization","Bearer alice-token")).andExpect(status().isForbidden());
    }
    @Test void revokingOneRanchPreservesAccessToOtherRanchWithSameToken() throws Exception {
        String first = ranchId("alice-token");
        var secondBody = registration();
        secondBody.put("name", "Second ranch");
        String second = json.readTree(register("alice-token", secondBody, 201)).get("id").asText();
        var firstMembership = memberships.findAll().stream()
            .filter(membership -> membership.getRanchId().toString().equals(first))
            .findFirst().orElseThrow();
        firstMembership.revoke();
        memberships.saveAndFlush(firstMembership);
        mvc.perform(get("/api/v1/ranch-units/" + first).header("Authorization", "Bearer alice-token"))
            .andExpect(status().isForbidden());
        mvc.perform(get("/api/v1/ranch-units/" + second).header("Authorization", "Bearer alice-token"))
            .andExpect(status().isOk());
    }
    @Test void wrongRoleAndBlockedAccountAreDenied() throws Exception {
        String id=ranchId("alice-token");
        jdbc.update("update iam.accounts set role='Veterinarian' where external_uid='alice'");
        mvc.perform(get("/api/v1/ranch-units/"+id).header("Authorization","Bearer alice-token")).andExpect(status().isForbidden());
        register("alice-token",registration(),403);
        jdbc.update("update iam.accounts set status='TEMPORARILY_BLOCKED' where external_uid='alice'");
        mvc.perform(get("/api/v1/auth/me").header("Authorization","Bearer alice-token")).andExpect(status().isForbidden());
    }
    @Test void pendingEmailMustBeVerifiedBeforeRanchAccess() throws Exception {
        String id=ranchId("pending-token");
        mvc.perform(get("/api/v1/ranch-units/"+id).header("Authorization","Bearer pending-token")).andExpect(status().isForbidden());
        when(identities.verify("pending-token")).thenReturn(identity("pending","pending@example.com",true));
        mvc.perform(get("/api/v1/ranch-units/"+id).header("Authorization","Bearer pending-token")).andExpect(status().isOk());
    }
    @Test void invalidTokenRejectedByLoginAndProtectedEndpoint() throws Exception {
        mvc.perform(post("/api/v1/auth/login").contentType(MediaType.APPLICATION_JSON).content(json.writeValueAsBytes(java.util.Map.of("idToken","invalid"))))
            .andExpect(status().isUnauthorized());
        mvc.perform(get("/api/v1/auth/me").header("Authorization","Bearer invalid")).andExpect(status().isUnauthorized());
    }
    @Test void invalidPayloadDoesNotPersist() throws Exception {
        var body=registration();body.put("estimatedHeadCount",-1);register("alice-token",body,400);
        body.put("estimatedHeadCount",1.5);register("alice-token",body,400);
        body=registration();body.remove("longitude");register("alice-token",body,400);
        assertThat(accounts.count()).isZero();assertThat(memberships.count()).isZero();
    }
    @Test void oldPasswordContractIsRejected() throws Exception {
        var body=registration();body.put("password","old-password");register("alice-token",body,400);
        mvc.perform(post("/api/v1/auth/login").contentType(MediaType.APPLICATION_JSON)
            .content(json.writeValueAsBytes(java.util.Map.of("email","alice@example.com","password","password"))))
            .andExpect(status().isBadRequest());
    }
    @Test void legacyAccountNeedsVerifiedEmailToLink() throws Exception {
        UUID id=UUID.randomUUID();
        jdbc.update("insert into iam.accounts(id,email,password_hash,status,role,created_at) values (?,?,?,'PENDING_VERIFICATION','RanchManager',CURRENT_TIMESTAMP)",id,"pending@example.com","legacy-hash");
        register("pending-token",registration(),403);
        when(identities.verify("pending-token")).thenReturn(identity("pending","pending@example.com",true));
        register("pending-token",registration(),201);
        assertThat(accounts.findByExternalUid("pending").orElseThrow().getId()).isEqualTo(id);
        assertThat(jdbc.queryForObject("select count(*) from iam.accounts where password_hash is not null",Integer.class)).isZero();
    }
    @Test void differentUidCannotTakeOverLinkedEmail() throws Exception {
        ranchId("alice-token");
        when(identities.verify("bob-token")).thenReturn(identity("bob","alice@example.com",true));
        assertThat(register("bob-token",registration(),409)).contains("IDENTITY_CONFLICT");
    }
    @Test void fieldOperatorNeedsAnExplicitOperatorMembership() throws Exception {
        String aliceRanch=ranchId("alice-token");ranchId("bob-token");
        UUID bobId=accounts.findByExternalUid("bob").orElseThrow().getId();
        jdbc.update("update iam.accounts set role='FieldOperator' where id=?",bobId);
        mvc.perform(get("/api/v1/ranch-units/"+aliceRanch).header("Authorization","Bearer bob-token")).andExpect(status().isForbidden());
        jdbc.update("insert into iam.ranch_membership(id,user_account_id,ranch_id,membership_type,active,created_at) values (?,?,?,'Operator',TRUE,CURRENT_TIMESTAMP)",UUID.randomUUID(),bobId,UUID.fromString(aliceRanch));
        mvc.perform(get("/api/v1/ranch-units/"+aliceRanch).header("Authorization","Bearer bob-token")).andExpect(status().isOk());
    }
    @Test void swaggerIsPublic() throws Exception {
        mvc.perform(get("/v3/api-docs")).andExpect(status().isOk()).andExpect(jsonPath("paths['/api/v1/auth/login']").exists());
    }
}
