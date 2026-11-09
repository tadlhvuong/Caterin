
'use strict';

(function ($) {
    const DEFAULT_ICON = 'ti ti-shield';

    // Danh sách icon được phép sử dụng trong Role.
    // Có thể mở rộng khi cần.
    const ROLE_ICONS = [
        'ti ti-shield',
        'ti ti-shield-check',
        'ti ti-shield-lock',
        'ti ti-shield-star',
        'ti ti-users',
        'ti ti-user',
        'ti ti-user-check',
        'ti ti-user-cog',
        'ti ti-user-shield',
        'ti ti-user-plus',
        'ti ti-user-minus',
        'ti ti-user-edit',
        'ti ti-crown',
        'ti ti-key',
        'ti ti-lock',
        'ti ti-lock-open',
        'ti ti-settings',
        'ti ti-adjustments',
        'ti ti-tool',
        'ti ti-database',
        'ti ti-server',
        'ti ti-building',
        'ti ti-building-store',
        'ti ti-home',
        'ti ti-dashboard',
        'ti ti-chart-bar',
        'ti ti-chart-pie',
        'ti ti-report',
        'ti ti-file-text',
        'ti ti-folder',
        'ti ti-folder-open',
        'ti ti-message',
        'ti ti-message-circle',
        'ti ti-bell',
        'ti ti-mail',
        'ti ti-shopping-cart',
        'ti ti-package',
        'ti ti-box',
        'ti ti-tags',
        'ti ti-receipt',
        'ti ti-credit-card',
        'ti ti-calendar',
        'ti ti-clock',
        'ti ti-clipboard-list',
        'ti ti-check',
        'ti ti-circle-check',
        'ti ti-alert-circle',
        'ti ti-ban',
        'ti ti-eye',
        'ti ti-pencil',
        'ti ti-trash',
        'ti ti-download',
        'ti ti-upload',
        'ti ti-world',
        'ti ti-globe',
        'ti ti-code',
        'ti ti-terminal',
        'ti ti-device-desktop',
        'ti ti-photo',
        'ti ti-image',
        'ti ti-star',
        'ti ti-heart',
        'ti ti-briefcase'
    ];

    class IconPicker {
        constructor() {
            this.$modal = $('#addRoleModal');
            this.$input = $('#modalRoleIcon');
            this.$preview = $('#roleIconPreview');
            this.$iconName = $('#roleIconName');
            this.$grid = $('#roleIconGrid');
            this.$search = $('#roleIconSearch');
            this.$empty = $('#roleIconEmpty');
            this.$clear = $('#clearRoleIcon');

            this.selectedIcon = DEFAULT_ICON;

            if (!this.$modal.length || !this.$input.length) {
                return;
            }

            this.init();
        }

        init() {
            this.render(ROLE_ICONS);
            this.bindEvents();
            this.setIcon();
        }

        bindEvents() {
            const self = this;

            this.$search.on('input', function () {
                self.search($(this).val());
            });

            this.$grid.on('click', '.role-icon-item', function () {
                const icon = $(this).data('icon');

                self.setIcon(icon);
            });

            this.$clear.on('click', function () {
                self.setIcon('');
            });

            // Khi mở modal ở chế độ Create.
            this.$modal.on('show.bs.modal', function () {
                const mode = $('#addRoleForm').data('mode');

                if (mode === 'create') {
                    self.reset();
                }
            });
        }

        render(icons) {
            this.$grid.empty();

            icons.forEach(icon => {
                const $button = $('<button>', {
                    type: 'button',
                    class: 'role-icon-item',
                    title: icon,
                    'aria-label': icon
                });

                const $i = $('<i>', {
                    class: icon
                });

                $button.append($i);
                $button.attr('data-icon', icon);

                if (icon === this.selectedIcon) {
                    $button.addClass('active');
                    $button.attr('aria-pressed', 'true');
                } else {
                    $button.attr('aria-pressed', 'false');
                }

                this.$grid.append($button);
            });

            this.$empty.toggleClass('d-none', icons.length > 0);
        }

        search(keyword) {
            const normalized = (keyword || '')
                .trim()
                .toLowerCase();

            const filtered = ROLE_ICONS.filter(icon =>
                icon.toLowerCase().includes(normalized)
            );

            this.render(filtered);
        }

        setIcon(icon) {
            const value = ROLE_ICONS.includes(icon) ? icon : '';

            this.selectedIcon = value;

            this.$input.val(value);

            if (value) {
                this.$preview.html(
                    $('<i>', { class: `${value} ti-md` })
                );

                this.$iconName.text(value);
            } else {
                this.$preview.html(
                    $('<i>', { class: `${DEFAULT_ICON} ti-md` })
                );

                this.$iconName.text('Chưa chọn icon');
            }

            this.$grid.find('.role-icon-item').each(function () {
                const isSelected = $(this).data('icon') === value;

                $(this)
                    .toggleClass('active', isSelected)
                    .attr('aria-pressed', isSelected ? 'true' : 'false');
            });
        }

        reset() {
            this.$search.val('');
            this.setIcon('');
            this.render(ROLE_ICONS);
        }

        // Gọi khi mở modal Update Role.
        setValue(icon) {
            this.setIcon(typeof icon === 'string' ? icon.trim() : '');
            this.render(ROLE_ICONS);

            this.setIcon(icon);
        }

        getValue() {
            return this.selectedIcon;
        }
    }

    // Khởi tạo một lần.
    $(function () {
        window.roleIconPicker = new IconPicker();
    });

})(jQuery);
