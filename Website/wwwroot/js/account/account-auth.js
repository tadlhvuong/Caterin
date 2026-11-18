
"use strict";

(function () {
    const SELECTORS = {
        form: "[data-auth-form]",
        ajaxForm: '[data-auth-form][data-ajax="true"]',
        submit: 'button[type="submit"]',
        summary: '[data-valmsg-summary="true"]',
        fieldError: '[data-valmsg-for]'
    };

    const DEFAULT_MESSAGES = {
        network: "Không thể kết nối đến máy chủ. Vui lòng thử lại.",
        server: "Đã xảy ra lỗi. Vui lòng thử lại.",
        session: "Phiên làm việc đã hết hạn. Vui lòng tải lại trang.",
        success: "Thao tác thành công."
    };

    function getSubmitButton(form) {
        return form.querySelector(SELECTORS.submit);
    }

    function setLoading(form, loading) {
        const button = getSubmitButton(form);

        if (!button) return;

        if (loading) {
            if (!button.dataset.originalHtml) {
                button.dataset.originalHtml = button.innerHTML;
            }

            button.disabled = true;
            button.innerHTML = `
                <span class="spinner-border spinner-border-sm me-8"
                      role="status"
                      aria-hidden="true"></span>
                Đang xử lý...
            `;
        } else {
            button.disabled = false;

            if (button.dataset.originalHtml) {
                button.innerHTML = button.dataset.originalHtml;
            }
        }
    }

    function clearErrors(form) {
        const summary = form.querySelector(SELECTORS.summary);

        if (summary) {
            summary.innerHTML = "";
            summary.classList.add("d-none");
        }

        form.querySelectorAll(SELECTORS.fieldError)
            .forEach(element => {
                element.textContent = "";
            });

        form.querySelectorAll(".is-invalid")
            .forEach(element => {
                element.classList.remove("is-invalid");
            });
    }

    function showSummary(form, message) {
        const summary = form.querySelector(SELECTORS.summary);

        if (!summary) {
            showMessage(message);
            return;
        }

        summary.innerHTML = "";

        const list = document.createElement("ul");
        list.className = "mb-0";

        const item = document.createElement("li");
        item.textContent = message;

        list.appendChild(item);
        summary.appendChild(list);

        summary.classList.remove("d-none");
    }

    function showFieldErrors(form, errors) {
        let hasGlobalError = false;

        Object.entries(errors || {}).forEach(([key, messages]) => {
            const message = Array.isArray(messages)
                ? messages[0]
                : messages;

            if (!message) return;

            if (!key || key === "") {
                showSummary(form, message);
                hasGlobalError = true;
                return;
            }

            const normalizedKey = key.toLowerCase();

            const field = Array.from(form.elements).find(element =>
                element.name &&
                element.name.toLowerCase() === normalizedKey
            );

            const errorElement = Array.from(
                form.querySelectorAll(SELECTORS.fieldError)
            ).find(element =>
                element.getAttribute("data-valmsg-for")
                    ?.toLowerCase() === normalizedKey
            );

            if (field) {
                field.classList.add("is-invalid");
            }

            if (errorElement) {
                errorElement.textContent = message;
            } else {
                showSummary(form, message);
                hasGlobalError = true;
            }
        });

        return hasGlobalError;
    }

    function showMessage(message) {
        if (typeof Swal !== "undefined") {
            Swal.fire({
                icon: "error",
                title: "Thông báo",
                text: message || DEFAULT_MESSAGES.server,
                confirmButtonText: "Đóng",
                confirmButtonColor: "#1F9345"
            });
        } else {
            alert(message || DEFAULT_MESSAGES.server);
        }
    }

    function showSuccess(message) {
        if (typeof Swal !== "undefined") {
            return Swal.fire({
                icon: "success",
                title: "Thành công",
                text: message || DEFAULT_MESSAGES.success,
                timer: 1200,
                showConfirmButton: false
            });
        }

        return Promise.resolve();
    }

    function redirect(url) {
        if (!url) return;

        window.location.assign(url);
    }

    async function handleResponse(form, response) {
        const contentType =
            response.headers.get("content-type") || "";

        // Xử lý trường hợp middleware trả HTML redirect
        // thay vì JSON.
        if (!contentType.includes("application/json")) {
            if (response.redirected) {
                redirect(response.url);
                return;
            }

            throw new Error(DEFAULT_MESSAGES.server);
        }

        const result = await response.json();

        if (response.status === 401) {
            showMessage(
                result.message || DEFAULT_MESSAGES.session
            );

            return;
        }

        if (response.status === 403) {
            showMessage(
                result.message || "Bạn không có quyền thực hiện thao tác này."
            );

            return;
        }

        if (!response.ok || !result.success) {
            clearErrors(form);

            if (result.errors) {
                showFieldErrors(form, result.errors);
            }

            if (!result.errors ||
                Object.keys(result.errors).length === 0) {
                showSummary(
                    form,
                    result.message || DEFAULT_MESSAGES.server
                );
            }

            return;
        }

        if (result.redirectUrl) {
            if (result.message) {
                await showSuccess(result.message);
            }

            redirect(result.redirectUrl);
        }
    }

    async function submitAjax(form) {
        if (form.dataset.submitting === "true") return;

        form.dataset.submitting = "true";

        clearErrors(form);
        setLoading(form, true);

        try {
            const formData = new FormData(form);

            const response = await fetch(form.action, {
                method: (form.method || "POST").toUpperCase(),
                body: formData,
                credentials: "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "application/json"
                },
                redirect: "follow"
            });

            await handleResponse(form, response);
        } catch (error) {
            console.error("[AccountAuth]", error);

            showMessage(
                error.message === DEFAULT_MESSAGES.server
                    ? error.message
                    : DEFAULT_MESSAGES.network
            );
        } finally {
            form.dataset.submitting = "false";
            setLoading(form, false);
        }
    }
    // HIDE/SHOW PASSWORD INPUT
    document.addEventListener("click", function (e) {
        const button = e.target.closest("[data-target]");

        if (!button || !button.hasAttribute("aria-pressed")) return;

        const target = document.querySelector(button.dataset.target);

        if (!target) return;

        const isPassword = target.type === "password";

        target.type = isPassword ? "text" : "password";

        button.setAttribute("aria-pressed", String(isPassword));
        button.setAttribute(
            "aria-label",
            isPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"
        );

        const icon = button.querySelector("i");

        if (icon) {
            icon.classList.toggle("ph-eye", isPassword);
            icon.classList.toggle("ph-eye-slash", !isPassword);
        }
    });
    // AJAX Login / Register
    document.addEventListener("submit", function (event) {
        const form = event.target.closest(SELECTORS.ajaxForm);

        if (!form) return;

        event.preventDefault();

        submitAjax(form);
    });

    // External Login:
    // Giữ nguyên browser navigation để OAuth Challenge hoạt động.
    document.addEventListener("submit", function (event) {
        const form = event.target.closest(
            '[data-auth-form][data-ajax="false"]'
        );

        if (!form) return;

        const button = getSubmitButton(form);

        if (!button || button.disabled) {
            event.preventDefault();
            return;
        }

        setLoading(form, true);
    });

})();
