/* =====================================================================
   LIFEHUB - PROJECT DETAIL (KANBAN KIỂU JIRA)
   Kéo thả nhiệm vụ, giao việc, gửi duyệt / duyệt / yêu cầu làm lại,
   bình luận, lịch sử, tick việc con, lọc bảng.
   ===================================================================== */
(function () {
    'use strict';

    var page = document.getElementById('projectDetail');
    if (!page) return;

    var ME = {
        id: page.dataset.userId,
        name: page.dataset.userName,
        canManage: page.dataset.canManage === 'true',
        canEdit: page.dataset.canEdit === 'true'
    };

    var LABELS = { todo: 'Cần làm', in_progress: 'Đang làm', review: 'Chờ duyệt', done: 'Hoàn thành' };
    var PROJECT_ID = page.dataset.projectId;

    var dragging = null;     // { type: 'task' | 'member', id }
    var reworkPending = null; // { id, to }
    var filterUser = '';

    /* ---------- Tiện ích ---------- */
    function toast(message, type) {
        if (window.ProjectUI && window.ProjectUI.toast) window.ProjectUI.toast(message, type);
    }

    async function postAction(action, values) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var body = new URLSearchParams();
        Object.keys(values).forEach(function (key) { body.set(key, values[key] == null ? "" : values[key]); });
        if (token) body.set("__RequestVerificationToken", token.value);
        var response = await fetch(action.startsWith("/") ? action : "/Project/" + action, {
            method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded", "X-Requested-With": "XMLHttpRequest" }, body: body
        });
        return response.ok;
    }

    function normalize(text) {
        return (text || '').toString().toLowerCase().normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').trim();
    }

    function pad(n) { return n < 10 ? '0' + n : String(n); }

    function nowText() {
        var d = new Date();
        return pad(d.getDate()) + '/' + pad(d.getMonth() + 1) + '/' + d.getFullYear() + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    }

    // Node kéo thả có thể là text node, nên lấy phần tử cha trước khi closest()
    function closest(node, selector) {
        var el = node && node.nodeType === 1 ? node : (node ? node.parentElement : null);
        return el ? el.closest(selector) : null;
    }

    function cardById(id) { return page.querySelector('[data-task-card][data-task-id="' + id + '"]'); }
    function modalById(id) { return document.getElementById('taskModal-' + id); }
    function keyOf(card) { return card.querySelector('.task-key').textContent.trim(); }
    function titleOf(card) { return card.querySelector('h4').textContent.trim(); }

    // Mở/đóng modal bằng cách "bấm" một nút ẩn, để dùng lại logic của project.js
    function openById(id) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.hidden = true;
        btn.setAttribute('data-modal-open', id);
        page.appendChild(btn);
        btn.click();
        btn.remove();
    }

    function closeById(id) {
        var modal = document.getElementById(id);
        var closer = modal ? modal.querySelector('[data-modal-close]') : null;
        if (closer) closer.click();
    }

    /* ---------- Đồng bộ giao diện ---------- */
    function syncColumns() {
        Array.prototype.forEach.call(page.querySelectorAll('[data-column]'), function (col) {
            var body = col.querySelector('.kanban-col__body');
            var count = body.querySelectorAll('[data-task-card]').length;
            col.querySelector('.proj-tab-count').textContent = count;

            var empty = body.querySelector('.kanban-empty');
            if (count === 0 && !empty) {
                var p = document.createElement('p');
                p.className = 'kanban-empty';
                p.textContent = 'Chưa có nhiệm vụ';
                body.appendChild(p);
            } else if (count > 0 && empty) {
                empty.remove();
            }
        });
    }

    function updateProgress() {
        var cards = page.querySelectorAll('[data-task-card]');
        var total = cards.length;
        var done = page.querySelectorAll('[data-task-card][data-status="done"]').length;
        var pct = total === 0 ? 0 : Math.round(done * 100 / total);

        var pctEl = document.getElementById('statProgress');
        var barEl = document.getElementById('statProgressBar');
        var doneEl = document.getElementById('statDone');
        if (pctEl) pctEl.textContent = pct + '%';
        if (barEl) barEl.style.width = pct + '%';
        if (doneEl) doneEl.textContent = done;
    }

    function syncMembers() {
        Array.prototype.forEach.call(page.querySelectorAll('[data-member-chip]'), function (chip) {
            var n = page.querySelectorAll(
                '[data-task-card][data-assignee="' + chip.dataset.userId + '"]:not([data-status="done"])').length;
            var span = chip.querySelector('[data-open-count]');
            if (span) span.textContent = n;
        });
    }

    // Hiện/ẩn các nút Gửi duyệt / Duyệt hoàn thành / Yêu cầu làm lại theo vai trò và trạng thái
    function refreshActions(modal, status) {
        var card = cardById(modal.dataset.taskId);
        if (!card) return;
        var mine = !card.dataset.assignee || card.dataset.assignee === ME.id;
        var show = {
            submit: ME.canEdit && (status === 'todo' || status === 'in_progress') && (ME.canManage || mine),
            approve: ME.canManage && status === 'review',
            rework: ME.canManage && (status === 'review' || status === 'done')
        };
        Array.prototype.forEach.call(modal.querySelectorAll('[data-review-action]'), function (btn) {
            btn.hidden = !show[btn.dataset.reviewAction];
        });
    }

    /* ---------- Bình luận và lịch sử ---------- */
    function syncCommentCount(modal) {
        var n = modal.querySelectorAll('[data-comment-list] .comment').length;
        var total = modal.querySelector('[data-comment-total]');
        if (total) total.textContent = n;

        var card = cardById(modal.dataset.taskId);
        var badge = card ? card.querySelector('[data-comment-count]') : null;
        if (badge) {
            badge.textContent = '💬 ' + n;
            badge.hidden = n === 0;
        }
    }

    function addComment(modal, text, isRework) {
        var list = modal.querySelector('[data-comment-list]');
        if (!list) return;

        var empty = list.querySelector('[data-comment-empty]');
        if (empty) empty.remove();

        var li = document.createElement('li');
        li.className = 'comment' + (isRework ? ' comment--rework' : '');
        li.innerHTML =
            '<span class="comment-avatar"></span>' +
            '<div class="comment-body">' +
            '<p class="comment-head"><strong></strong> <small>Vừa xong</small></p>' +
            '<p class="comment-text"></p>' +
            '</div>';
        li.querySelector('.comment-avatar').textContent = ME.name.charAt(0).toUpperCase();
        li.querySelector('strong').textContent = ME.name;
        li.querySelector('.comment-text').textContent = text;

        if (isRework) {
            var flag = document.createElement('span');
            flag.className = 'comment-flag';
            flag.textContent = 'Yêu cầu làm lại';
            li.querySelector('.comment-head').appendChild(flag);
        }

        list.appendChild(li);
        syncCommentCount(modal);
    }

    function addHistory(modal, text) {
        var list = modal.querySelector('[data-history-list]');
        if (!list) return;

        var li = document.createElement('li');
        li.innerHTML = '<span class="act-dot"></span><div><p><strong></strong> <span></span></p><small>Vừa xong</small></div>';
        li.querySelector('strong').textContent = ME.name;
        li.querySelector('p span').textContent = text;
        list.insertBefore(li, list.firstChild);

        var updated = modal.querySelector('[data-updated]');
        if (updated) updated.textContent = nowText();
    }

    /* ---------- Chuyển trạng thái ---------- */
    function moveError(card, to) {
        var from = card.dataset.status;
        if (!ME.canEdit) return 'Bạn chỉ có quyền xem dự án này.';
        if (ME.canManage) return null;

        // Member
        if (card.dataset.assignee && card.dataset.assignee !== ME.id)
            return 'Bạn chỉ chuyển được nhiệm vụ của mình hoặc nhiệm vụ chưa giao.';
        if (to === 'done')
            return 'Chỉ trưởng nhóm mới duyệt sang "Hoàn thành". Hãy chuyển sang "Chờ duyệt".';
        if (from === 'done')
            return 'Nhiệm vụ đã được duyệt, chỉ trưởng nhóm mới mở lại được.';
        return null;
    }

    // Trưởng nhóm kéo từ Chờ duyệt/Hoàn thành về chưa xong thì phải nhập lý do
    function needsReason(from, to) {
        return ME.canManage && (from === 'review' || from === 'done') && (to === 'todo' || to === 'in_progress');
    }

    async function requestMove(id, to, reason) {
        var card = cardById(id);
        if (!card) return false;

        var from = card.dataset.status;
        if (from === to) return false;

        var error = moveError(card, to);
        if (error) {
            toast(error, 'error');
            return false;
        }

        if (!reason && needsReason(from, to)) {
            openRework(id, to);
            return false;
        }
        try {
            var ok = await postAction("MoveTask", { projectId: PROJECT_ID, taskId: id, status: to, openTask: id, reason: reason || "" });
            if (!ok) throw new Error("Request rejected");
        } catch (_) { toast("Không thể cập nhật nhiệm vụ. Vui lòng thử lại.", "error"); return false; }
        applyMove(id, to, reason || '');
        return true;
    }

    function applyMove(id, to, reason) {
        var card = cardById(id);
        var body = page.querySelector('[data-column][data-status="' + to + '"] .kanban-col__body');
        if (!card || !body) return;

        body.appendChild(card);
        card.dataset.status = to;
        syncColumns();
        updateProgress();
        syncMembers();

        var modal = modalById(id);
        if (modal) {
            var select = modal.querySelector('[data-status-select]');
            if (select) select.value = to;

            if (reason) addComment(modal, reason, true);
            addHistory(modal, reason
                ? 'đã trả nhiệm vụ về "' + LABELS[to] + '" để làm lại'
                : (to === 'done'
                    ? 'đã duyệt và chuyển sang "Hoàn thành"'
                    : 'đã chuyển sang "' + LABELS[to] + '"'));

            var resolvedRow = modal.querySelector('[data-resolved-row]');
            var resolved = modal.querySelector('[data-resolved]');
            if (resolvedRow && resolved) {
                resolvedRow.hidden = to !== 'done';
                if (to === 'done') resolved.textContent = nowText();
            }
            refreshActions(modal, to);
        }

        toast(reason
            ? 'Đã trả ' + keyOf(card) + ' về "' + LABELS[to] + '" kèm lý do'
            : 'Đã chuyển ' + keyOf(card) + ' sang "' + LABELS[to] + '"', 'success');
    }

    /* ---------- Hộp thoại "Yêu cầu làm lại" ---------- */
    var reworkModal = document.getElementById('reworkModal');
    var reworkForm = reworkModal ? reworkModal.querySelector('[data-rework-form]') : null;

    function openRework(id, to) {
        if (!reworkModal || !reworkForm) return;
        var card = cardById(id);
        reworkPending = { id: id, to: to };

        openById('reworkModal'); // mở trước vì project.js sẽ reset form khi mở
        document.getElementById('reworkTaskName').textContent = card ? keyOf(card) + ' · ' + titleOf(card) : '';
        document.getElementById('reworkTarget').value = to;
    }

    if (reworkForm) {
        reworkForm.addEventListener('submit', function (e) {
            e.preventDefault();

            var textarea = document.getElementById('reworkReason');
            var reason = textarea.value.trim();
            var note = reworkForm.querySelector('[data-form-note]');

            if (!reason) {
                if (note) {
                    note.textContent = 'Vui lòng nhập lý do để người thực hiện biết cần sửa gì.';
                    note.setAttribute('data-state', 'error');
                    note.hidden = false;
                }
                textarea.focus();
                return;
            }

            if (!reworkPending) return;
            var pending = reworkPending;
            reworkPending = null;

            var target = document.getElementById('reworkTarget').value;
            requestMove(pending.id, target, reason).then(function (saved) { if (saved) closeById('reworkModal'); });
        });
    }

    /* ---------- Giao việc ---------- */
    async function assignTask(id, userId) {
        var card = cardById(id);
        if (!card) return;

        if (!ME.canManage) {
            toast('Chỉ trưởng nhóm mới giao việc được.', 'error');
            return;
        }
        try {
            var ok = await postAction("AssignTask", { projectId: PROJECT_ID, taskId: id, assigneeId: userId || "", openTask: id });
            if (!ok) throw new Error("Request rejected");
        } catch (_) { toast("Không thể giao nhiệm vụ. Vui lòng thử lại.", "error"); return false; }

        var name = '';
        if (userId) {
            var chip = page.querySelector('[data-member-chip][data-user-id="' + userId + '"]');
            name = chip ? chip.dataset.name : '';
        }

        card.dataset.assignee = userId || '';
        var avatar = card.querySelector('.task-assignee');
        if (avatar) {
            avatar.textContent = name ? name.charAt(0).toUpperCase() : '?';
            avatar.title = name || 'Chưa giao';
            avatar.classList.toggle('task-assignee--empty', !name);
        }

        var modal = modalById(id);
        if (modal) {
            var select = modal.querySelector('[data-assign-select]');
            if (select) select.value = userId || '';
            addHistory(modal, name ? 'đã giao cho ' + name : 'đã bỏ người thực hiện');
            refreshActions(modal, card.dataset.status);
        }

        syncMembers();
        applyBoardFilter();
        toast(name
            ? 'Đã giao ' + keyOf(card) + ' cho ' + name
            : 'Đã bỏ người thực hiện của ' + keyOf(card), 'success');
        return true;
    }

    /* ---------- Lọc bảng ---------- */
    var searchInput = document.getElementById('boardSearch');
    var clearBtn = document.getElementById('boardClear');

    function applyBoardFilter() {
        var q = normalize(searchInput ? searchInput.value : '');
        Array.prototype.forEach.call(page.querySelectorAll('[data-task-card]'), function (card) {
            var okText = !q || normalize(card.dataset.search).indexOf(q) !== -1;
            var okUser = !filterUser || card.dataset.assignee === filterUser;
            card.hidden = !(okText && okUser);
        });
        if (clearBtn) clearBtn.hidden = !(q || filterUser);
    }

    if (searchInput) searchInput.addEventListener('input', applyBoardFilter);

    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            if (searchInput) searchInput.value = '';
            filterUser = '';
            Array.prototype.forEach.call(page.querySelectorAll('[data-filter-user]'), function (b) {
                b.classList.remove('is-active');
                b.setAttribute('aria-pressed', 'false');
            });
            applyBoardFilter();
        });
    }

    /* ---------- Sự kiện click / change / submit ---------- */
    page.addEventListener('click', function (e) {
        // Lọc theo người thực hiện
        var avatarBtn = e.target.closest('[data-filter-user]');
        if (avatarBtn) {
            var uid = avatarBtn.getAttribute('data-filter-user');
            filterUser = filterUser === uid ? '' : uid;
            Array.prototype.forEach.call(page.querySelectorAll('[data-filter-user]'), function (b) {
                var active = b.getAttribute('data-filter-user') === filterUser;
                b.classList.toggle('is-active', active);
                b.setAttribute('aria-pressed', active ? 'true' : 'false');
            });
            applyBoardFilter();
            return;
        }

        // Tab Bình luận / Lịch sử
        var tab = e.target.closest('[data-activity-tab]');
        if (tab) {
            var modalEl = tab.closest('[data-task-modal]');
            var key = tab.getAttribute('data-activity-tab');
            Array.prototype.forEach.call(modalEl.querySelectorAll('[data-activity-tab]'), function (t) {
                t.classList.toggle('is-active', t === tab);
            });
            Array.prototype.forEach.call(modalEl.querySelectorAll('[data-activity-panel]'), function (p) {
                p.hidden = p.getAttribute('data-activity-panel') !== key;
            });
            return;
        }

        // Nút Gửi duyệt / Duyệt hoàn thành / Yêu cầu làm lại
        var review = e.target.closest('[data-review-action]');
        if (review) {
            var rid = review.closest('[data-task-modal]').dataset.taskId;
            var action = review.dataset.reviewAction;
            if (action === 'submit') requestMove(rid, 'review');
            else if (action === 'approve') requestMove(rid, 'done');
            else if (action === 'rework') requestMove(rid, 'in_progress');
            return;
        }

        // Tick việc con
        var subBtn = e.target.closest('[data-sub-toggle]');
        if (subBtn) {
            var row = subBtn.closest('.sub-row');
            var m = subBtn.closest('[data-task-modal]');
            var subtaskId = subBtn.dataset.subtaskId;
            postAction("ToggleSubtask", { projectId: PROJECT_ID, taskId: subtaskId, openTask: m.dataset.taskId }).then(function (ok) {
                if (!ok) { toast("Không thể cập nhật việc con.", "error"); return; }
            var done = row.classList.toggle('is-done');
            subBtn.textContent = done ? '✓' : '○';
            subBtn.title = done ? 'Mở lại' : 'Đánh dấu xong';
            subBtn.setAttribute('aria-label', subBtn.title);

            var total = m.querySelectorAll('.sub-row').length;
            var finished = m.querySelectorAll('.sub-row.is-done').length;
            var counter = m.querySelector('[data-sub-count]');
            if (counter) counter.textContent = '(' + finished + '/' + total + ')';

            var card = cardById(m.dataset.taskId);
            var cardSub = card ? card.querySelector('[data-card-sub]') : null;
            if (cardSub) cardSub.textContent = '☑ ' + finished + '/' + total + ' việc con';

            toast(done ? 'Đã hoàn thành việc con' : 'Đã mở lại việc con', 'success');
            });
        }
    });

    page.addEventListener('change', function (e) {
        var statusSelect = e.target.closest ? e.target.closest('[data-status-select]') : null;
        if (statusSelect) {
            var id = statusSelect.closest('[data-task-modal]').dataset.taskId;
            var card = cardById(id);
            requestMove(id, statusSelect.value).then(function (saved) { if (!saved && card) statusSelect.value = card.dataset.status; });
            return;
        }

        var assignSelect = e.target.closest ? e.target.closest('[data-assign-select]') : null;
        if (assignSelect) {
            var taskId = assignSelect.closest('[data-task-modal]').dataset.taskId;
            var previousAssignee = cardById(taskId)?.dataset.assignee || "";
            assignTask(taskId, assignSelect.value).then(function (saved) { if (!saved) assignSelect.value = previousAssignee; });
        }
    });

    page.addEventListener('submit', function (e) {
        var form = e.target.closest ? e.target.closest('[data-comment-form]') : null;
        if (!form) return;

        e.preventDefault();

        var textarea = form.querySelector('textarea');
        var text = textarea.value.trim();
        if (!text) {
            textarea.focus();
            return;
        }

        var formData = new FormData(form);
        postAction(form.action, Object.fromEntries(formData.entries())).then(function (saved) {
            if (!saved) { toast("Không thể lưu bình luận.", "error"); return; }
            addComment(form.closest('[data-task-modal]'), text, false);
            textarea.value = '';
            toast('Đã thêm bình luận', 'success');
        });
    });

    /* ---------- Kéo thả ---------- */
    function clearHighlights() {
        Array.prototype.forEach.call(page.querySelectorAll('.is-drop-target'), function (el) {
            el.classList.remove('is-drop-target');
        });
    }

    function resetDrag() {
        clearHighlights();
        Array.prototype.forEach.call(page.querySelectorAll('.is-dragging'), function (el) {
            el.classList.remove('is-dragging');
        });
        dragging = null;
    }

    page.addEventListener('dragstart', function (e) {
        var card = closest(e.target, '[data-task-card]');
        var chip = closest(e.target, '[data-member-chip]');

        if (card && card.getAttribute('draggable') === 'true') {
            dragging = { type: 'task', id: card.dataset.taskId };
            card.classList.add('is-dragging');
        } else if (chip && chip.getAttribute('draggable') === 'true') {
            dragging = { type: 'member', id: chip.dataset.userId };
            chip.classList.add('is-dragging');
        } else {
            return;
        }

        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', dragging.type + ':' + dragging.id); // Firefox cần setData
    });

    page.addEventListener('dragover', function (e) {
        if (!dragging) return;

        var target = dragging.type === 'task'
            ? closest(e.target, '[data-column]')
            : closest(e.target, '[data-task-card]');
        if (!target) return;

        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        if (!target.classList.contains('is-drop-target')) {
            clearHighlights();
            target.classList.add('is-drop-target');
        }
    });

    page.addEventListener('drop', function (e) {
        if (!dragging) return;
        var info = dragging;

        if (info.type === 'task') {
            var column = closest(e.target, '[data-column]');
            if (!column) return;
            e.preventDefault();
            resetDrag();
            requestMove(info.id, column.dataset.status);
            return;
        }

        var card = closest(e.target, '[data-task-card]');
        if (!card) return;
        e.preventDefault();
        resetDrag();
        assignTask(card.dataset.taskId, info.id);
    });

    page.addEventListener('dragend', resetDrag);

    /* ---------- Tab Workspace ---------- */
    var wsTabs = Array.prototype.slice.call(page.querySelectorAll('[data-ws-tab]'));
    var wsPanels = Array.prototype.slice.call(page.querySelectorAll('[data-ws-panel]'));
    var TAB_KEY = 'lifehub.project.tab.' + PROJECT_ID;

    // Đồng bộ trạng thái ở tab Danh sách với bảng Kanban (thẻ có thể vừa được kéo sang cột khác)
    function syncList() {
        Array.prototype.forEach.call(page.querySelectorAll('[data-list-row]'), function (row) {
            var card = cardById(row.dataset.taskId);
            if (!card) return;
            row.dataset.status = card.dataset.status;
            row.dataset.assignee = card.dataset.assignee || 'none';
            var pill = row.querySelector('[data-list-status]');
            if (pill) {
                pill.dataset.status = card.dataset.status;
                pill.textContent = LABELS[card.dataset.status] || pill.textContent;
            }
        });
        applyListFilter();
    }

    function showTab(name, focus) {
        var exists = wsPanels.some(function (p) { return p.dataset.wsPanel === name; });
        if (!exists) name = 'overview';
        wsTabs.forEach(function (t) {
            var on = t.dataset.wsTab === name;
            t.setAttribute('aria-selected', on ? 'true' : 'false');
            t.tabIndex = on ? 0 : -1;
            if (on && focus) t.focus();
        });
        wsPanels.forEach(function (p) { p.hidden = p.dataset.wsPanel !== name; });
        if (name === 'list') syncList();
        try { sessionStorage.setItem(TAB_KEY, name); } catch (e) { /* bỏ qua */ }
    }

    wsTabs.forEach(function (tab, i) {
        tab.addEventListener('click', function () { showTab(tab.dataset.wsTab); });
        tab.addEventListener('keydown', function (e) {
            var next = null;
            if (e.key === 'ArrowRight') next = wsTabs[(i + 1) % wsTabs.length];
            else if (e.key === 'ArrowLeft') next = wsTabs[(i - 1 + wsTabs.length) % wsTabs.length];
            else if (e.key === 'Home') next = wsTabs[0];
            else if (e.key === 'End') next = wsTabs[wsTabs.length - 1];
            if (next) { e.preventDefault(); showTab(next.dataset.wsTab, true); }
        });
    });

    // Liên kết "Xem tất cả →" trong tab Tổng quan
    page.addEventListener('click', function (e) {
        var go = closest(e.target, '[data-ws-go]');
        if (!go) return;
        e.preventDefault();
        showTab(go.dataset.wsGo);
        page.scrollIntoView({ block: 'start' });
    });

    /* ---------- Bộ lọc tab Danh sách ---------- */
    var listSearch = document.getElementById('listSearch');
    var listStatus = document.getElementById('listStatus');
    var listPriority = document.getElementById('listPriority');
    var listAssignee = document.getElementById('listAssignee');
    var listEmpty = document.getElementById('listEmpty');

    function applyListFilter() {
        var rows = page.querySelectorAll('[data-list-row]');
        var q = normalize(listSearch ? listSearch.value : '');
        var st = listStatus ? listStatus.value : '';
        var pr = listPriority ? listPriority.value : '';
        var as = listAssignee ? listAssignee.value : '';
        var shown = 0;
        Array.prototype.forEach.call(rows, function (row) {
            var ok = (!st || row.dataset.status === st) &&
                     (!pr || row.dataset.priority === pr) &&
                     (!as || row.dataset.assignee === as) &&
                     (!q || normalize(row.dataset.search).indexOf(q) !== -1);
            row.hidden = !ok;
            if (ok) shown++;
        });
        if (listEmpty) {
            listEmpty.hidden = shown !== 0 || rows.length === 0;
            if (rows.length > 0) listEmpty.querySelector('td').textContent = 'Không có nhiệm vụ khớp bộ lọc.';
        }
    }
    [listSearch, listStatus, listPriority, listAssignee].forEach(function (el) {
        if (el) el.addEventListener(el.tagName === 'INPUT' ? 'input' : 'change', applyListFilter);
    });

    // Dòng trong bảng mở được bằng bàn phím (Enter / Space)
    page.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' && e.key !== ' ') return;
        var row = closest(e.target, '[data-list-row]');
        if (row && e.target === row) { e.preventDefault(); row.click(); }
    });

    // Tab ban đầu: #task-123 mở tab Bảng; nếu không thì khôi phục tab đã dùng trước khi tải lại trang
    var initial = 'overview';
    if (/^#task-\d+$/.test(window.location.hash)) {
        initial = 'board';
    } else {
        try { initial = sessionStorage.getItem(TAB_KEY) || 'overview'; } catch (e) { /* bỏ qua */ }
    }
    showTab(initial);

    /* ---------- Khởi tạo ---------- */
    Array.prototype.forEach.call(page.querySelectorAll('[data-task-modal]'), function (modal) {
        var card = cardById(modal.dataset.taskId);
        if (card) refreshActions(modal, card.dataset.status);
    });
})();
