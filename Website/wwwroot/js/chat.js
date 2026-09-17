

const popup = document.getElementById("promoPopup");

const hideToday = document.getElementById("hideToday");

if (!localStorage.getItem("hidePromo")) {

    setTimeout(() => {

        popup.classList.add("show");

    }, 1800);

}

function closePopup() {

    popup.classList.remove("show");

    if (hideToday.checked) {

        const tomorrow = new Date();

        tomorrow.setHours(23, 59, 59, 999);

        localStorage.setItem(
            "hidePromo",
            tomorrow.getTime()
        );

    }

}

document.getElementById("closePopup")
    .addEventListener("click", closePopup);

document.getElementById("laterBtn")
    .addEventListener("click", closePopup);

const hideTime = localStorage.getItem("hidePromo");

if (hideTime) {

    if (Date.now() > hideTime) {

        localStorage.removeItem("hidePromo");

    }

}


// CHATBOX
document.addEventListener("DOMContentLoaded", () => {

    // =====================================
    // ELEMENTS
    // =====================================

    const toggle = document.getElementById("chatToggle");
    const windowEl = document.getElementById("chatWindow");
    const close = document.getElementById("chatClose");

    const input = document.getElementById("chatInput");
    const send = document.getElementById("sendBtn");

    const body = document.getElementById("chatBody");
    const typing = document.getElementById("typing");
    const badge = document.getElementById("chatBadge");

    const inboxId = Number(windowEl.dataset.inboxId);

    const name = windowEl.dataset.name || null;

    const email = windowEl.dataset.email || null;

    const phone = windowEl.dataset.phone || null;
    // =====================================
    // SIGNALR
    // =====================================

    const chatConnection =
        new signalR.HubConnectionBuilder()
            .withUrl("/hubs/chat")
            .withAutomaticReconnect()
            .build();


    // =====================================
    // CHAT STATE
    // =====================================

    let currentConversationId = null;
    let currentContactId = null;
    let currentGuestToken = getGuestToken();
    const processedMessageIds = new Set();
    const MAX_PROCESSED_MESSAGE_IDS = 500;


    let unreadMessageCount = 0;

    let typingTimeout = null;
    let isTyping = false;

    const TYPING_TIMEOUT = 1500;
    // =====================================
    // UNREAD MESSAGE BADGE
    // =====================================

    function updateUnreadBadge() {

        if (!badge) {
            return;
        }

        if (unreadMessageCount <= 0) {

            unreadMessageCount = 0;

            badge.textContent = "";
            badge.hidden = true;

            return;
        }

        badge.textContent =
            unreadMessageCount > 99
                ? "99+"
                : String(unreadMessageCount);

        badge.hidden = false;
    }

    // =====================================
    // TYPING
    // =====================================

    async function sendTypingStatus(isTypingNow) {
        console.log(isTypingNow);
        if (
            !currentConversationId ||
            !currentContactId
        ) {
            return;
        }

        if (
            chatConnection.state !==
            signalR.HubConnectionState.Connected
        ) {
            return;
        }

        try {
            console.log(currentConversationId + " : " + currentContactId + " : " + currentGuestToken + " : " + isTypingNow);
            await chatConnection.invoke(
                "SendTyping",
                currentConversationId,
                currentContactId,
                currentGuestToken,
                isTypingNow
            );

        }
        catch (error) {

            console.error(
                "SEND TYPING ERROR:",
                error
            );

        }
    }
    function handleTyping() {
        if (!input.value.trim()) {
            stopTyping();
            return;
        }

        // Chỉ gửi true một lần khi bắt đầu gõ
        if (!isTyping) {
            isTyping = true;
            sendTypingStatus(true);
        }

        clearTimeout(typingTimeout);

        typingTimeout = setTimeout(() => {
            stopTyping();
        }, TYPING_TIMEOUT);
    }

    function stopTyping() {

        clearTimeout(typingTimeout);

        typingTimeout = null;

        if (!isTyping) {
            return;
        }

        isTyping = false;

        sendTypingStatus(false);
    }
    // =====================================
    // OPEN / CLOSE
    // =====================================

    async function openChat() {

        windowEl.classList.add("open");
        toggle.classList.add("active");

        await loadConversationMessages();
        const marked = await markConversationAsRead();

        if (marked) {
            setUnreadMessageCount(0);
        }


        setTimeout(() => {
            input.focus();
        }, 250);
    }


    function closeChat() {
        stopTyping();
        hideTyping();

        windowEl.classList.remove("open");
        toggle.classList.remove("active");
    }

    function setUnreadMessageCount(count) {
        unreadMessageCount = Math.max(
            0,
            Number(count) || 0
        );

        updateUnreadBadge();
    }

    function incrementUnreadMessage() { 
        unreadMessageCount++;

        updateUnreadBadge();
    }

    function updateUnreadBadge() {
        if (!badge) {
            return;
        }

        if (unreadMessageCount <= 0) {
            badge.textContent = "";
            badge.hidden = true;
            return;
        }

        badge.textContent =
            unreadMessageCount > 99
                ? "99+"
                : String(unreadMessageCount);

        badge.hidden = false;
    }

    toggle.addEventListener("click", () => {

        if (windowEl.classList.contains("open")) {
            closeChat();
        }
        else {
            openChat();
        }

    });


    close.addEventListener("click", closeChat);


    // =====================================
    // SIGNALR EVENTS
    // =====================================
    chatConnection.on("chat.admin.online", function (data) {
        console.log("ONLINE");
        console.log(data);
        setChatAdminStatus(true);
    });

    chatConnection.on("chat.admin.offline", function (data) {
        console.log("OFFLINE");
        console.log(data);
        setChatAdminStatus(false);
    });

    chatConnection.on(
        "chat.message.received",
        function (message) {

            console.log(
                "MESSAGE RECEIVED:",
                message
            );

            hideTyping();

            const added = addMessage(message, true);
            if (!added) {
                return;
            }
            // Chỉ tính tin nhắn của Admin/Bot
            // và chỉ tăng khi chat đang đóng.
            const isIncomingMessage =
                message.senderType == 2 ||
                message.senderType == 4;

            const isChatClosed =
                !windowEl.classList.contains("open");

            if (
                isIncomingMessage &&
                isChatClosed
            ) {
                incrementUnreadMessage();
            }
        }
    );

    chatConnection.onreconnecting(() => {

        console.log("Chat reconnecting...");
        stopTyping();
        hideTyping();
    });

    chatConnection.onreconnected(async () => {

        console.log("Chat reconnected.");
        hideTyping();
        // Sau khi SignalR reconnect,
        // connection cũ không còn giữ group.
        if (
            currentConversationId &&
            currentContactId
        ) {

            try {
                await chatConnection.invoke(
                    "JoinConversation",
                    currentConversationId,
                    currentContactId,
                    currentGuestToken
                );
                await loadConversationMessages();

                await loadUnreadCount();
                console.log(
                    "Conversation synchronized."
                );
            }
            catch (error) {

                console.error(
                    "Join conversation after reconnect failed:",
                    error
                );

            }

        }

    });

    chatConnection.on("chat.conversation.status.updated",
        function (data) {

            const conversationId = data.conversationId;
            const status = data.status;

            updateConversationStatus(
                conversationId,
                status
            );
        }
    );

    chatConnection.on(
        "chat.conversation.read",
        function (data) {
            if (Number(data.conversationId) !== Number(currentConversationId)
            ) {
                return;
            }

            setUnreadMessageCount(0);
        }
    );

    chatConnection.on("chat.typing",
        function (data) {
            console.log("CHATTYPING");
            console.log(data);
            console.log(currentConversationId);
            if (Number(data.conversationId) !== Number(currentConversationId)) {
                return;
            }

            // Chỉ xử lý typing của Admin/Bot
            if (data.senderType != 2 && data.senderType != 4) {
                return;
            }

            if (data.isTyping) {
                showTyping();
            }
            else {
                hideTyping();
            }

        }
    );

    chatConnection.onclose(() => {

        console.log("Chat connection closed.");

    });

    // =====================================
    // START CHAT
    // =====================================

    async function startChatConnection() {

        try {

            // -----------------------------
            // 1. Connect SignalR
            // -----------------------------

            if (
                chatConnection.state !==
                signalR.HubConnectionState.Connected
            ) {

                await chatConnection.start();
                const isAdminOnline =  await chatConnection.invoke("IsAnyAdminOnline");
                setChatAdminStatus(isAdminOnline);
                console.log("Chat connected");

            }


            // -----------------------------
            // 2. Get existing session
            // -----------------------------

            currentContactId = Number(
                    localStorage.getItem(
                        "caterin_chat_contact_id"
                    )
                ) || null;


            currentConversationId =
                Number(
                    localStorage.getItem(
                        "caterin_chat_conversation_id"
                    )
                ) || null;


            currentGuestToken = getGuestToken();


            console.log("Existing chat session:", {
                inboxId: inboxId,
                contactId: currentContactId,
                conversationId: currentConversationId,
                hasGuestToken: !!currentGuestToken
            });
            // -----------------------------
            // 3. Existing chat
            // -----------------------------

            if (currentContactId && currentConversationId) {

                console.log(
                    "Existing chat session found."
                );

                await loadUnreadCount();

                await chatConnection.invoke(
                    "JoinConversation",
                    currentConversationId,
                    currentContactId,
                    currentGuestToken
                );
                console.log(
                    "Joined existing conversation:",
                    currentConversationId
                );

                await loadConversationMessages();
                return;
            }
            // ==========================================
            // NEW SESSION
            // ==========================================

            console.log(
                "No existing chat session. Starting..."
            );

            // -----------------------------
            // 4. Create new chat
            // -----------------------------
            const response = await fetch("/chat/start",
                {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body: JSON.stringify({
                        inboxId: inboxId,
                        name: name,
                        email: email,
                        phone: phone,
                        guestToken: currentGuestToken
                    })
                }
            );


            if (!response.ok) {
                throw new Error("Không thể bắt đầu cuộc trò chuyện." );

            }


            const conversation =
                await response.json();


            // -----------------------------
            // 5. Save chat session
            // -----------------------------

            currentContactId =
                conversation.contactId;

            currentConversationId =
                conversation.id;

            localStorage.setItem(
                "caterin_chat_contact_id",
                String(currentContactId)
            );

            localStorage.setItem(
                "caterin_chat_conversation_id",
                String(currentConversationId)
            );

            localStorage.setItem(
                "caterin_chat_guest_token",
                currentGuestToken
            );


            // -----------------------------
            // 6. Join conversation
            // -----------------------------

            await chatConnection.invoke(
                "JoinConversation",
                currentConversationId,
                currentContactId,
                currentGuestToken
            );
            await loadConversationMessages();

            console.log(
                "Joined new conversation:",
                currentConversationId
            );

        }
        catch (error) {

            console.error(
                "SignalR connection failed:",
                error
            );

            setTimeout(
                startChatConnection,
                3000
            );
        }

    }
    function getGuestToken() {
        let token = localStorage.getItem(
            "caterin_chat_guest_token"
        );

        if (!token) {
            token = crypto.randomUUID();

            localStorage.setItem(
                "caterin_chat_guest_token",
                token
            );
        }

        return token;
    }
    // =====================================
// LOAD CONVERSATION MESSAGES
// =====================================
    function setChatAdminStatus(isOnline) {
        const dot = document.getElementById("chatAdminStatusDot");
        const status = document.getElementById("chatStatusText");

        if (!dot || !status) {
            return;
        }
        const statusDot = status.querySelector("span:first-child");
        const statusLabel = status.querySelector(".status-label");

        const label = status.querySelector(".status-label");

        dot.classList.toggle("online", isOnline);
        dot.classList.toggle("offline", !isOnline);

        status.classList.toggle("online", isOnline);
        status.classList.toggle("offline", !isOnline);
        if (statusDot) {
            statusDot.classList.toggle("online", isOnline);
            statusDot.classList.toggle("offline", !isOnline);
        }
        if (label) {
            label.textContent = isOnline
                ? "Đang trực tuyến"
                : "Đang ngoại tuyến";
        }
    }


    function updateAdminPresence(adminId, isOnline) {
        document
            .querySelectorAll(`[data-admin-id="${adminId}"]`)
            .forEach(element => {
                element.classList.toggle("is-online", isOnline);
                element.classList.toggle("is-offline", !isOnline);

                const dot =
                    element.querySelector(".admin-status-dot");

                if (dot) {
                    dot.classList.toggle("online", isOnline);
                }

                const text =
                    element.querySelector(".admin-status-text");

                if (text) {
                    text.textContent =
                        isOnline ? "Online" : "Offline";
                }
            });
    }
async function loadConversationMessages() {

    if (
        !currentConversationId ||
        !currentContactId ||
        !currentGuestToken
    ) {
        return;
    }

    try {

        const response =
            await fetch(
                `/chat/${currentConversationId}/messages`,
                {
                    method: "GET",

                    headers: {
                        "Accept": "application/json",
                        "X-Chat-Contact-Id":
                            String(currentContactId),
                        "X-Chat-Guest-Token":
                            currentGuestToken
                    },

                    credentials: "same-origin"
                }
            );


        if (!response.ok) {

            throw new Error(
                `Không thể tải tin nhắn. Status: ${response.status}`
            );
        }


        const messages =
            await response.json();


        if (!Array.isArray(messages)) {

            console.error(
                "Messages response không hợp lệ:",
                messages
            );

            return;
        }


        /*
         * Clear message UI trước khi render history.
         */
        body.innerHTML = "";

        processedMessageIds.clear();
        /*
         * Render theo thứ tự:
         *
         * message cũ
         *      ↓
         * message mới
         */
        messages.forEach(message => {

            if (!message?.id) {
                return;
            }

            /*
             * Đánh dấu message đã được xử lý.
             *
             * Nếu SignalR gửi lại message này
             * sau khi load history thì addMessage()
             * sẽ bỏ qua.
             */
            addMessage(message, false);

        });


        scrollBottom(false);

    }
    catch (error) {

        console.error(
            "LOAD MESSAGES ERROR:",
            error
        );

    }
    }

    async function markConversationAsRead() {

        if (
            !currentConversationId ||
            !currentContactId ||
            !currentGuestToken
        ) {
            return false;
        }

        try {

            const response = await fetch(
                `/chat/${currentConversationId}/read`,
                {
                    method: "POST",

                    headers: {
                        "Accept": "application/json",
                        "X-Chat-Contact-Id":
                            String(currentContactId),
                        "X-Chat-Guest-Token":
                            currentGuestToken
                    },

                    credentials: "same-origin"
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

            console.error(
                "MARK CONVERSATION READ ERROR:",
                error
            );

            return false;
        }
    }

    async function loadUnreadCount() {
        console.log("LOAD UNREAD COUNT");
        console.log('id converssation: ' + currentConversationId);
        if (!currentConversationId) {
            setUnreadMessageCount(0);
            return;
        }

        try {
            const response = await fetch(
                `/chat/${currentConversationId}/unread-count`,
                {
                    method: "GET",
                    headers: {
                        "Accept": "application/json",
                        "X-Chat-Contact-Id":
                            currentContactId ?? "",
                        "X-Chat-Guest-Token":
                            currentGuestToken ?? ""
                    },
                    credentials: "same-origin"
                }
            );

            if (!response.ok) {
                throw new Error(
                    "Failed to load unread count."
                );
            }

            const result = await response.json();

            setUnreadMessageCount(
                result.data ?? result
            );
        }
        catch (error) {
            console.error(
                "Load unread count error:",
                error
            );
        }
    }
    // =====================================
    // SEND MESSAGE
    // =====================================

    async function sendMessage(text) {

        console.log("SEND MESSAGE START");

        text = text.trim();

        if (!text) {
            return;
        }
        stopTyping();
        if (
            !currentConversationId ||
            !currentContactId
        ) {
            console.error(
                "Chat session chưa được khởi tạo."
            );
            return;
        }

        if (
            chatConnection.state !==
            signalR.HubConnectionState.Connected
        ) {
            console.error(
                "SignalR chưa kết nối."
            );
            return;
        }

        input.value = "";

        try {

            await chatConnection.invoke(
                "SendCustomerMessage",
                currentConversationId,
                currentContactId,
                text,
                currentGuestToken
            );

            console.log("MESSAGE SENT");

        }
        catch (error) {

            console.error(
                "SEND MESSAGE ERROR:",
                error
            );

            input.value = text;
            input.focus();
        }
    }


    // =====================================
    // SEND BUTTON
    // =====================================

    send.addEventListener(
        "click",
        async () => {

            await sendMessage(
                input.value
            );

        }
    );


    // =====================================
    // ENTER
    // =====================================

    input.addEventListener(
        "keydown",
        async e => {

            if (
                e.key === "Enter" &&
                !e.shiftKey
            ) {

                e.preventDefault();

                await sendMessage(
                    input.value
                );

            }

        }
    );
    input.addEventListener(
        "input",
        handleTyping
    );

    // =====================================
    // QUICK REPLIES
    // =====================================

    document
        .querySelectorAll(
            ".quick-replies button"
        )
        .forEach(button => {

            button.addEventListener(
                "click",
                async () => {

                    const message =
                        button.dataset.message;

                    await sendMessage(
                        message
                    );

                }
            );

        });


    // =====================================
    // ADD MESSAGE FROM SIGNALR
    // =====================================

    function isMessageProcessed(messageId) {

        if (!messageId) {
            return true;
        }

        if (processedMessageIds.has(messageId)) {
            return true;
        }

        processedMessageIds.add(messageId);

        if (
            processedMessageIds.size >
            MAX_PROCESSED_MESSAGE_IDS
        ) {

            const firstId =
                processedMessageIds
                    .values()
                    .next()
                    .value;

            if (firstId !== undefined) {
                processedMessageIds.delete(firstId);
            }
        }

        return false;
    }
    function addMessage(message, shouldScroll = true) {
        if (!message?.id) {
            return false;
        }

        if (isMessageProcessed(message.id)) {
            return false;
        }

        const row = document.createElement("div");

        const isContact = message.senderType == 1;

        const isAdmin = message.senderType == 2;

        const isSystem = message.senderType == 3;

        const isBot = message.senderType == 4;

        if (isContact) {
            row.className = "message-row contact";
        }
        else if (isAdmin) {

            row.className = "message-row admin";

        }
        else if (isBot) {

            row.className = "message-row bot";

        }
        else if (isSystem) {

            row.className = "message-row system";

        }
        else {

            console.warn(
                "Unknown sender type:",
                message.senderType
            );

            return false;
        }

        let messageClass;

        if (isContact) {
            messageClass = "contact-message";
        }
        else if (isAdmin) {
            messageClass = "admin-message";
        }
        else if (isBot) {
            messageClass = "bot-message";
        }
        else {
            messageClass = "system-message";
        }


        row.innerHTML = `
        <div>
            <div class="message ${messageClass}">
                ${escapeHtml(message.content ?? "")}
            </div>

            <div class="message-time">
                ${formatMessageTime(message.createdAt)}
            </div>
        </div>
    `;

        body.appendChild(row);

        if (shouldScroll) {
            scrollBottom(true);
        }
        return true;
    }

    // =====================================
    // UPDATE CONVERSATION STATUS
    // =====================================

    function updateConversationStatus(conversationId, status) {
        //const item = document.querySelector(`[data-conversation-id="${conversationId}"]`);

        //if (item) {
        //    item.dataset.status = status;

        //    const badge = item.querySelector(".conversation-status"
        //    );

        //    if (badge) {
        //        badge.textContent = status;
        //    }
        //}

        //if (currentConversationId === conversationId) {
        //    updateCurrentConversationStatus(status);
        //}
        console.log("START UPDATE STATUS");
        if (
            Number(currentConversationId) !==
            Number(conversationId)
        ) {
            return;
        }

        console.log(
            "Conversation status updated:",
            {
                conversationId: conversationId,
                status: status
            }
        );

        updateCurrentConversationStatus(status);
    }
    function updateCurrentConversationStatus(status) {

        const normalizedStatus = getConversationStatusName(status);

        // =====================================
        // STATUS TEXT
        // =====================================

        const statusText =
            document.getElementById("chatStatusText");

        if (statusText) {
            const statusMap = {
                open: "Đang hỗ trợ",
                pending: "Đang chờ",
                resolved: "Đã giải quyết",
                closed: "Đã đóng"
            };

            statusText.textContent = statusMap[normalizedStatus] ?? status;
        }


        // =====================================
        // STATUS BADGE
        // =====================================

        const statusBadge = document.getElementById("chatStatusBadge");

        if (statusBadge) {

            statusBadge.dataset.status =
                normalizedStatus;

            statusBadge.classList.remove(
                "open",
                "pending",
                "resolved",
                "closed"
            );

            statusBadge.classList.add(
                normalizedStatus
            );
        }


        // =====================================
        // CHAT WINDOW
        // =====================================

        windowEl.dataset.status = normalizedStatus;
        const isEnded =
            normalizedStatus === "resolved" ||
            normalizedStatus === "closed";


        // =====================================
        // INPUT STATE
        // =====================================

        const isClosed =
            normalizedStatus === "closed";

        const isResolved =
            normalizedStatus === "resolved";

        input.disabled = isEnded;
        send.disabled = isEnded;

        input.placeholder = isEnded ? "Cuộc trò chuyện đã kết thúc" : "Nhập tin nhắn...";
    }
    function getConversationStatusName(status) {
        const statusMap = {
            1: "open",
            2: "pending",
            3: "resolved",
            4: "closed"
        };

        return statusMap[Number(status)] ?? "open";
    }
    // =====================================
    // TYPING
    // =====================================

    function showTyping() {
        typing.hidden = false;
        scrollBottom();

    }


    function hideTyping() {
        typing.hidden = true;
    }


    // =====================================
    // SCROLL
    // =====================================

    function scrollBottom(smooth = true) {
        requestAnimationFrame(() => {

            body.scrollTo({
                top: body.scrollHeight,
                behavior: smooth ? "smooth" : "auto"
            });

        });

    }


    // =====================================
    // TIME
    // =====================================

    function getCurrentTime() {

        return new Date().toLocaleTimeString(
            "vi-VN",
            {
                hour: "2-digit",
                minute: "2-digit"
            }
        );

    }


    function formatMessageTime(date) {

        return new Date(date)
            .toLocaleTimeString(
                "vi-VN",
                {
                    hour: "2-digit",
                    minute: "2-digit"
                }
            );

    }


    // =====================================
    // ESCAPE HTML
    // =====================================

    function escapeHtml(value) {

        const div =
            document.createElement("div");

        div.textContent = value;

        return div.innerHTML;

    }


    // =====================================
    // ATTACHMENT
    // =====================================

    document
        .getElementById("attachmentBtn")
        .addEventListener(
            "click",
            () => {

                alert(
                    "Chức năng gửi file sẽ được tích hợp sau."
                );

            }
        );


    // =====================================
    // EMOJI
    // =====================================

    document
        .getElementById("emojiBtn")
        .addEventListener(
            "click",
            () => {

                input.value += " 😊";

                input.focus();

            }
        );
    document.addEventListener(
        "keydown",
        e => {

            if (e.key === "Escape" && windowEl.classList.contains("open")) {
                closeChat();
            }

        }
    );


    // =====================================
    // INIT
    // =====================================

    startChatConnection();

});