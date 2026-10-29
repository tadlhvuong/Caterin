(function () {
    'use strict';

    function initFlatpickr(container = document) {
        container.querySelectorAll('[data-flatpickr]').forEach(input => {
            if (input._flatpickr) return;

            flatpickr(input, {
                dateFormat: 'd-m-Y',
                allowInput: true
            });
        });
    }

    // Khởi tạo khi trang tải xong
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => initFlatpickr());
    } else {
        initFlatpickr();
    }
    document.addEventListener('htmx:beforeSwap', function (event) {
        const target = event.detail.target;

        if (target?.id !== 'tab-content') return;

        target.querySelectorAll('[data-flatpickr]').forEach(input => {
            if (input._flatpickr) {
                input._flatpickr.destroy();
            }
        });
    });
    // Khởi tạo lại khi HTMX tải partial
    document.addEventListener('htmx:afterSwap', event => {
        initFlatpickr(event.detail.target);
    });

    window.initFlatpickr = initFlatpickr;
})();