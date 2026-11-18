
'use strict';

(() => {
    const STORAGE_KEY = 'caterin_wishlist';
    const EVENT_NAME = 'caterin:wishlist-updated';

    window.Caterin = window.Caterin || {};

    // Tránh khởi tạo module nhiều lần.
    if (window.Caterin.Wishlist) {
        return;
    }

    const Wishlist = {
        /**
         * Đọc dữ liệu Wishlist từ localStorage.
         */
        read() {
            try {
                const data = localStorage.getItem(STORAGE_KEY);

                if (!data) return [];

                const items = JSON.parse(data);

                if (!Array.isArray(items)) return [];

                return items.filter(item =>
                    item && item.id !== undefined &&
                    item.id !== null &&
                    String(item.id).trim() !== ''
                );
            } catch (error) {
                console.error('Không thể đọc Wishlist:', error);
                return [];
            }
        },

        /**
         * Lưu dữ liệu Wishlist.
         */
        save(items) {
            try {
                localStorage.setItem(
                    STORAGE_KEY,
                    JSON.stringify(items)
                );

                this.notify(items);

                return true;
            } catch (error) {
                console.error('Không thể lưu Wishlist:', error);
                return false;
            }
        },

        /**
         * Phát sự kiện khi Wishlist thay đổi.
         */
        notify(items) {
            this.updateUI(items);

            document.dispatchEvent(
                new CustomEvent(EVENT_NAME, {
                    detail: {
                        items,
                        count: items.length
                    }
                })
            );
        },

        /**
         * Lấy toàn bộ sản phẩm yêu thích.
         */
        getItems() {
            return this.read();
        },

        /**
         * Kiểm tra sản phẩm đã có trong Wishlist chưa.
         */
        contains(id) {
            if (id === undefined || id === null) {
                return false;
            }

            return this.read().some(item =>
                String(item.id) === String(id)
            );
        },

        /**
         * Thêm hoặc xóa sản phẩm khỏi Wishlist.
         *
         * @returns {boolean} true nếu thêm, false nếu xóa.
         */
        toggle(product) {
            if (!product || product.id === undefined ||
                product.id === null ||
                String(product.id).trim() === '') {
                console.error('Thông tin sản phẩm Wishlist không hợp lệ.');
                return false;
            }

            const items = this.read();

            const index = items.findIndex(item =>
                String(item.id) === String(product.id)
            );

            // Đã tồn tại: xóa khỏi Wishlist.
            if (index !== -1) {
                items.splice(index, 1);

                if (!this.save(items)) return false;

                return false;
            }

            // Chưa tồn tại: thêm vào Wishlist.
            items.push({
                id: product.id,
                name: product.name || '',
                price: product.price || '',
                image: product.image || '',
                slug: product.slug || ''
            });

            if (!this.save(items)) return false;

            return true;
        },

        /**
         * Xóa sản phẩm khỏi Wishlist theo ID.
         */
        remove(id) {
            const items = this.read();

            const filtered = items.filter(item =>
                String(item.id) !== String(id)
            );

            if (filtered.length === items.length) {
                return false;
            }

            return this.save(filtered);
        },

        /**
         * Cập nhật trạng thái nút Wishlist và bộ đếm.
         */
        updateUI(items = this.read()) {
            const ids = new Set(
                items.map(item => String(item.id))
            );

            // Cập nhật bộ đếm Wishlist trên Header.
            document.querySelectorAll('[data-wishlist-count]')
                .forEach(element => {
                    element.textContent = items.length;

                    element.classList.toggle(
                        'hidden',
                        items.length === 0
                    );
                });

            // Cập nhật trạng thái các nút yêu thích.
            document.querySelectorAll('[data-wishlist-product-id]')
                .forEach(button => {
                    const id = String(
                        button.dataset.wishlistProductId
                    );

                    const active = ids.has(id);

                    button.setAttribute(
                        'aria-pressed',
                        String(active)
                    );

                    button.classList.toggle(
                        'text-red-500',
                        active
                    );

                    const icon = button.querySelector('.ph-heart');

                    if (icon) {
                        icon.classList.toggle('ph-fill', active);
                        icon.classList.toggle('ph', !active);
                    }
                });
        },

        /**
         * Đồng bộ trạng thái từ localStorage.
         */
        refresh() {
            const items = this.read();

            this.notify(items);

            return items;
        }
    };

    // Công khai module.
    window.Caterin.Wishlist = Wishlist;

    // Đồng bộ khi có thay đổi từ tab trình duyệt khác.
    window.addEventListener('storage', event => {
        if (event.key === STORAGE_KEY) {
            Wishlist.refresh();
        }
    });

    // Khởi tạo trạng thái ban đầu.
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            Wishlist.refresh();
        }, { once: true });
    } else {
        Wishlist.refresh();
    }

})();
