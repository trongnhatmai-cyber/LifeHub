using System;


namespace LifeHub.Models.FinanceManagement;

public class Category
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
}
