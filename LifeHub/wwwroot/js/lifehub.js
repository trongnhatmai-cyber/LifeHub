
(function () {
    "use strict";

    const d = document;
    const $ = id => d.getElementById(id);

    // =========================
    // NGÀY HIỆN TẠI
    // =========================

    const days = [
        'Chủ nhật',
        'Thứ Hai',
        'Thứ Ba',
        'Thứ Tư',
        'Thứ Năm',
        'Thứ Sáu',
        'Thứ Bảy'
    ];

    const now = new Date();

    const today = $('today');

    if (today) {
        today.textContent =
            days[now.getDay()] + ', ' +
            String(now.getDate()).padStart(2, '0') + '/' +
            String(now.getMonth() + 1).padStart(2, '0') + '/' +
            now.getFullYear();
    }

    // =========================
    // LỊCH TUẦN
    // =========================

    const week = $('week');

    if (week) {
        const mon = new Date(now);

        mon.setDate(
            now.getDate() - ((now.getDay() + 6) % 7)
        );

        const labels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

        let html = '';

        for (let i = 0; i < 7; i++) {
            const x = new Date(mon);

            x.setDate(mon.getDate() + i);

            const active =
                x.toDateString() === now.toDateString();

            html += `
                <div class="day ${active ? 't' : ''}">
                    ${labels[i]}
                    <b>${String(x.getDate()).padStart(2, '0')}</b>
                </div>
            `;
        }

        week.innerHTML = html;
    }

    // =========================
    // DỮ LIỆU CÔNG VIỆC
    // =========================

    const tasks = [
        {
            t: 'Nộp bài tập Cơ sở dữ liệu',
            p: 'r',
            d: 0
        },
        {
            t: 'Họp nhóm đồ án LIFEHUB',
            p: 'w',
            d: 0
        },
        {
            t: 'Viết báo cáo tuần',
            p: '',
            d: 0
        },
        {
            t: 'Ôn từ vựng IELTS',
            p: '',
            d: 1
        }
    ];

    // =========================
    // DỮ LIỆU BÀI TẬP
    // =========================

    const asg = [
        {
            t: 'Bài tập lớn CSDL',
            s: 'Cơ sở dữ liệu',
            due: 'Hôm nay 23:59',
            p: 'r',
            d: 0
        },
        {
            t: 'Báo cáo Wireshark',
            s: 'Mạng máy tính',
            due: 'Thứ Hai',
            p: 'w',
            d: 0
        },
        {
            t: 'Essay chủ đề công nghệ',
            s: 'Tiếng Anh',
            due: 'Thứ Tư',
            p: '',
            d: 0
        },
        {
            t: 'Bài tập chương 3',
            s: 'Toán rời rạc',
            due: 'Đã nộp',
            p: '',
            d: 1
        }
    ];

    // =========================
    // RENDER DANH SÁCH
    // =========================

    function list(id, arr, cid, sub) {
        const container = $(id);

        if (!container) return;

        let html = '';

        arr.forEach((a, i) => {
            const priority =
                a.p === 'r' ? 'bad' :
                a.p === 'w' ? 'warn' : 'ok';

            html += `
                <div class="row ${a.d ? 'd' : ''}">

                    <button
                        class="cb ${a.d ? 'd' : ''}"
                        data-i="${i}"
                        aria-label="Hoàn thành"
                        type="button">
                    </button>

                    <span class="n">
                        ${escapeHTML(a.t)}

                        ${sub ? `
                            <br>
                            <small class="mut">
                                ${escapeHTML(a.s)} · ${escapeHTML(a.due)}
                            </small>
                        ` : ''}
                    </span>

                    ${a.d ? '' : `
                        <span
                            class="pt"
                            style="
                                width:10px;
                                height:10px;
                                border-radius:50%;
                                flex:none;
                                background:var(--${priority});
                            ">
                        </span>
                    `}

                </div>
            `;
        });

        container.innerHTML = html;

        const counter = $(cid);

        if (counter) {
            counter.textContent =
                arr.filter(x => !x.d).length;
        }
    }

    // =========================
    // CHỐNG CHÈN HTML
    // =========================

    function escapeHTML(value) {
        return String(value).replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        })[char]);
    }

    // =========================
    // CẬP NHẬT GIAO DIỆN
    // =========================

    function draw() {
        list('tasks', tasks, 'openCnt', false);
        list('asg', asg, 'asgCnt', true);
    }

    // =========================
    // ĐÁNH DẤU HOÀN THÀNH
    // =========================

    function setupList(id, arr) {
        const container = $(id);

        if (!container) return;

        container.addEventListener('click', e => {
            const button = e.target.closest('.cb');

            if (!button) return;

            const index = Number(button.dataset.i);

            const item = arr[index];

            if (!item) return;

            item.d = item.d ? 0 : 1;

            draw();
        });
    }

    setupList('tasks', tasks);
    setupList('asg', asg);

    // =========================
    // THÊM CÔNG VIỆC
    // =========================

    const addForm = $('addT');

    if (addForm) {
        addForm.addEventListener('submit', e => {
            e.preventDefault();

            const input = $('qi');

            const value = input.value.trim();

            if (!value) return;

            tasks.unshift({
                t: value,
                p: '',
                d: 0
            });

            input.value = '';

            draw();
        });
    }

    draw();

    // =========================
    // CHUYỂN TRANG
    // =========================

    const names = {
        'tong-quan': 'Tổng quan',
        'hoc-tap': 'Học tập'
    };

    function route() {
        let hash = (location.hash || '#tong-quan').slice(1);

        if (!names[hash]) {
            hash = 'tong-quan';
        }

        d.querySelectorAll('.view').forEach(view => {
            view.classList.toggle(
                'on',
                view.id === hash
            );
        });

        d.querySelectorAll('.menu-link').forEach(link => {
            link.classList.toggle(
                'on',
                link.dataset.page === hash
            );
        });

        const title = $('ttl');

        if (title) {
            title.textContent = names[hash];
        }

        d.body.style.setProperty(
            '--mc',
            hash === 'hoc-tap' ? '#2f7fc1' : '#2f9e63'
        );
    }

    window.addEventListener('hashchange', route);

    // =========================
    // MENU
    // =========================

    d.querySelectorAll('.menu-link').forEach(link => {
        link.addEventListener('click', e => {
            const page = link.dataset.page;

            // Các trang chưa xây dựng
            if (!names[page]) return;

            // Nếu đang ở Home và chuyển giữa các mục
            // thì sử dụng hash để đổi nội dung
            const url = new URL(link.href);

            if (url.pathname === location.pathname) {
                e.preventDefault();

                if (page === 'tong-quan') {
                    location.hash = 'tong-quan';
                } else {
                    location.hash = page;
                }
            }
        });
    });

    route();

    // =========================
    // DARK MODE
    // =========================

    const themeButton = $('theme');

    if (themeButton) {
        themeButton.addEventListener('click', () => {
            const root = d.documentElement;

            const isDark = root.dataset.theme
                ? root.dataset.theme === 'dark'
                : window.matchMedia(
                    '(prefers-color-scheme: dark)'
                  ).matches;

            root.dataset.theme = isDark ? 'light' : 'dark';
        });
    }

})();
