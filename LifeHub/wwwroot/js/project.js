/* =====================================================================
   LIFEHUB - PROJECT MANAGEMENT (DỰ ÁN) - dùng cho cả Index và Details
   Chỉ xử lý tương tác giao diện: tìm kiếm, lọc, sắp xếp, modal, xác nhận.
   Mọi thay đổi dữ liệu đều gửi bằng form POST về Controller.
   ===================================================================== */
(function () {
    'use strict';

    var page = document.querySelector('.project-page');
    if (!page) return;

    /* ---------- Tiện ích ---------- */
    // Bỏ dấu tiếng Việt để tìm "du an" vẫn ra "Dự án"
    function normalize(text) {
        return (text || '')
            .toString()
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .trim();
    }

    /* ---------- Tìm kiếm / Lọc / Sắp xếp (trang danh sách) ---------- */
    var grid = document.getElementById('projectGrid');
    var searchInput = document.getElementById('projectSearch');
    var sortSelect = document.getElementById('projectSort');
    var emptyBox = document.getElementById('projectEmpty');
    var counter = document.getElementById('projectResultCount');
    var tabs = Array.prototype.slice.call(page.querySelectorAll('.proj-tab[data-filter]'));
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
        if (!grid) return;
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

    function showNote(form, noteState, message) {
        var note = form.querySelector('[data-form-note]');
        if (!note) return;
        note.textContent = message;
        note.setAttribute('data-state', noteState);
        note.hidden = false;
    }

    function openModal(modal, opener) {
        lastFocus = document.activeElement;

        var form = modal.querySelector('form[data-reset-on-open]');
        if (form) {
            form.reset();
            hideNote(form);

            // Điền sẵn cột khi bấm "+" ở đầu cột Kanban
            var prefill = opener ? opener.getAttribute('data-prefill-status') : null;
            var statusField = form.querySelector('[name="status"]');
            if (prefill && statusField) statusField.value = prefill;
        }

        modal.hidden = false;
        document.body.classList.add('proj-modal-open');

        var focusTarget = form
            ? form.querySelector('input:not([type="hidden"]), select, textarea')
            : modal.querySelector('.proj-modal__close');
        if (focusTarget) focusTarget.focus();
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
            if (target) openModal(target, opener);
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
        if (e.key === 'Escape') {
            closeModal(page.querySelector('.proj-modal:not([hidden])'));
            return;
        }

        // Enter trên thẻ nhiệm vụ (không phải nút) cũng mở chi tiết
        if (e.key === 'Enter' && e.target.matches && e.target.matches('[data-task-card][data-modal-open]')) {
            var modal = document.getElementById(e.target.getAttribute('data-modal-open'));
            if (modal) openModal(modal, e.target);
        }
    });

    // Sau khi POST, Controller redirect về #task-ID để mở lại đúng nhiệm vụ
    var hashMatch = /^#task-(\d+)$/.exec(window.location.hash);
    if (hashMatch) {
        var taskModal = document.getElementById('taskModal-' + hashMatch[1]);
        if (taskModal) openModal(taskModal, null);
    }

    /* ---------- Form: xác nhận + kiểm tra ngày trước khi gửi ---------- */
    page.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.tagName !== 'FORM') return;

        var confirmText = form.getAttribute('data-confirm');
        if (confirmText && !window.confirm(confirmText)) {
            e.preventDefault();
            return;
        }

        var start = form.querySelector('[data-date-start]');
        var end = form.querySelector('[data-date-end]');
        if (start && end && start.value && end.value && end.value < start.value) {
            e.preventDefault();
            showNote(form, 'error', 'Hạn chót phải sau hoặc bằng ngày bắt đầu.');
            end.focus();
        }
    });

    // Select tự gửi form khi đổi giá trị (VD: đổi trạng thái dự án)
    page.addEventListener('change', function (e) {
        var el = e.target.closest ? e.target.closest('[data-autosubmit]') : null;
        if (el && el.form) el.form.submit();
    });
})();