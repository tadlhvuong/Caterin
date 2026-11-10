'use strict';

$(function () {
    let borderColor, bodyBg, headingColor;

    if (isDarkStyle) {
        borderColor = config.colors_dark.borderColor;
        bodyBg = config.colors_dark.bodyBg;
        headingColor = config.colors_dark.headingColor;
    } else {
        borderColor = config.colors.borderColor;
        bodyBg = config.colors.bodyBg;
        headingColor = config.colors.headingColor;
    }

    var dt_user_table = $('.datatables-users'),
        select2 = $('.select2'),
        userView = '',
    statusObj = {
        0: { title: 'Chờ kích hoạt', class: 'bg-label-info' },
        1: { title: 'Hoạt động', class: 'bg-label-success' },
        2: { title: 'Đình chỉ"', class: 'bg-label-warning' },
        3: { title: 'Đã xóa', class: 'bg-label-danger' },
        };


    if (select2.length) {
        var $this = select2;
        $this.wrap('<div class="position-relative"></div>').select2({
            placeholder: 'Select Country',
            dropdownParent: $this.parent()
        });
    }


    // Variable declaration for table
    $.fn.dataTable.ext.pager.numbers_length = 5;
    if (dt_user_table.length) {
        var dt_user = dt_user_table.DataTable({
            processing: true,
            serverSide: true,
            paging: true,
            pagingType: "simple_numbers",   
            ajax: {
                url: '/admin/user/get-users',
                type: 'POST',
                contentType: 'application/json',
                data: function (res) {
                    var orderBy = res.order && res.order.length ? res.order[0] : null;
                    
                    return JSON.stringify({
                        draw: res.draw,
                        start: res.start,
                        length: res.length,
                        search: res.search ? res.search.value : null,
                        sortColumn: orderBy ? res.columns[orderBy.column].data : null,
                        sortDirection: orderBy ? orderBy.dir : null,
                        userStatus: $('#FilterUsersStatus').val() ? parseInt($('#FilterUsersStatus').val(), 10) : null,
                        roleId: $('#FilterUsersRole').val() || null
                    });
                },

                error: function (xhr, error, thrown) {
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Có lỗi xảy ra',
                        text: 'Không thể tải danh sách đơn hàng.',
                        confirmButtonText: 'Đóng'
                    });
                }
            },
            select: {
                style: 'multi'
            },

            columns: [
                { data: 'id' },
                { data: 'id' },
                { data: 'userName' },
                { data: 'roles' },
                { data: 'status' },
                { data: '' }
            ],
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // For Checkboxes
                    targets: 1,
                    orderable: false,
                    checkboxes: {
                        selectAllRender: '<input type="checkbox" class="form-check-input">'
                    },
                    render: function () {
                        return '<input type="checkbox" class="dt-checkboxes form-check-input" >';
                    },
                    searchable: false
                },
                {
                    // User full name and email
                    targets: 2,
                    responsivePriority: 3,
                    render: function (data, type, full) {
                        const name = full.userName || '';
                        const email = full.email || '';
                        const userView = '/admin/user/user-details/'
                            + encodeURIComponent(full.id);

                        let output;
                        if (full.avatarUrl) {
                            output =
                                '<img src="' + full.avatarUrl + '" alt="Avatar" class="rounded-circle" style=" object-fit: contain;">';
                        } else {
                            const initials = name
                                .trim()
                                .split(/\s+/)
                                .map(part => part.charAt(0))
                                .filter(Boolean);

                            const displayInitials = (
                                (initials[0] || '') +
                                (initials.length > 1
                                    ? initials[initials.length - 1]
                                    : '')
                            ).toUpperCase();

                            output =
                                '<span class="avatar-initial rounded-circle bg-label-secondary">'
                                + displayInitials +
                                '</span>';
                        }

                        return `
        <div class="d-flex justify-content-start align-items-center user-name">
            <div class="avatar-wrapper">
                <div class="avatar avatar-sm me-4">
                    ${output}
                </div>
            </div>
            <div class="d-flex flex-column">
                <a href="${userView}"
                   class="text-heading text-truncate">
                    <span class="fw-medium">${name}</span>
                </a>
                <small>${email}</small>
            </div>
        </div>`;
                    }
                },
                {
                    targets: 3,
                    responsivePriority: 4,
                    render: function (data, type, full) {

                        const roleBadgeObj = {
                            System: '<i class="ti ti-shield ti-md text-danger me-2"></i>',
                            Admin: '<i class="ti ti-shield ti-md text-danger me-2"></i>',
                            Manager: '<i class="ti ti-briefcase ti-md text-primary me-2"></i>',
                            Maintainer: '<i class="ti ti-user ti-md text-success me-2"></i>',
                            Editor: '<i class="ti ti-edit ti-md text-warning me-2"></i>',
                            Subscriber: '<i class="ti ti-crown ti-md text-info me-2"></i>'
                        };

                        if (!data || data.length === 0) {
                            return '<span class="text-muted">No role</span>';
                        }

                        return data.map(function (role) {
                            return `
                                <div class="d-flex align-items-center">
                                  ${roleBadgeObj[role] ?? '<i class="ti ti-user ti-md text-secondary me-2"></i>'}
                                  <span class="text-heading">${role}</span>
                                </div>
                              `;
                        }).join('');
                    }
                },
                {
                    // User Status
                    targets: 4,
                    responsivePriority: 2,
                    render: function (data, type, full, meta) {
                        return renderUserStatus(full);
                    }
                },

                {
                    targets: -1,
                    responsivePriority: 1,
                    title: 'Thao tác',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full) {
                        const id = encodeURIComponent(full.id);
                        const detailsUrl = '/admin/user/user-details/' + id;
                        const editUrl = '/admin/user/user-details/' + id + '?edit=true';

                        const statusAction = full.status === 2
                            ? `<button type="button"
                                       class="dropdown-item suspend-user user-details-link"
                                       data-id="${id}">
                                   <i class="ti ti-user-off me-2"></i>Đình chỉ tài khoản
                               </button>`
                                                    : full.status === 3
                                                        ? `<button type="button"
                                           class="dropdown-item activate-user user-details-link"
                                           data-id="${id}">
                                       <i class="ti ti-user-check me-2"></i>Gỡ đình chỉ
                                   </button>`
                                : '';

                        const unlockAction = full.isIdentityLocked
                            ? `<button type="button"
                                       class="dropdown-item unlock-identity-user user-details-link"
                                       data-id="${id}">
                                   <i class="ti ti-lock-open me-2"></i>Mở khóa đăng nhập
                               </button>`
                            : '';
                        const isActive = full.isActive
                        ? `<button class="dropdown-item toggle-user-active"
                                   data-id="${id}"
                                   data-active="true">
                               <i class="ti ti-user-off me-2"></i>Disable Account
                           </button>`
                        : `<button class="dropdown-item toggle-user-active"
                                   data-id="${id}"
                                   data-active="false">
                               <i class="ti ti-user-check me-2"></i>Enable Account
                           </button>`
                        ;

                        return `
            <div class="d-flex align-items-center">
                <a href="${detailsUrl}" data-id="${id}"
                   class="user-details-link btn btn-icon btn-text-secondary rounded-pill"
                   title="Xem chi tiết">
                    <i class="ti ti-eye ti-md"></i>
                </a>

                <a href="${editUrl}" data-id="${id}"
                   class="user-details-link btn btn-icon btn-text-secondary rounded-pill"
                   title="Chỉnh sửa">
                    <i class="ti ti-edit ti-md"></i>
                </a>

                <div class="dropdown">
                    <button type="button"
                            class="btn btn-icon btn-text-secondary rounded-pill dropdown-toggle hide-arrow"
                            data-bs-toggle="dropdown"
                            aria-expanded="false"
                            title="Thao tác khác">
                        <i class="ti ti-dots-vertical ti-md"></i>
                    </button>

                    <div class="dropdown-menu dropdown-menu-end m-0">
                        ${statusAction}
                        ${unlockAction}
                        ${isActive}
                        <button type="button"
                                class="dropdown-item reset-password-user user-details-link"
                                data-id="${id}"
                                data-url="${detailsUrl}?tab=security">
                            <i class="ti ti-key me-2"></i>Đặt lại mật khẩu
                        </button>
                    </div>
                </div>
            </div>`;
                    }
                }

            ],
            order: [[2, 'desc']],
            dom:
                '<"card-header d-flex flex-wrap py-0 flex-column flex-sm-row pt-2"' +
                '<f>' +
                '<"d-flex justify-content-center justify-content-md-end align-items-baseline ms-md-auto mx-2"' +
                '<"dt-action-buttons d-flex justify-content-center flex-md-row align-items-baseline"lB>' +
                '>' +
                '>' +
                't' +

                '<"row m-0"' +
                '<"col-sm-12 col-md-6 mt-2 d-flex justify-content-center justify-content-md-start"i>' +
                '<"col-sm-12 col-md-6 mt-2 d-flex justify-content-center"p>' +
                '>',
            lengthMenu: [5, 10, 20, 50, 70, 100],

            language: {
                info: 'Hiển thị _START_ đến _END_ trong _TOTAL_ mục',
                infoFiltered: '(đã lọc từ _MAX_ mục)',
                infoEmpty: 'Không có dữ liệu',
                zeroRecords: 'Không tìm thấy dữ liệu',
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'Tìm kiếm...',

                paginate: {
                    next: '<i class="ti ti-chevron-right ti-sm"></i>',
                    previous: '<i class="ti ti-chevron-left ti-sm"></i>'
                }
            },
            buttons: [
                {
                    text:
                        '<i class="ti ti-plus ti-xs me-0 me-sm-2"></i>' +
                        '<span class="d-none d-sm-inline-block">Thêm tài khoản</span>',

                    className: 'add-new btn btn-primary ms-2 waves-effect waves-light',
                },
                {
                    extend: 'print',
                    text: '<i class="ti ti-printer"></i> Print',
                    className: 'btn btn-primary ms-2 waves-effect waves-light',
                    exportOptions: {
                        rows: function (idx, data, node) {

                            const checkbox = $(node)
                                .find('.dt-checkboxes');

                            // Row được tick
                            if (checkbox.prop('checked')) {
                                return true;
                            }

                            return false;
                        }
                    }
                }
            ],

            responsive: {
                details: {
                    display:
                        $.fn.dataTable.Responsive.display.modal({
                            header: function (row) {
                                var data = row.data();
                                return 'Chi tiết  ' + data.name;
                            }
                        }),

                    type: 'column',

                    renderer: function (api, rowIdx, columns) {

                        var data = $.map(columns, function (col, index) {
                            if (index < 2) {
                                return '';
                            }

                            return col.title !== ''
                                ? '<tr>' +
                                '<td>' +
                                col.title +
                                ':' +
                                '</td>' +
                                '<td>' +
                                col.data +
                                '</td>' +
                                '</tr>'
                                : '';
                        }
                        ).join('');

                        return data
                            ? $('<table class="table"><tbody />')
                                .append(data)
                            : false;
                    }
                }
            },
            headerCallback: function (thead) {
                const $header =
                    $(thead).find('th').eq(1);

                if (!$header.find('#select-all-users').length) {
                    $header.html(
                        '<input type="checkbox" ' +
                        'id="select-all-users" ' +
                        'class="form-check-input">'
                    );
                }
            },
            drawCallback: function () {
                //$('#select-all-products').prop('checked', false);
                updateSelectAllState();
            },
            initComplete: function () {
                // Adding status filter once table initialized

                $.get('/admin/user/roles', function (roles) {

                    var select = $(
                        '<select id="FilterUsersRole" class="form-select">' +
                        '<option value="">Tất cả</option>' +
                        '</select>'
                    );

                    select.appendTo('.user_role');

                    roles.forEach(function (roles) {

                        select.append(
                            `<option value="${roles.value}">${roles.name}</option>`
                        );


                    });

                    select.on('change', function () {
                        dt_user.ajax.reload();
                    });

                });

                $.get('/admin/user/statuses', function (statuses) {

                    var select = $(
                        '<select id="FilterUsersStatus" class="form-select">' +
                        '<option value="">Tất cả</option>' +
                        '</select>'
                    );

                    select.appendTo('.user_status');

                    statuses.forEach(function (status) {

                        select.append(
                            `<option value="${status.value}">${status.name}</option>`
                        );


                    });

                    select.on('change', function () {
                        dt_user.ajax.reload();
                    });

                });
            }

        });

    }
    function renderUserStatus(user) {
        if (user.isDeleted) {
            return '<span class="badge bg-label-danger">Đã xóa</span>';
        }

        // Pending
        if (user.status === 0) {
            return '<span class="badge bg-label-info">Chờ kích hoạt</span>';
        }

        // Suspended
        if (user.status === 2) {
            return '<span class="badge bg-label-warning">Đình chỉ</span>';
        }

        // Deactivated
        if (user.status === 3) {
            return '<span class="badge bg-label-danger">Đã vô hiệu hóa</span>';
        }

        // Active nhưng bị Disable
        if (!user.isActive) {
            return '<span class="badge bg-label-secondary">Disabled</span>';
        }

        // Active nhưng bị Identity Lockout
        if (user.isIdentityLocked) {
            return `
            <span class="badge bg-label-success">Hoạt động</span>
            <span class="badge bg-label-danger ms-1">Locked out</span>
        `;
        }

        return '<span class="badge bg-label-success">Hoạt động</span>';
    }
    function updateSelectAllState() {
        const $checkboxes =
            $('.datatables-users tbody .dt-checkboxes');

        const total = $checkboxes.length;
        const checked =
            $checkboxes.filter(':checked').length;

        const selectAll =  document.getElementById('select-all-users');

        if (!selectAll) {
            return;
        }

        if (total === 0) {
            selectAll.checked = false;
            selectAll.indeterminate = false;
        }
        else if (checked === total) {
            selectAll.checked = true;
            selectAll.indeterminate = false;
        }
        else if (checked > 0) {
            selectAll.checked = false;
            selectAll.indeterminate = true;
        }
        else {
            selectAll.checked = false;
            selectAll.indeterminate = false;
        }
    }


    // Checkbox All
    $(document).on(
        'change',
        '#select-all-users',
        function () {

            const checked = this.checked;

            $('.datatables-users tbody .dt-checkboxes')
                .prop('checked', checked);

            this.indeterminate = false;
        }
    );


    // Checkbox từng row
    $('.datatables-users tbody').on(
        'change',
        '.dt-checkboxes',
        function () {

            updateSelectAllState();
        }
    );
    // Delete Record
    $('.datatables-users tbody').on('click', '.delete-record', function () {
        dt_user.row($(this).parents('tr')).remove().draw();
    });

    setTimeout(() => {
        $('.dt-search .form-control').removeClass('form-control-sm');

        $('.dt-length .form-select').removeClass('form-select-sm');
    }, 300);


    //checkbox all
    $('#select-all-users').on('change', function () {
        const checked = this.checked;

        $('.datatables-users tbody .dt-checkboxes').prop('checked', checked);
    });

    $('.datatables-users tbody').on('change', '.dt-checkboxes', function () {
        const total = $('.datatables-users tbody .dt-checkboxes').length;
        const checked = $('.datatables-users tbody .dt-checkboxes:checked').length;

        $('#select-all-users').prop(
            'checked',
            total > 0 && total === checked
        );
    });

    $(document).on('click', '.toggle-password', function () {
        const targetId = $(this).data('target');
        const input = document.getElementById(targetId);

        if (!input) return;

        const isPassword = input.type === 'password';

        input.type = isPassword ? 'text' : 'password';

        $(this).find('i')
            .toggleClass('ti-eye', !isPassword)
            .toggleClass('ti-eye-off', isPassword);
    });
    $(document).on('click', '.add-new', function (e) {
        e.preventDefault();

        const element = document.getElementById('offcanvasAddUser');

        if (!element) return;

        bootstrap.Offcanvas.getOrCreateInstance(element).show();
    });
    function initCreateUser() {
        const offcanvasElement = document.getElementById('offcanvasAddUser');

        if (!offcanvasElement) return;

        const roleSelect = $('#createUserRoleIds');

        // Initialize Select2
        if (!roleSelect.hasClass('select2-hidden-accessible')) {
            roleSelect.select2({
                placeholder: 'Chọn vai trò',
                allowClear: true,
                width: '100%',
                dropdownParent: $(offcanvasElement)
            });
        }

        // Load roles
        $.get('/admin/user/roles')
            .done(function (roles) {
                roleSelect.empty();

                roles.forEach(function (role) {
                    roleSelect.append(
                        new Option(role.name, role.value, false, false)
                    );
                });

                roleSelect.trigger('change');
            })
            .fail(function () {
                console.error('Không thể tải danh sách vai trò.');
            });
    }

    initCreateUser();
    // Lấy antiforgery token từ Razor form hoặc token được render trong trang.
    function getAntiforgeryToken() {
        return $('input[name="__RequestVerificationToken"]')
            .first()
            .val();
    }

    function postUserAction(id, action, confirmText) {
        const token = getAntiforgeryToken();

        if (!token) {
            Swal.fire({
                icon: 'error',
                title: 'Thiếu bảo vệ yêu cầu',
                text: 'Không tìm thấy antiforgery token. Vui lòng tải lại trang.'
            });
            return;
        }

        Swal.fire({
            title: 'Bạn có chắc chắn?',
            text: confirmText,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Xác nhận',
            cancelButtonText: 'Hủy',
            reverseButtons: true
        }).then(function (result) {
            if (!result.isConfirmed) return;

            $.ajax({
                url: `/admin/user/user-${action}/${id}`,
                type: 'POST',
                data: {
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Thành công',
                        text: response.message,
                        timer: 1500,
                        showConfirmButton: false
                    });

                    // Giữ nguyên trang và bộ lọc hiện tại.
                    dt_user.ajax.reload(null, false);
                },
                error: function (xhr) {
                    const message =
                        xhr.responseJSON?.message ||
                        'Không thể thực hiện thao tác. Vui lòng thử lại.';

                    Swal.fire({
                        icon: 'error',
                        title: 'Thao tác thất bại',
                        text: message
                    });
                }
            });
        });
    }
    // Mở User Details / Edit nhưng vẫn giữ trang hiện tại nếu bị chặn Level.
    $(document).on('click', 'a.user-details-link', function (e) {
        // Giữ hành vi mặc định khi mở tab mới hoặc cửa sổ mới.
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.which !== 1) {
            return;
        }

        e.preventDefault();

        const url = this.href;

        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'html',

            success: function (html) {
                // Request hợp lệ: hiển thị trang Details nhận được.
                //document.open();
                //document.write(html);
                //document.close();
                window.location.href = url;
            },

            error: function (xhr) {
                const message =
                    xhr.responseJSON?.message ||
                    'Không thể truy cập thông tin tài khoản.';

                if (xhr.status === 403) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Không đủ quyền',
                        text: message,
                        confirmButtonText: 'Đóng'
                    });

                    return;
                }

                Swal.fire({
                    icon: 'error',
                    title: 'Có lỗi xảy ra',
                    text: message,
                    confirmButtonText: 'Đóng'
                });
            }
        });
    });
    $(document).on('submit', '#createUserForm', function (e) {
        e.preventDefault();

        const form = this;
        const $form = $(form);
        const submitButton = $('#btnCreateUser');

        if (form.dataset.submitting === 'true') return;

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        $.ajax({
            url: '/admin/user/user-create',
            type: 'POST',
            data: $form.serialize(),

            beforeSend: function () {
                form.dataset.submitting = 'true';
                submitButton.prop('disabled', true);
            },

            success: function (response) {
                if (!response.success) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Có lỗi xảy ra',
                        text: response.message
                    });
                    return;
                }

                const offcanvasElement =
                    document.getElementById('offcanvasAddUser');

                bootstrap.Offcanvas
                    .getOrCreateInstance(offcanvasElement)
                    .hide();

                Swal.fire({
                    icon: 'success',
                    title: 'Thành công',
                    text: response.message,
                    timer: 1500,
                    showConfirmButton: false
                });

                form.reset();
                $('#createUserRoleIds').val(null).trigger('change');

                if (dt_user) {
                    dt_user.ajax.reload(null, false);
                }
            },

            error: function (xhr) {
                const response = xhr.responseJSON;

                Swal.fire({
                    icon: 'error',
                    title: 'Có lỗi xảy ra',
                    text: response?.message || 'Không thể tạo tài khoản.'
                });
            },

            complete: function () {
                form.dataset.submitting = 'false';
                submitButton.prop('disabled', false);
            }
        });
    });
    // Đình chỉ tài khoản
    $(document).on('click', '.suspend-user', function (e) {
        e.preventDefault();

        postUserAction(
            $(this).data('id'),
            'suspend',
            'Tài khoản sẽ bị đình chỉ và không thể đăng nhập vào hệ thống.'
        );
    });

    // Gỡ đình chỉ
    $(document).on('click', '.activate-user', function (e) {
        e.preventDefault();

        postUserAction(
            $(this).data('id'),
            'activate',
            'Bạn muốn gỡ đình chỉ tài khoản này?'
        );
    });
    function checkUserLevel(id) {
        return $.ajax({
            url: `/admin/user/check-user-level/${encodeURIComponent(id)}`,
            type: 'GET',
            dataType: 'json'
        });
    }
    // Mở khóa đăng nhập Identity.
    $(document).on('click', '.unlock-identity-user', function (e) {
        e.preventDefault();

        const id = $(this).data('id');
        if (!id) return;

        checkUserLevel(id)
            .done(function () {
                // Gọi lại hàm AJAX unlock hiện tại của bạn tại đây.
                unlockIdentityUser(id);
            })
            .fail(function (xhr) {
                Swal.fire({
                    icon: 'error',
                    title: xhr.status === 403 ? 'Không đủ quyền' : 'Có lỗi xảy ra',
                    text: xhr.responseJSON?.message || 'Bạn không đủ cấp bậc để thực hiện thao tác này.',
                    confirmButtonText: 'Đóng'
                });
            });
    });
    $(document).on('click', '.toggle-user-active', function (e) {
        e.preventDefault();

        const id = $(this).data('id');
        const isActive = $(this).data('active') === true ||
            $(this).data('active') === 'true';

        if (!id) return;

        checkUserLevel(id)
            .done(function () {
                Swal.fire({
                    icon: 'warning',
                    title: isActive ? 'Disable tài khoản?' : 'Enable tài khoản?',
                    text: isActive
                        ? 'Tài khoản sẽ không thể đăng nhập.'
                        : 'Tài khoản sẽ được kích hoạt trở lại.',
                    showCancelButton: true,
                    confirmButtonText: 'Xác nhận',
                    cancelButtonText: 'Hủy'
                }).then(function (result) {
                    if (!result.isConfirmed) return;

                    $.ajax({
                        url: `/admin/user/${isActive ? 'user-disable' : 'user-enable'}/${encodeURIComponent(id)}`,
                        type: 'POST',
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
                        success: function (response) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Thành công',
                                text: response.message,
                                timer: 1500,
                                showConfirmButton: false
                            }).then(function () {
                                dt_user_table.DataTable().ajax.reload(null, false);
                            });
                        },
                        error: function (xhr) {
                            Swal.fire({
                                icon: 'error',
                                title: xhr.status === 403 ? 'Không đủ quyền' : 'Có lỗi xảy ra',
                                text: xhr.responseJSON?.message || 'Không thể cập nhật trạng thái tài khoản.'
                            });
                        }
                    });
                });
            })
            .fail(function (xhr) {
                Swal.fire({
                    icon: 'error',
                    title: xhr.status === 403 ? 'Không đủ quyền' : 'Có lỗi xảy ra',
                    text: xhr.responseJSON?.message || 'Bạn không đủ cấp bậc để thực hiện thao tác này.'
                });
            });
    });

    // Chuyển đến tab Security để đặt lại mật khẩu.
    $(document).on('click', '.reset-password-user', function (e) {
        e.preventDefault();

        const url = $(this).data('url');

        if (!url) return;

        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'html',
            success: function (html) {
                document.open();
                document.write(html);
                document.close();
            },
            error: function (xhr) {
                Swal.fire({
                    icon: 'error',
                    title: xhr.status === 403 ? 'Không đủ quyền' : 'Có lỗi xảy ra',
                    text: xhr.responseJSON?.message ||
                        'Không thể truy cập thông tin tài khoản.'
                });
            }
        });
    });

    function updateStatisticChange(selector, value) {
        const element = $(selector);
        const percent = Number(value) || 0;

        element.text(`${percent > 0 ? '+' : ''}${percent.toFixed(1)}%`);

        element.removeClass('text-success text-danger text-muted');

        if (percent > 0) {
            element.addClass('text-success');
        } else if (percent < 0) {
            element.addClass('text-danger');
        } else {
            element.addClass('text-muted');
        }
    }

    function loadUserStatistics() {
        $.ajax({
            url: '/admin/user/statistics',
            type: 'GET',
            success: function (response) {
                $('#totalUsers').text(response.totalUsers);
                $('#activeUsers').text(response.activeUsers);
                $('#pendingUsers').text(response.pendingUsers);
                $('#sessionUsers').text(response.sessionUsers);

                updateStatisticChange('#totalUsersChange', response.totalUsersChangePercent);

                updateStatisticChange('#activeUsersChange', response.activeUsersChangePercent);

                updateStatisticChange('#pendingUsersChange', response.pendingUsersChangePercent);

                updateStatisticChange('#sessionUsersChange', response.sessionUsersChangePercent);
            },
            error: function (xhr) {
                console.error('Không thể tải thống kê User:', xhr);
            }
        });
    }

    loadUserStatistics();
});
