/* =====================================================================
   LIFEHUB - ANALYTICS (THỐNG KÊ & BÁO CÁO)  |  wwwroot/js/report.js   (MỘT FILE DUY NHẤT)

   Mọi View trong Views/Report nạp đúng một dòng:
     <script src="~/js/report.js" asp-append-version="true"></script>

   VAI TRÒ: chỉ xử lý tương tác giao diện. Số liệu, KPI, biểu đồ đều do C# tính sẵn.
     1. Bộ lọc thời gian: chọn "Tùy chỉnh" thì hiện ô từ ngày / đến ngày; đổi so sánh kỳ trước thì tự gửi form.
     2. Bật / tắt từng chuỗi dữ liệu của biểu đồ: nút [data-series-toggle] điều khiển .rc-series trong #chart-<id>.
     3. Bảng: nút "Xem thêm / Thu gọn" hiện hoặc ẩn các dòng [data-extra].
     4. Menu xuất báo cáo: đóng khi bấm ra ngoài; nút [data-print] gọi in / lưu PDF.
     5. Form lọc GET (mục tiêu, trạng thái...): select có data-autosubmit tự gửi form.
===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
    "use strict";
    const page = document.querySelector(".report-page");
    if (!page) return;

    // 1. Bộ lọc thời gian
    const custom = page.querySelector(".custom-range");
    page.querySelectorAll("[data-range-custom]").forEach(btn => btn.addEventListener("click", event => {
        event.preventDefault();
        if (custom) { custom.hidden = !custom.hidden; if (!custom.hidden) { const f = custom.querySelector("input"); if (f) f.focus(); } }
    }));
    // Nút "Áp dụng" của khoảng tùy chỉnh: đặt range = custom trước khi gửi
    page.querySelectorAll("[data-range-apply]").forEach(btn => btn.addEventListener("click", () => {
        const hidden = page.querySelector('input[name="range"]');
        if (hidden) hidden.value = "custom";
    }));
    page.querySelectorAll("[data-autosubmit]").forEach(el => el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); }));

    // 2. Bật / tắt chuỗi dữ liệu
    page.querySelectorAll("[data-series-toggle]").forEach(btn => btn.addEventListener("click", () => {
        const chart = document.getElementById("chart-" + btn.dataset.chart);
        if (!chart) return;
        const on = btn.getAttribute("aria-pressed") !== "true";
        btn.setAttribute("aria-pressed", on ? "true" : "false");
        chart.querySelectorAll('.rc-series[data-series="' + btn.dataset.seriesToggle + '"]').forEach(g => g.classList.toggle("is-off", !on));
    }));

    // 3. Xem thêm dòng trong bảng
    page.querySelectorAll("[data-more]").forEach(btn => btn.addEventListener("click", () => {
        const table = document.getElementById(btn.dataset.more);
        if (!table) return;
        const expand = btn.getAttribute("aria-expanded") !== "true";
        table.querySelectorAll("tr[data-extra]").forEach(tr => { tr.hidden = !expand; });
        btn.setAttribute("aria-expanded", expand ? "true" : "false");
        btn.textContent = expand ? "Thu gọn" : "Xem thêm";
    }));

    // 4. Menu xuất báo cáo và in
    page.querySelectorAll("[data-print]").forEach(btn => btn.addEventListener("click", () => window.print()));
    document.addEventListener("click", event => {
        document.querySelectorAll("details.export-menu[open]").forEach(d => { if (!d.contains(event.target)) d.open = false; });
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") document.querySelectorAll("details.export-menu[open]").forEach(d => { d.open = false; });
    });
});
