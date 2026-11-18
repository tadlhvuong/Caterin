
'use strict';

(() => {
    const CartPage = {
        elements: {},
        products: [],
        isLoading: false,
        loadVersion: 0,

        init() {
            this.cacheElements();
            this.bindEvents();
            this.loadItems();
        },

        cacheElements() {
            this.elements = {
                loading: document.getElementById('cartLoading'),
                items: document.getElementById('cartItems'),
                empty: document.getElementById('cartEmpty'),
                error: document.getElementById('cartError'),

                itemCount: document.getElementById('cartItemCount'),
                summaryQuantity: document.getElementById('summaryQuantity'),
                subtotal: document.getElementById('cartSubtotal'),
                total: document.getElementById('cartTotal'),

                clearButton: document.getElementById('clearCartButton'),
                retryButton: document.getElementById('retryCartButton'),
                checkoutButton: document.getElementById('checkoutButton')
            };
        },

        bindEvents() {
            document.addEventListener('caterin:cart-updated', () => {
                this.loadItems();
            });

            this.elements.items?.addEventListener('click', event => {
                const button = event.target.closest('[data-cart-action]');

                if (!button) return;

                const action = button.dataset.cartAction;
                const id = Number(button.dataset.productId);

                if (!id || !window.Caterin?.Cart) return;

                const cartItem = window.Caterin.Cart
                    .getItems()
                    .find(item => Number(item.id) === id);

                if (!cartItem) return;

                if (action === 'increase') {
                    const product = this.products.find(x => x.id === id);
                    const stock = Number(product?.stockQuantity ?? 0);

                    if (stock <= 0) {
                        this.showToast('Sản phẩm hiện đã hết hàng.', 'warning');
                        return;
                    }

                    if (cartItem.quantity >= stock) {
                        this.showToast('Bạn đã chọn số lượng tối đa còn hàng.', 'warning');
                        return;
                    }

                    window.Caterin.Cart.updateQuantity(
                        id,
                        cartItem.quantity + 1
                    );
                }

                if (action === 'decrease') {
                    const nextQuantity = cartItem.quantity - 1;

                    if (nextQuantity <= 0) {
                        window.Caterin.Cart.remove(id);
                    } else {
                        window.Caterin.Cart.updateQuantity(id, nextQuantity);
                    }
                }

                if (action === 'remove') {
                    this.removeItem(id);
                }
            });

            this.elements.clearButton?.addEventListener('click', () => {
                this.clearCart();
            });

            this.elements.retryButton?.addEventListener('click', () => {
                this.loadItems();
            });
        },

        async loadItems() {
            if (!window.Caterin?.Cart) {
                this.showError();
                return;
            }

            const version = ++this.loadVersion;
            const cartItems = window.Caterin.Cart.getItems() || [];

            this.showLoading();

            if (cartItems.length === 0) {
                this.products = [];
                this.render();
                return;
            }

            const ids = cartItems
                .map(item => Number(item.id))
                .filter(id => Number.isInteger(id) && id > 0);

            if (ids.length === 0) {
                this.products = [];
                this.render();
                return;
            }

            try {
                const response = await fetch(
                    `/cart/items?ids=${encodeURIComponent(ids.join(','))}`,
                    {
                        method: 'GET',
                        headers: {
                            'Accept': 'application/json'
                        },
                        cache: 'no-store'
                    }
                );

                if (!response.ok) {
                    throw new Error('Không thể tải thông tin giỏ hàng.');
                }

                const products = await response.json();

                // Bỏ qua kết quả của request cũ nếu giỏ hàng vừa thay đổi.
                if (version !== this.loadVersion) return;

                this.products = Array.isArray(products) ? products : [];

                // Đồng bộ số lượng tồn kho với giỏ hàng.
                this.syncQuantities();

                // Loại bỏ sản phẩm không còn tồn tại trong catalog mock.
                const validIds = new Set(
                    this.products.map(product => Number(product.id))
                );

                cartItems.forEach(item => {
                    if (!validIds.has(Number(item.id))) {
                        window.Caterin.Cart.remove(item.id);
                    }
                });

                this.render();
            } catch (error) {
                console.error('Cart loading error:', error);

                if (version === this.loadVersion) {
                    this.showError();
                }
            }
        },

        syncQuantities() {
            const cart = window.Caterin.Cart;

            if (!cart) return;

            const items = cart.getItems();

            this.products.forEach(product => {
                const item = items.find(
                    x => Number(x.id) === Number(product.id)
                );

                if (!item) return;

                const stock = Number(product.stockQuantity || 0);

                if (stock > 0 && item.quantity > stock) {
                    cart.updateQuantity(product.id, stock);
                }
            });
        },

        render() {
            const cartItems = window.Caterin.Cart?.getItems() || [];

            const items = cartItems
                .map(item => {
                    const product = this.products.find(
                        x => Number(x.id) === Number(item.id)
                    );

                    if (!product) return null;

                    return {
                        ...product,
                        quantity: Number(item.quantity) || 1
                    };
                })
                .filter(Boolean);

            this.elements.items.innerHTML = '';

            if (items.length === 0) {
                this.showEmpty();
                this.updateSummary([]);
                return;
            }

            const fragment = document.createDocumentFragment();

            items.forEach(product => {
                fragment.appendChild(this.createProductElement(product));
            });

            this.elements.items.appendChild(fragment);

            this.elements.items.classList.remove('hidden');
            this.elements.empty.classList.add('hidden');
            this.elements.error.classList.add('hidden');
            this.elements.loading.classList.add('hidden');

            this.elements.clearButton?.classList.remove('hidden');

            this.updateSummary(items);
        },

        createProductElement(product) {
            const id = Number(product.id);
            const quantity = Number(product.quantity);
            const stock = Number(product.stockQuantity || 0);
            const isAvailable = product.isAvailable && stock > 0;

            const row = document.createElement('article');

            row.className =
                'flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:px-6';

            row.dataset.cartProductId = id;

            // Product image.
            const imageLink = document.createElement('a');
            imageLink.href = `/product/${encodeURIComponent(product.slug)}`;
            imageLink.className =
                'h-24 w-24 shrink-0 overflow-hidden rounded-xl bg-gray-50';

            const image = document.createElement('img');
            image.src = product.imageUrl;
            image.alt = product.name;
            image.loading = 'lazy';
            image.className = 'h-full w-full object-contain p-2';

            imageLink.appendChild(image);

            // Product information.
            const information = document.createElement('div');
            information.className = 'min-w-0 flex-1';

            const category = document.createElement('p');
            category.className =
                'text-xs font-medium text-[#1F9345]';
            category.textContent = product.categoryName || 'Caterin';

            const name = document.createElement('a');
            name.href = `/product/${encodeURIComponent(product.slug)}`;
            name.className =
                'mt-1 block line-clamp-2 text-sm font-bold text-gray-900 transition hover:text-[#1F9345]';
            name.textContent = product.name;

            const volume = document.createElement('p');
            volume.className = 'mt-1 text-xs text-gray-500';
            volume.textContent = product.volume || '';

            const stockLabel = document.createElement('p');
            stockLabel.className = isAvailable
                ? 'mt-2 text-xs font-medium text-green-600'
                : 'mt-2 text-xs font-medium text-red-500';

            stockLabel.textContent = isAvailable
                ? `Còn ${stock} sản phẩm`
                : 'Tạm hết hàng';

            information.append(category, name, volume, stockLabel);

            // Quantity controls.
            const quantityWrapper = document.createElement('div');
            quantityWrapper.className =
                'flex items-center justify-between gap-4 sm:flex-col sm:items-end sm:justify-center';

            const priceWrapper = document.createElement('div');
            priceWrapper.className = 'text-right';

            const unitPrice = document.createElement('p');
            unitPrice.className =
                'text-sm font-bold text-gray-900';
            unitPrice.textContent = this.formatPrice(product.price);

            priceWrapper.appendChild(unitPrice);

            if (product.originalPrice > product.price) {
                const originalPrice = document.createElement('p');
                originalPrice.className =
                    'mt-1 text-xs text-gray-400 line-through';
                originalPrice.textContent =
                    this.formatPrice(product.originalPrice);

                priceWrapper.appendChild(originalPrice);
            }

            const controls = document.createElement('div');
            controls.className = 'flex items-center gap-2';

            const quantityControl = document.createElement('div');
            quantityControl.className =
                'flex h-9 items-center overflow-hidden rounded-lg border border-gray-200';

            const decreaseButton = this.createActionButton(
                'decrease',
                id,
                'Giảm số lượng',
                'ph ph-minus'
            );

            const quantityText = document.createElement('span');
            quantityText.className =
                'min-w-9 px-2 text-center text-sm font-semibold text-gray-900';
            quantityText.textContent = quantity;

            const increaseButton = this.createActionButton(
                'increase',
                id,
                'Tăng số lượng',
                'ph ph-plus'
            );

            if (!isAvailable || quantity >= stock) {
                increaseButton.disabled = true;
                increaseButton.classList.add('cursor-not-allowed', 'opacity-40');
            }

            quantityControl.append(
                decreaseButton,
                quantityText,
                increaseButton
            );

            const removeButton = this.createActionButton(
                'remove',
                id,
                'Xóa sản phẩm',
                'ph ph-trash'
            );

            removeButton.className =
                'flex h-9 w-9 items-center justify-center rounded-lg text-gray-400 transition hover:bg-red-50 hover:text-red-500';

            controls.append(quantityControl, removeButton);

            const lineTotal = document.createElement('p');
            lineTotal.className =
                'mt-2 text-sm font-bold text-[#1F9345]';
            lineTotal.textContent =
                this.formatPrice(product.price * quantity);

            quantityWrapper.append(priceWrapper, controls, lineTotal);

            row.append(imageLink, information, quantityWrapper);

            if (!isAvailable) {
                row.classList.add('bg-red-50/30');
            }

            return row;
        },

        createActionButton(action, id, label, iconClass) {
            const button = document.createElement('button');

            button.type = 'button';
            button.dataset.cartAction = action;
            button.dataset.productId = id;
            button.setAttribute('aria-label', label);

            button.className =
                'flex h-8 w-8 items-center justify-center text-gray-500 transition hover:text-[#1F9345]';

            const icon = document.createElement('i');
            icon.className = `${iconClass} text-sm`;

            button.appendChild(icon);

            return button;
        },

        updateSummary(items) {
            const quantity = items.reduce(
                (total, item) => total + item.quantity,
                0
            );

            const subtotal = items.reduce(
                (total, item) => total + item.price * item.quantity,
                0
            );

            this.elements.itemCount.textContent = quantity;
            this.elements.summaryQuantity.textContent = quantity;
            this.elements.subtotal.textContent = this.formatPrice(subtotal);
            this.elements.total.textContent = this.formatPrice(subtotal);

            const hasUnavailableItem = items.some(
                item => !item.isAvailable || item.stockQuantity <= 0
            );

            const checkoutDisabled = items.length === 0 || hasUnavailableItem;

            this.elements.checkoutButton?.classList.toggle(
                'pointer-events-none',
                checkoutDisabled
            );

            this.elements.checkoutButton?.classList.toggle(
                'opacity-50',
                checkoutDisabled
            );

            this.elements.checkoutButton?.setAttribute(
                'aria-disabled',
                String(checkoutDisabled)
            );

            if (checkoutDisabled) {
                this.elements.checkoutButton?.setAttribute('tabindex', '-1');
            } else {
                this.elements.checkoutButton?.removeAttribute('tabindex');
            }
        },

        async removeItem(id) {
            const confirmRemove = await this.confirm(
                'Xóa sản phẩm?',
                'Bạn có chắc chắn muốn xóa sản phẩm này khỏi giỏ hàng?'
            );

            if (!confirmRemove) return;

            window.Caterin.Cart.remove(id);
            this.showToast('Đã xóa sản phẩm khỏi giỏ hàng.', 'success');
        },

        async clearCart() {
            const confirmClear = await this.confirm(
                'Xóa toàn bộ giỏ hàng?',
                'Tất cả sản phẩm sẽ bị xóa khỏi giỏ hàng.'
            );

            if (!confirmClear) return;

            window.Caterin.Cart.clear();
            this.showToast('Đã xóa toàn bộ giỏ hàng.', 'success');
        },

        confirm(title, text) {
            if (window.Swal) {
                return window.Swal.fire({
                    title,
                    text,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Đồng ý',
                    cancelButtonText: 'Hủy',
                    confirmButtonColor: '#1F9345',
                    cancelButtonColor: '#9CA3AF',
                    reverseButtons: true
                }).then(result => result.isConfirmed);
            }

            return Promise.resolve(window.confirm(`${title}\n${text}`));
        },

        showToast(message, icon = 'success') {
            if (!window.Swal) return;

            window.Swal.fire({
                toast: true,
                position: 'top-end',
                icon,
                title: message,
                showConfirmButton: false,
                timer: 2500,
                timerProgressBar: true
            });
        },

        showLoading() {
            this.elements.loading.classList.remove('hidden');
            this.elements.items.classList.add('hidden');
            this.elements.empty.classList.add('hidden');
            this.elements.error.classList.add('hidden');
        },

        showEmpty() {
            this.elements.loading.classList.add('hidden');
            this.elements.items.classList.add('hidden');
            this.elements.empty.classList.remove('hidden');
            this.elements.error.classList.add('hidden');
            this.elements.clearButton?.classList.add('hidden');
        },

        showError() {
            this.elements.loading.classList.add('hidden');
            this.elements.items.classList.add('hidden');
            this.elements.empty.classList.add('hidden');
            this.elements.error.classList.remove('hidden');
        },

        formatPrice(value) {
            return new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND',
                maximumFractionDigits: 0
            }).format(Number(value) || 0);
        }
    };

    document.addEventListener('DOMContentLoaded', () => {
        CartPage.init();
    });
})();
