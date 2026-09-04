/**
 * Page User List
 */

'use strict';

// Datatable (jquery)
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

  // Variable declaration for table
  var dt_user_table = $('.datatables-users'),
    select2 = $('.select2'),
    userView = 'app-user-view-account.html',
    //statusObj = {
    //  1: { title: 'Pending', class: 'bg-label-warning' },
    //  2: { title: 'Active', class: 'bg-label-success' },
    //  3: { title: 'Inactive', class: 'bg-label-secondary' }
    //};
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

  // Users datatable
  if (dt_user_table.length) {
    var dt_user = dt_user_table.DataTable({
        processing: true,
        serverSide: true,
        ajax: function (data, callback) {

            $.ajax({
                url: '/admin/user/list',
                type: 'GET',
                data: {
                    page: Math.floor(data.start / data.length) + 1,
                    pageSize: data.length,
                    keyword: data.search.value,
                    status: $('#FilterStatus').val(),
                    role: $('#UserRole').val()
                },
                success: function (res) {
                    callback({
                        data: res.items,
                        recordsTotal: res.totalCount,
                        recordsFiltered: res.totalCount
                    });
                }
            });
        },
      columns: [
        // columns according to JSON
        { data: 'id' },
        { data: 'id' },
        { data: 'fullName' },
        { data: 'roles' },
        { data: 'billing' },
        { data: 'status' },
          { data: 'action ' }
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
          responsivePriority: 4,
          render: function (data, type, full, meta) {
            var $name = full['fullName'],
              $email = full['email'],
              $image = full['avatar'];
            if ($image) {
              // For Avatar image
              var $output =
                '<img src="' + assetsPath + 'img/avatars/' + $image + '" alt="Avatar" class="rounded-circle">';
            } else {
              // For Avatar badge
              var stateNum = Math.floor(Math.random() * 6);
              var states = ['success', 'danger', 'warning', 'info', 'primary', 'secondary'];
              var $state = states[stateNum],
                  $name = full['fullName'],
                  userView = '/admin/user/user-details/' + full['id'],
                $initials = $name.match(/\b\w/g) || [];
              $initials = (($initials.shift() || '') + ($initials.pop() || '')).toUpperCase();
              $output = '<span class="avatar-initial rounded-circle bg-label-' + $state + '">' + $initials + '</span>';
            }
            // Creates full output for row
            var $row_output =
              '<div class="d-flex justify-content-start align-items-center user-name">' +
              '<div class="avatar-wrapper">' +
              '<div class="avatar avatar-sm me-4">' +
              $output +
              '</div>' +
              '</div>' +
              '<div class="d-flex flex-column">' +
              '<a href="' +
              userView +
              '" class="text-heading text-truncate"><span class="fw-medium">' +
              $name +
              '</span></a>' +
              '<small>' +
              $email +
              '</small>' +
              '</div>' +
              '</div>';
            return $row_output;
          }
        },
          {
              targets: 3,
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
          // Plans
          targets: 4,
          render: function (data, type, full, meta) {
            var $plan = full['billing'];

            return '<span class="text-heading">' + $plan + '</span>';
          }
        },
        {
          // User Status
          targets: 5,
          render: function (data, type, full, meta) {
            var $status = full['status'];
            return (
              '<span class="badge ' +
              statusObj[$status].class +
              '" text-capitalized>' +
              statusObj[$status].title +
              '</span>'
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
              '<div class="d-flex align-items-center">' +
              '<a href="javascript:;" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill delete-record"><i class="ti ti-trash ti-md"></i></a>' +
              '<a href="' +
              userView +
              '" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill"><i class="ti ti-eye ti-md"></i></a>' +
              '<a href="javascript:;" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-md"></i></a>' +
              '<div class="dropdown-menu dropdown-menu-end m-0">' +
              '<a href="javascript:;"" class="dropdown-item">Edit</a>' +
              '<a href="javascript:;" class="dropdown-item">Suspend</a>' +
              '</div>' +
              '</div>'
            );
          }
        }
      ],
      order: [[2, 'desc']],
      dom:
        '<"row m-0"' +
        '<"col-md-2"<"ms-n2"l>>' +
        '<"col-md-10"<"dt-action-buttons d-flex align-items-center justify-content-end"fB>>' +
        '>t' +
        '<"row m-0"' +
        '<"col-sm-12 col-md-6"i>' +
        '<"col-sm-12 col-md-6"p>' +
        '>',
      language: {
        sLengthMenu: '_MENU_',
        search: '',
        searchPlaceholder: 'Search User',
        paginate: {
          next: '<i class="ti ti-chevron-right ti-sm"></i>',
          previous: '<i class="ti ti-chevron-left ti-sm"></i>'
        }
      },
      // Buttons with Dropdown
      buttons: [
        {
          extend: 'collection',
          className: 'btn btn-label-secondary dropdown-toggle mx-4 waves-effect waves-light',
          text: '<i class="ti ti-upload me-2 ti-xs"></i>Export',
          buttons: [
            {
              extend: 'print',
              text: '<i class="ti ti-printer me-2" ></i>Print',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be print
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              },
              customize: function (win) {
                //customize print view for dark
                $(win.document.body)
                  .css('color', headingColor)
                  .css('border-color', borderColor)
                  .css('background-color', bodyBg);
                $(win.document.body)
                  .find('table')
                  .addClass('compact')
                  .css('color', 'inherit')
                  .css('border-color', 'inherit')
                  .css('background-color', 'inherit');
              }
            },
            {
              extend: 'csv',
              text: '<i class="ti ti-file-text me-2" ></i>Csv',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            },
            {
              extend: 'excel',
              text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            },
            {
              extend: 'pdf',
              text: '<i class="ti ti-file-code-2 me-2"></i>Pdf',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            },
            {
              extend: 'copy',
              text: '<i class="ti ti-copy me-2" ></i>Copy',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            }
          ]
        },
        {
          text: '<i class="ti ti-plus me-0 me-sm-1 ti-xs"></i><span class="d-none d-sm-inline-block">Add New User</span>',
          className: 'add-new btn btn-primary waves-effect waves-light',
          attr: {
            'data-bs-toggle': 'offcanvas',
            'data-bs-target': '#offcanvasAddUser'
          }
        }
      ],
      // For responsive popup
      responsive: {
        details: {
          display: $.fn.dataTable.Responsive.display.modal({
            header: function (row) {
              var data = row.data();
              return 'Details of ' + data['full_name'];
            }
          }),
          type: 'column',
          renderer: function (api, rowIdx, columns) {
            var data = $.map(columns, function (col, i) {
              return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                ? '<tr data-dt-row="' +
                    col.rowIndex +
                    '" data-dt-column="' +
                    col.columnIndex +
                    '">' +
                    '<td>' +
                    col.title +
                    ':' +
                    '</td> ' +
                    '<td>' +
                    col.data +
                    '</td>' +
                    '</tr>'
                : '';
            }).join('');

            return data ? $('<table class="table"/><tbody />').append(data) : false;
          }
        }
      },
      initComplete: function () {
        // Adding role filter once table initialized
          $.get('/admin/user/roles', function (roles) {

              var select = $(
                  '<select id="UserRole" class="form-select text-capitalize">' +
                  '<option value="">Select Role</option>' +
                  '</select>'
              );

              select.appendTo('.user_role');

              roles.forEach(function (role) {
                  select.append(
                      `<option value="${role.name}">
                ${role.name}
            </option>`
                  );
              });

              select.on('change', function () {
                  dt_user.ajax.reload();
              });

          });
        // Adding plan filter once table initialized
          $.get('/admin/user/statuses', function (statuses) {
              var select = $(
                  '<select id="FilterStatus" class="form-select">' +
                  '<option value="">Select Status</option>' +
                  '</select>'
              );

              select.appendTo('.user_status');

              statuses.forEach(function (status) {
                  select.append(
                      `<option value="${status.value}">
                ${status.name}
            </option>`
                  );
              });

              select.on('change', function () {
                  dt_user.ajax.reload();
              });

          });
      }
    });
  }


  // Delete Record
  $('.datatables-users tbody').on('click', '.delete-record', function () {
    dt_user.row($(this).parents('tr')).remove().draw();
  });

  // Filter form control to default size
  // ? setTimeout used for multilingual table initialization
  setTimeout(() => {
      $('.dt-length .form-select').removeClass('form-select-sm');
      $('.dt-search .form-control').removeClass('form-control-sm');
      
  }, 300);
});

// Validation & Phone mask
(function () {
  const phoneMaskList = document.querySelectorAll('.phone-mask'),
    addNewUserForm = document.getElementById('addNewUserForm');

  // Phone Number
  if (phoneMaskList) {
    phoneMaskList.forEach(function (phoneMask) {
      new Cleave(phoneMask, {
        phone: true,
        phoneRegionCode: 'US'
      });
    });
  }
  // Add New User Form Validation
  //const fv = FormValidation.formValidation(addNewUserForm, {
  //  fields: {
  //    userFullname: {
  //      validators: {
  //        notEmpty: {
  //          message: 'Please enter fullname '
  //        }
  //      }
  //    },
  //    userEmail: {
  //      validators: {
  //        notEmpty: {
  //          message: 'Please enter your email'
  //        },
  //        emailAddress: {
  //          message: 'The value is not a valid email address'
  //        }
  //      }
  //    }
  //  },
  //  plugins: {
  //    trigger: new FormValidation.plugins.trigger(),
  //    bootstrap5: new FormValidation.plugins.Bootstrap5({
  //      // Use this for enabling/changing valid/invalid class
  //      eleValidClass: '',
  //      rowSelector: function (field, ele) {
  //        // field is the field name & ele is the field element
  //        return '.mb-6';
  //      }
  //    }),
  //    submitButton: new FormValidation.plugins.SubmitButton(),
  //    // Submit the form when all fields are valid
  //    // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
  //    autoFocus: new FormValidation.plugins.AutoFocus()
  //  }
  //});
})();


$(function () {
    $('#btnGeneratePassword').on('click', function () {

        const password = generatePassword();

        $('#NewPassword').val(password);
        $('#ConfirmPassword').val(password);

        // trigger validation nếu dùng FormValidation
        fv.revalidateField('NewPassword');
        fv.revalidateField('ConfirmPassword');
    });
})