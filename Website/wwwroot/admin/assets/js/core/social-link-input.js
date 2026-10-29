/// Customer input create, remove and submit social links

(function () {
    'use strict';

    const container = document.getElementById('socialLinksContainer');
    const addButton = document.getElementById('addSocialLink');

    if (!container) return;

    const iconMap = {
        Facebook: 'ti-brand-facebook',
        Instagram: 'ti-brand-instagram',
        TikTok: 'ti-brand-tiktok',
        YouTube: 'ti-brand-youtube',
        Zalo: 'ti-message-circle',
        LinkedIn: 'ti-brand-linkedin',
        X: 'ti-brand-x',
        Website: 'ti-world'
    };

    const getRows = () =>
        [...container.querySelectorAll('.social-link-item')];

    function updateIndexes() {
        const rows = getRows();

        rows.forEach((row, index) => {
            const typeInput = row.querySelector('.social-link-type');
            const urlInput = row.querySelector('.social-link-url');

            if (typeInput) {
                typeInput.name = `SocialLinks[${ index }].Type`;
            }

            if (urlInput) {
                urlInput.name = `SocialLinks[${ index }].Url`;
            }

            // Xóa nút xóa cũ trước khi tạo lại.
            row.querySelector('[data-action="remove-social-link"]')
                ?.remove();

            // Chỉ hiển thị nút xóa khi có nhiều hơn một dòng.
            if (rows.length > 1) {
                const removeButton = document.createElement('button');

                removeButton.type = 'button';
                removeButton.className = 'btn btn-outline-danger';
                removeButton.dataset.action = 'remove-social-link';
                removeButton.setAttribute('aria-label', 'Xóa liên kết');
                removeButton.innerHTML = '<i class="ti ti-trash"></i>';

                row.appendChild(removeButton);
            }
        });
    }

    function updateIcon(row) {
        const type = row.querySelector('.social-link-type')?.value;
        const icon = row.querySelector('.social-link-icon');

        if (icon) {
            icon.className =
                `ti ${iconMap[type] || 'ti-link'} social-link-icon`;
        }
    }

    function updateAvailableSocialTypes() {
        const rows = getRows();

        const selectedTypes = new Set(
            rows.map(row =>
                row.querySelector('.social-link-type')?.value
            ).filter(Boolean)
        );

        rows.forEach(row => {
            const currentType =
                row.querySelector('.social-link-type')?.value;

            row.querySelectorAll('.social-link-options [data-type]')
                .forEach(option => {
                    const type = option.dataset.type;

                    option.disabled =
                        selectedTypes.has(type) && type !== currentType;

                    option.classList.toggle(
                        'active',
                        type === currentType
                    );
                });
        });
    }

    function createRow() {
        const firstRow = container.querySelector('.social-link-item');

        if (!firstRow) return null;

        const row = firstRow.cloneNode(true);

        const typeInput = row.querySelector('.social-link-type');
        const urlInput = row.querySelector('.social-link-url');

        // Chọn nền tảng chưa được sử dụng.
        const selectedTypes = new Set(
            getRows().map(item =>
                item.querySelector('.social-link-type')?.value
            )
        );

        const availableType = Object.keys(iconMap).find(
            type => !selectedTypes.has(type)
        );

        if (!availableType) {
            return null;
        }

        typeInput.value = availableType;
        urlInput.value = '';

        // Không sao chép trạng thái validation hoặc giá trị cũ.
        urlInput.classList.remove('is-valid', 'is-invalid');
        urlInput.setCustomValidity('');

        updateIcon(row);

        return row;
    }

    if (addButton) {
        addButton.addEventListener('click', function () {
            const row = createRow();

            if (!row) {
                return;
            }

            container.appendChild(row);

            updateIndexes();
            updateAvailableSocialTypes();
        });
    }

    container.addEventListener('click', function (event) {
        const removeButton = event.target.closest(
            '[data-action="remove-social-link"]'
        );

        if (removeButton && container.contains(removeButton)) {
            const rows = getRows();

            // Không xóa dòng cuối cùng.
            if (rows.length <= 1) return;

            removeButton.closest('.social-link-item').remove();

            updateIndexes();
            updateAvailableSocialTypes();
            return;
        }

        const option = event.target.closest(
            '.social-link-options [data-type]'
        );

        if (!option || option.disabled) return;

        const row = option.closest('.social-link-item');
        if (!row) return;

        const typeInput = row.querySelector('.social-link-type');
        typeInput.value = option.dataset.type;

        updateIcon(row);

        row.querySelectorAll('.social-link-options [data-type]')
            .forEach(item => {
                item.classList.toggle(
                    'active',
                    item.dataset.type === typeInput.value
                );
            });

        updateIndexes();
        updateAvailableSocialTypes();
    });

    // Kiểm tra lại dữ liệu trước khi form submit.
    const form = container.closest('form');

    if (form) {
        form.addEventListener('submit', function () {
            updateIndexes();
        });
    }

    updateIndexes();
    getRows().forEach(updateIcon);
    updateAvailableSocialTypes();
})();