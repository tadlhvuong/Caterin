
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
    //var statusObj = {
    //    true: {
    //        title: 'Hoạt động',
    //        class: 'bg-label-success'
    //    },
    //    false: {
    //        title: 'Tạm ngừng ',
    //        class: 'bg-label-warning'
    //    },
    //};

    //const commentEditor = document.querySelector('.comment-editor');
    //let commentQuill = null;
    //if (commentEditor) {
    //    commentQuill = new Quill(commentEditor, {
    //        modules: {
    //            toolbar: '.comment-toolbar',
    //        },
    //        placeholder: 'Miêu tả...',
    //        theme: 'snow'
    //    });
    //}

    const dtAttributeListTable = $('.datatables-attribute-list');

    if (!dtAttributeListTable.length) {
        return;
    }
    $.fn.dataTable.ext.pager.numbers_length = 7;
    const dtAttribute = dtAttributeListTable.DataTable({
        processing: true,
        serverSide: true,
        paging: true,
        pagingType: "simple_numbers", //or full_numbers 
        ajax: {
            url: '/admin/product/get-attributes',
            type: 'POST',
            contentType: 'application/json',

            data: function (d) {
                console.log(d);
                // DataTables column index -> backend SortColumn
                const columnMap = {
                    0: 'id',
                    1: 'Tên',
                    2: 'code',
                    3: 'Ngày',
                };
                const order = d.order && d.order.length ? d.order[0] : null;

                return JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    search: d.search?.value || '',
                    sortColumn: order ? columnMap[order.column] || 'id' : 'id',
                    sortDirection: order?.dir || 'asc'
                });
            },
            dataSrc: 'data'
        },

        columns: [
            { data: 'id' },
            { data: 'name' },
            { data: 'code' },
            { data: 'createdAt' },
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

            // Attribute
            {
                targets: 1,
                responsivePriority: 1,

                render: function (data) {
                    return '<span class="text-truncate d-flex align-items-center text-heading">' +
                        data +
                        '</span>'
                }
            },
            // Code
            {
                targets: 2,

                render: function (data) {
                    return '<span class="text-truncate d-flex align-items-center text-heading">' +
                        data +
                        '</span>'
                }
            },

            //Status 
            //{
            //    targets: 3,
            //    render: function (data, type, full) {

            //        var status = full.isActive;

            //        var item = statusObj[status];

            //        if (!item) {
            //            return '';
            //        }

            //        return (
            //            '<span class="badge ' +
            //            item.class +
            //            '">' +
            //            item.title +
            //            '</span>'
            //        );
            //    }
            //},

            // Product Count
            {
                targets: 3,
                responsivePriority: 2,

                render: function (data) {
                    var date = new Date(data).toLocaleDateString('vi-VN');
                    return '<span class="text-truncate d-flex align-items-center text-heading">' +
                        date +
                        '</span>'
                }
            },
            // Actions
            {
                targets: 4,
                title: 'Actions',
                searchable: false,
                orderable: false,
                responsivePriority: 3,

                render: function (data, type, full) {
                    return `
                        <div class="d-flex align-items-sm-center justify-content-sm-center">

                            <button
                                type="button"
                                class="btn btn-icon btn-text-secondary rounded-pill waves-effect waves-light btn-edit-attribute"
                                data-id="${full.id}">
                                <i class="ti ti-edit"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-text-secondary rounded-pill waves-effect waves-lightn btn-delete-attribute"
                                    data-id="${full.id}">
                                <i class="ti ti-trash"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],

        //order: [
        //    [1, 'asc']
        //],

        dom:
            '<"card-header d-flex flex-wrap py-0 flex-column flex-sm-row pt-2"' +
            '<f>' +
            '<"d-flex justify-content-center justify-content-md-end align-items-baseline ms-md-auto"' +
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
                    '<span class="d-none d-sm-inline-block">Thêm biến thể</span>',

                className:
                    'add-new btn btn-primary ms-2 waves-effect waves-light',

                attr: {
                    'data-bs-toggle': 'offcanvas',
                    'data-bs-target': '#offcanvasAttributeList'
                }
            }
        ],

        responsive: true
    });

    setTimeout(() => {
        $('.dt-search .form-control').removeClass('form-control-sm');
        $('.dt-length .form-select').removeClass('form-select-sm');
    }, 300);

    const nameInput = $('#Name');
    const slugInput = $('#Code');

    $(document).on('submit', '#form-create-attribute', function (e) {
        e.preventDefault();
        createAttribute();
    });
    const form = document.querySelector('#form-create-attribute');
    document.querySelector('#btn-reset-attribute').addEventListener('click', function () {
        clearValidationErrors(form);
    });
    function prepareCreateAttribute() {
        resetAttributeForm();

        $('#attributeOffcanvasTitle').text('Thêm mới');

        $('.data-submit').text('Thêm mới');
    }

    function prepareEditAttribute(attribute) {
        $('#Id').val(attribute.id);
        $('#Name').val(attribute.name);
        $('#Code').val(attribute.code);

        clearValidationErrors(document.querySelector('#form-create-attribute'));

        $('#attributeOffcanvasTitle').text('Cập nhật');
        $('.data-submit').text('Cập nhật');
    }

    function resetAttributeForm() {
        const form = document.querySelector('#form-create-attribute');

        if (!form)
            return;

        form.reset();

        $('#Id').val(0);

        clearValidationErrors(form);

        $('.data-submit').text('Thêm mới');

        $('#attributeOffcanvasTitle').text('Thêm mới');
    }

    function createAttribute() {
        const form = document.querySelector('#form-create-attribute');

        if (!form.length)
            return;

        const formData = new FormData(form);

        for (const [key, value] of formData.entries()) {
            console.log(key, value);
        }

        $.ajax({
            url: '/admin/product/edit-product-attribute',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            beforeSend: function () {
                form.querySelector('button[type="submit"]').disabled = true;
            },
            success: function (response) {
                if (response.success) {

                    // Đóng offcanvas
                    const offcanvasElement = document.getElementById('offcanvasAttributeList');

                    if (offcanvasElement) {
                        const offcanvas = bootstrap.Offcanvas.getInstance(offcanvasElement);

                        offcanvas?.hide();
                    }

                    // Reload DataTable
                    dtAttribute.ajax.reload(null, false);

                    // Reset form
                    form.reset();

                    // Toast thành công
                    if (typeof Toastify !== 'undefined') {
                        Toast.success(`${response.message}`);
                        clearValidationErrors(form);

                    }
                } else {
                    Toast.error(`${response.message}`);
                }
            },

            error: function (xhr) {
                handleValidationErrors(xhr, form);
            },

            complete: function () {
                form.querySelector('button[type="submit"]').disabled = false;
            }
        });
    }
    $(document).on('click', '.add-new', function () {
        prepareCreateAttribute();
    });

    $(document).on('click', '.btn-edit-attribute', async function () {
        clearValidationErrors(form);
        const id = $(this).data('id');
        try {

            const response = await fetch(`/admin/product/get-attribute/${id}`);

            const result = await response.json();
            if (!response.ok) {
                Toast.error(result.message || 'Unable to load attribute.');
                return;
            }
            prepareEditAttribute(result.data);

            const offcanvasElement = document.getElementById('offcanvasAttributeList');
            const offcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasElement);
            offcanvas.show();
        } catch (error) {
            console.error(error);
            Toast.error('Unable to load attribute.');
        }
    });

    $(document).on('click', '.btn-delete-attribute', async function () {

        const id = $(this).data('id');
        const confirmed = await Swal.fire({
            title: 'Delete attribute?',
            text: 'Bạn có chắc muốn xóa attribute này?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        });

        if (!confirmed.isConfirmed) {
            return;
        }

        const token = $('#form-create-attribute input[name="__RequestVerificationToken"]').val();

        try {
            const response = await fetch('/admin/product/delete-attribute', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    id: id
                })
            });

            const result = await response.json();

            if (!response.ok) {
                Toast.error(result.detail || result.message || 'Không thể xóa attribute.');
                return;
            }

            Toast.success(result.message);
            dtAttribute.ajax.reload(null, false);

        } catch (error) {
            console.error(error);
            Toast.error('Có lỗi xảy ra khi xóa attribute.');
        }
    });
});
