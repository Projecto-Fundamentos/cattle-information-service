package com.smartfarm.iam.infrastructure;
import com.google.auth.oauth2.GoogleCredentials;
import com.google.auth.oauth2.AccessToken;
import com.google.firebase.*;
import com.google.firebase.auth.FirebaseAuth;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.*;
import org.springframework.core.env.Environment;
import java.io.IOException;
import java.util.Arrays;
@Configuration
@Profile("!test & !postgres-test")
public class FirebaseConfiguration {
    @Bean(destroyMethod="delete")
    FirebaseApp firebaseApp(@Value("${app.firebase.project-id}") String projectId,Environment environment) throws IOException {
        if(projectId.isBlank()) throw new IllegalArgumentException("Set FIREBASE_PROJECT_ID");
        String emulator=System.getenv("FIREBASE_AUTH_EMULATOR_HOST");
        if(emulator!=null && !emulator.isBlank() && !Arrays.asList(environment.getActiveProfiles()).contains("local"))
            throw new IllegalStateException("Firebase emulator is allowed only with the local profile");
        var builder=FirebaseOptions.builder().setProjectId(projectId).setConnectTimeout(5000).setReadTimeout(10000);
        if(emulator==null || emulator.isBlank()) builder.setCredentials(GoogleCredentials.getApplicationDefault());
        else {
            if(!projectId.startsWith("demo-")) throw new IllegalStateException("Use a demo- project ID with the local emulator");
            builder.setCredentials(GoogleCredentials.create(new AccessToken("owner",null)));
        }
        // The SDK substitutes its renewable emulator credentials when the emulator host is configured.
        return FirebaseApp.initializeApp(builder.build(),"ichu-iam");
    }
    @Bean FirebaseAuth firebaseAuth(FirebaseApp app){return FirebaseAuth.getInstance(app);}
}
