
/* =========================================================
   LIFEHUB - NOTIFICATION PAGE
   Category / Read status / Search / Pagination
   Mark read / Delete / Navigation
   ========================================================= */

(function () {
    "use strict";

    const page = document.getElementById("notificationPage");

    if (!page) return;

    const items = Array.from(
        page.querySelectorAll(".notification-item")
    );

    const groups = Array.from(
        page.querySelectorAll(".notification-group")
    );

    const searchInput = page.querySelector("#notificationSearch");
    const moreBtn = page.querySelector("#notificationMore");
    const noMatch = page.querySelector("#notificationNoMatch");

    const pageSize =
        Math.max(1, parseInt(page.dataset.pageSize, 10) || 15);

    const state = {
        category: "all",
        read: "all",
        query: "",
        limit: pageSize
    };

    /* -----------------------------------------------------
       1. NORMALIZE SEARCH
       Hỗ trợ tìm kiếm tiếng Việt không dấu.
    ----------------------------------------------------- */

    function normalize(value) {
        return String(value || "")
            .toLocaleLowerCase("vi")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/đ/g, "d")
            .trim();
    }

    /* -----------------------------------------------------
       2. GET SEARCH CONTENT
    ----------------------------------------------------- */

    function getSearchContent(item) {
        const body = item.querySelector(".notification-body");

        if (!body) return "";

        return normalize(body.textContent);
    }

    /* -----------------------------------------------------
       3. FILTER NOTIFICATIONS
    ----------------------------------------------------- */

    function matches(item, query) {
        const category = item.dataset.category;
        const unread = item.dataset.unread === "true";

        const categoryMatch =
            state.category === "all" ||
            category === state.category;

        const readMatch =
            state.read === "all" ||
            (state.read === "unread" && unread);

        const searchMatch =
            !query ||
            getSearchContent(item).includes(query);

        return categoryMatch && readMatch && searchMatch;
    }

    /* -----------------------------------------------------
       4. UPDATE FILTER BUTTONS
    ----------------------------------------------------- */

    function setActive(selector, attribute, value) {
        page.querySelectorAll(selector).forEach(button => {
            const active = button.getAttribute(attribute) === value;

            button.classList.toggle("active", active);

            button.setAttribute(
                "aria-pressed",
                active ? "true" : "false"
            );
        });
    }

    /* -----------------------------------------------------
       5. RENDER NOTIFICATION LIST
    ----------------------------------------------------- */

    function applyFilters() {
        const query = normalize(state.query);

        let matched = 0;
        let displayed = 0;

        items.forEach(item => {
            const isMatch = matches(item, query);

            let visible = false;

            if (isMatch) {
                matched++;

                if (displayed < state.limit) {
                    visible = true;
                    displayed++;
                }
            }

            item.hidden = !visible;
        });

        // Ẩn nhóm ngày không có thông báo đang hiển thị.
        groups.forEach(group => {
            const hasVisibleItem = Array.from(
                group.querySelectorAll(".notification-item")
            ).some(item => !item.hidden);

            group.hidden = !hasVisibleItem;
        });

        // Trạng thái không có kết quả.
        if (noMatch) {
            noMatch.hidden =
                matched > 0 || items.length === 0;
        }

        // Nút xem thêm.
        if (moreBtn) {
            moreBtn.hidden = matched <= displayed;
        }

        // Thông tin hỗ trợ accessibility.
        page.setAttribute(
            "data-visible-count",
            String(displayed)
        );

        page.setAttribute(
            "data-matched-count",
            String(matched)
        );
    }

    /* -----------------------------------------------------
       6. CATEGORY FILTER
    ----------------------------------------------------- */

    page.querySelectorAll(".notification-filter")
        .forEach(button => {

            button.addEventListener("click", function () {
                state.category = this.dataset.category || "all";

                state.limit = pageSize;

                setActive(
                    ".notification-filter",
                    "data-category",
                    state.category
                );

                applyFilters();
            });

        });

    /* -----------------------------------------------------
       7. READ STATUS FILTER
    ----------------------------------------------------- */

    page.querySelectorAll(".notification-chip")
        .forEach(button => {

            button.addEventListener("click", function () {
                state.read = this.dataset.read || "all";

                state.limit = pageSize;

                setActive(
                    ".notification-chip",
                    "data-read",
                    state.read
                );

                applyFilters();
            });

        });

    /* -----------------------------------------------------
       8. SEARCH
    ----------------------------------------------------- */

    if (searchInput) {
        searchInput.addEventListener("input", function () {
            state.query = this.value;

            state.limit = pageSize;

            applyFilters();
        });

        searchInput.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                this.value = "";
                state.query = "";
                state.limit = pageSize;

                applyFilters();
            }
        });
    }

    /* -----------------------------------------------------
       9. LOAD MORE
    ----------------------------------------------------- */

    if (moreBtn) {
        moreBtn.addEventListener("click", function () {
            state.limit += pageSize;

            applyFilters();
        });
    }

    /* -----------------------------------------------------
       10. BACK BUTTON
    ----------------------------------------------------- */

    const backButton = page.querySelector("[data-back]");

    if (backButton) {
        backButton.addEventListener("click", function () {
            if (window.history.length > 1) {
                window.history.back();
            } else {
                window.location.assign("/Dashboard");
            }
        });
    }

    /* -----------------------------------------------------
       11. DELETE CONFIRMATION
    ----------------------------------------------------- */

    page.querySelectorAll("form[data-confirm]")
        .forEach(form => {

            form.addEventListener("submit", function (event) {
                const message =
                    this.getAttribute("data-confirm") ||
                    "Bạn có chắc chắn muốn thực hiện thao tác này?";

                if (!window.confirm(message)) {
                    event.preventDefault();
                }
            });

        });

    /* -----------------------------------------------------
       12. MARK AS READ BEFORE NAVIGATION

       Chỉ gửi AJAX nếu:
       - Thông báo chưa đọc.
       - Có antiforgery token.
       - Có endpoint mark-read được cấu hình trên topbar.

       Nếu AJAX thất bại vẫn điều hướng bình thường.
    ----------------------------------------------------- */

    const tokenInput = document.querySelector(
        '#lhTokenForm input[name="__RequestVerificationToken"]'
    );

    const topbar = document.querySelector(".app .topbar");

    const markUrl = topbar
        ? topbar.getAttribute("data-url-markread")
        : null;

    const pendingNavigation = new WeakSet();

    page.querySelectorAll("a[data-notification-link]")
        .forEach(link => {

            link.addEventListener("click", async function (event) {
                const isUnread =
                    this.dataset.unread === "true";

                if (!isUnread || !tokenInput || !markUrl) {
                    return;
                }

                // Tránh gửi nhiều request khi người dùng click liên tục.
                if (pendingNavigation.has(this)) {
                    event.preventDefault();
                    return;
                }

                event.preventDefault();

                pendingNavigation.add(this);

                const targetUrl = this.href;

                const formData = new FormData();

                formData.append(
                    "__RequestVerificationToken",
                    tokenInput.value
                );

                formData.append(
                    "id",
                    this.dataset.notificationLink
                );

                try {
                    await fetch(markUrl, {
                        method: "POST",
                        body: formData,
                        credentials: "same-origin",
                        headers: {
                            "X-Requested-With": "XMLHttpRequest"
                        }
                    });
                } catch (error) {
                    console.warn(
                        "Không thể đánh dấu thông báo đã đọc:",
                        error
                    );
                }

                window.location.assign(targetUrl);
            });

        });

    /* -----------------------------------------------------
       13. INITIALIZE
    ----------------------------------------------------- */

    applyFilters();

})();
