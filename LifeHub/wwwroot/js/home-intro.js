/* wwwroot/js/home-intro-extra.js
   Sơ đồ "Mọi thứ không hoạt động riêng lẻ" hiện lần lượt từng bước khi cuộn tới.
   Không có JS / người dùng tắt hiệu ứng -> sơ đồ hiển thị đầy đủ như bình thường. */
(function () {
    var canvas = document.querySelector('.lhi-flow-canvas');
    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (!canvas || reduce || !('IntersectionObserver' in window)) return;

    var stack = canvas.querySelector('.lhi-flow-stack');
    if (!stack) return;

    Array.prototype.forEach.call(stack.children, function (el, i) {
        el.style.setProperty('--k', i);
    });

    canvas.classList.add('is-armed');

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                canvas.classList.add('is-playing');
                observer.disconnect();
            }
        });
    }, { threshold: 0.35 });

    observer.observe(canvas);
})();