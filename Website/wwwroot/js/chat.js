(() => {
    "use strict";

    // ============================================================
    // CONFIG
    // ============================================================

    const CONFIG = {
        storage: {
            hidePromo: "hidePromo",
            contactId: "caterin_chat_contact_id",
            conversationId: "caterin_chat_conversation_id",
            guestToken: "caterin_chat_guest_token",
            notificationPrompt: "caterin_notification"
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
            processedMessageIds: 500
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
            closed: 4
        }
    };


    // ============================================================
    // DOM
    // ============================================================

    const DOM = {
        popup: document.getElementById("promoPopup"),
        hideToday: document.getElementById("hideToday"),
        closePopup: document.getElementById("closePopup"),
        laterButton: document.getElementById("laterBtn"),

        chatToggle: document.getElementById("chatToggle"),
        chatWindow: document.getElementById("chatWindow"),
        chatClose: document.getElementById("chatClose"),

        chatInput: document.getElementById("chatInput"),
        sendButton: document.getElementById("sendBtn"),

        chatBody: document.getElementById("chatBody"),
        typing: document.getElementById("typing"),
        chatBadge: document.getElementById("chatBadge"),
        quickReplies: document.querySelector(".quick-replies"),

        attachmentButton: document.getElementById("attachmentBtn"),
        emojiButton: document.getElementById("emojiBtn"),

        adminStatusDot: document.getElementById("chatAdminStatusDot"),
        chatStatusText: document.getElementById("chatStatusText"),
        chatStatusBadge: document.getElementById("chatStatusBadge"),
         
        notificationPrompt:
            document.querySelector("#chatNotificationPrompt"),

        notificationAllow:
            document.querySelector("#chatNotificationAllow"),

        notificationLater:
            document.querySelector("#chatNotificationLater"),

        chatHistoryLoader: document.getElementById("chatHistoryLoader"),

        loadPreviousConversationBtn:
            document.getElementById(
                "loadPreviousConversationBtn"
            ),
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
            hasActiveHistory: false
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

        messages: {
            limit: 30,
            oldestMessageId: null,
            hasMore: true,
            loading: false
        },

        history: {
            previousConversationId: null,
            hasPrevious: false,
            loading: false,

            oldestLoadedConversationId: null,
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
    // SIGNALR
    // ============================================================

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl("/hubs/chat?clientType=customer")
            .withAutomaticReconnect()
            .build();


    // ============================================================
    // STORAGE
    // ============================================================

    function getGuestToken() {
        let token =
            localStorage.getItem(CONFIG.storage.guestToken);

        if (!token) {
            token = crypto.randomUUID();

            localStorage.setItem(
                CONFIG.storage.guestToken,
                token
            );
        }

        return token;
    }


    function loadStoredSession() {
        state.currentContactId =
            Number(
                localStorage.getItem(
                    CONFIG.storage.contactId
                )
            ) || null;

        state.currentConversationId =
            Number(
                localStorage.getItem(
                    CONFIG.storage.conversationId
                )
            ) || null;

        state.guestToken = getGuestToken();
    }


    function saveSession(conversation) {
        state.currentContactId =
            Number(conversation.contactId) || null;

        state.currentConversationId =
            Number(conversation.id) || null;

        localStorage.setItem(
            CONFIG.storage.contactId,
            String(state.currentContactId)
        );

        localStorage.setItem(
            CONFIG.storage.conversationId,
            String(state.currentConversationId)
        );

        localStorage.setItem(
            CONFIG.storage.guestToken,
            state.guestToken
        );
    }


    // ============================================================
    // GENERIC HELPERS
    // ============================================================

    function isConnectionReady() {
        return (
            connection.state ===
            signalR.HubConnectionState.Connected
        );
    }


    function isChatOpen() {
        return DOM.chatWindow?.classList.contains("open") ?? false;
    }


    function isValidChatSession() {
        return Boolean(
            state.currentConversationId &&
            state.currentContactId
        );
    }


    function normalizeNumber(value) {
        const number = Number(value);

        return Number.isFinite(number)
            ? number
            : null;
    }


    function isSameConversation(conversationId) {
        return (
            Number(state.currentConversationId) ===
            Number(conversationId)
        );
    }


    function escapeHtml(value) {
        const element =
            document.createElement("div");

        element.textContent =
            value ?? "";

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

        const hiddenUntil =
            localStorage.getItem(
                CONFIG.storage.hidePromo
            );

        if (hiddenUntil) {
            return;
        }

        setTimeout(() => {
            DOM.popup?.classList.add("show");
        }, CONFIG.timing.promoDelay);


        DOM.closePopup?.addEventListener(
            "click",
            closePromoPopup
        );

        DOM.laterButton?.addEventListener(
            "click",
            closePromoPopup
        );
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
            appendMessageWithDate(
                fragment,
                message,
                previousMessage
            );

            previousMessage = message;
        });

        DOM.chatBody.prepend(fragment);

        normalizeDateSeparators();

        const newHeight = DOM.chatBody.scrollHeight;

        DOM.chatBody.scrollTop =
            previousTop + (newHeight - previousHeight);
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
            console.log(
                "preparePreviousConversationFor:",
                error
            );

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
    function isConversationResolved() {
        return (
            Number(state.currentConversationStatus) ===
            CONFIG.conversationStatus.resolved
        );
    }

    //FIX NEW 2
    async function openChat() {
        console.trace(
            "[OPEN CHAT]",
            {
                conversationId:
                    state.currentConversationId,

                conversationStatus:
                    state.currentConversationStatus,

                isResolved:
                    isConversationResolved(),

                hasActiveHistory:
                    state.chatSession.hasActiveHistory
            }
        );
        if (!DOM.chatWindow) return;

        DOM.chatWindow.classList.add("open");

        try {
            // Không có conversation
            if (!state.currentConversationId) {
                clearMessages();
                state.chatSession.hasActiveHistory = false;
                showQuickReplies();
                return;
            }
            // Conversation đã resolved và UI session trước đó đã kết thúc
            if (
                isConversationResolved() &&
                !state.chatSession.hasActiveHistory
            ) {
                console.log(
                    "[OPEN CHAT RESET RESOLVED]",
                    {
                        conversationId:
                            state.currentConversationId,

                        conversationStatus:
                            state.currentConversationStatus,

                        hasActiveHistory:
                            state.chatSession.hasActiveHistory
                    }
                );
                clearMessages();

                state.currentConversationId = null;
                state.currentConversationStatus = null;

                localStorage.removeItem(
                    CONFIG.storage.conversationId
                );

                state.messages.oldestMessageId = null;
                state.messages.hasMore = true;
                state.messages.loading = false;

                resetConversationHistoryState();

                showQuickReplies();

                return;
            }

            if (!DOM.chatBody?.children.length) {
                await loadConversationMessages({
                    initial: true
                });
            }

            await joinConversation();
            const marked = await markConversationAsRead();

            if (marked) {
                setUnreadMessageCount(0);
            }

        } catch (error) {
            console.error(
                "Không thể mở chat:",
                error
            );
        }
    }
    function updateCurrentConversationStatus(status) {
        const normalizedStatus =
            String(status ?? "").toLowerCase();

        const isClosed =
            normalizedStatus === "closed";

        if (DOM.chatInput) {
            DOM.chatInput.disabled = isClosed;
        }

        if (DOM.sendButton) {
            DOM.sendButton.disabled = isClosed;
        }

        if (DOM.chatInput) {
                if (normalizedStatus === "closed") {
                    DOM.chatInput.placeholder =
                    "Cuộc hội thoại đã đóng";
            } else if (normalizedStatus === "resolved") {
                DOM.chatInput.placeholder =
                    "Nhập tin nhắn để bắt đầu cuộc trò chuyện mới...";
            } else {
                DOM.chatInput.placeholder =
                    "Nhập tin nhắn...";
            }
        }
    }
    async function loadConversationMessages({
        initial = false
    } = {}) {
        if (
            !state.currentConversationId ||
            state.messages.loading
        ) {
            return;
        }
        if (initial) {
            state.chatSession.hasActiveHistory = false;
        }

        state.messages.loading = true;

        try {
            const params = new URLSearchParams();

            params.set(
                "limit",
                String(state.messages.limit)
            );

            if (
                !initial &&
                state.messages.oldestMessageId
            ) {
                params.set(
                    "before",
                    String(state.messages.oldestMessageId)
                );
            }

            const response = await fetch(
                `/chat/${state.currentConversationId}/messages?${params}`,
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
                    ? messages.length >= state.messages.limit
                    : Boolean(result.hasMore);

            /*
             * Initial load của conversation hiện tại
             */
            if (initial) {
                clearMessages();

                state.history.oldestLoadedConversationId =
                    state.currentConversationId;

                const currentPaging =
                    getConversationPaging(
                        state.currentConversationId
                    );

                currentPaging.oldestMessageId = null;
                currentPaging.hasMore = true;
                currentPaging.loading = false;
            }

            /*
             * Không có message
             */
            if (!messages.length) {
                state.messages.hasMore = false;

                if (initial) {
                    initializeQuickRepliesFromMessages([]);
                }

                await preparePreviousConversationFor(
                    state.currentConversationId
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
             * Load thêm message cũ của conversation hiện tại
             */
            else {
                prependHistoricalMessages(messages);
            }

            /*
             * Message cũ nhất của conversation hiện tại
             */
            state.messages.oldestMessageId =
                messages[0]?.id ??
                state.messages.oldestMessageId;

            state.messages.hasMore = hasMore;

            const currentPaging =
                getConversationPaging(
                    state.currentConversationId
                );

            currentPaging.oldestMessageId =
                state.messages.oldestMessageId;

            currentPaging.hasMore =
                hasMore;

            /*
             * Conversation hiện tại đã hết message.
             * Lúc này mới tìm conversation trước.
             */
            if (!hasMore) {
                await preparePreviousConversationFor(
                    state.currentConversationId
                );
            }

            state.chatSession.hasActiveHistory = true;
        }
        catch (error) {
            console.error(
                "loadConversationMessages:",
                error
            );
        }
        finally {
            state.messages.loading = false;
        }
    }
    function closeChat() {
        stopTyping();
        hideTyping();

        if (isConversationResolved()) {
            state.chatSession.hasActiveHistory = false;
        }
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
                markNotificationPrompted();
            }
        );

        DOM.notificationAllow?.addEventListener(
            "click",
            async () => {
                if (!("Notification" in window)) {
                    DOM.notificationPrompt.hidden = true;
                    return;
                }

                const permission =
                    await Notification.requestPermission();

                if (permission === "granted") {
                    localStorage.setItem(
                        CONFIG.storage.notificationPrompt,
                        "true"
                    );

                    const message =
                        state.notification.pendingMessage;

                    state.notification.pendingMessage = null;

                    DOM.notificationPrompt.hidden = true;

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
        if (
            !isValidChatSession() ||
            !isConnectionReady()
        ) {
            return;
        }

        try {
            await connection.invoke(
                "SendTyping",
                state.currentConversationId,
                state.currentContactId,
                state.guestToken,
                isTyping
            );
        }
        catch (error) {
            console.log(
                "Send typing failed:",
                error
            );
        }
    }


    function showTyping() {
        if (!DOM.typing) {
            return;
        }

        DOM.typing.hidden = false;

        scrollBottom();
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

        if (!isValidChatSession()) {
            return;
        }

        const status =
            await joinConversation();

        if (
            status ===
            CONFIG.conversationStatus.resolved
        ) {
            resetToNewChatState();
            return;
        }

        await loadUnreadCount();

        await loadConversationMessages({
            initial: true
        });

        updateCurrentConversationStatus(status);
    }
    function resetToNewChatState() {

        clearMessages();

        state.currentConversationId = null;
        state.currentConversationStatus = null;

        state.unreadMessageCount = 0;

        state.chatSession.hasActiveHistory = false;

        state.messages.oldestMessageId = null;
        state.messages.hasMore = true;
        state.messages.loading = false;

        state.history.previousConversationId = null;
        state.history.hasPrevious = false;
        state.history.loading = false;
        state.history.oldestLoadedConversationId = null;
        state.history.conversations.clear();

        localStorage.removeItem(
            CONFIG.storage.conversationId
        );

        hidePreviousConversationButton();

        setUnreadMessageCount(0);

        showQuickReplies();
    }

    async function joinConversation() {

        if (
            !state.currentConversationId ||
            !state.currentContactId ||
            !isConnectionReady()
        ) {
            return null;
        }

        const status =
            await connection.invoke(
                "JoinConversation",
                state.currentConversationId,
                state.currentContactId,
                state.guestToken
            );

        state.currentConversationStatus =
            Number(status);

        return state.currentConversationStatus;
    }


    async function createConversation() {
        if (!CHAT_CONFIG.inboxId) {
            throw new Error(
                "Chat inboxId không hợp lệ."
            );
        }
        const response =
            await fetch(
                "/chat/start",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },
                    credentials: "same-origin",
                    body: JSON.stringify({
                        inboxId:
                            CHAT_CONFIG.inboxId,

                        name:
                            CHAT_CONFIG.name,

                        email:
                            CHAT_CONFIG.email,

                        phone:
                            CHAT_CONFIG.phone,

                        guestToken:
                            state.guestToken
                    })
                }
            );

        if (!response.ok) {
            throw new Error(
                "Không thể bắt đầu cuộc trò chuyện."
            );
        }

        const conversation =
            await response.json();

        saveSession(conversation);

        state.currentConversationStatus =
            Number(conversation.status);
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

        if (!isValidChatSession()) {
            return;
        }

        try {
            await joinConversation();

            await loadConversationMessages({
                initial: true
            });

            await loadUnreadCount();
        }
        catch (error) {
            console.log(
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
            Number(message.conversationId);

        const currentConversationId =
            Number(state.currentConversationId);

        console.log(
            "[CLIENT MESSAGE RECEIVED]",
            {
                messageId: message.id,
                messageConversationId,
                currentConversationId,
                senderType: message.senderType,
                content: message.content
            }
        );

        if (
            messageConversationId &&
            messageConversationId !== currentConversationId
        ) {
            console.log(
                "[CLIENT MESSAGE IGNORED - DIFFERENT CONVERSATION]",
                {
                    messageConversationId,
                    currentConversationId
                }
            );

            return false;
        }

        hideTyping();

        const added =
            addMessage(
                message,
                true
            );

        console.log(
            "[CLIENT MESSAGE ADDED]",
            {
                messageId: message.id,
                added
            }
        );

        if (!added) {
            return false;
        }

        const senderType =
            Number(message.senderType);

        if (
            senderType === CONFIG.senderType.system
        ) {
            return true;
        }

        const isIncoming =
            senderType === CONFIG.senderType.admin ||
            senderType === CONFIG.senderType.bot;

        if (!isIncoming) {
            return true;
        }

        await acknowledgeMessageDelivered(
            message
        );

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

            state.notification.pendingMessage =
                message;

            showNotificationPrompt();

            return true;
        }

        showMessageNotification(
            message
        );

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
        const conversationId = Number(
            data?.conversationId
        );

        if (
            conversationId !==
            Number(state.currentConversationId)
        ) {
            return;
        }

        state.currentConversationStatus =
            data.status;

        updateCurrentConversationStatus(
            data.status
        );
    }

    function updateCurrentConversationStatus(status) {
        const normalizedStatus = Number(status);

        const isClosed =
            normalizedStatus === CONFIG.conversationStatus.closed;

        if (DOM.chatInput) {
            DOM.chatInput.disabled = isClosed;
        }

        if (DOM.sendButton) {
            DOM.sendButton.disabled = isClosed;
        }

        if (DOM.chatInput) {
            if (isClosed) {
                DOM.chatInput.placeholder =
                    "Cuộc hội thoại đã đóng";
            } else if (
                normalizedStatus ===
                CONFIG.conversationStatus.resolved
            ) {
                DOM.chatInput.placeholder =
                    "Nhập tin nhắn để bắt đầu cuộc trò chuyện mới...";
            } else {
                DOM.chatInput.placeholder =
                    "Nhập tin nhắn...";
            }
        }
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
                String(state.messages.limit)
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
                    ? messages.length >= state.messages.limit
                    : Boolean(result.hasMore);

            console.log(
                "Previous conversation:",
                conversationId,
                "messages:",
                messages.length,
                "hasMore:",
                hasMore
            );

            if (!messages.length) {
                paging.hasMore = false;
                paging.oldestMessageId = null;

                state.history.oldestLoadedConversationId =
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

            state.history.oldestLoadedConversationId =
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
        DOM.chatBody.innerHTML = '';

        if (!messages || messages.length === 0) {
            initializeQuickRepliesFromMessages(
                messages
            );

            return;
        }

        const fragment = document.createDocumentFragment();

        let previousMessage = null;

        messages.forEach(message => {

            appendMessageWithDate(
                fragment,
                message,
                previousMessage
            );

            previousMessage = message;
        });

        DOM.chatBody.appendChild(fragment);

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

        DOM.chatBody.scrollTop = DOM.chatBody.scrollHeight;
    }
    function prependMessages(messages) {
        if (!DOM.chatBody) {
            return;
        }

        const previousHeight =
            DOM.chatBody.scrollHeight;

        const previousTop =
            DOM.chatBody.scrollTop;

        const fragment =
            document.createDocumentFragment();

        let previousMessage = null;

        messages.forEach(message => {

            appendMessageWithDate(
                fragment,
                message,
                previousMessage
            );

            previousMessage = message;
        });

        DOM.chatBody.prepend(fragment);

        const newHeight =
            DOM.chatBody.scrollHeight;

        DOM.chatBody.scrollTop =
            previousTop +
            (newHeight - previousHeight);
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

        state.history.oldestLoadedConversationId =
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
            console.log("unread-count response:", result);

            const unreadCount = result?.data ?? result;

            console.log("unread count:", unreadCount);
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
        if (!isValidChatSession()) {
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

    async function sendMessage(text) {

        const message =
            String(text ?? "").trim();

        if (!message) {
            return;
        }

        stopTyping();

        if (!isConnectionReady()) {
            console.log("SignalR chưa kết nối.");
            return;
        }

        if (DOM.chatInput) {
            DOM.chatInput.value = "";
        }

        try {
            console.log(CHAT_CONFIG.inboxId);
            const result =
                await connection.invoke(
                    "SendCustomerMessage",
                    state.currentConversationId,
                    state.currentContactId,
                    message,
                    state.guestToken
                );

            if (!result) {
                return;
            }

            const newConversationId =
                Number(result.conversationId);

            const currentConversationId =
                Number(state.currentConversationId);

            /*
             * Conversation chuyển:
             *
             * Resolved A
             *      ↓
             * customer gửi message
             *      ↓
             * server tạo B
             */
            if (
                newConversationId &&
                newConversationId !== currentConversationId
            ) {

                state.currentConversationId =
                    newConversationId;

                state.currentConversationStatus =
                    Number(result.status);

                localStorage.setItem(
                    CONFIG.storage.conversationId,
                    String(newConversationId)
                );

                await joinConversation();

                state.chatSession.hasActiveHistory =
                    true;

                updateCurrentConversationStatus(
                    state.currentConversationStatus
                );

                addMessage(result, true);

                return;
            }

            /*
             * Message thuộc conversation hiện tại.
             */
            addMessage(result, true);

        }
        catch (error) {

            console.log(
                "Send message failed:",
                error
            );

            if (DOM.chatInput) {
                DOM.chatInput.value = message;
                DOM.chatInput.focus();
            }
        }
    }


    // ============================================================
    // MESSAGE RENDERING
    // ============================================================

    function isMessageProcessed(messageId) {
        if (!messageId) {
            return true;
        }

        if (
            state.processedMessageIds.has(
                messageId
            )
        ) {
            return true;
        }

        state.processedMessageIds.add(
            messageId
        );

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

        return false;
    }
    function createDateSeparator(date) {
        const separator =
            document.createElement("div");

        separator.className =
            "chat-date";

        const label =
            getDateSeparatorLabel(date);

        separator.innerHTML = `
        <span>${escapeHtml(label)}</span>
    `;

        return separator;
    }


    function getDateSeparatorLabel(date) {
        const messageDate =
            new Date(date);

        const now =
            new Date();

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

        const element =
            createMessageElement(message);

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

        const senderType =
            Number(message.senderType);

        const rowClass =
            getMessageRowClass(
                senderType
            );

        if (!rowClass) {
            return null;
        }

        const messageClass =
            getMessageClass(
                senderType
            );

        const isCustomer =
            senderType ===
            CONFIG.senderType.contact;


        const statusHtml =
            isCustomer
                ? buildMessageStatusHtml(
                    message.status
                )
                : "";

        const isSystemMessage = senderType === CONFIG.senderType.system;
        const avatarHtml =
            isCustomer || isSystemMessage
                ? ""
                : buildAdminAvatar();

        const row =
            document.createElement("div");

        row.className =
            `message-row ${rowClass}`;

        row.dataset.messageId =
            String(message.id);
        row.dataset.createdAt =
            message.createdAt;
        row.innerHTML = `
        ${avatarHtml}

        <div class="message-content">
            <div class="message ${messageClass}">${escapeHtml(message.content)}</div>

            <div class="message-meta">
                <span class="message-time">${formatMessageTime(message.createdAt)}</span>
                ${statusHtml}
            </div>
        </div>
    `;

        return row;
    }

    function addMessage(
        message,
        shouldScroll = true
    ) {
        if (!message?.id) {
            return false;
        }

        if (
            isMessageProcessed(
                message.id
            )
        ) {
            return false;
        }

        const row =
            createMessageElement(
                message
            );

        if (!row) {
            return false;
        }

        DOM.chatBody?.appendChild(row);
        console.log("addMessage");
        updateQuickRepliesActivity(
            message
        );

        if (shouldScroll) {
            scrollBottom(true);
        }

        return true;
    }


    function getMessageRowClass(senderType) {
        switch (senderType) {
            case CONFIG.senderType.contact:
                return "contact";

            case CONFIG.senderType.admin:
                return "admin";

            case CONFIG.senderType.bot:
                return "bot";

            case CONFIG.senderType.system:
                return "system";

            default:
                return null;
        }
    }


    function getMessageClass(senderType) {
        switch (senderType) {
            case CONFIG.senderType.contact:
                return "contact-message";

            case CONFIG.senderType.admin:
                return "admin-message";

            case CONFIG.senderType.bot:
                return "bot-message";

            case CONFIG.senderType.system:
                return "system-message";

            default:
                return "";
        }
    }


    function buildAdminAvatar() {
        return `<img src="/favicon.ico" width="20" alt="Favicon" class="avatar-initial rounded-circle">`;
    }


    // ============================================================
    // MESSAGE STATUS
    // ============================================================

    function buildMessageStatusHtml(status, showStatus = true) {
        if (!showStatus) {
            return "";
        }

        const statusConfig =
            getMessageStatusConfig(status);

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
                        "[●]"
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

    //function updateCurrentConversationStatus(
    //    status
    //) {
    //    const normalizedStatus =
    //        getConversationStatusName(
    //            status
    //        );

    //    const statusText =
    //        DOM.chatStatusText;

    //    const statusBadge =
    //        DOM.chatStatusBadge;

    //    const statusLabels = {
    //        open: "Đang hỗ trợ",
    //        pending: "Đang chờ",
    //        resolved: "Đã giải quyết",
    //        closed: "Đã đóng"
    //    };

    //    if (statusText) {
    //        statusText.textContent =
    //            statusLabels[
    //            normalizedStatus
    //            ] ?? normalizedStatus;
    //    }

    //    if (statusBadge) {
    //        statusBadge.dataset.status =
    //            normalizedStatus;

    //        statusBadge.classList.remove(
    //            "open",
    //            "pending",
    //            "resolved",
    //            "closed"
    //        );

    //        statusBadge.classList.add(
    //            normalizedStatus
    //        );
    //    }

    //    if (DOM.chatWindow) {
    //        DOM.chatWindow.dataset.status =
    //            normalizedStatus;
    //    }

    //    const isEnded =
    //        normalizedStatus === "resolved" ||
    //        normalizedStatus === "closed";

    //    if (DOM.chatInput) {
    //        DOM.chatInput.disabled =
    //            isEnded;

    //        DOM.chatInput.placeholder =
    //            isEnded
    //                ? "Cuộc trò chuyện đã kết thúc"
    //                : "Nhập tin nhắn...";
    //    }

    //    if (DOM.sendButton) {
    //        DOM.sendButton.disabled =
    //            isEnded;
    //    }
    //}


    function getConversationStatusName(
        status
    ) {
        const statusMap = {
            [CONFIG.conversationStatus.open]:
                "open",

            [CONFIG.conversationStatus.pending]:
                "pending",

            [CONFIG.conversationStatus.resolved]:
                "resolved",

            [CONFIG.conversationStatus.closed]:
                "closed"
        };

        return (
            statusMap[
            Number(status)
            ] ?? "open"
        );
    }


    // ============================================================
    // INPUT EVENTS
    // ============================================================

    function initInputEvents() {
        DOM.sendButton?.addEventListener(
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
            state.quickReplies.lastMessageAt =
                null;
            console.log('c');
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
        DOM.attachmentButton?.addEventListener(
            "click",
            () => {
                alert(
                    "Chức năng gửi file sẽ được tích hợp sau."
                );
            }
        );
    }


    function initEmoji() {
        DOM.emojiButton?.addEventListener(
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
                state.history.oldestLoadedConversationId
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
            if (state.messages.hasMore) {

                hidePreviousConversationButton();

                if (!state.messages.loading) {
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
                state.history.oldestLoadedConversationId
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
    async function loadOlderMessagesForConversation(conversationId) {
        const id = Number(conversationId);

        if (!id) {
            return;
        }

        console.log("loadOlderMessagesForConversation:", id);

        const paging = getConversationPaging(id);

        if (
            !paging ||
            paging.loading ||
            !paging.hasMore
        ) {
            return;
        }

        paging.loading = true;

        try {
            const params = new URLSearchParams();

            params.set(
                "limit",
                String(state.messages.limit)
            );

            if (paging.oldestMessageId) {
                params.set(
                    "before",
                    String(paging.oldestMessageId)
                );
            }

            const response = await fetch(
                `/chat/${id}/messages?${params}`,
                {
                    method: "GET",
                    headers: getChatHeaders(),
                    credentials: "same-origin"
                }
            );

            if (!response.ok) {
                throw new Error(
                    `Không thể tải thêm message. Status: ${response.status}`
                );
            }

            const result = await response.json();

            const messages =
                Array.isArray(result)
                    ? result
                    : result.items ?? [];

            const hasMore =
                Array.isArray(result)
                    ? messages.length >= state.messages.limit
                    : Boolean(result.hasMore);

            /*
             * Không còn message cũ hơn.
             */
            if (!messages.length) {
                paging.hasMore = false;

                await preparePreviousConversationFor(id);

                return;
            }

            /*
             * Append message cũ vào đầu chat.
             * Hàm này đã tự giữ nguyên viewport.
             */
            prependHistoricalMessages(messages);

            /*
             * API đang trả message theo thứ tự cũ -> mới,
             * nên message đầu tiên là oldest.
             */
            paging.oldestMessageId =
                messages[0]?.id ?? paging.oldestMessageId;

            paging.hasMore = hasMore;

            /*
             * Conversation hiện tại đã hết message.
             * Tìm conversation cũ hơn.
             *
             * Không cần gọi sync trực tiếp vì
             * preparePreviousConversationFor() sẽ cập nhật
             * previousConversationId + hasPrevious rồi sync button.
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
    // ============================================================
    // NOTIFICATION BROWSER
    // ============================================================
    function shouldShowNotificationPrompt() {
        const value = localStorage.getItem(CONFIG.storage.notificationPrompt);
        console.log(value);

        if (!value) {
            return true;
        }

        const expiresAt = Number(value);

        console.log(expiresAt);
        if (!Number.isFinite(expiresAt)) {
            localStorage.removeItem(
                CONFIG.storage.notificationPrompt
            );

            return true;
        }

        if (Date.now() >= expiresAt) {
            localStorage.removeItem(
                CONFIG.storage.notificationPrompt
            );

            return true;
        }

        return false;
    }
    function showNotificationPrompt() {
        console.log(DOM.notificationPrompt);
        if (!DOM.notificationPrompt) {
            return;
        }

        if (!("Notification" in window)) {
            return;
        }
        if (Notification.permission !== "default") {
            return;
        }

        if (!shouldShowNotificationPrompt()) {
            return;
        }
        DOM.notificationPrompt.hidden = false;
    }

    function markNotificationPrompted() {
        const expiresAt =
            Date.now() + CONFIG.timing.notificationPromptDelay;

        localStorage.setItem(
            CONFIG.storage.notificationPrompt, String(expiresAt)
        );

        DOM.notificationPrompt.hidden = true;
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

})();