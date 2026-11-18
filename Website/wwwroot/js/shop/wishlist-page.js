
'use strict';

(() => {
    const WishlistPage = {
        elements: {},

        /**
         * Khởi tạo trang Wishlist.
         */
        init() {
            this.cacheElements();

            // Đăng ký lắng nghe thay đổi Wishlist.
            this.bindEvents();

            // Đồng bộ trạng thái ban đầu.
            const wishlist = window.Caterin?.Wishlist;

            if (!wishlist) {
                console.error('Wishlist module chưa được tải.');
                return;
            }

            this.render(wishlist.getItems());
        },

        /**
         * Lấy các phần tử DOM.
         */
        cacheElements() {
            this.elements = {
                grid: document.getElementById('wishlistGrid'),
                total: document.getElementById('wishlistTotal'),
                empty: document.getElementById('wishlistEmpty')
            };
        },

        /**
         * Đăng ký sự kiện.
         */
        bindEvents() {
            document.addEventListener(
                'caterin:wishlist-updated',
                event => {
                    const items = event.detail?.items || [];

                    this.render(items);
                }
            );
        },

        /**
         * Cập nhật giao diện Wishlist.
         */
        render(items) {
            const { grid, total, empty } = this.elements;

            // Cập nhật tổng số sản phẩm yêu thích.
            if (total) {
                total.textContent = items.length;
            }

            if (!grid) return;

            // Danh sách trống.
            if (items.length === 0) {
                grid.innerHTML = '';
                grid.classList.add('hidden');

                if (empty) {
                    empty.classList.remove('hidden');
                }

                return;
            }

            // Có sản phẩm trở lại.
            grid.classList.remove('hidden');

            if (empty) {
                empty.classList.add('hidden');
            }

            // Chỉ xóa những card không còn trong Wishlist.
            grid.querySelectorAll('[data-wishlist-card-id]')
                .forEach(card => {
                    const id = card.dataset.wishlistCardId;

                    const exists = items.some(item =>
                        String(item.id) === String(id)
                    );

                    if (!exists) {
                        card.remove();
                    }
                });

            // Nếu không còn card nào trên giao diện.
            if (grid.children.length === 0) {
                grid.classList.add('hidden');

                if (empty) {
                    empty.classList.remove('hidden');
                }
            }
        }
    };

    // Khởi tạo sau khi DOM sẵn sàng.
    if (document.readyState === 'loading') {
        document.addEventListener(
            'DOMContentLoaded',
            () => WishlistPage.init(),
            { once: true }
        );
    } else {
        WishlistPage.init();
    }

})();
