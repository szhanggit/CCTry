# Todo List Application — Design Document

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET Core (latest LTS), C#, ASP.NET Core MVC |
| Frontend | HTML, Bootstrap 5, jQuery |
| Storage | In-memory only (no database, no persistence) |
| DI Container | ASP.NET Core built-in DI (`Microsoft.Extensions.DependencyInjection`) |
| Unit Testing | xUnit, Moq, FluentAssertions |

---

## Project Structure

The solution contains two projects: the main web application and a dedicated test project.

```
TodoList.sln
├── TodoList/                          (web application)
│   ├── TodoList.csproj
│   ├── Program.cs
│   ├── Models/
│   │   └── TodoItem.cs
│   ├── Services/
│   │   ├── ITodoService.cs
│   │   └── TodoService.cs
│   ├── Controllers/
│   │   └── TodoController.cs
│   ├── ViewModels/
│   │   ├── TodoListViewModel.cs
│   │   └── TodoFormViewModel.cs
│   ├── Views/
│   │   ├── Shared/
│   │   │   └── _Layout.cshtml
│   │   ├── Todo/
│   │   │   ├── Index.cshtml
│   │   │   └── _TodoItem.cshtml   (partial)
│   │   └── _ViewImports.cshtml
│   └── wwwroot/
│       ├── css/
│       │   └── site.css
│       └── js/
│           └── site.js
└── TodoList.Tests/                    (xUnit test project)
    ├── TodoList.Tests.csproj
    ├── Services/
    │   └── TodoServiceTests.cs
    ├── Controllers/
    │   └── TodoControllerTests.cs
    └── ViewModels/
        └── TodoFormViewModelValidationTests.cs
```

**Namespace map:**

| Folder | Namespace |
|---|---|
| `Models/` | `TodoList.Models` |
| `Services/` | `TodoList.Services` |
| `Controllers/` | `TodoList.Controllers` |
| `ViewModels/` | `TodoList.ViewModels` |
| `Tests/Services/` | `TodoList.Tests.Services` |
| `Tests/Controllers/` | `TodoList.Tests.Controllers` |
| `Tests/ViewModels/` | `TodoList.Tests.ViewModels` |

`ITodoService` / `TodoService` registered as `AddSingleton` in `Program.cs` so the single in-memory collection lives for the application lifetime.

---

## Data Model — `TodoItem`

**File:** `Models/TodoItem.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | Auto-incremented by service; immutable after creation |
| `Title` | `string` | Required; max 200 chars |
| `Description` | `string?` | Optional; nullable |
| `DueDate` | `DateTime?` | Optional; stored as UTC |
| `IsCompleted` | `bool` | Defaults to `false` on creation |
| `CreatedAt` | `DateTime` | Set at creation; UTC; immutable |
| `UpdatedAt` | `DateTime` | Updated on every mutation |

Validation lives on ViewModels, not on the model class itself.

---

## In-Memory Service — `ITodoService` / `TodoService`

**Files:** `Services/ITodoService.cs`, `Services/TodoService.cs`

### Interface Methods

| Signature | Description |
|---|---|
| `IReadOnlyList<TodoItem> GetAll()` | Returns a defensive copy of all items |
| `IReadOnlyList<TodoItem> GetByStatus(bool? isCompleted)` | `null` = all; `true` = completed; `false` = active |
| `TodoItem? GetById(int id)` | Returns single item or null |
| `TodoItem Add(string title, string? description, DateTime? dueDate)` | Creates item, assigns Id, sets timestamps |
| `TodoItem? Update(int id, string title, string? description, DateTime? dueDate, bool isCompleted)` | Full field replacement; returns null if not found |
| `bool Delete(int id)` | Returns false if not found |
| `bool ToggleComplete(int id)` | Flips `IsCompleted`; returns false if not found |
| `int CountActive()` | Count of items where `IsCompleted == false` |

### Thread Safety Design

`TodoService` holds:
- `private readonly List<TodoItem> _items = new();`
- `private int _nextId = 1;`
- `private readonly ReaderWriterLockSlim _lock = new();`

Read operations acquire `EnterReadLock` / `ExitReadLock`. Write operations (Add, Update, Delete, Toggle) acquire `EnterWriteLock` / `ExitWriteLock`. `GetAll` returns `_items.ToList()` inside the read lock (defensive copy). Id assigned via `Interlocked.Increment` inside the write lock.

---

## ViewModels

**File:** `ViewModels/TodoListViewModel.cs`

| Property | Type | Notes |
|---|---|---|
| `Items` | `IReadOnlyList<TodoItem>` | Filtered list for current request |
| `Filter` | `string` | Current filter: `"all"`, `"active"`, or `"completed"` |
| `ActiveCount` | `int` | Global count of incomplete items (not filtered) |

---

**File:** `ViewModels/TodoFormViewModel.cs` — used as JSON request body for Create and Edit

| Property | Type | Validation |
|---|---|---|
| `Title` | `string` | `[Required]`, `[MaxLength(200)]` |
| `Description` | `string?` | Optional |
| `DueDate` | `DateTime?` | Optional; ISO 8601 string |
| `IsCompleted` | `bool` | Used on Edit only; ignored on Create |

---

## Dependency Injection

### Container

ASP.NET Core's built-in DI container (`IServiceCollection`) is used exclusively — no third-party container needed.

### Service Registrations (`Program.cs`)

| Registration | Lifetime | Reason |
|---|---|---|
| `services.AddControllersWithViews()` | — | Registers MVC controllers and views; auto-wires constructor injection for all controllers |
| `services.AddSingleton<ITodoService, TodoService>()` | Singleton | The in-memory list must be shared across all requests; a scoped or transient registration would create a new empty list per request, losing all data |
| `ILogger<T>` | Singleton (framework) | Registered automatically by `WebApplication.CreateBuilder` via `AddLogging()`; no explicit call needed |

### Constructor Injection Map

**`TodoService`**

```
TodoService(ILogger<TodoService> logger)
```

| Parameter | Type | Source |
|---|---|---|
| `logger` | `ILogger<TodoService>` | Injected by DI container automatically |

The logger is used to record operations: item added/updated/deleted, lock contention events.

---

**`TodoController`**

```
TodoController(ITodoService todoService, ILogger<TodoController> logger)
```

| Parameter | Type | Source |
|---|---|---|
| `todoService` | `ITodoService` | Resolved to the singleton `TodoService` |
| `logger` | `ILogger<TodoController>` | Injected by DI container automatically |

Both fields are stored as `private readonly` backing fields. No property injection or service locator (`HttpContext.RequestServices`) is used anywhere in the codebase.

### DI Rules Applied Across the Codebase

- **Program.cs is the composition root** — all registrations happen here; no class calls `new` on a dependency it needs.
- **Depend on abstractions** — `TodoController` depends on `ITodoService`, not `TodoService`. This is the seam that allows Moq to inject a fake in tests.
- **No static service access** — logging and services are always received through constructors, never retrieved via static helpers.
- **Singleton safety** — `TodoService` is safe to use as a singleton because its internal state is protected by `ReaderWriterLockSlim` (see Thread Safety Design).

---

## Controller — `TodoController`

**File:** `Controllers/TodoController.cs`

Base route: `/Todo` — Index also reachable at `/` via default MVC route.

### Route Table

| Action | Verb | URL | Returns | Purpose |
|---|---|---|---|---|
| `Index` | GET | `/Todo?filter=all\|active\|completed` | `View(TodoListViewModel)` | Main page; full page render |
| `Create` | POST | `/Todo/Create` | JSON | Add new todo via AJAX |
| `Edit` | GET | `/Todo/Edit/{id}` | JSON | Fetch item data to populate modal |
| `Edit` | POST | `/Todo/Edit/{id}` | JSON | Save edits via AJAX |
| `Delete` | POST | `/Todo/Delete/{id}` | JSON | Delete item via AJAX |
| `Toggle` | POST | `/Todo/Toggle/{id}` | JSON | Toggle complete/incomplete via AJAX |

All POST endpoints are decorated with `[ValidateAntiForgeryToken]`. The CSRF token is injected once in `_Layout.cshtml` and set globally via `$.ajaxSetup` in `site.js`.

### Action Descriptions

**`Index(string filter = "all")`** — translates filter string to `bool?` for `GetByStatus`, fetches `CountActive`, builds `TodoListViewModel`, returns `View(viewModel)`.

**`Create [POST]`** — accepts `[FromBody] TodoFormViewModel`; validates `ModelState`; on failure returns error JSON; on success calls `_service.Add(...)` and returns item JSON.

**`Edit [GET] /Todo/Edit/{id}`** — calls `GetById`; returns item JSON or `{ success: false }` if not found.

**`Edit [POST] /Todo/Edit/{id}`** — accepts `[FromBody] TodoFormViewModel`; validates `ModelState`; calls `_service.Update(...)` and returns updated item JSON.

**`Delete [POST] /Todo/Delete/{id}`** — calls `_service.Delete(id)`; returns `{ success: true }` or `{ success: false, message: "Item not found" }`.

**`Toggle [POST] /Todo/Toggle/{id}`** — calls `_service.ToggleComplete(id)`; returns `{ success: true, isCompleted: <new state> }` or `{ success: false }`.

---

## JSON API Contract

All JSON endpoints return `Content-Type: application/json`. All POST endpoints require the header `RequestVerificationToken: <token>`.

### POST /Todo/Create

**Request body:**
```json
{
  "title": "string (required, max 200)",
  "description": "string | null",
  "dueDate": "ISO 8601 date string | null"
}
```

**Success (HTTP 200):**
```json
{
  "success": true,
  "item": {
    "id": 1,
    "title": "string",
    "description": "string | null",
    "dueDate": "ISO 8601 | null",
    "isCompleted": false,
    "createdAt": "ISO 8601"
  }
}
```

**Failure (HTTP 200):**
```json
{ "success": false, "errors": ["Title is required"] }
```

---

### GET /Todo/Edit/{id}

**Success:**
```json
{
  "success": true,
  "item": { "id": 1, "title": "string", "description": "string | null", "dueDate": "ISO 8601 | null", "isCompleted": false }
}
```

**Not found:**
```json
{ "success": false }
```

---

### POST /Todo/Edit/{id}

**Request body:**
```json
{
  "title": "string (required, max 200)",
  "description": "string | null",
  "dueDate": "ISO 8601 | null",
  "isCompleted": false
}
```

**Response:** Same shape as `POST /Todo/Create`.

---

### POST /Todo/Delete/{id}

**Request body:** Empty.

**Success:** `{ "success": true }`

**Not found:** `{ "success": false, "message": "Item not found" }`

---

### POST /Todo/Toggle/{id}

**Request body:** Empty.

**Success:** `{ "success": true, "isCompleted": true }`

**Not found:** `{ "success": false }`

---

## View Design

### `Views/Shared/_Layout.cshtml`

- Bootstrap 5 CSS + Bootstrap Icons loaded via CDN in `<head>`
- jQuery + Bootstrap 5 JS bundle loaded via CDN at end of `<body>`
- Navbar: app title "My Todo List" on the left; active-item count badge on the right
- `@RenderBody()` wrapped in `<div class="container py-4">`
- `@RenderSection("Scripts", required: false)` just before `</body>`

### `Views/Todo/Index.cshtml`

Page structure (top to bottom):

1. **Heading row** — `<h1>Todo List</h1>` + `<span class="badge ...">N items left</span>`
2. **Filter bar** — Bootstrap `nav nav-pills`: All / Active / Completed (anchor links `?filter=...`; active class set server-side)
3. **Add button** — `btn btn-primary` with Bootstrap Icon `bi-plus-lg`; triggers Add/Edit modal
4. **Todo list** — `<ul class="list-group">` with `@Html.Partial("_TodoItem", item)` per item
5. **Empty state** — `text-center text-muted py-5` paragraph; shown when list is empty
6. **Add/Edit modal** — single Bootstrap modal reused for both Add and Edit; JS sets `data-mode` and `data-id`
7. **Delete confirm modal** — smaller modal with Cancel + Delete buttons

### `Views/Todo/_TodoItem.cshtml` (partial)

One `list-group-item d-flex align-items-center` per `TodoItem`. Root element has `data-id="{item.Id}"`.

| Zone | Content |
|---|---|
| Left | `<input type="checkbox" class="toggle-complete">` — checked state reflects `IsCompleted` |
| Center | Title `<span>` (has `.todo-completed` class if done), description `<small class="text-muted">`, due date `<span class="badge ...">` |
| Right | Edit `<button class="btn btn-sm btn-outline-secondary btn-edit">`, Delete `<button class="btn btn-sm btn-outline-danger btn-delete">` |

---

## Frontend Interaction Design

### Full Page Reload vs AJAX

| User Action | Mechanism | Reason |
|---|---|---|
| Change filter tab | Full page reload (anchor `<a href="?filter=...">`) | Filter state lives in URL; bookmarkable |
| Add new todo | AJAX POST to `/Todo/Create` | No reload; new item injected into DOM |
| Edit todo | AJAX POST to `/Todo/Edit/{id}` | Updates item in place |
| Delete todo | AJAX POST to `/Todo/Delete/{id}` | Removes item from DOM without reload |
| Toggle complete | AJAX POST to `/Todo/Toggle/{id}` | Instant checkbox feedback; updates badge |

### DOM Update Strategy

| Action | DOM Update |
|---|---|
| Add | Prepend new card HTML (built from response JSON via `buildItemHtml`) to list |
| Edit | Find card by `data-id`; update title/description/dueDate/completed class from JSON |
| Delete | Find card by `data-id`; fade out and remove |
| Toggle | Find card by `data-id`; toggle `.todo-completed` class on title; update checkbox; call `updateCounter(±1)` |

### `wwwroot/js/site.js` Structure

Single file, wrapped in `$(document).ready(...)`:

| Section | Responsibility |
|---|---|
| CSRF setup | Read antiforgery token hidden field; set globally via `$.ajaxSetup({ headers: { RequestVerificationToken: token } })` |
| Modal management | `openAddModal()` clears form fields and sets `data-mode="add"`; `openEditModal(id)` calls GET `/Todo/Edit/{id}` then populates form and sets `data-mode="edit"` and `data-id` |
| Form submit handler | Single handler on `#todo-form submit`; reads `data-mode` / `data-id` from modal; dispatches to correct endpoint |
| Toggle handler | Delegated click on `.toggle-complete`; fires POST `/Todo/Toggle/{id}` |
| Delete handler | Delegated click on `.btn-delete`; shows confirm modal; on confirm fires POST `/Todo/Delete/{id}` |
| DOM helpers | `buildItemHtml(item)` — creates card HTML from JSON; `updateItem(item)` — patches existing card; `removeItem(id)` — removes card; `updateCounter(delta)` — increments/decrements badge |
| Error display | `showFormErrors(errors)` — renders validation messages inside modal `alert` div |

---

## Bootstrap Component Map

| UI Element | Bootstrap Component |
|---|---|
| Filter tabs | `nav nav-pills` |
| Todo list container | `list-group` |
| Individual todo item | `list-group-item d-flex align-items-center` |
| Active item count | `badge bg-primary rounded-pill` |
| Due date (future) | `badge bg-warning text-dark` |
| Due date (overdue) | `badge bg-danger text-white` |
| Add/Edit form | `modal modal-dialog modal-dialog-centered` |
| Delete confirmation | Second `modal` (smaller) |
| Add button | `btn btn-primary` + `bi-plus-lg` icon |
| Edit button | `btn btn-sm btn-outline-secondary` + `bi-pencil` icon |
| Delete button | `btn btn-sm btn-outline-danger` + `bi-trash` icon |
| Completed title style | Custom CSS `.todo-completed` → `text-decoration: line-through; color: #aaa` |
| Empty state | `text-center text-muted py-5` |
| Form validation errors | `alert alert-danger` inside modal; shown/hidden by JS |
| Loading spinner | `spinner-border spinner-border-sm` on action buttons while AJAX is in flight |

---

## Unit Test Project — `TodoList.Tests`

### Project Setup

**File:** `TodoList.Tests/TodoList.Tests.csproj`

| Package | Purpose |
|---|---|
| `Microsoft.NET.Test.Sdk` | Test runner host |
| `xunit` | Test framework |
| `xunit.runner.visualstudio` | VS / `dotnet test` integration |
| `Moq` | Mock `ITodoService` and `ILogger<T>` in controller tests |
| `FluentAssertions` | Readable assertion syntax (`result.Should().Be(...)`) |
| `coverlet.collector` | Code coverage collection |

The project also carries a `<ProjectReference>` to `../TodoList/TodoList.csproj`.

Run all tests with: `dotnet test`

---

### `Services/TodoServiceTests.cs`

Tests the concrete `TodoService` directly — no mocks. Each test creates a fresh `TodoService` instance (constructor receives a no-op `ILogger` from `Microsoft.Extensions.Logging.Abstractions.NullLogger<TodoService>.Instance`).

#### `GetAll()`

| Test | Scenario | Assert |
|---|---|---|
| `GetAll_WhenEmpty_ReturnsEmptyList` | No items added | Returns empty `IReadOnlyList<TodoItem>` |
| `GetAll_WhenItemsExist_ReturnsAllItems` | Two items added | Returns list with count 2 |
| `GetAll_ReturnsDefensiveCopy` | Mutate the returned list | Internal list is unaffected |

#### `GetByStatus(bool? isCompleted)`

| Test | Scenario | Assert |
|---|---|---|
| `GetByStatus_Null_ReturnsAll` | Two active, one completed | Returns all 3 items |
| `GetByStatus_False_ReturnsOnlyActive` | Two active, one completed | Returns 2 active items |
| `GetByStatus_True_ReturnsOnlyCompleted` | Two active, one completed | Returns 1 completed item |
| `GetByStatus_NoMatch_ReturnsEmpty` | All active, filter=completed | Returns empty list |

#### `GetById(int id)`

| Test | Scenario | Assert |
|---|---|---|
| `GetById_ExistingId_ReturnsItem` | Item added, query its Id | Returns item with matching Id |
| `GetById_NonExistentId_ReturnsNull` | Query id=999 | Returns null |

#### `Add(...)`

| Test | Scenario | Assert |
|---|---|---|
| `Add_ReturnsItemWithAssignedId` | Add one item | Returned item has Id > 0 |
| `Add_IdIsAutoIncremented` | Add two items | Second Id > First Id |
| `Add_IsCompletedDefaultsFalse` | Add item | `IsCompleted == false` |
| `Add_SetsCreatedAt` | Add item | `CreatedAt` is not `default(DateTime)` |
| `Add_UpdatedAtEqualsCreatedAt` | Add item | `UpdatedAt == CreatedAt` |
| `Add_ItemAppearsInGetAll` | Add item, call GetAll | List contains new item |
| `Add_NullableFieldsAcceptNull` | Pass null description and dueDate | Returns item with null fields |

#### `Update(int id, ...)`

| Test | Scenario | Assert |
|---|---|---|
| `Update_ExistingId_ReturnsUpdatedItem` | Add then update | Returned item has new Title/Description/DueDate |
| `Update_NonExistentId_ReturnsNull` | Update id=999 | Returns null |
| `Update_DoesNotChangeCreatedAt` | Add then update | `CreatedAt` unchanged |
| `Update_DoesNotChangeId` | Add then update | `Id` unchanged |
| `Update_UpdatedAtIsRefreshed` | Add; wait; update | `UpdatedAt >= CreatedAt` |

#### `Delete(int id)`

| Test | Scenario | Assert |
|---|---|---|
| `Delete_ExistingId_ReturnsTrue` | Add then delete | Returns `true` |
| `Delete_NonExistentId_ReturnsFalse` | Delete id=999 | Returns `false` |
| `Delete_ItemRemovedFromGetAll` | Add, delete, GetAll | List does not contain deleted item |

#### `ToggleComplete(int id)`

| Test | Scenario | Assert |
|---|---|---|
| `ToggleComplete_FalseToTrue_ReturnsTrue` | Add (IsCompleted=false), toggle | Returns `true`; item.IsCompleted is `true` |
| `ToggleComplete_TrueToFalse_Toggles` | Add, toggle twice | Item.IsCompleted back to `false` |
| `ToggleComplete_NonExistentId_ReturnsFalse` | Toggle id=999 | Returns `false` |
| `ToggleComplete_UpdatesUpdatedAt` | Add; toggle | `UpdatedAt >= CreatedAt` |

#### `CountActive()`

| Test | Scenario | Assert |
|---|---|---|
| `CountActive_WhenEmpty_ReturnsZero` | No items | Returns 0 |
| `CountActive_ReturnsOnlyActiveCount` | Two active, one completed | Returns 2 |
| `CountActive_DecreasesAfterToggleToComplete` | Add active, toggle | Returns count - 1 |
| `CountActive_IncreasesAfterToggleToActive` | Add completed, toggle | Returns count + 1 |

---

### `Controllers/TodoControllerTests.cs`

Tests `TodoController` in isolation. Each test creates a `Mock<ITodoService>` and a `Mock<ILogger<TodoController>>`, constructs the controller directly (`new TodoController(mockService.Object, mockLogger.Object)`), and asserts on the returned `IActionResult`.

To simulate invalid `ModelState`, call `controller.ModelState.AddModelError("Title", "Required")` before invoking the action.

To read JSON results, cast `result` to `JsonResult` and access `.Value`, then use `FluentAssertions`'s `BeEquivalentTo` or reflection on the anonymous type.

#### `Index(string filter)`

| Test | Scenario | Assert |
|---|---|---|
| `Index_FilterAll_CallsGetByStatusWithNull` | filter="all" | `GetByStatus(null)` called once |
| `Index_FilterActive_CallsGetByStatusFalse` | filter="active" | `GetByStatus(false)` called once |
| `Index_FilterCompleted_CallsGetByStatusTrue` | filter="completed" | `GetByStatus(true)` called once |
| `Index_AlwaysCallsCountActive` | Any filter | `CountActive()` called once |
| `Index_ReturnsViewResult` | Any filter | Result is `ViewResult` |
| `Index_ViewModelItemsMatchService` | Service returns 2 items | `ViewModel.Items.Count == 2` |
| `Index_ViewModelFilterMatchesInput` | filter="active" | `ViewModel.Filter == "active"` |
| `Index_ViewModelActiveCountMatchesService` | Service returns 3 | `ViewModel.ActiveCount == 3` |

#### `Create [POST]`

| Test | Scenario | Assert |
|---|---|---|
| `Create_InvalidModel_ReturnsSuccessFalse` | ModelState has error | JSON `success == false`; `errors` non-empty |
| `Create_InvalidModel_DoesNotCallService` | ModelState has error | `Add` never called |
| `Create_ValidModel_CallsServiceAdd` | Valid model | `Add` called once with correct args |
| `Create_ValidModel_ReturnsSuccessTrue` | Valid model | JSON `success == true` |
| `Create_ValidModel_ReturnsItemFromService` | Service returns item with Id=5 | JSON `item.id == 5` |

#### `Edit [GET]`

| Test | Scenario | Assert |
|---|---|---|
| `EditGet_ExistingId_ReturnsSuccessTrue` | Service returns item | JSON `success == true` and `item` present |
| `EditGet_NonExistentId_ReturnsSuccessFalse` | Service returns null | JSON `success == false` |

#### `Edit [POST]`

| Test | Scenario | Assert |
|---|---|---|
| `EditPost_InvalidModel_ReturnsSuccessFalse` | ModelState has error | JSON `success == false` |
| `EditPost_InvalidModel_DoesNotCallService` | ModelState has error | `Update` never called |
| `EditPost_ValidModel_CallsServiceUpdate` | Valid model | `Update` called once with id and correct args |
| `EditPost_ValidModel_ReturnsSuccessTrue` | Service returns updated item | JSON `success == true` |
| `EditPost_NotFound_ReturnsSuccessFalse` | Service returns null | JSON `success == false` |

#### `Delete [POST]`

| Test | Scenario | Assert |
|---|---|---|
| `Delete_ExistingId_ReturnsSuccessTrue` | Service returns true | JSON `success == true` |
| `Delete_NonExistentId_ReturnsSuccessFalse` | Service returns false | JSON `success == false` and `message` present |

#### `Toggle [POST]`

| Test | Scenario | Assert |
|---|---|---|
| `Toggle_ExistingId_ReturnsSuccessTrueAndNewState` | Service returns true; item now completed | JSON `success == true`, `isCompleted == true` |
| `Toggle_NonExistentId_ReturnsSuccessFalse` | Service returns false | JSON `success == false` |

---

### `ViewModels/TodoFormViewModelValidationTests.cs`

Tests Data Annotation validation on `TodoFormViewModel` using `Validator.TryValidateObject` from `System.ComponentModel.DataAnnotations`.

| Test | Scenario | Assert |
|---|---|---|
| `Title_Null_FailsValidation` | `Title = null` | Validation result contains error for `Title` |
| `Title_Empty_FailsValidation` | `Title = ""` | Validation result contains error for `Title` |
| `Title_201Chars_FailsValidation` | Title with 201 characters | Validation result contains error for `Title` |
| `Title_200Chars_PassesValidation` | Title with exactly 200 characters | No errors |
| `Title_Valid_PassesValidation` | `Title = "Buy milk"` | No validation errors |
| `Description_Null_PassesValidation` | `Description = null` | No errors |
| `DueDate_Null_PassesValidation` | `DueDate = null` | No errors |

---

### Testing Anti-Patterns Avoided

| Anti-pattern | Why avoided |
|---|---|
| Mocking `TodoService` in service tests | The point of service tests is to verify the real implementation |
| Using the real `TodoService` in controller tests | Controller tests should be isolated from service logic; Moq lets us control exactly what the service returns |
| Sharing state between tests | Each test constructs its own fresh `TodoService` or mock; xUnit creates a new class instance per test by default |
| Testing private methods | Only the public interface (the 8 service methods, 6 controller actions) is tested |

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Single modal reused for Add and Edit | Reduces HTML duplication; JS mode-switches via `data-mode` attribute |
| Filter via query string / full page reload | Bookmarkable state; no client-side routing needed |
| `ReaderWriterLockSlim` in service | Allows concurrent reads; blocks only on writes |
| Defensive copy in `GetAll` | Prevents callers from mutating the internal list outside the lock |
| All AJAX endpoints on `TodoController` | No separate API controller; keeps routing simple at this scale |
| `IReadOnlyList<TodoItem>` return type | Enforces immutability at the interface level without a DTO layer |
| Active count updated via JS delta | Avoids an extra round-trip GET after toggle |
| Bootstrap Icons via CDN | No npm/webpack build pipeline required |
| CSRF on all POST endpoints | ASP.NET Core default protection; token injected once and set globally in `$.ajaxSetup` |
| Built-in DI container | No third-party container needed; ASP.NET Core's container handles all registrations |
| `ITodoService` interface as DI seam | Controller depends on the interface, not the implementation, making it mockable in tests |
| `AddSingleton` for `TodoService` | Singleton lifetime ensures one shared in-memory list; thread safety handled by `ReaderWriterLockSlim` |
| Constructor injection throughout | No service locator or static helpers; dependencies are explicit and testable |
| `NullLogger` in service unit tests | Lets service tests create `TodoService` without a real logging infrastructure |
| Moq for controller tests | Controller tests verify routing logic only; Moq controls what the service returns without coupling to real service behavior |
| xUnit new instance per test | xUnit creates a fresh test class per test method by default, preventing shared mutable state |

---

## Implementation Order (when coding begins)

Follow this sequence to avoid dependency issues:

1. Create solution file (`TodoList.sln`) and both projects (`TodoList`, `TodoList.Tests`)
2. Add project reference from `TodoList.Tests` to `TodoList`
3. `Models/TodoItem.cs`
4. `Services/ITodoService.cs` + `Services/TodoService.cs`
5. Register `ITodoService` as singleton in `Program.cs` (`AddSingleton<ITodoService, TodoService>()`)
6. `ViewModels/TodoListViewModel.cs` + `ViewModels/TodoFormViewModel.cs`
7. `Controllers/TodoController.cs` — constructor accepts `ITodoService` and `ILogger<TodoController>`; Index action first
8. `Views/Shared/_Layout.cshtml` + `Views/_ViewImports.cshtml`
9. `Views/Todo/Index.cshtml` + `Views/Todo/_TodoItem.cshtml`
10. `wwwroot/js/site.js` — wire AJAX (toggle first, then delete, create, edit)
11. `wwwroot/css/site.css` — completed-item styles, empty-state, etc.
12. `TodoList.Tests/Services/TodoServiceTests.cs` — test all 8 service methods
13. `TodoList.Tests/Controllers/TodoControllerTests.cs` — test all 6 controller actions
14. `TodoList.Tests/ViewModels/TodoFormViewModelValidationTests.cs` — test annotation validation
15. Run `dotnet test` — all tests green

---

## Verification Checklist

**Unit tests:**
- `dotnet test` — all tests pass with 0 failures
- `TodoServiceTests` — all 8 service methods covered (28 test cases)
- `TodoControllerTests` — all 6 controller actions covered (22 test cases)
- `TodoFormViewModelValidationTests` — Required and MaxLength annotations verified (7 test cases)

**Runtime:**
- `dotnet run` — app starts on localhost without errors; DI container resolves `ITodoService` and `ILogger<T>`
- Add a todo item — appears in list without page reload
- Toggle complete — strikethrough applied; active counter updates; checkbox reflects state
- Edit item — modal pre-populated with existing values; changes reflected in list
- Delete item — confirm modal shown; item removed from DOM on confirm
- Filter tabs — correct subset shown; URL reflects `?filter=` value
- Empty state — shown when no items match the current filter
- Thread safety — `ReaderWriterLockSlim` in service covers concurrent request scenarios
