

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

    // =====================================
    // OPEN / CLOSE
    // =====================================

    function openChat() {

        windowEl.classList.add("open");
        toggle.classList.add("active");

        badge.style.display = "none";

        setTimeout(() => {
            input.focus();
        }, 250);
    }


    function closeChat() {

        windowEl.classList.remove("open");
        toggle.classList.remove("active");
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

    chatConnection.on(
        "chat.message.received",
        function (message) {

            console.log(
                "MESSAGE RECEIVED:",
                message
            );

            hideTyping();

            addMessage(message);
        }
    );


    chatConnection.onreconnecting(() => {

        console.log("Chat reconnecting...");

    });


    chatConnection.onreconnected(async () => {

        console.log("Chat reconnected.");

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

            }
            catch (error) {

                console.error(
                    "Join conversation after reconnect failed:",
                    error
                );

            }

        }

    });


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
                `/chat/conversations/${currentConversationId}/messages`,
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
            addMessage(message);

        });


        scrollBottom();

    }
    catch (error) {

        console.error(
            "LOAD MESSAGES ERROR:",
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
    function addMessage(message) {
        console.log(message);
        if (!message?.id) {
            return;
        }

        if (isMessageProcessed(message.id)) {
            return;
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

            return;
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

        scrollBottom();
    }

    // =====================================
    // TYPING
    // =====================================

    function showTyping() {

        typing.classList.add("show");

        scrollBottom();

    }


    function hideTyping() {

        typing.classList.remove("show");

    }


    // =====================================
    // SCROLL
    // =====================================

    function scrollBottom() {

        requestAnimationFrame(() => {

            body.scrollTo({
                top: body.scrollHeight,
                behavior: "smooth"
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