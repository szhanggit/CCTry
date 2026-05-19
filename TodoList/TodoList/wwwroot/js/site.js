$(document).ready(function () {

    // ── CSRF Setup ────────────────────────────────────────────────────────────
    var token = $('input[name="__RequestVerificationToken"]').val();
    $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });

    // ── Modal instances ───────────────────────────────────────────────────────
    var todoModal   = new bootstrap.Modal(document.getElementById('todoModal'));
    var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    var pendingDeleteId = null;

    // ── Open Add modal ────────────────────────────────────────────────────────
    $('#btnAddTodo').on('click', function () {
        openAddModal();
    });

    // ── Open Edit modal ───────────────────────────────────────────────────────
    $(document).on('click', '.btn-edit', function () {
        var id = $(this).closest('[data-id]').data('id');
        openEditModal(id);
    });

    // ── Open Delete confirm ───────────────────────────────────────────────────
    $(document).on('click', '.btn-delete', function () {
        pendingDeleteId = $(this).closest('[data-id]').data('id');
        deleteModal.show();
    });

    // ── Confirm delete ────────────────────────────────────────────────────────
    $('#btnConfirmDelete').on('click', function () {
        if (pendingDeleteId === null) return;
        var id = pendingDeleteId;
        $.post('/Todo/Delete/' + id)
            .done(function (res) {
                if (res.success) removeItem(id);
            })
            .always(function () {
                deleteModal.hide();
                pendingDeleteId = null;
            });
    });

    // ── Toggle complete ───────────────────────────────────────────────────────
    $(document).on('change', '.toggle-complete', function () {
        var $checkbox = $(this);
        var $item     = $checkbox.closest('[data-id]');
        var id        = $item.data('id');

        $.post('/Todo/Toggle/' + id)
            .done(function (res) {
                if (res.success) {
                    $item.find('.todo-title').toggleClass('todo-completed', res.isCompleted);
                    $checkbox.prop('checked', res.isCompleted);
                    updateActiveCount(res.isCompleted ? -1 : 1);
                } else {
                    $checkbox.prop('checked', !$checkbox.prop('checked'));
                }
            })
            .fail(function () {
                $checkbox.prop('checked', !$checkbox.prop('checked'));
            });
    });

    // ── Form submit (Add / Edit) ───────────────────────────────────────────────
    $('#todoForm').on('submit', function (e) {
        e.preventDefault();
        var $modal = $('#todoModal');
        var mode   = $modal.data('mode');
        var id     = $modal.data('edit-id');

        var wasCompleted = false;
        if (mode === 'edit') {
            wasCompleted = $('[data-id="' + id + '"]').find('.toggle-complete').is(':checked');
        }

        var data = {
            title:       $('#inputTitle').val().trim(),
            description: $('#inputDescription').val().trim() || null,
            dueDate:     $('#inputDueDate').val() || null,
            isCompleted: $('#inputIsCompleted').is(':checked')
        };

        clearFormErrors();

        var url = mode === 'edit' ? '/Todo/Edit/' + id : '/Todo/Create';
        $.ajax({ url: url, method: 'POST', contentType: 'application/json', data: JSON.stringify(data) })
            .done(function (res) {
                if (res.success) {
                    todoModal.hide();
                    if (mode === 'edit') {
                        updateItemInDom(res.item);
                        var isNowCompleted = res.item.isCompleted;
                        if (wasCompleted !== isNowCompleted)
                            updateActiveCount(isNowCompleted ? -1 : 1);
                    } else {
                        prependItem(res.item);
                        updateActiveCount(1);
                    }
                } else {
                    showFormErrors(res.errors || ['An error occurred. Please try again.']);
                }
            })
            .fail(function () {
                showFormErrors(['An unexpected error occurred. Please try again.']);
            });
    });

    // ── Modal helpers ─────────────────────────────────────────────────────────
    function openAddModal() {
        $('#todoModalLabel').text('Add Todo');
        $('#todoForm')[0].reset();
        $('#isCompletedRow').hide();
        clearFormErrors();
        $('#todoModal').data('mode', 'add').removeData('edit-id');
        todoModal.show();
        setTimeout(function () { $('#inputTitle').focus(); }, 300);
    }

    function openEditModal(id) {
        $.get('/Todo/Edit/' + id)
            .done(function (res) {
                if (!res.success) return;
                var item = res.item;
                $('#todoModalLabel').text('Edit Todo');
                $('#inputTitle').val(item.title);
                $('#inputDescription').val(item.description || '');
                $('#inputDueDate').val(item.dueDate ? item.dueDate.substring(0, 10) : '');
                $('#inputIsCompleted').prop('checked', item.isCompleted);
                $('#isCompletedRow').show();
                clearFormErrors();
                $('#todoModal').data('mode', 'edit').data('edit-id', id);
                todoModal.show();
                setTimeout(function () { $('#inputTitle').focus(); }, 300);
            });
    }

    // ── DOM update helpers ────────────────────────────────────────────────────
    function buildItemHtml(item) {
        var dueBadge = '';
        if (item.dueDate) {
            var due = new Date(item.dueDate);
            var isOverdue = due < new Date() && !item.isCompleted;
            var cls = isOverdue ? 'bg-danger text-white' : 'bg-warning text-dark';
            dueBadge = '<span class="badge ' + cls + ' ms-2">Due: ' + due.toLocaleDateString() + '</span>';
        }
        var titleCls = item.isCompleted ? 'todo-title todo-completed' : 'todo-title';
        var checked  = item.isCompleted ? 'checked' : '';
        var desc     = item.description
            ? '<div><small class="text-muted">' + escHtml(item.description) + '</small></div>'
            : '';

        return '<li class="list-group-item d-flex align-items-start gap-2 py-3" data-id="' + item.id + '">' +
            '<div class="pt-1">' +
            '<input type="checkbox" class="form-check-input toggle-complete" ' + checked + ' />' +
            '</div>' +
            '<div class="flex-grow-1 min-width-0">' +
            '<div><span class="' + titleCls + '">' + escHtml(item.title) + '</span>' + dueBadge + '</div>' +
            desc +
            '</div>' +
            '<div class="d-flex gap-1 flex-shrink-0">' +
            '<button class="btn btn-sm btn-outline-secondary btn-edit" title="Edit"><i class="bi bi-pencil"></i></button>' +
            '<button class="btn btn-sm btn-outline-danger btn-delete" title="Delete"><i class="bi bi-trash"></i></button>' +
            '</div></li>';
    }

    function prependItem(item) {
        $('#todoList').prepend(buildItemHtml(item));
        $('#emptyState').hide();
    }

    function updateItemInDom(item) {
        var $li = $('[data-id="' + item.id + '"]');

        $li.find('.todo-title')
            .text(item.title)
            .toggleClass('todo-completed', item.isCompleted);

        $li.find('.badge').remove();
        if (item.dueDate) {
            var due = new Date(item.dueDate);
            var isOverdue = due < new Date() && !item.isCompleted;
            var cls = isOverdue ? 'bg-danger text-white' : 'bg-warning text-dark';
            $li.find('.todo-title')
               .after('<span class="badge ' + cls + ' ms-2">Due: ' + due.toLocaleDateString() + '</span>');
        }

        $li.find('small.text-muted').parent().remove();
        if (item.description) {
            $li.find('.flex-grow-1')
               .append('<div><small class="text-muted">' + escHtml(item.description) + '</small></div>');
        }

        $li.find('.toggle-complete').prop('checked', item.isCompleted);
    }

    function removeItem(id) {
        var $li = $('[data-id="' + id + '"]');
        var wasActive = !$li.find('.toggle-complete').is(':checked');
        $li.fadeOut(200, function () {
            $(this).remove();
            if ($('#todoList li').length === 0) $('#emptyState').show();
        });
        if (wasActive) updateActiveCount(-1);
    }

    function updateActiveCount(delta) {
        var $badge = $('#activeCount');
        var next = Math.max(0, (parseInt($badge.text()) || 0) + delta);
        $badge.text(next);
        $('#nav-active-count').text(next + ' items left');
    }

    // ── Form error helpers ────────────────────────────────────────────────────
    function showFormErrors(errors) {
        var html = errors.map(function (e) { return '<div>' + escHtml(e) + '</div>'; }).join('');
        $('#formErrors').html(html).show();
    }

    function clearFormErrors() {
        $('#formErrors').hide().html('');
    }

    function escHtml(text) {
        return $('<div>').text(text || '').html();
    }
});
