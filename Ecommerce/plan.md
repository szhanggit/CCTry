# E-Commerce Website — Project Plan

## 1. Project Overview

A simplified e-commerce website built with ASP.NET Core MVC. No database; all data lives in-memory. The site allows users to browse products, manage a shopping cart, and complete a checkout flow.

---

## 2. Functional Requirements

### 2.1 Product Catalog
- Display all products on a listing page with image, name, price, and category filter
- Search products by keyword (name or description)
- Filter products by category
- View a product detail page with full description, price, stock status, and "Add to Cart" button

### 2.2 Shopping Cart
- Add a product to the cart (with quantity selection)
- View cart with line items: product name, unit price, quantity, subtotal
- Update quantity of any line item
- Remove a line item from the cart
- Display running cart total and item count in the navigation bar
- Cart persists for the duration of the browser session

### 2.3 Checkout
- Single-page checkout form collecting:
  - Customer name, email, shipping address
  - Payment method selection (Credit Card / PayPal — UI only, no real processing)
- Order summary panel shown alongside the form
- Form validation (client-side via jQuery Validation + server-side ModelState)
- On successful submission, clear the cart and redirect to an order confirmation page

### 2.4 Order Confirmation
- Display a generated order number, customer details, items ordered, and total
- Provide a "Continue Shopping" link back to the catalog

### 2.5 Navigation & Layout
- Persistent top navigation bar with: logo, category links, search box, cart icon with item count badge
- Responsive layout (mobile → desktop) via Bootstrap grid
- Footer with placeholder links

---

## 3. Non-Functional Requirements

- No database — all product data is seeded in-memory at startup; cart state is stored in ASP.NET Core Session
- No user authentication
- No real payment processing
- Runs entirely on a single ASP.NET Core process (no external services)

---

## 4. Technical Stack

| Concern          | Technology                              |
| ---------------- | --------------------------------------- |
| Framework        | ASP.NET Core 8 MVC                      |
| Language         | C# 12                                   |
| UI               | Razor Views, Bootstrap 5, jQuery 3      |
| Client Validation| jQuery Validation + Unobtrusive         |
| Session / State  | ASP.NET Core Session (cookie-backed)    |
| DI Container     | Built-in `Microsoft.Extensions.DI`      |
| JSON Seed Data   | Static `products.json` file read at startup |

---

## 5. Solution Structure

```
Ecommerce/
├── Ecommerce.sln
├── src/
│   └── Ecommerce.Web/                  ← single MVC project
│       ├── Controllers/
│       │   ├── HomeController.cs        ← landing page
│       │   ├── ProductController.cs     ← catalog + detail
│       │   ├── CartController.cs        ← cart CRUD (AJAX + full-page)
│       │   └── OrderController.cs       ← checkout + confirmation
│       ├── Models/
│       │   ├── Product.cs
│       │   ├── Category.cs
│       │   ├── CartItem.cs
│       │   ├── Cart.cs
│       │   ├── Order.cs
│       │   └── CheckoutViewModel.cs
│       ├── Services/
│       │   ├── IProductService.cs
│       │   ├── InMemoryProductService.cs
│       │   ├── ICartService.cs
│       │   ├── SessionCartService.cs
│       │   ├── IOrderService.cs
│       │   └── InMemoryOrderService.cs
│       ├── Data/
│       │   └── products.json            ← seed data (no DB)
│       ├── Views/
│       │   ├── Shared/
│       │   │   ├── _Layout.cshtml       ← nav bar, footer
│       │   │   └── _CartBadge.cshtml    ← partial: item count
│       │   ├── Home/
│       │   │   └── Index.cshtml         ← hero banner + featured products
│       │   ├── Product/
│       │   │   ├── Index.cshtml         ← product listing + filters
│       │   │   └── Detail.cshtml        ← product detail page
│       │   ├── Cart/
│       │   │   └── Index.cshtml         ← cart page
│       │   └── Order/
│       │       ├── Checkout.cshtml      ← checkout form + summary
│       │       └── Confirmation.cshtml  ← order confirmation
│       ├── wwwroot/
│       │   ├── css/site.css
│       │   └── js/site.js               ← jQuery interactions
│       └── Program.cs
└── tests/
    └── Ecommerce.Tests/
        ├── Services/
        │   ├── InMemoryProductServiceTests.cs
        │   └── SessionCartServiceTests.cs
        └── Controllers/
            └── CartControllerTests.cs
```

---

## 6. Domain Models

### Product
| Field       | Type     | Notes                        |
| ----------- | -------- | ---------------------------- |
| Id          | int      | Unique identifier            |
| Name        | string   | Display name                 |
| Description | string   | Full description             |
| Price       | decimal  | Unit price                   |
| ImageUrl    | string   | Relative path under wwwroot  |
| Category    | string   | Used for filtering           |
| Stock       | int      | 0 = out of stock             |

### CartItem
| Field     | Type    | Notes                  |
| --------- | ------- | ---------------------- |
| ProductId | int     |                        |
| Name      | string  | Snapshot at add time   |
| Price     | decimal | Snapshot at add time   |
| Quantity  | int     |                        |

### Cart
- Collection of `CartItem`
- Computed properties: `TotalItems`, `TotalPrice`
- Serialized to/from JSON and stored in `ISession`

### Order
| Field           | Type          | Notes                      |
| --------------- | ------------- | -------------------------- |
| OrderNumber     | string        | Generated GUID prefix      |
| PlacedAt        | DateTime      |                            |
| CustomerName    | string        |                            |
| CustomerEmail   | string        |                            |
| ShippingAddress | string        |                            |
| PaymentMethod   | string        |                            |
| Items           | List<CartItem>|                            |
| Total           | decimal       |                            |

### CheckoutViewModel
- Customer fields (Name, Email, Address, City, PostCode)
- PaymentMethod (enum: CreditCard, PayPal)
- ReadOnly `Cart` for the order summary panel

---

## 7. Service Layer

### IProductService / InMemoryProductService
- Loads products from `products.json` once at startup (singleton lifetime)
- `GetAll()` — returns all products
- `GetById(int id)` — single product or null
- `Search(string keyword, string? category)` — filtered list

### ICartService / SessionCartService
- Scoped lifetime; reads/writes `Cart` JSON from `ISession`
- `GetCart()` — deserializes current session cart
- `AddItem(Product, int quantity)` — adds or increments
- `UpdateQuantity(int productId, int quantity)` — sets quantity; removes if 0
- `RemoveItem(int productId)`
- `Clear()`

### IOrderService / InMemoryOrderService
- Singleton lifetime; stores orders in a `List<Order>` in memory
- `PlaceOrder(CheckoutViewModel, Cart)` → returns `Order`
- `GetOrder(string orderNumber)` → used on confirmation page

---

## 8. Controller Responsibilities

### HomeController
- `GET /` — renders hero banner and a sample of featured products (first 4)

### ProductController
- `GET /products` — listing page; accepts `keyword` and `category` query params
- `GET /products/{id}` — detail page

### CartController
- `GET /cart` — full cart page
- `POST /cart/add` — AJAX; returns JSON `{ success, totalItems }`
- `POST /cart/update` — AJAX; updates quantity, returns JSON `{ success, subtotal, total }`
- `POST /cart/remove` — AJAX; removes item, returns JSON `{ success, total, totalItems }`

### OrderController
- `GET /order/checkout` — checkout form (redirects to catalog if cart empty)
- `POST /order/checkout` — validates form, places order, clears cart, redirects to confirmation
- `GET /order/confirmation/{orderNumber}` — confirmation page

---

## 9. Client-Side Behaviour (jQuery)

| Interaction            | Mechanism                                      |
| ---------------------- | ---------------------------------------------- |
| Add to Cart            | AJAX POST → update nav badge without page reload |
| Update cart quantity   | Debounced input change → AJAX POST → update subtotal + total |
| Remove cart item       | AJAX POST → fade out row → update total        |
| Category filter        | Form GET submit (no JS required, graceful)     |
| Checkout form validate | jQuery Validation + Unobtrusive (client-side)  |
| Payment method toggle  | Show/hide placeholder card fields on select    |

---

## 10. Routing Summary

| Method | URL                            | Action                          |
| ------ | ------------------------------ | ------------------------------- |
| GET    | `/`                            | Home → featured products        |
| GET    | `/products`                    | Product listing                 |
| GET    | `/products/{id}`               | Product detail                  |
| GET    | `/cart`                        | Cart page                       |
| POST   | `/cart/add`                    | Add to cart (AJAX)              |
| POST   | `/cart/update`                 | Update quantity (AJAX)          |
| POST   | `/cart/remove`                 | Remove item (AJAX)              |
| GET    | `/order/checkout`              | Checkout form                   |
| POST   | `/order/checkout`              | Submit order                    |
| GET    | `/order/confirmation/{number}` | Order confirmation              |

---

## 11. Seed Data

`products.json` will contain ~12–16 products across 4 categories (Electronics, Clothing, Books, Home & Kitchen) with placeholder image URLs, prices, descriptions, and stock levels.

---

## 12. DI Registration (Program.cs)

```
AddSession()                          ← session middleware
AddSingleton<IProductService, InMemoryProductService>
AddSingleton<IOrderService, InMemoryOrderService>
AddScoped<ICartService, SessionCartService>
```

---

## 13. Testing Plan

### Unit Tests (`Ecommerce.Tests`)
- `InMemoryProductServiceTests` — GetAll, GetById, Search by keyword, Search by category
- `SessionCartServiceTests` — AddItem, UpdateQuantity (set to 0 removes), RemoveItem, TotalPrice calculation
- `CartControllerTests` — AJAX actions return correct JSON shape; redirect on empty cart

### Manual Test Checklist
- [ ] Browse catalog, filter by category, search by keyword
- [ ] Add multiple products; verify nav badge count
- [ ] Update and remove items in cart; verify totals update without full reload
- [ ] Submit checkout with invalid data; verify validation messages appear
- [ ] Submit valid checkout; verify order confirmation page and cart is cleared
- [ ] Confirm cart is empty after placing an order
