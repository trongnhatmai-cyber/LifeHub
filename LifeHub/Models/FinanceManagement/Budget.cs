using System;


namespace LifeHub.Models.FinanceManagement;

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal LimitAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int? AlertThresholdPercent { get; set; } = 80;
}