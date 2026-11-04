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

Port 5135 serves only the API until the frontend has been built, so starting just the backend, for
example with Run in an IDE, gives a 404 at `/`. To have the backend serve the app on port 5135, run
`npm run build` in `frontend` once and restart the backend.

### Tests

```sh
dotnet test
```

The frontend is linted with Oxlint. A custom plugin in `frontend/tools/oxlint` adds stricter rules,
for example against chained type assertions and against `typeof` checks where a value should be
parsed at its I/O boundary:

```sh
cd frontend
npm ci
npm run lint
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
- **Removed with the move to React:** the Razor views, jQuery and Bootstrap. The HTTPS redirect also
  went, because the README rules out HTTPS and the redirect only produced a startup warning.
- **Removed from version control:** build output and IDE files. There was no `.gitignore`, and more
  than half of the tracked files were generated.
- **Changes to the repository interface.** I removed `Update`, because nothing edits a todo. I
  replaced `ToggleTodo` with `SetComplete`, so that repeating a request is safe and a stale tab cannot
  undo a change. A missing id now gives `null` or `false`, because that is an expected outcome and not
  an exception.
- **An immutable `Todo`.** It was a class with public setters. Now it creates itself through
  `Todo.Create` and changes through `WithCompletion`, so its rules cannot be bypassed.
- **Added:** a test project and a Dockerfile.

## Architecture

```text
frontend/        React app: UI and calls to the API, plus end-to-end tests
Todo/            ASP.NET Core: HTTP endpoints, request and response types, composition root
Infrastructure/  In-memory implementation of the repository
Core/            Domain model and repository interfaces, with no dependencies
Tests/           Domain and repository tests, and API tests over HTTP
```

The backend is a layered architecture in the onion style. The dependencies point inwards: `Todo` and
`Infrastructure` depend on `Core`, and `Core` depends on nothing. `Todo` references `Infrastructure`
only to choose the implementation in `Program.cs`, which is its composition root.

The controller does not know how todos are stored, but moving to a database would take more than a
new class. `ITodoRepository` would have to become asynchronous, the repository's lifetime would change
from singleton to scoped, and the schema would need migrations.

| Endpoint                         | Result                                                  |
| -------------------------------- | ------------------------------------------------------- |
| `GET /api/todos`                 | `200` with all todos, oldest first                      |
| `GET /api/todos/{id}`            | `200`, `400` if the id is malformed, or `404`           |
| `POST /api/todos`                | `201` with the new todo, or `400` if invalid            |
| `PUT /api/todos/{id}/completion` | `200` with the updated todo, `400` if invalid, or `404` |
| `DELETE /api/todos/{id}`         | `204`, `400` if the id is malformed, or `404`           |
| `GET /openapi/v1.json`           | `200` with the OpenAPI document                         |
| `GET /healthz`                   | `200` when the application is healthy                   |

Errors are returned as RFC 9457 problem details.

## Design decisions

- **Repository pattern and dependency injection.** The controller depends on an interface, which
  gives dependency inversion and makes the storage replaceable.
- **No service layer.** The rules are in `Todo`: what makes a valid title, and that `CompletedAt`
  follows `IsComplete`. A service would only forward calls between the controller and the repository.
- **An immutable domain model, and a lock around the list.** The repository is a singleton shared
  between concurrent requests. The lock protects the list, and immutable todos can safely be handed
  out. A list keeps creation order, and the linear lookups don't matter at this size.
- **Time is passed in.** The controller reads a `TimeProvider` and hands the time to `Todo`, so the
  rules are deterministic and tests need neither a clock nor sleeping.
- **An explicit completion state rather than a toggle.** The request states the wanted result, so it
  is idempotent, and a rollback in the UI knows which value to restore. It is a `PUT` to a
  `/completion` sub-resource rather than a `PATCH`, because the state is replaced whole and that
  makes the idempotence part of the contract.
- **Separate API types.** `CreateTodoRequest` and `TodoResponse` keep the JSON contract independent
  of the domain model and mean a client cannot set fields such as `Id`.
- **One origin.** ASP.NET Core serves the built frontend, so there is one thing to deploy and no CORS
  to configure.
- **TanStack Query for server state.** It handles caching, refetching and loading and error states,
  so the app needs no global state library. TanStack Router and Start were left out: there is one
  page and no need for server rendering.
- **Optimistic completion, confirmed delete.** A checkbox should respond at once and can simply roll
  back on failure. A deleted row waits for the server, so it still exists if an error has to be shown.
  Only the last change to finish refetches the list, and focus and reconnect do not refetch during a
  change, so they don't undo each other. Requests are also tried while offline, so that they fail
  visibly rather than wait in silence.
- **Tests at the boundaries.** The API tests go through the real ASP.NET Core pipeline, and the
  end-to-end tests drive a real browser against the real backend. The end-to-end failure tests
  intercept network requests on purpose, to produce failures on demand. React components have no unit
  tests, because the end-to-end tests cover their behaviour and they hold little logic of their own.
  Tests wait for signals from the application, such as `aria-busy` on the list, rather than for fixed
  durations.
- **Caching follows the file names.** Vite hashes the names of the files under `/assets`, so they can
  be cached forever. `index.html` is checked again each time, and responses are compressed.

## Known limitations and what I would do next

- **The title limit is defined twice,** in the API and in the frontend. Generating the client from the
  OpenAPI document would remove the duplication.
- **Conflicting changes are last write wins.** Two tabs can set opposite states, and the last request
  decides. With users sharing todos, a version check with ETags would make such a conflict visible.
- **Real persistence** with EF Core and PostgreSQL, including the interface changes described under
  Architecture. Atomic updates in the database would replace the in-memory lock.
- **Users and authentication,** since all todos are currently shared.
- **CI** that runs all the tests and builds the image on every push.
