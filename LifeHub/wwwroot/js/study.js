/* =====================================================================
   LIFEHUB - STUDY MANAGEMENT  |  wwwroot/js/study.js   (MỘT FILE DUY NHẤT)

   Thay thế cả study.js cũ và study-pages.js. Sau khi chép file này vào:
   - Xóa wwwroot/js/study-pages.js.
   - Mọi View trong Views/Study đều nạp đúng một dòng:
       <script src="~/js/study.js" asp-append-version="true"></script>

   VAI TRÒ (đúng quy ước MVC + Mock Data của LIFEHUB)
   JavaScript CHỈ xử lý tương tác giao diện:
     1. Mở / đóng hộp thoại (modal), điền sẵn form khi sửa.
     2. Ẩn / hiện ô chỉ dùng cho bài thi.
     3. Kiểm tra form phía trình duyệt (giờ, ngày kết thúc phải sau ngày bắt đầu).
     4. Lọc nhanh bảng bài tập ở trang Tổng quan (lọc hàng đã được Razor render sẵn).
     5. Tự gửi form lọc (GET) khi đổi lựa chọn.
     6. Thông báo nhỏ (toast).
   JS KHÔNG chứa dữ liệu học tập, không tính GPA / tiến độ / đếm bài. Việc đó do C# làm.
   Form thêm / sửa / xóa hiện CHỈ MÔ PHỎNG, chưa lưu dữ liệu.

   QUY ƯỚC HTML MÀ FILE NÀY HIỂU (dùng cho mọi phân hệ sau)
   - Nút mở modal:   data-open="<id modal>"  data-mode="create|edit|delete"
                     data-title="Tiêu đề modal"  data-action="<url POST khi sửa>"
                     Các data-* còn lại tự điền vào ô input có name trùng (không phân biệt hoa thường):
                       data-code="IT301"  -> <input name="Code">
                       data-targetscore   -> <input name="TargetScore">
                     Ngoại lệ: data-assignmenttitle -> ô name="Title" (vì data-title đã dùng cho tiêu đề modal).
   - Tiêu đề modal:  phần tử có thuộc tính data-modal-title.
   - Hiển thị chữ trong modal: <strong data-text="name"> được điền từ data-name của nút mở.
   - Nút đóng:       data-close  (vẫn chấp nhận data-close-modal cũ).
   - Ô Id ẩn:        <input type="hidden" name="Id" disabled> -> JS chỉ bật khi mode = edit hoặc delete.
   - Ô chỉ cho bài thi: thêm thuộc tính data-exam-only vào .field, form phải có select name="Kind".
   - Form mô phỏng:  <form data-simulate> -> chặn gửi và hiện toast. Khi có back-end, BỎ data-simulate.
   - Kiểm tra thêm:  <form data-validate="slot|semester"> (xem hàm validate).
   - Form lọc GET:   select/input có data-autosubmit sẽ tự gửi form khi đổi.
   - Lọc nhanh Tổng quan: nút .study-chip[data-filter] + <tbody id="assignmentBody"> với
                     <tr data-status data-kind> + <tr id="assignmentEmpty">.

   LƯU Ý CSS: modal dùng position: fixed nên KHÔNG đặt bên trong .study-card / .summary-card
   (các thẻ này có hiệu ứng hover transform, sẽ làm modal lệch vị trí). Hãy đặt modal
   là con trực tiếp của .study-page.
===================================================================== */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    // Chỉ chạy trong trang học tập.
    const page = document.querySelector(".study-page");
    if (!page) return;

    // ================================================================
    // 1. THÔNG BÁO NHỎ (TOAST)
    // Dùng phần tử #studyToast nếu View có sẵn, nếu không thì tự tạo.
    // ================================================================
    let toastEl = document.getElementById("studyToast");
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

    // ================================================================
    // 2. MỞ / ĐÓNG MODAL
    // ================================================================
    function openModal(modal) {
        modal.hidden = false;
        document.body.classList.add("study-modal-open");   // khóa cuộn trang phía sau
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled), select:not(:disabled)");
        if (first) first.focus();
    }

    function closeModal(modal) {
        modal.hidden = true;
        // Chỉ mở khóa cuộn khi không còn modal nào đang mở.
        if (!document.querySelector(".study-modal:not([hidden])")) {
            document.body.classList.remove("study-modal-open");
        }
    }

    // ================================================================
    // 2b. NHẮC NHỞ Ô BẮT BUỘC
    // Ô nào có thuộc tính required thì tự thêm class "required" cho .field,
    // CSS sẽ hiện dấu * đỏ sau nhãn. Không cần sửa View.
    // ================================================================
    document.querySelectorAll(".study-modal [required]").forEach(el => {
        const field = el.closest(".field");
        if (field) field.classList.add("required");
    });

    function clearInvalid(form) {
        form.querySelectorAll(".field.invalid").forEach(f => f.classList.remove("invalid"));
    }

    // Lấy (hoặc tạo) dòng báo lỗi của form.
    function errorBox(form) {
        let box = form.querySelector(".form-error");
        if (!box) {
            box = document.createElement("p");
            box.className = "form-error";
            box.setAttribute("role", "alert");
            box.hidden = true;
            const foot = form.querySelector(".study-modal-foot");
            form.insertBefore(box, foot || null);
        }
        return box;
    }

    // Khi gõ vào ô đang báo lỗi thì gỡ viền đỏ của ô đó.
    document.addEventListener("input", event => {
        const field = event.target.closest && event.target.closest(".field.invalid");
        if (field) field.classList.remove("invalid");
    });

    // ================================================================
    // 3. CHUẨN BỊ FORM TRƯỚC KHI MỞ (thêm / sửa / xóa)
    // ================================================================
    // data-* dành riêng, không điền vào ô nhập.
    const RESERVED = new Set(["open", "mode", "action", "title"]);
    // Tên data-* khác tên ô nhập.
    const ALIAS = { assignmenttitle: "title" };

    // Ẩn/hiện các ô chỉ dùng cho bài thi (giữa kỳ, cuối kỳ).
    function syncExamFields(form) {
        const kind = form.querySelector('[name="Kind"]');
        const isExam = !!kind && (kind.value === "midterm" || kind.value === "final");
        form.querySelectorAll("[data-exam-only]").forEach(el => { el.hidden = !isExam; });
    }

    function prepareForm(modal, trigger) {
        const mode = trigger.dataset.mode || "create";
        const form = modal.querySelector("form");

        // Đổi tiêu đề modal (VD: "Thêm môn học" -> "Chỉnh sửa môn học").
        const titleEl = modal.querySelector("[data-modal-title]");
        if (titleEl && trigger.dataset.title) titleEl.textContent = trigger.dataset.title;

        if (form) {
            // Nhớ action gốc (asp-action của chế độ thêm) để khôi phục sau khi sửa.
            if (!form.dataset.defaultAction) form.dataset.defaultAction = form.getAttribute("action") || "";
            form.reset();
            form.setAttribute("action",
                mode === "edit" && trigger.dataset.action ? trigger.dataset.action : form.dataset.defaultAction);

            // Ô Id chỉ được gửi đi khi sửa hoặc xóa.
            const idInput = form.querySelector('[name="Id"]');
            if (idInput) idInput.disabled = !(mode === "edit" || mode === "delete");

            // Điền data-* của nút vào ô nhập có name trùng.
            const fields = Array.from(form.elements);
            Object.entries(trigger.dataset).forEach(([rawKey, value]) => {
                if (RESERVED.has(rawKey)) return;
                const key = ALIAS[rawKey] || rawKey;
                const field = fields.find(f => f.name && f.name.toLowerCase() === key);
                if (field) field.value = value;
            });

            syncExamFields(form);
            clearInvalid(form);
            const error = form.querySelector(".form-error");
            if (error) error.hidden = true;
        }

        // Điền chữ vào <... data-text="name"> (VD: tên môn trong modal xác nhận xóa).
        Object.entries(trigger.dataset).forEach(([key, value]) => {
            modal.querySelectorAll(`[data-text="${key}"]`).forEach(el => { el.textContent = value; });
        });
    }

    // ================================================================
    // 4. SỰ KIỆN CLICK / PHÍM CHUNG (event delegation)
    // ================================================================
    document.addEventListener("click", event => {
        // Mở modal
        const opener = event.target.closest("[data-open]");
        if (opener) {
            const modal = document.getElementById(opener.dataset.open);
            if (modal) {
                prepareForm(modal, opener);
                openModal(modal);
            }
            return;
        }

        // Đóng modal bằng nút Hủy / dấu ×  (chấp nhận cả data-close-modal cũ)
        const closer = event.target.closest("[data-close], [data-close-modal]");
        if (closer) {
            const modal = closer.closest(".study-modal");
            if (modal) closeModal(modal);
            return;
        }

        // Bấm vào nền mờ bên ngoài hộp thoại
        if (event.target.classList.contains("study-modal")) closeModal(event.target);
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            document.querySelectorAll(".study-modal:not([hidden])").forEach(closeModal);
        }
    });

    // Đổi "Loại" trong form bài tập -> ẩn/hiện ô thời lượng, phòng thi.
    document.addEventListener("change", event => {
        const select = event.target;
        if (select.matches && select.matches('.study-modal [name="Kind"]')) syncExamFields(select.form);
    });

    // ================================================================
    // 5. KIỂM TRA FORM PHÍA TRÌNH DUYỆT
    // Chỉ kiểm tra những gì HTML5 không làm được. Back-end vẫn phải kiểm tra lại.
    // ================================================================
    function validate(form) {
        if (form.dataset.validate === "slot") {
            const start = form.querySelector('[name="Start"]').value;
            const end = form.querySelector('[name="End"]').value;
            if (start && end && end <= start) return "Giờ kết thúc phải sau giờ bắt đầu.";
        }
        if (form.dataset.validate === "semester") {
            const start = form.querySelector('[name="StartDate"]').value;
            const end = form.querySelector('[name="EndDate"]').value;
            if (start && end && end <= start) return "Ngày kết thúc phải sau ngày bắt đầu.";
        }
        return "";
    }

    // ================================================================
    // 6. GỬI FORM (MÔ PHỎNG, CHƯA LƯU DỮ LIỆU)
    // BACK-END: khi action POST tương ứng đã làm xong, bỏ data-simulate khỏi form
    // để trình duyệt gửi thẳng lên server theo asp-action. Khi đó khối này không còn tác dụng.
    // ================================================================
    document.querySelectorAll("form[data-simulate]").forEach(form => {
        form.addEventListener("submit", event => {
            event.preventDefault();

            const error = errorBox(form);
            clearInvalid(form);

            // Ô thiếu hoặc sai: tô viền đỏ và liệt kê TÊN từng ô để người dùng biết sửa chỗ nào.
            if (!form.checkValidity()) {
                const bad = Array.from(form.elements).filter(el => el.willValidate && !el.checkValidity());
                const names = bad.map(el => {
                    const field = el.closest(".field");
                    if (field) field.classList.add("invalid");
                    const label = field && field.querySelector("label");
                    return label ? label.textContent.trim() : el.name;
                });
                error.textContent = "Chưa nhập đúng: " + names.join(", ") + ".";
                error.hidden = false;
                if (bad[0]) bad[0].focus();
                return;
            }

            const message = validate(form);
            if (message) { error.textContent = message; error.hidden = false; return; }
            error.hidden = true;

            const modal = form.closest(".study-modal");
            if (modal) closeModal(modal);
            toast("Bản mô phỏng giao diện: dữ liệu chưa được lưu.");
        });
    });

    // ================================================================
    // 7. FORM LỌC (GET): tự gửi khi đổi lựa chọn
    // Việc lọc thật do Controller + ViewModel làm; JS chỉ gửi form.
    // ================================================================
    document.querySelectorAll("[data-autosubmit]").forEach(el => {
        el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); });
    });

    // ================================================================
    // 8. LỌC NHANH BẢNG BÀI TẬP Ở TRANG TỔNG QUAN
    // Chỉ ẩn/hiện các hàng Razor đã render sẵn (không tạo dữ liệu mới).
    // Chỉ chạy khi trang có bảng #assignmentBody; chỉ tác động nút có data-filter,
    // nên không ảnh hưởng các chip dạng liên kết ở trang Môn học.
    // ================================================================
    const filterChips = document.querySelectorAll(".study-chip[data-filter]");
    const filterRows = document.querySelectorAll("#assignmentBody tr[data-status]");
    const emptyRow = document.getElementById("assignmentEmpty");

    if (filterChips.length && filterRows.length) {
        const matches = (row, filter) => {
            const status = row.dataset.status;
            const isExam = row.dataset.kind === "exam";
            switch (filter) {
                case "upcoming": return status === "upcoming" && !isExam;
                case "overdue": return status === "overdue";
                case "done": return status === "graded" || status === "submitted";
                case "exam": return isExam;
                default: return true;
            }
        };

        const applyFilter = filter => {
            let visible = 0;
            filterRows.forEach(row => {
                const ok = matches(row, filter);
                row.hidden = !ok;
                if (ok) visible++;
            });
            if (emptyRow) emptyRow.hidden = visible > 0;

            filterChips.forEach(chip => {
                const active = chip.dataset.filter === filter;
                chip.classList.toggle("active", active);
                chip.setAttribute("aria-pressed", active ? "true" : "false");
            });
        };

        filterChips.forEach(chip => chip.addEventListener("click", () => applyFilter(chip.dataset.filter)));
    }
});
