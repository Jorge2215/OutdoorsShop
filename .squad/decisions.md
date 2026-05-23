# Squad Decisions

## Active Decisions

---

### 2026-05-23: Project Initialized

**By:** Jorgito (via Squad Coordinator)  
**What:** Outdoors Shop project initialized. Team hired from The Wind-Up Bird Chronicle universe: Toru (Architect), Cinnamon (Backend Developer), Malta (Frontend Developer), Creta (Test Suite), Scribe (Documentation), Ralph (Work Monitor). Squad v0.9.4.  
**Why:** New project kickoff.  

---

### 2026-05-23: Branch Strategy

**By:** Jorgito  
**What:** `dev` is the active development branch. `main` is for production deployment. All squad feature branches follow `squad/{issue-number}-{slug}` convention and target `dev`.  
**Why:** Specified in project brief. Standard feature-branch promotion: feature → dev → main for release.  

---

### 2026-05-23: Tech Stack Confirmed

**By:** Jorgito  
**What:** React + TypeScript (frontend), .NET 10 Web API C# ASP.NET Core (backend), Azure SQL Database with Adventure Works-inspired schema (data), Azure Functions .NET isolated (serverless auxiliary tasks), Azure Blob Storage (assets and reports), JWT role-based auth with roles Administrator and Customer.  
**Why:** Specified in project brief. Baseline for all implementation decisions.  

---

### 2026-05-23: Domain Model Scope

**By:** Jorgito  
**What:** Core entities are Products, Categories (Camping, Trekking, Cycling, Climbing), Customers, Orders, and Inventory. Inspired by Adventure Works simplified schema on Azure SQL Database.  
**Why:** Specified in project brief. Cinnamon owns the data model design under Toru's architecture review.  

---

### 2026-05-23: Azure Function Scope

**By:** Jorgito  
**What:** Azure Functions handle three auxiliary tasks: seasonal discounts (scheduled), payment confirmation (event-driven), stock updates (event-driven).  
**Why:** Specified in project brief. Keeps the Web API lean — background/async work lives in Functions.  

---

### 2026-05-23: Storage Account Scope

**By:** Jorgito  
**What:** Azure Storage Account (Blob) stores product images, order receipts, and exported reports (CSV/Excel). The Web API integrates with the storage account for read/write operations.  
**Why:** Specified in project brief.  

---

## Governance

- All meaningful architectural changes require Toru's approval
- Document architectural decisions here via the inbox drop-box pattern
- Keep history focused on work, decisions focused on direction
