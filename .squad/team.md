# Squad Team

> OutdoorsShop

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Domain | Badge |
|------|------|---------|--------|-------|
| Toru | Architect | `.squad/agents/toru/charter.md` | System design, Azure infra, deployment strategy, ADRs | 🏗️ Architect |
| Cinnamon | Backend Dev | `.squad/agents/cinnamon/charter.md` | .NET 10 Web API, Azure SQL, EF Core, Azure Functions, Storage | 🔧 Backend |
| Malta | Frontend Dev | `.squad/agents/malta/charter.md` | React + TypeScript, product catalog, shopping cart, order flows | ⚛️ Frontend |
| Creta | Test Suite | `.squad/agents/creta/charter.md` | Unit tests, integration tests, edge cases, QA | 🧪 Tester |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | Design decisions, session logs, technical notes | 📋 Scribe |
| Ralph | Work Monitor | `.squad/agents/ralph/charter.md` | Backlog management, GitHub issues, PR tracking | 🔄 Monitor |

## Project Context

- **Project:** Outdoors Shop
- **Owner:** Jorgito
- **Squad Version:** 0.9.4
- **Created:** 2026-05-23
- **Universe:** The Wind-Up Bird Chronicle (Haruki Murakami)
- **Description:** A full-stack e-commerce web application for outdoor gear. Features a React frontend for product browsing, shopping cart, and order management; a .NET 10 Web API backend for products, categories, customers, orders, and inventory; Azure Functions for seasonal discounts, payment confirmation, and stock updates; and Azure Storage for product images, order receipts, and exported reports.
- **Purpose:** Compare the effort and efficiency of developing with GitHub Copilot + Squad versus traditional development methods, while delivering a functional proof of concept.
- **Tech Stack:**
  - React + TypeScript
  - .NET 10 Web API (C#)
  - Azure SQL Database
  - Azure Functions .NET isolated
  - Azure Blob Storage
  - JWT role-based auth (Administrator, Customer)
- **Repository:** https://github.com/Jorge2215/OutdoorsShop.git (dev branch = development, main branch = production deployment)
- **Expected Features:**
  - Product catalog with categories: Camping, Trekking, Cycling, Climbing
  - Shopping cart and order management
  - Payment simulation and confirmation
  - Inventory tracking and updates
  - CSV/Excel report exports
  - Role-based auth: Administrator, Customer
