package com.smartfarm.iam.infrastructure;
import io.swagger.v3.oas.models.*;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.security.SecurityScheme;
import org.springframework.context.annotation.*;
@Configuration
public class OpenApiConfiguration {
    @Bean OpenAPI openAPI() {
        return new OpenAPI().info(new Info().title("ICHU Identity & Access Management API").version("v1")
            .description("Firebase identity validation, account registration and ranch membership authorization"))
            .components(new Components().addSecuritySchemes("bearerAuth", new SecurityScheme()
                .type(SecurityScheme.Type.HTTP).scheme("bearer").bearerFormat("Firebase ID token")));
    }
}
