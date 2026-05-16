namespace Vantage.PMS.Authorization;

public static class PmsRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string GeneralManager = "GeneralManager";
    public const string FrontOfficeManager = "FrontOfficeManager";
    public const string FrontDesk = "FrontDesk";
    public const string HousekeepingSupervisor = "HousekeepingSupervisor";
    public const string Housekeeper = "Housekeeper";
    public const string FinanceManager = "FinanceManager";
    public const string Cashier = "Cashier";
    public const string FBManager = "FBManager";
    public const string FBServer = "FBServer";
    public const string FBCashier = "FBCashier";
    public const string KitchenStaff = "KitchenStaff";
    public const string Chef = "Chef";
    public const string SalesManager = "SalesManager";
    public const string BanquetManager = "BanquetManager";
    public const string RevenueManager = "RevenueManager";
    public const string PurchasingManager = "PurchasingManager";
    public const string InventoryManager = "InventoryManager";
    public const string HRManager = "HRManager";

    public static readonly string[] All =
    [
        SystemAdmin,
        GeneralManager,
        FrontOfficeManager,
        FrontDesk,
        HousekeepingSupervisor,
        Housekeeper,
        FinanceManager,
        Cashier,
        FBManager,
        FBServer,
        FBCashier,
        KitchenStaff,
        Chef,
        SalesManager,
        BanquetManager,
        RevenueManager,
        PurchasingManager,
        InventoryManager,
        HRManager
    ];

    public static readonly string[] AdminSetup =
    [
        SystemAdmin
    ];

    public static readonly string[] FrontOffice =
    [
        SystemAdmin,
        GeneralManager,
        FrontOfficeManager,
        FrontDesk
    ];

    public static readonly string[] Housekeeping =
    [
        SystemAdmin,
        GeneralManager,
        HousekeepingSupervisor,
        Housekeeper
    ];

    public static readonly string[] Finance =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager,
        Cashier
    ];

    public static readonly string[] FinanceApprovals =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager
    ];

    public static readonly string[] AccountsReceivable =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager
    ];

    public static readonly string[] FoodBeverageService =
    [
        SystemAdmin,
        GeneralManager,
        FBManager,
        FBServer,
        FBCashier
    ];

    public static readonly string[] FoodBeverageKitchen =
    [
        SystemAdmin,
        GeneralManager,
        FBManager,
        KitchenStaff,
        Chef
    ];

    public static readonly string[] Sales =
    [
        SystemAdmin,
        GeneralManager,
        SalesManager
    ];

    public static readonly string[] Banquet =
    [
        SystemAdmin,
        GeneralManager,
        SalesManager,
        BanquetManager
    ];

    public static readonly string[] Revenue =
    [
        SystemAdmin,
        GeneralManager,
        RevenueManager,
        FrontOfficeManager
    ];

    public static readonly string[] BookingEngineManagement =
    [
        SystemAdmin,
        GeneralManager,
        RevenueManager,
        FrontOfficeManager,
        FrontDesk
    ];

    public static readonly string[] GuestPortalManagement =
    [
        SystemAdmin,
        GeneralManager,
        FrontOfficeManager,
        FrontDesk,
        HousekeepingSupervisor,
        FinanceManager
    ];

    public static readonly string[] InventoryPurchasing =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager,
        PurchasingManager,
        InventoryManager,
        FBManager,
        HousekeepingSupervisor
    ];

    public static readonly string[] Reports =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager,
        FrontOfficeManager,
        FrontDesk,
        HousekeepingSupervisor,
        FBManager,
        SalesManager,
        BanquetManager,
        RevenueManager,
        PurchasingManager,
        InventoryManager
    ];

    public static readonly string[] ReportAdministration =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager
    ];

    public static readonly string[] ManagementAI =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager,
        FrontOfficeManager
    ];

    public static readonly string[] AIIntegrationSettings =
    [
        SystemAdmin
    ];

    public static readonly string[] SystemManagement =
    [
        SystemAdmin,
        GeneralManager
    ];

    public static readonly string[] SystemAdministration =
    [
        SystemAdmin
    ];

    public static readonly string[] PrintableDocuments =
    [
        SystemAdmin,
        GeneralManager,
        FrontOfficeManager,
        FrontDesk,
        FinanceManager,
        Cashier,
        FBManager,
        FBCashier,
        FBServer,
        KitchenStaff,
        Chef,
        SalesManager,
        BanquetManager,
        PurchasingManager,
        InventoryManager
    ];

    public static readonly string[] ClientDemo =
    [
        SystemAdmin,
        GeneralManager,
        SalesManager
    ];

    public static readonly string[] LaborCosting =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager,
        HRManager
    ];

    public static readonly string[] ExecutiveReporting =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager,
        RevenueManager,
        FrontOfficeManager,
        SalesManager,
        BanquetManager
    ];

    public static readonly string[] ExecutiveManagement =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager
    ];

    public static readonly string[] GroupManagement =
    [
        SystemAdmin,
        GeneralManager,
        FrontOfficeManager,
        FrontDesk,
        SalesManager,
        FinanceManager
    ];

    public static readonly string[] GroupFinance =
    [
        SystemAdmin,
        GeneralManager,
        FinanceManager
    ];
}
