(function () {
    'use strict';

    const root = document.getElementById('userDetails');
    const tabContent = document.getElementById('tab-content');

    if (!root || !tabContent) return;

    let currentRequest = null;
    let requestId = 0;

    function setActiveTab(tab) {
        document.querySelectorAll('[data-tab]').forEach(link => {
            const isActive = link.dataset.tab === tab;

            link.classList.toggle('active', isActive);

            if (isActive) {
                link.setAttribute('aria-current', 'page');
            } else {
                link.removeAttribute('aria-current');
            }
        });
    }

    function showLoading() {
        tabContent.innerHTML = `
            <div class="card">
                <div class="card-body text-center py-5">
                    <div class="spinner-border text-primary mb-3"
                         role="status">
                    </div>
                    <div class="text-muted">Đang tải dữ liệu...</div>
                </div>
            </div>`;
    }

    function showError(message) {
        tabContent.innerHTML = `
            <div class="alert alert-danger" role="alert">
                ${message}
            </div>`;
    }

    async function loadTab(tab, options = {}) {
        const {
            updateHistory = false
        } = options;

        const link = document.querySelector(
            `[data-ajax-tab="true"][data-tab="${tab}"]`
        );

        if (!link) return;

        const url = link.dataset.tabUrl;

        if (!url) {
            showError('Không tìm thấy địa chỉ tải nội dung tab.');
            return;
        }

        // Hủy request cũ nếu người dùng chuyển tab liên tục.
        if (currentRequest) {
            currentRequest.abort();
        }

        const controller = new AbortController();
        currentRequest = controller;

        const currentRequestId = ++requestId;

        // Dọn plugin của tab hiện tại trước khi chuyển tab.
        destroyTabPlugins();
        showLoading();

        try {
            const response = await fetch(url, {
                method: 'GET',
                credentials: 'same-origin',
                signal: controller.signal,
                headers: {
                    'Accept': 'text/html',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            // Nếu server chuyển hướng đến Login.
            if (
                response.redirected ||
                new URL(response.url).pathname.toLowerCase().includes('/auth/login')
            ) {
                window.location.assign(response.url);
                return;
            }

            if (response.status === 401 || response.status === 403) {
                throw new Error('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền truy cập.');
            }
            if (!response.ok) {
                if (response.status === 404) {
                    throw new Error('Không tìm thấy tài khoản hoặc nội dung tab.');
                }

                if (response.status === 403) {
                    throw new Error('Bạn không có quyền truy cập nội dung này.');
                }

                throw new Error('Không thể tải nội dung tab. Vui lòng thử lại.');
            }

            const contentType = response.headers.get('content-type') || '';

            if (!contentType.includes('text/html')) {
                throw new Error('Dữ liệu trả về không đúng định dạng.');
            }

            const html = await response.text();

            // Bỏ qua kết quả của request cũ.
            if (currentRequestId !== requestId) return;

            tabContent.innerHTML = html;
            if (tab === 'personal') {
                window.initAddressPicker?.(tabContent);
                window.initFlatpickr?.(tabContent);
                window.initSocialLinks?.(tabContent);
                window.initUserPersonal?.(tabContent);
            }
            if (tab === 'security') {
                window.initUserSecurity?.(tabContent);
                window.initAccountDeactivation?.(tabContent);
            }

            setActiveTab(tab);

            if (updateHistory) {
                history.pushState(
                    { tab },
                    '',
                    link.href
                );
            }

        } catch (error) {
            if (error.name === 'AbortError') return;

            if (currentRequestId !== requestId) return;

            console.error('Load user tab error:', error);

            showError(
                error.message || 'Đã xảy ra lỗi khi tải nội dung.'
            );
        } finally {
            if (currentRequestId === requestId) {
                currentRequest = null;
            }
        }
    }
    function destroyTabPlugins() {
        const inputs = tabContent.querySelectorAll('input');

        inputs.forEach(input => {
            if (input._flatpickr) {
                input._flatpickr.close();
                input._flatpickr.destroy();
            }
        });

        // Dọn calendar còn sót lại do Flatpickr tạo ra.
        document.querySelectorAll('.flatpickr-calendar').forEach(calendar => {
            calendar.remove();
        });
    }
    // Chuyển tab bằng AJAX.
    document.addEventListener('click', function (event) {
        const link = event.target.closest(
            '[data-ajax-tab="true"]'
        );

        if (!link) return;

        event.preventDefault();

        const tab = link.dataset.tab;

        if (!tab) return;

        loadTab(tab, {
            updateHistory: true
        });
    });

    // Hỗ trợ Back / Forward của trình duyệt.
    window.addEventListener('popstate', function () {
        const url = new URL(window.location.href);
        const tab = url.searchParams.get('tab') || 'personal';

        loadTab(tab);
    });

    // Tải tab ban đầu theo URL.
    const initialTab = new URL(window.location.href)
        .searchParams.get('tab') || 'personal';

    loadTab(initialTab);
})();