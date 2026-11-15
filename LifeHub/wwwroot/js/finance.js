/* =============================================
   LIFEHUB - FINANCE MANAGEMENT

   Vai trò của JavaScript:
   - Xử lý tương tác giao diện.
   - Vẽ biểu đồ từ dữ liệu Razor render.
   - Lọc giao dịch theo loại, tháng và khoảng ngày.
   - Chọn tuần xem biểu đồ.
   - Mở và đóng modal.
   - Kiểm tra dữ liệu nhập trên form.

   Quy ước kiến trúc:
   - Không khai báo MOCK_DATA trong JavaScript.
   - Không tự tạo nguồn dữ liệu tài chính.
   - Không tự tính lại tổng thu nhập, chi tiêu, ngân sách.
   - Không lưu hoặc giả lập lưu giao dịch.
   - Dữ liệu được cung cấp từ C# ViewModel.
============================================= */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    // =============================================
    // 1. HÀM TIỆN ÍCH
    // =============================================

    const $ = id => document.getElementById(id);

    const requiredIds = [
        "transactionBody",
        "transactionForm",
        "transactionModal",
        "chartColumns"
    ];

    // Dừng nếu không phải trang Finance hoặc thiếu phần tử chính.
    if (requiredIds.some(id => !$(id))) return;

    // Lấy các dòng giao dịch Razor đã render.
    const transactionRows = Array.from(
        $("transactionBody").querySelectorAll("tr")
    );

    // Định dạng số tiền.
    const money = value =>
        `${Math.round(Number(value) || 0).toLocaleString("vi-VN")} ₫`;

    // Chuyển ngày về YYYY-MM-DD theo múi giờ địa phương.
    function localDateValue(date = new Date()) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");

        return `${year}-${month}-${day}`;
    }

    // Chuyển YYYY-MM-DD thành DD/MM/YYYY.
    function formatDate(dateValue) {
        if (!dateValue) return "";

        const [year, month, day] = dateValue.split("-");

        if (!year || !month || !day) return dateValue;

        return `${day}/${month}/${year}`;
    }

    // =============================================
    // 2. ĐỌC DỮ LIỆU BIỂU ĐỒ TỪ RAZOR
    // =============================================

    // Đọc các điểm dữ liệu trong một khu vực HTML.
    function readChartPoints(container) {
        if (!container) return [];

        return Array.from(
            container.querySelectorAll(".chart-point")
        ).map(point => ({
            label: point.dataset.label || "",
            date: point.dataset.date || "",
            income: Number(point.dataset.income) || 0,
            expense: Number(point.dataset.expense) || 0
        }));
    }

    // Dữ liệu tháng do FinanceMockData và Razor cung cấp.
    const monthlyChartData = readChartPoints(
        $("monthlyChartData")
    );

    // Dữ liệu tuần được chia theo từng nhóm tuần.
    function readWeeklyChartData() {
        const container = $("weeklyChartData");

        if (!container) return [];

        return Array.from(
            container.querySelectorAll(".week-data")
        ).map(weekElement => ({
            index: Number(weekElement.dataset.weekIndex),
            points: readChartPoints(weekElement)
        }));
    }

    const weeklyChartData = readWeeklyChartData();

    // =============================================
    // 3. VẼ BIỂU ĐỒ
    // =============================================

    function drawChart(data) {
        const chart = $("chartColumns");

        if (!chart) return;

        chart.replaceChildren();

        if (!data || data.length === 0) {
            const emptyMessage = document.createElement("p");

            emptyMessage.textContent = "Chưa có dữ liệu biểu đồ.";
            emptyMessage.style.textAlign = "center";
            emptyMessage.style.color = "var(--finance-muted)";
            emptyMessage.style.padding = "30px 10px";

            chart.appendChild(emptyMessage);
            return;
        }

        // Trục biểu đồ hiện thiết kế theo mức tối đa 30 triệu.
        // Chỉ dùng giá trị để xác định chiều cao cột,
        // không thay đổi dữ liệu tài chính gốc.
        const maxValue = Math.max(
            30000000,
            ...data.flatMap(item => [
                item.income,
                item.expense
            ])
        );

        data.forEach(item => {
            const group = document.createElement("div");
            group.className = "chart-group";

            const bars = document.createElement("div");
            bars.className = "chart-bars";

            ["income", "expense"].forEach(type => {
                const bar = document.createElement("div");

                bar.className = `chart-bar ${type}`;

                const value = item[type];

                const height = Math.max(
                    value > 0 ? 1 : 0,
                    Math.min(100, value / maxValue * 100)
                );

                bar.style.height = `${height}%`;

                bar.title =
                    `${type === "income" ? "Thu nhập" : "Chi tiêu"}: ${money(value)}`;

                bars.appendChild(bar);
            });

            const label = document.createElement("small");
            label.textContent = item.label;

            group.append(bars, label);
            chart.appendChild(group);
        });
    }

    // Vẽ biểu đồ theo lựa chọn hiện tại.
    function updateChart() {
        const period = $("chartPeriod")?.value || "month";

        if (period === "week") {
            const selectedWeekIndex = Number(
                $("chartWeek")?.value || 0
            );

            const selectedWeek = weeklyChartData.find(
                week => week.index === selectedWeekIndex
            );

            drawChart(selectedWeek?.points || []);
        } else {
            drawChart(monthlyChartData);
        }
    }

    // Thay đổi chế độ biểu đồ tháng/tuần.
    $("chartPeriod")?.addEventListener("change", () => {
        const isWeekly = $("chartPeriod").value === "week";

        if ($("chartWeek")) {
            $("chartWeek").style.display =
                isWeekly ? "" : "none";
        }

        updateChart();
    });

    // Chọn tuần cụ thể.
    $("chartWeek")?.addEventListener("change", updateChart);

    // =============================================
    // 4. TẠO BỘ LỌC THÁNG TỪ GIAO DỊCH RAZOR
    // =============================================

    function generateMonthOptions() {
        const monthFilter = $("monthFilter");

        if (!monthFilter) return;

        // Lấy các tháng thực tế xuất hiện trong danh sách giao dịch.
        const months = [
            ...new Set(
                transactionRows
                    .map(row => row.dataset.month || row.dataset.date?.slice(0, 7))
                    .filter(Boolean)
            )
        ].sort((a, b) => b.localeCompare(a));

        monthFilter.replaceChildren();

        const allOption = document.createElement("option");
        allOption.value = "all";
        allOption.textContent = "Tất cả giao dịch";

        monthFilter.appendChild(allOption);

        months.forEach(monthValue => {
            const [year, month] = monthValue.split("-");

            const option = document.createElement("option");
            option.value = monthValue;
            option.textContent = `Tháng ${month}/${year}`;

            monthFilter.appendChild(option);
        });

        // Mặc định xem tất cả giao dịch.
        monthFilter.value = "all";
    }

    // =============================================
    // 5. LỌC GIAO DỊCH
    // =============================================

    function filterTransactions() {
        const typeFilter = $("transactionFilter")?.value || "all";
        const monthFilter = $("monthFilter")?.value || "all";

        const startDate = $("startDateFilter")?.value || "";
        const endDate = $("endDateFilter")?.value || "";

        // Kiểm tra khoảng ngày.
        if (startDate && endDate && startDate > endDate) {
            if ($("emptyTransactions")) {
                $("emptyTransactions").textContent =
                    "Ngày bắt đầu không được sau ngày kết thúc.";

                $("emptyTransactions").style.display = "block";
                $("emptyTransactions").hidden = false;
            }

            transactionRows.forEach(row => {
                row.hidden = true;
            });

            return;
        }

        let visibleCount = 0;

        transactionRows.forEach(row => {
            const rowType = (
                row.dataset.type || ""
            ).toLowerCase();

            const rowDate = row.dataset.date || "";

            const rowMonth =
                row.dataset.month || rowDate.slice(0, 7);

            const matchesType =
                typeFilter === "all" ||
                rowType === typeFilter;

            const matchesMonth =
                monthFilter === "all" ||
                rowMonth === monthFilter;

            const matchesStartDate =
                !startDate || (rowDate && rowDate >= startDate);

            const matchesEndDate =
                !endDate || (rowDate && rowDate <= endDate);

            const isVisible =
                matchesType &&
                matchesMonth &&
                matchesStartDate &&
                matchesEndDate;

            row.hidden = !isVisible;

            if (isVisible) {
                visibleCount++;
            }
        });

        // Cập nhật trạng thái rỗng.
        if ($("emptyTransactions")) {
            const isEmpty = visibleCount === 0;

            $("emptyTransactions").textContent =
                "Không tìm thấy giao dịch phù hợp.";

            $("emptyTransactions").hidden = !isEmpty;
            $("emptyTransactions").style.display =
                isEmpty ? "block" : "none";
        }
    }

    $("transactionFilter")?.addEventListener(
        "change",
        filterTransactions
    );

    $("monthFilter")?.addEventListener(
        "change",
        filterTransactions
    );

    $("startDateFilter")?.addEventListener(
        "change",
        filterTransactions
    );

    $("endDateFilter")?.addEventListener(
        "change",
        filterTransactions
    );

    // Xóa bộ lọc ngày.
    $("clearDateFilter")?.addEventListener("click", () => {
        if ($("startDateFilter")) {
            $("startDateFilter").value = "";
        }

        if ($("endDateFilter")) {
            $("endDateFilter").value = "";
        }

        filterTransactions();
    });

    // =============================================
    // 6. MỞ VÀ ĐÓNG MODAL
    // =============================================

    function openModal() {
        const modal = $("transactionModal");

        if (!modal) return;

        modal.style.display = "flex";
        modal.classList.add("show");
        modal.setAttribute("aria-hidden", "false");

        document.body.classList.add("modal-open");

        $("transactionName")?.focus();
    }

    function closeModal() {
        const modal = $("transactionModal");

        if (!modal) return;

        modal.style.display = "none";
        modal.classList.remove("show");
        modal.setAttribute("aria-hidden", "true");

        document.body.classList.remove("modal-open");
    }

    $("openTransaction")?.addEventListener("click", openModal);
    $("openTransaction2")?.addEventListener("click", openModal);

    $("closeModal")?.addEventListener("click", closeModal);
    $("cancelTransaction")?.addEventListener("click", closeModal);

    // Đóng modal khi nhấn vào nền bên ngoài.
    $("transactionModal")?.addEventListener("click", event => {
        if (event.target === $("transactionModal")) {
            closeModal();
        }
    });

    // Đóng modal bằng phím Escape.
    document.addEventListener("keydown", event => {
        if (
            event.key === "Escape" &&
            $("transactionModal")?.classList.contains("show")
        ) {
            closeModal();
        }
    });

    // =============================================
    // 7. LỌC DANH MỤC THEO LOẠI GIAO DỊCH
    // =============================================

    function filterCategories() {
        const typeSelect = $("transactionType");
        const categorySelect = $("transactionCategory");

        if (!typeSelect || !categorySelect) return;

        const selectedType = typeSelect.value.toLowerCase();

        let selectedOptionIsValid = false;

        Array.from(categorySelect.options).forEach(option => {
            if (!option.value) {
                option.hidden = false;
                return;
            }

            const optionType = (
                option.dataset.type || ""
            ).toLowerCase();

            const matches = optionType === selectedType;

            option.hidden = !matches;

            if (matches && option.selected) {
                selectedOptionIsValid = true;
            }
        });

        if (!selectedOptionIsValid) {
            categorySelect.value = "";
        }
    }

    $("transactionType")?.addEventListener(
        "change",
        filterCategories
    );

    // =============================================
    // 8. KIỂM TRA FORM GIAO DỊCH
    // =============================================

    // Tạo vùng thông báo trong modal.
    function getFormMessage() {
        let message = $("transactionFormMessage");

        if (!message) {
            message = document.createElement("p");
            message.id = "transactionFormMessage";

            message.style.fontSize = "12px";
            message.style.marginTop = "12px";
            message.style.color = "var(--finance-muted)";
            message.setAttribute("role", "status");

            $("transactionForm").appendChild(message);
        }

        return message;
    }

    $("transactionForm").addEventListener("submit", event => {
        // Không gửi form lên Controller.
        event.preventDefault();

        const form = event.currentTarget;

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const name = $("transactionName")?.value.trim() || "";
        const type = $("transactionType")?.value || "";
        const categorySelect = $("transactionCategory");
        const amount = Number($("transactionAmount")?.value);
        const date = $("transactionDate")?.value || "";

        if (
            !name ||
            !type ||
            !categorySelect?.value ||
            !Number.isFinite(amount) ||
            amount < 1000 ||
            !date
        ) {
            getFormMessage().textContent =
                "Vui lòng kiểm tra lại các thông tin đã nhập.";

            return;
        }

        const selectedCategory =
            categorySelect.options[categorySelect.selectedIndex];

        if (
            !selectedCategory ||
            selectedCategory.dataset.type?.toLowerCase() !== type.toLowerCase()
        ) {
            getFormMessage().textContent =
                "Danh mục không phù hợp với loại giao dịch.";

            return;
        }

        // Chỉ kiểm tra dữ liệu. Không thêm vào danh sách,
        // không cập nhật tổng thu/chi và không gửi lên server.
        getFormMessage().textContent =
            "Thông tin hợp lệ. Đây là giao diện mẫu nên giao dịch chưa được thêm hoặc lưu.";

    });

    // =============================================
    // 9. KHỞI TẠO GIAO DIỆN
    // =============================================

    generateMonthOptions();

    // Đặt ngày mặc định cho form.
    if ($("transactionDate")) {
        $("transactionDate").value = localDateValue();
    }

    // Vẽ biểu đồ tháng mặc định.
    updateChart();

    // Khởi tạo bộ lọc danh mục.
    filterCategories();

    // Áp dụng bộ lọc giao dịch mặc định.
    filterTransactions();

});
