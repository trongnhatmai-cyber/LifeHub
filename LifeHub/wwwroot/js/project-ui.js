/* =====================================================================
   LIFEHUB - PROJECT UI dùng chung: thông báo nổi, đếm số, tab "Nhiệm vụ gần đây".
   ===================================================================== */
(function () {
    'use strict';

    var page = document.querySelector('.project-page');
    if (!page) return;

    var wrap = null;

    function pad2(n) { return n < 10 ? '0' + n : String(n); }

    function toast(message, type) {
        if (!wrap) {
            wrap = document.createElement('div');
            wrap.className = 'project-toast-stack';
            wrap.setAttribute('aria-live', 'polite');
            page.appendChild(wrap);
        }
        var el = document.createElement('div');
        el.className = 'project-toast project-toast-' + (type || 'info');
        el.textContent = message; // textContent: không chèn HTML từ người dùng
        wrap.appendChild(el);
        window.setTimeout(function () { el.remove(); }, 4200);
    }

    window.ProjectUI = { toast: toast };

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
            t.classList.toggle('active', active);
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

})();
