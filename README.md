# 🖥️ PC Parts Scrapper


A full-stack web application that scrapes PC component prices from Pakistani online stores, tracks price history, and notifies users when prices drop below their target.
### LINK: https://findpcparts.app/

## ✨ Features

### 🛒 Product Management
- **Accordion-style product list** – products are collapsible, showing only the lowest price initially
- **Expand/Collapse All** – global controls for expanding/collapsing all products
- **Pagination** – browse products in manageable chunks (20 per page)
- **Category filtering** – filter products by category (CPU, GPU, etc.)
- **Best price badge** – highlights the cheapest listing for each product

### 📊 Price History
- **Interactive price chart** – visualize price trends over time
- **Store-specific history** – view price history for individual store listings
- **Scrollable chart** – horizontal scrolling for large datasets
- **Optimized performance** – adaptive chart rendering for large histories
- **Responsive design** – mobile-friendly with touch support

### 🔍 Smart Search
- **Live search** – results appear as you type with debounced requests
- **Search suggestions** – dropdown shows matching products in real-time
- **Keyboard friendly** – press Enter to see all results
- **URL persistence** – search queries are stored in the URL for sharing
- **Instant navigation** – click a result to go directly to the filtered product list

### 🔔 Price Alerts
- **Set target price alerts** – get notified when a product drops to your desired price
- **Real-time notifications** – in-app notification bell with unread count
- **Alert management** – create, update, and delete alerts
- **Persistent alerts** – alerts remain active until triggered or deleted
- **Instant notification** – confirmation when setting an alert

### 🔐 User Authentication
- **JWT-based authentication** – secure login and registration
- **Protected routes** – certain features require authentication
- **Persistent sessions** – stay logged in across browser sessions
- **Role-based access** – admin and user roles (future-ready)

### 🧠 Smart Features
- **State preservation** – return to the same list state (category, page, expanded products) when navigating back
- **Responsive UI** – works seamlessly on desktop, tablet, and mobile
- **Dark theme** – modern, dark interface optimized for long browsing sessions

## 🚀 Tech Stack

### Frontend
- **React 18** – UI framework
- **React Router** – navigation and routing
- **Recharts** – price history charts
- **CSS3** – custom styling with dark theme
- **Vite** – build tool and dev server

### Backend
- **ASP.NET Core 8** – REST API
- **Entity Framework Core** – ORM for database access
- **PostgreSQL** – primary database
- **JWT** – authentication and authorization
- **Playwright** – web scraping automation
- **HtmlAgilityPack** – HTML parsing for scraping

## 🚀 Deployment

The application is live and self-hosted on an **Oracle Cloud ARM VM** (Ubuntu, 24 GB RAM).

Everything runs in **Docker Compose** with four services:

| Service       | Role                                                        |
| ------------- | ----------------------------------------------------------- |
| `postgres-db` | PostgreSQL 16 — products, price history, users, alerts      |
| `backend`     | .NET 8 Web API + headless Playwright (Firefox) price scraper |
| `frontend`    | React (Vite) build served by nginx                          |
| `flaresolverr`| Cloudflare challenge solver for protected stores            |

- nginx reverse-proxies `/api/*` to the backend container.
- EF Core migrations are applied automatically on backend startup.
- Deploy flow: `git push` → `git pull` on the VM → `docker compose up -d --build`.
- Sensitive config (DB credentials, JWT secret) is injected via a `.env` file that is **not** committed to this repository.

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PostgreSQL (or use Docker)
- Docker (optional)

### 🔌 API Endpoints

| Method | Endpoint                       | Description                                  |
|--------|--------------------------------|----------------------------------------------|
| GET    | `/api/product`                 | Get products (with pagination & category filter) |
| GET    | `/api/product/{id}`            | Get product details                          |
| GET    | `/api/product/{id}/history`    | Get price history                            |
| POST   | `/api/auth/register`           | Register a new user                          |
| POST   | `/api/auth/login`              | Login and get JWT token                      |
| GET    | `/api/alerts`                  | Get user's price alerts                      |
| POST   | `/api/alerts`                  | Create a price alert                         |
| DELETE | `/api/alerts/{id}`             | Delete a price alert                         |
| GET    | `/api/notifications`           | Get user's notifications                     |
| POST   | `/api/notifications/{id}/read` | Mark notification as read                    |
