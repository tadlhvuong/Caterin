(function () {
    'use strict';
    const dateInput = document.querySelector('#dateOfBirth');

    if (dateInput) {
        flatpickr(dateInput, {
            dateFormat: 'Y-m-d'
        });
    }

    const form = document.getElementById('userDetailsForm');
    if (!form) return;

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
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });

            const contentType = response.headers.get('content-type') || '';

            if (response.redirected || !contentType.includes('application/json')) {
                throw new Error(
                    'Server chuyển hướng hoặc không trả JSON. Hãy kiểm tra phiên đăng nhập và quyền truy cập.'
                );
            }

            const result = await response.json();
            console.log(result);
            if (!response.ok || !result.success) {
                throw new Error(result.message || 'Không thể cập nhật thông tin.');
            }
            Toast.success(result.message);
        } catch (error) {
            Toast.error('Không thể cập nhật thông tin.');
        } finally {
            if (submitButton) {
                submitButton.disabled = false;
                submitButton.innerHTML = originalHtml;
            }
        }
    });
})();