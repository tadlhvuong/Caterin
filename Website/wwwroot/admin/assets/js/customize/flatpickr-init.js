(function () {
    'use strict';

    function initFlatpickr(container = document) {
        if (typeof flatpickr !== 'function') return;
        console.log('[Flatpickr] init called:', container);
        container.querySelectorAll('[data-flatpickr]').forEach(input => {
            console.log('[Flatpickr] found input:', input);
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