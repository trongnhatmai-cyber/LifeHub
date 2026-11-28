/* =====================================================================
   LIFEHUB - TIME MANAGEMENT (QUẢN LÝ THỜI GIAN)  |  wwwroot/js/time.js   (MỘT FILE DUY NHẤT)

   Mọi View trong Views/Time nạp đúng một dòng:
     <script src="~/js/time.js" asp-append-version="true"></script>

   VAI TRÒ: chỉ xử lý tương tác giao diện (không chứa dữ liệu, không tính toán nghiệp vụ):
     1. Toast.   2. Mở / đóng modal, điền sẵn form.   3. Modal chi tiết sự kiện (sửa được / chỉ xem).
     4. Form mô phỏng: kiểm tra rồi báo rõ "chưa lưu" (KHÔNG báo thành công giả).
     5. Tự gửi form lọc / chọn ngày.   6. Bộ lọc nguồn và loại sự kiện trên lịch.
     7. Bộ đếm Pomodoro (chạy ở trình duyệt, chưa lưu phiên).

   QUY ƯỚC HTML (giống study.js / habit.js)
   - Nút mở modal: data-open="<id>" data-mode="create|edit|detail|duplicate|delete" data-title="Tiêu đề modal"
     Các data-* còn lại tự điền vào ô input có name trùng (không phân biệt hoa thường).
     data-evtitle -> ô name="Title" (vì data-title đã dùng cho tiêu đề modal).
   - Chữ trong modal: <strong data-text="evtitle"> được điền từ data-evtitle của nút mở (trống thì hiện "—").
   - Modal chi tiết: [data-if-editable] chỉ hiện khi data-editable="true"; [data-if-readonly] hiện khi không sửa được;
     [data-origin-link] nhận href từ data-originurl; [data-banner] nhận màu từ data-color.
   - Bộ lọc nguồn: nút [data-source-filter="study"] (aria-pressed); phần tử sự kiện mang data-source / data-type.
   - Pomodoro: #pomoTime, #pomoState, #pomoProgress, [data-pomo="start|reset|skip"], [data-pomo-mode="focus|short|long"].
   LƯU Ý CSS: modal và toast là con trực tiếp của .time-page.
===================================================================== */

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    const page = document.querySelector(".time-page");
    if (!page) return;

    // ================================================================ 1. TOAST
    let toastEl = document.getElementById("timeToast");
    let toastTimer = null;
    function toast(message) {
        if (!toastEl) {
            toastEl = document.createElement("div");
            toastEl.className = "time-toast";
            toastEl.setAttribute("role", "status");
            page.appendChild(toastEl);
        }
        toastEl.textContent = message;
        toastEl.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => { toastEl.hidden = true; }, 4500);
    }
    if (toastEl && !toastEl.hidden && toastEl.textContent.trim()) toast(toastEl.textContent.trim());

    const NOT_SAVED = "Đã ghi nhận thao tác trên giao diện.";

    // ================================================================ 2. MODAL
    function openModal(modal) {
        modal.hidden = false;
        document.body.classList.add("time-modal-open");
        const first = modal.querySelector("input:not([type=hidden]):not(:disabled), select:not(:disabled)");
        if (first) first.focus();
    }
    function closeModal(modal) {
        modal.hidden = true;
        if (!document.querySelector(".time-modal:not([hidden])")) document.body.classList.remove("time-modal-open");
    }
    document.querySelectorAll(".time-modal [required]").forEach(el => {
        const field = el.closest(".field");
        if (field) field.classList.add("required");
    });
    document.addEventListener("input", e => {
        const f = e.target.closest && e.target.closest(".field.invalid");
        if (f) f.classList.remove("invalid");
    });

    const RESERVED = new Set(["open", "mode", "action", "title", "editable", "originurl", "text"]);
    const ALIAS = { evtitle: "title", evcolor: "color" };
    let lastTrigger = null;   // nút đã mở modal chi tiết, dùng khi bấm Chỉnh sửa trong modal

    function fillText(modal, data) {
        modal.querySelectorAll("[data-text]").forEach(el => {
            const v = data[el.dataset.text];
            el.textContent = v && v.trim() ? v : "—";
        });
    }

    function prepareForm(modal, trigger, mode) {
        const form = modal.querySelector("form");
        const titleEl = modal.querySelector("[data-modal-title]");
        if (titleEl && trigger.dataset.title) titleEl.textContent = trigger.dataset.title;
        if (!form) return;
        form.reset();
        const idInput = form.querySelector('[name="Id"]');
        if (idInput) idInput.disabled = !(mode === "edit" || mode === "delete");
        const fields = Array.from(form.elements);
        Object.entries(trigger.dataset).forEach(([raw, value]) => {
            if (RESERVED.has(raw)) return;
            const key = ALIAS[raw] || raw;
            const field = fields.find(f => f.name && f.name.toLowerCase() === key);
            if (field) field.value = value;
        });
        if (mode === "duplicate") {
            const t = form.querySelector('[name="Title"]');
            if (t && t.value) t.value = t.value + " (bản sao)";
        }
        form.querySelectorAll(".field.invalid").forEach(f => f.classList.remove("invalid"));
        const err = form.querySelector(".form-error");
        if (err) err.hidden = true;
        fillText(modal, trigger.dataset);
    }

    // ================================================================ 3. MODAL CHI TIẾT
    function prepareDetail(modal, trigger) {
        lastTrigger = trigger;
        const d = trigger.dataset;
        fillText(modal, d);
        const editable = d.editable === "true";
        modal.querySelectorAll("[data-if-editable]").forEach(el => { el.hidden = !editable; });
        modal.querySelectorAll("[data-if-readonly]").forEach(el => { el.hidden = editable; });
        modal.querySelectorAll("[data-origin-link]").forEach(a => {
            a.hidden = editable || !d.originurl;
            if (d.originurl) a.href = d.originurl;
        });
        modal.querySelectorAll("[data-banner]").forEach(el => el.style.setProperty("--ev-color", d.color || ""));
    }

    document.addEventListener("click", event => {
        const opener = event.target.closest("[data-open]");
        if (opener) {
            const modal = document.getElementById(opener.dataset.open);
            if (modal) {
                const mode = opener.dataset.mode || "create";
                if (mode === "detail") prepareDetail(modal, opener); else prepareForm(modal, opener, mode);
                openModal(modal);
            }
            return;
        }

        // Chỉnh sửa từ modal chi tiết: đóng chi tiết, mở form với dữ liệu của sự kiện vừa xem
        const edit = event.target.closest("[data-edit-detail]");
        if (edit && lastTrigger) {
            closeModal(edit.closest(".time-modal"));
            const form = document.getElementById("eventFormModal");
            if (form) {
                const fake = document.createElement("i");
                Object.entries(lastTrigger.dataset).forEach(([k, v]) => { fake.dataset[k] = v; });
                fake.dataset.title = "Chỉnh sửa sự kiện";
                prepareForm(form, fake, "edit");
                openModal(form);
            }
            return;
        }

        const closer = event.target.closest("[data-close]");
        if (closer) { const m = closer.closest(".time-modal"); if (m) closeModal(m); return; }
        if (event.target.classList.contains("time-modal")) { closeModal(event.target); return; }

        // Nút thao tác chưa có Back-end (đánh dấu hoàn thành...)
        if (event.target.closest("[data-simulate-action]")) toast(NOT_SAVED);
    });

    document.addEventListener("keydown", e => {
        if (e.key === "Escape") document.querySelectorAll(".time-modal:not([hidden])").forEach(closeModal);
    });

    // ================================================================ 4. FORM MÔ PHỎNG
    document.querySelectorAll("form[data-simulate]").forEach(form => {
        form.addEventListener("submit", event => {
            event.preventDefault();
            let box = form.querySelector(".form-error");
            if (!box) {
                box = document.createElement("p");
                box.className = "form-error";
                box.setAttribute("role", "alert");
                box.hidden = true;
                form.insertBefore(box, form.querySelector(".time-modal-foot"));
            }
            form.querySelectorAll(".field.invalid").forEach(f => f.classList.remove("invalid"));

            const bad = Array.from(form.elements).filter(el => el.willValidate && !el.checkValidity());
            const s = form.querySelector('[name="StartTime"]'), e = form.querySelector('[name="EndTime"]');
            let message = "";
            if (bad.length) {
                message = "Chưa nhập đúng: " + bad.map(el => {
                    const field = el.closest(".field");
                    if (field) field.classList.add("invalid");
                    const label = field && field.querySelector("label");
                    return label ? label.textContent.replace("*", "").trim() : el.name;
                }).join(", ") + ".";
            } else if (s && e && s.value && e.value && e.value <= s.value) {
                message = "Giờ kết thúc phải sau giờ bắt đầu.";
                e.closest(".field").classList.add("invalid");
            }
            if (message) { box.textContent = message; box.hidden = false; return; }

            box.hidden = true;
            const modal = form.closest(".time-modal");
            if (modal) closeModal(modal);
            toast(NOT_SAVED);
        });
    });

    // ================================================================ 5. TỰ GỬI FORM LỌC
    document.querySelectorAll("[data-autosubmit]").forEach(el => {
        el.addEventListener("change", () => { if (el.form) el.form.requestSubmit(); });
    });

    // ================================================================ 6. BỘ LỌC NGUỒN VÀ LOẠI TRÊN LỊCH
    const srcButtons = Array.from(document.querySelectorAll("[data-source-filter]"));
    const typeSelect = document.querySelector("[data-type-filter]");
    function applyCalendarFilters() {
        const on = new Set(srcButtons.filter(b => b.getAttribute("aria-pressed") === "true").map(b => b.dataset.sourceFilter));
        const type = typeSelect ? typeSelect.value : "all";
        document.querySelectorAll("[data-source]").forEach(el => {
            const okSource = !srcButtons.length || on.has(el.dataset.source);
            const okType = type === "all" || el.dataset.type === type;
            el.hidden = !(okSource && okType);
        });
    }
    srcButtons.forEach(b => b.addEventListener("click", () => {
        b.setAttribute("aria-pressed", b.getAttribute("aria-pressed") === "true" ? "false" : "true");
        b.classList.toggle("active");
        applyCalendarFilters();
    }));
    if (typeSelect) typeSelect.addEventListener("change", applyCalendarFilters);

    // ================================================================ 7. POMODORO
    const timeEl = document.getElementById("pomoTime");
    if (timeEl) {
        const stateEl = document.getElementById("pomoState");
        const ring = document.getElementById("pomoProgress");
        const startBtn = document.querySelector('[data-pomo="start"]');
        const doneEl = document.getElementById("pomoDone");
        const cfg = { focus: document.getElementById("pomoFocus"), short: document.getElementById("pomoShort"), long: document.getElementById("pomoLong") };
        const NAMES = { focus: "Đang tập trung", short: "Nghỉ ngắn", long: "Nghỉ dài" };
        const CIRC = 2 * Math.PI * 45;
        if (ring) { ring.style.strokeDasharray = String(CIRC); }

        const originalTitle = document.title;
        let mode = "focus", total = 0, remaining = 0, running = false, endAt = 0, tick = null, doneFocus = 0;

        const minutes = m => Math.max(1, Math.min(180, parseInt(cfg[m].value, 10) || 25));
        const fmt = s => String(Math.floor(s / 60)).padStart(2, "0") + ":" + String(s % 60).padStart(2, "0");

        function render() {
            timeEl.textContent = fmt(remaining);
            if (ring) ring.style.strokeDashoffset = String(CIRC * (1 - (total ? remaining / total : 0)));
            if (stateEl) stateEl.textContent = running ? NAMES[mode] : (remaining < total && remaining > 0 ? "Đã tạm dừng" : (mode === "focus" ? "Sẵn sàng tập trung" : NAMES[mode]));
            if (startBtn) startBtn.textContent = running ? "Tạm dừng" : (remaining < total && remaining > 0 ? "Tiếp tục" : "Bắt đầu");
            document.querySelectorAll("[data-pomo-mode]").forEach(b => {
                const on = b.dataset.pomoMode === mode;
                b.classList.toggle("active", on);
                b.setAttribute("aria-pressed", on ? "true" : "false");
            });
            document.title = running ? fmt(remaining) + " · " + NAMES[mode] : originalTitle;
        }
        function stop() { running = false; clearInterval(tick); tick = null; }
        function setMode(m) {
            stop();
            mode = m;
            total = remaining = minutes(m) * 60;
            render();
        }
        function saveSession(durMin) {
            const saveUrl = page.getAttribute("data-pomo-save-url");
            const tokenEl = document.querySelector("#pomoSaveTokenForm input[name='__RequestVerificationToken']");
            const targetEl = document.getElementById("pomoTarget");
            const targetVal = targetEl ? targetEl.value : "";
            const targetLabel = targetEl && targetEl.selectedIndex >= 0
                ? targetEl.options[targetEl.selectedIndex].text
                : "Tập trung tự do";

            if (!saveUrl || !tokenEl) {
                toast("Hoàn thành một phiên tập trung.");
                return;
            }

            const body = new URLSearchParams();
            body.set("target", targetVal);
            body.set("durationMinutes", String(durMin));
            body.set("__RequestVerificationToken", tokenEl.value);

            fetch(saveUrl, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
                body: body.toString(),
                credentials: "same-origin"
            })
            .then(r => r.ok ? r.json() : Promise.reject())
            .then(res => {
                toast("Hoàn thành và đã lưu phiên tập trung vào lịch sử.");
                const list = document.getElementById("pomoHistoryList");
                const empty = document.getElementById("pomoEmptyState");
                if (empty) empty.remove();
                if (list) {
                    const row = document.createElement("div");
                    row.className = "history-item";
                    const left = document.createElement("div");
                    const strong = document.createElement("strong");
                    strong.textContent = targetLabel;
                    const small = document.createElement("small");
                    small.textContent = res.completedLabel || "Vừa xong";
                    left.appendChild(strong);
                    left.appendChild(small);
                    const badge = document.createElement("span");
                    badge.className = "summary-badge";
                    badge.textContent = (res.durationMinutes || durMin) + " phút";
                    row.appendChild(left);
                    row.appendChild(badge);
                    const heading = list.querySelector(".section-heading");
                    if (heading && heading.nextSibling) list.insertBefore(row, heading.nextSibling);
                    else list.appendChild(row);
                }
            })
            .catch(() => toast("Hoàn thành phiên tập trung."));
        }
        function finish() {
            stop();
            remaining = 0;
            if (mode === "focus") {
                const durMin = minutes("focus");
                doneFocus += 1;
                if (doneEl) doneEl.textContent = String(doneFocus);
                saveSession(durMin);
                setMode(doneFocus % 4 === 0 ? "long" : "short");
            } else {
                toast("Hết giờ nghỉ. Sẵn sàng cho phiên tiếp theo.");
                setMode("focus");
            }
        }
        function loop() {
            remaining = Math.max(0, Math.round((endAt - Date.now()) / 1000));
            render();
            if (remaining <= 0) finish();
        }
        function toggle() {
            if (running) { stop(); render(); return; }
            if (remaining <= 0) remaining = total;
            endAt = Date.now() + remaining * 1000;
            running = true;
            tick = setInterval(loop, 250);
            render();
        }

        document.querySelector('[data-pomo="start"]').addEventListener("click", toggle);
        document.querySelector('[data-pomo="reset"]').addEventListener("click", () => setMode(mode));
        document.querySelector('[data-pomo="skip"]').addEventListener("click", () => setMode(mode === "focus" ? "short" : "focus"));
        document.querySelectorAll("[data-pomo-mode]").forEach(b => b.addEventListener("click", () => setMode(b.dataset.pomoMode)));
        Object.entries(cfg).forEach(([m, input]) => input.addEventListener("change", () => { if (!running && mode === m) setMode(m); }));
        setMode("focus");
    }
});
