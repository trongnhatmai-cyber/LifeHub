/* =====================================================================
   NOTIFICATION.JS - trang Thông báo (Views/Notification/Index.cshtml)
   Lọc theo loại / chưa đọc / từ khóa và "Xem thêm" đều làm ở trình duyệt trên dữ liệu
   đã render sẵn. Đánh dấu đã đọc và xóa gửi lên server bằng form POST thông thường.
   ===================================================================== */
(function () {
    "use strict";

    var page = document.getElementById("notificationPage");
    if (!page) return;

    var items = Array.prototype.slice.call(page.querySelectorAll(".notification-item"));
    var groups = Array.prototype.slice.call(page.querySelectorAll(".notification-group"));
    var searchInput = document.getElementById("notificationSearch");
    var moreBtn = document.getElementById("notificationMore");
    var noMatch = document.getElementById("notificationNoMatch");
    var pageSize = parseInt(page.getAttribute("data-page-size"), 10) || 15;

    var state = { category: "all", read: "all", query: "", limit: pageSize };

    function norm(s) {
        return (s || "").toString().toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d");
    }

    function apply() {
        var q = norm(state.query);
        var matched = 0, shown = 0;

        items.forEach(function (it) {
            var ok = (state.category === "all" || it.getAttribute("data-category") === state.category) &&
                     (state.read === "all" || it.getAttribute("data-unread") === "true") &&
                     (!q || norm(it.getAttribute("data-search")).indexOf(q) !== -1);
            var visible = false;
            if (ok) {
                matched++;
                if (matched <= state.limit) { visible = true; shown++; }
            }
            it.hidden = !visible;
        });

        groups.forEach(function (g) {
            g.hidden = !g.querySelector(".notification-item:not([hidden])");
        });

        if (noMatch) noMatch.hidden = matched !== 0 || items.length === 0;
        if (moreBtn) moreBtn.hidden = matched <= shown;
    }

    function setActive(selector, attr, value) {
        page.querySelectorAll(selector).forEach(function (b) {
            var on = b.getAttribute(attr) === value;
            b.classList.toggle("active", on);
            b.setAttribute("aria-pressed", on ? "true" : "false");
        });
    }

    page.querySelectorAll(".notification-filter").forEach(function (b) {
        b.addEventListener("click", function () {
            state.category = b.getAttribute("data-category");
            state.limit = pageSize;
            setActive(".notification-filter", "data-category", state.category);
            apply();
        });
    });

    page.querySelectorAll(".notification-chip").forEach(function (b) {
        b.addEventListener("click", function () {
            state.read = b.getAttribute("data-read");
            state.limit = pageSize;
            setActive(".notification-chip", "data-read", state.read);
            apply();
        });
    });

    if (searchInput) {
        searchInput.addEventListener("input", function () {
            state.query = searchInput.value;
            state.limit = pageSize;
            apply();
        });
    }

    if (moreBtn) {
        moreBtn.addEventListener("click", function () {
            state.limit += pageSize;
            apply();
        });
    }

    // Nút mũi tên quay lại: về trang trước, không có lịch sử thì về trang chủ ứng dụng.
    var back = page.querySelector("[data-back]");
    if (back) {
        back.addEventListener("click", function () {
            if (window.history.length > 1) window.history.back();
            else window.location.href = "/Dashboard";
        });
    }

    // Xác nhận trước khi xóa.
    page.querySelectorAll("form[data-confirm]").forEach(function (f) {
        f.addEventListener("submit", function (e) {
            if (!window.confirm(f.getAttribute("data-confirm"))) e.preventDefault();
        });
    });

    // Bấm tiêu đề có liên kết: đánh dấu đã đọc rồi mới chuyển trang.
    var tokenInput = document.querySelector('#lhTokenForm input[name="__RequestVerificationToken"]');
    var markUrl = document.querySelector(".app .topbar") ? document.querySelector(".app .topbar").getAttribute("data-url-markread") : null;
    page.querySelectorAll("a[data-notification-link]").forEach(function (a) {
        a.addEventListener("click", function (e) {
            if (a.getAttribute("data-unread") !== "true" || !tokenInput || !markUrl) return;
            e.preventDefault();
            var fd = new FormData();
            fd.append("__RequestVerificationToken", tokenInput.value);
            fd.append("id", a.getAttribute("data-notification-link"));
            var go = function () { window.location.href = a.href; };
            fetch(markUrl, { method: "POST", body: fd, credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } }).then(go, go);
        });
    });

    apply();
})();
