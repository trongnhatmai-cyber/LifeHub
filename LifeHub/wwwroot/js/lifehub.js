/* =====================================================================
   LIFEHUB.JS - tương tác giao diện dùng chung cho toàn hệ thống.
   Gồm: đổi chế độ Sáng / Tối, menu thả xuống (thông báo, tài khoản) và tìm kiếm toàn hệ thống (Ctrl + K).
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
            // Nút là icon (mặt trăng / mặt trời do CSS đổi theo data-theme), chỉ cập nhật nhãn cho trình đọc màn hình.
            toggle.setAttribute("aria-pressed", String(t === "dark"));
            toggle.setAttribute("aria-label", t === "dark" ? "Chuyển sang chế độ sáng" : "Chuyển sang chế độ tối");
            toggle.title = t === "dark" ? "Chế độ sáng" : "Chế độ tối";
        }
    }

    // Cho trang Cài đặt > Giao diện dùng lại cùng cơ chế đổi theme (không cần reload).
    window.LifeHub = window.LifeHub || {};
    window.LifeHub.applyTheme = applyTheme;
    window.LifeHub.clearSavedTheme = function () {
        try { localStorage.removeItem(THEME_KEY); } catch (e) { /* bỏ qua */ }
    };

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

/* Sidebar: bấm tên menu cha thì chuyển trang (không toggle); bấm mũi tên thì đóng/mở. */
(function () {
    "use strict";
    document.querySelectorAll(".app .menu-parent").forEach(function (summary) {
        summary.addEventListener("click", function (e) {
            if (e.target.closest(".menu-parent-link")) {
                e.stopPropagation();
            }
        });
        var link = summary.querySelector(".menu-parent-link");
        if (link) {
            link.addEventListener("click", function (e) {
                // chặn hành vi toggle mặc định của <summary>, vẫn cho thẻ <a> điều hướng
                e.stopPropagation();
            });
        }
    });

    // Accordion: mở một nhóm thì tự đóng các nhóm khác để sidebar không bị dài ra.
    var groups = document.querySelectorAll(".app .menu-group");
    groups.forEach(function (group) {
        group.addEventListener("toggle", function () {
            if (!group.open) return;
            groups.forEach(function (other) {
                if (other !== group && other.open) other.open = false;
            });
        });
    });
})();

/* =====================================================================
   TOPBAR: menu thả xuống, chuông thông báo, tìm kiếm toàn hệ thống (Ctrl + K)
   Dữ liệu lấy từ server qua các URL khai báo ở data-url-* của <header class="topbar">.
   Nội dung từ server luôn đưa vào bằng textContent, không dùng innerHTML.
   ===================================================================== */
(function () {
    "use strict";

    var bar = document.querySelector(".app .topbar");
    if (!bar) return;

    var URL_RECENT = bar.getAttribute("data-url-recent");
    var URL_MARK_READ = bar.getAttribute("data-url-markread");
    var URL_MARK_ALL = bar.getAttribute("data-url-markall");
    var URL_SEARCH = bar.getAttribute("data-url-search");
    var SVG_NS = "http://www.w3.org/2000/svg";

    function el(tag, cls, text) {
        var n = document.createElement(tag);
        if (cls) n.className = cls;
        if (text != null) n.textContent = text;
        return n;
    }
    function icon(name, cls) {
        var s = document.createElementNS(SVG_NS, "svg");
        s.setAttribute("class", "lh-ico" + (cls ? " " + cls : ""));
        var u = document.createElementNS(SVG_NS, "use");
        u.setAttribute("href", "#i-" + name);
        s.appendChild(u);
        return s;
    }
    function token() {
        var i = document.querySelector('#lhTokenForm input[name="__RequestVerificationToken"]');
        return i ? i.value : "";
    }
    function post(url, data) {
        var fd = new FormData();
        fd.append("__RequestVerificationToken", token());
        Object.keys(data || {}).forEach(function (k) { fd.append(k, data[k]); });
        return fetch(url, { method: "POST", body: fd, credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } });
    }
    function getJson(url) {
        return fetch(url, { credentials: "same-origin", redirect: "manual", headers: { "Accept": "application/json" } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .catch(function () { return null; });
    }

    /* ---------- 1. Menu thả xuống (thông báo, tài khoản) ---------- */
    var menus = [
        { btn: document.getElementById("lhNotifBtn"), pop: document.getElementById("lhNotifPop") },
        { btn: document.getElementById("lhUserBtn"), pop: document.getElementById("lhUserPop") }
    ].filter(function (m) { return m.btn && m.pop; });

    function closeMenus(except) {
        menus.forEach(function (m) {
            if (m === except) return;
            m.pop.hidden = true;
            m.btn.setAttribute("aria-expanded", "false");
        });
    }
    menus.forEach(function (m) {
        m.btn.addEventListener("click", function (e) {
            e.stopPropagation();
            var open = m.pop.hidden;
            closeMenus(m);
            m.pop.hidden = !open;
            m.btn.setAttribute("aria-expanded", String(open));
            if (open && m.btn.id === "lhNotifBtn") loadNotifications();
        });
        m.pop.addEventListener("click", function (e) { e.stopPropagation(); });
    });
    document.addEventListener("click", function () { closeMenus(null); });

    /* ---------- 2. Thông báo ---------- */
    function setBadge(n) {
        document.querySelectorAll("[data-lh-unread]").forEach(function (b) {
            b.textContent = n > 99 ? "99+" : String(n);
            b.hidden = n <= 0;
        });
    }

    function renderNotifications(data) {
        var list = document.getElementById("lhNotifList");
        if (!list) return;
        list.textContent = "";
        if (!data || !data.items || data.items.length === 0) {
            list.appendChild(el("p", "lh-pop-empty", "Bạn chưa có thông báo nào."));
            return;
        }
        data.items.forEach(function (n) {
            var row = el("button", "lh-notif" + (n.unread ? " is-unread" : ""));
            row.type = "button";

            var tone = el("span", "lh-tone tone-" + n.tone);
            tone.appendChild(icon(n.icon));
            var body = el("div", "lh-notif-body");
            body.appendChild(el("strong", null, n.title));
            body.appendChild(el("span", null, n.message));
            body.appendChild(el("small", null, n.time));

            row.appendChild(tone);
            row.appendChild(body);
            if (n.unread) row.appendChild(el("span", "lh-notif-dot"));

            row.addEventListener("click", function () {
                var go = function () { if (n.url) window.location.href = n.url; else loadNotifications(); };
                if (n.unread && URL_MARK_READ) post(URL_MARK_READ, { id: n.id }).then(go, go);
                else go();
            });
            list.appendChild(row);
        });
    }

    function loadNotifications() {
        if (!URL_RECENT) return Promise.resolve();
        return getJson(URL_RECENT).then(function (data) {
            if (!data) return;
            setBadge(data.unread);
            renderNotifications(data);
        });
    }

    var readAll = document.getElementById("lhNotifReadAll");
    if (readAll && URL_MARK_ALL) {
        readAll.addEventListener("click", function () {
            post(URL_MARK_ALL, {}).then(loadNotifications, loadNotifications);
        });
    }

    // Số chưa đọc cập nhật ngay khi tải trang (không cần mở dropdown).
    if (URL_RECENT) {
        getJson(URL_RECENT).then(function (data) { if (data) setBadge(data.unread); });
    }

    /* ---------- 3. Tìm kiếm toàn hệ thống ---------- */
    var search = document.getElementById("lhSearch");
    var input = document.getElementById("lhSearchInput");
    var body = document.getElementById("lhSearchBody");
    var openBtn = document.getElementById("lhSearchOpen");
    var closeBtn = document.getElementById("lhSearchClose");
    if (!search || !input || !body || !openBtn) return;

    var HISTORY_KEY = "lifehub_search_history";
    var timer = null, seq = 0;

    function readHistory() {
        try { return JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]").filter(function (x) { return typeof x === "string"; }); }
        catch (e) { return []; }
    }
    function writeHistory(list) {
        try { localStorage.setItem(HISTORY_KEY, JSON.stringify(list.slice(0, 6))); } catch (e) { /* bỏ qua */ }
    }
    function remember(q) {
        q = q.trim();
        if (q.length < 2) return;
        writeHistory([q].concat(readHistory().filter(function (x) { return x !== q; })));
    }

    function showHistory() {
        body.textContent = "";
        var h = readHistory();
        if (h.length === 0) {
            body.appendChild(el("p", "lh-search-empty", "Nhập từ khóa để tìm công việc, dự án, môn học, ghi chú, mục tiêu..."));
            return;
        }
        var label = el("div", "lh-search-label", "Tìm kiếm gần đây");
        var clear = el("button", null, "Xóa lịch sử");
        clear.type = "button";
        clear.addEventListener("click", function () { writeHistory([]); showHistory(); });
        label.appendChild(clear);
        body.appendChild(label);
        h.forEach(function (q) {
            var b = el("button", "lh-result");
            b.type = "button";
            b.appendChild(icon("history"));
            b.appendChild(el("b", null, q));
            b.addEventListener("click", function () { input.value = q; runSearch(); input.focus(); });
            body.appendChild(b);
        });
    }

    function renderResults(data, q) {
        body.textContent = "";
        if (!data || !data.groups || data.groups.length === 0) {
            body.appendChild(el("p", "lh-search-empty", "Không tìm thấy kết quả cho \u201C" + q + "\u201D."));
            return;
        }
        data.groups.forEach(function (g) {
            body.appendChild(el("div", "lh-search-label", g.label));
            g.items.forEach(function (it) {
                var a = el("a", "lh-result");
                a.href = it.url;
                a.appendChild(icon(g.icon));
                a.appendChild(el("b", null, it.title));
                if (it.meta) a.appendChild(el("small", null, it.meta));
                a.addEventListener("click", function () { remember(q); });
                body.appendChild(a);
            });
        });
        setActive(0);
    }

    function results() { return Array.prototype.slice.call(body.querySelectorAll("a.lh-result")); }
    function setActive(i) {
        var list = results();
        list.forEach(function (a, idx) { a.classList.toggle("is-active", idx === i); });
        if (list[i]) list[i].scrollIntoView({ block: "nearest" });
    }
    function activeIndex() {
        return results().findIndex(function (a) { return a.classList.contains("is-active"); });
    }

    function runSearch() {
        var q = input.value.trim();
        var mine = ++seq;
        if (q.length < 2) { showHistory(); return; }
        body.textContent = "";
        body.appendChild(el("p", "lh-search-empty", "Đang tìm..."));
        getJson(URL_SEARCH + "?q=" + encodeURIComponent(q)).then(function (data) {
            if (mine !== seq) return;              // bỏ kết quả cũ nếu người dùng đã gõ tiếp
            renderResults(data, q);
        });
    }

    function openSearch() {
        closeMenus(null);
        search.hidden = false;
        document.body.classList.add("lh-search-open");
        input.value = "";
        showHistory();
        input.focus();
    }
    function closeSearch() {
        if (search.hidden) return;
        search.hidden = true;
        document.body.classList.remove("lh-search-open");
        openBtn.focus();
    }

    openBtn.addEventListener("click", openSearch);
    if (closeBtn) closeBtn.addEventListener("click", closeSearch);
    search.addEventListener("click", function (e) { if (e.target === search) closeSearch(); });
    input.addEventListener("input", function () {
        clearTimeout(timer);
        timer = setTimeout(runSearch, 250);
    });
    input.addEventListener("keydown", function (e) {
        var list = results();
        if (e.key === "ArrowDown" && list.length) { e.preventDefault(); setActive(Math.min(activeIndex() + 1, list.length - 1)); }
        else if (e.key === "ArrowUp" && list.length) { e.preventDefault(); setActive(Math.max(activeIndex() - 1, 0)); }
        else if (e.key === "Enter") {
            var a = list[activeIndex()];
            if (a) { e.preventDefault(); remember(input.value); window.location.href = a.href; }
        }
    });

    document.addEventListener("keydown", function (e) {
        if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) {
            e.preventDefault();
            if (search.hidden) openSearch(); else closeSearch();
        } else if (e.key === "Escape") {
            if (!search.hidden) closeSearch();
            else closeMenus(null);
        }
    });
})();
