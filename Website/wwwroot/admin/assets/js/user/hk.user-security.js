
(function () {
    'use strict';

    // ============================================
    // ACCOUNT DEACTIVATION
    // ============================================

    function initAccountDeactivation(container = document) {
        const form = container.querySelector('#formAccountDeactivation');
        const checkbox = container.querySelector('#accountActivation');

        if (!form || !checkbox || form.dataset.initialized === 'true') {
            return;
        }

        const submitButton = form.querySelector('.deactivate-account');

        if (!submitButton) return;

        form.dataset.initialized = 'true';

        const userId =
            container.querySelector('#userDetails')?.dataset.userId ||
            document.getElementById('userDetails')?.dataset.userId;

        const antiForgeryToken = form.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value || container.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value;

        checkbox.checked = false;
        submitButton.disabled = true;

        checkbox.addEventListener('change', function () {
            submitButton.disabled = !checkbox.checked;
        });

        form.addEventListener('submit', async function (event) {
            event.preventDefault();

            if (!checkbox.checked || submitButton.disabled) return;

            if (!userId) {
                Toast.warning('Không xác định được tài khoản cần vô hiệu hóa.');
                return;
            }

            submitButton.disabled = true;
            checkbox.disabled = true;

            const originalText = submitButton.textContent.trim();
            submitButton.textContent = 'Processing...';

            try {
                const response = await fetch(
                    `/admin/user/${encodeURIComponent(userId)}/deactivate`,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': antiForgeryToken || ''
                        },
                        credentials: 'same-origin'
                    }
                );

                let data = {};

                try {
                    data = await response.json();
                } catch {
                    // Response không phải JSON.
                }

                if (!response.ok || !data.success) {
                    throw new Error(
                        data.message || 'Không thể vô hiệu hóa tài khoản.'
                    );
                }

                await Swal.fire({
                    icon: 'success',
                    title: 'Thành công',
                    text: data.message || 'Đã vô hiệu hóa tài khoản thành công.',
                    timer: 1500,
                    timerProgressBar: true,
                    showConfirmButton: false,
                    allowOutsideClick: false
                });

                window.location.href = '/admin/user';

            } catch (error) {
                Toast.error(
                    error.message || 'Đã xảy ra lỗi khi vô hiệu hóa tài khoản.'
                );

                checkbox.disabled = false;
                submitButton.disabled = !checkbox.checked;
                submitButton.textContent = originalText;
            }
        });
    }

    // ============================================
    // USER SECURITY - DEVICE SESSIONS
    // ============================================

    function initUserSecurity(container = document) {

        // Change Password
        initChangePassword(container);

        const section = container.querySelector('#userDevicesSection');

        if (!section || section.dataset.initialized === 'true') {
            return;
        }

        const userId = section.dataset.userId;

        const sessionsUrl =
            `/admin/user/${encodeURIComponent(userId)}/sessions`;

        const token = section.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value;

        const recentList = section.querySelector('#recentDevicesList');
        const viewAllButton = section.querySelector('#btnViewAllDevices');
        const allDevicesModal = container.querySelector('#allDevicesModal');
        const allDevicesList = container.querySelector('#allDevicesList');
        const allDevicesCount = container.querySelector('#allDevicesCount');
        const revokeAllButton = container.querySelector('#btnRevokeAllSessions');
        if (!userId || !recentList || !allDevicesList) {
            return;
        }


        // Đánh dấu sau khi xác định đủ DOM cần thiết.
        section.dataset.initialized = 'true';

        let sessions = [];
        let loading = false;

        // --------------------------------------------
        // Helpers
        // --------------------------------------------

        function escapeHtml(value) {
            return String(value ?? '').replace(/[&<>"']/g, char => ({
                '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
            })[char]);
        }

        function formatDate(value) {
            if (!value) return 'Chưa có dữ liệu';

            const date = new Date(value);

            if (Number.isNaN(date.getTime())) {
                return 'Chưa có dữ liệu';
            }

            return date.toLocaleString('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        }

        function getDeviceInfo(session) {
            const name = session.deviceName || 'Thiết bị không xác định';
            const value = name.toLowerCase();

            let icon = 'ti-device-desktop';

            if (/android|iphone|ipad|mobile/.test(value)) {
                icon = 'ti-device-mobile';
            } else if (/macintosh|mac os|laptop/.test(value)) {
                icon = 'ti-device-laptop';
            }

            return { name, icon };
        }


        // --------------------------------------------
        // Render recent devices
        // --------------------------------------------

        function renderRecentDevices() {
            if (!sessions.length) {
                recentList.innerHTML = `
                    <div class="text-muted py-3">
                        No devices are currently signed in.
                    </div>`;

                if (viewAllButton) {
                    viewAllButton.hidden = true;
                }

                return;
            }

            const recentSessions = sessions.slice(0, 2);

            recentList.innerHTML = recentSessions.map((session, index) => {
                const device = getDeviceInfo(session);

                return `
                    <div class="device-session-item d-flex align-items-start gap-3 py-3
                                ${index < recentSessions.length - 1 ? 'border-bottom' : ''}">

                        <div class="device-session-icon">
                            <i class="ti ${device.icon}"></i>
                        </div>

                        <div class="flex-grow-1 min-w-0">
                            <div class="d-flex flex-wrap align-items-center gap-2 mb-1">
                                <h6 class="mb-0">
                                    ${escapeHtml(device.name)}
                                </h6>

                                ${session.isCurrent
                        ? '<span class="badge bg-label-success">Current device</span>'
                        : ''}
                            </div>

                            <div class="text-muted small mb-1">
                                IP: ${escapeHtml(session.ipAddress || 'Unavailable')}
                            </div>

                            <div class="text-muted small">
                                Last active: ${escapeHtml(formatDate(session.lastActivityAt))}
                            </div>
                        </div>
                    </div>`;
            }).join('');

            if (viewAllButton) {
                viewAllButton.hidden = sessions.length <= 2;
            }
        }


        // --------------------------------------------
        // Render all devices
        // --------------------------------------------

        function renderAllDevices() {
            if (allDevicesCount) {
                allDevicesCount.textContent = `${sessions.length} sessions`;
            }

            if (!sessions.length) {
                allDevicesList.innerHTML = `
                    <div class="text-muted p-4">
                        No devices are currently signed in.
                    </div>`;

                return;
            }

            allDevicesList.innerHTML = sessions.map(session => {
                const device = getDeviceInfo(session);

                return `
                    <div class="device-session-row d-flex align-items-start gap-3
                                p-4 border-bottom">

                        <div class="device-session-icon">
                            <i class="ti ${device.icon}"></i>
                        </div>

                        <div class="flex-grow-1 min-w-0">
                            <div class="d-flex flex-wrap align-items-center gap-2 mb-2">
                                <h6 class="mb-0">
                                    ${escapeHtml(device.name)}
                                </h6>

                                ${session.isCurrent
                        ? '<span class="badge bg-label-success">Current device</span>'
                        : ''}
                            </div>

                            <div class="device-session-meta">
                                <i class="ti ti-world"></i>
                                <span>
                                    ${escapeHtml(session.ipAddress || 'IP unavailable')}
                                </span>
                            </div>

                            <div class="device-session-meta">
                                <i class="ti ti-clock"></i>
                                <span>
                                    Last active: ${escapeHtml(formatDate(session.lastActivityAt))}
                                </span>
                            </div>
                        </div>

                        ${!session.isCurrent ? `
                            <button type="button"
                                    class="btn btn-sm btn-outline-danger flex-shrink-0"
                                    data-session-id="${escapeHtml(session.id)}"
                                    data-device-name="${escapeHtml(device.name)}"
                                    data-action="revoke-session">

                                <i class="ti ti-logout me-sm-1"></i>

                                <span class="d-none d-sm-inline">
                                    Log out
                                </span>
                            </button>
                        ` : ''}
                    </div>`;
            }).join('');
        }


        // --------------------------------------------
        // Load sessions
        // --------------------------------------------

        async function loadSessions() {
            if (loading) return;

            loading = true;

            recentList.innerHTML = `
        <div class="text-muted p-3">
            Đang tải thiết bị...
        </div>`;

            try {
                const response = await fetch(sessionsUrl, {
                    method: 'GET',
                    credentials: 'same-origin',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                const result = await response.json().catch(() => null);

                if (!response.ok || !result) {
                    throw new Error(
                        result?.message || 'Không thể tải danh sách thiết bị.'
                    );
                }

                // Hỗ trợ cả mảng trực tiếp và response có data/sessions.
                const data = Array.isArray(result)
                    ? result
                    : Array.isArray(result.data)
                        ? result.data
                        : Array.isArray(result.sessions)
                            ? result.sessions
                            : Array.isArray(result.data?.sessions)
                                ? result.data.sessions
                                : null;

                if (!data) {
                    console.error('Invalid sessions response:', result);
                    throw new Error('Dữ liệu thiết bị không hợp lệ.');
                }

                sessions = data;

                renderRecentDevices();
                renderAllDevices();

                if (revokeAllButton) {
                    revokeAllButton.disabled = sessions.length === 0;
                }

            } catch (error) {
                sessions = [];

                recentList.innerHTML = `
            <div class="text-danger p-3">
                Không thể tải thiết bị.
            </div>`;

                allDevicesList.innerHTML = `
            <div class="text-danger p-3">
                Không thể tải danh sách thiết bị.
            </div>`;

                if (revokeAllButton) {
                    revokeAllButton.disabled = true;
                }

                console.error('Load sessions failed:', error);
                Toast.error(error.message || 'Có lỗi xảy ra.');

            } finally {
                loading = false;
            }
        }


        // --------------------------------------------
        // Revoke session
        // --------------------------------------------

        async function revokeSession(sessionId) {
            const session = sessions.find(
                x => String(x.id) === String(sessionId)
            );

            if (!session) {
                Toast.warning('Không tìm thấy phiên đăng nhập.');
                return;
            }

            const confirmed = typeof Swal !== 'undefined'
                ? await Swal.fire({
                    title: 'Đăng xuất thiết bị?',
                    text: 'Thiết bị này sẽ phải đăng nhập lại để tiếp tục sử dụng tài khoản.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Đăng xuất',
                    cancelButtonText: 'Hủy',
                    reverseButtons: true
                }).then(result => result.isConfirmed)
                : confirm('Bạn có chắc muốn đăng xuất thiết bị này?');

            if (!confirmed) return;

            const button = allDevicesList.querySelector(
                `[data-action="revoke-session"][data-session-id="${CSS.escape(String(sessionId))}"]`
            );

            if (button) {
                button.disabled = true;
                button.textContent = 'Đang xử lý...';
            }

            try {
                const response = await fetch(
                    `${sessionsUrl}/${encodeURIComponent(sessionId)}/revoke`,
                    {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: {
                            'Accept': 'application/json',
                            ...(token
                                ? { 'RequestVerificationToken': token }
                                : {})
                        }
                    }
                );

                const result = await response.json().catch(() => null);

                if (!response.ok || result?.success === false) {
                    throw new Error(
                        result?.message || 'Không thể đăng xuất thiết bị.'
                    );
                }

                Toast.success(
                    result?.message || 'Đã đăng xuất thiết bị.',
                    true
                );

                await loadSessions();

                if (session.isCurrent) {
                    window.location.assign('/auth/login');
                }

            } catch (error) {
                Toast.error(error.message || 'Có lỗi xảy ra.');
                await loadSessions();
            }
        }

        async function revokeAllSessions() {
            if (!sessions.length) {
                Toast.warning('Không có thiết bị nào cần đăng xuất.');
                return;
            }

            const confirmed = await Swal.fire({
                title: 'Đăng xuất tất cả thiết bị?',
                text: 'Tất cả thiết bị đang đăng nhập tài khoản này sẽ phải đăng nhập lại.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Đăng xuất tất cả',
                cancelButtonText: 'Hủy',
                confirmButtonColor: '#d33',
                reverseButtons: true,
                focusCancel: true
            });

            if (!confirmed.isConfirmed) return;

            const originalContent = revokeAllButton?.innerHTML;

            if (revokeAllButton) {
                revokeAllButton.disabled = true;
                revokeAllButton.innerHTML = `
            <span class="spinner-border spinner-border-sm me-1"></span>
            Đang xử lý...`;
            }

            try {
                const response = await fetch(
                    `${sessionsUrl}/revoke-all`,
                    {
                        method: 'POST',
                        credentials: 'same-origin',
                        headers: {
                            'Accept': 'application/json',
                            ...(token
                                ? { 'RequestVerificationToken': token }
                                : {})
                        }
                    }
                );

                const result = await response.json().catch(() => null);

                if (!response.ok || result?.success === false) {
                    throw new Error(
                        result?.message || 'Không thể đăng xuất tất cả thiết bị.'
                    );
                }

                await Swal.fire({
                    icon: 'success',
                    title: 'Thành công',
                    text: result?.message || 'Đã đăng xuất tất cả thiết bị.',
                    timer: 1500,
                    timerProgressBar: true,
                    showConfirmButton: false
                });

                await loadSessions();

            } catch (error) {
                Toast.error(
                    error.message || 'Có lỗi khi đăng xuất tất cả thiết bị.'
                );
            } finally {
                if (revokeAllButton) {
                    revokeAllButton.innerHTML = originalContent;
                    revokeAllButton.disabled = sessions.length === 0;
                }
            }
        }
        // --------------------------------------------
        // Events
        // --------------------------------------------

        allDevicesList.addEventListener('click', event => {
            const button = event.target.closest(
                '[data-action="revoke-session"]'
            );

            if (!button || !allDevicesList.contains(button)) return;

            revokeSession(button.dataset.sessionId);
        });

        if (revokeAllButton) {
            revokeAllButton.addEventListener('click', revokeAllSessions);
        }

        if (allDevicesModal && viewAllButton) {
            allDevicesModal.addEventListener('show.bs.modal', () => {
                loadSessions();
            });
        }

        // Load dữ liệu lần đầu khi tab Security được AJAX tải.
        loadSessions();
    }

    // ============================================
    // CHANGE PASSWORD
    // ============================================

    function initChangePassword(container = document) {
        const form = container.querySelector('#formChangePassword');
        const generateButton = container.querySelector('#btnGeneratePassword');

        if (!form || form.dataset.initialized === 'true') return;

        const $form = $(form);
        const passwordInput = form.querySelector('#newPassword');
        const confirmInput = form.querySelector('#confirmPassword');

        passwordInput.setAttribute('autocomplete', 'new-password');
        confirmInput.setAttribute('autocomplete', 'new-password');

        passwordInput.value = '';
        confirmInput.value = '';
        // Khởi tạo validation một lần.
        $form.validate({
            validClass: 'is-valid',
            errorClass: 'is-invalid',

            rules: {
                newPassword: {
                    required: true,
                    minlength: 12
                },
                confirmPassword: {
                    required: true,
                    equalTo: '#newPassword'
                }
            },

            errorElement: 'div',

            errorPlacement: function (error, element) {
                error.addClass('invalid-feedback');

                if (element.parent('.input-group').length) {
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                }
            },

            highlight: function (element) {
                $(element)
                    .removeClass('is-valid')
                    .addClass('is-invalid');
            },

            unhighlight: function (element) {
                $(element)
                    .removeClass('is-invalid')
                    .addClass('is-valid');
            }
        });

        form.dataset.initialized = 'true';
        window.Helpers?.initPasswordToggle?.();
        // Generate password
        if (generateButton) {
            generateButton.addEventListener('click', function () {
                const password = generatePassword();

                form.querySelector('#newPassword').value = password;
                form.querySelector('#confirmPassword').value = password;

                // Cập nhật trạng thái validation sau khi điền mật khẩu.
                $form.valid();
            });
        }

        // Submit password
        form.addEventListener('submit', async function (event) {
            event.preventDefault();

            if (!$form.valid()) return;

            const confirmed = await Swal.fire({
                icon: 'warning',
                title: 'Xác nhận đặt lại mật khẩu?',
                text: 'Bạn có chắc chắn muốn đặt lại mật khẩu cho tài khoản này không?',
                showCancelButton: true,
                confirmButtonText: 'Xác nhận',
                cancelButtonText: 'Hủy',
                reverseButtons: true,
                focusCancel: true
            });

            if (!confirmed.isConfirmed) return;

            const submitButton = form.querySelector('[type="submit"]');
            const originalContent = submitButton?.innerHTML;

            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML =
                    '<span class="spinner-border spinner-border-sm me-1"></span> Đang xử lý...';
            }

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'Accept': 'application/json'
                    },
                    body: new URLSearchParams(new FormData(form))
                });

                const data = await response.json().catch(() => null);

                if (!response.ok || !data?.success) {
                    throw new Error(
                        data?.message || 'Không thể đặt lại mật khẩu.'
                    );
                }

                await Swal.fire({
                    icon: 'success',
                    title: 'Thành công',
                    text: data.message || 'Đặt lại mật khẩu thành công.'
                });

                form.reset();
                $form.validate().resetForm();

                $form.find('.is-valid, .is-invalid')
                    .removeClass('is-valid is-invalid');

            } catch (error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi',
                    text: error.message || 'Đã xảy ra lỗi khi đặt lại mật khẩu.'
                });
            } finally {
                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.innerHTML = originalContent;
                }
            }
        });
    }

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
    // ============================================
    // EXPORT INITIALIZERS
    // ============================================

    window.initAccountDeactivation = initAccountDeactivation;
    window.initUserSecurity = initUserSecurity;
})();
