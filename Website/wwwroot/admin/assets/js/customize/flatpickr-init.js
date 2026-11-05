(function () {
    'use strict';

    function initFlatpickr(container = document) {
        if (typeof flatpickr !== 'function') return;
        container.querySelectorAll('[data-flatpickr]').forEach(input => {
            if (input._flatpickr) return;
            flatpickr(input, {
                dateFormat: 'd-m-Y',
                allowInput: true,
                appendTo: container
            });
        });
    }

    window.initFlatpickr = initFlatpickr;
})();