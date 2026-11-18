
'use strict';

(() => {
    const ShopPage = {
        form: null,
        results: null,
        currentRequest: null,

        init() {
            this.form = document.querySelector('[data-ajax-filter]');
            this.results = document.getElementById('shopResultsContainer');

            if (!this.form || !this.results) return;

            this.bindEvents();
        },

        bindEvents() {
            // Submit bộ lọc Sidebar.
            this.form.addEventListener('submit', event => {
                event.preventDefault();
                this.applyFilters();
            });

            // Bắt sự kiện xóa lọc bằng event delegation.
            document.addEventListener('click', event => {
                const clearLink = event.target.closest('[data-clear-shop-filter]');

                if (!clearLink) return;

                event.preventDefault();
                this.clearFilters();
            });

            // Phân trang AJAX.
            this.results.addEventListener('click', event => {
                const link = event.target.closest('[data-shop-page-link]');

                if (!link) return;

                event.preventDefault();

                this.loadUrl(link.href, true);
            });

            // Sắp xếp AJAX.
            this.results.addEventListener('change', event => {
                const sortSelect = event.target.closest(
                    '[data-shop-sort-form] select[name="sortBy"]'
                );

                if (!sortSelect) return;

                const sortForm = sortSelect.form;

                if (!sortForm) return;

                const url = this.buildUrlFromForm(sortForm);

                url.searchParams.set('page', '1');

                this.loadUrl(url.toString(), true);
            });

            // Back / Forward trình duyệt.
            window.addEventListener('popstate', () => {
                this.loadUrl(window.location.href, false);
            });
        },

        // =========================
        // FILTER
        // =========================

        buildUrl() {
            return this.buildUrlFromForm(this.form);
        },

        buildUrlFromForm(form) {
            const params = new URLSearchParams();

            const formData = new FormData(form);

            for (const [key, value] of formData.entries()) {
                const normalizedValue = String(value).trim();

                // Không đưa tham số rỗng vào URL.
                if (normalizedValue === '') continue;

                params.set(key, normalizedValue);
            }

            const url = new URL(form.action, window.location.origin);

            url.search = params.toString();

            return url;
        },

        async applyFilters() {
            const url = this.buildUrl();

            url.searchParams.set('page', '1');

            await this.loadUrl(url.toString(), true);
        },

        clearFilters() {
            this.form.reset();

            // Reset sort về mặc định.
            const sortSelect = this.results.querySelector(
                '[data-shop-sort-form] select[name="sortBy"]'
            );

            if (sortSelect) {
                sortSelect.value = 'featured';
            }

            // Xóa toàn bộ điều kiện lọc.
            const url = new URL(this.form.action, window.location.origin);

            url.searchParams.set('sortBy', 'featured');
            url.searchParams.set('page', '1');

            this.loadUrl(url.toString(), true);
        },

        // =========================
        // AJAX
        // =========================

        async loadUrl(url, updateHistory = true) {
            // Hủy request trước đó nếu vẫn đang chạy.
            if (this.currentRequest) {
                this.currentRequest.abort();
            }

            const controller = new AbortController();

            this.currentRequest = controller;

            try {
                this.setLoading(true);

                const response = await fetch(url, {
                    method: 'GET',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'text/html'
                    },
                    cache: 'no-store',
                    signal: controller.signal
                });

                if (!response.ok) {
                    throw new Error(
                        `Không thể tải danh sách sản phẩm. HTTP ${response.status}`
                    );
                }

                const html = await response.text();

                // Chỉ cập nhật nếu đây vẫn là request mới nhất.
                if (this.currentRequest !== controller) return;

                this.results.innerHTML = html;

                if (updateHistory) {
                    window.history.pushState({}, '', url);
                }

                this.syncFormFromUrl(url);

                // Thông báo cho các thành phần khác.
                document.dispatchEvent(
                    new CustomEvent('caterin:shop-filtered')
                );

            } catch (error) {
                if (error.name === 'AbortError') return;

                console.error('Shop AJAX error:', error);

                if (window.Swal) {
                    window.Swal.fire({
                        icon: 'error',
                        title: 'Không thể tải sản phẩm',
                        text: 'Vui lòng thử lại sau.'
                    });
                }
            } finally {
                if (this.currentRequest === controller) {
                    this.currentRequest = null;
                    this.setLoading(false);
                }
            }
        },

        // =========================
        // SYNC FORM
        // =========================

        syncFormFromUrl(url) {
            const currentUrl = new URL(url, window.location.origin);

            // Đồng bộ Sidebar.
            this.syncForm(this.form, currentUrl);

            // Đồng bộ Sort form sau khi partial được render lại.
            const sortForm = this.results.querySelector(
                '[data-shop-sort-form]'
            );

            if (sortForm) {
                this.syncForm(sortForm, currentUrl);
            }
        },

        syncForm(form, url) {
            form.querySelectorAll('[name]').forEach(input => {
                const values = url.searchParams.getAll(input.name);

                if (input.type === 'radio' || input.type === 'checkbox') {
                    input.checked = values.includes(input.value);
                    return;
                }

                if (
                    input.type === 'submit' ||
                    input.type === 'button'
                ) {
                    return;
                }

                input.value = values[0] || '';
            });
        },

        // =========================
        // LOADING
        // =========================

        setLoading(loading) {
            this.results.classList.toggle('opacity-50', loading);
            this.results.classList.toggle('pointer-events-none', loading);

            this.results.setAttribute(
                'aria-busy',
                String(loading)
            );
        }
    };

    document.addEventListener('DOMContentLoaded', () => {
        ShopPage.init();
    });

})();
