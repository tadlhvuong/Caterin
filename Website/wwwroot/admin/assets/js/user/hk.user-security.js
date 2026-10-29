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
            showMessage('Không xác định được tài khoản cần vô hiệu hóa.');
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
})();