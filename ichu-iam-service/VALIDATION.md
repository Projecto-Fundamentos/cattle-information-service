# Validación — 0.2.0 (Firebase / C4)

Verificación completada por el usuario desde Windows el 9 de octubre de 2026 a las 20:24 (America/Bogota). Resultado corroborado leyendo validation.log:

- **BUILD SUCCESS** con Java 21 y Maven.
- **20 pruebas ejecutadas: 0 fallos, 0 errores y 0 omitidas**.
- FirebaseAuthenticationAclTest: 3 pruebas aprobadas, con el SDK simulado.
- IdentityApiTest: 16 pruebas aprobadas, con H2 y el proveedor de identidad simulado.
- MembershipMigrationTest: 1 prueba aprobada; migración V1 → V2 conservando datos y creando la membresía Owner.
- JAR ejecutable generado: `target/ichu-iam-service-0.2.0.jar`.
- Tiempo total de Maven: 19.137 segundos.

Los avisos `Ranch access denied` son esperados: las pruebas comprueban que se rechacen accesos sin permisos. Las advertencias de Mockito y Commons Logging no impidieron la compilación ni las pruebas.

## Pendiente de verificar

- Recorrido HTTP completo contra Firebase Emulator (demo-ichu).
- Firebase real, PostgreSQL 16, Docker y GitHub Actions.

Las pruebas con dobles de IdentityProvider/FirebaseAuth verifican el comportamiento local y la delegación al Admin SDK, pero no sustituyen las pruebas reales del proveedor de autenticación.

## Siguiente paso

Ejecutar `iniciar-emulador.cmd` (Node.js 22+ con npm) y, en otra ventana, `iniciar-local.cmd`. Seguir el README para obtener un ID token, registrar una unidad y probar login y permisos desde Swagger.

## Historial

La versión 0.1.0 con JWT propio pasó 11 pruebas. La versión actual 0.2.0 con Firebase pasó las 20 pruebas descritas arriba. El bloqueo inicial de rutas Java dentro del entorno restringido del asistente se evitó ejecutando Maven desde la terminal normal de Windows.
