
(function () {
    'use strict';

    function initUserPersonal(container = document) {
        const form = container.querySelector('#userDetailsForm');

        if (!form) return;

        // Tránh đăng ký submit event nhiều lần.
        if (form.dataset.initialized === 'true') return;
        form.dataset.initialized = 'true';

        form.addEventListener('submit', async function (event) {
            event.preventDefault();

            const submitButton = form.querySelector('[type="submit"]');

            if (submitButton?.disabled) return;

            const originalHtml = submitButton?.innerHTML;

            try {
                if (submitButton) {
                    submitButton.disabled = true;
                    submitButton.innerHTML = 'Đang lưu...';
                }

                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'application/json'
                    },
                    credentials: 'same-origin'
                });

                const contentType = response.headers.get('content-type') || '';

                if (response.redirected ||
                    !contentType.includes('application/json')) {
                    throw new Error(
                        'Phiên đăng nhập có thể đã hết hạn hoặc server trả về dữ liệu không hợp lệ.'
                    );
                }

                const result = await response.json();

                if (!response.ok || !result.success) {
                    throw new Error(
                        result.message || 'Không thể cập nhật thông tin.'
                    );
                }

                Toast.success(result.message || 'Cập nhật thành công.');

            } catch (error) {
                console.error('Update personal error:', error);
                Toast.error(error.message || 'Không thể cập nhật thông tin.');
            } finally {
                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.innerHTML = originalHtml;
                }
            }
        });
    }

    window.initUserPersonal = initUserPersonal;
})();
