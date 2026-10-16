# RaceDay API

ASP.NET Core 10 Web API (Entity Framework Core Code-First, SQL Server) for managing race events, categories, enrolments and results.

## Project structure
| Path | Purpose |
| --- | --- |
| `src/RaceDay.Api` | The API: `Models`, `Data` (DbContext), `DTOs`, `Controllers`, `Services` |
| `tests/RaceDay.Tests` | xUnit tests (run the real API against an in-memory database) |
| `docs/` | Part 1 planning documents: `erd.png`, `api-endpoint-plan.md`, `schema.sql` |
| `.github/workflows/` | `validate-docs.yml` (docs check) and `build-and-test.yml` (build + tests) |

## Running locally
Requires the .NET 10 SDK and SQL Server (LocalDB comes with Visual Studio).

1. (Optional) set your own JWT signing key: `dotnet user-secrets set "Jwt:Key" "<32+ character secret>" --project src/RaceDay.Api`. A development-only placeholder key is in `appsettings.json` so the tests can run in CI.
2. Check the connection string in `src/RaceDay.Api/appsettings.json`.
3. Create the database from the model (Code-First):
   ```
   dotnet ef migrations add InitialCreate --project src/RaceDay.Api
   dotnet ef database update --project src/RaceDay.Api
   ```
   (If `dotnet ef` is missing: `dotnet tool install --global dotnet-ef`.)
4. Run: `dotnet run --project src/RaceDay.Api`, then open `https://localhost:7100/swagger`.
5. Test: `dotnet test`

## Using Swagger
1. Call `POST /api/auth/register`, then `POST /api/auth/login`.
2. Copy the `token` from the login response, click **Authorize** (top right) and paste it in.
3. Every endpoint can now be tried directly from the page. Each one shows its purpose, expected input and responses.

## Design decisions
- **Session management:** stateless JWT bearer tokens (2-hour expiry) carrying the user id, email and role. Roles are enforced with `[Authorize(Roles = ...)]`, and ownership (an organiser may only change their own events) is checked in each controller.
- **Passwords:** hashed with BCrypt; the raw password is never stored or returned.
- **Registering as Organiser:** anyone can register as a Participant, but registering as an Organiser requires a secret `organiserCode` (`Registration:OrganiserCode` in config), so users cannot simply give themselves organiser powers.
- **Cancelling an enrolment** deletes the enrolment row (and so any result linked to it), which frees the place in the category.
- **Event link on enrolments:** an enrolment stores participant and category; its event is reached through Category -> Event.
- **Payments:** the `Payments` table from the Part 1 ERD is kept in the schema and model, but no payment endpoints were planned or requested, so none exist.
- **Status/role columns** store readable text (e.g. `Organiser`, `Run`, `Finished`) and `schema.sql` has CHECK constraints for the allowed values.

## Deviations from the Part 1 plan
| Change | Reason |
| --- | --- |
| `Events` gained `EventType` (Run/Walk/Cycle) and `Distance`; event create/update bodies include them. ERD and `schema.sql` updated. | The Part 2 brief requires every event to capture distance and event type. |
| Added `GET /api/users/me/results`. | The brief requires participants to view their own results; the plan only had the public leaderboard. |
| Added `GET /api/events/{id}/enrolments`. | The brief requires organisers to view all enrolments for their events; the plan only had them per category. |
| `POST /api/auth/register` takes an extra `organiserCode` for Organisers. | Controls who can become an Organiser (see design decisions). |
| `PUT /api/categories/{id}` can return 409. | Prevents lowering capacity below the number already enrolled. |
| Added 400 / 403 / 404 responses on several endpoints. | Documents validation and permission failures the API actually returns. |

## Tests
`dotnet test` runs the suite in `tests/RaceDay.Tests`, covering success and failure cases for registration and login, event management and ownership, role-based rejection (401 and 403), enrolments (including duplicate and full-category cases), and results. The same tests run in GitHub Actions on every push and pull request to `main`.
