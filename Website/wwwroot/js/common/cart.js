
'use strict';

(() => {
    const STORAGE_KEY = 'caterin_cart';

    function readCart() {
        try {
            const value = JSON.parse(localStorage.getItem(STORAGE_KEY));
            return Array.isArray(value) ? value : [];
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(cart));

        document.dispatchEvent(new CustomEvent('caterin:cart-updated', {
            detail: {
                cart,
                count: getTotalQuantity(cart)
            }
        }));

        updateCartCount(cart);
    }

    function getTotalQuantity(cart = readCart()) {
        return cart.reduce((total, item) => total + item.quantity, 0);
    }

    function updateCartCount(cart = readCart()) {
        const count = getTotalQuantity(cart);

        document.querySelectorAll('[data-cart-count]').forEach(element => {
            element.textContent = count;
            element.classList.toggle('hidden', count === 0);
        });
    }

    function add(product, quantity = 1) {
        if (!product || !product.id) {
            throw new Error('Thông tin sản phẩm không hợp lệ.');
        }

        const cart = readCart();
        const productId = String(product.id);

        const existing = cart.find(item => item.id === productId);

        if (existing) {
            existing.quantity += quantity;
        } else {
            cart.push({
                id: productId,
                name: product.name || '',
                price: Number(product.price) || 0,
                image: product.image || '',
                slug: product.slug || '',
                quantity
            });
        }

        saveCart(cart);
        return cart;
    }

    function remove(productId) {
        const cart = readCart().filter(
            item => item.id !== String(productId)
        );

        saveCart(cart);
        return cart;
    }

    function updateQuantity(productId, quantity) {
        const cart = readCart();
        const item = cart.find(
            x => x.id === String(productId)
        );

        if (!item) return cart;

        if (quantity <= 0) {
            return remove(productId);
        }

        item.quantity = quantity;
        saveCart(cart);

        return cart;
    }

    function clear() {
        saveCart([]);
    }

    function getItems() {
        return readCart();
    }

    function getCount() {
        return getTotalQuantity();
    }

    function getSubtotal() {
        return readCart().reduce(
            (total, item) => total + item.price * item.quantity,
            0
        );
    }

    window.Caterin = window.Caterin || {};

    window.Caterin.Cart = {
        add,
        remove,
        updateQuantity,
        clear,
        getItems,
        getCount,
        getSubtotal,
        refresh: updateCartCount
    };

    document.addEventListener('DOMContentLoaded', () => {
        updateCartCount();
    });
})();
