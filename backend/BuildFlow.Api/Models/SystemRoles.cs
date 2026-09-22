namespace BuildFlow.Api.Models;

public static class SystemRoles
{
    public const string SiteEngineer = "SiteEngineer";
    public const string InventoryOfficer = "InventoryOfficer";
    public const string ProcurementOfficer = "ProcurementOfficer";
    public const string ProjectManager = "ProjectManager";
    public const string Administrator = "Administrator";
    public static readonly string[] All =
        [SiteEngineer, InventoryOfficer, ProcurementOfficer, ProjectManager, Administrator];
    public static readonly string[] AssignableByAdministrator =
        [SiteEngineer, ProjectManager, InventoryOfficer, ProcurementOfficer];
}
