# RaceDay Event Management System

## Project Description

RaceDay is a RESTful Web API for managing running, walking and cycling
events. It supports registration, login, profiles, events, categories,
enrolments and results. This repository contains the Part 2 API for the
PROG6212 Programming 2B PoE.

## User Roles

### Organiser

Organisers can manage their own profiles and events, manage categories
for events they own, view their event enrolments, and record/view
results. \### Participant Participants can manage their own profiles,
browse events and categories, enrol in events, and view their own
enrolments and results.

## Technologies Used

-   C#
-   ASP.NET Core Web API (.NET 8)
-   Entity Framework Core
-   SQL Server
-   Swagger / OpenAPI
-   xUnit automated tests
-   GitHub Actions

## Project Structure

``` text
RaceDay.Api/
├── Controllers/
├── Data/
├── Migrations/
├── Models/
├── Program.cs
└── appsettings.json
RaceDay.Contracts/
└── Dtos.cs
RaceDay.Api.Tests/
└── EventsApiTests.cs
.github/
└── workflows/
    └── dotnet.yml
```

-   `RaceDay.Api`: API endpoints and database access.
-   `RaceDay.Contracts`: shared request and response DTOs.
-   `RaceDay.Api.Tests`: automated tests.

## Database Setup

The API uses SQL Server and Entity Framework Core Code First migrations.

1.  Ensure SQL Server is running.
2.  Open `RaceDay.Api/appsettings.json` and verify the
    `RaceDayConnection` connection string matches your SQL Server
    instance.
3.  In Visual Studio, open **Tools \> NuGet Package Manager \> Package
    Manager Console**.
4.  Set **Default project** to `RaceDay.Api`.
5.  Run:

``` powershell
Update-Database
```

The configured development database is `RaceDayPart2Db`. Confirm that
the database and tables exist in SQL Server Management Studio (SSMS). Do
not commit real passwords or secrets to a public repository.

## How to Run the API

1.  Open `<img width="960" height="504" alt="image" src="https://github.com/user-attachments/assets/43aea454-faa6-456b-8168-84991378638a" />` in Visual Studio.
2.  Restore NuGet packages.
3.  Check the SQL Server connection string and ensure the database
    exists.
4.  Right-click `RaceDay.Api` in Solution Explorer and choose **Set as
    Startup Project**.
5.  Run using the HTTPS profile.
6.  Open the Swagger URL shown by Visual Studio.

## Authentication and Sessions

Users register as `Organiser` or `Participant`. Passwords are hashed
before storage. After successful login, the API stores the user's ID and
role in the session. Protected endpoints check the session, role and
event ownership where relevant. Logout clears the session.

## API Endpoints

Protected routes require a valid session. Some operations are restricted
to the Organiser who owns an event.

### Authentication

  Method   Endpoint               Purpose
  -------- ---------------------- ---------------------
  POST     `/api/auth/register`   Register an account
  POST     `/api/auth/login`      Log in
  POST     `/api/auth/logout`     Log out

### Profile

  Method   Endpoint            Purpose
  -------- ------------------- --------------------
  GET      `/api/profile/me`   View own profile
  PUT      `/api/profile/me`   Update own profile

### Events

  -----------------------------------------------------------------------
  Method                  Endpoint                Purpose
  ----------------------- ----------------------- -----------------------
  GET                     `/api/events`           List events

  GET                     `/api/events/{id}`      View one event

  GET                     `/api/events/mine`      List events managed by
                                                  the logged-in Organiser

  POST                    `/api/events`           Create event
                                                  (Organiser)

  PUT                     `/api/events/{id}`      Update owned event
                                                  (Organiser)

  DELETE                  `/api/events/{id}`      Delete owned event
                                                  (Organiser)
  -----------------------------------------------------------------------

### Categories

  ------------------------------------------------------------------------------------
  Method                  Endpoint                             Purpose
  ----------------------- ------------------------------------ -----------------------
  GET                     `/api/events/{eventId}/categories`   List event categories

  POST                    `/api/events/{eventId}/categories`   Create category
                                                               (authorised Organiser)

  PUT                     `/api/categories/{id}`               Update category
                                                               (authorised Organiser)

  DELETE                  `/api/categories/{id}`               Delete category
                                                               (authorised Organiser)
  ------------------------------------------------------------------------------------

### Enrolments

  ------------------------------------------------------------------------------------
  Method                  Endpoint                             Purpose
  ----------------------- ------------------------------------ -----------------------
  POST                    `/api/enrolments`                    Enrol as Participant

  GET                     `/api/enrolments/mine`               View own enrolments

  GET                     `/api/events/{eventId}/enrolments`   View event enrolments
                                                               (event-owning
                                                               Organiser)

  PUT                     `/api/enrolments/{id}/status`        Update enrolment status
                                                               (authorised Organiser)
  ------------------------------------------------------------------------------------

### Results

  ---------------------------------------------------------------------------------
  Method                  Endpoint                          Purpose
  ----------------------- --------------------------------- -----------------------
  POST                    `/api/results`                    Record a result
                                                            (authorised Organiser)

  GET                     `/api/results/mine`               View own results
                                                            (Participant)

  GET                     `/api/events/{eventId}/results`   View event results
                                                            (event-owning
                                                            Organiser)
  ---------------------------------------------------------------------------------

Use the running Swagger page as the definitive list of implemented
endpoints. Update this table if more endpoints are added, including a
result-update endpoint if implemented.

## Automated Testing

Tests are in `RaceDay.Api.Tests`. In Visual Studio, open **Test \> Test
Explorer** and select **Run All Tests**. Alternatively, from the
solution directory run:

``` bash
dotnet test
```

Tests should cover successful requests and failure scenarios, including
invalid credentials, unauthenticated access and role restrictions.

## GitHub Actions / CI

The workflow is located at `.github/workflows/dotnet.yml`. It should restore
dependencies, build the solution and run tests when code is pushed.

To check the result, open the repository on GitHub, select **Actions**,
open the latest workflow run, and confirm the job has a green check
mark.

### GitHub Actions Screenshot

Insert a screenshot of a successful green workflow run here before
submission:

`

## Part 1 Planning

Keep the Part 1 ERD, API Endpoint Plan and database script in the
repository, preferably in a `docs/` folder. The Part 2 implementation
should remain consistent with those documents. Explain any intentional
differences between planned and implemented routes.

## Video Presentation

Add your Unlisted YouTube video URL here before submission:

`[Paste your Unlisted YouTube video URL here]`

The video should show the API in Swagger, registration and login,
Organiser and Participant functionality, a rejected wrong-role request,
the database, automated tests and the successful GitHub Actions run.

## Known Development Notes

-   Verify the `RaceDayConnection` string matches the SQL Server
    instance on the computer running the API.
-   Current profile routes use `/api/profile/me`; enrolment and result
    history routes use `/mine`. Keep the README and Part 1 plan aligned,
    and explain route differences.
-   Review the endpoint table after changing controllers.

## Submission Checklist

-   [ ] API builds and runs.
-   [ ] SQL Server database and tables exist.
-   [ ] Swagger loads and endpoints have been tested.
-   [ ] Organiser and Participant access restrictions have been tested.
-   [ ] Automated tests pass locally.
-   [ ] Latest GitHub Actions run passes.
-   [ ] Screenshot of the successful GitHub Actions run is included.
-   [ ] At least 20 meaningful commits are present.
-   [ ] Part 1 planning documents are retained.
-   [ ] Unlisted YouTube video URL is added.
-   [ ] GitHub repository link is submitted as required.
