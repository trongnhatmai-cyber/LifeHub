/* =====================================================================
   LIFEHUB - PROJECT MANAGEMENT (DỰ ÁN)
   Chỉ xử lý tương tác giao diện: tìm kiếm, lọc, sắp xếp, modal.
   Dữ liệu do C# render sẵn trong Razor (data-attributes).
   ===================================================================== */
(function () {
    'use strict';

    var page = document.querySelector('.project-page');
    if (!page) return;

    /* ---------- Tiện ích ---------- */
    // Bỏ dấu tiếng Việt để tìm kiếm "du an" vẫn ra "Dự án"
    function normalize(text) {
        return (text || '')
            .toString()
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .trim();
    }

    /* ---------- Tìm kiếm / Lọc / Sắp xếp ---------- */
    var grid = document.getElementById('projectGrid');
    var searchInput = document.getElementById('projectSearch');
    var sortSelect = document.getElementById('projectSort');
    var emptyBox = document.getElementById('projectEmpty');
    var counter = document.getElementById('projectResultCount');
    var tabs = Array.prototype.slice.call(page.querySelectorAll('.proj-tab'));
    var cards = grid ? Array.prototype.slice.call(grid.querySelectorAll('[data-project-card]')) : [];

    var state = { filter: 'all', query: '', sort: 'default' };

    cards.forEach(function (card) {
        card.dataset.searchKey = normalize(card.dataset.search);
    });

    var comparators = {
        'default': function (a, b) {
            return Number(a.dataset.index) - Number(b.dataset.index);
        },
        // Dự án chưa xong xếp trước, rồi theo hạn chót gần nhất
        deadline: function (a, b) {
            var ra = a.dataset.status === 'completed' ? 1 : 0;
            var rb = b.dataset.status === 'completed' ? 1 : 0;
            if (ra !== rb) return ra - rb;
            if (a.dataset.due === b.dataset.due) return 0;
            return a.dataset.due < b.dataset.due ? -1 : 1;
        },
        progress: function (a, b) {
            return Number(b.dataset.progress) - Number(a.dataset.progress);
        },
        name: function (a, b) {
            return a.dataset.name.localeCompare(b.dataset.name, 'vi');
        }
    };

    function applySort() {
        if (!grid) return;
        var compare = comparators[state.sort] || comparators['default'];
        cards.slice().sort(compare).forEach(function (card) {
            grid.appendChild(card);
        });
    }

    function applyFilter() {
        var query = normalize(state.query);
        var visible = 0;

        cards.forEach(function (card) {
            var matchStatus = state.filter === 'all' || card.dataset.status === state.filter;
            var matchQuery = !query || card.dataset.searchKey.indexOf(query) !== -1;
            var show = matchStatus && matchQuery;
            card.hidden = !show;
            if (show) visible++;
        });

        if (emptyBox) emptyBox.hidden = visible !== 0;
        if (counter) counter.textContent = 'Hiển thị ' + visible + '/' + cards.length + ' dự án';
    }

    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            state.filter = tab.getAttribute('data-filter');
            tabs.forEach(function (t) {
                var active = t === tab;
                t.classList.toggle('is-active', active);
                t.setAttribute('aria-pressed', active ? 'true' : 'false');
            });
            applyFilter();
        });
    });

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            state.query = searchInput.value;
            applyFilter();
        });
    }

    if (sortSelect) {
        sortSelect.addEventListener('change', function () {
            state.sort = sortSelect.value;
            applySort();
        });
    }

    applySort();
    applyFilter();

    /* ---------- Modal ---------- */
    var lastFocus = null;

    function hideNote(form) {
        var note = form.querySelector('[data-form-note]');
        if (note) {
            note.hidden = true;
            note.textContent = '';
            note.removeAttribute('data-state');
        }
    }

    function showNote(form, state, message) {
        var note = form.querySelector('[data-form-note]');
        if (!note) return;
        note.textContent = message;
        note.setAttribute('data-state', state);
        note.hidden = false;
    }

    function openModal(modal) {
        lastFocus = document.activeElement;

        var form = modal.querySelector('[data-demo-form]');
        if (form) {
            form.reset();
            hideNote(form);
        }

        modal.hidden = false;
        document.body.classList.add('proj-modal-open');

        var first = modal.querySelector('input, select, textarea');
        if (first) first.focus();
    }

    function closeModal(modal) {
        if (!modal) return;
        modal.hidden = true;
        document.body.classList.remove('proj-modal-open');
        if (lastFocus && typeof lastFocus.focus === 'function') lastFocus.focus();
    }

    page.addEventListener('click', function (e) {
        var opener = e.target.closest('[data-modal-open]');
        if (opener) {
            var target = document.getElementById(opener.getAttribute('data-modal-open'));
            if (target) openModal(target);
            return;
        }

        var closer = e.target.closest('[data-modal-close]');
        if (closer) {
            closeModal(closer.closest('.proj-modal'));
            return;
        }

        // Bấm vào nền mờ bên ngoài hộp thoại
        if (e.target.classList && e.target.classList.contains('proj-modal')) {
            closeModal(e.target);
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        closeModal(page.querySelector('.proj-modal:not([hidden])'));
    });

    /* ---------- Form mô phỏng ----------
       Chưa có Database/API: chỉ kiểm tra dữ liệu nhập và báo rõ là chưa lưu.
       Không thêm thẻ mới vào danh sách và không báo thành công giả. */
    page.addEventListener('submit', function (e) {
        var form = e.target.closest('[data-demo-form]');
        if (!form) return;

        e.preventDefault();

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        var start = form.querySelector('[data-date-start]');
        var end = form.querySelector('[data-date-end]');
        if (start && end && start.value && end.value && end.value < start.value) {
            showNote(form, 'error', 'Hạn chót phải sau hoặc bằng ngày bắt đầu.');
            end.focus();
            return;
        }

        showNote(
            form,
            'info',
            'Đây là bản giao diện mô phỏng: dữ liệu chưa được lưu. Chức năng lưu sẽ hoạt động khi kết nối cơ sở dữ liệu.'
        );
    });
})();