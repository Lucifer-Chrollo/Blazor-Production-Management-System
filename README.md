# Inventory Management System

A comprehensive web-based Inventory Management System built with **Blazor Server** and **.NET**. This application helps businesses manage products, track stock, handle production via Work Orders, and maintain Bills of Materials (BOM).

## Features

### 📦 Product Management
- **Centralized Inventory**: Track Finish Goods, Raw Materials, and Assemblies.
- **Stock Tracking**: Real-time quantity updates, minimum stock alerts.
- **Cost Management**: Tracks Purchase Price, Sale Price, and calculates Weighted Average Cost (WAC) for manufactured items.

### 🏭 Production Control (Work Orders)
- **Work Order Management**: Create, start, and complete work orders for manufacturing.
- **Bill of Materials (BOM)**: Define multi-level recipes for products. Support for sub-assemblies.
- **Automated Stock Updates**:
    - Deducts raw materials upon completion.
    - Adds finished goods to stock.
    - Updates product cost based on production inputs (WAC).

### 📊 Dashboard & Reporting
- **Production Dashboard**: View active orders, recent production logs, and KPIs.
- **Transactions**: Full history of Stock In/Out and Adjustments.

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) (or later)
- SQL Server (or LocalDB)

### Installation

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/yourusername/InventoryManagementSystem.git
    cd InventoryManagementSystem
    ```

2.  **Configure Database**:
    - Update `appsettings.json` with your connection string.
    - The application uses ADO.NET / Dapper-style patterns. Ensure the database schema is applied (see `schema.sql` if available, or run migration scripts).

3.  **Run the Application**:
    ```bash
    dotnet watch run
    ```
    The application will launch in your default browser.

## Tech Stack
- **Framework**: ASP.NET Core Blazor Server
- **Language**: C#
- **UI Component Library**: Radzen Blazor Components
- **Database**: SQL Server
- **Data Access**: ADO.NET (Service Pattern)

## License
[MIT](LICENSE)
