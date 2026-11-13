document.addEventListener("DOMContentLoaded", () => {
    const html = document.documentElement;
    const themeToggle = document.getElementById("themeToggle");

    // Light Mode là giao diện mặc định
    const savedTheme = localStorage.getItem("lifehub-theme") || "light";

    html.setAttribute("data-theme", savedTheme);

    themeToggle?.addEventListener("click", () => {
        const currentTheme = html.getAttribute("data-theme");
        const newTheme = currentTheme === "light" ? "dark" : "light";

        html.setAttribute("data-theme", newTheme);
        localStorage.setItem("lifehub-theme", newTheme);
    });
});