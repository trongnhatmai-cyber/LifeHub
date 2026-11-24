/* =====================================================================
   LIFEHUB - LIFESTYLE (SINH HOẠT CÁ NHÂN)  |  wwwroot/js/lifestyle.js
   Chép từ habit.js: JS chỉ xử lý tương tác giao diện, KHÔNG tính thống kê hay tiến độ (việc của C#).
   Form ghi nhận / sửa / xóa / áp dụng kế hoạch hiện CHỈ MÔ PHỎNG (data-simulate), chưa có action POST.

   QUY ƯỚC HTML
   - Nút mở modal:  data-open="<id modal>" data-mode="create|edit|delete" data-title="Tiêu đề modal"
                    Các data-* còn lại tự điền vào ô có name trùng (không phân biệt hoa thường),
                    VD data-sleepdate="2026-11-23" -> <input name="SleepDate">.
   - Ô Id ẩn:       <input type="hidden" name="Id" disabled> chỉ bật khi mode = edit | delete.
   - Chữ trong modal: <strong data-text="name"> lấy từ data-name của nút mở.
   - Form lọc GET:  data-autosubmit trên select/input -> tự gửi khi đổi.
   - Trình sửa dòng hoạt động của kế hoạch: <template data-row-template>, nút data-add-row / data-remove-row.
   - Toast:         [data-toast] tự ẩn sau vài giây.
   ===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
    "use strict";
    const page = document.querySelector(".life-page");
    if (!page) return;

    // 1. TOAST do server render (TempData)
    page.querySelectorAll("[data-toast]").forEach(el => setTimeout(() => { el.hidden = true; }, 4200));

    // 2. MODAL
    function openModal(modal) {
        modal.hidden = false;
        document.body.classList.add("life-modal-open");
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled), select:not(:disabled), textarea");
        if (first) first.focus();
    }
    function closeModal(modal) {
        modal.hidden = true;
        if (!document.querySelector(".life-modal:not([hidden])")) document.body.classList.remove("life-modal-open");
    }

    // Dấu * cho ô bắt buộc
    document.querySelectorAll(".life-modal [required]").forEach(el => {
        const field = el.closest(".field");
        if (field) field.classList.add("required");
    });
    const clearInvalid = form => form.querySelectorAll(".field.invalid").forEach(f => f.classList.remove("invalid"));
    document.addEventListener("input", e => {
        const f = e.target.closest && e.target.closest(".field.invalid");
        if (f) f.classList.remove("invalid");
    });

    const RESERVED = new Set(["open", "mode", "action", "title"]);
    const ALIAS = {};

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
            const modal = closer.closest(".life-modal");
            if (modal) closeModal(modal);
            return;
        }
        if (e.target.classList.contains("life-modal")) closeModal(e.target);
    });
    document.addEventListener("keydown", e => {
        if (e.key === "Escape") document.querySelectorAll(".life-modal:not([hidden])").forEach(closeModal);
    });

    // 3. GỬI FORM (MÔ PHỎNG, CHƯA LƯU DỮ LIỆU)
    // BACK-END: khi có action POST, bỏ data-simulate khỏi form để gửi thật. Khối này không còn tác dụng.
    function toast(message) {
        let stack = page.querySelector(".life-toast-stack");
        if (!stack) { stack = document.createElement("div"); stack.className = "life-toast-stack"; page.appendChild(stack); }
        const el = document.createElement("div");
        el.className = "life-toast"; el.setAttribute("role", "status"); el.textContent = message;
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
                form.insertBefore(box, form.querySelector(".life-modal-foot"));
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
            box.hidden = true;
        }
        const modal = form.closest(".life-modal");
        if (modal) closeModal(modal);
        toast("Bản mô phỏng giao diện: dữ liệu chưa được lưu.");
    });

    // 3b. TRÌNH SỬA DANH SÁCH HOẠT ĐỘNG TRONG KẾ HOẠCH (modal #planModal)
    // Dòng mới nhân bản từ <template data-row-template>, tên dạng Activities[i].Field khớp SavePlanInput.
    const planModal = document.getElementById("planModal");
    if (planModal) {
        const tpl = planModal.parentElement.querySelector("template[data-row-template]");
        const list = planModal.querySelector("[data-rows]");

        function renumber() {
            list.querySelectorAll("[data-row]").forEach((row, i) => {
                row.querySelectorAll("[name]").forEach(el => { el.name = el.name.replace(/Activities\[\d+\]/, `Activities[${i}]`); });
                row.querySelectorAll("[id]").forEach(el => { el.id = el.id.replace(/-\d+$/, `-${i}`); });
                row.querySelectorAll("label[for]").forEach(l => { l.htmlFor = l.htmlFor.replace(/-\d+$/, `-${i}`); });
                const rm = row.querySelector("[data-remove-row]");
                if (rm) rm.hidden = list.children.length === 1;      // luôn giữ ít nhất một hoạt động
            });
        }
        function addRow() {
            if (!tpl) return;
            const html = tpl.innerHTML.replace(/__i__/g, String(list.children.length));
            list.insertAdjacentHTML("beforeend", html);
            renumber();
            const last = list.lastElementChild && list.lastElementChild.querySelector("input");
            if (last && list.children.length > 1) last.focus();
        }
        function resetRows() { list.innerHTML = ""; addRow(); }

        planModal.addEventListener("click", e => {
            if (e.target.closest("[data-add-row]")) { addRow(); return; }
            const rm = e.target.closest("[data-remove-row]");
            if (rm && list.children.length > 1) { rm.closest("[data-row]").remove(); renumber(); }
        });
        // Mỗi lần mở modal: về đúng một dòng trống (sau khi form.reset() ở prepareForm).
        document.addEventListener("click", e => {
            const opener = e.target.closest("[data-open]");
            if (opener && opener.dataset.open === "planModal") resetRows();
        });
    }

    // 4. FORM LỌC GET tự gửi khi đổi lựa chọn
    document.querySelectorAll("[data-autosubmit]").forEach(el => {
        el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); });
    });
});
