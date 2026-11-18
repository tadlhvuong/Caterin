
'use strict';

(() => {
    console.log('PRODUCT-ACTION');

    /**
     * Lấy thông tin sản phẩm từ button hoặc product card.
     */
    function getProductData(button) {
        const card = button.closest('[data-product-card]');
        const source = card || button;

        return {
            id: button.dataset.productId ||
                button.dataset.wishlistProductId,

            name: source.dataset.productName ||
                button.dataset.productName || '',

            price: source.dataset.productPrice ||
                button.dataset.productPrice || 0,

            image: source.dataset.productImage ||
                button.dataset.productImage || '',

            slug: source.dataset.productSlug ||
                button.dataset.productSlug || ''
        };
    }

    /**
     * Hiển thị thông báo.
     */
    function notify(message, icon = 'success') {
        if (window.Swal) {
            window.Swal.fire({
                toast: true,
                position: 'top-end',
                icon,
                title: message,
                showConfirmButton: false,
                timer: 2200,
                timerProgressBar: true
            });

            return;
        }

        console.info(message);
    }

    /**
     * Xử lý thêm giỏ hàng và Wishlist.
     */
    document.addEventListener('click', event => {
        const target = event.target;

        if (!(target instanceof Element)) return;

        // Thêm vào giỏ hàng.
        const cartButton = target.closest('[data-add-to-cart]');

        if (cartButton) {
            event.preventDefault();

            if (!window.Caterin?.Cart) {
                console.error('Cart module chưa được tải.');
                return;
            }

            try {
                const product = getProductData(cartButton);

                if (!product.id) {
                    console.error('Không tìm thấy ID sản phẩm.');
                    return;
                }

                window.Caterin.Cart.add(product);

                notify('Đã thêm sản phẩm vào giỏ hàng.');

            } catch (error) {
                console.error('Add to cart error:', error);

                notify(
                    'Không thể thêm sản phẩm vào giỏ hàng.',
                    'error'
                );
            }

            return;
        }

        // Thêm hoặc xóa Wishlist.
        const wishlistButton = target.closest(
            '[data-wishlist-product-id]'
        );

        if (!wishlistButton) return;

        event.preventDefault();

        if (!window.Caterin?.Wishlist) {
            console.error('Wishlist module chưa được tải.');
            return;
        }

        try {
            const product = getProductData(wishlistButton);

            if (!product.id) {
                console.error('Không tìm thấy ID sản phẩm.');
                return;
            }

            const added = window.Caterin.Wishlist.toggle(product);

            if (added === null) {
                notify(
                    'Không thể cập nhật danh sách yêu thích.',
                    'error'
                );
                return;
            }

            notify(
                added
                    ? 'Đã thêm vào danh sách yêu thích.'
                    : 'Đã xóa khỏi danh sách yêu thích.',
                added ? 'success' : 'info'
            );

        } catch (error) {
            console.error('Wishlist error:', error);

            notify(
                'Không thể cập nhật danh sách yêu thích.',
                'error'
            );
        }
    });
})();
