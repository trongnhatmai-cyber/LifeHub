/* =========================================================
   LIFEHUB DASHBOARD.JS
   1. Hiển thị ngày hiện tại và lịch tuần
   2. Quản lý danh sách việc cần làm (lưu trên trình duyệt)
   3. Cập nhật số việc đang mở
   4. Chuyển Sáng/Tối và ghi nhớ lựa chọn
   Lưu ý: localStorage chỉ lưu trên trình duyệt; muốn đồng bộ tài khoản
   hoặc nhiều thiết bị cần kết nối API/Database ở backend.
========================================================= */
(() => {
    "use strict";
    const $ = (id) => document.getElementById(id);
    const STORAGE_TASKS = "lifehub.dashboard.tasks.v1";
    const STORAGE_THEME = "lifehub.theme";
    const initialTasks = [
        { id: 1, title: "Nộp bài tập Cơ sở dữ liệu", done: false, priority: "high" },
        { id: 2, title: "Họp nhóm đồ án LIFEHUB", done: false, priority: "medium" },
        { id: 3, title: "Viết báo cáo tuần", done: false, priority: "normal" },
        { id: 4, title: "Ôn từ vựng IELTS", done: true, priority: "normal" }
    ];
    let tasks = loadTasks();

    function loadTasks() {
        try {
            const saved = JSON.parse(localStorage.getItem(STORAGE_TASKS));
            return Array.isArray(saved) ? saved : initialTasks.map(task => ({ ...task }));
        } catch {
            return initialTasks.map(task => ({ ...task }));
        }
    }

    function saveTasks() {
        try { localStorage.setItem(STORAGE_TASKS, JSON.stringify(tasks)); }
        catch (error) { console.warn("Không thể lưu danh sách công việc:", error); }
    }

    // Hiển thị ngày hiện tại ở thanh header của _Layout.cshtml.
    function renderCurrentDate() {
        const dateElement = $("currentDate");
        if (!dateElement) return;
        dateElement.textContent = new Intl.DateTimeFormat("vi-VN", {
            weekday: "long", day: "2-digit", month: "2-digit", year: "numeric"
        }).format(new Date());
    }

    // Tạo 7 ngày trong tuần, đánh dấu ngày hôm nay.
    function renderWeek() {
        const weekElement = $("week");
        if (!weekElement) return;
        const today = new Date();
        const monday = new Date(today);
        monday.setHours(0, 0, 0, 0);
        monday.setDate(today.getDate() - ((today.getDay() + 6) % 7));
        const labels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
        weekElement.replaceChildren();
        labels.forEach((label, index) => {
            const date = new Date(monday);
            date.setDate(monday.getDate() + index);
            const item = document.createElement("div");
            item.className = `day${date.toDateString() === today.toDateString() ? " t" : ""}`;
            const name = document.createElement("span");
            name.textContent = label;
            const number = document.createElement("b");
            number.textContent = String(date.getDate()).padStart(2, "0");
            item.append(name, number);
            weekElement.append(item);
        });
    }

    // Vẽ danh sách bằng DOM/textContent để nội dung người dùng nhập không bị hiểu thành HTML.
    function renderTasks() {
        const container = $("tasks");
        if (!container) return;
        container.replaceChildren();
        if (tasks.length === 0) {
            const empty = document.createElement("p");
            empty.className = "mut";
            empty.textContent = "Chưa có công việc. Hãy thêm việc đầu tiên của bạn!";
            container.append(empty);
        }
        tasks.forEach(task => {
            const row = document.createElement("div");
            row.className = `row${task.done ? " d" : ""}`;
            const toggle = document.createElement("button");
            toggle.type = "button";
            toggle.className = `cb${task.done ? " d" : ""}`;
            toggle.dataset.taskId = String(task.id);
            toggle.setAttribute("aria-label", task.done ? "Đánh dấu chưa hoàn thành" : "Đánh dấu hoàn thành");
            toggle.setAttribute("aria-pressed", String(Boolean(task.done)));
            toggle.textContent = task.done ? "✓" : "";
            const title = document.createElement("span");
            title.className = "n";
            title.textContent = task.title;
            row.append(toggle, title);
            if (!task.done) {
                const dot = document.createElement("span");
                dot.className = "pt";
                dot.style.background = task.priority === "high" ? "var(--bad)" : task.priority === "medium" ? "var(--warn)" : "var(--ok)";
                dot.setAttribute("aria-hidden", "true");
                row.append(dot);
            }
            container.append(row);
        });
        const count = tasks.filter(task => !task.done).length;
        const countElement = $("openCnt");
        if (countElement) countElement.textContent = String(count);
    }

    // Thêm công việc mới vào đầu danh sách và lưu lại trên trình duyệt.
    function bindTaskForm() {
        const form = $("addT");
        const input = $("qi");
        if (!form || !input) return;
        form.addEventListener("submit", event => {
            event.preventDefault();
            const title = input.value.trim();
            if (!title) { input.focus(); return; }
            tasks.unshift({ id: Date.now(), title, done: false, priority: "normal" });
            saveTasks();
            renderTasks();
            input.value = "";
            input.focus();
        });
        // Nút trong Hero đưa con trỏ thẳng đến ô thêm việc.
        document.querySelectorAll('[data-focus-task], .hero .btn').forEach(button => {
            button.addEventListener("click", () => input.focus());
        });
    }

    // Đánh dấu hoàn thành/chưa hoàn thành bằng event delegation.
    function bindTaskActions() {
        const container = $("tasks");
        if (!container) return;
        container.addEventListener("click", event => {
            const button = event.target.closest("button[data-task-id]");
            if (!button) return;
            const task = tasks.find(item => String(item.id) === button.dataset.taskId);
            if (!task) return;
            task.done = !task.done;
            saveTasks();
            renderTasks();
        });
    }

    // Áp dụng theme đã lưu; nếu chưa có thì dùng cài đặt màu của hệ điều hành.
    function applyTheme(theme) {
        const selected = theme === "dark" ? "dark" : "light";
        document.documentElement.dataset.theme = selected;
        const button = $("themeToggle");
        if (button) {
            button.textContent = selected === "dark" ? "Chế độ sáng" : "Chế độ tối";
            button.setAttribute("aria-label", selected === "dark" ? "Chuyển sang giao diện sáng" : "Chuyển sang giao diện tối");
            button.setAttribute("aria-pressed", String(selected === "dark"));
        }
    }

    function bindThemeToggle() {
        const button = $("themeToggle");
        let savedTheme = null;
        try { savedTheme = localStorage.getItem(STORAGE_THEME); } catch { /* Bỏ qua nếu trình duyệt chặn storage. */ }
        const systemDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
        applyTheme(savedTheme || (systemDark ? "dark" : "light"));
        if (!button) return;
        button.addEventListener("click", () => {
            const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
            applyTheme(next);
            try { localStorage.setItem(STORAGE_THEME, next); } catch { /* Không ảnh hưởng chức năng đổi theme. */ }
        });
    }

    // Khởi chạy toàn bộ chức năng khi DOM đã sẵn sàng.
    function init() {
        renderCurrentDate();
        renderWeek();
        renderTasks();
        bindTaskForm();
        bindTaskActions();
        bindThemeToggle();
    }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init, { once: true });
    else init();
})();
