
/* =====================================================
   LIFEHUB — CATEGORY MANAGEMENT
   Chỉ xử lý tìm kiếm và lọc tại trình duyệt.
   CRUD được xử lý bởi ASP.NET Core MVC.
===================================================== */

document.addEventListener("DOMContentLoaded", () => {
    const page = document.querySelector(".finance-page");

    if (!page) return;

    const filterButtons = page.querySelectorAll(".filter-button");
    const categoryRows = page.querySelectorAll(".category-row");
    const categorySearch = document.getElementById("categorySearch");
    const emptyResult = document.getElementById("categoryEmpty");

    if (!filterButtons.length || !categoryRows.length) return;

    let currentFilter = "all";

    const normalize = value =>
        (value || "").trim().toLocaleLowerCase("vi-VN");

    function filterCategories() {
        const keyword = normalize(categorySearch?.value);
        let visibleCount = 0;

        categoryRows.forEach(row => {
            const type = normalize(row.dataset.type);
            const name = normalize(
                row.dataset.name || row.textContent
            );

            const matchesType =
                currentFilter === "all" ||
                type === normalize(currentFilter);

            const matchesKeyword = name.includes(keyword);

            const visible = matchesType && matchesKeyword;

            row.hidden = !visible;

            if (visible) visibleCount++;
        });

        if (emptyResult) {
            emptyResult.hidden = visibleCount !== 0;
        }
    }

    filterButtons.forEach(button => {
        button.addEventListener("click", () => {
            filterButtons.forEach(item => {
                item.classList.remove("active");
                item.setAttribute("aria-pressed", "false");
            });

            button.classList.add("active");
            button.setAttribute("aria-pressed", "true");

            currentFilter = button.dataset.filter || "all";

            filterCategories();
        });
    });

    categorySearch?.addEventListener(
        "input",
        filterCategories
    );

    filterCategories();
});
