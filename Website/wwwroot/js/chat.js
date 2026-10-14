"use strict";

document.addEventListener("DOMContentLoaded", () => {
    // ============================================================
    // CONFIG
    // ============================================================
    const CONFIG = {
        storage: {
            hidePromo: "hidePromo",
            contactId: "caterin_chat_contact_id",
            conversationId: "caterin_chat_conversation_id",
            guestToken: "caterin_chat_guest_token",
            notificationPromptSnoozeUntil: "caterin_notification_prompt_snooze_until"
        },

        notification: {
            pendingMessage: null
        },

        timing: {
            promoDelay: 1800,
            typingTimeout: 1500,
            focusDelay: 250,
            reconnectDelay: 3000,
            quickReplyInactivity: 30 * 60 * 1000, //30' show quick reply 
            notificationPromptDelay: 7 * 24 * 60 * 60 * 1000
        },

        limits: {
            processedMessageIds: 500,
            attachmentsPerMessage: 5,
            attachmentSizeBytes: 12 * 1024 * 1024,
            attachmentTotalBytes: 25 * 1024 * 1024
        },

        senderType: {
            contact: 1,
            admin: 2,
            system: 3,
            bot: 4
        },

        messageStatus: {
            pending: 1,
            sent: 2,
            delivered: 3,
            read: 4,
            failed: 5
        },

        conversationStatus: {
            open: 1,
            pending: 2,
            resolved: 3,
            closed: 4,

            names: {
                1: "open",
                2: "pending",
                3: "resolved",
                4: "closed"
            }
        }
    };


    // ============================================================
    // DOM
    // ============================================================

    const DOM = {
        popup: document.getElementById("promoPopup"),
        hideToday: document.getElementById("hideToday"),
        closePopup: document.getElementById("closePopup"),
        laterBtn: document.getElementById("laterBtn"),

        chatToggle: document.getElementById("chatToggle"),
        chatWindow: document.getElementById("chatWindow"),
        chatClose: document.getElementById("chatClose"),

        chatInput: document.getElementById("chatInput"),
        sendBtn: document.getElementById("sendBtn"),

        chatBody: document.getElementById("chatBody"),
        typing: document.getElementById("typing"),
        chatBadge: document.getElementById("chatBadge"),
        quickReplies: document.querySelector(".quick-replies"),

        chatSessionReset: document.getElementById("chatSessionReset"),
        resetChatBox: document.getElementById("resetChatBox"),

        attachmentBtn: document.getElementById("attachmentBtn"),
        chatFileInput: document.getElementById("chatFileInput"),
        chatAttachmentPreview: document.getElementById("chatAttachmentPreview"),
        chatAttachmentList: document.getElementById("chatAttachmentList"),

        emojiBtn: document.getElementById("emojiBtn"),

        adminStatusDot: document.getElementById("chatAdminStatusDot"),
        chatStatusText: document.getElementById("chatStatusText"),
        chatStatusBadge: document.getElementById("chatStatusBadge"),
         
        notificationPrompt: document.querySelector("#chatNotificationPrompt"),

        notificationAllow: document.querySelector("#chatNotificationAllow"),

        notificationLater: document.querySelector("#chatNotificationLater"),

        chatHistoryLoader: document.getElementById("chatHistoryLoader"),

        loadPreviousConversationBtn: document.getElementById("loadPreviousConversationBtn"),
    };


    // ============================================================
    // CHAT CONFIG FROM HTML
    // ============================================================

    const CHAT_CONFIG = {
        inboxId: Number(DOM.chatWindow?.dataset.inboxId) || null,
        name: DOM.chatWindow?.dataset.name || null,
        email: DOM.chatWindow?.dataset.email || null,
        phone: DOM.chatWindow?.dataset.phone || null,
        isAuthenticated: DOM.chatWindow?.dataset.authenticated === "true"
    };

    // ============================================================
    // STATE
    // ============================================================

    const state = {
        currentConversationId: null,
        currentConversationStatus: null,
        currentContactId: null,
        guestToken: null,
        pendingConversationId: null,
        unreadMessageCount: 0,
        isTyping: false,
        typingTimer: null,

        reconnectTimer: null,
        chatSession: {
            hasActiveHistory: false,
            isInvalid: false
        },
        quickReplies: {
            visible: false,
            timer: null,
            lastMessageAt: null
        },
        notification: {
            pendingMessage: null
        },

        processedMessageIds: new Set(),

        messagePaging: {
            limit: 30,
            oldestMessageId: null,
            hasMore: true,
            loading: false
        },

        history: {
            previousConversationId: null,
            hasPrevious: false,
            loading: false,
            loadedConversationId: null,
            conversations: new Map(),
            userHasScrolledUp: false
        },
        touch: {
            startY: null,
            lastY: null
        },
        tabTitle: {
            original: document.title,
            timer: null,
            active: false,
            frame: 0
        },



        initialized: false
    };

    // ============================================================
    //MESSAGE_TYPES
    // ============================================================
    const MESSAGE_SENDER_CLASS = {
        [CONFIG.senderType.contact]: "contact",
        [CONFIG.senderType.admin]: "admin",
        [CONFIG.senderType.bot]: "bot",
        [CONFIG.senderType.system]: "system"
    };

    let pendingAttachments = [];
    let uploadSessionToken = null;
    const pendingAttachmentUploads = new Map();

    const CHAT_ATTACHMENT_CONFIG = {
        maxFiles: 5,
        maxFileSize: 12 * 1024 * 1024,
        maxTotalSize: 25 * 1024 * 1024,

        allowedExtensions: new Set([
            '.jpg', '.jpeg', '.png', '.webp',
            '.pdf', '.csv', '.txt',
            '.doc', '.docx', '.xls', '.xlsx'
        ]),

        allowedMimeTypes: new Set([
            // Images
            'image/jpeg',
            'image/png',
            'image/webp',

            // PDF
            'application/pdf',

            // Text and CSV
            'text/csv',
            'text/plain',
            'application/csv',

            // Word
            'application/msword',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document',

            // Excel
            'application/vnd.ms-excel',
            'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
        ])
    };
    // ============================================================
    // SIGNALR
    // ============================================================

    const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/chat?clientType=customer").withAutomaticReconnect().build();


    // ============================================================
    // STORAGE
    // ============================================================
    function loadStoredSession() {
        state.currentContactId = normalizeNumber(localStorage.getItem(CONFIG.storage.contactId));

        state.currentConversationId = normalizeNumber(localStorage.getItem(CONFIG.storage.conversationId));

        state.guestToken = localStorage.getItem(CONFIG.storage.guestToken) || null;
    }
    function persistContactIdentity(contactId, guestToken) {
        const normalizedContactId = normalizeNumber(contactId);

        state.currentContactId = normalizedContactId;

        if (normalizedContactId) {
            localStorage.setItem(CONFIG.storage.contactId, String(normalizedContactId));
        }

        /*
         * Chỉ update token khi server thực sự
         * cấp token mới.
         *
         * null / undefined:
         * giữ token hiện tại.
         */
        if (guestToken) {
            state.guestToken = guestToken;

            localStorage.setItem(
                CONFIG.storage.guestToken,
                guestToken
            );
        }
    }
    function persistConversation(conversationId, status = null) {
        const normalizedConversationId = normalizeNumber(conversationId);

        state.currentConversationId = normalizedConversationId;

        if (normalizedConversationId) {
            localStorage.setItem(CONFIG.storage.conversationId, String(normalizedConversationId));
        }

        if (status !== null && status !== undefined) {
            state.currentConversationStatus = Number(status);
        }
    }

    function clearConversationIdentity() {
        state.currentConversationId = null;
        state.currentConversationStatus = null;

        localStorage.removeItem(CONFIG.storage.conversationId);
    }

    //USER WHEN LOGOUT
    function clearCustomerIdentity() {
        state.currentContactId = null;
        state.currentConversationId = null;
        state.currentConversationStatus = null;
        state.guestToken = null;

        localStorage.removeItem(CONFIG.storage.contactId);

        localStorage.removeItem(CONFIG.storage.conversationId);

        localStorage.removeItem(CONFIG.storage.guestToken);
    }

    function hasConversationIdentity() {
        return Boolean(state.currentConversationId && state.currentContactId);
    }

    // ============================================================
    // GENERIC HELPERS
    // ============================================================
    function resetAfterAuthenticationLost() {
        clearMessages();

        state.currentContactId = null;
        state.currentConversationId = null;
        state.currentConversationStatus = null;
        state.guestToken = null;

        state.chatSession.hasActiveHistory = false;

        localStorage.removeItem(CONFIG.storage.contactId);

        localStorage.removeItem(CONFIG.storage.conversationId);

        localStorage.removeItem(CONFIG.storage.guestToken);

        resetConversationHistoryState();

        setUnreadMessageCount(0);

        showQuickReplies();
    }
    function isConnectionReady() {
        return (connection.state === signalR.HubConnectionState.Connected);
    }

    function isChatOpen() {
        return DOM.chatWindow?.classList.contains("open") ?? false;
    }

    function hasCurrentConversation() {
        return Boolean(state.currentConversationId && state.currentContactId);
    }


    function normalizeNumber(value) {
        const number = Number(value);

        return Number.isFinite(number) ? number : null;
    }


    function isSameConversation(conversationId) {
        return (Number(state.currentConversationId) === Number(conversationId));
    }


    function escapeHtml(value) {
        const element = document.createElement("div");

        element.textContent = value ?? "";

        return element.innerHTML;
    }


    function scrollBottom(smooth = true) {
        if (!DOM.chatBody) {
            return;
        }
        requestAnimationFrame(() => {
            DOM.chatBody.scrollTo({
                top: DOM.chatBody.scrollHeight,
                behavior: smooth ? "smooth" : "auto"
            });
        });
    }


    function formatMessageTime(date) {
        return new Date(date).toLocaleTimeString(
            "vi-VN",
            {
                hour: "2-digit",
                minute: "2-digit"
            }
        );
    }


    // ============================================================
    // PROMO POPUP
    // ============================================================

    function initPromoPopup() {
        if (!DOM.popup) {
            return;
        }

        cleanupPromoStorage();

        const hiddenUntil = localStorage.getItem(CONFIG.storage.hidePromo);

        if (hiddenUntil) {
            return;
        }

        setTimeout(() => { DOM.popup?.classList.add("show"); }, CONFIG.timing.promoDelay);


        DOM.closePopup?.addEventListener("click", closePromoPopup);

        DOM.laterBtn?.addEventListener("click", closePromoPopup);
    }


    function cleanupPromoStorage() {
        const hiddenUntil =
            localStorage.getItem(
                CONFIG.storage.hidePromo
            );

        if (!hiddenUntil) {
            return;
        }

        if (Date.now() > Number(hiddenUntil)) {
            localStorage.removeItem(
                CONFIG.storage.hidePromo
            );
        }
    }


    function closePromoPopup() {
        DOM.popup?.classList.remove("show");

        if (!DOM.hideToday?.checked) {
            return;
        }

        const tomorrow =
            new Date();

        tomorrow.setHours(
            23,
            59,
            59,
            999
        );

        localStorage.setItem(
            CONFIG.storage.hidePromo,
            String(tomorrow.getTime())
        );
    }

    // ============================================================
    // CONVERSATION HISTORY
    // ============================================================
    function prependHistoricalMessages(messages) {
        if (
            !DOM.chatBody ||
            !Array.isArray(messages) ||
            messages.length === 0
        ) {
            return;
        }
        const previousHeight = DOM.chatBody.scrollHeight;
        const previousTop = DOM.chatBody.scrollTop;

        const fragment = document.createDocumentFragment();

        let previousMessage = null;

        messages.forEach(message => {
            if (!message?.id) {
                return;
            }

            appendMessageWithDate(
                fragment,
                message,
                previousMessage
            );

            previousMessage = message;

            markMessageProcessed(message.id);
        });

        DOM.chatBody.prepend(fragment);

        normalizeDateSeparators();

        const newHeight = DOM.chatBody.scrollHeight;

        DOM.chatBody.scrollTop = previousTop + (newHeight - previousHeight);
    }
    function showPreviousConversationButton() {
        const loader = DOM.chatHistoryLoader;

        if (!loader) {
            return;
        }

        loader.classList.remove("hidden");

        if (DOM.loadPreviousConversationBtn) {
            DOM.loadPreviousConversationBtn.disabled = false;
            DOM.loadPreviousConversationBtn.textContent =
                "Xem cuộc trò chuyện trước";
        }
    }


    function hidePreviousConversationButton() {
        DOM.chatHistoryLoader?.classList.add("hidden");
    }


    function getConversationPaging(conversationId) {
        const id = Number(conversationId);

        if (!id) {
            return null;
        }

        let paging = state.history.conversations.get(id);

        if (!paging) {
            paging = {
                conversationId: id,
                oldestMessageId: null,
                hasMore: true,
                loading: false
            };

            state.history.conversations.set(id, paging);
        }

        return paging;
    }

    async function preparePreviousConversationFor(conversationId) {
        if (!conversationId) {
            return;
        }

        try {
            const response = await fetch(
                `/chat/${conversationId}/previous`,
                {
                    method: "GET",
                    headers: getChatHeaders(),
                    credentials: "same-origin"
                }
            );

            if (response.status === 204) {
                state.history.previousConversationId = null;
                state.history.hasPrevious = false;

                hidePreviousConversationButton();
                return;
            }

            if (!response.ok) {
                throw new Error(
                    `Previous conversation failed: ${response.status}`
                );
            }

            const previous = await response.json();

            const previousId =
                Number(previous?.id) || null;

            state.history.previousConversationId =
                previousId;

            state.history.hasPrevious =
                Boolean(previousId);

            /*
             * Không show trực tiếp ở đây.
             *
             * Button chỉ được show nếu user thực sự
             * đang ở đầu conversation hiện tại.
             */
            syncPreviousConversationButton();

        } catch (error) {
            state.history.previousConversationId = null;
            state.history.hasPrevious = false;

            hidePreviousConversationButton();
        }
    }


    function syncPreviousConversationButton() {

        if (
            !state.history.hasPrevious ||
            !state.history.userHasScrolledUp
        ) {
            hidePreviousConversationButton();
            return;
        }

        if (!DOM.chatBody) {
            hidePreviousConversationButton();
            return;
        }

        if (DOM.chatBody.scrollTop > 120) {
            hidePreviousConversationButton();
            return;
        }

        showPreviousConversationButton();
    }
    //TITLE ANIMATION
    function startUnreadTitle() {
        if (state.tabTitle.active) return;

        state.tabTitle.active = true;
        state.tabTitle.frame = 0;

        state.tabTitle.timer = setInterval(() => {
            const count = state.unreadMessageCount;

            if (count <= 0) {
                stopUnreadTitle();
                return;
            }

            state.tabTitle.frame++;

            document.title =
                state.tabTitle.frame % 2 === 0
                    ? state.tabTitle.original
                    : `💬 (${count}) Tin nhắn mới`;
        }, 1200);
    }

    function stopUnreadTitle() {
        if (state.tabTitle.timer) {
            clearInterval(state.tabTitle.timer);
            state.tabTitle.timer = null;
        }

        state.tabTitle.active = false;
        state.tabTitle.frame = 0;

        document.title = state.tabTitle.original;
    }

    // ============================================================
    // UNREAD BADGE
    // ============================================================

    function setUnreadMessageCount(count) {
        state.unreadMessageCount =
            Math.max(
                0,
                Number(count) || 0
            );

        renderUnreadBadge();

        if (state.unreadMessageCount > 0) {
            startUnreadTitle();
        }
        else {
            stopUnreadTitle();
        }
    }


    function incrementUnreadMessage() {
        state.unreadMessageCount++;

        renderUnreadBadge();

        startUnreadTitle();
    }


    function renderUnreadBadge() {
        if (!DOM.chatBadge) {
            return;
        }

        if (state.unreadMessageCount <= 0) {
            DOM.chatBadge.textContent = "";
            DOM.chatBadge.hidden = true;

            return;
        }

        DOM.chatBadge.textContent =
            state.unreadMessageCount > 99
                ? "99+"
                : String(state.unreadMessageCount);

        DOM.chatBadge.hidden = false;
    }


    // ============================================================
    // CHAT OPEN / CLOSE
    // ============================================================
    async function openChat() {
        if (!DOM.chatWindow) {
            return;
        }

        DOM.chatWindow.classList.add("open");
        if (state.chatSession.isInvalid) {
            return;
        }
        try {
            /*
             * =====================================================
             * 1. AUTHENTICATED CUSTOMER
             * =====================================================
             *
             * Khi đã login:
             * - Không phụ thuộc guestToken
             * - Backend sẽ xác định ChatContact từ userId
             */
            if (CHAT_CONFIG.isAuthenticated) {

                /*
                 * Không có conversation
                 * => New chat
                 */
                if (!state.currentConversationId) {
                    clearMessages();

                    state.chatSession.hasActiveHistory =
                        false;

                    showQuickReplies();

                    return;
                }

                /*
                 * Có conversation:
                 * cho phép xem history kể cả Resolved/Closed.
                 */
                if (!DOM.chatBody?.children.length) {
                    await loadConversationMessages({
                        initial: true
                    });
                }

                if (
                    state.currentConversationId &&
                    state.currentContactId
                ) {
                    const status = await joinConversation();

                    if (status !== null) {
                        updateCurrentConversationStatus(
                            status
                        );
                    }

                    const marked =
                        await markConversationAsRead();

                    if (marked) {
                        setUnreadMessageCount(0);
                    }
                }
                return;
            }


            /*
             * =====================================================
             * 2. GUEST
             * =====================================================
             */

            /*
             * Guest không có token thì KHÔNG được sử dụng
             * contactId / conversationId cũ.
             *
             * contactId chỉ là identity reference.
             * guestToken mới là credential để truy cập.
             */
            if (
                state.currentContactId &&
                state.currentConversationId &&
                !state.guestToken
            ) {
                showChatSessionInvalid();
                return;
            }


            /*
             * =====================================================
             * 3. GUEST NEW CHAT
             * =====================================================
             */
            if (!state.currentConversationId) {
                clearMessages();

                state.chatSession.hasActiveHistory =
                    false;

                showQuickReplies();

                return;
            }


            /*
             * =====================================================
             * 4. GUEST EXISTING CHAT
             * =====================================================
             *
             * Đến đây guest phải có:
             *
             * contactId
             * conversationId
             * guestToken
             */
            if (!state.currentContactId ||
                !state.guestToken) {

                clearCustomerIdentity();

                state.currentConversationStatus =
                    null;

                state.chatSession.hasActiveHistory =
                    false;

                resetConversationHistoryState();
                clearMessages();

                showQuickReplies();

                return;
            }


            /*
             * =====================================================
             * 5. LOAD HISTORY
             * =====================================================
             */
            if (!DOM.chatBody?.children.length) {
                await loadConversationMessages({
                    initial: true
                });
            }


            /*
             * =====================================================
             * 6. JOIN CONVERSATION
             * =====================================================
             */
            if (
                state.currentConversationId &&
                state.currentContactId
            ) {
                const status =
                    await joinConversation();

                if (status !== null) {
                    updateCurrentConversationStatus(
                        status
                    );
                }

                const marked =
                    await markConversationAsRead();

                if (marked) {
                    setUnreadMessageCount(0);
                }
            }
        }
        catch (error) {
            console.error(
                "Không thể mở chat:",
                error
            );
        }
    }

    async function loadConversationMessages({ initial = false } = {}) {
        if (!state.currentConversationId || state.messagePaging.loading) {
            return;
        }

        if (initial) {
            state.chatSession.hasActiveHistory = false;
        }

        state.messagePaging.loading = true;

        const conversationId = state.currentConversationId;

        try {
            const paging = getConversationPaging(conversationId);

            const { messages, hasMore } = await fetchConversationMessages(
                conversationId,
                {
                    limit: state.messagePaging.limit,
                    before: initial ? null : state.messagePaging.oldestMessageId
                }
            );

            /*
             * Initial load của conversation hiện tại
             */
            if (initial) {
                clearMessages();

                state.history.loadedConversationId = conversationId;

                paging.oldestMessageId = null;
                paging.hasMore = true;
                paging.loading = false;
            }

            /*
             * Không có message
             */
            if (!messages.length) {
                state.messagePaging.hasMore = false;

                if (initial) {
                    initializeQuickRepliesFromMessages([]);
                }

                await preparePreviousConversationFor(
                    conversationId
                );

                return;
            }

            /*
             * Initial
             */
            if (initial) {
                renderInitialMessages(messages);

                initializeQuickRepliesFromMessages(
                    messages
                );
            }
            /*
             * Load thêm message cũ
             */
            else {
                prependHistoricalMessages(messages);
            }

            /*
             * Message cũ nhất
             */
            const oldestMessageId = messages[0]?.id ?? state.messagePaging.oldestMessageId;

            state.messagePaging.oldestMessageId = oldestMessageId;

            state.messagePaging.hasMore = hasMore;

            paging.oldestMessageId = oldestMessageId;

            paging.hasMore = hasMore;

            /*
             * Đã hết message của conversation hiện tại
             */
            if (!hasMore) {
                await preparePreviousConversationFor(
                    conversationId
                );
            }
            state.chatSession.hasActiveHistory = true;
        }
        catch (error) {
            console.error("loadConversationMessages:", error);
        }
        finally {
            state.messagePaging.loading = false;
        }
    }

    async function loadOlderMessagesForConversation(conversationId) {
        const id = Number(conversationId);

        if (!id) {
            return;
        }

        const paging =
            getConversationPaging(id);

        if (!paging || paging.loading || !paging.hasMore) {
            return;
        }

        paging.loading = true;

        try {
            const {
                messages,
                hasMore
            } = await fetchConversationMessages(
                id,
                {
                    limit: state.messagePaging.limit,
                    before: paging.oldestMessageId
                }
            );

            /*
             * Không còn message cũ hơn
             */
            if (!messages.length) {
                paging.hasMore = false;

                await preparePreviousConversationFor(id);

                return;
            }

            /*
             * Append message cũ vào đầu chat.
             * Hàm này giữ nguyên viewport.
             */
            prependHistoricalMessages(messages);

            /*
             * API trả cũ -> mới,
             * phần tử đầu tiên là oldest.
             */
            paging.oldestMessageId =
                messages[0]?.id ??
                paging.oldestMessageId;

            paging.hasMore =
                hasMore;

            /*
             * Đã hết message của conversation này
             */
            if (!paging.hasMore) {
                await preparePreviousConversationFor(id);
            }
        }
        catch (error) {
            console.error(
                "Load older conversation messages failed:",
                error
            );
        }
        finally {
            paging.loading = false;
        }
    }

    async function fetchConversationMessages(
        conversationId,
        {
            limit,
            before = null
        } = {}
    ) {
        const params = new URLSearchParams();

        params.set(
            "limit",
            String(limit)
        );

        if (before) {
            params.set(
                "before",
                String(before)
            );
        }

        const response = await fetch(
            `/chat/${conversationId}/messages?${params}`,
            {
                method: "GET",
                headers: getChatHeaders(),
                credentials: "same-origin"
            }
        );

        if (!response.ok) {
            throw new Error(
                `Không thể tải messages. Status: ${response.status}`
            );
        }

        const result = await response.json();

        const messages =
            Array.isArray(result)
                ? result
                : result.items ?? [];

        const hasMore =
            Array.isArray(result)
                ? messages.length >= limit
                : Boolean(result.hasMore);

        return {
            messages,
            hasMore
        };
    }
    function closeChat() {
        stopTyping();
        hideTyping();

        DOM.chatWindow?.classList.remove("open");
        DOM.chatToggle?.classList.remove("active");
    }


    function initChatToggle() {
        DOM.chatToggle?.addEventListener(
            "click",
            () => {
                if (isChatOpen()) {
                    closeChat();
                } else {
                    openChat();
                }
            }
        );

        DOM.chatClose?.addEventListener(
            "click",
            closeChat
        );

        //DOM.chatForm?.addEventListener("submit", handleSendMessage);
        // Notification
        DOM.notificationLater?.addEventListener(
            "click",
            () => {
                snoozeNotificationPrompt();
            }
        );

        DOM.notificationAllow?.addEventListener(
            "click",
            async () => {
                if (!("Notification" in window)) {
                    DOM.notificationPrompt.hidden = true;
                    return;
                }

                const permission = await Notification.requestPermission();

                if (permission === "granted") {
                    localStorage.removeItem(
                        CONFIG.storage.notificationPromptSnoozeUntil
                    );

                    const message =
                        state.notification.pendingMessage;

                    state.notification.pendingMessage = null;

                    DOM.notificationPrompt.classList.remove("is-visible");

                    setTimeout(() => {
                        DOM.notificationPrompt.hidden = true;
                    }, 250);

                    if (message) {
                        showMessageNotification(message);
                    }

                    return;
                }

                markNotificationPrompted();
            }
        );
    }


    // ============================================================
    // ADMIN PRESENCE
    // ============================================================

    function setChatAdminStatus(isOnline) {
        const status =
            DOM.chatStatusText;

        if (!status) {
            return;
        }

        const dot =
            DOM.adminStatusDot ||
            status.querySelector(
                "span:first-child"
            );

        const label =
            status.querySelector(
                ".status-label"
            );

        DOM.adminStatusDot?.classList.toggle(
            "online",
            isOnline
        );

        DOM.adminStatusDot?.classList.toggle(
            "offline",
            !isOnline
        );

        status.classList.toggle(
            "online",
            isOnline
        );

        status.classList.toggle(
            "offline",
            !isOnline
        );

        dot?.classList.toggle(
            "online",
            isOnline
        );

        dot?.classList.toggle(
            "offline",
            !isOnline
        );

        if (label) {
            label.textContent =
                isOnline
                    ? "Đang trực tuyến"
                    : "Đang ngoại tuyến";
        }
    }


    // ============================================================
    // TYPING
    // ============================================================

    function handleTyping() {
        const value =
            DOM.chatInput?.value.trim();
        if (!value) {
            stopTyping();

            return;
        }

        if (!state.isTyping) {
            state.isTyping = true;

            sendTypingStatus(true);
        }

        clearTimeout(
            state.typingTimer
        );

        state.typingTimer =
            setTimeout(
                stopTyping,
                CONFIG.timing.typingTimeout
            );
    }


    function stopTyping() {
        clearTimeout(
            state.typingTimer
        );

        state.typingTimer = null;

        if (!state.isTyping) {
            return;
        }

        state.isTyping = false;

        sendTypingStatus(false);
    }


    async function sendTypingStatus(isTyping) {
        if (!state.currentConversationId || !state.currentContactId || !isConnectionReady()) {
            return;
        }

        await connection.invoke("SendTyping",
            state.currentConversationId, state.currentContactId, state.guestToken, isTyping
        );
    }


    function showTyping() {
        if (!DOM.typing) {
            return;
        }
        DOM.typing.hidden = false;

        scrollBottom(true);
    }


    function hideTyping() {
        if (!DOM.typing) {
            return;
        }

        DOM.typing.hidden = true;
    }


    // ============================================================
    // SIGNALR CONNECTION
    // ============================================================
    let chatResizeObserver;

    async function startChatConnection() {
        try {
            loadStoredSession();

            await ensureSignalRConnection();

            await initializeChatSession();
        }
        catch (error) {
            console.log(
                "Chat initialization failed:",
                error
            );

            scheduleReconnect();
        }
    }


    async function ensureSignalRConnection() {
        if (isConnectionReady()) {
            return;
        }

        await connection.start();

        const isAdminOnline =
            await connection.invoke(
                "IsAnyAdminOnline"
            );

        setChatAdminStatus(
            Boolean(isAdminOnline)
        );
    }

    async function initializeChatSession() {
        if (state.chatSession.isInvalid) {
            return;
        }

        /*
         * =========================================================
         * USER KHÔNG AUTHENTICATED
         *
         * Giữ nguyên Guest flow hiện tại.
         * =========================================================
         */
        if (!CHAT_CONFIG.isAuthenticated) {

            state.currentConversationStatus = null;
            state.chatSession.hasActiveHistory = false;

            resetConversationHistoryState();
            clearMessages();

            showQuickReplies();

            return;
        }

        /*
         * =========================================================
         * USER VỪA AUTHENTICATED
         *
         * Nếu localStorage vẫn còn:
         *
         *   contactId
         *   guestToken
         *   conversationId
         *
         * thì đây là Guest session vừa login.
         *
         * PHẢI claim / merge trước.
         *
         * Tuyệt đối không JoinConversation() ở bước này.
         * =========================================================
         */
        if (
            state.currentContactId &&
            state.guestToken
        ) {
            await initializeAuthenticatedChat();

            return;
        }

        /*
         * =========================================================
         * AUTHENTICATED SESSION BÌNH THƯỜNG
         *
         * Không còn guestToken.
         * Chỉ restore authenticated conversation nếu có.
         * =========================================================
         */
        if (!hasConversationIdentity()) {
            showQuickReplies();

            return;
        }

        const status = await joinConversation();

        if (status === null) {
            return;
        }

        state.currentConversationStatus = status;

        await loadUnreadCount();

        await loadConversationMessages({
            initial: true
        });

        updateCurrentConversationStatus(status);
    }

    async function initializeAuthenticatedChat() {
        try {
            const result = await fetch(
                "/chat/start",
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        inboxId: CHAT_CONFIG.inboxId,

                        contactId:
                            state.currentContactId || null,

                        conversationId:
                            state.currentConversationId || null,

                        guestToken:
                            state.guestToken || null
                    })
                }
            );

            if (!result.ok) {
                throw new Error(
                    `Chat start failed: ${result.status}`
                );
            }

            const data = await result.json();

            // =====================================================
            // Canonical Contact
            // =====================================================

            if (data.contactId) {
                state.currentContactId =
                    normalizeNumber(data.contactId);

                localStorage.setItem(
                    CONFIG.storage.contactId,
                    String(state.currentContactId)
                );
            }

            // =====================================================
            // Conversation
            //
            // Backend quyết định:
            //
            // Open/Pending -> giữ
            // Resolved/Closed -> null
            // =====================================================

            const conversationId =
                normalizeNumber(data.conversationId);

            if (conversationId) {

                state.currentConversationId =
                    conversationId;

                localStorage.setItem(
                    CONFIG.storage.conversationId,
                    String(conversationId)
                );

            } else {

                state.currentConversationId = null;
                state.currentConversationStatus = null;

                localStorage.removeItem(
                    CONFIG.storage.conversationId
                );
            }

            // =====================================================
            // Authenticated không còn dùng guest token
            // =====================================================

            state.guestToken = null;

            localStorage.removeItem(
                CONFIG.storage.guestToken
            );

            // =====================================================
            // Reset UI
            // =====================================================

            clearMessages();

            state.chatSession.hasActiveHistory = false;
            state.chatSession.isInvalid = false;

            // =====================================================
            // Nếu conversation vẫn active
            // => join + load lại conversation
            // =====================================================

            if (conversationId) {

                const status =
                    await joinConversation();

                if (status === null) {
                    return data;
                }

                state.currentConversationStatus =
                    status;

                await loadUnreadCount();

                await loadConversationMessages({
                    initial: true
                });

                updateCurrentConversationStatus(status);

                return data;
            }

            // =====================================================
            // Không còn conversation active
            // => chat mới
            // =====================================================

            showQuickReplies();

            return data;
        }
        catch (error) {
            console.error(
                "initializeAuthenticatedChat failed:",
                error
            );

            throw error;
        }
    }
    function resetToNewChatState() {
        clearMessages();

        /*
         * Chỉ reset conversation.
         *
         * Không xóa Contact.
         * Không xóa guestToken.
         */
        state.currentConversationId = null;
        state.currentConversationStatus = null;

        state.chatSession.hasActiveHistory =
            false;

        state.messagePaging.oldestMessageId = null;
        state.messagePaging.hasMore = true;
        state.messagePaging.loading = false;

        resetConversationHistoryState();

        clearConversationIdentity();

        setUnreadMessageCount(0);

        showQuickReplies();
    }

async function joinConversation() {
    if (!state.currentConversationId || !state.currentContactId || !isConnectionReady()) {
        return null;
    }
    try {
        const guestToken = CHAT_CONFIG.isAuthenticated ? null : state.guestToken;
        const status =
            await connection.invoke(
                "JoinConversation",
                Number(state.currentConversationId),
                Number(state.currentContactId),
                guestToken
            );

        state.currentConversationStatus =
            Number(status);

        updateCurrentConversationStatus(
            state.currentConversationStatus
        );
        return state.currentConversationStatus;
    }
    catch (error) {
        console.error(
            "JoinConversation failed:",
            error
        );

        return null;
    }
}
    function scheduleReconnect() {
        if (state.reconnectTimer) {
            return;
        }

        state.reconnectTimer =
            setTimeout(
                async () => {
                    state.reconnectTimer = null;

                    await startChatConnection();
                },
                CONFIG.timing.reconnectDelay
            );
    }


    // ============================================================
    // SIGNALR EVENTS
    // ============================================================

    function registerSignalREvents() {
        connection.on(
            "chat.admin.online",
            () => {
                setChatAdminStatus(true);
            }
        );


        connection.on(
            "chat.admin.offline",
            () => {
                setChatAdminStatus(false);
            }
        );


        connection.onreconnecting(
            () => {
                stopTyping();
                hideTyping();
            }
        );


        connection.onreconnected(
            handleSignalRReconnected
        );


        connection.on(
            "chat.message.received", handleMessageReceived
        );


        connection.on(
            "chat.message.status.updated",
            handleMessageStatusUpdated
        );


        connection.on(
            "chat.conversation.status.updated",
            handleConversationStatusUpdated
        );


        connection.on(
            "chat.typing",
            handleTypingEvent
        );
        connection.on(
            "chat.conversation.created",
            handleConversationCreated
        );

        connection.onclose(
            () => {
                hideTyping();
            }
        );
    }
    function handleConversationCreated(conversation) {
        if (!conversation?.id) {
            return;
        }

        const conversationId =
            Number(conversation.id);

        const contactId =
            Number(conversation.contactId);

        if (
            state.currentContactId &&
            contactId &&
            contactId !== Number(state.currentContactId)
        ) {
            return;
        }

        if (
            Number(state.currentConversationId) !==
            conversationId
        ) {
            return;
        }

        const status = Number(conversation.status);

        state.currentConversationStatus = status;

        updateCurrentConversationStatus(status);
    }

    async function handleSignalRReconnected() {
        hideTyping();

        /*
         * Không có conversation/contact:
         *
         * - Không JoinConversation
         * - Không restore history
         * - Không tạo DB
         *
         * Đây là trạng thái bình thường của guest mới.
         */
        if (!hasConversationIdentity()) {
            return;
        }

        try {
            /*
             * Reconnect chỉ restore session hiện tại.
             *
             * Không được tạo identity mới tại đây.
             */
            const status = await joinConversation();

            if (status === null) {
                /*
                 * Conversation hiện tại không còn access.
                 *
                 * Không tự refresh token.
                 * Không tạo conversation mới.
                 */
                return;
            }

            state.currentConversationStatus = status;

            await loadConversationMessages({
                initial: true
            });
            await loadUnreadCount();

            updateCurrentConversationStatus(status);
        }
        catch (error) {
            console.error(
                "Conversation synchronization failed:",
                error
            );
        }
    }


    async function handleMessageReceived(message) {
        if (!message?.id) {
            return false;
        }

        const messageConversationId =
            normalizeNumber(
                message.conversationId
            );

        const currentConversationId =
            normalizeNumber(
                state.currentConversationId
            );

        /*
         * Không thuộc conversation hiện tại
         * thì không render vào chatbox.
         */
        if (
            messageConversationId &&
            currentConversationId &&
            messageConversationId !==
            currentConversationId
        ) {
            return false;
        }

        hideTyping();

        const added =
            addMessage(
                message,
                true
            );

        if (!added) {
            return false;
        }

        const senderType =
            Number(message.senderType);

        if (
            senderType ===
            CONFIG.senderType.system
        ) {
            return true;
        }

        const isIncoming =
            senderType ===
            CONFIG.senderType.admin ||
            senderType ===
            CONFIG.senderType.bot;

        if (!isIncoming) {
            return true;
        }

        await acknowledgeMessageDelivered(message);

        if (isChatOpen()) {
            const marked =
                await markConversationAsRead();

            if (marked) {
                setUnreadMessageCount(0);
            }
        }
        else {
            incrementUnreadMessage();
        }

        if (shouldShowNotificationPrompt()) {
            state.notification.pendingMessage = message;

            showNotificationPrompt();

            return true;
        }

        if (Notification.permission === "granted") {
            showMessageNotification(message);
        }


        return true;
    }

    function handleMessageStatusUpdated(data) {
        if (
            Array.isArray(data.messageIds)
        ) {
            data.messageIds.forEach(
                messageId => {
                    updateMessageStatus(
                        messageId,
                        data.status
                    );
                }
            );

            return;
        }

        updateMessageStatus(
            data.messageId,
            data.status
        );
    }


    function handleConversationStatusUpdated(data) {
        const conversationId = normalizeNumber(data?.conversationId);

        if (conversationId !== normalizeNumber(state.currentConversationId)) {
            return;
        }

        updateCurrentConversationStatus(
            data.status
        );

        /*
         * Resolve:
         *
         * - giữ conversationId
         * - giữ history
         * - không reopen
         * - composer chuyển sang "new conversation"
         *
         * Khi user send tiếp:
         * server tạo conversation mới.
         */
        if (Number(data.status) === CONFIG.conversationStatus.resolved) {
            state.chatSession.hasActiveHistory =
                true;
        }
    }
    function updateCurrentConversationStatus(status) {
        const normalizedStatus =
            Number(status);

        state.currentConversationStatus =
            normalizedStatus;

        const isResolved =
            normalizedStatus ===
            CONFIG.conversationStatus.resolved;

        const isClosed =
            normalizedStatus ===
            CONFIG.conversationStatus.closed;

        const statusName =
            getConversationStatusName(
                normalizedStatus
            );

        if (DOM.chatWindow) {
            DOM.chatWindow.dataset.status =
                statusName;
        }

        if (DOM.chatStatusBadge) {
            DOM.chatStatusBadge.dataset.status =
                statusName;

            DOM.chatStatusBadge.classList.remove(
                "open",
                "pending",
                "resolved",
                "closed"
            );

            DOM.chatStatusBadge.classList.add(
                statusName
            );
        }

        /*
         * Không disable composer chỉ vì Resolved.
         *
         * Resolved/Closed đều không được reopen.
         * Khi user gửi:
         *
         *     Server tạo conversation mới.
         */
        if (DOM.chatInput) {
            DOM.chatInput.disabled = false;

            if (isResolved || isClosed) {
                DOM.chatInput.placeholder = "Nhập tin nhắn để bắt đầu cuộc trò chuyện mới...";
            }
            else {
                DOM.chatInput.placeholder = "Nhập tin nhắn...";
            }
        }

        if (DOM.sendBtn) {
            DOM.sendBtn.disabled = false;
        }

        /*
         * Closed/Resolved vẫn chỉ là trạng thái của
         * conversation hiện tại.
         *
         * Không xóa conversationId ở đây.
         *
         * Chỉ khi user bắt đầu session mới hoặc server
         * trả về conversation mới thì client chuyển ID.
         */
    }
    function handleTypingEvent(data) {
        if (
            !isSameConversation(
                data.conversationId
            )
        ) {
            return;
        }

        const senderType =
            Number(data.senderType);

        const isAdminOrBot =
            senderType ===
            CONFIG.senderType.admin ||
            senderType ===
            CONFIG.senderType.bot;

        if (!isAdminOrBot) {
            return;
        }

        if (data.isTyping) {
            showTyping();
        } else {
            hideTyping();
        }
    }


    // ============================================================
    // API
    // ============================================================

    function getChatHeaders() {
        return {
            "Accept": "application/json",

            "X-Chat-Contact-Id":
                state.currentContactId
                    ? String(state.currentContactId)
                    : "",

            "X-Chat-Guest-Token":
                state.guestToken || ""
        };
    }


    async function loadPreviousConversation() {
        if (
            state.history.loading ||
            !state.history.hasPrevious ||
            !state.history.previousConversationId
        ) {
            return;
        }

        const conversationId =
            Number(state.history.previousConversationId);

        if (!conversationId) {
            return;
        }

        const paging =
            getConversationPaging(conversationId);

        if (!paging || paging.loading) {
            return;
        }

        state.history.loading = true;
        paging.loading = true;

        hidePreviousConversationButton();

        try {
            const params = new URLSearchParams();

            params.set(
                "limit",
                String(state.messagePaging.limit)
            );

            const response = await fetch(
                `/chat/${conversationId}/messages?${params}`,
                {
                    method: "GET",
                    headers: getChatHeaders(),
                    credentials: "same-origin"
                }
            );

            if (!response.ok) {
                throw new Error(
                    `Load previous conversation failed: ${response.status}`
                );
            }

            const result = await response.json();

            // QUAN TRỌNG:
            // API của bạn dùng "items", không phải "messages"
            const messages =
                Array.isArray(result)
                    ? result
                    : result.items ?? [];

            const hasMore =
                Array.isArray(result)
                    ? messages.length >= state.messagePaging.limit
                    : Boolean(result.hasMore);

            if (!messages.length) {
                paging.hasMore = false;
                paging.oldestMessageId = null;

                state.history.loadedConversationId =
                    conversationId;

                await preparePreviousConversationFor(
                    conversationId
                );

                return;
            }

            // Prepend conversation cũ vào đầu chat
            prependHistoricalMessages(messages);

            paging.oldestMessageId =
                messages[0]?.id ?? null;

            paging.hasMore = hasMore;

            state.history.loadedConversationId =
                conversationId;

            if (paging.hasMore) {
                /*
                 * Conversation cũ này vẫn còn message
                 * ở phía trước.
                 *
                 * Chưa được phép hiện:
                 * "Xem cuộc trò chuyện trước"
                 */
                state.history.previousConversationId = null;
                state.history.hasPrevious = false;

                hidePreviousConversationButton();
            }
            else {
                /*
                 * Đã load hết conversation này.
                 * Tìm conversation trước nó.
                 */
                await preparePreviousConversationFor(
                    conversationId
                );
            }
        }
        catch (error) {
            console.error(
                "loadPreviousConversation:",
                error
            );
        }
        finally {
            paging.loading = false;
            state.history.loading = false;
        }
    }
    function renderInitialMessages(messages) {
        if (!DOM.chatBody) {
            return;
        }

        DOM.chatBody.innerHTML = "";

        if (
            !Array.isArray(messages) ||
            messages.length === 0
        ) {
            initializeQuickRepliesFromMessages(
                messages
            );

            return;
        }

        const fragment =
            document.createDocumentFragment();

        let previousMessage = null;

        messages.forEach(message => {
            if (!message?.id) {
                return;
            }

            appendMessageWithDate(
                fragment,
                message,
                previousMessage
            );

            previousMessage =
                message;

            markMessageProcessed(
                message.id
            );
        });

        DOM.chatBody.appendChild(
            fragment
        );

        initializeQuickRepliesFromMessages(
            messages
        );
        scrollToBottom();
    }
    function normalizeDateSeparators() {
        if (!DOM.chatBody) {
            return;
        }

        const children =
            Array.from(
                DOM.chatBody.children
            );

        let previousMessage = null;

        children.forEach(element => {

            if (
                !element.classList.contains(
                    "message-row"
                )
            ) {
                return;
            }

            const messageId =
                element.dataset.messageId;

            if (!messageId) {
                return;
            }

            const separator =
                element.previousElementSibling;

            if (
                separator?.classList.contains(
                    "chat-date"
                )
            ) {
                if (
                    previousMessage &&
                    isSameMessageDate(
                        previousMessage.createdAt,
                        element.dataset.createdAt
                    )
                ) {
                    separator.remove();
                }
            }

            previousMessage = {
                id: messageId,
                createdAt:
                    element.dataset.createdAt
            };
        });
    }
    function scrollToBottom() {
        if (!DOM.chatBody) return;

        requestAnimationFrame(() => {
            DOM.chatBody.scrollTop = DOM.chatBody.scrollHeight;
        });
    }

    function resetChatBox() {
        /*
         * 1. Clear identity
         */
        clearCustomerIdentity();

        /*
         * 2. Clear conversation state
         */
        state.currentConversationStatus = null;
        state.chatSession.hasActiveHistory = false;
        state.chatSession.isInvalid = false;

        resetConversationHistoryState();

        /*
         * 3. Clear messages
         */
        clearMessages();

        /*
         * 4. Hide invalid-session UI
         */
        DOM.chatSessionReset?.setAttribute(
            "hidden",
            ""
        );

        /*
         * 5. Enable composer
         */
        DOM.chatInput?.removeAttribute("disabled");
        DOM.sendBtn?.removeAttribute("disabled");

        DOM.attachmentBtn?.removeAttribute("disabled");
        DOM.emojiBtn?.removeAttribute("disabled");

        /*
         * 6. Hiện quick replies
         */
        showQuickReplies();

        /*
         * 7. Focus input
         */
        DOM.chatInput?.focus();
    }
    function showChatSessionInvalid() {
        state.chatSession.isInvalid = true;

        DOM.chatSessionReset?.removeAttribute("hidden");

        /*
         * Không cho user tiếp tục gửi message
         */
        DOM.chatInput?.setAttribute("disabled", "disabled");
        DOM.sendBtn?.setAttribute("disabled", "disabled");

        /*
         * Không cho typing / attachment / emoji
         */
        DOM.attachmentBtn?.setAttribute("disabled", "disabled");
        DOM.emojiBtn?.setAttribute("disabled", "disabled");

        stopTyping();
        hideQuickReplies();
    }
    function clearMessages() {
        DOM.chatBody?.replaceChildren();

        clearQuickReplyTimer();

        state.processedMessageIds.clear();

        state.quickReplies.lastMessageAt = null;

        resetConversationHistoryState();
    }
    function resetConversationHistoryState() {

        state.history.previousConversationId = null;
        state.history.hasPrevious = false;
        state.history.loading = false;

        state.history.loadedConversationId =
            null;

        state.history.userHasScrolledUp =
            false;

        state.history.conversations.clear();

        hidePreviousConversationButton();
    }

    async function loadUnreadCount() {
        if (!state.currentConversationId) {
            setUnreadMessageCount(0);

            return;
        }

        try {
            const response =
                await fetch(
                    `/chat/${state.currentConversationId}/unread-count`,
                    {
                        method: "GET",

                        headers:
                            getChatHeaders(),

                        credentials:
                            "same-origin"
                    }
                );

            if (!response.ok) {
                throw new Error(
                    `Failed to load unread count. Status: ${response.status}`
                );
            }

            const result =
                await response.json();
                
            setUnreadMessageCount(
                result?.data ?? result
            );
        }
        catch (error) {
            console.log(
                "Load unread count failed:",
                error
            );
        }
    }


    async function markConversationAsRead() {
        if (!hasConversationIdentity()) {
            return false;
        }

        try {
            const response =
                await fetch(
                    `/chat/${state.currentConversationId}/read`,
                    {
                        method: "POST",

                        headers:
                            getChatHeaders(),

                        credentials:
                            "same-origin"
                    }
                );

            if (!response.ok) {
                throw new Error(
                    `Mark read failed. Status: ${response.status}`
                );
            }

            return true;
        }
        catch (error) {
            console.log(
                "Mark conversation read failed:",
                error
            );

            return false;
        }
    }


    async function acknowledgeMessageDelivered(message) {
        if (
            !message?.id ||
            !state.currentContactId ||
            !isConnectionReady()
        ) {
            return;
        }

        try {
            await connection.invoke(
                "CustomerMessageDelivered",
                Number(message.id),
                Number(state.currentContactId),
                state.guestToken || null
            );
        }
        catch (error) {
            console.log(
                "Acknowledge message delivered failed:",
                error
            );
        }
    }


    // ============================================================
    // SEND MESSAGE
    // ============================================================

    function isMessageProcessed(messageId) {
        return state.processedMessageIds.has(
            String(messageId)
        );
    }
    function markMessageProcessed(messageId) {
        if (!messageId) {
            return;
        }

        const id =
            String(messageId);

        if (
            state.processedMessageIds.has(id)
        ) {
            return;
        }

        state.processedMessageIds.add(id);

        if (
            state.processedMessageIds.size >
            CONFIG.limits.processedMessageIds
        ) {
            const firstId =
                state.processedMessageIds
                    .values()
                    .next()
                    .value;

            if (firstId !== undefined) {
                state.processedMessageIds.delete(
                    firstId
                );
            }
        }
    }

    async function sendMessage(text) {
        const message = String(text ?? "").trim();

        if (!message && pendingAttachments.length === 0) {
            return;
        }

        if (!isConnectionReady()) {
            console.log(
                "SignalR not connected."
            );

            return;
        }

        stopTyping();

        const originalValue = DOM.chatInput?.value ?? "";
        let sendSucceeded = false;
        try {
            // =====================================================
            // 1. UPLOAD ATTACHMENTS
            // =====================================================
            // Upload thành công chưa có nghĩa là message đã gửi.
            // Chỉ xóa preview sau khi Hub xác nhận thành công.

            const uploadIds = await uploadPendingAttachments();
            // =====================================================
            // 2. SEND MESSAGE
            // =====================================================

            const result =
                await connection.invoke("SendCustomerMessage", CHAT_CONFIG.inboxId,
                    state.currentConversationId || null, state.currentContactId || null,
                    message, state.guestToken || null, uploadSessionToken, uploadIds
                );

            if (!result) {
                throw new Error("Server không trả về kết quả gửi message.");
            }
            sendSucceeded = true;
            uploadSessionToken = null;
            clearPendingAttachments();
            // =====================================================
            // 3. SYNC CONTACT
            // =====================================================

            if (result.contactId) {
                persistContactIdentity(result.contactId, result.guestToken);
            }

            // =====================================================
            // 4. SYNC CONVERSATION
            // =====================================================

            const newConversationId = normalizeNumber(result.conversationId);

            const previousConversationId = normalizeNumber(state.currentConversationId);

            const conversationChanged = Boolean(newConversationId && newConversationId !== previousConversationId);

            if (newConversationId) {
                state.currentConversationId = newConversationId;

                state.currentConversationStatus = Number(result.status);

                persistConversation(newConversationId, result.status);
            }

            // =====================================================
            // 5. HANDLE NEW CONVERSATION
            // =====================================================
            // Giữ nguyên rule:
            // - First send có thể tạo conversation mới.
            // - Resolved/Closed không bị reopen.
            // - Nếu server trả conversation mới, tải history của nó.

            if (conversationChanged) {
                clearMessages();

                await joinConversation();

                await loadConversationMessages({
                    initial: true
                });
            }
            else if (
                result.status !== undefined &&
                result.status !== null
            ) {
                state.currentConversationStatus =
                    Number(result.status);

                updateCurrentConversationStatus(
                    state.currentConversationStatus
                );
            }

            // =====================================================
            // 6. RENDER SENT MESSAGE
            // =====================================================
            // Không render result hai lần.
            // createMessageElement() cần hỗ trợ message.attachments.

            const sentMessage =
                result.message ?? null;

            if (sentMessage?.id) {
                addMessage(
                    sentMessage,
                    true
                );
            }
            // =====================================================
            // 7. CLEAR COMPOSER AFTER SUCCESS
            // =====================================================

            if (DOM.chatInput) {
                DOM.chatInput.value = "";
            }

            hideQuickReplies();

            state.chatSession.hasActiveHistory = true;
        }
        catch (error) {
            console.error(
                "Send message failed:",
                error
            );
            const errorMessage =
                String(error?.message ?? error ?? "");

            /*
             * =====================================================
             * INVALID CHAT SESSION
             * =====================================================
             */
            if (errorMessage.includes("CHAT_SESSION_INVALID")) {
                
                showChatSessionInvalid();

                return;
            }

            /*
             * =====================================================
             * NORMAL SEND ERROR
                Server đã xác nhận lưu tin nhắn.
                Lỗi xảy ra ở bước cập nhật giao diện sau đó.
             * =====================================================
             */
            if (sendSucceeded) {
                console.error(
                    "Message was sent, but the UI could not be synchronized.",
                    error
                );
                return;
            }
            if (DOM.chatInput) {
                DOM.chatInput.value =
                    originalValue || message;

                DOM.chatInput.focus();
            }
            Toast.error(
                error?.message ||
                "Không thể gửi tin nhắn. Vui lòng thử lại."
            );

        } finally {
            if (!sendSucceeded && DOM.chatInput) {
                DOM.chatInput.focus();
            }
        }
    }
    function addMessage(message, shouldScroll = true) {
        if (!message?.id || !DOM.chatBody) {
            return false;
        }

        if (
            isMessageProcessed(message.id)
        ) {
            return false;
        }

        const row = createMessageElement(message);

        if (!row) {
            return false;
        }

        DOM.chatBody.appendChild(row);

        /*
         * Mark ngay sau khi render.
         */
        markMessageProcessed(
            message.id
        );

        updateQuickRepliesActivity(
            message
        );

        if (shouldScroll) {
            scrollBottom(true);
        }
        return true;
    }

    function createDateSeparator(date) {
        const separator = document.createElement("div");

        separator.className = "chat-date";

        const label = getDateSeparatorLabel(date);

        separator.innerHTML = `<span>${escapeHtml(label)}</span>`;

        return separator;
    }


    function getDateSeparatorLabel(date) {
        const messageDate =
            new Date(date);

        const now = new Date();

        const messageDay =
            new Date(
                messageDate.getFullYear(),
                messageDate.getMonth(),
                messageDate.getDate()
            );

        const today =
            new Date(
                now.getFullYear(),
                now.getMonth(),
                now.getDate()
            );

        const diffDays =
            Math.floor(
                (
                    today.getTime() -
                    messageDay.getTime()
                ) /
                86400000
            );

        if (diffDays === 0) {
            return "Hôm nay";
        }

        if (diffDays === 1) {
            return "Hôm qua";
        }

        return messageDate.toLocaleDateString(
            "vi-VN",
            {
                day: "2-digit",
                month: "2-digit",
                year: "numeric"
            }
        );
    }
    function appendMessageWithDate(
        fragment,
        message,
        previousMessage
    ) {
        if (
            !previousMessage ||
            !isSameMessageDate(
                previousMessage.createdAt,
                message.createdAt
            )
        ) {
            fragment.appendChild(
                createDateSeparator(
                    message.createdAt
                )
            );
        }

        const element = createMessageElement(message);

        if (element) {
            fragment.appendChild(element);
        }
    }

    function isSameMessageDate(
        firstDate,
        secondDate
    ) {
        const first =
            new Date(firstDate);

        const second =
            new Date(secondDate);

        return (
            first.getFullYear() ===
            second.getFullYear() &&
            first.getMonth() ===
            second.getMonth() &&
            first.getDate() ===
            second.getDate()
        );
    }
    function createMessageElement(message) {

        if (!message?.id) {
            return null;
        }

        const senderType = Number(message.senderType);

        const classes = getMessageClasses(message.senderType);

        const rowClass = classes.row;

        if (!rowClass) {
            return null;
        }

        const messageClass = classes.message;

        const isCustomer = senderType === CONFIG.senderType.contact;

        const statusHtml = isCustomer ? buildMessageStatusHtml(message.status) : "";

        const isSystemMessage = senderType === CONFIG.senderType.system;

        const isShowContent = escapeHtml(message.content) == "" ? "hidden" : "";

        const avatarHtml = isCustomer || isSystemMessage ? "" : buildAdminAvatar();

        const row = document.createElement("div");

        row.className = `message-row ${rowClass}`;
        row.dataset.messageId = String(message.id);
        row.dataset.createdAt = message.createdAt;
        row.innerHTML = `${avatarHtml}
        <div class="message-content">
            <div class="message ${messageClass} ${isShowContent}"></div>

            <div class="message-meta">
                <span class="message-time">${formatMessageTime(message.createdAt)}</span>
                ${statusHtml}
            </div>
        </div>
    `;
        const messageElement = row.querySelector(".message");

        if (messageElement) {
            messageElement.appendChild(
                renderMessageText(message.content)
            );
        }
        const messageContent = row.querySelector(".message-content");
        const attachmentsElement = renderMessageAttachments(message.attachments);

        if (messageContent && attachmentsElement) {
            const meta = messageContent.querySelector(".message-meta");
            if (meta) {
                messageContent.insertBefore(attachmentsElement, meta);
            } else {
                messageContent.appendChild(attachmentsElement);
            }
        }
        return row;
    }
    function renderMessageText(content) {
        const text = String(content ?? "");
        const fragment = document.createDocumentFragment();

        const urlPattern = /https?:\/\/[^\s<>"']+/gi;
        let lastIndex = 0;
        let match;

        while ((match = urlPattern.exec(text)) !== null) {
            let url = match[0];
            let trailing = "";

            // Tách dấu câu cuối URL để không đưa vào liên kết.
            while (/[.,!?;:]$/.test(url)) {
                trailing = url.slice(-1) + trailing;
                url = url.slice(0, -1);
            }

            // Xử lý dấu đóng ngoặc không thuộc URL.
            while (
                /[)\]}]$/.test(url) &&
                (
                    (url.endsWith(")") && (url.match(/\(/g) || []).length < (url.match(/\)/g) || []).length) ||
                    (url.endsWith("]") && (url.match(/\[/g) || []).length < (url.match(/\]/g) || []).length) ||
                    (url.endsWith("}") && (url.match(/\{/g) || []).length < (url.match(/\}/g) || []).length)
                )
            ) {
                trailing = url.slice(-1) + trailing;
                url = url.slice(0, -1);
            }

            fragment.appendChild(
                document.createTextNode(text.slice(lastIndex, match.index))
            );

            if (url) {
                const link = document.createElement("a");
                link.href = url;
                link.textContent = url;
                link.target = "_blank";
                link.rel = "noopener noreferrer";
                link.className = "message-link";

                fragment.appendChild(link);
            }

            if (trailing) {
                fragment.appendChild(document.createTextNode(trailing));
            }

            lastIndex = match.index + match[0].length;
        }

        fragment.appendChild(
            document.createTextNode(text.slice(lastIndex))
        );

        return fragment;

    }
    function getMessageClasses(senderType) {
        const name = MESSAGE_SENDER_CLASS[senderType];

        if (!name) {
            return {
                row: null,
                message: ""
            };
        }

        return {
            row: name,
            message: `${name}-message`
        };
    }
    function renderMessageText(content) {
    const text = String(content ?? "");
    const fragment = document.createDocumentFragment();

    const urlPattern = /https?:\/\/[^\s<>"']+/gi;
    let lastIndex = 0;
    let match;

    while ((match = urlPattern.exec(text)) !== null) {
        let url = match[0];
        let trailing = "";

        // Tách dấu câu cuối URL để không đưa vào liên kết.
        while (/[.,!?;:]$/.test(url)) {
            trailing = url.slice(-1) + trailing;
            url = url.slice(0, -1);
        }

        // Xử lý dấu đóng ngoặc không thuộc URL.
        while (
            /[)\]}]$/.test(url) &&
            (
                (url.endsWith(")") && (url.match(/\(/g) || []).length < (url.match(/\)/g) || []).length) ||
                (url.endsWith("]") && (url.match(/\[/g) || []).length < (url.match(/\]/g) || []).length) ||
                (url.endsWith("}") && (url.match(/\{/g) || []).length < (url.match(/\}/g) || []).length)
            )
        ) {
            trailing = url.slice(-1) + trailing;
            url = url.slice(0, -1);
        }

        fragment.appendChild(
            document.createTextNode(text.slice(lastIndex, match.index))
        );

        if (url) {
            const link = document.createElement("a");
            link.href = url;
            link.textContent = url;
            link.target = "_blank";
            link.rel = "noopener noreferrer";
            link.className = "message-link";

            fragment.appendChild(link);
        }

        if (trailing) {
            fragment.appendChild(document.createTextNode(trailing));
        }

        lastIndex = match.index + match[0].length;
    }

    fragment.appendChild(
        document.createTextNode(text.slice(lastIndex))
    );

    return fragment;
}
    function buildAdminAvatar() {
        return `<img src="/favicon.ico" width="20" alt="Favicon" class="avatar-initial rounded-circle">`;
    }


    // ============================================================
    // MESSAGE STATUS
    // ============================================================

    function buildMessageStatusHtml(status) {
        const statusConfig = getMessageStatusConfig(status);

        if (!statusConfig) {
            return "";
        }

        return `
            <span class="message-status ${statusConfig.className}" data-message-status="${statusConfig.status}" title="${statusConfig.title}">
                ${statusConfig.content}
            </span>
        `;
    }


    function getMessageStatusConfig(status) {
        switch (Number(status)) {
            case CONFIG.messageStatus.pending:
                return {
                    status:
                        CONFIG.messageStatus.pending,

                    className:
                        "pending",

                    title:
                        "Đang gửi",

                    content:
                        `<i class="ph ph-spinner"></i>`
                };

            case CONFIG.messageStatus.sent:
                return {
                    status:
                        CONFIG.messageStatus.sent,

                    className:
                        "sent",

                    title:
                        "Đã gửi",

                    content:
                        "✓"
                };

            case CONFIG.messageStatus.delivered:
                return {
                    status:
                        CONFIG.messageStatus.delivered,

                    className:
                        "delivered",

                    title:
                        "Đã nhận",

                    content:
                        "✓✓"
                };

            case CONFIG.messageStatus.read:
                return {
                    status:
                        CONFIG.messageStatus.read,

                    className:
                        "read",

                    title:
                        "Đã đọc",

                    content:
                        "✓✓"
                };

            case CONFIG.messageStatus.failed:
                return {
                    status:
                        CONFIG.messageStatus.failed,

                    className:
                        "failed",

                    title:
                        "Gửi thất bại",

                    content:
                        `<i class="ph ph-warning-circle"></i>`
                };

            default:
                return null;
        }
    }


    function updateMessageStatus(
        messageId,
        status
    ) {
        if (!DOM.chatBody) {
            return;
        }

        const row =
            DOM.chatBody.querySelector(
                `.message-row[data-message-id="${messageId}"]`
            );

        if (!row) {
            return;
        }

        const statusElement =
            row.querySelector(
                ".message-status"
            );

        if (!statusElement) {
            return;
        }

        const config =
            getMessageStatusConfig(
                status
            );

        if (!config) {
            statusElement.className =
                "message-status";

            statusElement.innerHTML = "";
            statusElement.title = "";

            return;
        }

        statusElement.className =
            `message-status ${config.className}`;

        statusElement.dataset.messageStatus =
            String(config.status);

        statusElement.innerHTML =
            config.content;

        statusElement.title =
            config.title;
    }


    // ============================================================
    // CONVERSATION STATUS
    // ============================================================

    function getConversationStatusName(status) {
        return (
            CONFIG.conversationStatus.names[Number(status)] ?? "open"
        );
    }

    // ============================================================
    // ATTACHMENT
    // ============================================================
    async function uploadPendingAttachments() {
        if (pendingAttachments.length > CONFIG.limits.attachmentsPerMessage) {
            throw new Error("Mỗi tin nhắn chỉ được đính kèm tối đa 5 file.");
        }

        const totalSize = pendingAttachments.reduce(
            (sum, file) => sum + file.size,
            0
        );

        if (totalSize > CONFIG.limits.attachmentTotalBytes) {
            throw new Error("Tổng dung lượng file vượt quá 25 MB.");
        }

        const uploadIds = [];

        for (const file of pendingAttachments) {
            if (file.size <= 0) {
                throw new Error(`File "${file.name}" không hợp lệ.`);
            }

            if (file.size > CONFIG.limits.attachmentSizeBytes) {
                throw new Error(`File "${file.name}" vượt quá giới hạn 12 MB.`);
            }


            const validationError = validateAdminAttachment(file);

            if (validationError) {
                console.warn(`${file.name}: ${validationError}`);
                continue;
            }

            let upload = pendingAttachmentUploads.get(file);

            // Upload ID đã hết hạn thì không tái sử dụng.
            if (
                upload?.expiresAt &&
                new Date(upload.expiresAt).getTime() <= Date.now()
            ) {
                pendingAttachmentUploads.delete(file);
                upload = null;
            }

            if (!upload) {
                const formData = new FormData();
                formData.append("file", file);

                const headers = getChatHeaders();

                if (uploadSessionToken) {
                    headers["X-Chat-Upload-Token"] = uploadSessionToken;
                }

                const response = await fetch("/chat/attachments/upload", {
                    method: "POST",
                    headers,
                    body: formData,
                    credentials: "same-origin"
                });

                if (!response.ok) {
                    let message = "Không thể tải file lên. Vui lòng thử lại.";

                    try {
                        const data = await response.json();

                        if (data?.message) {
                            message = data.message;
                        }
                    } catch {
                        // Response không phải JSON.
                    }

                    throw new Error(message);
                }

                const data = await response.json();
                const uploadId = Number(data?.uploadId);

                if (!Number.isSafeInteger(uploadId) || uploadId <= 0) {
                    throw new Error("Server trả về upload ID không hợp lệ.");
                }

                // Lưu token mới nếu server cấp.
                if (
                    typeof data.uploadSessionToken === "string" &&
                    data.uploadSessionToken.length > 0
                ) {
                    uploadSessionToken = data.uploadSessionToken;
                }

                // Guest chưa có danh tính cần token cho các upload tạm.
                //const headersForGuest = getChatHeaders();
                //const hasGuestIdentity =
                //    Boolean(headersForGuest["X-Chat-Contact-Id"]) &&
                //    Boolean(headersForGuest["X-Chat-Guest-Token"]);

                //if (!hasGuestIdentity && !uploadSessionToken) {
                //    throw new Error(
                //        "Phiên upload không hợp lệ. Vui lòng tải file lên lại."
                //    );
                //}

                upload = {
                    uploadId,
                    expiresAt: data.expiresAt ?? null
                };

                pendingAttachmentUploads.set(file, upload);
            }

            uploadIds.push(upload.uploadId);
        }

        return uploadIds;
    }

    function clearPendingAttachments() {
        pendingAttachments = [];
        pendingAttachmentUploads.clear();
        renderAttachmentPreview();
    }


    async function downloadChatAttachment(attachmentId, fileName = "") {
        const headers = {};

        const contactId = localStorage.getItem("caterin_chat_contact_id");
        const guestToken = localStorage.getItem("caterin_chat_guest_token");

        if (contactId) {
            headers["X-Chat-Contact-Id"] = contactId;
        }

        if (guestToken) {
            headers["X-Chat-Guest-Token"] = guestToken;
        }

        const response = await fetch(
            `/chat/attachments/${encodeURIComponent(attachmentId)}/download`,
            {
                method: "GET",
                headers,
                credentials: "same-origin"
            }
        );

        if (!response.ok) {
            throw new Error("Không thể tải file đính kèm.");
        }

        const blob = await response.blob();
        const objectUrl = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = objectUrl;

        // Ưu tiên tên file đã có từ attachment DTO.
        const safeFileName = String(fileName || `attachment-${attachmentId}`)
            .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "_")
            .trim();

        link.download = safeFileName || `attachment-${attachmentId}`;
        link.style.display = "none";

        document.body.appendChild(link);
        link.click();
        link.remove();

        // Tránh thu hồi Blob URL quá sớm trong quá trình bắt đầu tải.
        setTimeout(() => URL.revokeObjectURL(objectUrl), 10000);
    }

    function getAttachmentType(contentType, fileName) {
        const mime = String(contentType || "").toLowerCase();
        const extension = String(fileName || "")
            .split(".")
            .pop()
            .toLowerCase();

        if (mime.startsWith("image/")) return "image";
        if (mime === "application/pdf" || extension === "pdf") return "pdf";

        if (
            mime.includes("word") ||
            ["doc", "docx", "odt", "rtf"].includes(extension)
        ) {
            return "word";
        }

        if (
            mime.includes("spreadsheet") ||
            mime.includes("excel") ||
            ["xls", "xlsx", "csv", "ods"].includes(extension)
        ) {
            return "excel";
        }

        if (
            mime.includes("presentation") ||
            mime.includes("powerpoint") ||
            ["ppt", "pptx", "odp"].includes(extension)
        ) {
            return "powerpoint";
        }

        if (
            ["zip", "rar", "7z", "tar", "gz"].includes(extension) ||
            mime.includes("zip") ||
            mime.includes("compressed")
        ) {
            return "archive";
        }

        if (["txt", "md", "log"].includes(extension) || mime.startsWith("text/")) {
            return "text";
        }

        return "file";
    }

    function getAttachmentTypeLabel(type) {
        const labels = {
            pdf: "PDF",
            word: "DOC",
            excel: "XLS",
            powerpoint: "PPT",
            archive: "ZIP",
            text: "TXT",
            file: "FILE"
        };

        return labels[type] || "FILE";
    }

    async function loadAttachmentImage(img, attachmentId) {
        if (!img) {
            return;
        }

        // Vô hiệu hóa kết quả từ request trước đó.
        const requestId = String(
            Number(img.dataset.previewRequestId || 0) + 1
        );

        img.dataset.previewRequestId = requestId;

        revokeImageObjectUrl(img);

        img.classList.remove("is-loaded");
        img.classList.remove("is-load-failed");

        try {
            const response = await fetch(
                `/chat/attachments/${encodeURIComponent(attachmentId)}/preview`,
                {
                    method: "GET",
                    headers: getChatHeaders(),
                    credentials: "same-origin"
                }
            );

            if (!response.ok) {
                throw new Error(
                    `Image request failed: ${response.status}`
                );
            }

            const blob = await response.blob();

            // Bỏ qua kết quả nếu ảnh đã được yêu cầu tải lại.
            if (img.dataset.previewRequestId !== requestId) {
                return;
            }

            const allowedTypes = new Set([
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif"
            ]);

            if (!allowedTypes.has(blob.type.toLowerCase())) {
                throw new Error("Định dạng ảnh không được hỗ trợ.");
            }

            if (!img.isConnected) {
                return;
            }

            const objectUrl = URL.createObjectURL(blob);
            img.dataset.objectUrl = objectUrl;

            img.onload = () => {
                if (img.dataset.objectUrl !== objectUrl) {
                    return;
                }

                img.classList.add("is-loaded");
                img.classList.remove("is-load-failed");
            };

            img.onerror = () => {
                if (img.dataset.objectUrl !== objectUrl) {
                    return;
                }

                img.classList.remove("is-loaded");
                img.classList.add("is-load-failed");

                revokeImageObjectUrl(img);
            };

            img.src = objectUrl;
        } catch (error) {
            if (img.dataset.previewRequestId === requestId) {
                revokeImageObjectUrl(img);

                img.classList.remove("is-loaded");
                img.classList.add("is-load-failed");
            }

            console.error("Load attachment preview failed:", error);
        }
    }

    function renderMessageAttachments(attachments) {
        if (!Array.isArray(attachments) || attachments.length === 0) {
            return null;
        }

        const container = document.createElement("div");
        container.className = "message-attachments";

        attachments.forEach(attachment => {
            const attachmentId = Number(attachment?.id);

            if (!Number.isSafeInteger(attachmentId) || attachmentId <= 0) {
                return;
            }

            const fileName = String(attachment.fileName || `File ${attachmentId}`);
            const contentType = String(attachment.contentType || "");
            const fileSize = Number(attachment.fileSize) || 0;
            const type = getAttachmentType(contentType, fileName);

            const item = document.createElement("div");
            item.className = `message-attachment is-${type}`;

            if (type === "image") {
                item.classList.add("message-attachment-image");

                const imageWrapper = document.createElement("div");
                imageWrapper.className = "message-image-container";

                const imageButton = document.createElement("button");
                imageButton.type = "button";
                imageButton.className = "message-attachment-image-button";
                imageButton.setAttribute("aria-label", "Xem ảnh đính kèm");

                const image = document.createElement("img");
                image.className = "message-attachment-preview";
                image.alt = fileName;
                image.loading = "lazy";
                image.decoding = "async";

                const loading = document.createElement("span");
                loading.className = "message-attachment-image-loading";
                loading.textContent = "Đang tải ảnh...";

                image.addEventListener("load", () => {
                    loading.remove();
                });

                image.addEventListener("error", () => {
                    loading.textContent = "Không thể tải ảnh";
                });

                imageButton.addEventListener("click", () => {
                    if (
                        !image.dataset.objectUrl ||
                        !image.classList.contains("is-loaded")
                    ) {
                        return;
                    }

                    openImageLightbox(image.dataset.objectUrl, fileName);
                });

                imageButton.append(image, loading);

                // Thanh thông tin nằm ở đáy ảnh
                const imageBottom = document.createElement("div");
                imageBottom.className = "message-image-bottom";

                // Bên trái: tên file và dung lượng
                const imageInfo = document.createElement("div");
                imageInfo.className = "message-image-info";

                const name = document.createElement("span");
                name.className = "message-image-name";
                name.textContent = fileName;
                name.title = fileName;

                const size = document.createElement("span");
                size.className = "message-attachment-size";
                size.textContent = formatAttachmentSize(fileSize);

                imageInfo.append(name, size);

                // Bên phải: nút tải xuống
                const downloadButton = createAttachmentDownloadButton(
                    attachmentId,
                    fileName
                );

                imageBottom.append(imageInfo, downloadButton);

                imageWrapper.append(imageButton, imageBottom);
                item.appendChild(imageWrapper);

                loadAttachmentImage(image, attachmentId);

            } else {
                const icon = document.createElement("div");
                icon.className = `message-attachment-icon is-${type}`;
                icon.setAttribute("aria-hidden", "true");
                icon.textContent = getAttachmentTypeLabel(type);

                const info = document.createElement("div");
                info.className = "message-attachment-info";

                const name = document.createElement("span");
                name.className = "message-attachment-name";
                name.textContent = fileName;
                name.title = fileName;

                const size = document.createElement("span");
                size.className = "message-attachment-size";
                size.textContent = formatAttachmentSize(fileSize);

                info.append(name, size);

                const downloadButton = createAttachmentDownloadButton(
                    attachmentId,
                    fileName
                );

                item.append(icon, info, downloadButton);
            }

            container.appendChild(item);
        });

        return container.childElementCount > 0 ? container : null;
    }
    function validateAdminAttachment(file) {
        const dotIndex = file.name.lastIndexOf('.');
        const extension = dotIndex >= 0
            ? file.name.slice(dotIndex).toLowerCase()
            : '';

        if (!CHAT_ATTACHMENT_CONFIG.allowedExtensions.has(extension)) {
            Toast.warning("Định dạng file không được hỗ trợ")
            return 'Định dạng file không được hỗ trợ.';
        }

        if (
            file.type &&
            !CHAT_ATTACHMENT_CONFIG.allowedMimeTypes.has(file.type)
        ) {
            Toast.warning("Loại file không được hỗ trợ.")
            return 'Loại file không được hỗ trợ.';
        }

        if (file.size <= 0) {
            Toast.warning("File rỗng hoặc không hợp lệ.")
            return 'File rỗng hoặc không hợp lệ.';
        }

        if (file.size > CHAT_ATTACHMENT_CONFIG.maxFileSize) {
            Toast.warning("File vượt quá giới hạn dung lượng cho phép.")
            return 'File vượt quá giới hạn dung lượng cho phép.';
        }

        return null;
    }
    function createAttachmentDownloadButton(attachmentId, fileName) {
        const button = document.createElement("button");

        button.type = "button";
        button.className = "message-attachment-download";
        button.setAttribute("aria-label", `Tải xuống ${fileName}`);

        const icon = document.createElement("span");
        icon.className = "message-attachment-download-icon";
        icon.setAttribute("aria-hidden", "true");
        icon.textContent = "↓";

        const label = document.createElement("span");
        label.textContent = "";

        button.append(icon, label);

        button.addEventListener("click", async () => {
            if (button.disabled) return;

            button.disabled = true;
            label.textContent = "Đang tải...";

            try {
                await downloadChatAttachment(attachmentId, fileName);
            } catch (error) {
                console.error("Download attachment failed:", error);
                Toast.error("Không thể tải file. Vui lòng thử lại.");
            } finally {
                button.disabled = false;
                label.textContent = "Tải xuống";
            }
        });

        return button;
    }

    function formatAttachmentSize(size) {
        if (!Number.isFinite(size) || size <= 0) {
            return "";
        }

        if (size < 1024) {
            return `${size} B`;
        }

        if (size < 1024 * 1024) {
            return `${(size / 1024).toFixed(1)} KB`;
        }

        return `${(size / (1024 * 1024)).toFixed(1)} MB`;
    }

    function closeImageLightbox() {
        const lightbox = document.querySelector(".chat-image-lightbox");

        if (!lightbox) {
            return;
        }

        lightbox.classList.remove("is-open");

        const preview = lightbox.querySelector("img");

        if (preview) {
            preview.removeAttribute("src");
            preview.alt = "";
        }
    }

    function openImageLightbox(imageUrl, altText = "Ảnh đính kèm") {
        let lightbox = document.querySelector(".chat-image-lightbox");

        if (!lightbox) {
            lightbox = document.createElement("div");
            lightbox.className = "chat-image-lightbox";

            lightbox.innerHTML = `
            <button type="button"
                    class="chat-image-lightbox-close"
                    aria-label="Đóng ảnh">&times;</button>
            <img class="chat-image-lightbox-img" alt="">
        `;

            lightbox.addEventListener("click", event => {
                if (
                    event.target === lightbox ||
                    event.target.closest(".chat-image-lightbox-close")
                ) {
                    closeImageLightbox();
                }
            });

            document.addEventListener("keydown", event => {
                if (
                    event.key === "Escape" &&
                    lightbox.classList.contains("is-open")
                ) {
                    closeImageLightbox();
                }
            });

            document.body.appendChild(lightbox);
        }

        const preview = lightbox.querySelector("img");

        preview.src = imageUrl;
        preview.alt = altText;

        lightbox.classList.add("is-open");
    }

    // ============================================================
    // INPUT EVENTS
    // ============================================================

    function initInputEvents() {
        DOM.sendBtn?.addEventListener(
            "click",
            () => sendMessage(
                DOM.chatInput?.value
            )
        );


        DOM.chatInput?.addEventListener(
            "keydown",
            async event => {
                if (
                    event.key !== "Enter" ||
                    event.shiftKey
                ) {
                    return;
                }

                event.preventDefault();

                await sendMessage(
                    DOM.chatInput.value
                );
            }
        );


        DOM.chatInput?.addEventListener(
            "input",
            handleTyping
        );

        DOM.resetChatBox?.addEventListener(
            "click",
            resetChatBox
        );
    }

    // ============================================================
    // QUICK REPLIES STATE
    // ============================================================

    function showQuickReplies() {
        if (!DOM.quickReplies) {
            return;
        }

        DOM.quickReplies.hidden = false;

        DOM.quickReplies.classList.add("show");

        state.quickReplies.visible = true;
    }


    function hideQuickReplies() {
        if (!DOM.quickReplies) {
            return;
        }

        DOM.quickReplies.hidden = true;

        DOM.quickReplies.classList.remove("show");

        state.quickReplies.visible = false;
    }


    function clearQuickReplyTimer() {
        if (state.quickReplies.timer) {
            clearTimeout(
                state.quickReplies.timer
            );

            state.quickReplies.timer = null;
        }
    }


    function updateQuickRepliesActivity(message) {
        if (!message?.createdAt) {
            return;
        }

        const messageTime =
            new Date(message.createdAt).getTime();

        if (!Number.isFinite(messageTime)) {
            return;
        }

        if (
            !state.quickReplies.lastMessageAt ||
            messageTime >
            state.quickReplies.lastMessageAt
        ) {
            state.quickReplies.lastMessageAt =
                messageTime;
        }

        scheduleQuickReplies();
    }


    function scheduleQuickReplies() {
        clearQuickReplyTimer();

        if (!state.quickReplies.lastMessageAt) {
            showQuickReplies();

            return;
        }

        const inactivity =
            Date.now() -
            state.quickReplies.lastMessageAt;

        const remaining =
            CONFIG.timing.quickReplyInactivity -
            inactivity;

        if (remaining <= 0) {
            showQuickReplies();

            return;
        }

        hideQuickReplies();

        state.quickReplies.timer =
            setTimeout(
                showQuickReplies,
                remaining
            );
    }


    function initializeQuickRepliesFromMessages(
        messages
    ) {
        if (
            !Array.isArray(messages) ||
            messages.length === 0
        ) {
            state.quickReplies.lastMessageAt = null;
            showQuickReplies();

            return;
        }

        const latestMessage =
            messages.reduce(
                (latest, message) => {
                    if (!latest) {
                        return message;
                    }

                    return new Date(message.createdAt) >
                        new Date(latest.createdAt)
                        ? message
                        : latest;
                },
                null
            );
        if (!latestMessage) {
            showQuickReplies();

            return;
        }

        state.quickReplies.lastMessageAt =
            new Date(
                latestMessage.createdAt
            ).getTime();

        scheduleQuickReplies();
    }
    // ============================================================
    // QUICK REPLIES
    // ============================================================

    function initQuickReplies() {
        document
            .querySelectorAll(
                ".quick-replies button"
            )
            .forEach(button => {
                button.addEventListener(
                    "click",
                    async () => {
                        hideQuickReplies();

                        await sendMessage(
                            button.dataset.message
                        );
                    }
                );
            });
    }


    // ============================================================
    // ATTACHMENT / EMOJI
    // ============================================================


    function initAttachment() {
        const attachmentBtn = DOM.attachmentBtn;
        const fileInput = DOM.chatFileInput;

        if (!attachmentBtn || !fileInput) {
            return;
        }

        if (attachmentBtn.dataset.initialized === "true") {
            return;
        }

        attachmentBtn.dataset.initialized = "true";

        attachmentBtn.addEventListener("click", () => {
            fileInput.click();
        });

        fileInput.addEventListener("change", event => {
            const input = event.currentTarget;
            const files = Array.from(input.files ?? []);

            // Cho phép chọn lại đúng file vừa chọn.
            input.value = "";

            if (!files.length) {
                return;
            }

            addPendingAttachments(files);
        });
    }

    function addPendingAttachments(files) {
        if (!files?.length) {
            return;
        }

        const remainingSlots = 5 - pendingAttachments.length;

        if (remainingSlots <= 0) {
            Toast.warning("Bạn chỉ có thể đính kèm tối đa 5 file.");
            return;
        }

        const selectedFiles = Array.from(files);
        const acceptedFiles = selectedFiles.slice(0, remainingSlots);

        pendingAttachments.push(...acceptedFiles);
        renderAttachmentPreview();

        if (acceptedFiles.length < selectedFiles.length) {
            Toast.warning("Chỉ có thể đính kèm tối đa 5 file mỗi tin nhắn.");
        }
    }
    function cleanupImageObjectUrls(container) {
        if (!container) {
            return;
        }

        const images = container.querySelectorAll("img[data-object-url]");

        for (const img of images) {
            const objectUrl = img.dataset.objectUrl;

            if (!objectUrl) {
                continue;
            }

            // Nếu lightbox đang dùng chung URL này, đóng nó trước.
            const lightbox = document.querySelector(".chat-image-lightbox");
            const lightboxImage = lightbox?.querySelector("img");

            if (lightbox?.classList.contains("is-open") && lightboxImage?.src === objectUrl) {
                closeImageLightbox();
            }

            revokeImageObjectUrl(img);
        }
    }
    function renderAttachmentPreview() {
        const preview = DOM.chatAttachmentPreview;
        const list = DOM.chatAttachmentList;

        if (!preview || !list) {
            return;
        }

        cleanupImageObjectUrls(list);
        list.innerHTML = "";

        if (!pendingAttachments.length) {
            preview.hidden = true;
            return;
        }

        preview.hidden = false;

        pendingAttachments.forEach((file, index) => {
            list.appendChild(createAttachmentPreviewItem(file, index));
        });
    }

    function createAttachmentPreviewItem(file, index) {
        const item = document.createElement("div");
        item.className = "chat-attachment-item";

        if (file.type.startsWith("image/")) {
            const image = document.createElement("img");
            const objectUrl = URL.createObjectURL(file);

            image.dataset.objectUrl = objectUrl;
            image.src = objectUrl;
            image.alt = file.name;
            image.loading = "lazy";

            image.onload = () => {
                image.classList.add("is-loaded");
                image.classList.remove("is-load-failed");
            };

            image.onerror = () => {
                image.classList.add("is-load-failed");
                revokeImageObjectUrl(image);
            };

            image.addEventListener("click", () => {
                if (image.dataset.objectUrl) {
                    openImageLightbox(
                        image.dataset.objectUrl,
                        image.alt
                    );
                }
            });

            item.appendChild(image);
        } else {
            const fileInfo = document.createElement("div");
            const type = getAttachmentType(file.type, file.name);
            fileInfo.className = "chat-attachment-file is-${type}";

            const icon = document.createElement("div");
            icon.className = `message-attachment-icon is-${type}`;
            icon.setAttribute("aria-hidden", "true");
            icon.textContent = getAttachmentTypeLabel(type);


            //icon.className = `message-attachment-icon is-${type}`;
            //icon.setAttribute("aria-hidden", "true");
            //icon.textContent = getAttachmentTypeLabel(type);
            //icon.setAttribute("class", "chat-attachment-icon is-${type}");
            //icon.setAttribute("viewBox", "0 0 24 24");
            //icon.setAttribute("fill", "none");

        //    icon.innerHTML = `
        //    <path d="M14 2H6C4.9 2 4 2.9 4 4V20C4 21.1 4.9 22 6 22H18C19.1 22 20 21.1 20 20V8L14 2Z"
        //          stroke="currentColor"
        //          stroke-width="1.7"
        //          stroke-linejoin="round" />
        //    <path d="M14 2V8H20"
        //          stroke="currentColor"
        //          stroke-width="1.7"
        //          stroke-linejoin="round" />
        //`;

            const fileName = document.createElement("span");
            fileName.className = "chat-attachment-file-name";
            fileName.title = file.name;
            fileName.textContent = file.name;

            fileInfo.append(icon);
            item.appendChild(fileInfo);
        }

        const removeButton = document.createElement("button");

        removeButton.type = "button";
        removeButton.className = "chat-attachment-remove";
        removeButton.setAttribute("aria-label", `Xóa ${file.name}`);
        removeButton.textContent = "×";

        removeButton.addEventListener("click", () => {
            removePendingAttachment(index);
        });

        item.appendChild(removeButton);

        return item;
    }

    function removePendingAttachment(index) {
        if (!Number.isInteger(index) ||
            index < 0 ||
            index >= pendingAttachments.length
        ) {
            return;
        }
        const removedFile = pendingAttachments[index];
        pendingAttachments.splice(index, 1);
        pendingAttachmentUploads.delete(removedFile);
        renderAttachmentPreview();
    }

    function revokeImageObjectUrl(img) {
        if (!img) {
            return;
        }

        const objectUrl = img.dataset.objectUrl;

        if (objectUrl) {
            URL.revokeObjectURL(objectUrl);
            delete img.dataset.objectUrl;
        }
    }
    function initEmoji() {
        DOM.emojiBtn?.addEventListener(
            "click",
            () => {
                if (!DOM.chatInput) {
                    return;
                }

                DOM.chatInput.value +=
                    " 😊";

                DOM.chatInput.focus();

                handleTyping();
            }
        );
    }


    // ============================================================
    // KEYBOARD
    // ============================================================

    function initKeyboardEvents() {
        document.addEventListener(
            "keydown",
            event => {
                if (
                    event.key === "Escape" &&
                    isChatOpen()
                ) {
                    closeChat();
                }
            }
        );
    }

    function initConversationHistory() {
        DOM.loadPreviousConversationBtn?.addEventListener(
            "click",
            loadPreviousConversation
        );
    }
    function initMessageLazyLoading() {

        DOM.chatBody?.addEventListener(
            "scroll",
            handleMessageScroll,
            { passive: true }
        );

        DOM.chatBody?.addEventListener(
            "wheel",
            handleMessageWheel,
            { passive: false }
        );

        DOM.chatBody?.addEventListener(
            "touchstart",
            handleMessageTouchStart,
            { passive: true }
        );

        DOM.chatBody?.addEventListener(
            "touchmove",
            handleMessageTouchMove,
            { passive: false }
        );

        DOM.chatBody?.addEventListener(
            "touchend",
            handleMessageTouchEnd,
            { passive: true }
        );
    }

    async function handleMessageScroll() {

        if (!DOM.chatBody) {
            return;
        }

        if (DOM.chatBody.scrollTop > 120) {

            state.history.userHasScrolledUp = false;

            hidePreviousConversationButton();

            return;
        }

        state.history.userHasScrolledUp = true;

        // =========================================================
        // 1. Current conversation
        // =========================================================

        if (
            Number(
                state.history.loadedConversationId
            ) ===
            Number(
                state.currentConversationId
            )
        ) {

            /*
             * Current conversation vẫn còn message cũ.
             *
             * Ưu tiên load message của conversation hiện tại
             * trước. Chưa được hiện previous conversation loader.
             */
            if (state.messagePaging.hasMore) {

                hidePreviousConversationButton();

                if (!state.messagePaging.loading) {
                    await loadConversationMessages();
                }

                return;
            }

            /*
             * Current conversation đã hết message.
             *
             * Bây giờ mới tìm conversation trước.
             */
            await preparePreviousConversationFor(
                state.currentConversationId
            );

            syncPreviousConversationButton();

            return;
        }

        // =========================================================
        // 2. Previous conversation đang được load
        // =========================================================

        const conversationId =
            Number(
                state.history.loadedConversationId
            );

        if (!conversationId) {
            hidePreviousConversationButton();
            return;
        }

        const paging =
            getConversationPaging(
                conversationId
            );

        if (!paging) {
            hidePreviousConversationButton();
            return;
        }

        /*
         * Conversation history hiện tại vẫn còn
         * message cũ hơn.
         *
         * Tiếp tục ưu tiên load message.
         */
        if (paging.hasMore) {

            hidePreviousConversationButton();

            if (!paging.loading) {
                await loadOlderMessagesForConversation(
                    conversationId
                );
            }

            return;
        }

        /*
         * Đã hết message của conversation history này.
         *
         * Tìm conversation cũ hơn nữa.
         */
        await preparePreviousConversationFor(
            conversationId
        );

        syncPreviousConversationButton();
    }

    async function handleMessageWheel(event) {

        if (!DOM.chatBody) {
            return;
        }

        const chatBody = DOM.chatBody;

        const atTop =
            chatBody.scrollTop <= 0;

        const atBottom =
            chatBody.scrollTop + chatBody.clientHeight >=
            chatBody.scrollHeight - 1;

        const canScroll =
            chatBody.scrollHeight > chatBody.clientHeight;

        /*
         * Chat đang có nội dung để scroll
         * và wheel vẫn nằm trong vùng scroll hợp lệ.
         */
        if (canScroll) {

            if (event.deltaY < 0 && atTop) {
                // Đã ở đầu → xử lý history
                event.preventDefault();

                state.history.userHasScrolledUp = true;

                await handleMessageScroll();

                return;
            }

            if (event.deltaY > 0 && atBottom) {
                // Đã ở cuối → không cho page phía sau scroll
                event.preventDefault();

                return;
            }

            /*
             * Đang scroll bên trong chat.
             * Không preventDefault để browser tự scroll chatBody.
             */
            return;
        }

        /*
         * Chat không đủ height để có native scrollbar.
         * Nhưng wheel vẫn phải thuộc về chat.
         */
        event.preventDefault();
        if (event.deltaY > 0) {

            state.history.userHasScrolledUp = false;

            hidePreviousConversationButton();

            return;
        }
        if (event.deltaY < 0) {

            state.history.userHasScrolledUp = true;

            await handleMessageScroll();
        }
    }
    function handleMessageTouchStart(event) {

        if (!event.touches?.length) {
            return;
        }

        const y = event.touches[0].clientY;

        state.touch.startY = y;
        state.touch.lastY = y;
    }
    async function handleMessageTouchMove(event) {

        if (
            !DOM.chatBody ||
            state.touch.startY === null
        ) {
            return;
        }

        const y = event.touches[0].clientY;
        const deltaY = y - state.touch.lastY;

        state.touch.lastY = y;

        const chatBody = DOM.chatBody;

        const atTop =
            chatBody.scrollTop <= 0;

        const atBottom =
            chatBody.scrollTop +
            chatBody.clientHeight >=
            chatBody.scrollHeight - 1;

        const canScroll =
            chatBody.scrollHeight >
            chatBody.clientHeight;

        /*
         * Vuốt xuống khi đang ở đầu:
         * để browser tự xử lý native scroll.
         */
        if (deltaY > 0 && !atTop) {
            return;
        }

        /*
         * Vuốt lên khi đang ở cuối:
         * chat tự scroll nếu còn khoảng scroll.
         */
        if (deltaY < 0 && !atBottom) {
            return;
        }

        /*
         * Không có scrollbar hoặc đã chạm boundary.
         * Giữ gesture trong chat, không để page phía sau scroll.
         */
        event.preventDefault();

        /*
         * Vuốt lên ở đầu → history.
         */
        if (deltaY > 0 && atTop) {

            state.history.userHasScrolledUp = true;

            await handleMessageScroll();

            return;
        }

        /*
         * Vuốt xuống ở cuối → chỉ xử lý UI animation.
         */
        if (deltaY < 0 && atBottom) {

            triggerChatBoundaryAnimation();

            return;
        }

        /*
         * Chat không có scrollbar:
         * vuốt lên vẫn kích hoạt history.
         */
        if (!canScroll && deltaY > 0) {

            state.history.userHasScrolledUp = true;

            await handleMessageScroll();
        }
    }
    function handleMessageTouchEnd() {

        state.touch.startY = null;
        state.touch.lastY = null;
    }

    function triggerChatBoundaryAnimation() {

        const loader =
            DOM.chatHistoryLoader;

        if (!loader) {
            return;
        }

        loader.classList.remove("chat-boundary-bounce");

        /*
         * Force reflow để animation có thể chạy lại
         * mỗi lần user wheel/touch.
         */
        void loader.offsetWidth;

        loader.classList.add(
            "chat-boundary-bounce"
        );
    }
    // ============================================================
    // NOTIFICATION BROWSER
    // ============================================================
    function shouldShowNotificationPrompt() {
        if (!("Notification" in window)) {
            return false;
        }

        if (Notification.permission !== "default") {
            return false;
        }

        const value = localStorage.getItem(
            CONFIG.storage.notificationPromptSnoozeUntil
        );

        // Chưa từng bấm Later
        if (!value) {
            return true;
        }

        const expiresAt = Number(value);

        // Dữ liệu localStorage không hợp lệ
        if (!Number.isFinite(expiresAt)) {
            localStorage.removeItem(
                CONFIG.storage.notificationPromptSnoozeUntil
            );

            return true;
        }

        // Đã hết 7 ngày
        if (Date.now() >= expiresAt) {
            localStorage.removeItem(
                CONFIG.storage.notificationPromptSnoozeUntil
            );

            return true;
        }

        return false;
    }
    function showNotificationPrompt() {
        if (!DOM.notificationPrompt) {
            return;
        }

        if (!("Notification" in window)) {
            return;
        }

        if (Notification.permission !== "default") {
            return;
        }

        DOM.notificationPrompt.hidden = false;

        requestAnimationFrame(() => {
            DOM.notificationPrompt.classList.add("is-visible");
        });
    }

    function snoozeNotificationPrompt() {
        const expiresAt =
            Date.now() +
            CONFIG.timing.notificationPromptDelay;

        localStorage.setItem(
            CONFIG.storage.notificationPromptSnoozeUntil,
            String(expiresAt)
        );

        DOM.notificationPrompt.classList.remove("is-visible");

        setTimeout(() => {
            DOM.notificationPrompt.hidden = true;
        }, 250);
    }
    function showMessageNotification(message) {
        if (!("Notification" in window)) {
            return;
        }

        if (Notification.permission !== "granted") {
            return;
        }

        if (!message) {
            return;
        }

        //const senderName = message.senderName || "Caterin";
        const senderName = "Caterin";
        const content = message.content || "Bạn có tin nhắn mới.";

        const notification = new Notification(
            `${senderName} đã gửi tin nhắn`,
            {
                body: content,
                icon: "/favicon.ico"
            }
        );

        notification.onclick = () => {
            window.focus();

            if (!isChatOpen()) {
                openChat();
            }

            notification.close();
        };
    }
    // ============================================================
    // INITIALIZATION
    // ============================================================

    function init() {

        if (state.initialized) {
            return;
        }

        state.initialized = true;
        loadStoredSession();
        initPromoPopup();
        initChatToggle();
        initInputEvents();
        initQuickReplies();
        initAttachment();
        initEmoji();
        initKeyboardEvents();
        initConversationHistory();
        initMessageLazyLoading();
        registerSignalREvents();

        startChatConnection();
    }


    // ============================================================
    // START
    // ============================================================

    if (
        document.readyState ===
        "loading"
    ) {
        document.addEventListener(
            "DOMContentLoaded",
            init,
            { once: true }
        );
    }
    else {
        init();
    }

    window.addEventListener(
        "caterin:customer-logout",
        function () {
            clearCustomerIdentity();

            state.currentConversationStatus = null;
            state.chatSession.hasActiveHistory = false;

            resetConversationHistoryState();
            clearMessages();

            showQuickReplies();
        }
    );
});
