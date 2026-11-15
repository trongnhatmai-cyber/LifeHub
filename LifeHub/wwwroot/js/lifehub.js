/* =====================================================================
   LIFEHUB.JS - tương tác giao diện dùng chung cho toàn hệ thống.
   Hiện chỉ có: đổi chế độ Sáng / Tối và nhớ lựa chọn trên trình duyệt.
   Không chứa nghiệp vụ và không tạo nguồn dữ liệu (dữ liệu do C# cung cấp).
   ===================================================================== */
(function () {
    "use strict";

    const THEME_KEY = "lifehub_theme";
    const root = document.documentElement;
    const toggle = document.getElementById("themeToggle");

    function readSavedTheme() {
        try { return localStorage.getItem(THEME_KEY); } catch (e) { return null; }
    }

    function applyTheme(theme, save) {
        const t = theme === "dark" ? "dark" : "light";
        root.setAttribute("data-theme", t);
        document.body.setAttribute("data-theme", t);

        if (save) {
            try { localStorage.setItem(THEME_KEY, t); } catch (e) { /* storage bị chặn: bỏ qua */ }
        }

        if (toggle) {
            toggle.textContent = t === "dark" ? "☀ Chế độ sáng" : "☾ Chế độ tối";
            toggle.setAttribute("aria-pressed", String(t === "dark"));
        }
    }

    // Ưu tiên lựa chọn đã lưu; chưa có thì theo cài đặt màu của hệ điều hành.
    const prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
    applyTheme(readSavedTheme() || (prefersDark ? "dark" : "light"), false);

    if (toggle) {
        toggle.addEventListener("click", function () {
            const current = root.getAttribute("data-theme") || "light";
            applyTheme(current === "dark" ? "light" : "dark", true);
        });
    }
})();
