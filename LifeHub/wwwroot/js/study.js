
// =========================================
// STUDY MANAGEMENT - JAVASCRIPT
// =========================================

document.addEventListener("DOMContentLoaded", function () {

    // Dữ liệu bài tập mẫu
    let assignments = [
        {
            name: "Bài tập SQL - Truy vấn dữ liệu",
            subject: "Cơ sở dữ liệu",
            deadline: "16/11/2026",
            status: "Chưa hoàn thành"
        },
        {
            name: "Bài thực hành lập trình Web",
            subject: "Lập trình Web",
            deadline: "18/11/2026",
            status: "Chưa hoàn thành"
        },
        {
            name: "Bài tập chương 3",
            subject: "Toán rời rạc",
            deadline: "20/11/2026",
            status: "Chưa hoàn thành"
        }
    ];

    // Lấy các phần tử HTML
    const assignmentContainer = document.getElementById("asg");
    const assignmentCount = document.getElementById("asgCnt");
    const addAssignmentButton = document.getElementById("addAssignmentBtn");
    const addSubjectButton = document.getElementById("addSubjectBtn");

    // Hiển thị danh sách bài tập
    function renderAssignments() {
        if (!assignmentContainer || !assignmentCount) return;

        assignmentContainer.innerHTML = "";

        assignments.forEach(function (assignment, index) {

            const item = document.createElement("div");
            item.className = "study-exam";

            const info = document.createElement("div");
            info.className = "study-exam-info";

            const title = document.createElement("strong");
            title.textContent = assignment.name;

            const detail = document.createElement("small");
            detail.textContent =
                assignment.subject + " · Hạn: " + assignment.deadline;

            info.appendChild(title);
            info.appendChild(detail);

            const completeButton = document.createElement("button");
            completeButton.type = "button";
            completeButton.className = "study-btn";
            completeButton.textContent = "Hoàn thành";
            completeButton.style.padding = "7px 10px";
            completeButton.style.fontSize = "11px";

            completeButton.addEventListener("click", function () {
                assignments.splice(index, 1);
                renderAssignments();
            });

            item.appendChild(info);
            item.appendChild(completeButton);

            assignmentContainer.appendChild(item);
        });

        assignmentCount.textContent = assignments.length;
    }

    // Thêm bài tập mẫu qua hộp thoại nhập liệu
    if (addAssignmentButton) {
        addAssignmentButton.addEventListener("click", function () {

            const name = prompt("Nhập tên bài tập:");
            if (!name || !name.trim()) return;

            const subject = prompt("Nhập tên môn học:");
            if (!subject || !subject.trim()) return;

            const deadline = prompt("Nhập hạn nộp (VD: 25/11/2026):");
            if (!deadline || !deadline.trim()) return;

            assignments.push({
                name: name.trim(),
                subject: subject.trim(),
                deadline: deadline.trim(),
                status: "Chưa hoàn thành"
            });

            renderAssignments();
        });
    }

    // Nút thêm môn học - hiện thông báo chức năng đang chờ kết nối
    if (addSubjectButton) {
        addSubjectButton.addEventListener("click", function () {
            alert("Chức năng thêm môn học sẽ được kết nối ở bước quản lý dữ liệu.");
        });
    }

    // Hiển thị dữ liệu lần đầu
    renderAssignments();
});
