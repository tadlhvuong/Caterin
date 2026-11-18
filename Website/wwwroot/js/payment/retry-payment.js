
'use strict';

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("retryPaymentForm");

    if (!form) return;

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        if (!form.reportValidity()) return;

        const submitButton = form.querySelector(
            'button[type="submit"]'
        );

        if (!submitButton || submitButton.disabled) return;

        const originalHtml = submitButton.innerHTML;

        try {
            submitButton.disabled = true;
            submitButton.innerHTML = `
                <i class="ph ph-spinner animate-spin text-lg"></i>
                Đang xử lý...
            `;

            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: new FormData(form)
            });

            const contentType = response.headers.get("content-type");

            if (!contentType?.includes("application/json")) {
                throw new Error("Phản hồi từ máy chủ không hợp lệ.");
            }

            const result = await response.json();

            if (!response.ok || !result.success) {
                await Swal.fire({
                    icon: "error",
                    title: "Không thể thanh toán",
                    text: result.message || "Đã xảy ra lỗi. Vui lòng thử lại.",
                    confirmButtonColor: "#1F9345"
                });

                return;
            }

            await Swal.fire({
                icon: "success",
                title: "Đã tạo yêu cầu thanh toán",
                text: result.message || "Vui lòng chờ xác nhận giao dịch.",
                confirmButtonColor: "#1F9345"
            });

            if (result.redirectUrl) {
                window.location.href = result.redirectUrl;
            }

        } catch (error) {
            console.error("Retry payment error:", error);

            await Swal.fire({
                icon: "error",
                title: "Lỗi kết nối",
                text: "Không thể kết nối đến máy chủ. Vui lòng thử lại.",
                confirmButtonColor: "#1F9345"
            });

        } finally {
            submitButton.disabled = false;
            submitButton.innerHTML = originalHtml;
        }
    });
});
