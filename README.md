# Todo

A todo application built for the XFlow full-stack developer case. It has a React frontend on top
of a REST API in ASP.NET Core, and stores its data in memory.

You can create todos, see them all, mark them as done (and undo that) and delete them.

## Getting started

### With a container

The only requirement is Docker.

```sh
docker build -t xflow-todo .
docker run --rm -p 5135:8080 xflow-todo
```

Then open <http://localhost:5135>.

### For development

You need the .NET 10 SDK and Node.js 24 or later. Run these in two terminals:

```sh
dotnet run --project Todo --launch-profile http
```

```sh
cd frontend
npm ci
npm run dev
```

Then open <http://localhost:5173>. Vite reloads the frontend on changes and forwards API calls to
the backend on port 5135.

### Tests

```sh
dotnet test
```

The end-to-end tests drive a real browser against the real backend. They build the frontend and
start the backend themselves:

```sh
cd frontend
npm ci
npx playwright install chromium
npm run test:e2e
```

To test the running container instead, start it as above and add `E2E_BASE_URL`:

```sh
E2E_BASE_URL=http://localhost:5135 npm run test:e2e
```

Data is lost when the application stops, as the case allows.

## Where this differs from the brief

The case PDF and the README in the starting repository disagree: the PDF lets you choose how to
build the UI, while the README requires MVC. I followed the PDF and built a React app with a REST
API, because the role is full-stack. These are the other changes to the starting repository:

- **.NET 10 instead of .NET 8.** .NET 8 reaches end of support on 10 November 2026. The `global.json`
  version was also invalid.
- **Removed from version control:** build output and IDE files. There was no `.gitignore`, and more
  than half of the tracked files were generated.
- **Changes to the repository interface.** I removed `Update`, because nothing edits a todo. A
  missing id now gives `null` or `false`, because that is an expected outcome and not an exception.
- **Added:** a test project and a Dockerfile.

## Architecture

```text
frontend/        React app: UI and calls to the API, plus end-to-end tests
Todo/            ASP.NET Core: HTTP endpoints, request and response types, composition root
Infrastructure/  In-memory implementation of the repository
Core/            Domain model and repository interfaces, with no dependencies
Tests/           Repository tests and API tests over HTTP
```

The dependencies point inwards: `Todo` and `Infrastructure` depend on `Core`, and `Core` depends on
nothing. A database would take a new `ITodoRepository` implementation and a one-line change in
`Program.cs`.

| Endpoint                       | Result                                      |
| ------------------------------ | ------------------------------------------- |
| `GET /api/todos`               | `200` with all todos, oldest first          |
| `GET /api/todos/{id}`          | `200`, or `404`                             |
| `POST /api/todos`              | `201` with the new todo, or `400` if invalid |
| `POST /api/todos/{id}/toggle`  | `200` with the updated todo, or `404`       |
| `DELETE /api/todos/{id}`       | `204`, or `404`                             |

## Design decisions

- **Repository pattern and dependency injection.** The controller depends on an interface, which
  gives dependency inversion and makes the storage replaceable.
- **No service layer.** With no business logic to hold, it would only forward calls. The one domain
  rule, that `CompletedAt` follows `IsComplete`, is in `Todo.Toggle()`.
- **A lock, and copies out of the repository.** The repository is a singleton shared between
  concurrent requests. Copies mean no one can change a stored todo outside the lock. A list keeps
  creation order, and the linear lookups don't matter at this size.
- **Separate API types.** `CreateTodoRequest` and `TodoResponse` keep the JSON contract independent
  of the domain model and mean a client cannot set fields such as `Id`.
- **One origin.** ASP.NET Core serves the built frontend, so there is one thing to deploy and no CORS
  to configure.
- **TanStack Query for server state.** It handles caching, refetching and loading and error states,
  so the app needs no global state library. TanStack Router and Start were left out: there is one
  page and no need for server rendering.
- **Optimistic toggle, confirmed delete.** A checkbox should respond at once and can simply roll back
  on failure. A deleted row waits for the server, so it still exists if an error has to be shown.
  Only the last change to finish refetches the list, so concurrent changes don't undo each other.
- **Tests at the boundaries.** The API tests and the end-to-end tests use the real pipeline rather
  than mocks. React components have no unit tests, because the end-to-end tests cover their behaviour
  and the components hold little logic of their own.

## Known limitations and what I would do next

- **Toggle is not idempotent.** A stale tab can undo a completion made in another tab. Setting the
  state explicitly with `PUT /api/todos/{id}/complete` would fix that.
- **The title limit is defined twice,** in the API and in the frontend. Generating the client from an
  OpenAPI document would remove the duplication.
- **Real persistence** with EF Core and PostgreSQL, which would also make the in-memory lock
  unnecessary.
- **Users and authentication,** since all todos are currently shared.
- **CI** that runs all the tests and builds the image on every push.
