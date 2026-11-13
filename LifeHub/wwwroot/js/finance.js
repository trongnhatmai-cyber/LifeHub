/* =============================================
   LIFEHUB - FINANCE MANAGEMENT
   Xử lý biểu đồ, ngân sách, tiết kiệm, lọc và modal giao dịch.
   Lưu ý: thêm giao dịch hiện chỉ cập nhật giao diện, chưa lưu database.
============================================= */
document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    // Lấy phần tử theo ID và định dạng tiền Việt Nam.
    const $ = id => document.getElementById(id);
    const money = value => `${Math.round(Number(value) || 0).toLocaleString("vi-VN")} ₫`;
    const readMoney = element => {
        if (!element) return 0;
        const parsed = Number(String(element.textContent).replace(/[^\d-]/g, ""));
        return Number.isFinite(parsed) ? parsed : 0;
    };
    const requiredIds = ["totalIncome", "totalExpense", "totalBalance", "transactionBody", "transactionForm", "transactionModal"];
    if (requiredIds.some(id => !$(id))) return;

    // Đọc số liệu ban đầu từ Razor View.
    let income = readMoney($("totalIncome"));
    let expense = readMoney($("totalExpense"));
    const budgetLabel = document.querySelector(".budget-details span");
    const monthlyBudget = readMoney(budgetLabel);
    const savingGoalHeading = document.querySelector(".saving-goal-content h2");
    const savingGoal = readMoney(savingGoalHeading);
    let savingProgress = readMoney($("savingProgress"));
    let transactions = Array.from($("transactionBody").querySelectorAll("tr"));

    // Cập nhật tổng thu, tổng chi, số dư và trạng thái ngân sách.
    function updateSummary() {
        $("totalIncome").textContent = money(income);
        $("totalExpense").textContent = money(expense);
        $("totalBalance").textContent = money(income - expense);
        if ($("expenseBreakdown")) $("expenseBreakdown").textContent = money(expense);
        updateBudget();
    }

    // Cập nhật thanh tiến độ và cảnh báo ngân sách.
    function updateBudget() {
        const percent = monthlyBudget > 0 ? expense / monthlyBudget * 100 : 0;
        if ($("budgetSpent")) $("budgetSpent").textContent = money(expense);
        if ($("budgetPercent")) $("budgetPercent").textContent = `${Math.round(percent)}%`;
        if ($("budgetRemaining")) $("budgetRemaining").textContent = money(monthlyBudget - expense);
        if ($("budgetProgress")) {
            $("budgetProgress").style.width = `${Math.min(100, Math.max(0, percent))}%`;
            $("budgetProgress").classList.toggle("over", percent > 100);
        }
        if ($("budgetMessage")) {
            $("budgetMessage").textContent = percent > 100
                ? "Bạn đã vượt ngân sách tháng này. Hãy xem lại các khoản chi."
                : percent >= 80
                    ? "Bạn đã sử dụng phần lớn ngân sách. Hãy cân nhắc chi tiêu."
                    : "Bạn đang kiểm soát ngân sách khá tốt!";
        }
    }

    // Cập nhật vòng tròn mục tiêu tiết kiệm.
    function updateSaving() {
        const percent = savingGoal > 0 ? Math.min(100, Math.max(0, savingProgress / savingGoal * 100)) : 0;
        if ($("savingPercent")) $("savingPercent").textContent = `${Math.round(percent)}%`;
        if ($("savingCircle")) {
            $("savingCircle").style.background = `conic-gradient(var(--finance-accent) ${percent * 3.6}deg, var(--finance-soft) 0deg)`;
            $("savingCircle").dataset.progress = String(percent);
        }
        const remaining = document.querySelector(".saving-footer strong");
        if (remaining) remaining.textContent = money(Math.max(0, savingGoal - savingProgress));
    }

    // Dữ liệu biểu đồ minh họa; thay bằng dữ liệu thống kê từ server khi tích hợp backend.
    const monthData = [
        { label: "T6", income: 15, expense: 9 }, { label: "T7", income: 18, expense: 12 },
        { label: "T8", income: 16, expense: 10 }, { label: "T9", income: 22, expense: 13 },
        { label: "T10", income: 20, expense: 14 }, { label: "T11", income: 25, expense: 12.8 }
    ];
    const weekData = [
        { label: "T2", income: 3, expense: 1.5 }, { label: "T3", income: 4, expense: 2 },
        { label: "T4", income: 2, expense: 1 }, { label: "T5", income: 5, expense: 3 },
        { label: "T6", income: 4, expense: 2 }, { label: "T7", income: 3, expense: 1.5 },
        { label: "CN", income: 4, expense: 1.8 }
    ];

    // Vẽ biểu đồ cột theo tuần hoặc theo tháng.
    function drawChart(period = "month") {
        const chart = $("chartColumns");
        if (!chart) return;
        const data = period === "week" ? weekData : monthData;
        const max = 30;
        chart.replaceChildren();
        data.forEach(item => {
            const group = document.createElement("div");
            group.className = "chart-group";
            const bars = document.createElement("div");
            bars.className = "chart-bars";
            ["income", "expense"].forEach(type => {
                const bar = document.createElement("div");
                bar.className = `chart-bar ${type}`;
                bar.style.height = `${Math.max(2, Math.min(100, item[type] / max * 100))}%`;
                bar.title = `${type === "income" ? "Thu nhập" : "Chi tiêu"}: ${item[type]} triệu`;
                bars.appendChild(bar);
            });
            const label = document.createElement("small");
            label.textContent = item.label;
            group.append(bars, label);
            chart.appendChild(group);
        });
    }
    $("chartPeriod")?.addEventListener("change", event => drawChart(event.target.value));

    // Lọc giao dịch theo loại thu nhập hoặc chi tiêu.
    function filterTransactions() {
        const filter = $("transactionFilter")?.value || "all";
        let visible = 0;
        transactions.forEach(row => {
            const matches = filter === "all" || row.dataset.type === filter;
            row.hidden = !matches;
            if (matches) visible++;
        });
        if ($("emptyTransactions")) $("emptyTransactions").hidden = visible > 0;
    }
    $("transactionFilter")?.addEventListener("change", filterTransactions);

    // Mở và đóng modal thêm giao dịch.
    function openModal() {
        $("transactionModal").classList.add("show");
        document.body.classList.add("modal-open");
        $("transactionModal").setAttribute("aria-hidden", "false");
        setTimeout(() => $("transactionName")?.focus(), 80);
    }
    function closeModal() {
        $("transactionModal").classList.remove("show");
        document.body.classList.remove("modal-open");
        $("transactionModal").setAttribute("aria-hidden", "true");
    }
    $("openTransaction")?.addEventListener("click", openModal);
    $("openTransaction2")?.addEventListener("click", openModal);
    $("closeModal")?.addEventListener("click", closeModal);
    $("cancelTransaction")?.addEventListener("click", closeModal);
    $("transactionModal").addEventListener("click", event => {
        if (event.target === $("transactionModal")) closeModal();
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && $("transactionModal").classList.contains("show")) closeModal();
    });

    // Đặt ngày mặc định theo múi giờ địa phương, tránh lệch ngày do UTC.
    function localDateValue(date = new Date()) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    }
    if ($("transactionDate")) $("transactionDate").value = localDateValue();

    // Tạo ô bảng bằng textContent để tránh chèn HTML không an toàn từ dữ liệu nhập.
    function createCell(content, className = "") {
        const cell = document.createElement("td");
        if (className) cell.className = className;
        if (content instanceof Node) cell.appendChild(content);
        else cell.textContent = content;
        return cell;
    }

    // Thêm giao dịch mới vào bảng và cập nhật tổng thu/chi.
    // Chỉ hiển thị danh mục phù hợp với loại giao dịch.
    function filterCategories() {
        const selectedType = $("transactionType").value;
        const categorySelect = $("transactionCategory");
        if (!categorySelect) return;

        let selectedOptionValid = false;

        Array.from(categorySelect.options).forEach(option => {
            if (!option.value) {
                option.hidden = false;
                return;
            }

            const matches = option.dataset.type === selectedType;
            option.hidden = !matches;

            if (matches && option.selected) {
                selectedOptionValid = true;
            }
        });

        if (!selectedOptionValid) {
            categorySelect.value = "";
        }
    }

    $("transactionType")?.addEventListener("change", filterCategories);

    // Gửi giao dịch đến Controller để lưu vào database.
    $("transactionForm").addEventListener("submit", async event => {
        event.preventDefault();

        const form = event.currentTarget;
        const submitButton = form.querySelector('[type="submit"]');

        const name = $("transactionName").value.trim();
        const categoryId = $("transactionCategory").value;
        const amount = Number($("transactionAmount").value);
        const date = $("transactionDate").value;

        if (!name || !categoryId || !date ||
            !Number.isFinite(amount) || amount <= 0) {
            alert("Vui lòng nhập đầy đủ thông tin giao dịch hợp lệ.");
            return;
        }

        const parsedDate = new Date(`${date}T00:00:00`);

        if (Number.isNaN(parsedDate.getTime())) {
            alert("Ngày giao dịch không hợp lệ.");
            return;
        }

        // Chặn gửi nhiều lần khi đang lưu.
        submitButton.disabled = true;
        submitButton.textContent = "Đang lưu...";

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(result.message || "Không thể lưu giao dịch.");
            }

            // Tải lại dữ liệu từ database để cập nhật tổng thu, chi và bảng.
            window.location.reload();
        } catch (error) {
            alert(error.message || "Đã xảy ra lỗi khi lưu giao dịch.");
            submitButton.disabled = false;
            submitButton.textContent = "Lưu giao dịch";
        }
    });

    // Khởi tạo toàn bộ thành phần khi trang tải xong.
    updateSummary();
    updateSaving();
    drawChart($("chartPeriod")?.value || "month");
    filterTransactions();
    filterCategories();
});
