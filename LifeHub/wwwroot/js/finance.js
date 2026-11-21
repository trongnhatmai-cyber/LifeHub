/* =====================================================================
   LIFEHUB - FINANCE MANAGEMENT  |  wwwroot/js/finance.js   (MỘT FILE DUY NHẤT)

   Thay thế finance.js cũ, finance-modules.js và category-form.js. Sau khi chép file này vào:
   - Xóa wwwroot/js/finance-modules.js và wwwroot/js/category-form.js.
   - Mọi View trong Views/Finance đều nạp đúng một dòng (trong @section Scripts):
       <script src="~/js/finance.js" asp-append-version="true"></script>

   VAI TRÒ (đúng quy ước MVC của LIFEHUB)
   JavaScript CHỈ xử lý tương tác giao diện:
     1. Thông báo nhỏ (toast) và tự ẩn thông báo do Controller gửi qua TempData.
     2. Mở / đóng hộp thoại (modal), điền sẵn form khi sửa, trả focus về nút đã mở.
     3. Ẩn / hiện các ô theo loại giao dịch; lọc danh mục theo loại.
     4. Kiểm tra form phía trình duyệt (đủ số dư, hai ví khác nhau) và chống bấm gửi hai lần.
     5. Tự gửi form lọc (GET) khi đổi lựa chọn.
     6. Tab lọc danh mục (ẩn/hiện thẻ Razor đã render sẵn).
     7. Trang Tổng quan: vẽ biểu đồ dòng tiền và lọc nhanh bảng lịch sử giao dịch.
   JS KHÔNG chứa dữ liệu tài chính, không tự tính tổng thu/chi, số dư, tiến độ. Việc đó do C# làm.
   Form thêm / sửa / xóa gửi THẬT lên Controller (POST). JS chỉ chặn khi dữ liệu rõ ràng sai;
   Controller vẫn kiểm tra lại toàn bộ.

   QUY ƯỚC HTML MÀ FILE NÀY HIỂU (giống study.js)
   - Nút mở modal:   data-open="<id modal>"  data-mode="create|edit|delete"
                     data-title="Tiêu đề modal"  data-action="<url POST khi sửa hoặc xóa>"
                     Các data-* còn lại tự điền vào ô input có name trùng (không phân biệt hoa thường):
                       data-walletid="3"  -> <select name="WalletId">
                       data-amount="50000" -> <input name="Amount">
                     Ngoại lệ: data-goaltitle -> ô name="Title" (vì data-title đã dùng cho tiêu đề modal).
   - Tiêu đề modal:  phần tử có thuộc tính data-modal-title.
   - Chữ trong modal: <strong data-text="name"> được điền từ data-name của nút mở
                     (các data-text không có dữ liệu sẽ được xóa trống, không giữ chữ của lần mở trước).
   - Nút đóng:       data-close.
   - Ô Id ẩn:        <input type="hidden" name="Id" disabled> -> JS chỉ bật khi mode = edit hoặc delete.
   - Form giao dịch: <form data-tx-form> có ô name="TransactionType" hoặc name="Type".
                     Ô chỉ dùng cho một số loại: data-show-for="expense" | "transfer" | "income expense".
                     Ô bị ẩn sẽ bị vô hiệu hóa (disabled) nên KHÔNG được gửi lên server.
                     Option danh mục có data-type="Income|Expense" để lọc theo loại.
   - Kiểm tra thêm:  <form data-validate="transaction|transfer|contribute"> (xem hàm validate).
                     <option data-balance="1500000"> cho ô chọn ví nguồn (số thuần, dấu chấm thập phân).
   - Ngày mặc định:  <input type="date" data-default="today"> tự điền ngày hôm nay khi mở modal thêm.
   - Form lọc GET:   select/input có data-autosubmit sẽ tự gửi form khi đổi lựa chọn.
   - Tab lọc danh mục: nút [data-category-filter="all|expense|income"] + thẻ [data-category-kind].
   - Thông báo:      phần tử có data-toast (do _FinanceFlash.cshtml render) tự ẩn sau vài giây.

   LƯU Ý CSS: modal dùng position: fixed nên KHÔNG đặt bên trong .finance-card / .summary-card
   (các thẻ này có hiệu ứng hover transform, sẽ làm modal lệch vị trí). Hãy đặt modal
   là con trực tiếp của .finance-page.
===================================================================== */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    // Chỉ chạy trong trang tài chính.
    const page = document.querySelector(".finance-page");
    if (!page) return;

    const $ = id => document.getElementById(id);

    // ================================================================
    // 1. THÔNG BÁO NHỎ (TOAST)
    // Thông báo từ server (TempData) đã có sẵn trong HTML: chỉ cần hẹn giờ ẩn.
    // toast(message, isError) tạo thông báo mới bằng JS (dùng cho cảnh báo phía trình duyệt).
    // ================================================================
    function hideLater(el, ms = 4500) {
        setTimeout(() => {
            el.hidden = true;
            const stack = el.parentElement;
            if (stack && stack.classList.contains("finance-toast-stack") && !stack.querySelector("[data-toast]:not([hidden])")) {
                stack.hidden = true;
            }
        }, ms);
    }

    document.querySelectorAll("[data-toast]").forEach(el => hideLater(el));

    function toast(message, isError = false) {
        let stack = page.querySelector(".finance-toast-stack");
        if (!stack) {
            stack = document.createElement("div");
            stack.className = "finance-toast-stack";
            page.appendChild(stack);
        }
        stack.hidden = false;
        const el = document.createElement("div");
        el.className = "finance-toast" + (isError ? " error" : "");
        el.setAttribute("role", isError ? "alert" : "status");
        el.dataset.toast = "";
        el.textContent = message;
        stack.appendChild(el);
        hideLater(el);
    }

    // ================================================================
    // 2. MỞ / ĐÓNG MODAL
    // ================================================================
    function openModal(modal, opener) {
        modal._opener = opener || null;           // để trả focus khi đóng
        modal.hidden = false;
        document.body.classList.add("finance-modal-open");   // khóa cuộn trang phía sau
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled), select:not(:disabled)");
        if (first) first.focus();
    }

    function closeModal(modal) {
        modal.hidden = true;
        // Chỉ mở khóa cuộn khi không còn modal nào đang mở.
        if (!document.querySelector(".finance-modal:not([hidden])")) {
            document.body.classList.remove("finance-modal-open");
        }
        if (modal._opener && document.contains(modal._opener)) modal._opener.focus();
    }

    // ================================================================
    // 2b. NHẮC NHỞ Ô BẮT BUỘC
    // Ô nào có thuộc tính required thì tự thêm class "required" cho .field,
    // CSS sẽ hiện dấu * đỏ sau nhãn. Không cần sửa View.
    // ================================================================
    document.querySelectorAll(".finance-modal [required]").forEach(el => {
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
            form.insertBefore(box, form.querySelector(".finance-modal-foot") || null);
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
    const ALIAS = { goaltitle: "title" };

    function today() {
        const d = new Date();
        const m = String(d.getMonth() + 1).padStart(2, "0");
        const day = String(d.getDate()).padStart(2, "0");
        return `${d.getFullYear()}-${m}-${day}`;
    }

    function prepareForm(modal, trigger) {
        const mode = trigger.dataset.mode || "create";
        const form = modal.querySelector("form");

        // Đổi tiêu đề modal (VD: "Thêm ví" -> "Chỉnh sửa ví").
        const titleEl = modal.querySelector("[data-modal-title]");
        if (titleEl && trigger.dataset.title) titleEl.textContent = trigger.dataset.title;

        if (form) {
            // Nhớ action gốc (asp-action của chế độ thêm) để khôi phục sau khi sửa / xóa.
            if (!form.dataset.defaultAction) form.dataset.defaultAction = form.getAttribute("action") || "";
            form.reset();
            // Sửa và xóa dùng url riêng của nút bấm; thêm mới dùng action gốc.
            const useOwnAction = (mode === "edit" || mode === "delete") && trigger.dataset.action;
            form.setAttribute("action", useOwnAction ? trigger.dataset.action : form.dataset.defaultAction);

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

            // Ngày mặc định = hôm nay (chỉ khi ô còn trống, tức là đang thêm mới).
            form.querySelectorAll('[data-default="today"]').forEach(input => {
                if (!input.value) input.value = today();
            });

            syncTransactionFields(form);
            clearInvalid(form);
            const error = form.querySelector(".form-error");
            if (error) error.hidden = true;
            const submit = form.querySelector('[type="submit"]');
            if (submit && !submit.hasAttribute("data-keep-disabled")) submit.disabled = submit.dataset.initialDisabled === "true";
        }

        // Điền chữ vào <... data-text="name">. Xóa trống trước để không giữ chữ của lần mở trước.
        modal.querySelectorAll("[data-text]").forEach(el => { el.textContent = trigger.dataset[el.dataset.text] || ""; });
    }

    // Nút gửi vốn bị vô hiệu hóa từ server (VD: không có ví) phải giữ nguyên trạng thái đó.
    document.querySelectorAll('.finance-modal form [type="submit"]').forEach(btn => {
        btn.dataset.initialDisabled = btn.disabled ? "true" : "false";
    });

    // ================================================================
    // 3b. FORM GIAO DỊCH: ĐỔI LOẠI -> ẨN/HIỆN Ô, LỌC DANH MỤC
    // Loại chuyển tiền: không có danh mục, cần ví nhận. Chỉ chi tiêu mới gắn được mục tiêu tiết kiệm.
    // Ô bị ẩn phải bị vô hiệu hóa để không gửi lên server (service từ chối nếu gửi thừa).
    // ================================================================
    function txType(form) {
        const el = form.querySelector('[name="TransactionType"], [name="Type"]');
        return el ? el.value.trim().toLowerCase() : "";
    }

    function syncTransactionFields(form) {
        if (!form.hasAttribute("data-tx-form")) return;
        const type = txType(form);

        form.querySelectorAll("[data-show-for]").forEach(group => {
            const show = group.dataset.showFor.toLowerCase().split(/\s+/).includes(type);
            group.hidden = !show;
            group.querySelectorAll("input, select").forEach(i => { i.disabled = !show; });
        });

        // Danh mục: chỉ giữ các danh mục cùng loại với giao dịch.
        const category = form.querySelector('[name="CategoryId"]');
        if (category) {
            Array.from(category.options).forEach(option => {
                if (!option.value) return;
                const match = (type === "income" || type === "expense") &&
                    (option.dataset.type || "").trim().toLowerCase() === type;
                option.hidden = !match;
                option.disabled = !match;
            });
            const selected = category.options[category.selectedIndex];
            if (!selected || selected.disabled) category.value = "";
        }
    }

    document.addEventListener("change", event => {
        const el = event.target;
        if (el.matches && el.matches('[data-tx-form] [name="TransactionType"], [data-tx-form] [name="Type"]')) {
            syncTransactionFields(el.form);
        }
    });

    document.querySelectorAll("form[data-tx-form]").forEach(syncTransactionFields);

    // ================================================================
    // 4. SỰ KIỆN CLICK / PHÍM CHUNG (event delegation)
    // ================================================================
    let downOnBackdrop = false;   // tránh đóng modal khi người dùng bôi đen chữ rồi thả chuột ra ngoài

    document.addEventListener("mousedown", event => {
        downOnBackdrop = event.target.classList && event.target.classList.contains("finance-modal");
    });

    document.addEventListener("click", event => {
        // Mở modal
        const opener = event.target.closest("[data-open]");
        if (opener) {
            const modal = document.getElementById(opener.dataset.open);
            if (modal) {
                prepareForm(modal, opener);
                openModal(modal, opener);
            }
            return;
        }

        // Đóng modal bằng nút Hủy / dấu ×
        const closer = event.target.closest("[data-close]");
        if (closer) {
            const modal = closer.closest(".finance-modal");
            if (modal) closeModal(modal);
            return;
        }

        // Bấm vào nền mờ bên ngoài hộp thoại (cả nhấn xuống và thả ra đều ở nền)
        if (downOnBackdrop && event.target.classList.contains("finance-modal")) closeModal(event.target);
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            document.querySelectorAll(".finance-modal:not([hidden])").forEach(closeModal);
        }
    });

    // ================================================================
    // 5. KIỂM TRA FORM PHÍA TRÌNH DUYỆT
    // Chỉ kiểm tra những gì HTML5 không làm được. Back-end vẫn kiểm tra lại.
    // Số dư lấy từ data-balance do server render, chỉ dùng để cảnh báo sớm.
    // ================================================================
    function enough(walletSelect, amount) {
        const option = walletSelect && walletSelect.options[walletSelect.selectedIndex];
        if (!option || option.dataset.balance === undefined) return "";
        const balance = Number(option.dataset.balance);
        if (Number.isFinite(balance) && amount > balance) return "Số dư của ví nguồn không đủ.";
        return "";
    }

    function validate(form) {
        const amount = Number((form.querySelector('[name="Amount"]') || {}).value);
        switch (form.dataset.validate) {
            case "transaction": {
                const type = txType(form);
                const wallet = form.querySelector('[name="WalletId"]');
                const to = form.querySelector('[name="ToWalletId"]');
                if (type === "transfer") {
                    if (!to || !to.value) return "Hãy chọn ví nhận tiền.";
                    if (to.value === wallet.value) return "Ví nguồn và ví nhận phải khác nhau.";
                }
                if (type === "expense" || type === "transfer") return enough(wallet, amount);
                return "";
            }
            case "transfer": {
                const from = form.querySelector('[name="FromWalletId"]');
                const to = form.querySelector('[name="ToWalletId"]');
                if (from.value === to.value) return "Ví nguồn và ví đích phải khác nhau.";
                return enough(from, amount);
            }
            case "contribute":
                return enough(form.querySelector('[name="WalletId"]'), amount);
            default:
                return "";
        }
    }

    // ================================================================
    // 6. GỬI FORM TRONG MODAL (GỬI THẬT LÊN CONTROLLER)
    // - Ô thiếu/sai: chặn gửi, tô viền đỏ và liệt kê TÊN từng ô.
    // - Qua kiểm tra: khóa nút gửi để không bấm hai lần (tránh ghi trùng giao dịch / trừ tiền hai lần).
    // ================================================================
    document.querySelectorAll(".finance-modal form").forEach(form => {
        form.addEventListener("submit", event => {
            const error = errorBox(form);
            clearInvalid(form);

            if (!form.checkValidity()) {
                event.preventDefault();
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
            if (message) {
                event.preventDefault();
                error.textContent = message;
                error.hidden = false;
                return;
            }

            error.hidden = true;
            const submit = form.querySelector('[type="submit"]');
            if (submit) submit.disabled = true;
        });
    });

    // Quay lại bằng nút Back của trình duyệt: mở khóa các nút gửi đã bị khóa.
    window.addEventListener("pageshow", event => {
        if (!event.persisted) return;
        document.querySelectorAll('.finance-modal form [type="submit"]').forEach(btn => {
            btn.disabled = btn.dataset.initialDisabled === "true";
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
    // 8. TAB LỌC DANH MỤC (trang Danh mục)
    // Chỉ ẩn/hiện các thẻ Razor đã render sẵn (không tạo dữ liệu mới).
    // ================================================================
    const tabs = document.querySelectorAll("[data-category-filter]");
    const cards = document.querySelectorAll("[data-category-kind]");
    const categoryEmpty = $("categoryFilterEmpty");

    tabs.forEach(tab => tab.addEventListener("click", () => {
        const filter = tab.dataset.categoryFilter;
        let visible = 0;
        tabs.forEach(t => {
            const active = t === tab;
            t.classList.toggle("active", active);
            t.setAttribute("aria-pressed", active ? "true" : "false");
        });
        cards.forEach(card => {
            const show = filter === "all" || card.dataset.categoryKind === filter;
            card.hidden = !show;
            if (show) visible++;
        });
        if (categoryEmpty) categoryEmpty.hidden = visible > 0;
    }));

    // ================================================================
    // 9. TRANG TỔNG QUAN: BIỂU ĐỒ DÒNG TIỀN VÀ LỌC LỊCH SỬ GIAO DỊCH
    // Chỉ chạy khi trang có #chartColumns / #transactionBody.
    // Dữ liệu biểu đồ do Razor render vào các phần tử .chart-point; JS chỉ đọc để vẽ.
    // ================================================================
    const money = value => `${Math.round(Number(value) || 0).toLocaleString("vi-VN")} ₫`;

    function readPoints(container) {
        if (!container) return [];
        return Array.from(container.querySelectorAll(".chart-point")).map(p => ({
            label: p.dataset.label || "",
            income: Number(p.dataset.income) || 0,
            expense: Number(p.dataset.expense) || 0
        }));
    }

    const chart = $("chartColumns");
    if (chart) {
        const monthly = readPoints($("monthlyChartData"));
        const weekly = $("weeklyChartData")
            ? Array.from($("weeklyChartData").querySelectorAll(".week-data")).map(w => ({
                index: Number(w.dataset.weekIndex), points: readPoints(w) }))
            : [];

        function drawChart(data) {
            chart.replaceChildren();
            if (!data.length) {
                const empty = document.createElement("p");
                empty.className = "finance-empty";
                empty.textContent = "Chưa có dữ liệu biểu đồ.";
                chart.appendChild(empty);
                return;
            }

            // Trục dọc luôn là bội của 30 triệu (30tr/20tr/10tr/0, 60tr/40tr/20tr/0...).
            // Chỉ dùng để quy đổi chiều cao cột, không thay đổi dữ liệu gốc.
            const peak = Math.max(...data.flatMap(d => [d.income, d.expense]));
            const maxValue = Math.max(1, Math.ceil(peak / 30000000)) * 30000000;
            const yLabels = $("chartYLabels");
            if (yLabels) {
                const ticks = [maxValue, maxValue * 2 / 3, maxValue / 3, 0];
                yLabels.replaceChildren(...ticks.map(t => {
                    const s = document.createElement("span");
                    s.textContent = t === 0 ? "0" : `${Math.round(t / 1000000)}tr`;
                    return s;
                }));
            }

            data.forEach(item => {
                const group = document.createElement("div");
                group.className = "chart-group";
                const bars = document.createElement("div");
                bars.className = "chart-bars";

                ["income", "expense"].forEach(type => {
                    const bar = document.createElement("div");
                    bar.className = `chart-bar ${type}`;
                    const value = item[type];
                    bar.style.height = `${Math.max(value > 0 ? 1 : 0, Math.min(100, value / maxValue * 100))}%`;
                    bar.title = `${type === "income" ? "Thu nhập" : "Chi tiêu"}: ${money(value)}`;
                    bars.appendChild(bar);
                });

                const label = document.createElement("small");
                label.textContent = item.label;
                group.append(bars, label);
                chart.appendChild(group);
            });
        }

        function updateChart() {
            const period = ($("chartPeriod") || {}).value || "month";
            if (period === "week") {
                const index = Number(($("chartWeek") || {}).value || 0);
                const week = weekly.find(w => w.index === index);
                drawChart(week ? week.points : []);
            } else {
                drawChart(monthly);
            }
        }

        const periodSelect = $("chartPeriod");
        if (periodSelect) periodSelect.addEventListener("change", () => {
            if ($("chartWeek")) $("chartWeek").hidden = periodSelect.value !== "week";
            updateChart();
        });
        if ($("chartWeek")) $("chartWeek").addEventListener("change", updateChart);
        updateChart();
    }

    const body = $("transactionBody");
    if (body) {
        const rows = Array.from(body.querySelectorAll("tr"));
        const emptyBox = $("emptyTransactions");
        const emptyText = emptyBox ? emptyBox.textContent.trim() : "";

        // Danh sách tháng lấy từ chính các hàng đã render.
        const monthSelect = $("monthFilter");
        if (monthSelect) {
            const months = [...new Set(rows.map(r => r.dataset.month).filter(Boolean))].sort((a, b) => b.localeCompare(a));
            months.forEach(value => {
                const [year, month] = value.split("-");
                const option = document.createElement("option");
                option.value = value;
                option.textContent = `Tháng ${month}/${year}`;
                monthSelect.appendChild(option);
            });
        }

        function filterRows() {
            const type = ($("transactionFilter") || {}).value || "all";
            const month = (monthSelect || {}).value || "all";
            const start = ($("startDateFilter") || {}).value || "";
            const end = ($("endDateFilter") || {}).value || "";

            if (start && end && start > end) {
                rows.forEach(r => { r.hidden = true; });
                if (emptyBox) { emptyBox.textContent = "Ngày bắt đầu không được sau ngày kết thúc."; emptyBox.hidden = false; }
                return;
            }

            let visible = 0;
            rows.forEach(r => {
                const date = r.dataset.date || "";
                const ok = (type === "all" || (r.dataset.type || "") === type) &&
                    (month === "all" || r.dataset.month === month) &&
                    (!start || date >= start) && (!end || date <= end);
                r.hidden = !ok;
                if (ok) visible++;
            });

            if (emptyBox) {
                emptyBox.textContent = rows.length ? "Không tìm thấy giao dịch phù hợp." : emptyText;
                emptyBox.hidden = visible > 0;
            }
        }

        ["transactionFilter", "monthFilter", "startDateFilter", "endDateFilter"].forEach(id => {
            const el = $(id);
            if (el) el.addEventListener("change", filterRows);
        });

        const clear = $("clearDateFilter");
        if (clear) clear.addEventListener("click", () => {
            if ($("startDateFilter")) $("startDateFilter").value = "";
            if ($("endDateFilter")) $("endDateFilter").value = "";
            filterRows();
        });

        filterRows();
    }
});
