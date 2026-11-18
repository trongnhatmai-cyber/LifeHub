/* =============================================
   LIFEHUB - STUDY PAGES (Môn học, Thời khóa biểu, Bài tập, Điểm số, Tài liệu)

   Vai trò của JavaScript: chỉ xử lý tương tác giao diện.
   - Mở/đóng hộp thoại và điền sẵn form khi sửa, xóa, nhập điểm, đặt mục tiêu.
   - Ẩn/hiện ô chỉ dùng cho bài thi, kiểm tra giờ bắt đầu và kết thúc.
   - Tự gửi form lọc (GET) khi đổi lựa chọn.

   Không có dữ liệu học tập trong file này. Lọc, đếm, GPA, tiến độ đều do C# (Controller + ViewModel) tính.
   Form thêm/sửa/xóa hiện CHỈ MÔ PHỎNG giao diện, chưa lưu dữ liệu.
============================================= */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    const page = document.querySelector(".study-page[data-module]");
    if (!page) return;

    // ---------- Thông báo nhỏ ----------
    let toastEl = null;
    let toastTimer = null;

    function toast(message) {
        if (!toastEl) {
            toastEl = document.createElement("div");
            toastEl.className = "study-toast";
            toastEl.setAttribute("role", "status");
            page.appendChild(toastEl);
        }
        toastEl.textContent = message;
        toastEl.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => { toastEl.hidden = true; }, 3800);
    }

    // ---------- Hộp thoại ----------
    function openModal(modal) {
        modal.hidden = false;
        document.body.classList.add("study-modal-open");
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled), select:not(:disabled)");
        if (first) first.focus();
    }

    function closeModal(modal) {
        modal.hidden = true;
        if (!document.querySelector(".study-modal:not([hidden])")) document.body.classList.remove("study-modal-open");
    }

    // Khóa data-* dành riêng, không điền vào ô nhập. "assignmenttitle" điền vào ô Title
    // (vì data-title đã dùng cho tiêu đề hộp thoại).
    const RESERVED = new Set(["open", "mode", "action", "title"]);
    const ALIAS = { assignmenttitle: "title" };

    function syncExamFields(form) {
        const kind = form.querySelector('[name="Kind"]');
        const isExam = !!kind && (kind.value === "midterm" || kind.value === "final");
        form.querySelectorAll("[data-exam-only]").forEach(el => { el.hidden = !isExam; });
    }

    function prepareForm(modal, trigger) {
        const mode = trigger.dataset.mode || "create";
        const form = modal.querySelector("form");

        const titleEl = modal.querySelector("[data-modal-title]");
        if (titleEl && trigger.dataset.title) titleEl.textContent = trigger.dataset.title;

        if (form) {
            if (!form.dataset.defaultAction) form.dataset.defaultAction = form.getAttribute("action") || "";
            form.reset();
            form.setAttribute("action", mode === "edit" && trigger.dataset.action ? trigger.dataset.action : form.dataset.defaultAction);

            // Ô Id chỉ gửi đi khi sửa hoặc xóa
            const idInput = form.querySelector('[name="Id"]');
            if (idInput) idInput.disabled = !(mode === "edit" || mode === "delete");

            const fields = Array.from(form.elements);
            Object.entries(trigger.dataset).forEach(([rawKey, value]) => {
                if (RESERVED.has(rawKey)) return;
                const key = ALIAS[rawKey] || rawKey;
                const field = fields.find(f => f.name && f.name.toLowerCase() === key);
                if (field) field.value = value;
            });

            syncExamFields(form);
            const error = form.querySelector(".form-error");
            if (error) error.hidden = true;
        }

        Object.entries(trigger.dataset).forEach(([key, value]) => {
            modal.querySelectorAll(`[data-text="${key}"]`).forEach(el => { el.textContent = value; });
        });
    }

    document.addEventListener("click", event => {
        const opener = event.target.closest("[data-open]");
        if (opener) {
            const modal = document.getElementById(opener.dataset.open);
            if (modal) {
                prepareForm(modal, opener);
                openModal(modal);
            }
            return;
        }

        const closer = event.target.closest("[data-close]");
        if (closer) {
            const modal = closer.closest(".study-modal");
            if (modal) closeModal(modal);
            return;
        }

        if (event.target.classList.contains("study-modal")) closeModal(event.target);
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") document.querySelectorAll(".study-modal:not([hidden])").forEach(closeModal);
    });

    document.querySelectorAll('.study-modal [name="Kind"]').forEach(select => {
        select.addEventListener("change", () => syncExamFields(select.form));
    });

    // ---------- Kiểm tra form phía trình duyệt ----------
    function validate(form) {
        if (form.dataset.validate === "slot") {
            const start = form.querySelector('[name="Start"]').value;
            const end = form.querySelector('[name="End"]').value;
            if (start && end && end <= start) return "Giờ kết thúc phải sau giờ bắt đầu.";
        }
        return "";
    }

    // ---------- Gửi form (MÔ PHỎNG, chưa lưu dữ liệu) ----------
    // BACK-END: khi action POST tương ứng đã làm xong, xóa khối này để form gửi thẳng lên server theo asp-action.
    document.querySelectorAll("form[data-simulate]").forEach(form => {
        form.addEventListener("submit", event => {
            event.preventDefault();

            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const message = validate(form);
            const error = form.querySelector(".form-error");
            if (message) {
                if (error) { error.textContent = message; error.hidden = false; }
                return;
            }
            if (error) error.hidden = true;

            const modal = form.closest(".study-modal");
            if (modal) closeModal(modal);
            toast("Bản mô phỏng giao diện: dữ liệu chưa được lưu.");
        });
    });

    // ---------- Form lọc (GET): tự gửi khi đổi lựa chọn ----------
    document.querySelectorAll("[data-autosubmit]").forEach(el => {
        el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); });
    });
});
