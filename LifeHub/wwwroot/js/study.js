// =========================================
// STUDY MANAGEMENT - JAVASCRIPT
// Chỉ xử lý tương tác giao diện: hộp thoại, bộ lọc bảng bài tập, thông báo.
// Dữ liệu do C# (StudyController + ViewModel) render sẵn trong View.
// =========================================

document.addEventListener("DOMContentLoaded", function () {
    const byId = (id) => document.getElementById(id);

    // ---------- Thông báo nhỏ (toast) ----------
    const toast = byId("studyToast");
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
        document.body.classList.add("study-modal-open");
        const first = modal.querySelector("input:not([type=hidden]), select");
        if (first) first.focus();
    }

    function closeModal(modal) {
        if (!modal) return;
        modal.hidden = true;
        if (!document.querySelector(".study-modal:not([hidden])")) {
            document.body.classList.remove("study-modal-open");
        }
    }

    const courseModal = byId("courseModal");
    const assignmentModal = byId("assignmentModal");

    const openers = [
        ["addSubjectBtn", courseModal],
        ["addAssignmentBtn", assignmentModal],
        ["addAssignmentBtn2", assignmentModal]
    ];
    openers.forEach(function (pair) {
        const button = byId(pair[0]);
        if (button) button.addEventListener("click", function () { openModal(pair[1]); });
    });

    document.querySelectorAll(".study-modal").forEach(function (modal) {
        // Nút Hủy / dấu ×
        modal.querySelectorAll("[data-close-modal]").forEach(function (btn) {
            btn.addEventListener("click", function () { closeModal(modal); });
        });
        // Bấm vào nền mờ để đóng
        modal.addEventListener("mousedown", function (e) {
            if (e.target === modal) closeModal(modal);
        });
    });

    document.addEventListener("keydown", function (e) {
        if (e.key !== "Escape") return;
        document.querySelectorAll(".study-modal:not([hidden])").forEach(closeModal);
    });

    // ---------- Form bài tập: chỉ hiện "thời lượng / phòng thi" khi là bài thi ----------
    const kindSelect = byId("asgKind");

    function syncExamFields() {
        if (!kindSelect) return;
        const isExam = kindSelect.value === "midterm" || kindSelect.value === "final";
        document.querySelectorAll("[data-exam-only]").forEach(function (el) { el.hidden = !isExam; });
    }
    if (kindSelect) kindSelect.addEventListener("change", syncExamFields);

    // ---------- Bộ lọc bảng bài tập (lọc hàng đã render sẵn) ----------
    const chips = document.querySelectorAll(".study-chip");
    const rows = document.querySelectorAll("#assignmentBody tr[data-status]");
    const emptyRow = byId("assignmentEmpty");

    function rowMatches(row, filter) {
        const status = row.dataset.status;
        const isExam = row.dataset.kind === "exam";
        switch (filter) {
            case "upcoming": return status === "upcoming" && !isExam;
            case "overdue": return status === "overdue";
            case "done": return status === "graded" || status === "submitted";
            case "exam": return isExam;
            default: return true;
        }
    }

    function applyFilter(filter) {
        let visible = 0;
        rows.forEach(function (row) {
            const ok = rowMatches(row, filter);
            row.hidden = !ok;
            if (ok) visible++;
        });
        if (emptyRow) emptyRow.hidden = visible > 0;

        chips.forEach(function (chip) {
            const active = chip.dataset.filter === filter;
            chip.classList.toggle("active", active);
            chip.setAttribute("aria-pressed", active ? "true" : "false");
        });
    }

    chips.forEach(function (chip) {
        chip.addEventListener("click", function () { applyFilter(chip.dataset.filter); });
    });
});
