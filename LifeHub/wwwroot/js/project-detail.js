/* =====================================================================
   LIFEHUB - PROJECT DETAIL: kéo thả Kanban và kéo thành viên vào nhiệm vụ.
   JS chỉ điền form ẩn rồi submit; việc lưu dữ liệu do Controller/Service làm.
   ===================================================================== */
(function () {
    'use strict';

    var page = document.getElementById('projectDetail');
    if (!page) return;

    var moveForm = document.getElementById('moveTaskForm');
    var assignForm = document.getElementById('assignTaskForm');
    var dragging = null; // { type: 'task' | 'member', id, status }

    // Node có thể là text node, nên lấy phần tử cha trước khi closest()
    function closest(node, selector) {
        var el = node && node.nodeType === 1 ? node : (node ? node.parentElement : null);
        return el ? el.closest(selector) : null;
    }

    function clearHighlights() {
        Array.prototype.forEach.call(page.querySelectorAll('.is-drop-target'), function (el) {
            el.classList.remove('is-drop-target');
        });
    }

    function reset() {
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
            var column = closest(card, '[data-column]');
            dragging = { type: 'task', id: card.dataset.taskId, status: column ? column.dataset.status : '' };
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
            reset();

            var status = column.dataset.status;
            if (status === info.status || !moveForm) return; // thả lại cùng cột thì bỏ qua

            moveForm.elements.taskId.value = info.id;
            moveForm.elements.status.value = status;
            moveForm.submit();
            return;
        }

        var card = closest(e.target, '[data-task-card]');
        if (!card) return;
        e.preventDefault();
        reset();

        if (!assignForm) return;
        assignForm.elements.taskId.value = card.dataset.taskId;
        assignForm.elements.assigneeId.value = info.id;
        assignForm.submit();
    });

    page.addEventListener('dragend', reset);
})();