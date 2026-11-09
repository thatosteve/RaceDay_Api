# Part 3 - MVC Web Application, Azure Blob Storage and Docker

Part 3 adds `RaceDay_Mvc`, an ASP.NET Core 8 MVC front end that consumes the Part 2 API. The MVC project has **no database access and no duplicated business logic**: every read and write goes through `ApiClient`, and **all file uploads go through the API**, which stores images in Azure Blob Storage. The MVC app never talks to Azure.

## What Part 3 adds
- `RaceDay_Mvc/` MVC project: role-aware navigation, Organiser and Participant interfaces, RaceDay theme (Bootstrap + custom CSS).
- `ApiClient` forwards the API's session cookie, so the API remains the single authority for authentication and roles.
- `[RoleAuthorize]` filter: not logged in -> login page; wrong role -> access denied page.
- API changes: real Azure Blob upload for event banners and profile pictures, `GET /api/events/mine` for the dashboard, `ResultId` on the organiser enrolments list, and `TotalFinishers` on `GET /api/results/me`.
- Dockerfile for the MVC app and an updated GitHub Actions workflow (builds API + MVC, runs tests, builds the Docker image).

## Pages
| Role | Pages |
|---|---|
| Organiser | Dashboard, My events (create / edit / delete with banner upload), Categories, Enrolments (confirm / set pending), Results entry, Profile |
| Participant | Find events (filters), Event details + Enter event, My entries (colour-coded status), My results (time + position, e.g. "47th out of 312 finishers"), Profile with picture upload |

## Running locally
1. Run `Update-Database` for the API as in Part 2.
2. Store the Azure connection string (never commit it):
   `dotnet user-secrets set "AzureBlob:ConnectionString" "<connection string>" --project RaceDay_Api`
3. Set `ApiBaseUrl` in `RaceDay_Mvc/appsettings.json` to the API's HTTPS address.
4. In Visual Studio: Solution > Configure Startup Projects > Multiple startup projects > start `RaceDay_Api` and `RaceDay_Mvc`.

## Running the MVC app in Docker
The container needs to reach the API running on your machine, so start the API listening on all interfaces:

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
Workflow: `.github/workflows/dotnet.yml` restores, builds the whole solution (API + MVC), runs the unit tests and builds the Docker image.

**CI screenshot:** _(paste the green build screenshot here)_

## Video walkthrough
**YouTube (unlisted):** _(paste link here)_

## AI usage disclosure
AI assistance was used to help scaffold and debug the MVC application, API changes, Dockerfile and documentation. I reviewed, ran and understand the code and explain its structure and design decisions in the video.
