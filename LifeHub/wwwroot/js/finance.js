document.addEventListener("DOMContentLoaded", () => {
    // Lọc danh mục theo loại
    const filterButtons = document.querySelectorAll(".filter-button");
    const categoryRows = document.querySelectorAll(".category-row");
    const categorySearch = document.getElementById("categorySearch");

    let currentFilter = "all";

    function filterCategories() {
        const keyword = categorySearch?.value
            .trim()
            .toLowerCase() || "";

        categoryRows.forEach(row => {
            const type = row.dataset.type;
            const name = row.dataset.name || "";

            const matchesType =
                currentFilter === "all" || type === currentFilter;

            const matchesName = name.includes(keyword);

            row.hidden = !(matchesType && matchesName);
        });
    }

    filterButtons.forEach(button => {
        button.addEventListener("click", () => {
            filterButtons.forEach(item =>
                item.classList.remove("active")
            );

            button.classList.add("active");
            currentFilter = button.dataset.filter;

            filterCategories();
        });
    });

    categorySearch?.addEventListener("input", filterCategories);
});

document.addEventListener("DOMContentLoaded", function () {

    const $ = id => document.getElementById(id);

    const money = value =>
        Math.round(value).toLocaleString("vi-VN") + " ₫";

    const number = value =>
        Number(String(value).replace(/[^\d-]/g, "")) || 0;

    let income = number($("totalIncome").textContent);
    let expense = number($("totalExpense").textContent);

    const monthlyBudget = number(
        document.querySelector(".budget-details span").textContent
    );

    const savingGoal = number(
        document.querySelector(".saving-goal-content h2").textContent
    );

    const savingProgress = number(
        $("savingProgress").textContent
    );

    let transactions = Array.from(
        $("transactionBody").querySelectorAll("tr")
    );

    /* SUMMARY */

    function updateSummary() {
        $("totalIncome").textContent = money(income);
        $("totalExpense").textContent = money(expense);
        $("totalBalance").textContent = money(income - expense);

        updateBudget();
    }

    /* BUDGET */

    function updateBudget() {

        const percent = monthlyBudget > 0
            ? expense / monthlyBudget * 100
            : 0;

        $("budgetSpent").textContent = money(expense);
        $("budgetPercent").textContent = Math.round(percent) + "%";
        $("budgetRemaining").textContent =
            money(monthlyBudget - expense);

        $("budgetProgress").style.width =
            Math.min(100, percent) + "%";

        $("budgetProgress").classList.toggle(
            "over",
            percent > 100
        );

        $("budgetMessage").textContent =
            percent > 100
                ? "Bạn đã vượt ngân sách tháng này. Hãy xem lại các khoản chi."
                : percent >= 80
                    ? "Bạn đã sử dụng phần lớn ngân sách. Hãy cân nhắc chi tiêu."
                    : "Bạn đang kiểm soát ngân sách khá tốt!";
    }

    /* SAVING */

    function updateSaving() {

        const percent = savingGoal > 0
            ? Math.min(100, savingProgress / savingGoal * 100)
            : 0;

        $("savingPercent").textContent =
            Math.round(percent) + "%";

        $("savingCircle").style.background =
            `conic-gradient(var(--f-accent) ${percent * 3.6}deg, var(--f-soft) 0deg)`;
    }

    /* CHART */

    const monthData = [
        { label: "T6", income: 15, expense: 9 },
        { label: "T7", income: 18, expense: 12 },
        { label: "T8", income: 16, expense: 10 },
        { label: "T9", income: 22, expense: 13 },
        { label: "T10", income: 20, expense: 14 },
        { label: "T11", income: 25, expense: 12.8 }
    ];

    const weekData = [
        { label: "T2", income: 3, expense: 1.5 },
        { label: "T3", income: 4, expense: 2 },
        { label: "T4", income: 2, expense: 1 },
        { label: "T5", income: 5, expense: 3 },
        { label: "T6", income: 4, expense: 2 },
        { label: "T7", income: 3, expense: 1.5 },
        { label: "CN", income: 4, expense: 1.8 }
    ];

    function drawChart(period = "month") {

        const data = period === "week" ? weekData : monthData;
        const max = 30;

        $("chartColumns").innerHTML = "";

        data.forEach(item => {

            const group = document.createElement("div");
            group.className = "chart-group";

            const bars = document.createElement("div");
            bars.className = "chart-bars";

            ["income", "expense"].forEach(type => {

                const bar = document.createElement("div");
                bar.className = "chart-bar " + type;

                bar.style.height =
                    Math.max(2, Math.min(100, item[type] / max * 100)) + "%";

                bar.title =
                    (type === "income" ? "Thu nhập: " : "Chi tiêu: ")
                    + item[type] + " triệu";

                bars.appendChild(bar);
            });

            const label = document.createElement("small");
            label.textContent = item.label;

            group.append(bars, label);
            $("chartColumns").appendChild(group);
        });
    }

    $("chartPeriod").addEventListener("change", function () {
        drawChart(this.value);
    });

    /* TRANSACTION FILTER */

    function filterTransactions() {

        const filter = $("transactionFilter").value;
        let visible = 0;

        transactions.forEach(row => {

            const matches =
                filter === "all" ||
                row.dataset.type === filter;

            row.hidden = !matches;

            if (matches) visible++;
        });

        $("emptyTransactions").hidden = visible > 0;
    }

    $("transactionFilter").addEventListener(
        "change",
        filterTransactions
    );

    /* MODAL */

    function openModal() {
        $("transactionModal").classList.add("show");
    }

    function closeModal() {
        $("transactionModal").classList.remove("show");
    }

    $("openTransaction").addEventListener("click", openModal);
    $("openTransaction2").addEventListener("click", openModal);
    $("closeModal").addEventListener("click", closeModal);

    $("transactionModal").addEventListener("click", function (e) {
        if (e.target === this) closeModal();
    });

    /* ADD TRANSACTION */

    $("transactionDate").value =
        new Date().toLocaleDateString("en-CA");

    $("transactionForm").addEventListener("submit", function (e) {

        e.preventDefault();

        const name = $("transactionName").value.trim();
        const type = $("transactionType").value;
        const category = $("transactionCategory").value;
        const amount = Number($("transactionAmount").value);
        const date = $("transactionDate").value;

        if (!name || !date || !Number.isFinite(amount) || amount <= 0) {
            alert("Vui lòng nhập thông tin giao dịch hợp lệ.");
            return;
        }

        const transactionDate = new Date(date + "T00:00:00");
        const today = new Date();

        if (
            transactionDate.getMonth() !== today.getMonth() ||
            transactionDate.getFullYear() !== today.getFullYear()
        ) {
            alert("Bản demo chỉ thêm giao dịch trong tháng hiện tại.");
            return;
        }

        const row = document.createElement("tr");
        row.dataset.type = type;

        const nameCell = document.createElement("td");
        const nameStrong = document.createElement("strong");
        nameStrong.textContent = name;
        nameCell.appendChild(nameStrong);

        const categoryCell = document.createElement("td");
        const categorySpan = document.createElement("span");
        categorySpan.className = "transaction-category";
        categorySpan.textContent = category;
        categoryCell.appendChild(categorySpan);

        const dateCell = document.createElement("td");
        dateCell.textContent = transactionDate.toLocaleDateString("vi-VN");

        const typeCell = document.createElement("td");
        const typeSpan = document.createElement("span");

        typeSpan.className =
            "transaction-type " +
            (type === "income" ? "type-income" : "type-expense");

        typeSpan.textContent =
            type === "income" ? "Thu nhập" : "Chi tiêu";

        typeCell.appendChild(typeSpan);

        const amountCell = document.createElement("td");
        amountCell.className = "right";

        const amountStrong = document.createElement("strong");
        amountStrong.className =
            type === "income" ? "text-income" : "text-expense";

        amountStrong.textContent =
            (type === "income" ? "+" : "-") + money(amount);

        amountCell.appendChild(amountStrong);

        row.append(
            nameCell,
            categoryCell,
            dateCell,
            typeCell,
            amountCell
        );

        $("transactionBody").prepend(row);
        transactions.unshift(row);

        if (type === "income") {
            income += amount;
        } else {
            expense += amount;
        }

        updateSummary();
        filterTransactions();

        closeModal();
        this.reset();

        $("transactionDate").value =
            new Date().toLocaleDateString("en-CA");
    });

    /* INIT */

    updateSummary();
    updateSaving();
    drawChart();
    filterTransactions();

});
