// =========================================
// TASK MANAGEMENT - JAVASCRIPT
// Chỉ xử lý tương tác giao diện: hộp thoại, tìm kiếm/lọc, kéo thả Kanban, thông báo.
// Dữ liệu do C# (TaskController + ViewModel) render sẵn trong View.
// =========================================

document.addEventListener("DOMContentLoaded", function () {
    const byId = function (id) { return document.getElementById(id); };
    const allCards = function () { return Array.from(document.querySelectorAll(".task-card-item")); };

    // ---------- Thông báo nhỏ (toast) ----------
    const toast = byId("taskToast");
    let toastTimer = null;

    function showToast(message) {
        if (!toast) return;
        toast.textContent = message;
        toast.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { toast.hidden = true; }, 3500);
    }

    // ---------- Hộp thoại ----------
    function openModal(modal) {
        if (!modal) return;
        modal.hidden = false;
        document.body.classList.add("task-modal-open");
        const first = modal.querySelector("input:not([type=hidden]), select, textarea");
        if (first) first.focus();
    }

    function closeModal(modal) {
        if (!modal) return;
        modal.hidden = true;
        if (!document.querySelector(".task-modal:not([hidden])")) {
            document.body.classList.remove("task-modal-open");
        }
    }

    const taskModal = byId("taskModal");
    const createBtn = byId("createTaskBtn");
    if (createBtn) createBtn.addEventListener("click", function () { openModal(taskModal); });

    document.querySelectorAll(".task-modal").forEach(function (modal) {
        modal.querySelectorAll("[data-close-modal]").forEach(function (btn) {
            btn.addEventListener("click", function () { closeModal(modal); });
        });
        modal.addEventListener("mousedown", function (e) {
            if (e.target === modal) closeModal(modal);
        });
    });

    document.addEventListener("keydown", function (e) {
        if (e.key !== "Escape") return;
        document.querySelectorAll(".task-modal:not([hidden])").forEach(closeModal);
    });

    // ---------- Gửi form (MÔ PHỎNG, chưa lưu dữ liệu) ----------
    // BACK-END: khi action Create trong TaskController đã làm xong, xóa khối này
    // để form gửi thẳng lên server theo asp-action đã khai báo.
    document.querySelectorAll(".task-modal form").forEach(function (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            if (!form.reportValidity()) return;

            closeModal(form.closest(".task-modal"));
            form.reset();
            syncAssignee();
            showToast("Bản mô phỏng giao diện: dữ liệu chưa được lưu.");
        });
    });

    // ---------- Cập nhật số đếm và trạng thái rỗng ----------
    function setSummary(key, value, pad) {
        const el = document.querySelector('[data-summary="' + key + '"]');
        if (el) el.textContent = pad ? String(value).padStart(2, "0") : value;
    }

    function refreshCounts() {
        const counts = { todo: 0, in_progress: 0, review: 0, done: 0 };

        document.querySelectorAll(".kanban-column").forEach(function (col) {
            const key = col.dataset.column;
            const cards = Array.from(col.querySelectorAll(".task-card-item"));
            counts[key] = cards.length;

            const badge = col.querySelector("[data-column-count]");
            if (badge) badge.textContent = cards.length;

            const empty = col.querySelector(".column-empty");
            if (empty) empty.hidden = cards.some(function (c) { return !c.hidden; });
        });

        setSummary("total", counts.todo + counts.in_progress + counts.review + counts.done, false);
        setSummary("todo", counts.todo, true);
        setSummary("in_progress", counts.in_progress, true);
        setSummary("done", counts.done, true);
    }

    // ---------- Tìm kiếm và lọc (ẩn/hiện thẻ đã render sẵn) ----------
    const searchInput = byId("taskSearch");
    const prioritySelect = byId("taskPriorityFilter");
    const projectSelect = byId("taskProjectFilter");

    // Bỏ dấu tiếng Việt để gõ "doan" vẫn tìm ra "đồ án"
    function fold(text) {
        return (text || "").toLowerCase()
            .normalize("NFD").replace(/[\u0300-\u036f]/g, "")
            .replace(/đ/g, "d");
    }

    let activeScope = "all";   // all / personal / assigned / project

    function scopeMatches(card) {
        switch (activeScope) {
            case "personal": return !card.dataset.projectId;
            case "assigned": return card.dataset.assigned === "1";
            case "project": return !!card.dataset.projectId;
            default: return true;
        }
    }

    function applyFilters() {
        const q = fold(searchInput ? searchInput.value.trim() : "");
        const priority = prioritySelect ? prioritySelect.value : "";
        const project = projectSelect ? projectSelect.value : "";

        allCards().forEach(function (card) {
            const ok = scopeMatches(card)
                && (!q || fold(card.dataset.search).includes(q))
                && (!priority || card.dataset.priority === priority)
                && (!project || card.dataset.projectId === project);
            card.hidden = !ok;
        });
        refreshCounts();
    }

    if (searchInput) searchInput.addEventListener("input", applyFilters);
    if (prioritySelect) prioritySelect.addEventListener("change", applyFilters);
    if (projectSelect) projectSelect.addEventListener("change", applyFilters);

    // ---------- Tab phạm vi: Tất cả / Cá nhân / Được giao cho tôi / Trong dự án ----------
    const scopeButtons = document.querySelectorAll("[data-scope]");
    scopeButtons.forEach(function (btn) {
        btn.addEventListener("click", function () {
            activeScope = btn.dataset.scope;
            scopeButtons.forEach(function (b) {
                const on = b === btn;
                b.classList.toggle("active", on);
                b.setAttribute("aria-pressed", on ? "true" : "false");
            });
            applyFilters();
        });
    });

    // ---------- Chi tiết công việc + bình luận ----------
    const detailModal = byId("taskDetailModal");
    const detailPanels = Array.from(document.querySelectorAll("[data-detail-id]"));

    function openDetail(id) {
        detailPanels.forEach(function (panel) { panel.hidden = panel.dataset.detailId !== String(id); });
        openModal(detailModal);
    }

    allCards().forEach(function (card) {
        card.addEventListener("click", function (e) {
            if (e.target.closest("a")) return;   // bấm vào tên dự án thì đi sang trang dự án, không mở chi tiết
            openDetail(card.dataset.id);
        });
        card.addEventListener("keydown", function (e) {
            if (e.key === "Enter" && e.target === card) openDetail(card.dataset.id);
        });
    });

    // ---------- Form thêm việc: chỉ giao cho thành viên của dự án đã chọn ----------
    const projectField = byId("taskProject");
    const assigneeWrap = document.querySelector("[data-assignee-field]");
    const assigneeSelect = byId("taskAssignee");

    function syncAssignee() {
        if (!projectField || !assigneeSelect || !assigneeWrap) return;
        const projectId = projectField.value;
        assigneeWrap.hidden = !projectId;
        assigneeSelect.value = "";
        Array.from(assigneeSelect.options).forEach(function (opt) {
            if (!opt.dataset.project) return;
            const match = opt.dataset.project === projectId;
            opt.hidden = !match;
            opt.disabled = !match;
        });
    }
    if (projectField) projectField.addEventListener("change", syncAssignee);
    syncAssignee();

    // ---------- Kéo thả thẻ giữa các cột (MÔ PHỎNG, chưa lưu) ----------
    const zones = Array.from(document.querySelectorAll("[data-dropzone]"));
    let dragged = null;

    function moveCard(card, status, zone) {
        if (card.dataset.status === status) return;

        zone.insertBefore(card, zone.querySelector(".column-empty"));
        card.dataset.status = status;
        card.classList.toggle("is-done", status === "done");
        refreshCounts();

        const statusLabels = { todo: "Cần làm", in_progress: "Đang làm", review: "Chờ duyệt", done: "Đã xong" };
        const detailStatus = document.querySelector('[data-detail-id="' + card.dataset.id + '"] [data-detail-status]');
        if (detailStatus && statusLabels[status]) detailStatus.textContent = statusLabels[status];

        // BACK-END: thay dòng toast bằng fetch POST tới /Task/UpdateStatus
        // (gửi Id + Status kèm __RequestVerificationToken), nếu lỗi thì đưa thẻ về cột cũ.
        showToast("Bản mô phỏng: trạng thái chưa được lưu, tải lại trang sẽ trở về như cũ.");
    }

    allCards().forEach(function (card) {
        card.addEventListener("dragstart", function (e) {
            dragged = card;
            card.classList.add("dragging");
            e.dataTransfer.effectAllowed = "move";
            e.dataTransfer.setData("text/plain", card.dataset.id);
        });
        card.addEventListener("dragend", function () {
            card.classList.remove("dragging");
            dragged = null;
            zones.forEach(function (z) { z.classList.remove("drag-over"); });
        });
    });

    zones.forEach(function (zone) {
        zone.addEventListener("dragover", function (e) {
            if (!dragged) return;
            e.preventDefault();
            zone.classList.add("drag-over");
        });
        zone.addEventListener("dragleave", function (e) {
            if (!zone.contains(e.relatedTarget)) zone.classList.remove("drag-over");
        });
        zone.addEventListener("drop", function (e) {
            e.preventDefault();
            zone.classList.remove("drag-over");
            if (dragged) moveCard(dragged, zone.dataset.dropzone, zone);
        });
    });
});