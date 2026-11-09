# RaceDay – Event Management System

**Module:** PROG6212 Programming 2B
**POE:** Parts 1, 2 and 3 – Planning, RESTful API, MVC Web Application, Azure Blob Storage and Docker

---

## System Description

RaceDay is a web-based event management system for the South African road running, walking, and cycling community. It lets Event Organisers create and manage events, categories, and participant results, while Participants browse upcoming events, enter events by choosing a category, and track their personal performance history.

- **Part 1** – planning artefacts (ERD, API endpoint plan, SQL script) in `/docs`.
- **Part 2** – the RESTful API built to that plan, with session authentication, Swagger and unit tests.
- **Part 3** – the ASP.NET Core MVC front end that consumes the API, Azure Blob Storage for images (through the API), and a Dockerfile for the MVC app.

---

## User Roles

- **Organiser** – creates, edits, and deletes their own events (with a banner image); adds Age/Distance categories to those events; views all enrolments for their events; updates enrolment status; captures and updates participant results.
- **Participant** – creates an account, browses and filters events, enters an event by selecting a category, views their own enrolments and status, tracks their personal results, and manages their profile (including a profile picture).

Role-based access is enforced at the API level (Part 2) and reflected in the MVC interface (Part 3), where each role has its own navigation and pages.

---

## Repository Structure

```
RaceDay_Api.sln
RaceDay_Api/                   ASP.NET Core 8 Web API
  Controllers/                 Auth, Users, Events, Categories, Enrolments, Results
  Data/                        AppDbContext (Code-First)
  DTOs/                        Request objects
  Models/                      Entity classes
  Services/                    PasswordService, BlobService (Azure Blob Storage)
  Auth/                        SessionAuthorize, RoleAuthorize
  Migrations/
RaceDay_Mvc/                   ASP.NET Core 8 MVC front end (Part 3)
  Controllers/                 Account, Home, Organiser, Profile
  Services/                    ApiClient (all calls to the API)
  Filters/                     RoleAuthorize (MVC role guard)
  Models/ Views/ wwwroot/
  Dockerfile
RaceDay_Testing/               xUnit test project
docs/
  erd.png
  erd.dbml
  api-endpoint-plan.docx
  raceday_database.sql
.github/workflows/
  dotnet.yml
README.md
```

---

# Part 1 – System Planning and Database

## The Data Model (6 entities)

`roles`, `users`, `events`, `categories`, `enrolments`, `results`.

Key design decisions:

- `roles` is a lookup table referenced by `users`, keeping role handling normalised.
- `enrolments` is the junction table resolving the many-to-many relationship between Participants and Events. It carries the chosen `category_id` and the enrolment `status` (Pending / Confirmed).
- `results` links to `enrolment_id` (one-to-one), so a result inherits the participant, event, and category from its enrolment without duplicating data.
- `profile_image_url` and `banner_image_url` were planned in Part 1 and are populated in Part 3 from Azure Blob Storage.

## SQL Script

`docs/raceday_database.sql` creates the full schema with all primary keys, foreign keys, `UNIQUE`, `NOT NULL`, `DEFAULT`, and `CHECK` constraints, and seeds:

- 2 Organisers, 2 Participants
- 3 Events with categories
- Sample enrolments and results

It matches the ERD exactly.

---

# Part 2 – RESTful API

Part 2 adds the ASP.NET Core 8 Web API that powers RaceDay. It is built with **Entity Framework Core (Code-First)** against **SQL Server**, uses **server-side session authentication** with **role-based access control**, exposes every endpoint through **Swagger**, and is covered by an **xUnit test project**.

## What Part 2 adds

- A working REST API implementing every endpoint from the Part 1 plan.
- An EF Core Code-First schema matching the Part 1 ERD and SQL script (see "Schema fidelity").
- Session-based authentication with BCrypt password hashing.
- Role-based access enforced with custom `[SessionAuthorize]` and `[RoleAuthorize("Organiser" | "Participant")]` filters.
- Swagger UI for trying every endpoint (enabled in the Development environment).
- xUnit tests covering authentication, role enforcement, events, and enrolments.
- A GitHub Actions workflow that restores, builds, and runs the tests on every push.

## Setup and run (API)

1. Install the **.NET 8 SDK** and **SQL Server** (LocalDB, Express, or full SQL Server).
2. Open `RaceDay_Api.sln` in **Visual Studio 2022**.
3. Confirm the connection string in `RaceDay_Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```
   Change the server name if you are using SQL Express or a full instance.
4. In **Package Manager Console** (Default project = `RaceDay_Api`), run:
   ```powershell
   Update-Database
   ```
   This creates `RaceDayDb` with the full schema and seeds the two roles.

   > If you already ran the Part 1 SQL script against a database with the same name, drop it first (`Drop-Database`) or change the database name. Otherwise the migration fails with SQL error 2714 ("There is already an object named ...").
5. Run the API (**F5** or **Ctrl+F5**) and open:
   ```
   https://localhost:<port>/swagger
   ```
6. Run the tests: **Test → Run All Tests** (or `dotnet test`).

## Using Swagger

1. `POST /api/auth/register` – register a new Organiser or Participant (pass `"role": "Organiser"` or `"role": "Participant"`).
2. `POST /api/auth/login` – log in. The browser stores the session cookie, so you can then try any protected endpoint from the same Swagger page.
3. To switch roles, call `POST /api/auth/logout` first and log in as the other user (or open a fresh browser profile).

> The placeholder password hashes in the Part 1 seed data are not real BCrypt hashes, so those seeded users cannot log in. Register new users through the API or the MVC register page instead.

## Endpoint summary

### Authentication (`AuthController`)
| Method | Route | Role |
|---|---|---|
| POST | `/api/auth/register` | None |
| POST | `/api/auth/login` | None |
| POST | `/api/auth/logout` | Any |

### User profile (`UsersController`)
| Method | Route | Role |
|---|---|---|
| GET | `/api/users/me` | Any |
| PUT | `/api/users/me` | Any |
| POST | `/api/users/me/profile-image` | Participant – uploads to Azure Blob Storage (Part 3) |

### Events (`EventsController`)
| Method | Route | Role |
|---|---|---|
| GET | `/api/events` (optional `?type=` and `?date=`) | Any logged-in user |
| GET | `/api/events/mine` | Organiser – events created by the logged-in Organiser (Part 3, dashboard) |
| GET | `/api/events/{id}` | Any logged-in user |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Organiser (owner) |
| DELETE | `/api/events/{id}` | Organiser (owner) |
| POST | `/api/events/{id}/banner` | Organiser (owner) – uploads to Azure Blob Storage (Part 3) |

### Categories (`CategoriesController`)
| Method | Route | Role |
|---|---|---|
| GET | `/api/events/{eventId}/categories` | Any logged-in user |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) |
| PUT | `/api/categories/{id}` | Organiser (owner) |
| DELETE | `/api/categories/{id}` | Organiser (owner) – 409 if enrolments exist |

### Enrolments (`EnrolmentsController`)
| Method | Route | Role |
|---|---|---|
| POST | `/api/events/{eventId}/enrolments` | Participant |
| GET | `/api/enrolments/me` | Participant |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) |
| PUT | `/api/enrolments/{id}/status` | Organiser (owner) |

### Results (`ResultsController`)
| Method | Route | Role |
|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) |
| PUT | `/api/results/{id}` | Organiser (owner) |
| GET | `/api/results/me` | Participant |

## Validation and status codes

| Rule | Response |
|---|---|
| No session | 401 Unauthorized |
| Wrong role, or an Organiser touching another Organiser's event, category, enrolment, or result | 403 Forbidden |
| Event type is not `run`, `walk`, or `cycle` | 400 Bad Request |
| Category type is not `Age` or `Distance` | 400 Bad Request |
| Enrolling with a category that does not belong to the event | 400 Bad Request |
| Enrolment status is not `Pending` or `Confirmed` | 400 Bad Request |
| Image is missing, over 5 MB, or not JPG/PNG/WEBP | 400 Bad Request |
| Enrolling twice for the same event | 409 Conflict |
| Capturing a second result for the same enrolment | 409 Conflict |
| Deleting a category that has enrolments | 409 Conflict |
| Unknown event, category, enrolment, or result id | 404 Not Found |

## Authentication design

- Passwords are hashed with **BCrypt**; only the hash is stored in `USERS.password_hash`.
- On successful login (or registration), the API stores the user's id and role in the **server-side ASP.NET Core session** (cookie `.RaceDay.Session`, `HttpOnly`, 2-hour idle timeout).
- A custom `[SessionAuthorize]` filter returns **401** when there is no session.
- A custom `[RoleAuthorize("Organiser")]` (or `"Participant"`) filter returns **403** when the session exists but the role is wrong.
- Ownership checks run inside each controller, so an Organiser can only modify resources that belong to their own events.
- `PasswordService.Verify` returns `false` for malformed hashes instead of throwing, so placeholder seed rows cause a failed login rather than a 500.
- Endpoints that list enrolments return projections rather than entities, so password hashes are never sent to the client.

## Schema fidelity

The EF Core Code-First model is configured in `AppDbContext.OnModelCreating` to map to the Part 1 SQL script:

| Part 1 SQL | EF Core mapping |
|---|---|
| Tables `ROLES`, `USERS`, `EVENTS`, `CATEGORIES`, `ENROLMENTS`, `RESULTS` | `ToTable("ROLES")` etc. |
| Columns `role_id`, `user_id`, `event_id`, ... | `HasColumnName("role_id")` etc. |
| `PK_*`, `FK_*`, `UQ_*`, `CK_*` constraint names | `HasConstraintName(...)`, `HasCheckConstraint(...)`, named unique indexes |
| `DEFAULT GETDATE()`, `DEFAULT 'Pending'` | `HasDefaultValueSql(...)`, `HasDefaultValue(...)` |
| No cascade on delete | `OnDelete(DeleteBehavior.Restrict)` on every foreign key |

The only difference in object type is that the SQL script uses `UNIQUE` constraints while EF Core creates equivalent unique indexes. The runtime behaviour is identical.

## Deviations from the Part 1 plan (explained)

| Change | Reason |
|---|---|
| `DELETE /api/categories/{id}` returns 409 if enrolments exist | The foreign key from `ENROLMENTS` is NO ACTION, so the delete would otherwise fail at the database level. 409 gives the client a clear reason. |
| `DELETE /api/events/{id}` removes the event's results, enrolments, and categories first | Foreign keys are RESTRICT, so the API removes dependants in the correct order (results, enrolments, categories, event). |
| `POST /api/auth/register` also starts a session | Convenience: new users are logged in immediately. `POST /api/auth/login` still works as planned. |
| `POST /api/events/{eventId}/enrolments` takes `{ categoryId }` | The event id comes from the route, so it is not repeated in the body. |
| `GET /api/events/mine` added (Part 3) | The Organiser dashboard needs only the events the logged-in Organiser created. |
| `ResultId` added to the Organiser enrolments list; `TotalFinishers` added to `GET /api/results/me` (Part 3) | The results entry page needs to know whether a result already exists, and "My Results" shows positions such as "47th out of 312 finishers". |

## Unit tests

`RaceDay_Testing` boots the real API in memory using `WebApplicationFactory<Program>` with the EF Core InMemory provider, and explicitly seeds the two roles (InMemory does not apply `HasData` reliably). The API runs in a `Testing` environment, where HTTPS redirection is switched off so tests can use plain HTTP. No test contacts Azure: `BlobService` creates its client lazily, so the API starts without Azure credentials.

| File | What it covers |
|---|---|
| `AuthTests.cs` | Register then fetch profile; login with the correct password (200); login with a wrong password (401) |
| `RoleTests.cs` | Participant cannot create an event (403); unauthenticated requests are rejected (401) |
| `EventsTests.cs` | Organiser can create an event (201); invalid event type returns 400; logged-in user can list events; Organiser cannot update another Organiser's event (403) |
| `EnrolmentsTests.cs` | Participant can enrol (201); duplicate enrolment returns 409; Organiser cannot enrol as a Participant (403); Organiser can view enrolments for their own event |

---

# Part 3 – MVC Web Application, Azure Blob Storage and Docker

Part 3 adds `RaceDay_Mvc`, an ASP.NET Core 8 MVC front end that consumes the Part 2 API. The MVC project has **no database access and no duplicated business logic**: every read and write goes through `ApiClient`, and **all file uploads go through the API**, which stores images in Azure Blob Storage. The MVC app never talks to Azure.

## What Part 3 adds

- `RaceDay_Mvc/` project: role-aware navigation, Organiser and Participant interfaces, RaceDay theme (Bootstrap + custom CSS).
- `ApiClient` forwards the API's session cookie, so the API remains the single authority for authentication and roles.
- `[RoleAuthorize]` filter in MVC: not logged in → login page; wrong role → access denied page.
- API changes: real Azure Blob upload for event banners and profile pictures, `GET /api/events/mine` for the dashboard, `ResultId` on the Organiser enrolments list, and `TotalFinishers` on `GET /api/results/me`.
- A Dockerfile for the MVC app and an updated GitHub Actions workflow that builds API + MVC, runs tests, and builds the Docker image.

## Pages

| Role | Pages |
|---|---|
| Organiser | Dashboard, My events (create / edit / delete with banner upload), Categories, Enrolments (confirm / set pending), Results entry, Profile |
| Participant | Find events (filters), Event details + Enter event, My entries (colour-coded status), My results (time + position, e.g. "47th out of 312 finishers"), Profile with picture upload |

## Azure Blob Storage

- The API (`BlobService`) uploads images to a Blob container with public read access for blobs, and saves the resulting URL in `events.banner_image_url` / `users.profile_image_url`.
- Images must be JPG, PNG or WEBP and 5 MB or smaller.
- The container (default name `raceday-images`, override with `AzureBlob:ContainerName`) is created automatically on first upload if it does not exist.
- The connection string is **never committed**. It is stored in user secrets locally, or in an environment variable in other environments.

## Setup and run (full application, local)

**Prerequisites:** .NET 8 SDK, SQL Server (LocalDB/Express), Visual Studio 2022, an Azure Storage account, and Docker Desktop (for the container).

1. Set up the database as described in "Setup and run (API)" above (`Update-Database`).
2. Store the Azure connection string (never commit it):
   ```
   dotnet user-secrets set "AzureBlob:ConnectionString" "<connection string>" --project RaceDay_Api
   ```
3. Set `ApiBaseUrl` in `RaceDay_Mvc/appsettings.json` to the API's HTTPS address (for example `https://localhost:7123`).
4. In Visual Studio: **Solution → Configure Startup Projects → Multiple startup projects** and start both `RaceDay_Api` and `RaceDay_Mvc`.
5. Register an Organiser and a Participant on the MVC register page to try each role.

## Running the MVC app in Docker

The container hosts only the MVC app. The API still runs on your machine (with SQL Server and Azure reachable from it). Start the API listening on all interfaces over plain HTTP:

```
dotnet run --project RaceDay_Api --urls "http://0.0.0.0:5080"
```

Build and run the MVC container:

```
docker build -t raceday-mvc ./RaceDay_Mvc
docker run -d --name raceday-mvc -p 8080:8080 --add-host=host.docker.internal:host-gateway -e ApiBaseUrl=http://host.docker.internal:5080 raceday-mvc
```

Open http://localhost:8080. Stop it with `docker rm -f raceday-mvc`.

## CI/CD

Workflow: `.github/workflows/dotnet.yml` restores and builds the whole solution (API + MVC), runs the unit tests, and builds the MVC Docker image on every push.

<img width="1920" height="1008" alt="Screenshot 2026-11-09 184108" src="https://github.com/user-attachments/assets/4c6bf665-e6aa-4b94-9607-0ccc35317e2c" />


---

## Video walkthroughs

| Part | YouTube (unlisted) |

| Part 3 – MVC, Blob Storage and Docker |(https://youtu.be/xFkRCyyM2M0) |

## AI usage disclosure

AI assistance was used to help scaffold and debug the API, MVC application, tests, Dockerfile and documentation. I reviewed, ran, and understand the code, and I explain its structure and design decisions in the videos.
