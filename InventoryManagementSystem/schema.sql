USE master;
GO

IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'InventoryDB')
BEGIN
    CREATE DATABASE InventoryDB;
END
GO

USE InventoryDB;
GO

-- Categories
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Categories](
    [CategoryId] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([CategoryId] ASC)
);
END
GO

-- Suppliers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Suppliers](
    [SupplierId] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    [ContactPerson] [nvarchar](max) NULL,
    [Email] [nvarchar](max) NULL,
    [Phone] [nvarchar](max) NULL,
    [Address] [nvarchar](max) NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Suppliers] PRIMARY KEY CLUSTERED ([SupplierId] ASC)
);
END
GO

-- Products
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Products](
    [ProductId] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    [SKU] [nvarchar](max) NOT NULL,
    [Description] [nvarchar](max) NULL,
    [PurchasePrice] [decimal](18, 2) NOT NULL,
    [SalePrice] [decimal](18, 2) NOT NULL,
    [SalePriceWithGST] [decimal](18, 2) NOT NULL,
    [Price] [decimal](18, 2) NOT NULL,
    [Quantity] [int] NOT NULL,
    [MinimumStock] [int] NOT NULL,
    [StockCardNo] [nvarchar](max) NULL,
    [CategoryId] [int] NULL,
    [SupplierId] [int] NULL,
    [Location] [nvarchar](max) NULL,
    [ProductModel] [nvarchar](max) NULL,
    [ItemNature] [int] NOT NULL,
    [ProductType] [int] NOT NULL,
    [IsKPI] [bit] NOT NULL,
    [UOM] [nvarchar](max) NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [LastUpdated] [datetime2](7) NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([ProductId] ASC),
 CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories] ([CategoryId]) ON DELETE CASCADE,
 CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY([SupplierId]) REFERENCES [dbo].[Suppliers] ([SupplierId])
);
END
GO

-- BillOfMaterials
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillOfMaterials]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[BillOfMaterials](
    [BOMId] [int] IDENTITY(1,1) NOT NULL,
    [FinishedProductId] [int] NOT NULL,
    [RawMaterialId] [int] NOT NULL,
    [QuantityRequired] [decimal](18, 2) NOT NULL,
    [Unit] [nvarchar](max) NOT NULL,
    [Notes] [nvarchar](max) NULL,
    [IsActive] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [LastUpdated] [datetime2](7) NULL,
 CONSTRAINT [PK_BillOfMaterials] PRIMARY KEY CLUSTERED ([BOMId] ASC),
 CONSTRAINT [FK_BillOfMaterials_Products_FinishedProductId] FOREIGN KEY([FinishedProductId]) REFERENCES [dbo].[Products] ([ProductId]), -- No cascade to avoid cycles usually, but can be adjusted
 CONSTRAINT [FK_BillOfMaterials_Products_RawMaterialId] FOREIGN KEY([RawMaterialId]) REFERENCES [dbo].[Products] ([ProductId])
);
END
GO

-- WorkOrders
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrders]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[WorkOrders](
    [WorkOrderId] [int] IDENTITY(1,1) NOT NULL,
    [OrderNumber] [nvarchar](max) NOT NULL,
    [ProductId] [int] NOT NULL,
    [QuantityOrdered] [int] NOT NULL,
    [DurationMinutes] [int] NOT NULL,
    [QuantityProduced] [int] NOT NULL,
    [TotalCost] [decimal](18, 2) NOT NULL,
    [Status] [int] NOT NULL,
    [StartDate] [datetime2](7) NOT NULL,
    [CompletionDate] [datetime2](7) NULL,
    [Notes] [nvarchar](max) NULL,
    [BuildReference] [nvarchar](max) NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [LastUpdated] [datetime2](7) NULL,
 CONSTRAINT [PK_WorkOrders] PRIMARY KEY CLUSTERED ([WorkOrderId] ASC),
 CONSTRAINT [FK_WorkOrders_Products_ProductId] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([ProductId]) ON DELETE CASCADE
);
END
GO

-- ProductionLogs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProductionLogs]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ProductionLogs](
    [LogId] [int] IDENTITY(1,1) NOT NULL,
    [WorkOrderId] [int] NOT NULL,
    [QuantityProduced] [int] NOT NULL,
    [ProductionDate] [datetime2](7) NOT NULL,
    [OperatorName] [nvarchar](max) NULL,
    [ShiftInfo] [nvarchar](max) NULL,
    [Notes] [nvarchar](max) NULL,
 CONSTRAINT [PK_ProductionLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
 CONSTRAINT [FK_ProductionLogs_WorkOrders_WorkOrderId] FOREIGN KEY([WorkOrderId]) REFERENCES [dbo].[WorkOrders] ([WorkOrderId]) ON DELETE CASCADE
);
END
GO

-- StockTransactions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StockTransactions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[StockTransactions](
    [TransactionId] [int] IDENTITY(1,1) NOT NULL,
    [ProductId] [int] NOT NULL,
    [Type] [int] NOT NULL,
    [Quantity] [int] NOT NULL,
    [Reference] [nvarchar](max) NULL,
    [Notes] [nvarchar](max) NULL,
    [TransactionDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_StockTransactions] PRIMARY KEY CLUSTERED ([TransactionId] ASC),
 CONSTRAINT [FK_StockTransactions_Products_ProductId] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([ProductId]) ON DELETE CASCADE
);
END
GO
