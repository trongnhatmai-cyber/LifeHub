/* LIFEHUB – Landing: header, hiệu ứng hiện dần, thanh điều hướng tính năng,
   quy trình theo bước, luồng dữ liệu, bật/tắt phân hệ minh họa, FAQ.
   Mỗi phần tự bỏ qua nếu trang không có phần tử tương ứng. */
(function () {
    var d = document;
    var reduce = matchMedia('(prefers-reduced-motion:reduce)').matches;
    var $ = function (s, r) { return (r || d).querySelector(s); };
    var $$ = function (s, r) { return [].slice.call((r || d).querySelectorAll(s)); };

    /* Header: đổ bóng khi cuộn + menu mobile */
    var nav = $('#lpNav');
    function onScroll() { if (nav) nav.classList.toggle('is-scrolled', window.scrollY > 8); }
    onScroll();
    addEventListener('scroll', onScroll, { passive: true });

    var burger = $('#lpBurger'), links = $('#lpLinks');
    if (burger && links) {
        var close = function () { links.classList.remove('is-open'); burger.setAttribute('aria-expanded', 'false'); burger.setAttribute('aria-label', 'Mở menu'); };
        burger.addEventListener('click', function () {
            var open = links.classList.toggle('is-open');
            burger.setAttribute('aria-expanded', String(open));
            burger.setAttribute('aria-label', open ? 'Đóng menu' : 'Mở menu');
        });
        d.addEventListener('keydown', function (e) { if (e.key === 'Escape') close(); });
        links.addEventListener('click', function (e) { if (e.target.closest('a')) close(); });
    }

    /* Hiện dần khi cuộn: chạy một lần */
    var rv = $$('.rv');
    if (!('IntersectionObserver' in window) || reduce) {
        rv.forEach(function (e) { e.classList.add('in'); });
    } else {
        var io = new IntersectionObserver(function (es) {
            es.forEach(function (x) { if (x.isIntersecting) { x.target.classList.add('in'); io.unobserve(x.target); } });
        }, { threshold: .12, rootMargin: '0px 0px -40px 0px' });
        rv.forEach(function (e) { io.observe(e); });
    }

    /* Features: tô sáng mục đang xem trên thanh điều hướng */
    var fnLinks = $$('.fn a[href^="#"]');
    if (fnLinks.length && 'IntersectionObserver' in window) {
        var so = new IntersectionObserver(function (es) {
            es.forEach(function (x) {
                if (!x.isIntersecting) return;
                fnLinks.forEach(function (a) { a.classList.toggle('is-on', a.getAttribute('href') === '#' + x.target.id); });
                var on = $('.fn a.is-on');
                if (on && on.scrollIntoView) on.scrollIntoView({ inline: 'center', block: 'nearest' });
            });
        }, { rootMargin: '-35% 0px -55% 0px' });
        fnLinks.forEach(function (a) { var t = $(a.getAttribute('href')); if (t) so.observe(t); });
    }

    /* How it works: minh họa đổi theo bước đang đọc (không khóa cuộn) */
    var steps = $$('.wf-step'), panes = $$('.wf-pane'), wfTitle = $('#wfTitle');
    function showStep(i) {
        steps.forEach(function (s, k) { s.classList.toggle('is-on', k === i); });
        panes.forEach(function (p, k) { p.classList.toggle('is-on', k === i); });
        if (wfTitle && steps[i]) wfTitle.textContent = steps[i].getAttribute('data-title') || '';
    }
    if (steps.length) {
        showStep(0);
        if ('IntersectionObserver' in window) {
            var wo = new IntersectionObserver(function (es) {
                es.forEach(function (x) { if (x.isIntersecting) showStep(steps.indexOf(x.target)); });
            }, { rootMargin: '-40% 0px -40% 0px' });
            steps.forEach(function (s) { wo.observe(s); });
        }
        steps.forEach(function (s, i) { s.addEventListener('click', function () { showStep(i); }); });
    }

    /* How it works: chọn luồng dữ liệu (tab) */
    var tabs = $$('.cd-tabs [role="tab"]'), panels = $$('.cd-panel');
    function pick(i, focus) {
        tabs.forEach(function (t, k) { t.setAttribute('aria-selected', String(k === i)); t.tabIndex = k === i ? 0 : -1; });
        panels.forEach(function (p, k) { p.classList.toggle('is-on', k === i); p.hidden = k !== i; });
        if (focus) tabs[i].focus();
    }
    if (tabs.length) {
        tabs.forEach(function (t, i) {
            t.addEventListener('click', function () { pick(i); });
            t.addEventListener('keydown', function (e) {
                if (e.key === 'ArrowRight') { e.preventDefault(); pick((i + 1) % tabs.length, true); }
                if (e.key === 'ArrowLeft') { e.preventDefault(); pick((i - 1 + tabs.length) % tabs.length, true); }
            });
        });
        pick(0);
    }

    /* Home: bật/tắt phân hệ trong sidebar minh họa */
    $$('.pz-opt input[data-mod]').forEach(function (cb) {
        cb.addEventListener('change', function () {
            var t = $('.mk-nav[data-mod="' + cb.getAttribute('data-mod') + '"]');
            if (t) t.classList.toggle('is-off', !cb.checked);
        });
    });

    /* Mockup tương tác: tick checkbox công việc & chuyển tab minh họa */
    $$('.mk .mk-row .mk-cb').forEach(function (box) {
        box.setAttribute('role', 'button');
        box.setAttribute('tabindex', '0');
        box.setAttribute('aria-label', 'Đánh dấu hoàn thành minh họa');
        var toggle = function () {
            box.classList.toggle('on');
            var row = box.closest('.mk-row');
            if (row) row.classList.toggle('done', box.classList.contains('on'));
        };
        box.addEventListener('click', toggle);
        box.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggle(); }
        });
    });

    $$('.mk-tabs').forEach(function (bar) {
        var items = $$('span', bar);
        items.forEach(function (tab) {
            tab.addEventListener('click', function () {
                items.forEach(function (x) { x.classList.remove('on'); });
                tab.classList.add('on');
                var mk = bar.closest('.mk');
                var key = tab.getAttribute('data-filter');
                if (mk && key) {
                    $$('.mk-row[data-cat]', mk).forEach(function (r) {
                        var cats = (r.getAttribute('data-cat') || '').split(' ');
                        r.style.display = (key === 'all' || cats.indexOf(key) !== -1) ? '' : 'none';
                    });
                }
            });
        });
    });

    /* FAQ: mở/đóng mượt (details vẫn dùng được khi tắt JS) */
    $$('.faq details').forEach(function (det) {
        var sum = $('summary', det), body = $('.faq-a', det);
        if (!sum || !body) return;
        if (det.open) body.style.height = 'auto';
        sum.addEventListener('click', function (e) {
            if (reduce) return;
            e.preventDefault();
            if (det.open) {
                body.style.height = body.scrollHeight + 'px';
                requestAnimationFrame(function () { body.style.height = '0px'; });
                setTimeout(function () { det.open = false; }, 350);
            } else {
                det.open = true;
                body.style.height = '0px';
                requestAnimationFrame(function () { body.style.height = body.scrollHeight + 'px'; });
                setTimeout(function () { body.style.height = 'auto'; }, 360);
            }
        });
    });
})();
