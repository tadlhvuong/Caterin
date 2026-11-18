
'use strict';

document.addEventListener("DOMContentLoaded", () => {
    const container = document.getElementById("orderListContainer");

    if (!container) return;

    const content = document.getElementById("orderListContent");
    const tabs = document.querySelectorAll(".order-status-tab");
    const searchForm = document.getElementById("orderSearchForm");
    const keywordInput = document.getElementById("orderKeyword");
    const clearSearchButton = document.getElementById("clearOrderSearch");

    const url = container.dataset.url;

    if (!content || !url) return;

    let currentStatus = container.dataset.status || "All";
    let currentKeyword = container.dataset.keyword || "";
    let currentPage = Number(
        new URL(window.location.href).searchParams.get("page") || 1
    );

    let currentRequest = null;
    let requestVersion = 0;

    // =========================
    // LOAD ORDERS
    // =========================
    async function loadOrders({
        status = currentStatus,
        keyword = currentKeyword,
        page = 1,
        updateHistory = true
    } = {}) {

        currentRequest?.abort();

        const controller = new AbortController();
        currentRequest = controller;

        const version = ++requestVersion;

        const params = new URLSearchParams({
            status,
            page: String(page)
        });

        if (keyword.trim()) {
            params.set("keyword", keyword.trim());
        }

        content.classList.add("opacity-50", "pointer-events-none");

        try {
            const response = await fetch(`${url}?${params.toString()}`, {
                method: "GET",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                signal: controller.signal
            });

            if (!response.ok) {
                throw new Error("Không thể tải danh sách đơn hàng.");
            }

            const html = await response.text();

            // Không cho request cũ ghi đè request mới.
            if (version !== requestVersion) return;

            content.innerHTML = html;

            currentStatus = status;
            currentKeyword = keyword.trim();
            currentPage = page;

            container.dataset.status = currentStatus;
            container.dataset.keyword = currentKeyword;

            updateActiveTab(currentStatus);
            updateSearchUI();

            if (updateHistory) {
                updateBrowserUrl();
            }

        } catch (error) {
            if (error.name !== "AbortError") {
                console.error(error);

                Swal.fire({
                    icon: "error",
                    title: "Không thể tải đơn hàng",
                    text: "Vui lòng thử lại sau."
                });
            }
        } finally {
            if (version === requestVersion) {
                content.classList.remove("opacity-50", "pointer-events-none");
                currentRequest = null;
            }
        }
    }

    // =========================
    // UPDATE ACTIVE TAB
    // =========================
    function updateActiveTab(status) {
        tabs.forEach(tab => {
            const active = (tab.dataset.status || "All") === status;

            tab.classList.toggle("text-[#1F9345]", active);
            tab.classList.toggle("text-gray-500", !active);
            tab.classList.toggle("hover:text-gray-900", !active);

            tab.setAttribute("aria-pressed", String(active));

            tab.querySelector(".tab-indicator")
                ?.classList.toggle("hidden", !active);
        });
    }

    // =========================
    // UPDATE SEARCH UI
    // =========================
    function updateSearchUI() {
        if (keywordInput) {
            keywordInput.value = currentKeyword;
        }

        if (clearSearchButton) {
            clearSearchButton.classList.toggle(
                "hidden",
                !currentKeyword
            );
        }
    }

    // =========================
    // UPDATE URL
    // =========================
    function updateBrowserUrl() {
        const newUrl = new URL(window.location.href);

        if (currentStatus && currentStatus !== "All") {
            newUrl.searchParams.set("status", currentStatus);
        } else {
            newUrl.searchParams.delete("status");
        }

        if (currentKeyword) {
            newUrl.searchParams.set("keyword", currentKeyword);
        } else {
            newUrl.searchParams.delete("keyword");
        }

        if (currentPage > 1) {
            newUrl.searchParams.set("page", currentPage);
        } else {
            newUrl.searchParams.delete("page");
        }

        window.history.pushState({}, "", newUrl);
    }

    // =========================
    // STATUS TABS
    // =========================
    tabs.forEach(tab => {
        tab.addEventListener("click", () => {
            const status = tab.dataset.status || "All";

            if (status === currentStatus) return;

            loadOrders({
                status,
                keyword: currentKeyword,
                page: 1
            });
        });
    });

    // =========================
    // SEARCH
    // =========================
    if (searchForm) {
        searchForm.addEventListener("submit", event => {
            event.preventDefault();

            loadOrders({
                status: currentStatus,
                keyword: keywordInput?.value || "",
                page: 1
            });
        });
    }

    // =========================
    // CLEAR SEARCH
    // =========================
    clearSearchButton?.addEventListener("click", () => {
        loadOrders({
            status: currentStatus,
            keyword: "",
            page: 1
        });
    });

    // =========================
    // AJAX PAGINATION
    // Event delegation: dùng được sau khi thay partial.
    // =========================
    content.addEventListener("click", event => {
        const link = event.target.closest("a[data-page]");

        if (!link) return;

        event.preventDefault();

        const page = Number(link.dataset.page);

        if (!Number.isInteger(page) || page < 1 || page === currentPage) {
            return;
        }

        loadOrders({
            status: currentStatus,
            keyword: currentKeyword,
            page
        });
    });

    // =========================
    // BROWSER BACK / FORWARD
    // =========================
    window.addEventListener("popstate", () => {
        const params = new URLSearchParams(window.location.search);

        loadOrders({
            status: params.get("status") || "All",
            keyword: params.get("keyword") || "",
            page: Math.max(1, Number(params.get("page") || 1)),
            updateHistory: false
        });
    });

    // =========================
    // EXTERNAL RELOAD
    // Ví dụ: sau khi hủy đơn thành công.
    // =========================
    window.orderList = {
        reload: () => loadOrders({
            status: currentStatus,
            keyword: currentKeyword,
            page: currentPage
        }),

        getState: () => ({
            status: currentStatus,
            keyword: currentKeyword,
            page: currentPage
        })
    };

});
