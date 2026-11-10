(() => {
    const content = document.getElementById('profile-content');

    if (!content) return;

    let currentRequest = null;
    let isRedirectingToLogin = false;

    const getTabButton = tab =>
        document.querySelector(`.account-menu[data-tab="${tab}"]`);

    const getTabFromUrl = () =>
        new URL(window.location.href).searchParams.get('tab') || 'personal';

    function setActiveTab(tab) {
        document.querySelectorAll('.account-menu[data-tab]').forEach(button => {
            button.classList.toggle('active', button.dataset.tab === tab);
        });
    }

    function showMessage(icon, title, text) {
        if (window.Swal) {
            return Swal.fire({
                icon,
                title,
                text,
                confirmButtonText: 'Đóng'
            });
        }

        alert(text);
    }

    function redirectToLogin() {
        if (isRedirectingToLogin) return;

        isRedirectingToLogin = true;

        showMessage(
            'warning',
            'Phiên đăng nhập đã hết hạn',
            'Vui lòng đăng nhập lại để tiếp tục sử dụng.'
        ).then(() => {
            window.location.href = '/account/login';
        });
    }

    async function handleResponse(response) {
        const authStatus = response.headers.get('X-Auth-Status');

        if (
            response.status === 401 ||
            authStatus === 'Expired' ||
            (response.redirected && response.url.includes('/account/login'))
        ) {
            redirectToLogin();
            return false;
        }

        if (response.status === 403) {
            await showMessage(
                'error',
                'Không có quyền truy cập',
                'Bạn không có quyền thực hiện thao tác này.'
            );
            return false;
        }

        if (response.status === 404) {
            await showMessage(
                'error',
                'Không tìm thấy nội dung',
                'Nội dung bạn yêu cầu không tồn tại hoặc đã bị thay đổi.'
            );
            return false;
        }

        if (!response.ok) {
            await showMessage(
                'error',
                'Có lỗi xảy ra',
                'Không thể xử lý yêu cầu. Vui lòng thử lại sau.'
            );
            return false;
        }

        return true;
    }

    function updateUrl(tab, pushState = true) {
        const url = new URL(window.location.href);
        url.searchParams.set('tab', tab);

        if (pushState) {
            window.history.pushState({ tab }, '', url);
        } else {
            window.history.replaceState({ tab }, '', url);
        }
    }

    async function loadProfileTab(tab, options = {}) {
        const {
            pushState = true,
            url: customUrl = null
        } = options;

        const button = getTabButton(tab);
        const url = customUrl || button?.dataset.url;

        if (!url) {
            await showMessage(
                'error',
                'Không thể tải nội dung',
                'Không tìm thấy đường dẫn của tab này.'
            );
            return;
        }

        // Không tải lại nếu tab hiện tại đã được tải thành công.
        if (
            content.dataset.currentTab === tab &&
            content.innerHTML.trim() !== ''
        ) {
            setActiveTab(tab);

            if (pushState) {
                updateUrl(tab);
            }

            return;
        }

        // Hủy request tải tab trước đó nếu người dùng chuyển tab liên tục.
        currentRequest?.abort();

        const controller = new AbortController();
        currentRequest = controller;

        content.setAttribute('aria-busy', 'true');

        try {
            const response = await fetch(url, {
                method: 'GET',
                credentials: 'same-origin',
                signal: controller.signal,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const valid = await handleResponse(response);

            if (!valid) return;

            const html = await response.text();

            if (!html.trim()) {
                throw new Error('Máy chủ không trả về nội dung hợp lệ.');
            }

            content.innerHTML = html;
            content.dataset.currentTab = tab;

            setActiveTab(tab);

            if (pushState) {
                updateUrl(tab);
            }

            initializeProfileForm(content);
        } catch (error) {
            if (error.name === 'AbortError') return;

            console.error('Profile tab error:', error);

            await showMessage(
                'error',
                'Không thể tải nội dung',
                error.message || 'Vui lòng kiểm tra kết nối và thử lại.'
            );
        } finally {
            if (currentRequest === controller) {
                currentRequest = null;
                content.removeAttribute('aria-busy');
            }
        }
    }

    function initializeProfileForm(container) {
        // Khởi tạo lại jQuery Unobtrusive Validation sau khi thay partial.
        if (
            window.jQuery &&
            jQuery.validator &&
            jQuery.validator.unobtrusive
        ) {
            const forms = container.querySelectorAll('form');

            forms.forEach(form => {
                const $form = $(form);

                $form.removeData('validator');
                $form.removeData('unobtrusiveValidation');

                $.validator.unobtrusive.parse($form);
            });
        }
    }

    // Điều hướng giữa các tab.
    document.addEventListener('click', function (e) {
        const button = e.target.closest(
            '.account-menu[data-tab][data-url]'
        );

        if (!button) return;

        e.preventDefault();

        loadProfileTab(button.dataset.tab);
    });

    // Submit các form nằm trong profile-content bằng Fetch.
    document.addEventListener('submit', async function (e) {
        const form = e.target.closest('#profile-content form');

        if (!form) return;

        e.preventDefault();

        if (form.dataset.submitting === 'true') return;

        form.dataset.submitting = 'true';

        const submitButton = form.querySelector('[type="submit"]');
        const originalButtonHtml = submitButton?.innerHTML;

        if (submitButton) {
            submitButton.disabled = true;
            submitButton.innerHTML = 'Đang xử lý...';
        }

        try {
            const response = await fetch(form.action || window.location.href, {
                method: (form.method || 'POST').toUpperCase(),
                body: new FormData(form),
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const valid = await handleResponse(response);

            if (!valid) return;

            // Đổi mật khẩu thành công: server trả URL đăng nhập trong header.
            const redirectUrl = response.headers.get('X-Redirect-Url');

            if (redirectUrl) {
                window.location.href = redirectUrl;
                return;
            }

            const html = await response.text();

            if (!html.trim()) {
                throw new Error('Máy chủ không trả về nội dung hợp lệ.');
            }

            content.innerHTML = html;
            initializeProfileForm(content);
        } catch (error) {
            console.error('Profile form error:', error);

            await showMessage(
                'error',
                'Không thể xử lý yêu cầu',
                error.message || 'Vui lòng thử lại sau.'
            );
        } finally {
            // Không can thiệp nút nếu form đã bị thay thế bởi partial mới.
            if (form.isConnected) {
                form.dataset.submitting = 'false';

                if (submitButton?.isConnected) {
                    submitButton.disabled = false;
                    submitButton.innerHTML = originalButtonHtml;
                }
            }
        }
    });

    // Quay lại / tiến tới bằng nút Back, Forward của trình duyệt.
    window.addEventListener('popstate', function () {
        const tab = getTabFromUrl();
        loadProfileTab(tab, { pushState: false });
    });

    // Tải tab ban đầu.
    document.addEventListener('DOMContentLoaded', function () {
        const initialTab = content.dataset.initialTab || getTabFromUrl();
        const initialUrl = content.dataset.initialUrl;

        setActiveTab(initialTab);

        loadProfileTab(initialTab, {
            pushState: false,
            url: initialUrl
        });
    });
})();