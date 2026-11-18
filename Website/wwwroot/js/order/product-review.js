
'use strict';

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("productReviewForm");

    if (!form) return;

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        if (!form.reportValidity()) return;

        const submitButton = form.querySelector('[type="submit"]');
        if (!submitButton || submitButton.disabled) return;

        const originalHtml = submitButton.innerHTML;

        try {
            submitButton.disabled = true;
            submitButton.innerHTML = `
                <i class="ph ph-spinner animate-spin text-lg"></i>
                Đang gửi...
            `;


            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: new FormData(form)
            });

            const result = await response.json();

            if (!response.ok || !result.success) {
                await Swal.fire({
                    icon: "error",
                    title: "Không thể gửi đánh giá",
                    text: result.message || "Đã xảy ra lỗi. Vui lòng thử lại.",
                    confirmButtonColor: "#1F9345"
                });

                return;
            }

            await Swal.fire({
                icon: "success",
                title: "Đánh giá thành công",
                text: result.message,
                confirmButtonColor: "#1F9345"
            });

            if (result.redirectUrl) {
                window.location.href = result.redirectUrl;
            }

        } catch (error) {
            console.error("Product review error:", error);

            await Swal.fire({
                icon: "error",
                title: "Lỗi kết nối",
                text: "Không thể gửi đánh giá. Vui lòng thử lại.",
                confirmButtonColor: "#1F9345"
            });
        } finally {
            submitButton.disabled = false;
            submitButton.innerHTML = originalHtml;
        }
    });
});
