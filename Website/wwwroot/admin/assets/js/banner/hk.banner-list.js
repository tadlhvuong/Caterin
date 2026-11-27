
'use strict';

$(function () {
    const tableSelector = '.datatables-banners';
    const token = $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();

    const urls = {
        getBanners: '/admin/banner/get-banners',
        statuses: '/admin/banner/statuses',
        positions: '/admin/banner/positions',
        linkTypes: '/admin/banner/link-types',
        togglePublish: '/admin/banner/toggle-publish',
        delete: '/admin/banner/delete',
        create: '/admin/banner/create',
        edit: id => `/admin/banner/edit/${id}`
    };

    const $table = $(tableSelector);

    if (!$table.length) return;

    let bannerTable;
    let isProcessing = false;

    // =========================================================
    // HELPERS
    // =========================================================

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function showSuccess(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: message,
                showConfirmButton: false,
                timer: 2500
            });
        }
    }

    function showError(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: 'Có lỗi xảy ra',
                text: message || 'Vui lòng thử lại.'
            });
        }
    }

    function formatDate(value) {
        if (!value) return 'Không giới hạn';

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) return '-';

        return date.toLocaleString('vi-VN', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function getDateRange(startAt, endAt) {
        const start = startAt
            ? formatDate(startAt)
            : 'Không giới hạn';

        const end = endAt
            ? formatDate(endAt)
            : 'Không giới hạn';

        return `
            <div class="d-flex flex-column gap-1">
                <small><span class="text-muted">Từ:</span> ${escapeHtml(start)}</small>
                <small><span class="text-muted">Đến:</span> ${escapeHtml(end)}</small>
            </div>
        `;
    }

    function getStatusBadge(status) {
        const statuses = {
            0: {
                text: 'Bản nháp',
                className: 'bg-label-secondary'
            },
            1: {
                text: 'Đã lên lịch',
                className: 'bg-label-info'
            },
            2: {
                text: 'Đang hiển thị',
                className: 'bg-label-success'
            },
            3: {
                text: 'Đã hết hạn',
                className: 'bg-label-danger'
            }
        };

        const item = statuses[status] || {
            text: 'Không xác định',
            className: 'bg-label-secondary'
        };

        return `
            <span class="badge ${item.className}">
                ${item.text}
            </span>
        `;
    }

    function getDeviceBadges(row) {
        const devices = [];

        if (row.hasDesktop) {
            devices.push(`
                <span class="badge bg-label-primary">
                    <i class="ti ti-device-desktop me-1"></i> PC
                </span>
            `);
        }

        if (row.hasMobile) {
            devices.push(`
                <span class="badge bg-label-info">
                    <i class="ti ti-device-mobile me-1"></i> Mobile
                </span>
            `);
        }

        if (!devices.length) {
            return '<span class="text-muted">Chưa có ảnh</span>';
        }

        return `<div class="d-flex flex-wrap gap-1">${devices.join('')}</div>`;
    }

    function getBannerImage(row) {
        const imageUrl = row.desktopImageUrl || row.mobileImageUrl;

        if (!imageUrl) {
            return `
            <div class="avatar avatar-lg flex-shrink-0">
                <span class="avatar-initial rounded bg-label-secondary">
                    <i class="ti ti-photo"></i>
                </span>
            </div>
        `;
        }

        return `
        <div class="avatar avatar-lg flex-shrink-0">
            <img
                src="${escapeHtml(imageUrl)}"
                alt="${escapeHtml(row.title)}"
                class="rounded"
                style="width: 80px; height: 56px; object-fit: cover;"
                loading="lazy"
                onerror="this.style.display='none';"
            />
        </div>
    `;
    }

    function getPublishSwitch(row) {
        return `
            <div class="form-check form-switch d-flex justify-content-center">
                <input
                    class="form-check-input banner-toggle-publish"
                    type="checkbox"
                    role="switch"
                    data-id="${row.id}"
                    ${row.isPublished ? 'checked' : ''}
                    aria-label="Thay đổi trạng thái xuất bản"
                />
            </div>
        `;
    }

    function getRowActions(row) {
        return `
            <div class="d-flex align-items-center gap-2">

                <a
                    href="${urls.edit(row.id)}"
                    class="btn btn-sm btn-icon btn-text-secondary rounded-pill"
                    title="Chỉnh sửa"
                >
                    <i class="ti ti-edit"></i>
                </a>

                <div class="dropdown">
                    <button
                        type="button"
                        class="btn btn-sm btn-icon btn-text-secondary rounded-pill dropdown-toggle hide-arrow"
                        data-bs-toggle="dropdown"
                        aria-expanded="false"
                    >
                        <i class="ti ti-dots-vertical"></i>
                    </button>

                    <div class="dropdown-menu dropdown-menu-end">

                        <button
                            type="button"
                            class="dropdown-item text-danger banner-delete"
                            data-id="${row.id}"
                            data-title="${escapeHtml(row.title)}"
                        >
                            <i class="ti ti-trash me-2"></i>
                            Xóa Banner
                        </button>

                    </div>
                </div>

            </div>
        `;
    }

    function getColumnSortName(columnIndex) {
        const columns = {
            2: 'title',
            3: 'position',
            5: 'startAt',
            8: 'sortOrder'
        };

        return columns[columnIndex] || 'sortOrder';
    }

    function getAntiForgeryHeader() {
        return {
            RequestVerificationToken: token
        };
    }

    function reloadTable(resetPaging = false) {
        if (bannerTable) {
            bannerTable.ajax.reload(null, resetPaging);
        }
    }

    // =========================================================
    // AJAX HELPERS
    // =========================================================

    function postJson(url, data) {
        return $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            headers: getAntiForgeryHeader(),
            data: JSON.stringify(data)
        });
    }

    function loadFilters() {
        // Filter trạng thái
        $.get(urls.statuses, function (statuses) {
            const select = $(
                '<select id="FilterStatus" class="form-select">' +
                '<option value="">Chọn trạng thái</option>' +
                '</select>'
            );

            select.appendTo('.banner_status');

            statuses.forEach(function (status) {
                select.append(
                    `<option value="${status.value}">${status.name}</option>`
                );
            });

            select.on('change', function () {
                bannerTable.ajax.reload();
            });
        });

        // Filter vị trí
        $.get(urls.positions, function (positions) {
            const select = $(
                '<select id="FilterPosition" class="form-select">' +
                '<option value="">Chọn vị trí</option>' +
                '</select>'
            );

            select.appendTo('.banner_position');

            positions.forEach(function (position) {
                select.append(
                    `<option value="${position.value}">${position.name}</option>`
                );
            });

            select.on('change', function () {
                bannerTable.ajax.reload();
            });
        });

        // Filter loại liên kết
        $.get(urls.linkTypes, function (linkTypes) {
            const select = $(
                '<select id="FilterLinkType" class="form-select">' +
                '<option value="">Loại liên kết</option>' +
                '</select>'
            );

            select.appendTo('.banner_link_type');

            linkTypes.forEach(function (linkType) {
                select.append(
                    `<option value="${linkType.value}">${linkType.name}</option>`
                );
            });

            select.on('change', function () {
                bannerTable.ajax.reload();
            });
        });
    }

    // =========================================================
    // RESPONSIVE DETAILS
    // =========================================================

    function renderResponsiveDetails(api, rowIdx, columns) {
        const data = $.map(columns, function (column) {
            if (!column.hidden) return '';

            return `
                <tr data-dt-row="${column.rowIndex}" data-dt-column="${column.columnIndex}">
                    <td class="fw-medium">${escapeHtml(column.title)}</td>
                    <td>${column.data || ''}</td>
                </tr>
            `;
        }).join('');

        return data
            ? $('<table class="table table-sm mb-0"/>').append(data)
            : false;
    }

    // =========================================================
    // DATATABLE
    // =========================================================

    bannerTable = $table.DataTable({
        processing: true,
        serverSide: true,

        paging: true,
        pagingType: 'simple_numbers',
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],

        responsive: {
            details: {
                display: $.fn.dataTable.Responsive.display.modal({
                    header: function (row) {
                        const data = row.data();
                        return 'Chi tiết Banner: ' + data.title;
                    }
                }),

                type: 'column',

                renderer: function (api, rowIdx, columns) {
                    const data = $.map(columns, function (col, index) {
                        if (index < 2) return '';

                        return col.title !== ''
                            ? '<tr>' +
                            '<td>' + escapeHtml(col.title) + ':</td>' +
                            '<td>' + (col.data || '') + '</td>' +
                            '</tr>'
                            : '';
                    }).join('');

                    return data
                        ? $('<table class="table"><tbody /></table>').append(data)
                        : false;
                }
            }
        },

        select: {
            style: 'multi',
            selector: 'td:nth-child(2)'
        },

        order: [[8, 'asc']],

        ajax: {
            url: urls.getBanners,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',

            headers: getAntiForgeryHeader(),

            data: function (d) {
                const order = d.order && d.order.length
                    ? d.order[0]
                    : null;

                return JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,

                    search: d.search ? d.search.value : null,

                    sortColumn: order
                        ? d.columns[order.column].data
                        : null,

                    sortDirection: order
                        ? order.dir
                        : null,

                    status: $('#FilterStatus').val()
                        ? parseInt($('#FilterStatus').val(), 10)
                        : null,

                    position: $('#FilterPosition').val()
                        ? parseInt($('#FilterPosition').val(), 10)
                        : null,

                    linkType: $('#FilterLinkType').val()
                        ? parseInt($('#FilterLinkType').val(), 10)
                        : null
                });
            },

            error: function (xhr) {
                console.error('Banner DataTable error:', xhr.responseText);
                showError('Không thể tải danh sách Banner.');
            }
        },

        columns: [
            {
                data: null,
                defaultContent: '',
                className: 'control',
                orderable: false,
                searchable: false,
                responsivePriority: 1
            },
            {
                data: 'id',
                orderable: false,
                searchable: false,
                className: 'dt-checkboxes-cell',
                width: '35px',
                render: function (data, type) {
                    if (type !== 'display') return data;

                    return `
                        <input
                            type="checkbox"
                            class="dt-checkboxes form-check-input banner-row-checkbox"
                            value="${data}"
                            aria-label="Chọn Banner"
                        />
                    `;
                }
            },
            {
                data: 'title',
                render: function (data, type, row) {
                    if (type !== 'display') return data;

                    return `
            <div class="banner-title-cell">
                <div class="banner-title-cell__image">
                    ${getBannerImage(row)}
                </div>

                <div class="banner-title-cell__content">
                    <span class="fw-medium text-heading">
                        ${escapeHtml(data || '')}
                    </span>
                    <small class="text-muted">#${row.id}</small>
                </div>
            </div>`;
                }
            },
            {
                data: 'positionName',
                render: function (data, type) {
                    if (type !== 'display') return data;

                    return `
                        <span class="badge bg-label-primary">
                            ${escapeHtml(data || '-')}
                        </span>
                    `;
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    if (type !== 'display') return '';

                    return getDeviceBadges(row);
                }
            },
            {
                data: null,
                orderable: true,
                searchable: false,
                render: function (data, type, row) {
                    if (type !== 'display') {
                        return row.startAt || '';
                    }

                    return getDateRange(row.startAt, row.endAt);
                }
            },
            {
                data: 'status',
                render: function (data, type) {
                    if (type !== 'display') return data;

                    return getStatusBadge(data);
                }
            },
            {
                data: 'isPublished',
                orderable: false,
                searchable: false,
                className: 'text-center',
                render: function (data, type, row) {
                    if (type !== 'display') return data;

                    return getPublishSwitch(row);
                }
            },
            {
                data: 'sortOrder',
                className: 'text-center',
                render: function (data, type) {
                    if (type !== 'display') return data;

                    return `
                        <span class="fw-medium">
                            ${data ?? 0}
                        </span>
                    `;
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                className: 'text-center',
                responsivePriority: 2,
                render: function (data, type, row) {
                    if (type !== 'display') return '';

                    return getRowActions(row);
                }
            }
        ],

        columnDefs: [
            {
                targets: [0, 1, 4, 7, 9],
                orderable: false
            }
        ],

        dom:
            '<"card-header d-flex flex-wrap py-0 flex-column flex-sm-row pt-2"' +
            '<f>' +
            '<"d-flex justify-content-center justify-content-md-end align-items-baseline ms-md-auto"' +
            '<"dt-action-buttons d-flex justify-content-center flex-md-row align-items-baseline"' + '<"mx-2"l>' +
            '<"mx-2"B>' + '>' +
            '>' +
            '>' +
            't' +

            '<"row m-0"' +
            '<"col-sm-12 col-md-6 mt-2 d-flex justify-content-center justify-content-md-start"i>' +
            '<"col-sm-12 col-md-6 mt-2 d-flex justify-content-center"p>' +
            '>',

        language: {
            sProcessing: 'Đang xử lý...',
            sLengthMenu: '_MENU_',
            sZeroRecords: 'Không tìm thấy dữ liệu phù hợp',
            sInfo: 'Hiển thị _START_ đến _END_ trong tổng _TOTAL_ Banner',
            sInfoEmpty: 'Hiển thị 0 đến 0 trong tổng 0 Banner',
            sInfoFiltered: '(lọc từ _MAX_ Banner)',
            sSearch: '',
            sSearchPlaceholder: 'Tìm kiếm Banner...',
            oPaginate: {
                sFirst: 'Đầu',
                sLast: 'Cuối',
                sNext: '<i class="ti ti-chevron-right"></i>',
                sPrevious: '<i class="ti ti-chevron-left"></i>'
            }
        },

        buttons: [
            {
                text: '<i class="ti ti-plus me-1"></i> Thêm Banner',
                className: 'btn btn-primary',
                action: function () {
                    window.location.href = urls.create;
                }
            },
            {
                extend: 'print',
                text: '<i class="ti ti-printer me-1"></i> In đã chọn',
                className: 'btn btn-label-secondary',

                exportOptions: {
                    columns: [2, 3, 4, 5, 6, 7, 8],

                    rows: function (idx, data, node) {
                        return $(node)
                            .find('.banner-row-checkbox')
                            .prop('checked');
                    },

                    format: {
                        body: function (data) {
                            return $('<div>').html(data).text().trim();
                        }
                    }
                },

                action: function (e, dt, button, config) {
                    const selectedCount = dt
                        .rows()
                        .nodes()
                        .to$()
                        .find('.banner-row-checkbox:checked')
                        .length;

                    if (!selectedCount) {
                        showError('Vui lòng chọn ít nhất một Banner để in.');
                        return;
                    }

                    $.fn.dataTable.ext.buttons.print.action.call(
                        this,
                        e,
                        dt,
                        button,
                        config
                    );
                }
            }
        ],

        initComplete: function () {
            const api = this.api();

            // Search input
            $('.dataTables_filter input')
                .addClass('form-control')
                .attr('placeholder', 'Tìm kiếm Banner...')
                .removeClass('form-control-sm');

            $('.dataTables_filter label').contents().filter(function () {
                return this.nodeType === 3;
            }).remove();

            // Length selector
            $('.dataTables_length select')
                .addClass('form-select');

            // Select all checkbox
            const $header = $(api.table().header());

            $header.find('th').eq(1).html(`
                <input
                    type="checkbox"
                    class="form-check-input banner-select-all"
                    aria-label="Chọn tất cả Banner"
                />
            `);

            loadFilters();
        },

        headerCallback: function (thead) {
            $(thead).find('.banner-select-all').prop('checked', false);
        },

        drawCallback: function () {
            const api = this.api();

            const $rows = $(api.rows({ page: 'current' }).nodes());

            const total = $rows.length;
            const checked = $rows.find('.banner-row-checkbox:checked').length;

            $(api.table().header())
                .find('.banner-select-all')
                .prop('checked', total > 0 && total === checked);
        }


    });
    setTimeout(() => {
        $('.dt-search .form-control')
            .removeClass('form-control-sm');
        $('.dt-length .form-select')
            .removeClass('form-select-sm');
    }, 300);
    // =========================================================
    // SELECT ROW
    // =========================================================

    $(document).on('change', '.banner-row-checkbox', function (e) {
        e.stopPropagation();

        const $row = $(this).closest('tr');
        const row = bannerTable.row($row);

        if (this.checked) {
            row.select();
        } else {
            row.deselect();
        }

        updateSelectAllState();
    });

    // =========================================================
    // SELECT ALL CURRENT PAGE
    // =========================================================

    $(document).on('change', '.banner-select-all', function () {
        const checked = this.checked;

        const rows = bannerTable.rows({
            page: 'current'
        });

        rows.nodes().to$()
            .find('.banner-row-checkbox')
            .prop('checked', checked);

        if (checked) {
            rows.select();
        } else {
            rows.deselect();
        }

        updateSelectAllState();
    });

    function updateSelectAllState() {
        const $headerCheckbox = $(bannerTable.table().header())
            .find('.banner-select-all');

        const $rows = $(bannerTable.rows({
            page: 'current'
        }).nodes());

        const total = $rows.length;
        const checked = $rows.find('.banner-row-checkbox:checked').length;

        $headerCheckbox.prop(
            'checked',
            total > 0 && total === checked
        );
    }

    // =========================================================
    // PUBLISH / UNPUBLISH
    // =========================================================

    $(document).on('change', '.banner-toggle-publish', function () {
        const $switch = $(this);

        if (isProcessing) {
            $switch.prop('checked', !$switch.prop('checked'));
            return;
        }

        const id = Number($switch.data('id'));
        const isPublished = $switch.prop('checked');

        const previousState = !isPublished;

        $switch.prop('disabled', true);

        postJson(urls.togglePublish, {
            id: id,
            isPublished: isPublished
        })
            .done(function (response) {
                if (response.success) {
                    showSuccess(response.message || 'Đã cập nhật trạng thái xuất bản.');
                    reloadTable(false);
                } else {
                    $switch.prop('checked', previousState);
                    showError(response.message || 'Không thể cập nhật Banner.');
                }
            })
            .fail(function (xhr) {
                $switch.prop('checked', previousState);

                const message = xhr.responseJSON?.message
                    || 'Không thể cập nhật trạng thái xuất bản.';

                showError(message);
            })
            .always(function () {
                $switch.prop('disabled', false);
            });
    });

    // =========================================================
    // DELETE BANNER - SOFT DELETE
    // =========================================================

    $(document).on('click', '.banner-delete', function () {
        const id = Number($(this).data('id'));
        const title = $(this).data('title') || 'Banner này';

        if (typeof Swal === 'undefined') {
            if (!confirm(`Bạn có chắc muốn xóa "${title}"?`)) return;

            deleteBanner(id);
            return;
        }

        Swal.fire({
            title: 'Xóa Banner?',
            html: `
                Bạn có chắc muốn xóa
                <strong>${escapeHtml(title)}</strong>?
                <br/>
                <small class="text-muted">
                    Banner sẽ được chuyển vào trạng thái đã xóa.
                </small>
            `,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Xóa Banner',
            cancelButtonText: 'Hủy',
            customClass: {
                confirmButton: 'btn btn-danger me-2',
                cancelButton: 'btn btn-label-secondary'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (result.isConfirmed) {
                deleteBanner(id);
            }
        });
    });

    function deleteBanner(id) {
        if (isProcessing) return;

        isProcessing = true;

        postJson(urls.delete, {
            id: id
        })
            .done(function (response) {
                if (response.success) {
                    showSuccess(response.message || 'Đã xóa Banner.');
                    reloadTable(false);
                } else {
                    showError(response.message || 'Không thể xóa Banner.');
                }
            })
            .fail(function (xhr) {
                const message = xhr.responseJSON?.message
                    || 'Không thể xóa Banner.';

                showError(message);
            })
            .always(function () {
                isProcessing = false;
            });
    }

});
