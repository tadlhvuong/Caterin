/**
 * User View 
 */
'use strict';
(function () {

    //document.addEventListener('DOMContentLoaded', function () {
    //    window.ChatScroll?.initAll(
    //        document.getElementById('userDetailsNavScroll')?.parentElement
    //        ?? document
    //    );
    //});

    
    document.body.addEventListener('htmx:afterSwap', function (e) {
        if (e.target.id === 'tab-content') {
            window.Helpers.initPasswordToggle();
        }
        const form = document.getElementById('updateUserForm');
       
        if (!form) return;

        const submitButton = document.getElementById('btnUpdateUser');
        const modalElement = document.getElementById('editUser');
        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

$(document).on('submit', '#updateUserForm', function (e) {
    e.preventDefault();

    const $form = $(this);

    if ($form.data('submitting')) {
        return;
    }

    if ($form.valid && !$form.valid()) {
        return;
    }

    const $submitBtn = $form.find('[type="submit"]');
    const originalContent = $submitBtn.html();

    // Xóa lỗi validation cũ.
    $form.find('[data-valmsg-for]').text('');

    const $summary = $form.find('[data-valmsg-summary="true"]');
    $summary.empty()
        .addClass('validation-summary-valid')
        .removeClass('validation-summary-errors');

    function showValidationErrors(data) {
        if (data.errors) {
            Object.entries(data.errors).forEach(([key, messages]) => {
                const $field = $form.find(
                    `[data - valmsg -for= "${key}"]`
                );

                if ($field.length) {
                    $field.text(
                        Array.isArray(messages)
                            ? messages.join(' ')
                            : messages
                    );
                }
            });
        }

        if (data.message) {
            $summary
                .removeClass('validation-summary-valid')
                .addClass('validation-summary-errors')
                .text(data.message);
        }
    }

    $form.data('submitting', true);

    $.ajax({
        url: $form.attr('action'),
        type: 'POST',
        data: $form.serialize(),

        beforeSend: function () {
            $submitBtn.prop('disabled', true)
                .html(
                    '<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...'
                );
        },

        success: function (response) {
            if (!response.success) {
                showValidationErrors(response);
                return;
            }

            // Cập nhật số điện thoại trên card Details.
            const phoneNumber = $form
                .find('[name="PhoneNumber"]')
                .val()?.trim();

            $('#detailPhoneNumber').text(
                phoneNumber || 'Chưa cập nhật'
            );

            // Cập nhật trạng thái tài khoản.
            const isActive = $form
                .find('[name="IsActive"]')
                .is(':checked');

            const $statusButton = $('.suspend-user');

            if ($statusButton.length) {
                $statusButton
                    .attr('data-active', String(isActive))
                    .text(isActive ? 'Suspend' : 'Activate')
                    .toggleClass('btn-label-danger', isActive)
                    .toggleClass('btn-label-success', !isActive);
            }

            Swal.fire({
                icon: 'success',
                title: 'Thành công',
                text: response.message || 'Cập nhật tài khoản thành công.',
                customClass: {
                    confirmButton: 'btn btn-success waves-effect waves-light'
                },
                buttonsStyling: false
            }).then(() => {
                const modalElement = document.getElementById('editUser');

                if (modalElement) {
                    bootstrap.Modal
                        .getOrCreateInstance(modalElement)
                        .hide();
                }
            });
        },

        error: function (xhr) {
            const data = xhr.responseJSON;

            if (data && (data.errors || data.message)) {
                showValidationErrors(data);
                return;
            }

            Swal.fire({
                icon: 'error',
                title: 'Lỗi',
                text: data?.message
                    || 'Đã xảy ra lỗi khi cập nhật tài khoản.'
            });
        },

        complete: function () {
            $form.data('submitting', false);

            $submitBtn.prop('disabled', false)
                .html(originalContent);
        }
    });
});

        $('#btnGeneratePassword').on('click', function () {
            console.log('generate');
            const password = generatePassword();

            $('#newPassword').val(password);
            $('#confirmPassword').val(password);
        });

        $('#formChangePassword').on('submit', function (e) {
            e.preventDefault();

            const $form = $(this);

            if (!$form.valid()) {
                return;
            }

            Swal.fire({
                icon: 'warning',
                title: 'Xác nhận đặt lại mật khẩu?',
                text: 'Bạn có chắc chắn muốn đặt lại mật khẩu cho tài khoản này không?',
                showCancelButton: true,
                confirmButtonText: 'Xác nhận',
                cancelButtonText: 'Hủy',
                reverseButtons: true,
                focusCancel: true
            }).then((result) => {
                if (!result.isConfirmed) {
                    return;
                }

                const formData = $form.serialize();
                const $submitBtn = $form.find('[type="submit"]');

                $.ajax({
                    url: $form.attr('action'),
                    type: 'POST',
                    data: formData,

                    beforeSend: function () {
                        $submitBtn.prop('disabled', true);
                    },

                    success: function (response) {
                        if (response.success) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Thành công',
                                text: response.message || 'Đặt lại mật khẩu thành công.'
                            }).then(() => {
                                $('#changePasswordModal').modal('hide');
                                $form[0].reset();
                                $form.validate().resetForm();

                                $form.find('.is-valid, .is-invalid')
                                    .removeClass('is-valid is-invalid');
                            });
                        } else {
                            Swal.fire({
                                icon: 'error',
                                title: 'Thất bại',
                                text: response.message || 'Không thể đặt lại mật khẩu.'
                            });
                        }
                    },

                    error: function (xhr) {
                        const message = xhr.responseJSON?.message
                            || 'Đã xảy ra lỗi khi đặt lại mật khẩu.';

                        Swal.fire({
                            icon: 'error',
                            title: 'Lỗi',
                            text: message
                        });
                    },

                    complete: function () {
                        $submitBtn.prop('disabled', false);
                    }
                });
            });
        });

        $('#formChangePassword').validate({

            validClass: 'is-valid',      // Không dùng class valid mặc định
            errorClass: 'is-invalid',

            rules: {
                newPassword: {
                    required: true,
                    minlength: 8
                },
                confirmPassword: {
                    required: true,
                    equalTo: '#newPassword'
                }
            },

            highlight: function (element) {
                $(element)
                    .removeClass('is-valid')
                    .addClass('is-invalid');
                console.log(element.className);
            },

            unhighlight: function (element) {
                console.log('unhighlight');
                $(element)
                    .removeClass('is-invalid')
                    .addClass('is-valid');
            },

            errorElement: 'div',

            errorPlacement: function (error, element) {

                error.addClass('invalid-feedback');

                if (element.parent('.input-group').length) {
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                }
            }
        });
    });



    const suspendUser = document.querySelector('.suspend-user');

    if (suspendUser) {
        suspendUser.addEventListener('click', async function (event) {
            event.preventDefault();

            const button = this;
            const userId = button.dataset.userId;
            const isActive = button.dataset.active === 'true';

            if (!userId) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Không tìm thấy tài khoản.'
                });
                return;
            }

            const action = isActive ? 'suspend' : 'activate';

            const result = await Swal.fire({
                title: isActive ? 'Are you sure?' : 'Activate user?',
                text: isActive
                    ? 'Bạn có chắc chắn muốn tạm ngưng tài khoản này không?'
                    : 'Bạn có chắc chắn muốn kích hoạt lại tài khoản này không?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: isActive
                    ? 'Yes, suspend user!'
                    : 'Yes, activate user!',
                cancelButtonText: 'Cancel',
                customClass: {
                    confirmButton: isActive
                        ? 'btn btn-danger me-2 waves-effect waves-light'
                        : 'btn btn-success me-2 waves-effect waves-light',
                    cancelButton:
                        'btn btn-label-secondary waves-effect waves-light'
                },
                buttonsStyling: false
            });

            if (!result.isConfirmed) return;

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            if (!token) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Không tìm thấy mã xác thực. Vui lòng tải lại trang.'
                });
                return;
            }

            button.classList.add('disabled');
            button.style.pointerEvents = 'none';

            try {
                const response = await fetch(
                    `/admin/user/user-${action}/${encodeURIComponent(userId)}`,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': token
                        }
                    }
                );

                const data = await response.json();

                if (!response.ok || !data.success) {
                    throw new Error(
                        data.message || 'Không thể thực hiện thao tác.'
                    );
                }

                // Cập nhật trạng thái nút sau khi server xác nhận thành công.
                button.dataset.active = isActive ? 'false' : 'true';
                button.textContent = isActive ? 'Activate' : 'Suspend';

                button.classList.toggle('btn-label-danger', !isActive);
                button.classList.toggle('btn-label-success', isActive);

                await Swal.fire({
                    icon: 'success',
                    title: isActive ? 'Suspended!' : 'Activated!',
                    text: data.message,
                    customClass: {
                        confirmButton: 'btn btn-success waves-effect waves-light'
                    },
                    buttonsStyling: false
                });
            } catch (error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: error.message || 'Đã xảy ra lỗi. Vui lòng thử lại.'
                });
            } finally {
                button.classList.remove('disabled');
                button.style.pointerEvents = '';
            }
        });
    }

    ///UPLOAD AVATAR
    const fileInput = document.getElementById('upload');
    const avatarImage = document.getElementById('userAvatar');

    const antiForgeryInput = document.querySelector(
        '#avatarAntiForgeryForm input[name="__RequestVerificationToken"]'
    );

    if (!fileInput || !avatarImage) return;

    const userId = document.getElementById('userDetails')?.dataset.userId;
    const maxFileSize = 2 * 1024 * 1024;
    const allowedExtensions = ['.jpg', '.jpeg', '.png', '.webp'];

    let isUploading = false;

    function setUploading(value) {
        isUploading = value;
        fileInput.disabled = value;
    }

    function getErrorMessage(data, fallback) {
        return data?.message || fallback;
    }

    fileInput.addEventListener('change', async function () {
        const file = fileInput.files?.[0];

        if (!file) return;

        // Cho phép chọn lại cùng một file sau khi upload thất bại.
        const clearSelection = () => {
            fileInput.value = '';
        };

        const extension = file.name
            .substring(file.name.lastIndexOf('.'))
            .toLowerCase();

        if (!allowedExtensions.includes(extension)) {
            Toast.info('Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.');
            clearSelection();
            return;
        }

        if (file.size <= 0 || file.size > maxFileSize) {
            Toast.info('Ảnh phải có dung lượng lớn hơn 0 và không vượt quá 2 MB.');
            clearSelection();
            return;
        }

        if (!userId) {
            Toast.info('Không xác định được tài khoản cần cập nhật.');
            clearSelection();
            return;
        }

        if (isUploading) {
            clearSelection();
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        setUploading(true);

        try {
            const response = await fetch(
                `/admin/user/${encodeURIComponent(userId)}/avatar`,
                {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken':
                            antiForgeryInput?.value || ''
                    },
                    body: formData,
                    credentials: 'same-origin'
                }
            );

            let data = {};

            try {
                data = await response.json();
            } catch {
                // Response không phải JSON.
            }

            if (!response.ok) {
                throw new Error(
                    getErrorMessage(data, 'Không thể tải ảnh đại diện lên.')
                );
            }

            if (!data.avatarUrl) {
                throw new Error('Server không trả về đường dẫn ảnh.');
            }

            // Tránh cache ảnh cũ nếu URL được tái sử dụng.
            const url = new URL(data.avatarUrl, window.location.origin);
            url.searchParams.set('_t', Date.now().toString());

            avatarImage.src = url.toString();

            Toast.success(data.message || 'Cập nhật ảnh đại diện thành công.');
        } catch (error) {
            Toast.error(error.message || 'Đã xảy ra lỗi khi upload avatar.');
        } finally {
            setUploading(false);
            clearSelection();
        }
    });
})();

function generatePassword(length = 16) {
    if (!Number.isInteger(length) || length < 12 || length > 128) {
        throw new RangeError("Password length must be between 12 and 128.");
    }

    const upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const lower = "abcdefghijkmnopqrstuvwxyz";
    const numbers = "23456789";
    const special = "!@#$%^&*";
    const groups = [upper, lower, numbers, special];
    const all = groups.join("");

    // Dùng Web Crypto API để sinh số ngẫu nhiên an toàn.
    function randomIndex(max) {
        const limit = Math.floor(0x100000000 / max) * max;
        const buffer = new Uint32Array(1);
        let value;

        do {
            crypto.getRandomValues(buffer);
            value = buffer[0];
        } while (value >= limit);

        return value % max;
    }

    const password = groups.map(
        group => group[randomIndex(group.length)]
    );

    while (password.length < length) {
        password.push(all[randomIndex(all.length)]);
    }

    // Fisher–Yates shuffle.
    for (let i = password.length - 1; i > 0; i--) {
        const j = randomIndex(i + 1);
        [password[i], password[j]] = [password[j], password[i]];
    }

    return password.join("");
}