/* =========================================================
   LIFEHUB - TASK MANAGEMENT
   Dùng chung cho:
   - Index.cshtml
   - Task.cshtml
   - Category.cshtml
   - _TaskModal.cshtml
   - _CategoryModal.cshtml

   Chức năng:
   1. Khởi tạo module
   2. Mở / đóng modal công việc
   3. Xem chi tiết công việc
   4. Chuyển chế độ thêm / sửa / xóa công việc
   5. Chọn loại công việc cá nhân / dự án
   6. Lọc công việc trên Kanban
   7. Mở / đóng modal danh mục
   8. Xem chi tiết / sửa / xóa danh mục

   Form công việc và danh mục gửi POST về TaskController; JS chỉ quản lý modal và lọc.
   ========================================================= */

document.addEventListener("DOMContentLoaded", function () {

    "use strict";

    /* =====================================================
       01. COMMON HELPERS
       ===================================================== */

    const $ = (selector, parent = document) => {
        return parent.querySelector(selector);
    };

    const $$ = (selector, parent = document) => {
        return Array.from(parent.querySelectorAll(selector));
    };

    // Gán nội dung text an toàn cho phần tử.
    function setText(element, value, fallback = "—") {
        if (!element) return;

        const text = value === null || value === undefined || value === ""
            ? fallback
            : String(value);

        element.textContent = text;
    }

    // Đóng tất cả section trong modal.
    function hideSections(modal, selector) {
        if (!modal) return;

        $$(selector, modal).forEach(section => {
            section.hidden = true;
        });
    }

    // Mở một section theo mode.
    function showSection(modal, selector, mode) {
        if (!modal) return;

        hideSections(modal, selector);

        const section = $(
            `${selector}[data-${selector.includes("task") ? "task" : "category"}-modal-section="${mode}"]`,
            modal
        );

        if (section) {
            section.hidden = false;
        }
    }

    // Đưa phần tử về trạng thái mặc định.
    function resetForm(form) {
        if (!form) return;

        form.reset();

        // Xóa các thông báo validation cũ.
        $$("[data-valmsg-for]", form).forEach(message => {
            message.textContent = "";
        });
    }

    // Chuyển chuỗi thành dạng màu an toàn để hiển thị.
    function normalizeColor(value, fallback = "#ea580c") {
        if (!value) return fallback;

        const color = String(value).trim();

        // Chỉ chấp nhận mã màu hex.
        if (/^#[0-9a-fA-F]{3,8}$/.test(color)) {
            return color;
        }

        return fallback;
    }


    /* =====================================================
       02. TASK MODAL - ELEMENTS
       ===================================================== */

    const taskModal = $("#taskModal");
    const taskForm = $("#taskForm");

    let currentTaskId = null;
    let lastTaskTrigger = null;

    const taskModalTitle = $("#taskModalTitle", taskModal || document);

    // Tìm dữ liệu công việc theo ID trong vùng data-task-record.
    function getTaskRecord(taskId) {
        if (!taskModal || !taskId) return null;

        return $$(".task-modal-data [data-task-record]", taskModal)
            .find(record => record.dataset.id === String(taskId)) || null;
    }

    // Lấy một field trong form công việc.
    function getTaskField(name) {
        if (!taskForm) return null;

        return $(`[data-task-field="${name}"]`, taskForm);
    }

    // Gán giá trị cho field công việc.
    function setTaskField(name, value) {
        const field = getTaskField(name);

        if (!field) return;

        field.value = value ?? "";
    }


    /* =====================================================
       03. TASK MODAL - OPEN / CLOSE
       ===================================================== */

    function openTaskModal(trigger, mode = "detail") {
        if (!taskModal) return;

        lastTaskTrigger = trigger || document.activeElement;

        taskModal.hidden = false;
        document.body.classList.add("task-modal-open");

        const taskId = trigger?.dataset.taskId;

        currentTaskId = taskId || null;

        if (mode === "create") {
            prepareTaskCreate();
        }
        else if (mode === "detail") {
            prepareTaskDetail(taskId);
        }
        else if (mode === "edit") {
            prepareTaskEdit(taskId);
        }
        else if (mode === "delete") {
            prepareTaskDelete(taskId);
        }

        const closeButton = $("[data-close-task-modal]", taskModal);

        if (closeButton) {
            closeButton.focus();
        }
    }

    function closeTaskModal() {
        if (!taskModal) return;

        taskModal.hidden = true;
        document.body.classList.remove("task-modal-open");

        if (lastTaskTrigger && typeof lastTaskTrigger.focus === "function") {
            lastTaskTrigger.focus();
        }
    }

    // Mở modal từ các nút / thẻ có data-open-task-modal.
    $$("[data-open-task-modal]").forEach(trigger => {
        trigger.addEventListener("click", function () {
            const mode = this.dataset.modalMode || "detail";
            openTaskModal(this, mode);
        });

        // Hỗ trợ nhấn Enter hoặc Space trên thẻ công việc.
        if (trigger.tagName.toLowerCase() !== "button") {
            trigger.addEventListener("keydown", function (event) {
                if (event.key !== "Enter" && event.key !== " ") {
                    return;
                }

                event.preventDefault();

                const mode = this.dataset.modalMode || "detail";
                openTaskModal(this, mode);
            });
        }
    });

    // Đóng modal bằng nút hoặc backdrop.
    if (taskModal) {
        $$("[data-close-task-modal]", taskModal).forEach(button => {
            button.addEventListener("click", closeTaskModal);
        });
    }


    /* =====================================================
       04. TASK MODAL - DETAIL MODE
       ===================================================== */

    function prepareTaskDetail(taskId) {
        if (!taskModal) return;

        const record = getTaskRecord(taskId);

        if (!record) {
            console.warn("Không tìm thấy dữ liệu công việc:", taskId);
            return;
        }

        hideSections(taskModal, "[data-task-modal-section]");

        const section = $(
            '[data-task-modal-section="detail"]',
            taskModal
        );

        if (section) {
            section.hidden = false;
        }

        if (taskModalTitle) {
            taskModalTitle.textContent = "Chi tiết công việc";
        }

        // Thông tin cơ bản.
        setText($("[data-detail-title]", taskModal), record.dataset.title);
        setText($("[data-detail-description]", taskModal), record.dataset.description, "Chưa có mô tả");

        // Trạng thái.
        const statusElement = $("[data-detail-status]", taskModal);

        if (statusElement) {
            setText(statusElement, record.dataset.statusLabel || record.dataset.status);

            statusElement.className = "task-status-label";
            statusElement.classList.add(record.dataset.status || "");
        }

        // Mức độ ưu tiên.
        const priorityElement = $("[data-detail-priority]", taskModal);

        if (priorityElement) {
            setText(priorityElement, record.dataset.priorityLabel || record.dataset.priority);

            priorityElement.className = "task-priority";
            priorityElement.classList.add(record.dataset.priority || "");
        }

        // Phạm vi công việc.
        const scopeLabels = {
            personal: "Cá nhân",
            assigned: "Được giao",
            project: "Dự án"
        };

        setText(
            $("[data-detail-scope]", taskModal),
            scopeLabels[record.dataset.scope] || record.dataset.scope
        );

        setText(
            $("[data-detail-category]", taskModal),
            record.dataset.categoryName
        );

        setText(
            $("[data-detail-project]", taskModal),
            record.dataset.projectName
        );

        setText(
            $("[data-detail-assignee]", taskModal),
            record.dataset.assignee
        );

        setText(
            $("[data-detail-assigner]", taskModal),
            record.dataset.assigner
        );

        // Ngày hoàn thành.
        setText(
            $("[data-detail-deadline]", taskModal),
            formatDate(record.dataset.deadline),
            "Chưa có hạn"
        );

        // Thời gian ước tính.
        const estimate = record.dataset.estimate;

        setText(
            $("[data-detail-estimate]", taskModal),
            estimate ? `${estimate} phút` : "",
            "Chưa xác định"
        );

        // Danh sách nhãn.
        renderTaskTags(record);

        // Công việc con.
        renderTaskSubtasks(record);

        // Bình luận.
        renderTaskComments(record);
    }

    // Hiển thị ngày theo định dạng dd/MM/yyyy.
    function formatDate(value) {
        if (!value) return "";

        const parts = value.split("-");

        if (parts.length !== 3) {
            return value;
        }

        return `${parts[2]}/${parts[1]}/${parts[0]}`;
    }

    // Render danh sách tags.
    function renderTaskTags(record) {
        const container = $("[data-detail-tags]", taskModal);

        if (!container) return;

        container.replaceChildren();

        const tags = $$("[data-record-tags] [data-tag-name]", record);

        if (tags.length === 0) {
            const empty = document.createElement("span");
            empty.textContent = "Chưa có nhãn";
            container.appendChild(empty);
            return;
        }

        tags.forEach(tag => {
            const item = document.createElement("span");

            item.className = "task-detail-tag";
            item.textContent = tag.dataset.tagName || "";

            const color = normalizeColor(tag.dataset.tagColor, "");

            if (color) {
                item.style.borderColor = color;
            }

            container.appendChild(item);
        });
    }

    // Hiển thị tiến độ công việc con.
    function renderTaskSubtasks(record) {
        const container = $("[data-detail-subtasks]", taskModal);

        if (!container) return;

        container.replaceChildren();

        const total = Number(record.dataset.subtaskTotal || 0);
        const done = Number(record.dataset.subtaskDone || 0);

        const message = document.createElement("p");

        if (total <= 0) {
            message.textContent = "Chưa có công việc con.";
        } else {
            message.textContent = `${done}/${total} công việc con đã hoàn thành.`;
        }

        container.appendChild(message);
    }

    // Hiển thị bình luận.
    function renderTaskComments(record) {
        const container = $("[data-detail-comments]", taskModal);

        if (!container) return;

        container.replaceChildren();

        const comments = $$("[data-record-comments] [data-comment]", record);

        if (comments.length === 0) {
            const empty = document.createElement("p");
            empty.textContent = "Chưa có bình luận.";
            container.appendChild(empty);
            return;
        }

        comments.forEach(comment => {
            const item = document.createElement("div");
            item.className = "task-detail-comment";

            const author = document.createElement("strong");
            author.textContent = comment.dataset.author || "Người dùng";

            const content = document.createElement("p");
            content.textContent = comment.dataset.content || "";

            const created = document.createElement("small");
            created.textContent = comment.dataset.created || "";

            item.append(author, content, created);
            container.appendChild(item);
        });
    }


    /* =====================================================
       05. TASK MODAL - CREATE MODE
       ===================================================== */

    function prepareTaskCreate() {
        if (!taskModal) return;

        hideSections(taskModal, "[data-task-modal-section]");

        const section = $(
            '[data-task-modal-section="form"]',
            taskModal
        );

        if (section) {
            section.hidden = false;
        }

        if (taskModalTitle) {
            taskModalTitle.textContent = "Thêm công việc";
        }

        currentTaskId = null;

        resetForm(taskForm);

        setTaskField("id", "");
        setTaskField("type", "personal");

        // Mặc định chọn loại cá nhân.
        setTaskType("personal");

        const submitButton = $("[data-task-form-submit]", taskForm);

        if (submitButton) {
            submitButton.textContent = "Thêm công việc";
        }
    }


    /* =====================================================
       06. TASK MODAL - EDIT MODE
       ===================================================== */

    function prepareTaskEdit(taskId) {
        if (!taskModal) return;

        const record = getTaskRecord(taskId);

        if (!record) {
            console.warn("Không tìm thấy công việc để chỉnh sửa:", taskId);
            return;
        }

        hideSections(taskModal, "[data-task-modal-section]");

        const section = $(
            '[data-task-modal-section="form"]',
            taskModal
        );

        if (section) {
            section.hidden = false;
        }

        if (taskModalTitle) {
            taskModalTitle.textContent = "Chỉnh sửa công việc";
        }

        resetForm(taskForm);

        currentTaskId = taskId;

        // Đổ dữ liệu vào form.
        setTaskField("id", record.dataset.id);
        setTaskField("title", record.dataset.title);
        setTaskField("description", record.dataset.description);
        setTaskField("priority", record.dataset.priority);
        setTaskField("status", record.dataset.status);
        setTaskField("deadline", record.dataset.deadline);
        setTaskField("estimate", record.dataset.estimate);
        setTaskField("category", record.dataset.categoryId);
        setTaskField("project", record.dataset.projectId);
        setTaskField("assignee", record.dataset.assignee);

        // Đổ danh sách nhãn vào input.
        const tagsInput = getTaskField("tags");

        if (tagsInput) {
            const tags = $$("[data-record-tags] [data-tag-name]", record)
                .map(tag => tag.dataset.tagName)
                .filter(Boolean);

            tagsInput.value = tags.join(", ");
        }

        const type = record.dataset.scope === "project"
            ? "project"
            : "personal";

        setTaskType(type);

        const submitButton = $("[data-task-form-submit]", taskForm);

        if (submitButton) {
            submitButton.textContent = "Lưu thay đổi";
        }
    }


    /* =====================================================
       07. TASK MODAL - DELETE MODE
       ===================================================== */

    function prepareTaskDelete(taskId) {
        if (!taskModal) return;

        const record = getTaskRecord(taskId);

        if (!record) {
            console.warn("Không tìm thấy công việc cần xóa:", taskId);
            return;
        }

        hideSections(taskModal, "[data-task-modal-section]");

        const section = $(
            '[data-task-modal-section="delete"]',
            taskModal
        );

        if (section) {
            section.hidden = false;
        }

        if (taskModalTitle) {
            taskModalTitle.textContent = "Xóa công việc";
        }

        currentTaskId = taskId;

        const deleteId = $(`[data-task-delete-id]`, taskModal);
        if (deleteId) deleteId.value = taskId;

        setText(
            $("[data-delete-task-title]", taskModal),
            record.dataset.title
        );
    }


    /* =====================================================
       08. TASK MODAL - SWITCH MODE
       ===================================================== */

    if (taskModal) {
        $$("[data-task-modal-mode]", taskModal).forEach(button => {
            button.addEventListener("click", function () {
                const mode = this.dataset.taskModalMode;

                if (mode === "edit") {
                    prepareTaskEdit(currentTaskId);
                }

                if (mode === "delete") {
                    prepareTaskDelete(currentTaskId);
                }

                if (mode === "detail") {
                    prepareTaskDetail(currentTaskId);
                }
            });
        });
    }


    /* =====================================================
       09. TASK TYPE - PERSONAL / PROJECT
       ===================================================== */

    function setTaskType(type) {
        if (!taskModal) return;

        const isProject = type === "project";

        // Cập nhật hidden input TaskType.
        setTaskField("type", type);

        // Cập nhật trạng thái các nút.
        $$("[data-task-type]", taskModal).forEach(button => {
            const active = button.dataset.taskType === type;

            button.classList.toggle("active", active);
            button.setAttribute("aria-pressed", String(active));
        });

        // Ẩn / hiện nhóm field tương ứng.
        $$("[data-personal-fields]", taskModal).forEach(field => {
            field.hidden = isProject;
        });

        $$("[data-project-fields]", taskModal).forEach(field => {
            field.hidden = !isProject;
        });

        // Khi chuyển loại, xóa giá trị không thuộc loại đang chọn.
        if (isProject) {
            setTaskField("category", "");
        } else {
            setTaskField("project", "");
            setTaskField("assignee", "");
        }
    }

    if (taskModal) {
        $$("[data-task-type]", taskModal).forEach(button => {
            button.addEventListener("click", function () {
                setTaskType(this.dataset.taskType);
            });
        });
    }


    /* =====================================================
       10. TASK MODAL - PROJECT MEMBER FILTER
       ===================================================== */

    const projectSelect = getTaskField("project");
    const assigneeSelect = getTaskField("assignee");

    if (projectSelect && assigneeSelect) {

        // Lưu lại toàn bộ option thành viên ban đầu.
        const originalAssigneeOptions = Array.from(assigneeSelect.options)
            .map(option => option.cloneNode(true));

        projectSelect.addEventListener("change", function () {
            const selectedProject = this.value;
            const currentAssignee = assigneeSelect.value;

            assigneeSelect.replaceChildren();

            // Giữ option mặc định.
            const defaultOption = originalAssigneeOptions[0]?.cloneNode(true);

            if (defaultOption) {
                assigneeSelect.appendChild(defaultOption);
            }

            // Chỉ hiển thị thành viên thuộc dự án đã chọn.
            originalAssigneeOptions.slice(1).forEach(option => {
                if (option.dataset.projectId === selectedProject) {
                    assigneeSelect.appendChild(option.cloneNode(true));
                }
            });

            // Giữ thành viên nếu vẫn thuộc dự án đang chọn.
            const validAssignee = Array.from(assigneeSelect.options)
                .some(option => option.value === currentAssignee);

            assigneeSelect.value = validAssignee ? currentAssignee : "";
        });
    }


    /* =====================================================
       11. TASK MODAL - FORM SUBMIT
       ===================================================== */

    if (taskForm) {
        taskForm.addEventListener("submit", function (event) {
            if (!taskForm.reportValidity()) {
                event.preventDefault();
            }
        });

        const submitButton = $("[data-task-form-submit]", taskForm);

        if (submitButton) {
            submitButton.addEventListener("click", function () {
                if (!taskForm.reportValidity()) {
                    return;
                }

                taskForm.requestSubmit();
            });
        }
    }

    // Nút xác nhận xóa công việc.
    const taskDeleteButton = $("[data-task-delete-confirm]", taskModal || document);

    if (taskDeleteButton) {
        taskDeleteButton.addEventListener("click", function () {
            if (!currentTaskId) return;

            taskModal.dispatchEvent(
                new CustomEvent("task:delete-request", {
                    bubbles: true,
                    detail: {
                        taskId: currentTaskId
                    }
                })
            );

        });
    }


    /* =====================================================
       12. TASK LIST - FILTER
       ===================================================== */

    const taskSearch = $("#taskSearch");
    const taskStatusFilter = $("#taskStatusFilter");
    const taskPriorityFilter = $("#taskPriorityFilter");
    const taskProjectFilter = $("#taskProjectFilter");
    const taskCategoryFilter = $("#taskCategoryFilter");

    const kanbanCards = $$(".task-card[data-status]");

    function filterTasks() {
        if (kanbanCards.length === 0) return;

        const keyword = taskSearch?.value.trim().toLowerCase() || "";
        const status = taskStatusFilter?.value || "";
        const priority = taskPriorityFilter?.value || "";
        const project = taskProjectFilter?.value || "";
        const category = taskCategoryFilter?.value || "";

        kanbanCards.forEach(card => {
            const cardSearch = (card.dataset.search || card.textContent)
                .toLowerCase();

            const matchKeyword = !keyword || cardSearch.includes(keyword);
            const matchStatus = !status || card.dataset.status === status;
            const matchPriority = !priority || card.dataset.priority === priority;
            const matchProject = !project || card.dataset.projectId === project;
            const matchCategory = !category || card.dataset.categoryId === category;

            const visible =
                matchKeyword &&
                matchStatus &&
                matchPriority &&
                matchProject &&
                matchCategory;

            card.hidden = !visible;
        });

        refreshKanbanCounts();
    }

    // Gắn sự kiện tìm kiếm.
    [
        taskSearch,
        taskStatusFilter,
        taskPriorityFilter,
        taskProjectFilter,
        taskCategoryFilter
    ].forEach(input => {
        if (!input) return;

        input.addEventListener(
            input === taskSearch ? "input" : "change",
            filterTasks
        );
    });


    /* =====================================================
       13. TASK LIST - KANBAN COUNTS
       ===================================================== */

    function refreshKanbanCounts() {
        $$(".task-kanban-column").forEach(column => {
            const cards = $$(".task-card", column)
                .filter(card => !card.hidden);

            const countElement = $(
                ".task-kanban-column__header > span",
                column
            );

            if (countElement) {
                countElement.textContent = cards.length;
            }

            const emptyElement = $(".task-kanban-empty", column);

            if (emptyElement) {
                emptyElement.hidden = cards.length > 0;
            }
        });
    }

    refreshKanbanCounts();


    /* =====================================================
       14. CATEGORY MODAL - ELEMENTS
       ===================================================== */

    const categoryModal = $("#taskCategoryModal");
    const categoryForm = $("#taskCategoryForm");

    let currentCategoryId = null;
    let lastCategoryTrigger = null;

    const categoryModalTitle = $("#categoryModalTitle", categoryModal || document);

    function getCategoryRecord(categoryId) {
        if (!categoryModal || !categoryId) return null;

        return $$(".category-modal-data [data-category-record]", categoryModal)
            .find(record => record.dataset.id === String(categoryId)) || null;
    }

    function getCategoryField(name) {
        if (!categoryForm) return null;

        return $(`[data-category-field="${name}"]`, categoryForm);
    }

    function setCategoryField(name, value) {
        const field = getCategoryField(name);

        if (!field) return;

        field.value = value ?? "";
    }


    /* =====================================================
       15. CATEGORY MODAL - OPEN / CLOSE
       ===================================================== */

    function openCategoryModal(trigger, mode = "detail") {
        if (!categoryModal) return;

        lastCategoryTrigger = trigger || document.activeElement;

        categoryModal.hidden = false;
        document.body.classList.add("task-modal-open");

        const categoryId = trigger?.dataset.categoryId;

        currentCategoryId = categoryId || null;

        if (mode === "create") {
            prepareCategoryCreate();
        }
        else if (mode === "detail") {
            prepareCategoryDetail(categoryId);
        }
        else if (mode === "edit") {
            prepareCategoryEdit(categoryId);
        }
        else if (mode === "delete") {
            prepareCategoryDelete(categoryId);
        }

        const closeButton = $("[data-close-category-modal]", categoryModal);

        if (closeButton) {
            closeButton.focus();
        }
    }

    function closeCategoryModal() {
        if (!categoryModal) return;

        categoryModal.hidden = true;
        document.body.classList.remove("task-modal-open");

        if (
            lastCategoryTrigger &&
            typeof lastCategoryTrigger.focus === "function"
        ) {
            lastCategoryTrigger.focus();
        }
    }

    $$("[data-open-category-modal]").forEach(trigger => {
        trigger.addEventListener("click", function () {
            const mode = this.dataset.modalMode || "detail";
            openCategoryModal(this, mode);
        });
    });

    if (categoryModal) {
        $$("[data-close-category-modal]", categoryModal).forEach(button => {
            button.addEventListener("click", closeCategoryModal);
        });
    }


    /* =====================================================
       16. CATEGORY MODAL - DETAIL MODE
       ===================================================== */

    function prepareCategoryDetail(categoryId) {
        if (!categoryModal) return;

        const record = getCategoryRecord(categoryId);

        if (!record) {
            console.warn("Không tìm thấy danh mục:", categoryId);
            return;
        }

        hideSections(categoryModal, "[data-category-modal-section]");

        const section = $(
            '[data-category-modal-section="detail"]',
            categoryModal
        );

        if (section) {
            section.hidden = false;
        }

        if (categoryModalTitle) {
            categoryModalTitle.textContent = "Chi tiết danh mục";
        }

        const icon = $("[data-category-detail-icon]", categoryModal);
        const color = $("[data-category-detail-color]", categoryModal);

        setText(
            $("[data-category-detail-name]", categoryModal),
            record.dataset.name
        );

        setText(
            $("[data-category-detail-description]", categoryModal),
            record.dataset.description,
            "Danh mục chưa có mô tả."
        );

        setText(
            $("[data-category-detail-count]", categoryModal),
            record.dataset.count,
            "0"
        );

        setText(
            $("[data-category-detail-color-name]", categoryModal),
            record.dataset.color
        );

        setText(
            $("[data-category-detail-icon-name]", categoryModal),
            record.dataset.icon
        );

        if (icon) {
            icon.textContent = getCategoryIconText(record.dataset.icon);
            icon.style.color = normalizeColor(record.dataset.color);
            icon.style.backgroundColor =
                `color-mix(in srgb, ${normalizeColor(record.dataset.color)} 13%, transparent)`;
        }

        if (color) {
            color.style.backgroundColor = normalizeColor(record.dataset.color);
        }
    }

    // Chuyển mã icon thành ký hiệu hiển thị đơn giản.
    function getCategoryIconText(icon) {
        const icons = {
            folder: "▰",
            book: "▤",
            briefcase: "▣",
            target: "◎",
            heart: "♥",
            star: "★",
            calendar: "▦"
        };

        return icons[icon] || "▧";
    }


    /* =====================================================
       17. CATEGORY MODAL - CREATE MODE
       ===================================================== */

    function prepareCategoryCreate() {
        if (!categoryModal) return;

        hideSections(categoryModal, "[data-category-modal-section]");

        const section = $(
            '[data-category-modal-section="form"]',
            categoryModal
        );

        if (section) {
            section.hidden = false;
        }

        if (categoryModalTitle) {
            categoryModalTitle.textContent = "Thêm danh mục";
        }

        currentCategoryId = null;

        resetForm(categoryForm);

        setCategoryField("id", "");

        const submitButton = $("[data-category-form-submit]", categoryForm);

        if (submitButton) {
            submitButton.textContent = "Thêm danh mục";
        }
    }


    /* =====================================================
       18. CATEGORY MODAL - EDIT MODE
       ===================================================== */

    function prepareCategoryEdit(categoryId) {
        if (!categoryModal) return;

        const record = getCategoryRecord(categoryId);

        if (!record) {
            console.warn("Không tìm thấy danh mục để chỉnh sửa:", categoryId);
            return;
        }

        hideSections(categoryModal, "[data-category-modal-section]");

        const section = $(
            '[data-category-modal-section="form"]',
            categoryModal
        );

        if (section) {
            section.hidden = false;
        }

        if (categoryModalTitle) {
            categoryModalTitle.textContent = "Chỉnh sửa danh mục";
        }

        resetForm(categoryForm);

        currentCategoryId = categoryId;

        setCategoryField("id", record.dataset.id);
        setCategoryField("name", record.dataset.name);
        setCategoryField("description", record.dataset.description);
        setCategoryField("color", normalizeColor(record.dataset.color));
        setCategoryField("icon", record.dataset.icon);

        const submitButton = $("[data-category-form-submit]", categoryForm);

        if (submitButton) {
            submitButton.textContent = "Lưu thay đổi";
        }
    }


    /* =====================================================
       19. CATEGORY MODAL - DELETE MODE
       ===================================================== */

    function prepareCategoryDelete(categoryId) {
        if (!categoryModal) return;

        const record = getCategoryRecord(categoryId);

        if (!record) {
            console.warn("Không tìm thấy danh mục cần xóa:", categoryId);
            return;
        }

        hideSections(categoryModal, "[data-category-modal-section]");

        const section = $(
            '[data-category-modal-section="delete"]',
            categoryModal
        );

        if (section) {
            section.hidden = false;
        }

        if (categoryModalTitle) {
            categoryModalTitle.textContent = "Xóa danh mục";
        }

        currentCategoryId = categoryId;

        setText(
            $("[data-category-delete-name]", categoryModal),
            record.dataset.name
        );
    }


    /* =====================================================
       20. CATEGORY MODAL - SWITCH MODE
       ===================================================== */

    if (categoryModal) {
        $$("[data-category-modal-mode]", categoryModal).forEach(button => {
            button.addEventListener("click", function () {
                const mode = this.dataset.categoryModalMode;

                if (mode === "edit") {
                    prepareCategoryEdit(currentCategoryId);
                }

                if (mode === "delete") {
                    prepareCategoryDelete(currentCategoryId);
                }

                if (mode === "detail") {
                    prepareCategoryDetail(currentCategoryId);
                }
            });
        });
    }


    /* =====================================================
       21. CATEGORY MODAL - FORM SUBMIT
       ===================================================== */

    if (categoryForm) {
        categoryForm.addEventListener("submit", function (event) {
            if (!categoryForm.reportValidity()) {
                event.preventDefault();
            }
        });

        const submitButton = $(
            "[data-category-form-submit]",
            categoryForm
        );

        if (submitButton) {
            submitButton.addEventListener("click", function () {
                if (!categoryForm.reportValidity()) {
                    return;
                }

                categoryForm.requestSubmit();
            });
        }
    }

    // Nút xác nhận xóa danh mục.
    const categoryDeleteButton = $(
        "[data-category-delete-confirm]",
        categoryModal || document
    );

    if (categoryDeleteButton) {
        categoryDeleteButton.addEventListener("click", function () {
            if (!currentCategoryId) return;

            const deleteId = $(`[data-category-delete-id]`, categoryModal);
            if (deleteId) deleteId.value = currentCategoryId;

            categoryModal.dispatchEvent(
                new CustomEvent("category:delete-request", {
                    bubbles: true,
                    detail: {
                        categoryId: currentCategoryId
                    }
                })
            );

        });
    }


    /* =====================================================
       22. ESCAPE KEY - CLOSE MODAL
       ===================================================== */

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") return;

        if (taskModal && !taskModal.hidden) {
            closeTaskModal();
            return;
        }

        if (categoryModal && !categoryModal.hidden) {
            closeCategoryModal();
        }
    });


    /* =====================================================
       23. MODULE INITIALIZATION
       ===================================================== */

    // Chỉ khởi tạo phần tử tồn tại trên trang hiện tại.
    // Trang Index không có Kanban vẫn hoạt động bình thường.
    // Trang Task không có category modal vẫn hoạt động bình thường.

    console.info("LIFEHUB Task Management initialized.");

});
