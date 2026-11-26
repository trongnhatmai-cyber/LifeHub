namespace LifeHub.Models.ViewModels.TimeManagement
{
    // Tên thuộc tính trùng name="" trong _EventFormModal.cshtml.
    // Hiện form chỉ mô phỏng (chưa lưu): Event.EndTime của Back-end mới là DateOnly, chưa có giờ kết thúc,
    // nhắc nhở và lặp lại. Khi Back-end sẵn sàng, tạo action POST nhận class này.
    public class SaveEventInput
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string EventType { get; set; } = "Cá nhân";
        public DateOnly StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string Recurrence { get; set; } = "none";       // none / daily / weekly / monthly / custom
        public string? Reminder { get; set; }                  // số phút nhắc trước
        public string? Color { get; set; }
    }
}
