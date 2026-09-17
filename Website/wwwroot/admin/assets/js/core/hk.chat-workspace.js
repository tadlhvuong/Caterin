/**
 * Caterin Chat Workspace
 */

'use strict';

window.ChatScroll = {

    instances: new Map(),

    init(element) {

        if (
            typeof PerfectScrollbar === 'undefined' ||
            !element
        ) {
            return null;
        }

        const id = element.id;

        if (!id) {
            console.warn(
                'PerfectScrollbar element must have an id.'
            );

            return null;
        }

        if (this.instances.has(id)) {
            return this.instances.get(id);
        }

        const scrollbar =
            new PerfectScrollbar(
                element,
                {
                    wheelPropagation: false,
                    suppressScrollX: true
                }
            );

        this.instances.set(
            id,
            scrollbar
        );

        return scrollbar;
    },

    update(element) {

        if (!element) {
            return;
        }

        const scrollbar = this.instances.get(element.id);

        if (scrollbar) {
            scrollbar.update();
        }
    },

    destroy(element) {

        if (!element) {
            return;
        }

        const scrollbar =
            this.instances.get(element.id);

        if (!scrollbar) {
            return;
        }

        scrollbar.destroy();

        this.instances.delete(
            element.id
        );
    },

    initAll(container = document) {

        container
            .querySelectorAll('[data-perfect-scrollbar]')
            .forEach(element => {
                this.init(element);
            });
    }
};

document.addEventListener('DOMContentLoaded', function () {

    const workspace = document.getElementById('chatWorkspace');
    ChatScroll.initAll(workspace);

    if (!workspace) {
        return;
    }

    /* =========================================================
       ELEMENTS
    ========================================================= */

    const filterSidebar =
        document.getElementById('chatFilterSidebar');

    const filterClose =
        document.getElementById('chatFilterClose');

    const openChatFiltersButton =
        document.getElementById('openChatFilters');

    const overlay =
        document.getElementById('chatWorkspaceOverlay');

    const conversationPanel =
        document.getElementById('conversationPanel');

    const chatDetailPanel =
        document.getElementById('chatDetailPanel');

    const conversationList =
        document.getElementById('conversationList');

    const conversationSearch =
        document.getElementById('conversationSearch');

    const backToConversationsButton =
        document.getElementById('backToConversations');

    const chatContactButton =
        document.getElementById('chatContactButton');

    const contactDrawer =
        document.getElementById('chatContactDrawer');

    const closeContactDrawerButton =
        document.getElementById('closeContactDrawer');

    const sendMessageForm =
        document.getElementById('sendMessageForm');

    const messageInput =
        document.getElementById('messageInput');

    const chatHistoryBody =
        document.getElementById('chatHistoryBody');

    const chatHistory =
        document.getElementById('chatHistory');

    const refreshConversations =
        document.getElementById('refreshConversations');


    /* =========================================================
       STATE
    ========================================================= */

    let selectedConversationId = null;
    let adminChatConnection = null;
    let joinedConversationId = null;
    const processedMessageIds = new Set();
    const MAX_PROCESSED_MESSAGE_IDS = 500;
    let currentFilters = {
        inbox: 'all',
        status: null,
        label: null,
        search: ''
    };

    /* =========================================================
   SIGNALR
========================================================= */

    async function initChatSignalR() {

        if (typeof signalR === 'undefined') {
            console.error('SignalR is not loaded.');
            return;
        }

        adminChatConnection =
            new signalR.HubConnectionBuilder()
                .withUrl('/hubs/chat')
                .withAutomaticReconnect()
                .build();


        /* =====================================================
           RECEIVE MESSAGE
        ===================================================== */

        adminChatConnection.on(
            'chat.message.received',
            function (message) {
                console.log(
                    '=== MESSAGE RECEIVED ===',
                    {
                        id: message?.id,
                        conversationId: message?.conversationId,
                        selectedConversationId,
                        processed: message?.id
                            ? processedMessageIds.has(message.id)
                            : null
                    }
                );

                if (!message?.id) {
                    console.log('RETURN: no message id');
                    return;
                }

                if (isMessageProcessed(message.id)) {
                    console.log(
                        'RETURN: duplicate',
                        message.id
                    );
                    return;
                }

                console.log(
                    'NEW MESSAGE:',
                    message.id
                );


                /*
                 * Conversation list
                 */
                const isCurrentConversation =
                    selectedConversationId &&
                    String(message.conversationId) ===
                    String(selectedConversationId);

                if (isCurrentConversation) {

                    // Đang mở Details
                    appendChatMessage(message);
                    scrollChatToBottom();

                } else {

                    // Không mở conversation này
                    updateConversationList(message);
                }
            }
        );


        /* =====================================================
           CONNECTION EVENTS
        ===================================================== */

        adminChatConnection.onreconnecting(
            function (error) {

                console.warn(
                    'Chat SignalR reconnecting...',
                    error
                );

            }
        );


        adminChatConnection.onreconnected(
            async function (connectionId) {

                console.log(
                    'Chat SignalR reconnected:',
                    connectionId
                );


                /*
                 * Sau khi reconnect:
                 * JoinInbox lại.
                 */

                await joinInbox();

                /*
                 * Nếu đang mở conversation
                 * thì join lại conversation.
                 */

                if (selectedConversationId) {

                    await joinConversation(
                        selectedConversationId
                    );

                }

            }
        );


        adminChatConnection.onclose(
            function (error) {

                console.warn(
                    'Chat SignalR connection closed.',
                    error
                );

            }
        );


        /* =====================================================
           START CONNECTION
        ===================================================== */

        try {

            await adminChatConnection.start();

            console.log(
                'Chat SignalR connected.'
            );


            await joinInbox();


        } catch (error) {

            console.error(
                'Chat SignalR connection failed:',
                error
            );

        }

    }

    async function joinInbox() {

        if (
            !adminChatConnection ||
            adminChatConnection.state !==
            signalR.HubConnectionState.Connected
        ) {
            return;
        }


        /*
         * inboxId nên lấy từ page/ViewModel/data attribute.
         *
         * Ví dụ:
         *
         * <div id="chatWorkspace"
         *      data-inbox-id="1">
         */

        const inboxId =
            workspace.dataset.inboxId;


        if (!inboxId) {

            console.warn(
                'InboxId is missing.'
            );

            return;

        }


        try {

            await adminChatConnection.invoke(
                'JoinInbox',
                Number(inboxId)
            );

            console.log(
                'Joined inbox:',
                inboxId
            );

        } catch (error) {

            console.error(
                'JoinInbox failed:',
                error
            );

        }

    }

    async function openConversation(conversationItem) {

        if (!conversationItem) {
            return;
        }

        selectedConversationId =
            conversationItem.dataset.conversationId;


        document
            .querySelectorAll('.conversation')
            .forEach(item => {
                item.classList.remove('active');
            });

        conversationItem.classList.add('active');

        conversationPanel.classList.add('d-none');

        chatDetailPanel.classList.remove('d-none');

        closeContactDrawer();

        if (isMobile()) {
            closeFilterSidebar();
        }

        updateChatHeader(
            conversationItem
        );

        /*
         * Join SignalR conversation room
         */
        await joinConversation(
            selectedConversationId
        );

        /*
         * Load messages
         */
        await loadConversation(
            selectedConversationId
        );

    }

    async function joinConversation(
        conversationId
    ) {

        if (
            !conversationId ||
            !adminChatConnection ||
            adminChatConnection.state !==
            signalR.HubConnectionState.Connected
        ) {
            return;
        }


        /*
         * Không Join lại cùng conversation
         */
        if (
            String(joinedConversationId) ===
            String(conversationId)
        ) {
            return;
        }


        /*
         * Nếu trước đó đã join conversation khác
         * thì rời room cũ trước.
         */

        if (joinedConversationId) {

            try {

                await adminChatConnection.invoke(
                    'LeaveAdminConversation',
                    Number(joinedConversationId)
                );

            } catch (error) {

                console.warn(
                    'LeaveAdminConversation failed:',
                    error
                );

            }

        }


        try {

            await adminChatConnection.invoke(
                'JoinAdminConversation',
                Number(conversationId)
            );

            joinedConversationId =
                conversationId;

            console.log(
                'Joined conversation:',
                conversationId
            );

        } catch (error) {

            console.error(
                'JoinAdminConversation failed:',
                error
            );

        }

    }
    function appendChatMessage(message) {
        if (!message || !chatHistory) {
            console.warn('Cannot append message.', {
                message,
                chatHistory
            });

            return;
        }

        const li = document.createElement('li');

        const isContact =
            message.senderType === 1;

        const isAdmin =
            message.senderType === 2;


        const isSystem =
            message.senderType === 3;

        const isBot =
            message.senderType === 4;

        console.log('Rendering message:', message.id, message.senderType, message.content
        );

        if (isSystem) {
            li.className = 'chat-message text-center';

            li.innerHTML = `
            <div class="text-muted">
                <small>
                    ${escapeHtml(message.content || '')}
                </small>
            </div>
        `;

            chatHistory.appendChild(li);
            ChatScroll.update(chatHistoryBody);
            return;
        }

        if (!isAdmin && !isContact) {
            console.warn(
                'Unknown sender type:',
                message.senderType
            );

            return;
        }

        li.className = isAdmin
            ? 'chat-message chat-message-right'
            : 'chat-message';

        li.innerHTML = `
        <div class="d-flex overflow-hidden">

            <div class="chat-message-wrapper">

                ${isContact
                ? `
                            <div class="text-muted mb-1">
                                <small>
                                    ${escapeHtml(
                    message.senderName ||
                    'Contact'
                )}
                                </small>
                            </div>
                        `
                : ''
            }

                <div class="chat-message-text">
                    <p class="mb-0 text-break">
                        ${escapeHtml(
                message.content || ''
            )}
                    </p>
                </div>

                <div class="${isAdmin ? 'text-end' : ''
            } text-muted mt-1">

                    <small>
                        ${formatMessageTime(
                message.createdAt
            )}
                    </small>

                </div>

            </div>

        </div>
    `;

        chatHistory.appendChild(li);

        console.log('Message appended:', message.id
        );
    }
    function formatMessageTime(value) {

        if (!value) {
            return '';
        }

        const date =
            new Date(value);

        if (Number.isNaN(date.getTime())) {
            return '';
        }

        return date.toLocaleTimeString(
            'vi-VN',
            {
                hour: '2-digit',
                minute: '2-digit'
            }
        );
    }
    function getMessageStatusIcon(status) {

        switch (status) {

            case 'Sending':
                return `
                <i class="ti ti-clock ti-16px me-1"></i>
            `;

            case 'Sent':
                return `
                <i class="ti ti-check ti-16px me-1"></i>
            `;

            case 'Delivered':
                return `
                <i class="ti ti-checks ti-16px me-1"></i>
            `;

            case 'Read':
                return `
                <i class="ti ti-checks ti-16px text-success me-1"></i>
            `;

            case 'Failed':
                return `
                <i class="ti ti-alert-circle ti-16px text-danger me-1"></i>
            `;

            default:
                return '';
        }
    }
    /* =========================================================
       PERFECT SCROLLBAR
    ========================================================= */

    let chatHistoryScrollbar = null;

    if (
        typeof PerfectScrollbar !== 'undefined' &&
        chatHistoryBody
    ) {
        chatHistoryScrollbar = new PerfectScrollbar(
            chatHistoryBody,
            {
                wheelPropagation: false,
                suppressScrollX: true
            }
        );
    }


    /* =========================================================
       HELPERS
    ========================================================= */

    function scrollChatToBottom() {

        if (!chatHistoryBody) {
            return;
        }

        requestAnimationFrame(() => {

            chatHistoryBody.scrollTop =
                chatHistoryBody.scrollHeight;

            if (chatHistoryScrollbar) {
                chatHistoryScrollbar.update();
            }

        });
    }

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
                processedMessageIds.values().next().value;

            if (firstId !== undefined) {
                processedMessageIds.delete(firstId);
            }
        }

        return false;
    }
    function isMobile() {

        return window.innerWidth < 992;

    }


    function openFilterSidebar() {

        if (!filterSidebar) {
            return;
        }

        filterSidebar.classList.add('show');

        if (overlay) {
            overlay.classList.add('show');
        }

    }


    function closeFilterSidebar() {

        if (!filterSidebar) {
            return;
        }

        filterSidebar.classList.remove('show');

        updateOverlay();

    }


    function updateOverlay() {

        const filterOpen =
            filterSidebar &&
            filterSidebar.classList.contains('show');

        const contactOpen =
            contactDrawer &&
            contactDrawer.classList.contains('show');

        if (filterOpen || contactOpen) {

            overlay?.classList.add('show');

        } else {

            overlay?.classList.remove('show');

        }

    }


    /* =========================================================
       CONVERSATION DETAIL
    ========================================================= */

    function updateChatHeader(conversationItem) {

        const name =
            conversationItem
                .querySelector('.conversation-name')
                ?.textContent
                ?.trim();

        const contactName =
            document.getElementById('chatContactName');

        if (contactName && name) {
            contactName.textContent = name;
        }

        const status =
            conversationItem.dataset.status;

        const statusElement =
            document.getElementById('chatContactStatus');

        if (statusElement) {

            const statusText = {
                open: 'Open',
                pending: 'Pending',
                resolved: 'Resolved'
            };

            statusElement.textContent =
                statusText[status] || status;

        }

    }


    async function closeConversation() {

        if (
            joinedConversationId &&
            adminChatConnection &&
            adminChatConnection.state ===
            signalR.HubConnectionState.Connected
        ) {

            try {

                await adminChatConnection.invoke(
                    'LeaveAdminConversation',
                    Number(joinedConversationId)
                );

            } catch (error) {

                console.warn(
                    'LeaveAdminConversation failed:',
                    error
                );

            }

        }


        joinedConversationId = null;
        selectedConversationId = null;

        closeContactDrawer();

        chatDetailPanel.classList.add('d-none');

        conversationPanel.classList.remove('d-none');

    }


    /* =========================================================
       LOAD CONVERSATION
       =========================================================
       
       Sau này đổi hàm này thành HTMX:
       
       GET /admin/chat/conversations/{id}
       
       hoặc:
       
       htmx.ajax('GET', ...)
       
    ========================================================= */
    //Fix sau: Khi chuyển từ conversation A sang B rồi quay lại A, processedMessageIds vẫn còn dữ liệu cũ.
    async function loadConversation(conversationId) {

        if (!conversationId) {
            return;
        }

        if (!chatHistory) {
            return;
        }

        try {

            // Hiển thị trạng thái loading
            chatHistory.innerHTML = `
            <li class="chat-loading">
                <span>Đang tải tin nhắn...</span>
            </li>
        `;

            const response = await fetch(
                `/admin/chat/${conversationId}/messages`,
                {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    },
                    credentials: 'same-origin'
                }
            );

            if (!response.ok) {
                throw new Error(
                    `Load messages failed: ${response.status}`
                );
            }

            const messages = await response.json();

            // Nếu user đã chuyển sang conversation khác
            // trong lúc request đang chạy thì không render nữa.
            if (
                String(selectedConversationId) !==
                String(conversationId)
            ) {
                return;
            }

            // Xóa loading + messages cũ
            chatHistory.innerHTML = '';

            if (!Array.isArray(messages) || messages.length === 0) {
                chatHistory.innerHTML = `
                <li class="chat-empty">
                    <span>Chưa có tin nhắn.</span>
                </li>
            `;

                return;
            }

            /*
             * API thường trả message theo ASC:
             *
             * oldest
             *   ↓
             * newest
             *
             * Render theo đúng thứ tự đó.
             */
            //Fix cơ chế sau này: chúng ta có thể thêm cơ chế loadingConversationId/buffer realtime.
            messages.forEach(message => {

                if (!message?.id) {
                    return;
                }

                /*
                 * Đánh dấu message lịch sử đã được xử lý.
                 *
                 * Nếu ngay sau đó SignalR gửi lại message này
                 * thì chat.message.received sẽ bị deduplicate.
                 */
                isMessageProcessed(message.id);

                appendChatMessage(message);
            });

            ChatScroll.update(chatHistoryBody);
            scrollChatToBottom();

        } catch (error) {

            console.error(
                'Load conversation failed:',
                error
            );

            // Chỉ render lỗi nếu vẫn đang ở conversation này
            if (
                String(selectedConversationId) !==
                String(conversationId)
            ) {
                return;
            }

            chatHistory.innerHTML = `
            <li class="chat-error">
                <span>Không thể tải tin nhắn.</span>
                <button type="button"
                        class="chat-retry"
                        data-conversation-id="${conversationId}">
                    Thử lại
                </button>
            </li>
        `;
        }
    }


    /* =========================================================
       FILTER
    ========================================================= */

    const filterItems =
        document.querySelectorAll(
            '.chat-filter-list li[data-filter-type]'
        );


    filterItems.forEach(filterItem => {

        filterItem.addEventListener(
            'click',
            function () {

                const type =
                    this.dataset.filterType;

                const value =
                    this.dataset.filterValue;

                /*
                 * Cho phép bỏ filter status/label
                 * khi click lại item đang active.
                 */

                if (
                    this.classList.contains('active') &&
                    type !== 'inbox'
                ) {

                    this.classList.remove('active');

                    currentFilters[type] = null;

                } else {

                    if (type === 'inbox') {

                        document
                            .querySelectorAll(
                                '.chat-filter-list li[data-filter-type="inbox"]'
                            )
                            .forEach(item => {
                                item.classList.remove('active');
                            });

                    }

                    if (type === 'status') {

                        document
                            .querySelectorAll(
                                '.chat-filter-list li[data-filter-type="status"]'
                            )
                            .forEach(item => {
                                item.classList.remove('active');
                            });

                    }

                    if (type === 'label') {

                        document
                            .querySelectorAll(
                                '.chat-filter-list li[data-filter-type="label"]'
                            )
                            .forEach(item => {
                                item.classList.remove('active');
                            });

                    }

                    this.classList.add('active');

                    currentFilters[type] = value;

                }

                applyConversationFilters();

                if (isMobile()) {
                    closeFilterSidebar();
                }

            }
        );

    });


    /* =========================================================
       SEARCH
    ========================================================= */

    if (conversationSearch) {

        conversationSearch.addEventListener(
            'input',
            function () {

                currentFilters.search =
                    this.value
                        .trim()
                        .toLowerCase();

                applyConversationFilters();

            }
        );

    }


    function applyConversationFilters() {

        const items =
            document.querySelectorAll(
                '.conversation-item'
            );

        let visibleCount = 0;

        items.forEach(item => {

            const name =
                item.querySelector(
                    '.conversation-name'
                )
                    ?.textContent
                    ?.toLowerCase() || '';

            const preview =
                item.querySelector(
                    '.conversation-preview'
                )
                    ?.textContent
                    ?.toLowerCase() || '';

            const status =
                item.dataset.status;

            const inbox =
                item.dataset.inbox;

            const label =
                item.dataset.label;

            let visible = true;


            /* Search */

            if (currentFilters.search) {

                const matched =
                    name.includes(
                        currentFilters.search
                    ) ||
                    preview.includes(
                        currentFilters.search
                    );

                if (!matched) {
                    visible = false;
                }

            }


            /* Inbox */

            if (
                visible &&
                currentFilters.inbox &&
                currentFilters.inbox !== 'all'
            ) {

                if (
                    inbox !== currentFilters.inbox
                ) {
                    visible = false;
                }

            }


            /* Status */

            if (
                visible &&
                currentFilters.status
            ) {

                if (
                    status !== currentFilters.status
                ) {
                    visible = false;
                }

            }


            /* Label */

            if (
                visible &&
                currentFilters.label
            ) {

                if (
                    label !== currentFilters.label
                ) {
                    visible = false;
                }

            }


            item.classList.toggle(
                'd-none',
                !visible
            );

            if (visible) {
                visibleCount++;
            }

        });


        updateConversationTotal(
            visibleCount
        );

    }


    function updateConversationTotal(count) {

        const element =
            document.getElementById(
                'conversationTotal'
            );

        if (!element) {
            return;
        }

        element.textContent =
            `${count} conversations`;

    }


    /* =========================================================
       CONVERSATION CLICK
    ========================================================= */

    if (conversationList) {

        conversationList.addEventListener(
            'click',
            function (event) {

                const conversationItem =
                    event.target.closest(
                        '.conversation-item'
                    );

                if (!conversationItem) {
                    return;
                }

                openConversation(conversationItem);

            }
        );

    }


    /* =========================================================
       BACK
    ========================================================= */

    if (backToConversationsButton) {

        backToConversationsButton.addEventListener(
            'click',
            function () {

                closeConversation();

            }
        );

    }


    /* =========================================================
       CONTACT DRAWER
    ========================================================= */

    if (chatContactButton) {

        chatContactButton.addEventListener(
            'click',
            function () {

                if (!selectedConversationId) {
                    return;
                }

                contactDrawer.classList.add('show');

                updateOverlay();

            }
        );

    }


    if (closeContactDrawerButton) {

        closeContactDrawerButton.addEventListener(
            'click',
            function () {

                closeContactDrawer();

            }
        );

    }


    function closeContactDrawer() {

        if (!contactDrawer) {
            return;
        }

        contactDrawer.classList.remove('show');

        updateOverlay();

    }


    /* =========================================================
       OVERLAY
    ========================================================= */

    if (overlay) {

        overlay.addEventListener(
            'click',
            function () {

                closeFilterSidebar();

                closeContactDrawer();

            }
        );

    }


    /* =========================================================
       MOBILE FILTER
    ========================================================= */

    if (openChatFiltersButton) {

        openChatFiltersButton.addEventListener(
            'click',
            function () {

                openFilterSidebar();

            }
        );

    }


    if (filterClose) {

        filterClose.addEventListener(
            'click',
            function () {

                closeFilterSidebar();

            }
        );

    }


    /* =========================================================
       SEND MESSAGE
    ========================================================= */

    if (sendMessageForm) {
        sendMessageForm.addEventListener(
            'submit',
            async function (event) {

                event.preventDefault();

                const content = messageInput.value.trim();

                if (!content) {
                    return;
                }

                if (!selectedConversationId) {
                    return;
                }

                if (
                    !adminChatConnection ||
                    adminChatConnection.state !==
                    signalR.HubConnectionState.Connected
                ) {
                    console.warn('Chat SignalR is not connected.');
                    return;
                }

                const conversationId =
                    Number(selectedConversationId);

                try {
                    const sendButton =
                        sendMessageForm.querySelector(
                            '.send-msg-btn'
                        );

                    if (sendButton) {
                        sendButton.disabled = true;
                    }

                    await adminChatConnection.invoke(
                        'SendAdminMessage',
                        conversationId,
                        content
                    );

                    messageInput.value = '';

                } catch (error) {

                    console.error(
                        'Send admin message failed:',
                        error
                    );

                } finally {

                    const sendButton =
                        sendMessageForm.querySelector(
                            '.send-msg-btn'
                        );

                    if (sendButton) {
                        sendButton.disabled = false;
                    }

                    messageInput.focus();
                }
            }
        );
    }


    /* =========================================================
       APPEND OUTGOING MESSAGE
    ========================================================= */

    function appendOutgoingMessage(content) {

        const li =
            document.createElement('li');

        li.className =
            'chat-message chat-message-right';

        li.innerHTML = `
            <div class="d-flex overflow-hidden">

                <div class="chat-message-wrapper">

                    <div class="chat-message-text">

                        <p class="mb-0 text-break">
                            ${escapeHtml(content)}
                        </p>

                    </div>

                    <div class="text-end text-muted mt-1">

                        <i class="ti ti-clock ti-16px me-1"></i>

                        <small>
                            Sending...
                        </small>

                    </div>

                </div>

                <div class="user-avatar flex-shrink-0 ms-3">

                    <div class="avatar avatar-sm">

                        <img src="/admin/assets/img/avatars/1.png"
                             alt="Avatar"
                             class="rounded-circle" />

                    </div>

                </div>

            </div>
        `;

        chatHistory.appendChild(li);

    }


    /* =========================================================
       HTML ESCAPE
    ========================================================= */

    function escapeHtml(value) {

        const div =
            document.createElement('div');

        div.textContent = value;

        return div.innerHTML;

    }


    /* =========================================================
       REFRESH
    ========================================================= */

    if (refreshConversations) {

        refreshConversations.addEventListener(
            'click',
            function () {

                /*
                 * TODO:
                 *
                 * HTMX:
                 *
                 * htmx.ajax(...)
                 *
                 * hoặc reload Conversation list.
                 */

                refreshConversations
                    .classList.add('ti-spin');

                setTimeout(() => {

                    refreshConversations
                        .classList.remove('ti-spin');

                }, 500);

            }
        );

    }


    /* =========================================================
       REALTIME
       ========================================================= */

    /*
     * SignalR sẽ gọi những hàm này.
     *
     * Ví dụ:
     *
     * connection.on(
     *     "MessageCreated",
     *     message => {
     *         handleMessageCreated(message);
     *     }
     * );
     *
     * connection.on(
     *     "ConversationUpdated",
     *     conversation => {
     *         handleConversationUpdated(conversation);
     *     }
     * );
     */


    //window.handleMessageCreated =
    //    function (message) {

    //        /*
    //         * message.ConversationId
    //         * message.Content
    //         * message.SenderId
    //         * message.CreatedAt
    //         */

    //        if (
    //            String(message.conversationId) !==
    //            String(selectedConversationId)
    //        ) {

    //            /*
    //             * Conversation khác:
    //             *
    //             * 1. Update last message
    //             * 2. Update unread
    //             * 3. Move conversation lên đầu
    //             */

    //            updateConversationPreview(
    //                message
    //            );

    //            return;
    //        }


    //        /*
    //         * Conversation đang mở:
    //         * append message trực tiếp.
    //         */

    //        appendRealtimeMessage(
    //            message
    //        );

    //        scrollChatToBottom();

    //    };


    //window.handleConversationUpdated =
    //    function (conversation) {

    //        /*
    //         * Update:
    //         *
    //         * - status
    //         * - last message
    //         * - unread
    //         * - timestamp
    //         * - assignment
    //         * - label
    //         */

    //        updateConversationItem(
    //            conversation
    //        );

    //        applyConversationFilters();

    //    };


    //function appendRealtimeMessage(message) {

    //    const isMine =
    //        message.senderType === 'User';

    //    const li =
    //        document.createElement('li');

    //    li.className =
    //        isMine
    //            ? 'chat-message chat-message-right'
    //            : 'chat-message';


    //    const avatar =
    //        isMine
    //            ? '/admin/assets/img/avatars/1.png'
    //            : '/admin/assets/img/avatars/4.png';


    //    li.innerHTML = `
    //        <div class="d-flex overflow-hidden">

    //            ${!isMine
    //            ? `
    //                    <div class="user-avatar flex-shrink-0 me-3">

    //                        <div class="avatar avatar-sm">

    //                            <img src="${avatar}"
    //                                 alt="Avatar"
    //                                 class="rounded-circle" />

    //                        </div>

    //                    </div>
    //                    `
    //            : ''
    //        }

    //            <div class="chat-message-wrapper">

    //                <div class="chat-message-text">

    //                    <p class="mb-0 text-break">
    //                        ${escapeHtml(message.content || '')}
    //                    </p>

    //                </div>

    //                <div class="${isMine ? 'text-end' : ''} text-muted mt-1">

    //                    ${isMine
    //            ? '<i class="ti ti-checks ti-16px text-success me-1"></i>'
    //            : ''
    //        }

    //                    <small>
    //                        ${message.time || ''}
    //                    </small>

    //                </div>

    //            </div>

    //            ${isMine
    //            ? `
    //                    <div class="user-avatar flex-shrink-0 ms-3">

    //                        <div class="avatar avatar-sm">

    //                            <img src="${avatar}"
    //                                 alt="Avatar"
    //                                 class="rounded-circle" />

    //                        </div>

    //                    </div>
    //                    `
    //            : ''
    //        }

    //        </div>
    //    `;

    //    chatHistory.appendChild(li);

    //}


    function updateConversationList(message) {

        const item = document.querySelector(
            `.conversation-item[data-conversation-id="${message.conversationId}"]`
        );

        if (!item) {
            return;
        }

        // Luôn cập nhật preview
        const preview = item.querySelector('.conversation-preview');

        if (preview) {
            preview.textContent = message.content || '';
        }

        // Luôn cập nhật thời gian
        const time = item.querySelector('.conversation-time');

        if (time && message.createdAt) {
            time.textContent =
                formatConversationTime(message.createdAt);
        }

        // Nếu đang mở conversation này
        // thì không cần unread
        if (
            String(message.conversationId) ===
            String(selectedConversationId)
        ) {
            return;
        }

        // Conversation không mở -> đưa lên đầu
        conversationList.prepend(item);
        ChatScroll.update(conversationList);
        // Tăng unread
        let unread = item.querySelector('.conversation-unread');

        if (!unread) {
            unread = document.createElement('span');
            unread.className = 'conversation-unread';

            item
                .querySelector('.conversation-bottom')
                ?.appendChild(unread);
        }

        const currentCount = parseInt(
            unread.textContent || '0',
            10
        );

        unread.textContent = currentCount + 1;
    }


    function updateConversationItem(conversation) {

        const item =
            document.querySelector(
                `.conversation-item[data-conversation-id="${conversation.id}"]`
            );

        if (!item) {
            return;
        }

        if (conversation.status) {
            item.dataset.status =
                conversation.status;
        }

        if (conversation.inbox) {
            item.dataset.inbox =
                conversation.inbox;
        }

        if (conversation.label) {
            item.dataset.label =
                conversation.label;
        }

    }


    /* =========================================================
       INITIAL
    ========================================================= */

    applyConversationFilters();

    scrollChatToBottom();
    initChatSignalR();
});