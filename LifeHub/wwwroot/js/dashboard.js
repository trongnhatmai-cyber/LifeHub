/* =========================================================
   LIFEHUB DASHBOARD.JS  (Views/Dashboard/Index.cshtml)
   1. Đổi lời chào theo giờ của trình duyệt (máy chủ có thể khác múi giờ).
   Chuyển Sáng/Tối do lifehub.js đảm nhiệm, file này không xử lý theme.
   Dữ liệu từng khu vực do Controller và View đưa sẵn, không cần JS.
========================================================= */
(() => {
    "use strict";

    function greetingByHour(hour) {
        if (hour < 11) return "Chào buổi sáng";
        if (hour < 14) return "Chào buổi trưa";
        if (hour < 18) return "Chào buổi chiều";
        return "Chào buổi tối";
    }

    function init() {
        const el = document.querySelector("[data-greeting]");
        if (el) el.textContent = greetingByHour(new Date().getHours());
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init, { once: true });
    else init();
})();
