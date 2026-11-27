
'use strict';

document.addEventListener("DOMContentLoaded", () => {

    // =====================================================
    // MOBILE MENU
    // =====================================================

    const menu = document.querySelector(".mobile-menu");
    const overlay = document.querySelector(".mobile-menu-overlay");
    const openButton = document.querySelector(".mobile-menu-toggle");
    const closeButton = document.querySelector(".mobile-menu .close-button");

    if (!menu || !overlay || !openButton || !closeButton) return;

    function openMenu() {
        menu.classList.add("active");
        overlay.classList.add("active");

        menu.setAttribute("aria-hidden", "false");
        overlay.setAttribute("aria-hidden", "false");
        openButton.setAttribute("aria-expanded", "true");

        document.body.classList.add("overflow-hidden");
    }

    function closeMenu() {
        menu.classList.remove("active");
        overlay.classList.remove("active");

        menu.setAttribute("aria-hidden", "true");
        overlay.setAttribute("aria-hidden", "true");
        openButton.setAttribute("aria-expanded", "false");

        document.body.classList.remove("overflow-hidden");
    }

    openButton.setAttribute("aria-controls", "mobileMenu");
    openButton.setAttribute("aria-expanded", "false");

    openButton.addEventListener("click", openMenu);
    closeButton.addEventListener("click", closeMenu);
    overlay.addEventListener("click", closeMenu);

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && menu.classList.contains("active")) {
            closeMenu();
        }
    });

    //Dropdown danh mục search
    const dropdown = document.getElementById('headerCategoryDropdown');

    if (!dropdown || dropdown.dataset.initialized === 'true') {
        return;
    }

    const button = dropdown.querySelector('#headerCategoryButton');
    const menuCategory = dropdown.querySelector('#headerCategoryMenu');
    const arrow = dropdown.querySelector('#headerCategoryArrow');
    const valueInput = dropdown.querySelector('#headerCategoryValue');
    const label = dropdown.querySelector('#headerCategoryLabel');
    const options = Array.from(dropdown.querySelectorAll('.category-option'));

    // Validate required elements.
    if (!button || !menuCategory || !valueInput || !label || options.length === 0) {
        return;
    }

    dropdown.dataset.initialized = 'true';

    let isOpen = false;
    let selectedOption = null;

    // --------------------------------------------------
    // Dropdown state
    // --------------------------------------------------

    const setOpen = (open, focusOption = false) => {
        isOpen = open;

        menuCategory.classList.toggle('invisible', !open);
        menuCategory.classList.toggle('opacity-0', !open);
        menuCategory.classList.toggle('translate-y-2', !open);

        menuCategory.classList.toggle('visible', open);
        menuCategory.classList.toggle('opacity-100', open);
        menuCategory.classList.toggle('translate-y-0', open);

        button.setAttribute('aria-expanded', String(open));

        arrow?.classList.toggle('rotate-180', open);

        if (open && focusOption) {
            const target = selectedOption || options[0];
            target?.focus();
        }
    };

    // --------------------------------------------------
    // Select option
    // --------------------------------------------------

    const selectOption = (option, shouldFocus = true) => {
        if (!options.includes(option)) {
            return;
        }

        selectedOption = option;

        const value = option.dataset.value ?? '';
        const text = option.dataset.label ?? 'Tất cả';

        valueInput.value = value;
        label.textContent = text;

        options.forEach(item => {
            const selected = item === option;

            item.setAttribute('aria-selected', String(selected));

            item.classList.toggle('bg-[#edf7ef]', selected);

            const check = item.querySelector('.category-check');
            check?.classList.toggle('hidden', !selected);

            const title = item.querySelector('span:nth-child(2) span:first-child');

            if (title) {
                title.classList.toggle('font-semibold', selected);
                title.classList.toggle('font-medium', !selected);
            }
        });

        setOpen(false);

        if (shouldFocus) {
            button.focus();
        }
    };

    // --------------------------------------------------
    // Open / close
    // --------------------------------------------------

    button.addEventListener('click', () => {
        setOpen(!isOpen, !isOpen);
    });

    // --------------------------------------------------
    // Option events
    // --------------------------------------------------

    options.forEach((option, index) => {
        option.setAttribute('tabindex', '-1');

        option.addEventListener('click', () => {
            selectOption(option);
        });

        option.addEventListener('keydown', event => {
            let nextIndex = index;

            switch (event.key) {
                case 'ArrowDown':
                    nextIndex = (index + 1) % options.length;
                    break;

                case 'ArrowUp':
                    nextIndex = (index - 1 + options.length) % options.length;
                    break;

                case 'Home':
                    nextIndex = 0;
                    break;

                case 'End':
                    nextIndex = options.length - 1;
                    break;

                case 'Enter':
                case ' ':
                    event.preventDefault();
                    selectOption(option);
                    return;

                case 'Escape':
                    event.preventDefault();
                    setOpen(false);
                    button.focus();
                    return;

                case 'Tab':
                    setOpen(false);
                    return;

                default:
                    return;
            }

            event.preventDefault();
            options[nextIndex].focus();
        });
    });

    // --------------------------------------------------
    // Click outside
    // --------------------------------------------------

    document.addEventListener('click', event => {
        if (!dropdown.contains(event.target)) {
            setOpen(false);
        }
    });

    // --------------------------------------------------
    // Keyboard: button
    // --------------------------------------------------

    button.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();

            if (!isOpen) {
                setOpen(true, true);
            } else {
                (selectedOption || options[0]).focus();
            }
        }

        if (event.key === 'Escape' && isOpen) {
            event.preventDefault();
            setOpen(false);
        }
    });

    // --------------------------------------------------
    // Restore category from URL
    // --------------------------------------------------

    const params = new URLSearchParams(window.location.search);
    const currentValue = params.get('categoryId');

    const initialOption = currentValue !== null
        ? options.find(option => option.dataset.value === currentValue)
        : options.find(option => option.dataset.value === '');

    if (initialOption) {
        selectOption(initialOption, false);
    } else {
        selectOption(options[0], false);
    }


    // MOBILE MENU DROPDOWN
    document.querySelectorAll('.mobile-menu-toggle').forEach(toggle => {
        toggle.addEventListener('click', () => {
            const submenuId = toggle.getAttribute('aria-controls');
            const submenu = document.getElementById(submenuId);
            const icon = toggle.querySelector('i');

            if (!submenu) return;

            const isExpanded =
                toggle.getAttribute('aria-expanded') === 'true';

            toggle.setAttribute('aria-expanded', String(!isExpanded));
            submenu.classList.toggle('show', !isExpanded);
            icon?.classList.toggle('rotate-180', !isExpanded);
        });
    });

    // MOBILE SEARCH POPUP
    console.log("MOBILEPOPUP");
    const popup = document.getElementById('mobileSearchPopup');
    const panel = document.getElementById('mobileSearchPanel');
    const openMobileSearchButton = document.getElementById('mobileSearchToggle');
    const closeMobileSearchButton = document.getElementById('mobileSearchClose');
    const searchInput = document.getElementById('mobileSearchKeyword');

    if (!popup || !panel || !openMobileSearchButton || !closeMobileSearchButton) return;

    const openPopup = () => {
        popup.classList.add('active');
        popup.setAttribute('aria-hidden', 'false');
        openMobileSearchButton.setAttribute('aria-expanded', 'true');

        panel.classList.remove('-translate-y-3');
        panel.classList.add('translate-y-0');

        document.body.classList.add('overflow-hidden');

        requestAnimationFrame(() => {
            searchInput?.focus();
        });
    };

    const closePopup = () => {
        popup.classList.remove('active');
        popup.setAttribute('aria-hidden', 'true');
        openMobileSearchButton.setAttribute('aria-expanded', 'false');

        panel.classList.remove('translate-y-0');
        panel.classList.add('-translate-y-3');

        document.body.classList.remove('overflow-hidden');

        openMobileSearchButton.focus();
    };

    openMobileSearchButton.addEventListener('click', openPopup);
    closeMobileSearchButton.addEventListener('click', closePopup);

    // Click outside popup
    popup.addEventListener('click', event => {
        if (event.target === popup) {
            closePopup();
        }
    });

    // ESC to close
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && popup.classList.contains('active')) {
            closePopup();
        }
    });

    // Close when switching to tablet/desktop
    window.addEventListener('resize', () => {
        if (window.innerWidth >= 768 &&
            popup.classList.contains('active')) {
            closePopup();
        }
    });


    // PRELOADER

    const preloader = document.querySelector(".preloader");

    if (!preloader) return;

    let isHidden = false;

    function hidePreloader() {
        if (isHidden) return;

        isHidden = true;

        preloader.classList.add("is-hidden");

        setTimeout(() => {
            preloader.remove();
        }, 550);
    }

    // Trang tải xong toàn bộ tài nguyên.
    window.addEventListener("load", hidePreloader, { once: true });

    // Trường hợp sự kiện load đã xảy ra.
    if (document.readyState === "complete") {
        requestAnimationFrame(hidePreloader);
    }

    // Fallback nếu tài nguyên tải quá lâu.
    setTimeout(hidePreloader, 15000);

    // END PRELOADER


    //SCROll TO TOP
    const scrollTopButton = document.getElementById('scrollTop');
    const progressCircle = document.getElementById('scrollProgress');

    if (!scrollTopButton || !progressCircle) return;

    const circumference = 2 * Math.PI * 48;
    const showOffset = 300;

    let ticking = false;

    progressCircle.style.strokeDasharray = circumference;
    progressCircle.style.strokeDashoffset = circumference;

    const updateScroll = () => {
        const scrollTop = window.scrollY;
        const scrollHeight =
            document.documentElement.scrollHeight - window.innerHeight;

        const progress = scrollHeight > 0
            ? Math.min(scrollTop / scrollHeight, 1)
            : 0;

        progressCircle.style.strokeDashoffset =
            circumference * (1 - progress);

        const isVisible = scrollTop > showOffset;

        scrollTopButton.classList.toggle('visible', isVisible);
        scrollTopButton.classList.toggle('invisible', !isVisible);
        scrollTopButton.classList.toggle('translate-y-0', isVisible);
        scrollTopButton.classList.toggle('translate-y-4', !isVisible);
        scrollTopButton.classList.toggle('opacity-100', isVisible);
        scrollTopButton.classList.toggle('opacity-0', !isVisible);
        if (!isVisible && document.activeElement === scrollTopButton) {
            scrollTopButton.blur();
        }
        scrollTopButton.setAttribute('aria-hidden', String(!isVisible));
        scrollTopButton.tabIndex = isVisible ? 0 : -1;

        ticking = false;
    };

    window.addEventListener('scroll', () => {
        if (!ticking) {
            requestAnimationFrame(updateScroll);
            ticking = true;
        }
    }, { passive: true });

    window.addEventListener('resize', updateScroll, { passive: true });

    scrollTopButton.addEventListener('click', () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });

    updateScroll();

    //END SCROLL TO TOP


});
