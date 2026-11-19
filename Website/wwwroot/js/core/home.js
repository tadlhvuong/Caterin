
'use strict';

(() => {
    const initHomeBanner = () => {
        const banner = document.getElementById('homeBanner');

        if (!banner || banner.dataset.initialized === 'true') return;

        const slides = [...banner.querySelectorAll('[data-banner-slide]')];
        const dots = [...banner.querySelectorAll('[data-banner-dot]')];
        const prev = banner.querySelector('[data-banner-prev]');
        const next = banner.querySelector('[data-banner-next]');

        if (slides.length < 2) return;

        banner.dataset.initialized = 'true';

        let current = 0;
        let timer = null;
        const interval = 5500;
        const reducedMotion = window.matchMedia(
            '(prefers-reduced-motion: reduce)'
        );

        const showSlide = (index) => {
            current = (index + slides.length) % slides.length;

            slides.forEach((slide, i) => {
                const active = i === current;

                slide.classList.toggle('opacity-100', active);
                slide.classList.toggle('translate-x-0', active);
                slide.classList.toggle('opacity-0', !active);
                slide.classList.toggle('translate-x-8', !active);
                slide.classList.toggle('pointer-events-none', !active);

                slide.setAttribute('aria-hidden', String(!active));
            });

            dots.forEach((dot, i) => {
                const active = i === current;

                dot.setAttribute('aria-current', String(active));
                dot.classList.toggle('w-8', active);
                dot.classList.toggle('w-2.5', !active);
                dot.classList.toggle('bg-[#1F9345]', active);
                dot.classList.toggle('bg-gray-300', !active);
            });
        };

        const stop = () => {
            if (timer) {
                clearInterval(timer);
                timer = null;
            }
        };

        const start = () => {
            stop();

            if (reducedMotion.matches || document.hidden) return;

            timer = setInterval(() => {
                showSlide(current + 1);
            }, interval);
        };

        prev?.addEventListener('click', () => {
            showSlide(current - 1);
            start();
        });

        next?.addEventListener('click', () => {
            showSlide(current + 1);
            start();
        });

        dots.forEach((dot, index) => {
            dot.addEventListener('click', () => {
                showSlide(index);
                start();
            });
        });

        banner.addEventListener('mouseenter', stop);
        banner.addEventListener('mouseleave', start);
        banner.addEventListener('focusin', stop);
        banner.addEventListener('focusout', (event) => {
            if (!banner.contains(event.relatedTarget)) start();
        });

        document.addEventListener('visibilitychange', () => {
            document.hidden ? stop() : start();
        });

        reducedMotion.addEventListener?.('change', start);

        showSlide(0);
        start();
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initHomeBanner);
    } else {
        initHomeBanner();
    }


    //VOURCHER
    //slider

    (() => {
        const initCouponSwiper = () => {
            const element = document.getElementById("couponSwiper");

            if (!element || typeof Swiper === "undefined") return;

            if (element.swiper) return;

            new Swiper(element, {
                slidesPerView: "auto",
                spaceBetween: 16,
                speed: 650,

                grabCursor: true,
                watchOverflow: true,
                resistanceRatio: 0.85,

                noSwiping: true,
                noSwipingSelector: ".voucher-copy",

                pagination: {
                    el: "#couponPagination",
                    clickable: true
                },

                breakpoints: {
                    640: {
                        slidesPerView: 2,
                        spaceBetween: 16
                    },

                    1024: {
                        slidesPerView: 3,
                        spaceBetween: 16
                    }
                },

                a11y: {
                    enabled: true
                }
            });
        };

        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", initCouponSwiper, {
                once: true
            });
        } else {
            initCouponSwiper();
        }
    })();


    //COUPON BUTTON
    document.addEventListener('click', async (event) => {
        const button = event.target.closest('.voucher-copy');

        if (!button) return;

        const code = button.dataset.code;

        if (!code) return;

        try {
            await navigator.clipboard.writeText(code);

            const originalHtml = button.innerHTML;

            button.innerHTML = '<i class="ph ph-check text-sm"></i> Đã sao chép';
            button.classList.add('bg-gray-800');
            button.classList.remove('bg-[#1F9345]');

            setTimeout(() => {
                button.innerHTML = originalHtml;
                button.classList.remove('bg-gray-800');
                button.classList.add('bg-[#1F9345]');
            }, 1800);
        } catch {
            if (window.Swal) {
                Swal.fire({
                    icon: 'error',
                    title: 'Không thể sao chép',
                    text: 'Vui lòng sao chép mã ' + code + ' thủ công.'
                });
            } else {
                alert('Vui lòng sao chép mã: ' + code);
            }
        }
    });

    //PROMOTIONAL

    const initLifestyleSlider = () => {
        const element = document.getElementById("lifestyleSwiper");

        if (!element || typeof Swiper === "undefined") return;

        const desktopQuery = window.matchMedia("(min-width: 1024px)");
        let slider = null;

        const syncSlider = () => {
            if (desktopQuery.matches) {
                if (slider) {
                    slider.destroy(true, true);
                    slider = null;
                }

                return;
            }

            if (slider) return;

            slider = new Swiper(element, {
                slidesPerView: 1.08,
                spaceBetween: 12,
                speed: 650,

                grabCursor: true,
                watchOverflow: true,
                resistanceRatio: 0.85,
                threshold: 5,

                rewind: true,

                pagination: {
                    el: "#lifestylePagination",
                    clickable: true
                },

                a11y: {
                    enabled: true,
                    prevSlideMessage: "Banner trước",
                    nextSlideMessage: "Banner tiếp theo",
                    paginationBulletMessage: "Chuyển đến banner {{index}}"
                }
            });
        };

        syncSlider();

        if (desktopQuery.addEventListener) {
            desktopQuery.addEventListener("change", syncSlider);
        } else {
            desktopQuery.addListener(syncSlider);
        }
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initLifestyleSlider, {
            once: true
        });
    } else {
        initLifestyleSlider();
    }

    //RECOMMEND
    const section = document.getElementById('recommended');
    if (!section) return;

    const grid = section.querySelector('#recommendedGrid');
    const items = [...grid.querySelectorAll('.recommended-item')];
    const loadMoreButton = section.querySelector('#recommendedLoadMore');
    const loadMoreWrapper = section.querySelector('#recommendedLoadMoreWrapper');
    const filters = [...section.querySelectorAll('.recommended-filter')];

    const mobileQuery = window.matchMedia('(max-width: 639px)');

    let currentFilter = 'all';
    let visibleCount = mobileQuery.matches ? 4 : 8;
    let isLoadingMore = false;

    const getBatchSize = () => mobileQuery.matches ? 4 : 8;

    const getFilteredItems = () => {
        return items.filter(item => {
            return currentFilter === 'all' ||
                item.dataset.category === currentFilter;
        });
    };

    const renderItems = () => {
        const filteredItems = getFilteredItems();

        items.forEach(item => {
            item.hidden = true;
        });

        filteredItems.forEach((item, index) => {
            item.hidden = index >= visibleCount;
        });

        loadMoreWrapper.hidden = filteredItems.length <= visibleCount;
    };

    // Load more
    loadMoreButton.addEventListener('click', async () => {
        if (isLoadingMore) return;

        isLoadingMore = true;
        loadMoreButton.disabled = true;

        const text = loadMoreButton.querySelector('.recommended-load-text');
        const icon = loadMoreButton.querySelector('i');

        text.textContent = 'Đang tải...';
        icon.className = 'ph ph-spinner-gap animate-spin text-base';

        // Giả lập độ trễ AJAX trong giai đoạn mock data.
        await new Promise(resolve => setTimeout(resolve, 450));

        visibleCount += getBatchSize();
        renderItems();

        isLoadingMore = false;
        loadMoreButton.disabled = false;

        text.textContent = 'Xem thêm';
        icon.className = 'ph ph-arrow-down text-base';
    });

    // Filter category
    filters.forEach(button => {
        button.addEventListener('click', () => {
            currentFilter = button.dataset.filter;
            visibleCount = getBatchSize();

            filters.forEach(filter => {
                const active = filter === button;

                filter.classList.toggle('bg-[#1F9345]', active);
                filter.classList.toggle('text-white', active);
                filter.classList.toggle('border-[#1F9345]', active);

                filter.classList.toggle('bg-white', !active);
                filter.classList.toggle('text-gray-600', !active);
                filter.classList.toggle('border-gray-200', !active);
            });

            renderItems();
        });
    });

    // Responsive: cập nhật số sản phẩm ban đầu khi đổi breakpoint.
    const handleBreakpointChange = () => {
        visibleCount = getBatchSize();
        renderItems();
    };

    if (mobileQuery.addEventListener) {
        mobileQuery.addEventListener('change', handleBreakpointChange);
    } else {
        mobileQuery.addListener(handleBreakpointChange);
    }

    // Add to cart AJAX
    grid.addEventListener('click', async event => {
        const button = event.target.closest('.recommended-add-cart');
        if (!button) return;

        const productId = Number(button.dataset.productId);
        if (!productId || button.disabled) return;

        const originalHtml = button.innerHTML;

        button.disabled = true;
        button.innerHTML = `
            <i class="ph ph-spinner-gap animate-spin text-base"></i>
            <span>Đang thêm...</span>
        `;

        try {
            const token = section.querySelector(
                'input[name="__RequestVerificationToken"]'
            )?.value;

            const response = await fetch(section.dataset.cartUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || '',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({
                    productId: productId,
                    quantity: 1
                })
            });

            const result = await response.json();

            if (!response.ok || result.success === false) {
                throw new Error(result.message || 'Không thể thêm sản phẩm vào giỏ hàng.');
            }

            if (window.Swal) {
                await Swal.fire({
                    icon: 'success',
                    title: 'Đã thêm vào giỏ hàng',
                    text: result.message || 'Sản phẩm đã được thêm vào giỏ hàng.',
                    timer: 1500,
                    showConfirmButton: false
                });
            }

            document.dispatchEvent(new CustomEvent('caterin:cart-updated', {
                detail: result
            }));

        } catch (error) {
            if (window.Swal) {
                Swal.fire({
                    icon: 'error',
                    title: 'Không thành công',
                    text: error.message || 'Vui lòng thử lại.'
                });
            } else {
                alert(error.message || 'Vui lòng thử lại.');
            }
        } finally {
            button.disabled = false;
            button.innerHTML = originalHtml;
        }
    });

    // Initial render
    renderItems();

    //BLOG

    (() => {
        'use strict';

        const slider = document.querySelector('[data-blog-slider]');
        if (!slider) return;

        const track = slider.querySelector('[data-blog-track]');
        const prevButton = document.querySelector('.home-blog-prev');
        const nextButton = document.querySelector('.home-blog-next');

        if (!track || !prevButton || !nextButton) return;

        const originalSlides = [...track.querySelectorAll('[data-blog-slide]')];
        const total = originalSlides.length;

        if (total <= 1) {
            prevButton.hidden = true;
            nextButton.hidden = true;
            return;
        }

        let currentIndex = 0;
        let visibleCount = getVisibleCount();
        let slideWidth = 0;
        let isAnimating = false;
        let startX = 0;
        let currentX = 0;
        let isDragging = false;

        function getVisibleCount() {
            if (window.innerWidth < 640) return 1;
            if (window.innerWidth < 1024) return 2;
            return 3;
        }

        function createClones() {
            track.querySelectorAll('[data-blog-clone]').forEach(el => el.remove());

            const firstClones = originalSlides.slice(0, visibleCount);
            const lastClones = originalSlides.slice(-visibleCount);

            lastClones.forEach(slide => {
                const clone = slide.cloneNode(true);
                clone.dataset.blogClone = 'true';
                clone.setAttribute('aria-hidden', 'true');
                track.prepend(clone);
            });

            firstClones.forEach(slide => {
                const clone = slide.cloneNode(true);
                clone.dataset.blogClone = 'true';
                clone.setAttribute('aria-hidden', 'true');
                track.append(clone);
            });
        }

        function updatePosition(animate = true) {
            const slides = [...track.children];

            if (!slides.length) return;

            slideWidth = slider.clientWidth / visibleCount;

            slides.forEach(slide => {
                slide.style.width = `${slideWidth}px`;
            });

            track.style.transition = animate
                ? 'transform 500ms cubic-bezier(0.22, 1, 0.36, 1)'
                : 'none';

            track.style.transform =
                `translate3d(-${(currentIndex + visibleCount) * slideWidth}px, 0, 0)`;
        }

        function moveNext() {
            if (isAnimating) return;

            isAnimating = true;
            currentIndex++;
            updatePosition();
        }

        function movePrev() {
            if (isAnimating) return;

            isAnimating = true;
            currentIndex--;
            updatePosition();
        }

        function handleTransitionEnd() {
            if (currentIndex >= total) {
                currentIndex = 0;
                updatePosition(false);
            } else if (currentIndex < 0) {
                currentIndex = total - 1;
                updatePosition(false);
            }

            isAnimating = false;
        }

        nextButton.addEventListener('click', moveNext);
        prevButton.addEventListener('click', movePrev);

        track.addEventListener('transitionend', handleTransitionEnd);

        // Touch / mouse swipe
        slider.addEventListener('pointerdown', event => {
            if (event.pointerType === 'mouse' && event.button !== 0) return;

            startX = event.clientX;
            currentX = startX;
            isDragging = true;
        });

        slider.addEventListener('pointermove', event => {
            if (!isDragging) return;

            currentX = event.clientX;
        });

        function finishDrag() {
            if (!isDragging) return;

            const distance = currentX - startX;
            const threshold = 45;

            isDragging = false;

            if (Math.abs(distance) < threshold) return;

            if (distance < 0) {
                moveNext();
            } else {
                movePrev();
            }
        }

        slider.addEventListener('pointerup', finishDrag);
        slider.addEventListener('pointercancel', finishDrag);
        slider.addEventListener('pointerleave', finishDrag);

        // Responsive
        let resizeTimer;

        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);

            resizeTimer = setTimeout(() => {
                const nextVisibleCount = getVisibleCount();

                if (nextVisibleCount === visibleCount) {
                    updatePosition(false);
                    return;
                }

                visibleCount = nextVisibleCount;
                currentIndex = 0;

                createClones();
                updatePosition(false);
            }, 150);
        });

        // Init
        createClones();
        updatePosition(false);
    })();


    //BRAND

    (() => {
        'use strict';

        const slider = document.getElementById('brand-slider');
        const track = document.getElementById('brand-track');
        const prevButton = document.getElementById('brand-prev');
        const nextButton = document.getElementById('brand-next');

        if (!slider || !track || !prevButton || !nextButton) return;

        const originalSlides = Array.from(track.children);
        const total = originalSlides.length;

        if (total <= 1) return;

        let currentIndex = 0;
        let visibleCount = 5;
        let slideWidth = 0;
        let isTransitioning = false;
        let resizeTimer;

        const getVisibleCount = () => {
            if (window.innerWidth < 640) return 2;
            if (window.innerWidth < 1024) return 3;
            if (window.innerWidth < 1280) return 4;
            return 5;
        };

        const setTransition = enabled => {
            track.style.transition = enabled
                ? 'transform 450ms cubic-bezier(0.22, 1, 0.36, 1)'
                : 'none';
        };

        const updatePosition = () => {
            track.style.transform =
                `translate3d(-${(currentIndex + visibleCount) * slideWidth}px, 0, 0)`;
        };

        const setupSlider = () => {
            visibleCount = getVisibleCount();

            track.innerHTML = '';

            const lastClones = originalSlides.slice(-visibleCount)
                .map(slide => slide.cloneNode(true));

            const firstClones = originalSlides.slice(0, visibleCount)
                .map(slide => slide.cloneNode(true));

            lastClones.forEach(slide => track.appendChild(slide));
            originalSlides.forEach(slide => track.appendChild(slide));
            firstClones.forEach(slide => track.appendChild(slide));

            const slides = Array.from(track.children);

            slideWidth = slider.clientWidth / visibleCount;

            slides.forEach(slide => {
                slide.style.width = `${slideWidth}px`;
                slide.style.flex = `0 0 ${slideWidth}px`;
            });

            currentIndex = 0;
            isTransitioning = false;

            setTransition(false);
            updatePosition();

            requestAnimationFrame(() => setTransition(true));
        };

        const move = direction => {
            if (isTransitioning) return;

            isTransitioning = true;
            currentIndex += direction;
            updatePosition();
        };

        prevButton.addEventListener('click', () => move(-1));
        nextButton.addEventListener('click', () => move(1));

        track.addEventListener('transitionend', event => {
            if (event.target !== track) return;

            if (currentIndex >= total) {
                currentIndex = 0;
                setTransition(false);
                updatePosition();
            } else if (currentIndex < 0) {
                currentIndex = total - 1;
                setTransition(false);
                updatePosition();
            }

            isTransitioning = false;
        });

        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(setupSlider, 150);
        });

        setupSlider();
    })();

})();
