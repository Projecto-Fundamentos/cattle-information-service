# ICHU IAM — Java/Spring Boot, Firebase y C4

Versión **0.2.0**. Implementa el alcance de registro de cuenta/unidad productiva y autenticación siguiendo el flujo de identidad del C4: **cliente → Firebase Authentication → ID token → API IAM**. Java 21 / Spring Boot 3.5.7 sustituyen la tecnología .NET del diagrama por petición del usuario; el servicio continúa como proceso independiente.

## Qué cambia respecto a 0.1.0

- Firebase Authentication es el único proveedor de identidad. Ya no se aceptan JWT HS256 propios ni se requieren `JWT_SECRET` o contraseñas en IAM.
- `POST /api/v1/ranch-units` requiere Bearer Firebase; el cuerpo contiene únicamente los datos de la unidad. La identidad procede del token validado.
- `POST /api/v1/auth/login` recibe `{ "idToken": "..." }`. Valida ese token y devuelve el mismo como `accessToken`, su tiempo restante y los datos de IAM. **No intercambia tokens ni autentica contraseñas**. Las credenciales y la renovación de sesión se manejan en Firebase desde el cliente, como en el C4.
- Roles (`RanchManager`, `FieldOperator`, `Veterinarian`) separados de membresías (`Owner`, `Operator`). El registro crea RanchManager + Owner; no permite elegir privilegios en el cuerpo.
- Una cuenta puede registrar varias unidades con nombres distintos y tener varias membresías.
- El acceso a una unidad comprueba estado, rol y membresía activa en la base de datos en cada consulta.

## Probar sin un proyecto Firebase real (Windows)

El emulador usa el ID ficticio `demo-ichu`. No requiere cuenta Firebase, proyecto cloud ni credenciales privadas.

1. Detén el servidor anterior con **Ctrl+C**. No borres `data/`.
2. Ejecuta **validar.cmd** y comprueba `BUILD SUCCESS`. Compila 0.2.0 y ejecuta la nueva suite; las 11 pruebas de 0.1.0 no certifican esta versión.
3. Para el emulador necesitas **Node.js 22 o superior con npm**. Ejecuta **iniciar-emulador.cmd**. La primera ejecución descarga Firebase CLI 15.33.0; deja la ventana abierta.
4. Ejecuta **iniciar-local.cmd** en otra ventana. Usa Java 21 portátil en la carpeta original, o `JAVA_HOME` si extrajiste el proyecto en otro lugar. Configura automáticamente `FIREBASE_PROJECT_ID=demo-ichu`, `FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099` y el perfil `local` con H2 persistente.
5. Abre [Swagger](http://localhost:8080/swagger-ui/index.html) y la [interfaz del emulador](http://127.0.0.1:4000).

### Obtener el primer ID token

En una tercera ventana de PowerShell ejecuta:

```powershell
$body = @{email='diego@example.com'; password='MiClaveSegura2026!'; returnSecureToken=$true} | ConvertTo-Json
$session = Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=demo-key' -ContentType 'application/json' -Body $body
$session.idToken
```

Si el usuario ya existe, usa este login en lugar del registro:

```powershell
$session = Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=demo-key' -ContentType 'application/json' -Body $body
$session.idToken
```

La contraseña del ejemplo es solo para el emulador local. Los datos de Firebase Emulator se pierden al detenerlo salvo que configures exportación/importación; H2 sí conserva los datos en `data/`. Para reutilizar una cuenta local con una identidad Firebase recreada con otro UID, no se reasigna automáticamente: usa una identidad persistida del emulador o un proceso de recuperación revisado.

### Registrar la unidad

En Swagger, pulsa **Authorize** y pega el ID token sin el prefijo `Bearer`. Ejecuta `POST /api/v1/ranch-units`:

```json
{
  "name": "Fundo San José",
  "region": "Apurímac",
  "district": "Andahuaylas",
  "estimatedHeadCount": 120,
  "latitude": -13.6556,
  "longitude": -73.3878
}
```

Respuesta **201**, con `id`, `accountId` y `accountStatus`. La cuenta IAM, la unidad y la membresía se guardan en una transacción; la identidad Firebase ya existía antes. Si falla el alta local, puedes reintentar con esa identidad sin crear otra cuenta Firebase.

### Login y comprobación

`POST /api/v1/auth/login`:

```json
{"idToken":"PEGA_AQUI_EL_ID_TOKEN_DE_FIREBASE"}
```

Devuelve **200** con `accessToken` (el mismo ID token), `tokenType`, `expiresIn` e `identity` (cuenta, correo, rol, estado y membresías). Si aún no registraste una unidad devuelve **404 ACCOUNT_NOT_REGISTERED**. No existe un token local de acceso alternativo.

`GET /api/v1/auth/me` muestra tu cuenta y las membresías activas.

`GET /api/v1/ranch-units/{id}` exige correo verificado, cuenta activa y rol/membresía compatible. Un usuario pendiente puede completar el registro e inspeccionar su identidad, pero no acceder a esa consulta de la unidad.

Para verificar el correo en el emulador, edita el usuario desde su interfaz y marca el correo como verificado. Inicia sesión otra vez para obtener un ID token actualizado y reemplázalo en Swagger. En producción el cliente debe completar el flujo de verificación de correo de Firebase.

## Configurar Firebase real

1. Crea un proyecto Firebase y habilita Authentication con el proveedor que usará el frontend (por ejemplo email/password).
2. Define `FIREBASE_PROJECT_ID` y configura Application Default Credentials para el Admin SDK. En desarrollo local, `GOOGLE_APPLICATION_CREDENTIALS` puede apuntar a un archivo de cuenta de servicio **fuera del repositorio**. No publiques ni envíes ese archivo al chat.
3. **Elimina `FIREBASE_AUTH_EMULATOR_HOST` del entorno**. El servicio rechaza el uso del emulador si el perfil `local` no está activo.
4. Configura PostgreSQL con `DB_URL`, `DB_USER`, `DB_PASSWORD`. Ejecuta el JAR con esas variables. El perfil `local` selecciona H2 si solo necesitas probar Firebase real sin PostgreSQL.
5. El frontend obtiene sus ID tokens desde Firebase y los envía por HTTPS al backend. El backend verifica firma, emisor, audiencia, expiración y revocación/usuario deshabilitado mediante `verifyIdToken(token, true)`.

No hay credenciales predeterminadas, autenticación simulada en producción ni fallback a JWT antiguos. Las pruebas automáticas sustituyen la interfaz del proveedor; esto no equivale a probar Firebase real.

## PostgreSQL / Docker

Completa `.env` a partir de `.env.example`: `DB_PASSWORD`, `FIREBASE_PROJECT_ID` y `FIREBASE_CREDENTIALS_FILE` (ruta absoluta a credenciales externas). Ejecuta `docker compose up --build`. La cuenta de servicio se monta como archivo de solo lectura y no se copia a la imagen. El usuario del contenedor debe poder leer ese archivo. Para cloud se recomienda identidad de carga de trabajo/ADC en vez de distribuir claves.

Docker no levanta el emulador. Esta configuración usa Firebase real y PostgreSQL 16. HTTPS debe configurarse en el punto de entrada del despliegue.

## Migración de los datos anteriores

- **V1 permanece intacta.** V2 añade UID externo, rol y membresías; conserva IDs, cuentas y unidades.
- Las unidades previas reciben una membresía Owner para su propietario original. Se elimina la restricción de una única unidad por propietario y se añade unicidad de nombre normalizado por propietario.
- La columna de hash antigua se conserva temporalmente para no destruir información durante la migración; **nunca se usa para autenticar** y se limpia al vincular la cuenta.
- Vincular una cuenta antigua exige un token Firebase con el mismo correo **verificado**. Un UID distinto no puede apropiarse de una cuenta ya vinculada.
- No se convierten automáticamente las contraseñas antiguas a Firebase. El usuario debe tener una identidad Firebase y verificar su correo. Haz una copia de seguridad de la base antes de migrar un entorno con datos importantes.

## Componentes y alcance C4

| C4 | Implementación |
| --- | --- |
| Identity & Profile API | `IdentityController` (alcance de cuenta/unidad/login) |
| Identity & Profile Application Service | `IdentityService` |
| User Account Aggregate | `Account` + membresías |
| Authorization Domain Service | `AuthorizationDomainService` |
| Identity Repository | repositorios JPA de cuentas, unidades y membresías |
| Firebase Authentication ACL | `IdentityProvider` + `FirebaseAuthenticationAcl` |

Los modelos `ProfessionalProfile` y `HerdAdvisory`, y sus endpoints, siguen fuera del alcance de los dos endpoints solicitados. El rol Veterinarian queda representado, pero no recibe acceso sin implementar las asesorías y sus reglas. No se afirma que todo IAM o todo ICHU esté implementado. Los nombres físicos `accounts`/`ranch_units` se mantienen para preservar compatibilidad; representan los conceptos UserAccount/Ranch del modelo.

El evento `RanchUnitRegisteredEvent` sigue siendo local al proceso. No hay outbox ni broker. Los demás servicios no deben inferir permisos vigentes únicamente a partir de un token Firebase.

## Pruebas

Ejecuta `validar.cmd` o `./mvnw verify` (JDK 21). La suite cubre alta, login, identidad inválida, acceso entre unidades, rol incorrecto, revocación de membresías, verificación de correo, vinculación segura de cuentas antiguas, validación y migración V1 → V2. La prueba del adaptador comprueba que se llama al Admin SDK con verificación de revocación, usando mocks.

Para PostgreSQL usa una base **exclusiva de pruebas** y `./mvnw test -Dspring.profiles.active=postgres-test` con variables DB. Las pruebas eliminan las cuentas, unidades y membresías de esa base. GitHub Actions configura ambos perfiles.

Consulta [VALIDATION.md](VALIDATION.md) para distinguir lo ejecutado de lo pendiente, y [docs/C4-REVIEW.md](docs/C4-REVIEW.md) para la revisión arquitectónica.

Fuentes: [validación de tokens Firebase](https://firebase.google.com/docs/auth/admin/verify-id-tokens), [emulador Authentication](https://firebase.google.com/docs/emulator-suite/connect_auth), [REST de Authentication](https://firebase.google.com/docs/reference/rest/auth).

### Si aparece un aviso sobre JAVA_HOME

Los scripts buscan un JDK en JAVA_HOME, en la carpeta de herramientas del proyecto original de este equipo y en PATH (javac). Usa la version actualizada del ZIP y extrae toda la carpeta antes de ejecutar; no ejecutes los archivos directamente dentro del ZIP. La busqueda no modifica variables globales de Windows.

## Verificación confirmada de 0.2.0

El 9 de octubre de 2026: **BUILD SUCCESS y 20 pruebas aprobadas**, sin fallos, errores ni pruebas omitidas. Se generó el JAR 0.2.0. Falta verificar el recorrido con Firebase Emulator y los despliegues externos; las pruebas automatizadas usan dobles del proveedor.
