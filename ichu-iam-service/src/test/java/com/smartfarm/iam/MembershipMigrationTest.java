package com.smartfarm.iam;
import org.flywaydb.core.Flyway;
import org.junit.jupiter.api.Test;
import java.sql.*;
import java.util.UUID;
import static org.assertj.core.api.Assertions.*;
class MembershipMigrationTest {
    @Test void v2PreservesLegacyDataAndBackfillsOwnerMembership() throws Exception {
        String url="jdbc:h2:mem:migration-"+UUID.randomUUID()+";MODE=PostgreSQL;DATABASE_TO_LOWER=TRUE;DB_CLOSE_DELAY=-1";
        Flyway.configure().dataSource(url,"sa","").schemas("iam").defaultSchema("iam").target("1").load().migrate();
        String account=UUID.randomUUID().toString(),ranch=UUID.randomUUID().toString();
        try(var con=DriverManager.getConnection(url,"sa","");var sql=con.createStatement()) {
            sql.executeUpdate("insert into iam.accounts values ('"+account+"','legacy@example.com','hash','PENDING_VERIFICATION',CURRENT_TIMESTAMP)");
            sql.executeUpdate("insert into iam.ranch_units values ('"+ranch+"','"+account+"','Fundo','Region','District',12,NULL,NULL,CURRENT_TIMESTAMP)");
        }
        Flyway.configure().dataSource(url,"sa","").schemas("iam").defaultSchema("iam").load().migrate();
        try(var con=DriverManager.getConnection(url,"sa","");var sql=con.createStatement()) {
            try(var rs=sql.executeQuery("select user_account_id,ranch_id,membership_type from iam.ranch_membership")) {
                assertThat(rs.next()).isTrue();assertThat(rs.getString(1)).isEqualTo(account);
                assertThat(rs.getString(2)).isEqualTo(ranch);assertThat(rs.getString(3)).isEqualTo("Owner");
            }
            try(var rs=sql.executeQuery("select password_hash,external_uid from iam.accounts")) {
                assertThat(rs.next()).isTrue();assertThat(rs.getString(1)).isEqualTo("hash");assertThat(rs.getString(2)).isNull();
            }
        }
    }
}
