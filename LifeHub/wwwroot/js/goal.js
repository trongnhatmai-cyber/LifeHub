/* =====================================================================
   LIFEHUB - GOAL MANAGEMENT  |  wwwroot/js/goal.js
   Chép từ study.js: JS chỉ xử lý tương tác giao diện, KHÔNG tính tiến độ hay đếm dữ liệu (việc của C#).
   Form thêm / sửa / xóa hiện CHỈ MÔ PHỎNG (data-simulate), chưa có action POST.

   QUY ƯỚC HTML
   - Nút mở modal:  data-open="<id modal>" data-mode="create|edit|delete" data-title="Tiêu đề modal"
                    data-action="<url POST>"  (ghi đè action của form, dùng cho sửa/xóa)
                    Các data-* còn lại tự điền vào ô có name trùng (không phân biệt hoa thường).
                    Ngoại lệ: data-goaltitle và data-krtitle -> ô name="Title" (data-title đã dùng cho tiêu đề modal).
   - Ô Id ẩn:       <input type="hidden" name="Id" disabled> chỉ bật khi mode = edit | delete.
   - Chữ trong modal: <strong data-text="name"> lấy từ data-name của nút mở.
   - Ẩn ô tiến độ nhập tay khi mục tiêu đã có Key Result: .field[data-manual-progress] + data-haskr="true".
   - Form lọc GET:  data-autosubmit trên select/input -> tự gửi khi đổi.
   - Toast:         [data-toast] tự ẩn sau vài giây.
   ===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
    "use strict";
    const page = document.querySelector(".goal-page");
    if (!page) return;

    // 1. TOAST do server render (TempData)
    page.querySelectorAll("[data-toast]").forEach(el => setTimeout(() => { el.hidden = true; }, 4200));

    // 2. MODAL
    function openModal(modal) {
        modal.hidden = false;
        document.body.classList.add("goal-modal-open");
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled), select:not(:disabled), textarea");
        if (first) first.focus();
    }
    function closeModal(modal) {
        modal.hidden = true;
        if (!document.querySelector(".goal-modal:not([hidden])")) document.body.classList.remove("goal-modal-open");
    }

    // Dấu * cho ô bắt buộc
    document.querySelectorAll(".goal-modal [required]").forEach(el => {
        const field = el.closest(".field");
        if (field) field.classList.add("required");
    });
    const clearInvalid = form => form.querySelectorAll(".field.invalid").forEach(f => f.classList.remove("invalid"));
    document.addEventListener("input", e => {
        const f = e.target.closest && e.target.closest(".field.invalid");
        if (f) f.classList.remove("invalid");
    });

    const RESERVED = new Set(["open", "mode", "action", "title", "haskr"]);
    const ALIAS = { goaltitle: "title", krtitle: "title" };

    function prepareForm(modal, trigger) {
        const mode = trigger.dataset.mode || "create";
        const form = modal.querySelector("form");
        const titleEl = modal.querySelector("[data-modal-title]");
        if (titleEl && trigger.dataset.title) titleEl.textContent = trigger.dataset.title;

        if (form) {
            if (!form.dataset.defaultAction) form.dataset.defaultAction = form.getAttribute("action") || "";
            form.reset();
            form.setAttribute("action", trigger.dataset.action || form.dataset.defaultAction);

            const idInput = form.querySelector('[name="Id"]');
            if (idInput) idInput.disabled = !(mode === "edit" || mode === "delete");

            const fields = Array.from(form.elements);
            Object.entries(trigger.dataset).forEach(([rawKey, value]) => {
                if (RESERVED.has(rawKey)) return;
                const key = ALIAS[rawKey] || rawKey;
                const field = fields.find(f => f.name && f.name.toLowerCase() === key);
                if (field) field.value = value;
            });

            // Mục tiêu đã có Key Result: tiến độ do hệ thống tính, ẩn ô nhập tay.
            form.querySelectorAll("[data-manual-progress]").forEach(el => {
                const lock = trigger.dataset.haskr === "true";
                el.hidden = lock;
                const input = el.querySelector("input");
                if (input) input.disabled = lock;
            });

            clearInvalid(form);
            const err = form.querySelector(".form-error");
            if (err) err.hidden = true;
        }
        Object.entries(trigger.dataset).forEach(([key, value]) => {
            modal.querySelectorAll(`[data-text="${key}"]`).forEach(el => { el.textContent = value; });
        });
    }

    document.addEventListener("click", e => {
        const opener = e.target.closest("[data-open]");
        if (opener) {
            const modal = document.getElementById(opener.dataset.open);
            if (modal) { prepareForm(modal, opener); openModal(modal); }
            return;
        }
        const closer = e.target.closest("[data-close]");
        if (closer) {
            const modal = closer.closest(".goal-modal");
            if (modal) closeModal(modal);
            return;
        }
        if (e.target.classList.contains("goal-modal")) closeModal(e.target);
    });
    document.addEventListener("keydown", e => {
        if (e.key === "Escape") document.querySelectorAll(".goal-modal:not([hidden])").forEach(closeModal);
    });

    // 3. GỬI FORM (MÔ PHỎNG, CHƯA LƯU DỮ LIỆU)
    // BACK-END: khi có action POST, bỏ data-simulate khỏi form để gửi thật. Khối này không còn tác dụng.
    function toast(message) {
        let stack = page.querySelector(".goal-toast-stack");
        if (!stack) { stack = document.createElement("div"); stack.className = "goal-toast-stack"; page.appendChild(stack); }
        const el = document.createElement("div");
        el.className = "goal-toast"; el.setAttribute("role", "status"); el.textContent = message;
        stack.appendChild(el);
        setTimeout(() => el.remove(), 3800);
    }
    document.addEventListener("submit", e => {
        const form = e.target;
        if (!form.hasAttribute || !form.hasAttribute("data-simulate")) return;
        e.preventDefault();
        let box = form.querySelector(".form-error");
        clearInvalid(form);
        if (form.matches("[data-validate]")) {
            if (!box) {
                box = document.createElement("p");
                box.className = "form-error"; box.setAttribute("role", "alert"); box.hidden = true;
                form.insertBefore(box, form.querySelector(".goal-modal-foot"));
            }
            if (!form.checkValidity()) {
                const bad = Array.from(form.elements).filter(el => el.willValidate && !el.checkValidity());
                const names = bad.map(el => {
                    const field = el.closest(".field");
                    if (field) field.classList.add("invalid");
                    const label = field && field.querySelector("label");
                    return label ? label.textContent.replace("*", "").trim() : el.name;
                });
                box.textContent = "Chưa nhập đúng: " + names.join(", ") + ".";
                box.hidden = false;
                if (bad[0]) bad[0].focus();
                return;
            }
            if (form.dataset.validate === "goal") {
                const s0 = form.querySelector('[name="StartDate"]').value;
                const t0 = form.querySelector('[name="TargetDate"]').value;
                if (s0 && t0 && t0 < s0) { box.textContent = "Ngày mục tiêu phải sau hoặc bằng ngày bắt đầu."; box.hidden = false; return; }
            }
            box.hidden = true;
        }
        const modal = form.closest(".goal-modal");
        if (modal) closeModal(modal);
        toast("Bản mô phỏng giao diện: dữ liệu chưa được lưu.");
    });

    // 4. FORM LỌC GET tự gửi khi đổi lựa chọn
    document.querySelectorAll("[data-autosubmit]").forEach(el => {
        el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); });
    });
});
