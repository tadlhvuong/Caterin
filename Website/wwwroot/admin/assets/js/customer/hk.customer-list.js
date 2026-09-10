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
            title: 'Chờ kích hoạt',
            class: 'bg-label-secondary'
        },
        1: {
            title: 'Hoạt động',
            class: 'bg-label-success'
        },
        2: {
            title: 'Đình chỉ',
            class: 'bg-label-warning'
        },
        3: {
            title: 'Đã xóa',
            class: 'bg-label-secondary'
        }
    };
    var stockFilterObj = {
        0: {
            title: 'Hết hàng'
        },
        1: {
            title: 'Còn hàng'
        }
    };

  // Variable declaration for table
    var dt_customer_table = $('.datatables-customers');

    $.fn.dataTable.ext.pager.numbers_length = 7;
    if (dt_customer_table.length) {

        var dt_customers = dt_customer_table.DataTable({
            processing: true,
            serverSide: true,
            paging: true,
            pagingType: "simple_numbers",
            ajax: {
                url: '/admin/customer/get-customers',
                type: 'POST',
                contentType: 'application/json',
                data: function (d) {
                    console.log(d);
                    var order = d.order && d.order.length ? d.order[0] : null;

                    return JSON.stringify({
                        draw: d.draw,
                        start: d.start,
                        length: d.length,

                        search: d.search ? d.search.value : null,

                        sortColumn: order ? d.columns[order.column].data : null,

                        sortDirection: order ? order.dir : null,

                        status: $('#FilterStatus').val() ? parseInt($('#FilterStatus').val(), 10) : null,

                        //stock: $('#ProductStock').val() ? parseInt($('#ProductStock').val(), 10) : null
                    });
                },

                error: function (xhr, error, thrown) {
                    console.error('DataTable error:', xhr);

                    Swal.fire({
                        icon: 'error',
                        title: 'Có lỗi xảy ra',
                        text: 'Không thể tải danh sách khách hàng.',
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
                { data: 'customerName' },
                { data: 'orderCount' },
                { data: 'totalSpent' },
                { data: 'lastOrderAt' },
                { data: 'status' },
                { data: 'joinDate' },
                { data: null }
            ],

            columnDefs: [
                {
                    className: 'control',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function () {
                        return '';
                    }
                },

                {
                    targets: 1,
                    orderable: false,
                    searchable: false,
                    checkboxes: true,
                    responsivePriority: 3,

                    render: function () {
                        return '<input type="checkbox" class="dt-checkboxes form-check-input">';
                    },
                    checkboxes: {
                        selectAllRender: '<input type="checkbox" id="select-all-customers" class="form-check-input">'
                    }
                },

                {
                    targets: 2,
                    responsivePriority: 1,

                    render: function (data, type, full) {
                        var name = full.name;
                        var id = full.id;
                        var phone = full.phoneNumber ?? "Chưa có SĐT";
                        var image = full.imageUrl ?? "/images/Default/default-user.png";
                        var output;

                        if (image) {
                            output = '<img src="' + image + '" alt="Product-' + id + '" class="rounded-2">';
                        } else {

                            var stateNum = Math.floor(Math.random() * 7);

                            var states = [
                                'success',
                                'danger',
                                'warning',
                                'info',
                                'dark',
                                'primary',
                                'secondary'
                            ];

                            var state = states[stateNum];

                            var initials = (name.match(/\b\w/g) || []);

                            initials =
                                (
                                    (initials.shift() || '') +
                                    (initials.pop() || '')
                                ).toUpperCase();

                            output =
                                '<span class="avatar-initial rounded-2 bg-label-' +
                                state +
                                '">' +
                                initials +
                                '</span>';
                        }

                        return (
                            '<div class="d-flex justify-content-start align-items-center product-name">' +

                            '<div class="avatar-wrapper ">' +
                            '<div class="avatar me-4 rounded-2 bg-label-secondary">' +
                            output +
                            '</div>' +
                            '</div>' +

                            '<div class="d-flex flex-column">' +

                            '<h6 class="text-nowrap mb-0">' +
                            name +
                            '</h6>' +

                            '<small class="text-truncate d-none d-sm-block">' +
                            (phone ?? '') +
                            '</small>' +

                            '</div>' +

                            '</div>'
                        );
                    }
                },
                {
                    targets: 3,

                    render: function (data, type, full) {

                        var orderCount = full.orderCount;

                        return (
                            '<span class="text-truncate d-flex justify-content-end align-items-center text-heading">' +
                            orderCount +
                            '</span>'
                        );
                    }
                },
                {
                    targets: 4,

                    render: function (data, type, full) {

                        var totalSpent = full.totalSpent;
                        return (
                            '<span class="text-truncate d-flex justify-content-end align-items-center text-heading">' +
                            Number(totalSpent).toLocaleString('vi-VN') + ' đ' +
                            '</span>'
                        );
                    }
                },
                {
                    targets: 5,

                    render: function (data, type, full) {

                        var lastOrderAt = new Date(full.lastOrderAt).toLocaleDateString('vi-VN');

                        return (
                            '<span class="text-truncate d-flex justify-content-center align-items-center text-heading">' +
                            lastOrderAt +
                            '</span>'
                        );
                    }
                },
                {
                    targets: 6,
                    responsivePriority: 5,

                    render: function (data, type, full) {

                        var status = full.status;
                        var item = statusObj[status];

                        if (!item)
                            return '';

                        return ('<span class="badge ' + item.class + '">' + item.title + '</span>');
                    }
                },
                {
                    targets: 7,

                    render: function (data, type, full) {
                        var createdAt = new Date(full.createdAt).toLocaleDateString('vi-VN');

                        return (
                            '<span class="text-truncate d-flex justify-content-center align-items-center text-heading">' +
                            createdAt +
                            '</span>'
                        );
                    }
                },
                {
                    targets: -1,
                    title: 'Thao tác',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 4,

                    render: function (data, type, full) {
                        const suspendText = full.status === 1 ? 'Khóa' : 'Mở';
                        return (
                            '<div class="d-inline-block text-nowrap">' +

                            '<a href="/admin/customer/edit/' +
                            full.id +
                            '" ' +
                            'class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect waves-light">' +
                            '<i class="ti ti-edit ti-md"></i>' +
                            '</a>' +

                            '<button class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect waves-light dropdown-toggle hide-arrow" ' +
                            'data-bs-toggle="dropdown">' +
                            '<i class="ti ti-dots-vertical ti-md"></i>' +
                            '</button>' +

                            '<div class="dropdown-menu dropdown-menu-end m-0">' +

                            '<a href="/admin/customer/details/' +
                            full.id +
                            '" class="dropdown-item">' +
                            'View' +
                            '</a>' +

                            '<a href="javascript:void(0);" ' +
                            'class="dropdown-item customer-suspend" ' +
                            'data-id="' + full.id + '">' +
                            suspendText +
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
                        '<span class="d-none d-sm-inline-block">Thêm khách hàng</span>',

                    className: 'add-new btn btn-primary ms-2 waves-effect waves-light',
                },
                {
                    extend: 'print',
                    text: '<i class="ti ti-printer"></i> Print',
                    className: 'btn btn-primary ms-2 waves-effect waves-light',
                    exportOptions: {
                        rows: function (idx, data, node) {

                            const checkbox = $(node).find('.dt-checkboxes');

                            if (checkbox.prop('checked'))
                                return true;

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

                        return data ? $('<table class="table"><tbody />').append(data) : false;
                    }
                }
            },
            headerCallback: function (thead) {
                const $header = $(thead).find('th').eq(1);

                if (!$header.find('#select-all-customers').length) {
                    $header.html(
                        '<input type="checkbox" ' +
                        'id="select-all-customers" ' +
                        'class="form-check-input">'
                    );
                }
            },
            drawCallback: function () {
                updateSelectAllState();
            },
            initComplete: function () {
                // Adding status filter once table initialized
                $.get('/admin/customer/statuses', function (statuses) {

                    var select = $(
                        '<select id="FilterStatus" class="form-select">' +
                        '<option value="">Chọn trạng thái</option>' +
                        '</select>'
                    );

                    select.appendTo('.customer_status');

                    statuses.forEach(function (status) {

                        select.append(
                            `<option value="${status.value}">${status.name}</option>`
                        );


                    });

                    select.on('change', function () {
                        dt_customers.ajax.reload();
                    });

                });
                //var stockSelect = $(
                //    '<select id="ProductStock" class="form-select">' +
                //    '<option value="">Tồn kho</option>' +
                //    '<option value="1">Còn hàng</option>' +
                //    '<option value="0">Hết hàng</option>' +
                //    '</select>'
                //);
                //stockSelect
                //    .appendTo('.product_stock')
                //    .on('change', function () {
                //        dt_products.ajax.reload();
                //    });
            }

        });

    }
    function formatPrice(row) {
        const totalSpent = Number(row.totalSpent || 0);

        if (minPrice === maxPrice) {
            return minPrice.toLocaleString("vi-VN");
        }

        return `${minPrice.toLocaleString("vi-VN")} - ${maxPrice.toLocaleString("vi-VN")}`;
    }
    function updateSelectAllState() {
    const $checkboxes = $('.datatables-customers tbody .dt-checkboxes');

    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;

    const selectAll =
        document.getElementById('select-all-customers');

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
    '#select-all-customers',
    function () {
        const checked = this.checked;

        $('.datatables-customers tbody .dt-checkboxes').prop('checked', checked);

        this.indeterminate = false;
    }
);


// Checkbox từng row
$('.datatables-customers tbody').on(
    'change',
    '.dt-checkboxes',
    function () {
        updateSelectAllState();
    }
);
  // Delete Record
  $('.datatables-customers tbody').on('click', '.delete-record', function () {
    dt_customers.row($(this).parents('tr')).remove().draw();
  });

    setTimeout(() => {
        $('.dt-search .form-control').removeClass('form-control-sm');

        $('.dt-length .form-select').removeClass('form-select-sm');
    }, 300);


    //checkbox all
    $('#select-all-customers').on('change', function () {
        console.log('select all');
        const checked = this.checked;

        $('.datatables-customers tbody .dt-checkboxes')
            .prop('checked', checked);
    });
    $('.datatables-customers tbody').on('change', '.dt-checkboxes', function () {
        const total = $('.datatables-customers tbody .dt-checkboxes').length;
        const checked = $('.datatables-customers tbody .dt-checkboxes:checked').length;

        $('#select-all-customers').prop('checked', total > 0 && total === checked
        );
    });


    $(document).on('click', '.add-new', function () {
        window.location.href='/admin/customer/create'
    });

    $(document).on('click', '.customers-suspend', function () {
        const token = $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();
        console.log('new');
        const id = $(this).data('id');

        $.ajax({
            url: '/admin/customer/toggle-suspend',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify({ id: id }),
            success: function (response) {
                if (response.success) {
                    Toast.success(response.message);
                    dt_customers.ajax.reload(null, false);
                    $('.dtr-bs-modal').modal('hide');
                } else {
                    Toast.error(response.message);
                }
            },
            error: function (xhr) {
                Toast.error('Có lỗi xảy ra.');
                console.error(xhr);
            }
        });
    });

});
