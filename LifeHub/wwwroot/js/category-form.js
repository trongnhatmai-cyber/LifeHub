
/* =====================================================
   LIFEHUB — CATEGORY MANAGEMENT
   Index: tìm kiếm, lọc danh mục.
   Create/Edit: ẩn hiện hạn mức theo loại danh mục.
===================================================== */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    const page = document.querySelector(".finance-page");

    if (!page) return;

    // =============================================
    // 1. TIỆN ÍCH CHUẨN HÓA TIẾNG VIỆT
    // =============================================

    function normalize(value) {
        return (value || "")
            .trim()
            .toLocaleLowerCase("vi-VN")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/đ/g, "d");
    }

    // =============================================
    // 2. TÌM KIẾM VÀ LỌC DANH MỤC
    // =============================================

    const filterButtons = page.querySelectorAll(".filter-button");
    const categoryRows = page.querySelectorAll(".category-row");
    const categorySearch = document.getElementById("categorySearch");
    const emptyResult = document.getElementById("categoryEmpty");

    if (filterButtons.length > 0 && categoryRows.length > 0) {
        let currentFilter = "all";

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
    }

    // =============================================
    // 3. ẨN HIỆN HẠN MỨC THEO LOẠI DANH MỤC
    // =============================================

    const typeSelect = page.querySelector("#Type");
    const expenseBlocks = page.querySelectorAll("[data-expense-only]");

    if (typeSelect && expenseBlocks.length > 0) {
        function syncExpenseFields() {
            const isExpense = typeSelect.value === "Expense";

            expenseBlocks.forEach(block => {
                block.hidden = !isExpense;

                block.querySelectorAll("input, select").forEach(input => {
                    input.disabled = !isExpense;
                });
            });
        }

        typeSelect.addEventListener("change", syncExpenseFields);

        syncExpenseFields();
    }
});
