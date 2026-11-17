/* LIFEHUB - TRANG GIỚI THIỆU (Home/Index và Home/Features)
   Chỉ xử lý hiệu ứng giao diện. Mỗi phần tự bỏ qua nếu trang không có phần tử tương ứng. */
(function () {
    var d = document;
    var reduce = matchMedia('(prefers-reduced-motion:reduce)').matches;

    // Hiện dần khi cuộn: chữ trượt từ phải, hình trượt từ trái, tiêu đề trượt lên
    d.querySelectorAll('.lh-split').forEach(function (s) {
        s.querySelectorAll('.lh-copy>*').forEach(function (e, i) {
            e.classList.add('lh-rv');
            if (e.tagName !== 'H2') e.classList.add('lh-r');
            e.style.setProperty('--lh-d', i * .1 + 's');
        });
        var vis = s.querySelector('.lh-vis');
        if (vis) vis.classList.add('lh-rv', 'lh-l');
    });
    d.querySelectorAll('.lh-head,.lh-who span,.lh-mod,.lh-step,.lh-end,.lh-also,.lh-modc,.lh-lk,.lh-flowwrap').forEach(function (e, i) {
        e.classList.add('lh-rv');
        e.style.setProperty('--lh-d', (i % 5) * .08 + 's');
    });

    // Tiêu đề đầu trang hiện từng chữ
    var h1 = d.querySelector('.lh-hero h1');
    if (h1) h1.innerHTML = h1.textContent.split(' ').map(function (w, i) {
        return '<span class="lh-w" style="--lh-i:' + i + '">' + w + '</span>';
    }).join(' ');

    // Thanh tiến độ cuộn trang
    var bar = d.createElement('div');
    bar.className = 'lh-prog';
    d.body.appendChild(bar);
    addEventListener('scroll', function () {
        var max = d.documentElement.scrollHeight - innerHeight;
        bar.style.transform = 'scaleX(' + (max > 0 ? scrollY / max : 0) + ')';
    }, { passive: true });

    // Khung Dashboard nghiêng theo con trỏ chuột (chỉ trang chủ)
    var app = d.querySelector('.lh-app'), hero = d.querySelector('.lh-hero');
    if (app && hero && !reduce) {
        hero.addEventListener('mousemove', function (e) {
            var r = app.getBoundingClientRect();
            var x = (e.clientX - r.left) / r.width - .5, y = (e.clientY - r.top) / r.height - .5;
            app.style.transform = 'perspective(1400px) rotateY(' + x * 6 + 'deg) rotateX(' + (-y * 5) + 'deg)';
        });
        hero.addEventListener('mouseleave', function () { app.style.transform = ''; });
    }

    // Kích hoạt hiệu ứng khi phần tử vào màn hình
    var items = d.querySelectorAll('.lh-rv');
    if (!('IntersectionObserver' in window)) {
        items.forEach(function (e) { e.classList.add('lh-in'); });
    } else {
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (x) {
                if (x.isIntersecting) { x.target.classList.add('lh-in'); io.unobserve(x.target); }
            });
        }, { threshold: .15 });
        items.forEach(function (e) { io.observe(e); });
    }

    // Thanh chuyển nhanh trang tính năng: tô sáng nhóm đang xem
    // chỉ lấy liên kết dạng #neo (bỏ qua liên kết sang trang khác)
    var links = [].slice.call(d.querySelectorAll('.lh-subnav a')).filter(function (a) { return (a.getAttribute('href') || '').charAt(0) === '#'; });
    if (links.length && 'IntersectionObserver' in window) {
        var so = new IntersectionObserver(function (entries) {
            entries.forEach(function (x) {
                if (!x.isIntersecting) return;
                links.forEach(function (a) { a.classList.toggle('lh-on', a.getAttribute('href') === '#' + x.target.id); });
                var on = d.querySelector('.lh-subnav a.lh-on');
                if (on) on.scrollIntoView({ inline: 'center', block: 'nearest' });
            });
        }, { rootMargin: '-35% 0px -55% 0px' });
        links.forEach(function (a) {
            var target = d.querySelector(a.getAttribute('href'));
            if (target) so.observe(target);
        });
    }
})();