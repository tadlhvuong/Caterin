
'use strict';

// Logout.
document.querySelector('#logout-form')
    ?.addEventListener('submit', async function (e) {
        e.preventDefault();

        const form = this;

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(
                    'Không thể đăng xuất. Vui lòng thử lại.'
                );
            }

            const result = await response.json();

            if (result.success) {
                window.dispatchEvent(
                    new CustomEvent('caterin:customer-logout')
                );

                window.location.href = result.returnUrl;
            }
        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Thông báo',
                text: error.message ||
                    'Đã xảy ra lỗi khi đăng xuất.',
                confirmButtonText: 'Đóng'
            });
        }
    });
