/* =============================================
   LIFEHUB - FINANCE MODULES (Giao dịch, Ví, Mục tiêu tiết kiệm, Báo cáo)

   Vai trò của JavaScript: chỉ xử lý tương tác giao diện.
   - Mở/đóng hộp thoại, điền sẵn form khi sửa/xóa/nạp tiền.
   - Lọc danh mục theo loại giao dịch, kiểm tra form phía trình duyệt.
   - Tự gửi form lọc (GET) khi đổi lựa chọn.

   Không có dữ liệu tài chính trong file này. Lọc, tổng thu/chi, số dư, tiến độ, so sánh ngân sách
   đều do C# (Controller + ViewModel) tính. Form thêm/sửa/xóa hiện CHỈ MÔ PHỎNG giao diện, chưa lưu.
============================================= */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    const page = document.querySelector(".finance-page[data-module]");
    if (!page) return;

    // ---------- Thông báo nhỏ ----------
    let toastEl = null;
    let toastTimer = null;

    function toast(message) {
        if (!toastEl) {
            toastEl = document.createElement("div");
            toastEl.className = "finance-toast";
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
        modal.classList.add("show");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("modal-open");
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled):not([readonly]), select:not(:disabled)");
        if (first) first.focus();
    }

    function closeModal(modal) {
        modal.classList.remove("show");
        modal.setAttribute("aria-hidden", "true");
        if (!document.querySelector(".finance-modal.show")) document.body.classList.remove("modal-open");
    }

    const RESERVED = new Set(["open", "mode", "action", "title"]);

    // Điền form từ thuộc tính data-* của nút bấm. data-categoryid điền vào ô name="CategoryId" (không phân biệt hoa thường).
    function prepareForm(modal, trigger) {
        const mode = trigger.dataset.mode || "create";
        const form = modal.querySelector("form");

        const titleEl = modal.querySelector("[data-modal-title]");
        if (titleEl && trigger.dataset.title) titleEl.textContent = trigger.dataset.title;

        if (form) {
            if (!form.dataset.defaultAction) form.dataset.defaultAction = form.getAttribute("action") || "";
            form.reset();
            form.setAttribute("action", mode === "edit" && trigger.dataset.action ? trigger.dataset.action : form.dataset.defaultAction);

            const idInput = form.querySelector('[name="Id"]');
            if (idInput) idInput.disabled = !(mode === "edit" || mode === "delete");

            // Ô chỉ dùng khi thêm mới (VD: số tiền đã có của mục tiêu)
            form.querySelectorAll("[data-create-only]").forEach(wrap => {
                wrap.hidden = mode === "edit";
                wrap.querySelectorAll("input, select").forEach(i => { i.disabled = mode === "edit"; });
            });

            const fields = Array.from(form.elements);
            Object.entries(trigger.dataset).forEach(([key, value]) => {
                if (RESERVED.has(key)) return;
                const field = fields.find(f => f.name && f.name.toLowerCase() === key.toLowerCase());
                if (field) field.value = value;
            });

            form.querySelectorAll('[data-default="today"]').forEach(input => {
                if (!input.value) input.value = localDate();
            });

            const error = form.querySelector(".form-error");
            if (error) error.hidden = true;
            syncCategoryOptions(form);
        }

        Object.entries(trigger.dataset).forEach(([key, value]) => {
            modal.querySelectorAll(`[data-text="${key}"]`).forEach(el => { el.textContent = value; });
        });
    }

    function localDate(date = new Date()) {
        const m = String(date.getMonth() + 1).padStart(2, "0");
        const d = String(date.getDate()).padStart(2, "0");
        return `${date.getFullYear()}-${m}-${d}`;
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
            const modal = closer.closest(".finance-modal");
            if (modal) closeModal(modal);
            return;
        }

        if (event.target.classList.contains("finance-modal")) closeModal(event.target);
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") document.querySelectorAll(".finance-modal.show").forEach(closeModal);
    });

    // ---------- Danh mục theo loại giao dịch ----------
    function syncCategoryOptions(form) {
        const type = form.querySelector('[name="Type"]');
        const category = form.querySelector('[name="CategoryId"]');
        if (!type || !category) return;

        Array.from(category.options).forEach(option => {
            if (!option.value) return;
            const match = option.dataset.type === type.value;
            option.hidden = !match;
            option.disabled = !match;
        });
        const selected = category.options[category.selectedIndex];
        if (!selected || selected.disabled) category.value = "";
    }

    document.querySelectorAll('.finance-modal form [name="Type"]').forEach(select => {
        select.addEventListener("change", () => syncCategoryOptions(select.form));
    });

    // ---------- Kiểm tra form phía trình duyệt ----------
    function validate(form) {
        switch (form.dataset.validate) {
            case "transfer": {
                const from = form.querySelector('[name="FromWalletId"]');
                const to = form.querySelector('[name="ToWalletId"]');
                if (from.value === to.value) return "Ví nguồn và ví đích phải khác nhau.";
                return enough(form, from);
            }
            case "contribute":
                return enough(form, form.querySelector('[name="WalletId"]'));
            default:
                return "";
        }
    }

    // Số dư ví lấy từ data-balance do server render, chỉ dùng để cảnh báo.
    function enough(form, walletSelect) {
        const option = walletSelect.options[walletSelect.selectedIndex];
        const amount = Number(form.querySelector('[name="Amount"]').value);
        const balance = Number(option?.dataset.balance);
        if (Number.isFinite(balance) && amount > balance) return "Số dư của ví nguồn không đủ.";
        return "";
    }

    // ---------- Gửi form (MÔ PHỎNG, chưa lưu dữ liệu) ----------
    // BACK-END: khi action POST tương ứng đã làm xong, xóa khối này để form gửi thẳng lên server theo asp-action.
    document.querySelectorAll(".finance-modal form[data-simulate]").forEach(form => {
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

            closeModal(form.closest(".finance-modal"));
            toast("Bản mô phỏng giao diện: dữ liệu chưa được lưu.");
        });
    });

    // ---------- Form lọc (GET): tự gửi khi đổi lựa chọn ----------
    document.querySelectorAll("[data-autosubmit]").forEach(el => {
        el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); });
    });
});