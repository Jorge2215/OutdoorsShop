## Base Prompt

## Goal
Our goal is to design and implement a web application called "Outdoors Shop" using .NET 10 as the main development framework. The application will be based on a simplified relational database model inspired by Adventure Works, hosted in Azure SQL Database. 

The solution will include:
- A React frontend for browsing products, managing a shopping cart, and placing orders.
- A .NET 10 Web API backend to handle products, categories, customers, orders, and inventory.
- An Azure Function to process auxiliary tasks such as seasonal discounts, payment confirmation, and stock updates.
- An Azure Storage Account to store product images, order receipts, and exported reports.

## Team
Squad Members names must be Characters from Haruki Murakami's novel "The Wind up Bird Chronicle"

Team Squad roles and purposed names:
- Cinnamon (Backend Developer): builds APIs and database integration.
- Malta (Frontend Developer): designs UI components and shopping flows.
- Creta (Test Suite): validates functionality, edge cases, and error handling.
- Toru (Architect): defines the overall system architecture and deployment strategy.
- Scribe (Documentation): records design decisions and technical notes.
- Ralph: ensures synchronization and consistency across all agents.

## Context
Project Repository on GitHub is: https://github.com/Jorge2215/OutdoorsShop.git
- Use "dev" branch for development
- Use "main" branch for deployment

## Output

Expected features:
- Product catalog with categories (Camping, Trekking, Cycling, Climbing).
- Shopping cart and order management.
- Payment simulation and confirmation.
- Inventory tracking and updates.
- Export of reports in CSV/Excel format.
- Role-based authentication (Administrator, Customer).

## Purpose

The purpose of this project is to compare the effort and efficiency of developing with GitHub Copilot + Squad versus traditional development methods, while delivering a functional proof of concept.

##Prompt Create Tables on Azure SQL DB
Cinnamon, create the EF Core initial migration. Use the following ConnectioString: Server=tcp:azure-sql-pampa.database.windows.net,1433;Initial Catalog=OutdoorsShopDB;Persist Security Info=False;User ID=ShopAdmin;Password=Jorgito2026!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

##Change deployment for frontend Web application

Current situation:
- The frontend React application is currently deployed as a static website inside the Azure Storage Account named "stoutdoorsdev".

Goal:
- Deploy the frontend React Web App to an Azure Static Web App instead.
- Target resource group: rg-outdoors-dev
- Target Static Web App name: app-outdoorsweb-swa

Tasks:
1. Update the deployment script to provision and configure the Static Web App resource.
2. Configure the build and deployment pipeline (GitHub Actions) to publish the React frontend directly to the Static Web App.
3. Ensure the Static Web App is connected to the correct resource group and region.
4. Once the deployment is successful, delete the Storage Account "stoutdoorsdev" to avoid confusion and unnecessary costs.


## Resume
copilot --resume=561f7ff3-e5a1-4fa8-9a38-91908166a3d2