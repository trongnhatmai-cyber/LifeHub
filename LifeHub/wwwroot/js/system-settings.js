/* =====================================================================
   SYSTEM-SETTINGS.JS - các trang Cài đặt chung (Views/Settings/*)
   Chỉ xử lý tương tác ở trình duyệt: tìm mục cài đặt, xem thử và nhớ giao diện,
   lọc nhật ký, tự gửi form đổi quyền, xác nhận trước khi thu hồi, kiểm tra mật khẩu.
   Mọi dữ liệu và quyền hạn do server (SettingsController) quyết định.
   ===================================================================== */
(function () {
    "use strict";

    var page = document.getElementById("settingsPage");
    if (!page) return;

    function norm(s) {
        return (s || "").toString().toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d");
    }

    /* ---------- Tổng quan: tìm mục cài đặt ---------- */
    var searchBox = document.getElementById("settingsSearch");
    var cards = Array.prototype.slice.call(page.querySelectorAll(".settings-link-card"));
    var noMatch = document.getElementById("settingsNoMatch");
    if (searchBox) {
        searchBox.addEventListener("input", function () {
            var q = norm(searchBox.value.trim());
            var shown = 0;
            cards.forEach(function (c) {
                var ok = !q || norm(c.getAttribute("data-keywords")).indexOf(q) !== -1;
                c.hidden = !ok;
                if (ok) shown++;
            });
            if (noMatch) noMatch.hidden = shown !== 0;
        });
    }

    /* ---------- Giao diện: xem thử ngay và nhớ lựa chọn trên trình duyệt ---------- */
    var appearance = document.getElementById("appearanceForm");
    if (appearance) {
        var saved = appearance.getAttribute("data-saved-theme") === "dark" ? "dark" : "light";
        var api = window.LifeHub || {};

        // Máy chủ là nguồn đúng: đồng bộ lại localStorage theo giá trị đã lưu trong tài khoản.
        if (api.applyTheme) api.applyTheme(saved, true);

        appearance.querySelectorAll('input[name="ThemeMode"]').forEach(function (r) {
            r.addEventListener("change", function () {
                if (api.applyTheme) api.applyTheme(r.value, false);   // xem thử, chưa lưu
            });
        });

        appearance.addEventListener("submit", function () {
            var picked = appearance.querySelector('input[name="ThemeMode"]:checked');
            if (picked && api.applyTheme) api.applyTheme(picked.value, true);
        });
    }

    /* ---------- Nhật ký: lọc theo loại, thời gian, từ khóa ---------- */
    var logTable = document.getElementById("logTable");
    if (logTable) {
        var rows = Array.prototype.slice.call(logTable.querySelectorAll("tbody tr"));
        var logSearch = document.getElementById("logSearch");
        var logRange = document.getElementById("logRange");
        var logNoMatch = document.getElementById("logNoMatch");
        var chips = Array.prototype.slice.call(page.querySelectorAll(".settings-chip"));
        var group = "all";

        var applyLog = function () {
            var q = norm(logSearch ? logSearch.value.trim() : "");
            var days = parseFloat(logRange ? logRange.value : "0") || 0;
            var shown = 0;
            rows.forEach(function (tr) {
                var ok = (group === "all" || tr.getAttribute("data-group") === group) &&
                         (!days || parseFloat(tr.getAttribute("data-age")) <= days) &&
                         (!q || norm(tr.getAttribute("data-search")).indexOf(q) !== -1);
                tr.hidden = !ok;
                if (ok) shown++;
            });
            if (logNoMatch) logNoMatch.hidden = shown !== 0;
        };

        chips.forEach(function (c) {
            c.addEventListener("click", function () {
                group = c.getAttribute("data-group");
                chips.forEach(function (x) {
                    var on = x === c;
                    x.classList.toggle("active", on);
                    x.setAttribute("aria-pressed", on ? "true" : "false");
                });
                applyLog();
            });
        });
        if (logSearch) logSearch.addEventListener("input", applyLog);
        if (logRange) logRange.addEventListener("change", applyLog);
    }

    /* ---------- Chia sẻ: đổi quyền tự gửi form, thu hồi có xác nhận ---------- */
    page.querySelectorAll("select[data-autosubmit]").forEach(function (s) {
        s.addEventListener("change", function () { s.form.submit(); });
    });
    page.querySelectorAll("form[data-confirm]").forEach(function (f) {
        f.addEventListener("submit", function (e) {
            if (!window.confirm(f.getAttribute("data-confirm"))) e.preventDefault();
        });
    });

    /* ---------- Bảo mật: kiểm tra nhanh trước khi gửi (server vẫn kiểm tra lại) ---------- */
    var pwForm = document.getElementById("passwordForm");
    if (pwForm) {
        var errBox = document.getElementById("passwordError");
        pwForm.addEventListener("submit", function (e) {
            var a = document.getElementById("NewPassword").value;
            var b = document.getElementById("ConfirmPassword").value;
            var msg = "";
            if (a.length < 8) msg = "Mật khẩu mới phải có ít nhất 8 ký tự.";
            else if (a !== b) msg = "Mật khẩu nhập lại chưa trùng khớp.";
            if (msg) {
                e.preventDefault();
                errBox.textContent = msg;
                errBox.hidden = false;
            }
        });
    }
})();
