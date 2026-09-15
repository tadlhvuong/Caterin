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
    var statusObj = {
        0: {
            title: 'Ẩn',
            class: 'bg-label-secondary'
        },
        1: {
            title: 'Đang bán',
            class: 'bg-label-success'
        },
        2: {
            title: 'Ngừng bán',
            class: 'bg-label-warning'
        },
        3: {
            title: 'Lưu trữ',
            class: 'bg-label-secondary'
        }
    },
        paymentStatusObj = {
            0: {
                title: 'Chờ thanh toán',
                class: 'bg-label-warning'
            },
            1: {
                title: 'Đang xử lý',
                class: 'bg-label-info'
            },
            2: {
                title: 'Đã thanh toán',
                class: 'bg-label-success'
            },
            3: {
                title: 'Thanh toán thất bại',
                class: 'bg-label-danger'
            },
            4: {
                title: 'Đã hủy',
                class: 'bg-label-secondary'
            },
            5: {
                title: 'Đã hoàn tiền',
                class: 'bg-label-primary'
            },
            6: {
                title: 'Hoàn tiền một phần',
                class: 'bg-label-primary'
            }
        };

    // Variable declaration for table
    var dt_order_table = $('.datatables-orders');

    $.fn.dataTable.ext.pager.numbers_length = 7;
    if (dt_order_table.length) {
        var dt_orders = dt_order_table.DataTable({
            processing: true,
            serverSide: true,
            paging: true,
            pagingType: "simple_numbers",
            ajax: {
                url: '/admin/order/get-orders',
                type: 'POST',
                contentType: 'application/json',
                data: function (d) {
                    var order = d.order && d.order.length ? d.order[0] : null;

                    return JSON.stringify({
                        draw: d.draw,
                        start: d.start,
                        length: d.length,

                        search: d.search ? d.search.value : null,

                        sortColumn: order ? d.columns[order.column].data : null,

                        sortDirection: order ? order.dir : null,

                        orderStatus: $('#FilterOrderStatus').val() ? parseInt($('#FilterOrderStatus').val(), 10) : null,

                        paymentStatus: $('#FilterPaymentStatus').val() ? parseInt($('#FilterPaymentStatus').val(), 10) : null
                    });
                },

                error: function (xhr, error, thrown) {
                    console.error('DataTable error:', xhr);

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
                { data: 'order' },
                { data: 'date' },
                { data: 'customer' }, //email //avatar
                { data: 'payment' },
                { data: 'status' },
                { data: 'method' }, //method_number
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
                    // Order ID
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $order_id = full['id'];
                        var $order_code = full['order'];
                        // Creates full output for row
                        var urldtail = "/admin/order/order-details/" + $order_id;
                        var $row_output = '<a href="' + urldtail + '"><span>#' + $order_code + '</span></a>';
                        return $row_output;
                    }
                },
                {
                    // Date and Time
                    targets: 3,
                    render: function (data, type, full, meta) {

                        var date = new Date(data).toLocaleDateString('vi-VN', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                            second: '2-digit',
                            hour12: false
                        });
                        return '<span class="text-nowrap">' + date + '</span>';
                    }
                },
                {
                    // Customers
                    targets: 4,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        var $name = full['customer'],
                            $email = full['email'],
                            $avatar = full['avatar'];
                        if ($avatar) {
                            // For Avatar image
                            var $output =
                                '<img src="' + assetsPath + 'img/avatars/' + $avatar + '" alt="Avatar" class="rounded-circle">';
                        } else {
                            // For Avatar badge
                            var stateNum = Math.floor(Math.random() * 6);
                            var states = ['success', 'danger', 'warning', 'info', 'dark', 'primary', 'secondary'];
                            var $state = states[stateNum],
                                $name = full['customer'],
                                $initials = $name.match(/\b\w/g) || [];
                            $initials = (($initials.shift() || '') + ($initials.pop() || '')).toUpperCase();
                            $output = '<span class="avatar-initial rounded-circle bg-label-' + $state + '">' + $initials + '</span>';
                        }
                        // Creates full output for row
                        var $row_output =
                            '<div class="d-flex justify-content-start align-items-center order-name text-nowrap">' +
                            '<div class="avatar-wrapper">' +
                            '<div class="avatar avatar-sm me-3">' +
                            $output +
                            '</div>' +
                            '</div>' +
                            '<div class="d-flex flex-column">' +
                            '<h6 class="m-0"><a href="pages-profile-user.html" class="text-heading">' +
                            $name +
                            '</a></h6>' +
                            '<small>' +
                            $email +
                            '</small>' +
                            '</div>' +
                            '</div>';
                        return $row_output;
                    }
                },
                {
                    targets: 5,
                    render: function (data, type, full, meta) {
                        const status = Number(full.status);
                        const paymentInfo = paymentStatusObj[status];

                        if (!paymentInfo) {
                            return '<span class="badge bg-label-secondary">Không xác định</span>';
                        }

                        return `
            <span class="badge px-2 ${paymentInfo.class}" text-capitalized>
                ${paymentInfo.title}
            </span>
        `;
                    }
                },
                {
                    targets: -3,
                    render: function (data, type, full, meta) {
                        const status = Number(full.status);
                        const statusInfo = statusObj[status];

                        if (!statusInfo) {
                            return '<span class="badge bg-label-secondary">Không xác định</span>';
                        }

                        return `
            <span class="badge px-2 ${statusInfo.class}" text-capitalized>
                ${statusInfo.title}
            </span>
        `;
                    }
                },
                {
                    // Payment Method
                    targets: -2,
                    render: function (data, type, full, meta) {
                        var $method = full['method'];
                        var $method_number = full['method_number'];

                        if ($method == 'paypal') {
                            $method_number = '@gmail.com';
                        }
                        return (
                            '<div class="d-flex align-items-center text-nowrap">' +
                            '<img src="' +
                            assetsPath +
                            'img/icons/payments/' +
                            $method +
                            '.png" alt="' +
                            $method +
                            '" width="29">' +
                            '<span><i class="ti ti-dots me-1 mt-1"></i>' +
                            $method_number +
                            '</span>' +
                            '</div>'
                        );
                    }
                },
                {
                    // Actions
                    targets: -1,
                    title: 'Actions',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        return (
                            '<div class="d-flex justify-content-sm-start align-items-sm-center">' +
                            '<button class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical"></i></button>' +
                            '<div class="dropdown-menu dropdown-menu-end m-0">' +
                            '<a href="app-ecommerce-order-details.html" class="dropdown-item">View</a>' +
                            '<a href="javascript:0;" class="dropdown-item delete-record">' +
                            'Delete' +
                            '</a>' +
                            '</div>' +
                            '</div>'
                        );
                    }
                }
            ],

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
                        '<span class="d-none d-sm-inline-block">Thêm đơn hàng</span>',

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

                if (!$header.find('#select-all-orders').length) {
                    $header.html(
                        '<input type="checkbox" ' +
                        'id="select-all-orders" ' +
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
                $.get('/admin/order/order-statuses', function (statuses) {

                    var select = $(
                        '<select id="FilterOrderStatus" class="form-select">' +
                        '<option value="">Tất cả</option>' +
                        '</select>'
                    );

                    select.appendTo('.order_status');

                    statuses.forEach(function (status) {

                        select.append(
                            `<option value="${status.value}">${status.name}</option>`
                        );


                    });

                    select.on('change', function () {
                        dt_orders.ajax.reload();
                    });

                });
                $.get('/admin/order/payment-statuses', function (statuses) {

                    var select = $(
                        '<select id="FilterPaymentStatus" class="form-select">' +
                        '<option value="">Tất cả</option>' +
                        '</select>'
                    );

                    select.appendTo('.payment_status');

                    statuses.forEach(function (status) {

                        select.append(
                            `<option value="${status.value}">${status.name}</option>`
                        );


                    });

                    select.on('change', function () {
                        dt_orders.ajax.reload();
                    });

                });
            }

        });

    }
    function formatProductPrice(row) {
        const minPrice = Number(row.minPrice || 0);
        const maxPrice = Number(row.maxPrice || 0);

        if (minPrice === maxPrice) {
            return minPrice.toLocaleString("vi-VN");
        }

        return `${minPrice.toLocaleString("vi-VN")} - ${maxPrice.toLocaleString("vi-VN")}`;
    }
    function updateSelectAllState() {
        const $checkboxes =
            $('.datatables-orders tbody .dt-checkboxes');

        const total = $checkboxes.length;
        const checked =
            $checkboxes.filter(':checked').length;

        const selectAll =
            document.getElementById('select-all-orders');

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
        '#select-all-orders',
        function () {

            const checked = this.checked;

            $('.datatables-orders tbody .dt-checkboxes')
                .prop('checked', checked);

            this.indeterminate = false;
        }
    );


    // Checkbox từng row
    $('.datatables-orders tbody').on(
        'change',
        '.dt-checkboxes',
        function () {

            updateSelectAllState();
        }
    );
    // Delete Record
    $('.datatables-orders tbody').on('click', '.delete-record', function () {
        dt_orders.row($(this).parents('tr')).remove().draw();
    });

    setTimeout(() => {
        $('.dt-search .form-control')
            .removeClass('form-control-sm');

        $('.dt-length .form-select')
            .removeClass('form-select-sm');
    }, 300);


    //checkbox all
    $('#select-all-orders').on('change', function () {
        console.log('select all');
        const checked = this.checked;

        $('.datatables-orders tbody .dt-checkboxes')
            .prop('checked', checked);
    });
    $('.datatables-orders tbody').on('change', '.dt-checkboxes', function () {
        const total = $('.datatables-orders tbody .dt-checkboxes').length;
        const checked = $('.datatables-orders tbody .dt-checkboxes:checked').length;

        $('#select-all-orders').prop(
            'checked',
            total > 0 && total === checked
        );
    });


    $(document).on('click', '.add-new', function () {
        window.location.href = '/admin/order/create'
    });
});
