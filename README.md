# RaceDay – Event Management System

**Module:** PROG6212 Programming 2B
**POE:** Parts 1 and 2 – Planning, Database, and RESTful API

---

## System Description

RaceDay is a web-based event management system for the South African road running, walking, and cycling community. It lets Event Organisers create and manage events, categories, and participant results, while Participants browse upcoming events, enter events by choosing a category, and track their personal performance history.

- **Part 1** contains the planning artefacts (ERD, API endpoint plan, SQL script).
- **Part 2** contains the RESTful API built to that plan.
- **Part 3** will add the MVC front end and containerisation.

---

## User Roles

- **Organiser** – creates, edits, and deletes their own events; adds Age/Distance categories to those events; views all enrolments for their events; updates enrolment status; captures and updates participant results.
- **Participant** – creates an account, browses events, enters an event by selecting a category, views their own enrolments, and tracks their personal results.

Role-based access is enforced at the API level in Part 2 and will be reflected in the MVC interface in Part 3.

---

## Repository Structure

```
RaceDay_Api.sln
RaceDay_Api/                   ASP.NET Core 8 Web API
  Controllers/                 Auth, Users, Events, Categories, Enrolments, Results
  Data/                        AppDbContext (Code-First)
  DTOs/                        Request objects
  Models/                      Entity classes
  Services/                    IPasswordService / PasswordService
  Auth/                        SessionAuthorize, RoleAuthorize
  Migrations/
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
- `profile_image_url` and `banner_image_url` are planned now for the Azure Blob Storage integration in Part 3.

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

## Setup and run

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

> The placeholder password hashes in the Part 1 seed data are not real BCrypt hashes, so those seeded users cannot log in. Register new users through the API instead.

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
| POST | `/api/users/me/profile-image` | Participant (placeholder – Azure Blob in Part 3) |

### Events (`EventsController`)
| Method | Route | Role |
|---|---|---|
| GET | `/api/events` (optional `?type=` and `?date=`) | Any logged-in user |
| GET | `/api/events/{id}` | Any logged-in user |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Organiser (owner) |
| DELETE | `/api/events/{id}` | Organiser (owner) |
| POST | `/api/events/{id}/banner` | Organiser (owner) – placeholder, Azure Blob in Part 3 |

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
- CORS is configured for the Part 3 MVC client (`https://localhost:7001` for now, to be updated with the real URL).

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
| `POST /api/users/me/profile-image` and `POST /api/events/{id}/banner` are placeholders | Azure Blob Storage is added in Part 3. The endpoints and role rules are already in place, so Part 3 only adds the storage implementation. |
| `POST /api/auth/register` also starts a session | Convenience: new users are logged in immediately. `POST /api/auth/login` still works as planned. |
| `POST /api/events/{eventId}/enrolments` takes `{ categoryId }` | The event id comes from the route, so it is not repeated in the body. |

## Unit tests

`RaceDay_Testing` boots the real API in memory using `WebApplicationFactory<Program>` with the EF Core InMemory provider, and explicitly seeds the two roles (InMemory does not apply `HasData` reliably). The API runs in a `Testing` environment, where HTTPS redirection is switched off so tests can use plain HTTP.

| File | What it covers |
|---|---|
| `AuthTests.cs` | Register then fetch profile; login with the correct password (200); login with a wrong password (401) |
| `RoleTests.cs` | Participant cannot create an event (403); unauthenticated requests are rejected (401) |
| `EventsTests.cs` | Organiser can create an event (201); invalid event type returns 400; logged-in user can list events; Organiser cannot update another Organiser's event (403) |
| `EnrolmentsTests.cs` | Participant can enrol (201); duplicate enrolment returns 409; Organiser cannot enrol as a Participant (403); Organiser can view enrolments for their own event |

## CI/CD

Workflow: `.github/workflows/dotnet.yml` (restore, build, and test on every push).

**CI screenshot:** 
<img width="1920" height="1008" alt="image" src="https://github.com/user-attachments/assets/c2af8e54-dce6-4e70-bbf4-e09a220e0e70" />


## Video walkthrough

**YouTube (unlisted):** 
https://youtu.be/OMAWLUNDShA

## AI usage disclosure

AI assistance was used to help scaffold and debug the API, tests, and documentation. I reviewed, ran, and understand the code, and I explain its structure and design decisions in the video.
