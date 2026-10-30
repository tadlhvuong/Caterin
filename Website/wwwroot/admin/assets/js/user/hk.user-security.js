(function () {
    'use strict';
    document.addEventListener('htmx:beforeSwap', function (event) {
        const target = event.detail.target;
        if (!target) return;

        target.querySelectorAll('[data-flatpickr]').forEach(input => {
            if (input._flatpickr) {
                input._flatpickr.destroy();
            }
        });
    });
    const form = document.getElementById('formAccountDeactivation');
    const checkbox = document.getElementById('accountActivation');
    const submitButton = form?.querySelector('.deactivate-account');

    if (!form || !checkbox || !submitButton) return;

    const userId = document.getElementById('userDetails')?.dataset.userId;
    const antiForgeryToken = document.querySelector(
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
            Toast.success(error.message || 'Đã xảy ra lỗi khi vô hiệu hóa tài khoản.');
            checkbox.disabled = false;
            submitButton.disabled = !checkbox.checked;
            submitButton.textContent = originalText;
        }
    });


    ///USER SESSION
    function initUserSecurity() {
        const section = document.getElementById('userDevicesSection');

        if (!section) return;

        const userId = section.dataset.userId;

        const sessionsUrl =
            `/admin/user/${encodeURIComponent(userId)}/sessions`;

        const token = section.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value;

        const recentList = document.getElementById('recentDevicesList');
        const viewAllButton = document.getElementById('btnViewAllDevices');
        const allDevicesModal = document.getElementById('allDevicesModal');
        const allDevicesList = document.getElementById('allDevicesList');
        const allDevicesCount = document.getElementById('allDevicesCount');

        if (!userId || !recentList || !allDevicesList) return;

        let sessions = [];
        let loading = false;

        function escapeHtml(value) {
            return String(value ?? '').replace(/[&<>"']/g, char => ({
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;'
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

        async function loadSessions() {
            if (loading) return;

            loading = true;
            recentList.innerHTML =
                '<div class="text-muted p-3">Đang tải thiết bị...</div>';

            try {
                const response = await fetch(sessionsUrl, {
                    method: 'GET',
                    credentials: 'same-origin',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error('Không thể tải danh sách thiết bị.');
                }

                sessions = await response.json();
                console.log('API sessions:', sessions);
                console.log('DeviceName:', sessions[0]?.deviceName);
                console.log('DeviceName PascalCase:', sessions[0]?.DeviceName);
                if (!Array.isArray(sessions)) {
                    throw new Error('Dữ liệu thiết bị không hợp lệ.');
                }

                renderRecentDevices();
                renderAllDevices();
            } catch (error) {
                recentList.innerHTML =
                    '<div class="text-danger p-3">Không thể tải thiết bị.</div>';

                allDevicesList.innerHTML =
                    '<div class="text-danger p-3">Không thể tải danh sách thiết bị.</div>';

                Toast.error(error.message || 'Có lỗi xảy ra.');
            } finally {
                loading = false;
            }
        }

        function getDeviceInfo(session) {
            // Tạm sử dụng DeviceName/UserAgent từ API.
            // Có thể bổ sung parser để tách OS và browser sau.
            console.log(session);
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

        function renderRecentDevices() {
            const button = document.getElementById('btnViewAllDevices');

            if (!sessions.length) {
                recentList.innerHTML = `
            <div class="text-muted py-3">
                No devices are currently signed in.
            </div>`;

                if (button) button.hidden = true;
                return;
            }

            recentList.innerHTML = sessions.slice(0, 2).map(session => {
                const device = getDeviceInfo(session);
                return `
            <div class="device-session-item d-flex align-items-start gap-3 py-3
                        ${session !== sessions.slice(0, 2).at(-1)
                        ? 'border-bottom' : ''}">
                <div class="device-session-icon">
                    <i class="ti ${device.icon}"></i>
                </div>

                <div class="flex-grow-1 min-w-0">
                    <div class="d-flex flex-wrap align-items-center gap-2 mb-1">
                        <h6 class="mb-0">${escapeHtml(device.name)}</h6>
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

            if (button) button.hidden = sessions.length <= 2;
        }

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
                        <h6 class="mb-0">${escapeHtml(device.name)}</h6>
                        ${session.isCurrent
                        ? '<span class="badge bg-label-success">Current device</span>'
                        : ''}
                    </div>

                    <div class="device-session-meta">
                        <i class="ti ti-world"></i>
                        <span>${escapeHtml(session.ipAddress || 'IP unavailable')}</span>
                    </div>

                    <div class="device-session-meta">
                        <i class="ti ti-clock"></i>
                        <span>Last active: ${escapeHtml(formatDate(session.lastActivityAt))}</span>
                    </div>
                </div>

                ${!session.isCurrent ? `
                    <button type="button"
                            class="btn btn-sm btn-outline-danger flex-shrink-0"
                            data-session-id="${escapeHtml(session.id)}"
                            data-device-name="${escapeHtml(device.name)}"
                            data-action="revoke-session">
                        <i class="ti ti-logout me-sm-1"></i>
                        <span class="d-none d-sm-inline">Log out</span>
                    </button>` : ''}
            </div>`;
            }).join('');
        }


        async function revokeSession(sessionId) {
            const session = sessions.find(x => String(x.id) === String(sessionId));

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

                if (!response.ok) {
                    throw new Error(
                        result?.message || 'Không thể đăng xuất thiết bị.'
                    );
                }

                Toast.success(
                    result?.message || 'Đã đăng xuất thiết bị.',
                    true
                );

                // Tải lại cả Recent Devices và All Devices.
                await loadSessions();

                // Nếu thu hồi chính session đang sử dụng, chuyển về trang đăng nhập.
                if (session.isCurrent) {
                    window.location.assign('/auth/login');
                }
            } catch (error) {
                Toast.error(error.message || 'Có lỗi xảy ra.');
                await loadSessions();
            }
        }

        allDevicesList.addEventListener('click', event => {
            const button = event.target.closest(
                '[data-action="revoke-session"]'
            );

            if (!button) return;

            revokeSession(button.dataset.sessionId);
        });

        if (viewAllButton && allDevicesModal) {
            allDevicesModal.addEventListener('show.bs.modal', () => {
                loadSessions();
            });
        }

        // Tải dữ liệu thật khi phần giao diện được khởi tạo.
        loadSessions();
    }
    // Khi trang đã tải xong
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initUserSecurity);
    } else {
        initUserSecurity();
    }

    // Khi HTMX tải xong nội dung tab
    document.addEventListener('htmx:afterSwap', initUserSecurity);
})();