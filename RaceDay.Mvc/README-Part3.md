# RaceDay Application -- Part 3

## ASP.NET Core MVC, Azure Blob Storage and Docker

Part 3 extends the RaceDay application developed in Part 2 by adding an
ASP.NET Core MVC web application. The MVC application communicates with
the existing RaceDay API and provides separate functionality for
organisers and participants.

**Repository:** https://github.com/KevinThendow/RaceDay.Api\
**Part 3 branch:** `part-3`

------------------------------------------------------------------------

## 1. Solution structure

  Project/folder        Purpose
  --------------------- --------------------------------------------------
  `RaceDay.Api`         Web API endpoints and application business logic
  `RaceDay.Contracts`   Shared data-transfer objects and contracts
  `RaceDay.Mvc`         ASP.NET Core MVC user interface
  `RaceDay.Api.Tests`   API test project
  `docs`                Project documentation and supporting evidence

The MVC application communicates with the API through the
`RaceDayApiClient` service. The MVC project does not connect directly to
the application database.

## 2. Technologies

-   C# and .NET 8
-   ASP.NET Core Web API and MVC
-   Entity Framework Core
-   SQL Server
-   Azure Blob Storage
-   Docker Desktop
-   Git and GitHub
-   Visual Studio

## 3. Application functionality

### Participant functionality

-   Register and log in.
-   Browse race events and view event details.
-   View available event categories.
-   Enrol in an event.
-   View personal event enrolments.
-   View race results.
-   View and update profile information.
-   Upload a profile picture.

### Organiser functionality

-   Log in and access the organiser dashboard.
-   Create and view race events.
-   Edit existing events.
-   Create and manage event categories.
-   View participant enrolments for an event.
-   Record race results.

The available pages and actions depend on the user's role and the
permissions enforced by the API.

## 4. Azure Blob Storage

Azure Blob Storage is integrated for participant profile-picture
storage.

The application uploads the selected image to the configured private
blob container. The API stores the blob reference with the participant's
profile and provides a time-limited read URL when the image needs to be
displayed.

### Configuration and security

Configure these API settings securely:

-   `AzureBlobStorage:ConnectionString`
-   `AzureBlobStorage:ContainerName`

Use .NET User Secrets for local development or environment
variables/secure configuration in a deployment environment. Do not
commit connection strings, access keys, passwords, `secrets.json`, or
other credentials to GitHub.

The profile-image container is intended to remain private. Do not enable
anonymous public access merely to display profile pictures.

## 5. Run the application locally

1.  Open `RaceDay.Api.slnx` in Visual Studio.
2.  Configure the API's database connection and other required settings.
3.  Configure Azure Blob Storage settings securely for the API.
4.  Start the `RaceDay.Api` project.
5.  Start the `RaceDay.Mvc` project.
6.  Open the MVC URL shown by the application or configured in its
    launch settings.

The MVC API base URL is configured through `ApiSettings:BaseUrl`. The
local value in `RaceDay.Mvc/appsettings.json` is:

``` json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5041/"
  }
}
```

Use the actual API URL and port configured on your computer. Do not
assume that the example port is the same on every machine.

## 6. Docker deployment of the MVC application

The MVC Dockerfile is located at `RaceDay.Mvc/Dockerfile`. Build the
image from the solution root directory so that the Docker build context
includes the MVC and Contracts projects.

### Build the MVC image

``` bash
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc:part3 .
```

### Start the API on the Windows host

For the tested local setup, start the API so that it listens on port
5000:

``` bash
dotnet run --project RaceDay.Api --urls http://0.0.0.0:5000
```

Leave the API running while using the MVC container.

### Run the MVC container

``` bash
docker run -d --name raceday-mvc -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5000/ raceday-mvc:part3
```

`host.docker.internal` allows the container to reach the API running on
the Windows host. The environment variable overrides the MVC API base
URL for this Docker setup.

### Open the application

Browse to:

``` text
http://localhost:8080
```

### Useful Docker commands

Check running containers:

``` bash
docker ps
```

View the MVC container logs:

``` bash
docker logs raceday-mvc
```

Stop the container:

``` bash
docker stop raceday-mvc
```

Start it again:

``` bash
docker start raceday-mvc
```

Remove the stopped container:

``` bash
docker rm raceday-mvc
```

## 7. Testing performed

The following manual checks were performed using the MVC application
running in Docker and the API running on the host:

-   MVC home page loaded through `http://localhost:8080`.
-   Participant registration and login.
-   Participant event browsing and event details.
-   Participant enrolment in an event after an organiser added an event
    category.
-   Participant My Events and My Results pages.
-   Participant profile page.
-   Organiser login and dashboard.
-   Organiser event creation and editing.
-   Organiser category management.
-   Organiser viewing participant enrolments.
-   Organiser recording a race result.
-   Participant viewing the recorded result.
-   API requests from the MVC container, confirmed through the container
    logs.

These are manual application checks; they should not be interpreted as a
claim that every possible automated or production test has been
completed.

## 8. GitHub and Part 3 source code

The existing Part 2 repository is being extended for Part 3. Part 3
development is organised on the `part-3` branch so that the Part 2
`master` branch remains separate.

Repository:

https://github.com/KevinThendow/RaceDay.Api

To view the Part 3 branch, open the repository's branch selector and
choose `part-3` after the branch has been pushed to GitHub.

## 9. YouTube demonstration video

A video demonstration is required to show the completed Part 3
application. The video has **not yet been recorded or uploaded**.

After recording and uploading the demonstration, replace the placeholder
below with the actual YouTube URL:

**YouTube demonstration:** `INSERT_YOUTUBE_VIDEO_URL_HERE`

Recommended demonstration contents:

1.  Introduce the RaceDay application and its purpose.
2.  Show the MVC home page and navigation.
3.  Demonstrate participant registration and login.
4.  Browse events and view event details.
5.  Show event category selection and participant enrolment.
6.  Show the participant's My Events and My Results pages.
7.  Log in as an organiser and show the organiser dashboard.
8.  Demonstrate event creation and editing.
9.  Demonstrate category management and viewing enrolments.
10. Record a race result and show it on the participant's My Results
    page.
11. Demonstrate profile-picture upload using Azure Blob Storage.
12. Show the Docker image, running container, and application in the
    browser at `http://localhost:8080`.

Set the YouTube video's visibility to **Unlisted** or **Public** so the
lecturer can view it. Check the link in a private/incognito browser
window before submitting.

## 10. Security notes

-   Never commit Azure connection strings, passwords, API keys, or local
    secrets.
-   Keep `secrets.json` and development-only configuration out of source
    control.
-   Keep the Azure profile-image container private.
-   Use secure configuration mechanisms for local and deployed
    environments.
-   Review staged files before every commit and push.

## 11. Conclusion

Part 3 adds an ASP.NET Core MVC interface to the RaceDay solution, with
participant and organiser workflows, Azure Blob Storage integration for
profile pictures, and Docker deployment of the MVC application. The
manual tests listed above confirm the principal participant and
organiser workflows and the communication between the MVC container and
the API running on the host.

Before final submission, add the YouTube demonstration URL and verify
that the `part-3` branch and all required project files have been pushed
to GitHub.
