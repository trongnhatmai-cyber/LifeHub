/* =====================================================================
   LIFEHUB - PROJECT UI dùng chung: thông báo nổi, đếm số, tab "Nhiệm vụ gần đây",
   tick cột mốc có hiệu ứng (mô phỏng phía trình duyệt, chưa lưu dữ liệu).
   ===================================================================== */
(function () {
    'use strict';

    var page = document.querySelector('.project-page');
    if (!page) return;

    var NOTE = ' (mô phỏng, chưa lưu)';
    var wrap = null;

    function pad2(n) { return n < 10 ? '0' + n : String(n); }

    function toast(message, type) {
        if (!wrap) {
            wrap = document.createElement('div');
            wrap.className = 'proj-toast-wrap';
            wrap.setAttribute('aria-live', 'polite');
            page.appendChild(wrap);
        }
        var el = document.createElement('div');
        el.className = 'proj-toast proj-toast--' + (type || 'info');
        el.textContent = message; // textContent: không chèn HTML từ người dùng
        wrap.appendChild(el);
        window.setTimeout(function () { el.remove(); }, 4200);
    }

    window.ProjectUI = { toast: toast, note: NOTE };

    /* ---------- Số tổng quan đếm lên từ 0 ---------- */
    function countUp() {
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        Array.prototype.forEach.call(page.querySelectorAll('[data-count]'), function (el) {
            var target = parseInt(el.getAttribute('data-count'), 10);
            if (isNaN(target) || target <= 0) return;

            var start = null;
            var duration = 700;

            function step(ts) {
                if (start === null) start = ts;
                var p = Math.min((ts - start) / duration, 1);
                var value = Math.round(target * (1 - Math.pow(1 - p, 3)));
                el.textContent = pad2(value);
                if (p < 1) window.requestAnimationFrame(step);
            }

            el.textContent = pad2(0);
            window.requestAnimationFrame(step);
        });
    }
    countUp();

    /* ---------- Tab "Giao cho tôi" / "Tôi đã giao" ---------- */
    page.addEventListener('click', function (e) {
        var tab = e.target.closest('[data-recent-tab]');
        if (!tab) return;

        var key = tab.getAttribute('data-recent-tab');
        Array.prototype.forEach.call(page.querySelectorAll('[data-recent-tab]'), function (t) {
            var active = t === tab;
            t.classList.toggle('is-active', active);
            t.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
        Array.prototype.forEach.call(page.querySelectorAll('[data-recent-panel]'), function (panel) {
            panel.hidden = panel.getAttribute('data-recent-panel') !== key;
        });
    });

    /* ---------- Tiến độ cột mốc (x/y và thanh) ---------- */
    function updateMilestoneProgress(scope) {
        if (!scope) return;
        var total = scope.querySelectorAll('[data-ms-item]').length;
        var done = scope.querySelectorAll('[data-ms-item].is-done').length;

        var text = scope.querySelector('[data-ms-progress]');
        if (text) text.textContent = done + '/' + total;

        var bar = scope.querySelector('[data-ms-bar]');
        if (bar) bar.style.width = (total === 0 ? 0 : Math.round(done * 100 / total)) + '%';
    }

    /* ---------- Tick cột mốc ---------- */
    page.addEventListener('submit', function (e) {
        var form = e.target.closest ? e.target.closest('form[data-sim-milestone]') : null;
        if (!form) return;

        e.preventDefault();

        var item = form.closest('[data-ms-item]');
        var button = form.querySelector('.ms-toggle');
        if (!item || !button) return;

        var done = item.classList.toggle('is-done');
        item.classList.toggle('is-overdue', !done && item.getAttribute('data-past-due') === 'true');

        button.textContent = done ? '✓' : '○';
        var label = done ? 'Mở lại cột mốc' : 'Đánh dấu hoàn thành';
        button.title = label;
        button.setAttribute('aria-label', label);

        // Hiệu ứng nảy: gỡ class, ép trình duyệt tính lại rồi gắn lại để chạy lại animation
        button.classList.remove('is-popping');
        void button.offsetWidth;
        button.classList.add('is-popping');

        var meta = item.querySelector('[data-ms-meta]');
        if (meta) meta.textContent = done ? 'Đã hoàn thành' : (item.getAttribute('data-open-text') || '');

        updateMilestoneProgress(item.closest('[data-ms-scope]'));

        var titleEl = item.querySelector('.ms-title, .ms-node__title');
        var title = titleEl ? titleEl.textContent.trim() : 'cột mốc';
        toast((done ? 'Đã hoàn thành cột mốc "' : 'Đã mở lại cột mốc "') + title + '"' + NOTE, 'success');
    });
})();