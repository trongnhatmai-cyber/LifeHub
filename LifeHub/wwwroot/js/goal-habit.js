(() => {
    const root = document.querySelector(".gh-page");
    if (!root) return;

    const search = root.querySelector("[data-gh-search]");
    const filter = root.querySelector("[data-gh-filter]");
    const items = [...root.querySelectorAll(".gh-item")];
    const empty = root.querySelector("[data-gh-empty]");

    function applyFilters() {
        const term = (search?.value || "").trim().toLocaleLowerCase("vi");
        const status = filter?.value || "";
        let visible = 0;

        items.forEach(item => {
            const text = (item.dataset.title || "").toLocaleLowerCase("vi");
            const itemStatus = item.dataset.status || "";
            const matches = (!term || text.includes(term)) &&
                (!status || itemStatus === status || (status === "Đang thực hiện" && ["Đúng tiến độ", "Tích cực", "Gần đạt"].includes(itemStatus)));
            item.hidden = !matches;
            if (matches) visible++;
        });

        if (empty) empty.hidden = visible !== 0;
    }

    search?.addEventListener("input", applyFilters);
    filter?.addEventListener("change", applyFilters);

    const toast = document.querySelector("[data-gh-toast]");
    document.querySelectorAll("[data-demo-action]").forEach(button => {
        button.addEventListener("click", () => {
            if (!toast) return;
            toast.hidden = false;
            window.setTimeout(() => { toast.hidden = true; }, 2600);
        });
    });

    // Checkbox chỉ thay đổi trạng thái xem trước trên trình duyệt, không ghi dữ liệu.
    root.querySelectorAll(".gh-check").forEach(checkbox => {
        checkbox.addEventListener("change", () => {
            const title = checkbox.closest(".gh-item")?.querySelector("h3");
            if (title) title.style.opacity = checkbox.checked ? ".55" : "1";
            if (title) title.style.textDecoration = checkbox.checked ? "line-through" : "none";
        });
    });
})();
