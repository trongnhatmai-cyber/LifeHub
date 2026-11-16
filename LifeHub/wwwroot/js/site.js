/* LIFEHUB - JavaScript dùng chung (nạp trong _Layout).
   Giao diện sáng là mặc định; lựa chọn được nhớ trong localStorage. */
document.addEventListener("DOMContentLoaded", () => {
    const KEY = "lifehub-theme";
    const html = document.documentElement;
    const toggle = document.getElementById("themeToggle");

    // localStorage có thể bị chặn (chế độ riêng tư), nên bọc try/catch.
    const readTheme = () => { try { return localStorage.getItem(KEY); } catch { return null; } };
    const saveTheme = value => { try { localStorage.setItem(KEY, value); } catch { /* bỏ qua */ } };

    function applyTheme(theme) {
        html.setAttribute("data-theme", theme);
        toggle?.setAttribute("aria-pressed", String(theme === "dark"));
    }

    applyTheme(readTheme() === "dark" ? "dark" : "light");

    toggle?.addEventListener("click", () => {
        const next = html.getAttribute("data-theme") === "dark" ? "light" : "dark";
        applyTheme(next);
        saveTheme(next);
    });
});