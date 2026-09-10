
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
        true: {
            title: 'Hoạt động',
            class: 'bg-label-success'
        },
        false: {
            title: 'Tạm ngừng ',
            class: 'bg-label-warning'
        },
    };



    const commentEditor = document.querySelector('.comment-editor');
    let commentQuill = null;
    if (commentEditor) {
        commentQuill = new Quill(commentEditor, {
            modules: {
                toolbar: '.comment-toolbar',
            },
            placeholder: 'Miêu tả...',
            theme: 'snow'
        });
    }

    const dtCategoryListTable = $('.datatables-category-list');

    if (!dtCategoryListTable.length) {
        return;
    }
    $.fn.dataTable.ext.pager.numbers_length = 7;
    const dtCategory = dtCategoryListTable.DataTable({
        processing: true,
        serverSide: true,
        paging: true,
        pagingType: "simple_numbers", //or full_numbers 
        ajax: {
            url: '/admin/product/get-categories',
            type: 'POST',
            contentType: 'application/json',

            data: function (d) {
                // DataTables column index -> backend SortColumn
                const columnMap = {
                    0: 'id',
                    1: 'Tên',
                    2: 'slug',
                    3: 'Trạng thái',
                    4: 'SL Sản phẩm'
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
            { data: 'slug' },
            { data: 'isActive' },
            { data: 'productCount' },
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

            // Category
            {
                targets: 1,
                responsivePriority: 1,

                render: function (data, type, full) {
                    const name = full.name || '';
                    const description = full.description || '';

                    const initials = (name.match(/\b\w/g) || [])
                        .slice(0, 2)
                        .join('')
                        .toUpperCase();

                    return `
                        <div class="d-flex align-items-center">

                            <div class="avatar-wrapper me-3 rounded-2 bg-label-secondary">
                                <div class="avatar">
                                    <span class="avatar-initial rounded-2 bg-label-primary">
                                        ${initials}
                                    </span>
                                </div>
                            </div>

                            <div class="d-flex flex-column justify-content-center">

                                <span class="text-heading text-wrap fw-medium">
                                    ${name}
                                </span>

                                ${description
                            ? `
                                        <span class="text-truncate mb-0 d-none d-sm-block">
                                            <small>${description}</small>
                                        </span>
                                      `
                            : ''
                        }

                            </div>

                        </div>
                    `;
                }
            },

            // Slug
            {
                targets: 2,

                render: function (data) {
                    return `
                        <span class="text-muted">
                            ${data || ''}
                        </span>
                    `;
                }
            },

            //Status 
            {
                targets: 3,
                render: function (data, type, full) {

                    var status = full.isActive;

                    var item = statusObj[status];

                    if (!item) {
                        return '';
                    }

                    return (
                        '<span class="badge ' +
                        item.class +
                        '">' +
                        item.title +
                        '</span>'
                    );
                }
            },

            // Product Count
            {
                targets: 4,
                responsivePriority: 2,

                render: function (data) {
                    return ` <div class="text-sm-end"> ${data ?? 0} </div> `;
                }
            },

            // Actions
            {
                targets: 5,
                title: 'Actions',
                searchable: false,
                orderable: false,
                responsivePriority: 3,

                render: function (data, type, full) {
                    return `
                        <div class="d-flex align-items-sm-center justify-content-sm-center">

                            <button
                                type="button"
                                class="btn btn-icon btn-text-secondary rounded-pill waves-effect waves-light btn-edit-category"
                                data-id="${full.id}">
                                <i class="ti ti-edit"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-text-secondary rounded-pill waves-effect waves-lightn btn-delete-category"
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
                    '<span class="d-none d-sm-inline-block">Add Category</span>',

                className:
                    'add-new btn btn-primary ms-2 waves-effect waves-light',

                attr: {
                    'data-bs-toggle': 'offcanvas',
                    'data-bs-target': '#offcanvasEcommerceCategoryList'
                }
            }
        ],

        responsive: true
    });

    setTimeout(() => {
        $('.dt-search .form-control').removeClass('form-control-sm');

        $('.dt-length .form-select').removeClass('form-select-sm');
    }, 300);

    const btnGenerateSlug = document.querySelector('#btn-generate-slug');
    const nameInput = $('#Name');
    const slugInput = $('#Slug');

    btnGenerateSlug?.addEventListener('click', async function (e) {
        e.preventDefault();

        const name = nameInput.val()?.trim();

        if (!name) {
            Toast.warning('Please enter category name first.');
            nameInput?.focus();
            return;
        }

        try {
            btnGenerateSlug.classList.add('disabled');
            btnGenerateSlug.textContent = 'Generating...';

            const response = await fetch(`/admin/product/generate-slug-category?name=${encodeURIComponent(name)}`
            );

            const result = await response.json();
            console.log('slug generate: ');
            console.log(result);
            if (!response.ok || !result.success) {
                Toast.error(
                    result.message || 'Unable to generate slug.'
                );
                return;
            }

            slugInput.val(result.slug);
            clearValidationErrors(form);

        } catch (error) {
            console.error(error);

            Toast.error('Unable to generate slug.');
        } finally {
            btnGenerateSlug.classList.remove('disabled');
            btnGenerateSlug.innerHTML =
                '<i class="ti ti-wand me-1"></i> Generate Slug';
        }
    });

    $(document).on('submit', '#form-create-category', function (e) {
        e.preventDefault();
        createCategory();
    });
    const form = document.querySelector('#form-create-category');
    document.querySelector('#btn-reset-category').addEventListener('click', function () {
        clearValidationErrors(form);
        commentQuill.root.innerHTML = '';
    });
    function prepareCreateCategory() {
        resetCategoryForm();

        $('#offcanvasEcommerceCategoryListLabel')
            .text('Add Category');

        $('.data-submit')
            .text('Add');
    }

    function prepareEditCategory(category) {
        $('#Id').val(category.id);
        $('#Name').val(category.name);
        $('#Slug').val(category.slug);
        $('#SeoTitle').val(category.seoTitle);
        $('#SeoDescription').val(category.seoDescription);
        $('#SeoKeyword').val(category.seoKeyword);

        commentQuill.root.innerHTML = category.description ?? '';

        $('#IsActive')
            .val(category.isActive ? 'true' : 'false')
            .trigger('change');
        clearValidationErrors(
            document.querySelector('#form-create-category')
        );
        $('#offcanvasEcommerceCategoryListLabel')
            .text('Edit Category');

        $('.data-submit')
            .text('Update');
    }

    function resetCategoryForm() {
        const form = document.querySelector('#form-create-category');

        if (!form) {
            return;
        }

        form.reset();

        // ID
        $('#Id').val(0);

        // Validation
        clearValidationErrors(form);

        // Quill
        if (commentQuill) {
            commentQuill.root.innerHTML = '';
        }

        // Default status
        $('#IsActive').val('true').trigger('change');

        // Button
        $('.data-submit').text('Add');

        // Title
        $('#offcanvasEcommerceCategoryListLabel')
            .text('Add Category');
    }

    function createCategory() {
        const form = document.querySelector('#form-create-category');

        if (!form.length) {
            return;
        }
        const formData = new FormData(form);

        if (commentEditor) {
            formData.set('Description', commentQuill.root.innerHTML);
        }
        for (const [key, value] of formData.entries()) {
            console.log(key, value);
        }

        //const id = Number($('#Id').val());
        //const url = id === 0
        //    ? '/admin/product/create-product-category'
        //    : '/admin/product/update-product-category';

        $.ajax({
            url: '/admin/product/edit-product-category',
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
                    const offcanvasElement = document.getElementById('offcanvasEcommerceCategoryList');

                    if (offcanvasElement) {
                        const offcanvas = bootstrap.Offcanvas.getInstance(offcanvasElement);

                        offcanvas?.hide();
                    }

                    // Reload DataTable
                    dtCategory.ajax.reload(null, false);

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
                console.log('bb');
                handleValidationErrors(xhr, form);
            },

            complete: function () {
                form.querySelector('button[type="submit"]').disabled = false;
            }
        });
    }
    $(document).on('click', '.add-new', function () {

        prepareCreateCategory();

    });

    $(document).on('click', '.btn-edit-category', async function () {
        clearValidationErrors(form);
        const id = $(this).data('id');
        try {

            const response = await fetch(`/admin/product/get-category/${id}`);

            const result = await response.json();
            if (!response.ok) {
                Toast.error(result.message || 'Unable to load category.');
                return;
            }
            prepareEditCategory(result.data);
            const offcanvasElement = document.getElementById('offcanvasEcommerceCategoryList');

            const offcanvas =
                bootstrap.Offcanvas.getOrCreateInstance(offcanvasElement);

            offcanvas.show();
        } catch (error) {
            console.error(error);
            Toast.error('Unable to load category.');
        }
    });

    $(document).on('click', '.btn-delete-category', async function () {

        const id = $(this).data('id');
        const confirmed = await Swal.fire({
            title: 'Delete category?',
            text: 'Bạn có chắc muốn xóa category này?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        });

        if (!confirmed.isConfirmed) {
            return;
        }

        const token = $('#form-create-category input[name="__RequestVerificationToken"]').val();

        try {
            const response = await fetch('/admin/product/delete-category', {
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
                Toast.error(result.detail || result.message || 'Không thể xóa category.');
                return;
            }

            Toast.success(result.message);
            dtCategory.ajax.reload(null, false);

        } catch (error) {
            console.error(error);
            Toast.error('Có lỗi xảy ra khi xóa category.');
        }
    });
});
