/**
 * Caterin Chat Workspace
 *
 * Refactored for:
 * - Clear state management
 * - Realtime SignalR
 * - Conversation/message separation
 * - Lazy-loading ready
 */

'use strict';

/* =========================================================
   PERFECT SCROLLBAR
========================================================= */

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

        const scrollbar = new PerfectScrollbar(
            element,
            {
                wheelPropagation: false,
                suppressScrollX: true
            }
        );

        this.instances.set(id, scrollbar);

        return scrollbar;
    },

    update(element) {

        if (!element) {
            return;
        }

        const scrollbar =
            this.instances.get(element.id);

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

        this.instances.delete(element.id);
    },

    initAll(container = document) {

        container
            .querySelectorAll(
                '[data-perfect-scrollbar]'
            )
            .forEach(element => {
                this.init(element);
            });
    }
};


/* =========================================================
   WORKSPACE
========================================================= */

document.addEventListener(
    'DOMContentLoaded',
    function () {

        const workspace =
            document.getElementById(
                'chatWorkspace'
            );

        if (!workspace) {
            return;
        }

        ChatScroll.initAll(workspace);


        /* =====================================================
           DOM
        ===================================================== */

        const DOM = {

            workspace,

            inboxId:
                workspace.dataset.inboxId,

            filterSidebar:
                document.getElementById(
                    'chatFilterSidebar'
                ),

            filterClose:
                document.getElementById(
                    'chatFilterClose'
                ),

            openChatFiltersButton:
                document.getElementById(
                    'openChatFilters'
                ),

            overlay:
                document.getElementById(
                    'chatWorkspaceOverlay'
                ),

            conversationPanel:
                document.getElementById(
                    'conversationPanel'
                ),

            chatDetailPanel:
                document.getElementById(
                    'chatDetailPanel'
                ),

            conversationList:
                document.getElementById(
                    'conversationList'
                ),

            conversationSearch:
                document.getElementById(
                    'conversationSearch'
                ),

            backButton:
                document.getElementById(
                    'backToConversations'
                ),

            chatContactButton:
                document.getElementById(
                    'chatContactButton'
                ),

            contactDrawer:
                document.getElementById(
                    'chatContactDrawer'
                ),

            closeContactDrawerButton:
                document.getElementById(
                    'closeContactDrawer'
                ),

            sendMessageForm:
                document.getElementById(
                    'sendMessageForm'
                ),

            messageInput:
                document.getElementById(
                    'messageInput'
                ),

            chatHistoryBody:
                document.getElementById(
                    'chatHistoryBody'
                ),

            chatHistory:
                document.getElementById(
                    'chatHistory'
                ),

            refreshConversations:
                document.getElementById(
                    'refreshConversations'
                ),

            chatContactName:
                document.getElementById(
                    'chatContactName'
                ),

            chatContactStatus:
                document.getElementById(
                    'chatContactStatus'
                ),

            conversationTotal:
                document.getElementById(
                    'conversationTotal'
                ),

            typing:
                document.getElementById(
                    'typing'
                )
        };


        /* =====================================================
           STATE
        ===================================================== */

        const state = {

            /*
             * Current conversation being displayed.
             *
             * Đây là ID duy nhất chúng ta sử dụng.
             */
            currentConversationId: null,

            /*
             * Conversation SignalR room.
             */
            joinedConversationId: null,

            /*
             * SignalR connection.
             */
            connection: null,

            /*
             * Prevent duplicate realtime messages.
             *
             * Sau này lazy loading vẫn có thể dùng.
             */
            processedMessageIds: new Set(),

            maxProcessedMessageIds: 500,

            /*
             * Current message request.
             *
             * Dùng AbortController để:
             *
             * A → B
             *
             * request A có thể bị cancel.
             */
            messageRequestController: null,

            /*
             * Typing.
             */
            typing: {

                isTyping: false,

                timeout: null,

                timeoutDuration: 1500
            },

            /*
             * Mark conversation read debounce.
             */
            markRead: {

                timer: null,

                delay: 300
            },

            /*
             * Message loading state.
             *
             * Chuẩn bị cho lazy loading.
             */
            messages: {

                loading: false,

                hasMore: true,

                oldestMessageId: null,

                newestMessageId: null
            }
        };
        const conversationState = {
            items: new Map(),

            loading: false,
            hasMore: true,

            oldestLastMessageAt: null,
            oldestId: null,

            filters: {
                inboxId: null,
                status: 'all',
                search: '',
                assignedUserId: null,
                labelId: null
            }
        };
        const conversationCountState = {
            all: 0,
            open: 0,
            pending: 0,
            resolved: 0,
            loading: false
        };
        let conversationSearchTimer = null;
        let conversationCountsRefreshQueued = false;
        /* =====================================================
           HELPERS
        ===================================================== */

        function normalizeId(value) {

            if (
                value === null ||
                value === undefined
            ) {
                return null;
            }

            return String(value);
        }


        function isSameId(a, b) {

            return (
                normalizeId(a) !== null &&
                normalizeId(a) === normalizeId(b)
            );
        }


        function isMobile() {

            return window.innerWidth < 992;
        }


        function escapeHtml(value) {

            const div =
                document.createElement('div');

            div.textContent =
                value ?? '';

            return div.innerHTML;
        }


        function formatMessageTime(value) {

            if (!value) {
                return '';
            }

            const date =
                new Date(value);

            if (
                Number.isNaN(
                    date.getTime()
                )
            ) {
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


        function formatConversationTime(value) {

            if (!value) {
                return '';
            }

            const date =
                new Date(value);

            if (
                Number.isNaN(
                    date.getTime()
                )
            ) {
                return '';
            }

            const now =
                new Date();

            const sameDay =
                date.getFullYear() ===
                now.getFullYear() &&
                date.getMonth() ===
                now.getMonth() &&
                date.getDate() ===
                now.getDate();

            if (sameDay) {

                return date.toLocaleTimeString(
                    'vi-VN',
                    {
                        hour: '2-digit',
                        minute: '2-digit'
                    }
                );
            }

            return date.toLocaleDateString(
                'vi-VN',
                {
                    day: '2-digit',
                    month: '2-digit'
                }
            );
        }


        /* =====================================================
           SCROLL
        ===================================================== */

        function scrollChatToBottom() {

            if (!DOM.chatHistoryBody) {
                return;
            }

            requestAnimationFrame(() => {

                DOM.chatHistoryBody.scrollTop =
                    DOM.chatHistoryBody.scrollHeight;

                ChatScroll.update(
                    DOM.chatHistoryBody
                );
            });
        }


        /* =====================================================
           MESSAGE DEDUPLICATION
        ===================================================== */

        function isMessageProcessed(messageId) {

            if (!messageId) {
                return true;
            }

            const id =
                String(messageId);

            if (
                state.processedMessageIds.has(id)
            ) {
                return true;
            }

            state.processedMessageIds.add(id);

            if (
                state.processedMessageIds.size >
                state.maxProcessedMessageIds
            ) {

                const firstId =
                    state.processedMessageIds
                        .values()
                        .next()
                        .value;

                if (firstId) {

                    state.processedMessageIds.delete(
                        firstId
                    );
                }
            }

            return false;
        }


        /* =====================================================
           UI - FILTER SIDEBAR
        ===================================================== */

        function openFilterSidebar() {

            if (!DOM.filterSidebar) {
                return;
            }

            DOM.filterSidebar.classList.add(
                'show'
            );

            DOM.overlay?.classList.add(
                'show'
            );
        }


        function closeFilterSidebar() {

            if (!DOM.filterSidebar) {
                return;
            }

            DOM.filterSidebar.classList.remove(
                'show'
            );

            updateOverlay();
        }


        function closeContactDrawer() {

            if (!DOM.contactDrawer) {
                return;
            }

            DOM.contactDrawer.classList.remove(
                'show'
            );

            updateOverlay();
        }


        function updateOverlay() {

            const filterOpen =
                DOM.filterSidebar?.classList.contains(
                    'show'
                );

            const contactOpen =
                DOM.contactDrawer?.classList.contains(
                    'show'
                );

            DOM.overlay?.classList.toggle(
                'show',
                Boolean(
                    filterOpen ||
                    contactOpen
                )
            );
        }


        /* =====================================================
           CONVERSATION STATE
        ===================================================== */

        function getCurrentConversationId() {

            return state.currentConversationId;
        }


        function isCurrentConversation(
            conversationId
        ) {

            return isSameId(
                conversationId,
                state.currentConversationId
            );
        }


        /* =====================================================
           CONVERSATION UNREAD
        ===================================================== */

        function getConversationUnreadCount(
            item
        ) {

            if (!item) {
                return 0;
            }

            const count =
                parseInt(
                    item.dataset.unreadCount ||
                    '0',
                    10
                );

            return Number.isNaN(count)
                ? 0
                : count;
        }


        function setConversationUnreadCount(
            item,
            count
        ) {

            if (!item) {
                return;
            }

            count = Math.max(
                0,
                Number(count) || 0
            );

            item.dataset.unreadCount =
                String(count);

            const bottom =
                item.querySelector(
                    '.conversation-bottom'
                );

            if (!bottom) {
                return;
            }

            let badge =
                bottom.querySelector(
                    '.conversation-unread'
                );

            if (count <= 0) {

                badge?.remove();

                item.classList.remove(
                    'conversation-unread-item'
                );

                return;
            }

            if (!badge) {

                badge =
                    document.createElement(
                        'span'
                    );

                badge.className =
                    'conversation-unread badge rounded-pill bg-primary';

                bottom.appendChild(
                    badge
                );
            }

            badge.textContent =
                count > 99
                    ? '99+'
                    : String(count);

            item.classList.add(
                'conversation-unread-item'
            );
        }


        /* =====================================================
           CONVERSATION HEADER
        ===================================================== */

        function updateChatHeader(
            conversationItem
        ) {

            if (!conversationItem) {
                return;
            }

            const name =
                conversationItem
                    .querySelector(
                        '.conversation-name'
                    )
                    ?.textContent
                    ?.trim();

            if (
                DOM.chatContactName &&
                name
            ) {

                DOM.chatContactName.textContent =
                    name;
            }

            const status =
                getConversationStatusKey(
                    conversationItem.dataset.status
                );

            if (DOM.chatContactStatus) {

                const statusText = {

                    open: 'Open',

                    pending: 'Pending',

                    resolved: 'Resolved'
                };

                DOM.chatContactStatus.textContent =
                    statusText[status] ||
                    status;
            }
        }


        /* =====================================================
           MESSAGE RENDER
        ===================================================== */

        function createMessageElement(
            message
        ) {

            if (!message) {
                return null;
            }

            const li =
                document.createElement('li');

            const isContact =
                Number(message.senderType) === 1;

            const isAdmin =
                Number(message.senderType) === 2;

            const isSystem =
                Number(message.senderType) === 3;

            const isBot =
                Number(message.senderType) === 4;

            if (isSystem) {

                li.className =
                    'chat-message text-center';

                li.innerHTML = `
                    <div class="text-muted">
                        <small>
                            ${escapeHtml(
                    message.content || ''
                )}
                        </small>
                    </div>
                `;

                return li;
            }

            if (
                !isAdmin &&
                !isContact &&
                !isBot
            ) {

                console.warn(
                    'Unknown sender type:',
                    message.senderType
                );

                return null;
            }

            li.className =
                isAdmin
                    ? 'chat-message chat-message-right'
                    : 'chat-message';


            const senderName =
                message.senderName?.trim() ||
                'Guest';


            const guestAvatar = `
                <div class="flex-shrink-0 avatar">
                    <span class="avatar-initial rounded-circle bg-label-secondary">
                        <i class="ti ti-user"></i>
                    </span>
                </div>
            `;


            const contactAvatar =
                guestAvatar;


            li.innerHTML = `
                <div class="chat-message-inner">

                    ${isContact
                    ? contactAvatar
                    : ''
                }

                    <div class="chat-message-wrapper">

                        ${isContact
                    ? `
                                    <div class="text-muted mb-1">
                                        <small>${escapeHtml(senderName)}</small>
                                    </div>
                                `
                    : ''
                }

                        <div class="chat-message-text">
                            <p class="mb-0">${escapeHtml(message.content || '')}</p>
                        </div>

                        <div class="${isAdmin ? 'text-end' : '' } text-muted mt-1">
                            <small>${formatMessageTime(message.createdAt)}</small>
                        </div>
                    </div>

                </div>
            `;

            return li;
        }


        function appendChatMessage(
            message
        ) {

            if (
                !message ||
                !DOM.chatHistory
            ) {
                return false;
            }

            const element =
                createMessageElement(
                    message
                );

            if (!element) {
                return false;
            }

            DOM.chatHistory.appendChild(
                element
            );

            ChatScroll.update(
                DOM.chatHistoryBody
            );

            return true;
        }


        function renderMessages(
            messages
        ) {

            if (!DOM.chatHistory) {
                return;
            }

            DOM.chatHistory.innerHTML = '';

            if (
                !Array.isArray(messages) ||
                messages.length === 0
            ) {

                renderEmptyMessages();

                return;
            }

            const fragment =
                document.createDocumentFragment();

            messages.forEach(
                message => {

                    if (!message?.id) {
                        return;
                    }

                    isMessageProcessed(
                        message.id
                    );

                    const element =
                        createMessageElement(
                            message
                        );

                    if (element) {
                        fragment.appendChild(
                            element
                        );
                    }
                }
            );

            DOM.chatHistory.appendChild(
                fragment
            );

            ChatScroll.update(
                DOM.chatHistoryBody
            );
        }

        function prependMessages(
            messages
        ) {

            if (
                !DOM.chatHistory ||
                !Array.isArray(messages) ||
                messages.length === 0
            ) {
                return;
            }

            const fragment =
                document.createDocumentFragment();

            messages.forEach(
                message => {

                    if (!message?.id) {
                        return;
                    }

                    /*
                     * Message đã tồn tại thì bỏ qua.
                     */
                    if (
                        state.processedMessageIds.has(
                            String(message.id)
                        )
                    ) {
                        return;
                    }

                    isMessageProcessed(
                        message.id
                    );

                    const element =
                        createMessageElement(
                            message
                        );

                    if (element) {
                        fragment.appendChild(
                            element
                        );
                    }
                }
            );

            if (!fragment.childNodes.length) {
                return;
            }

            /*
             * Chèn toàn bộ batch vào đầu.
             */
            DOM.chatHistory.prepend(
                fragment
            );
        }
        function renderLoadingMessages() {

            if (!DOM.chatHistory) {
                return;
            }

            DOM.chatHistory.innerHTML = `
                <li class="chat-loading">
                    <span>
                        Đang tải tin nhắn...
                    </span>
                </li>
            `;

            ChatScroll.update(
                DOM.chatHistoryBody
            );
        }


        function renderEmptyMessages() {

            if (!DOM.chatHistory) {
                return;
            }

            DOM.chatHistory.innerHTML = `
                <li class="chat-empty">
                    <span>
                        Chưa có tin nhắn.
                    </span>
                </li>
            `;

            ChatScroll.update(
                DOM.chatHistoryBody
            );
        }


        function renderMessageError(
            conversationId
        ) {

            if (!DOM.chatHistory) {
                return;
            }

            DOM.chatHistory.innerHTML = `
                <li class="chat-error">
                    <span>
                        Không thể tải tin nhắn.
                    </span>

                    <button
                        type="button"
                        class="chat-retry"
                        data-conversation-id="${escapeHtml(
                conversationId
            )}">
                        Thử lại
                    </button>
                </li>
            `;

            ChatScroll.update(
                DOM.chatHistoryBody
            );
        }


        /* =====================================================
           MESSAGE API
        ===================================================== */

        async function fetchConversationMessages(
            conversationId,
            options = {}
        ) {

            const {
                limit = 30,
                before = null,
                signal
            } = options;

            const params =
                new URLSearchParams();

            params.set(
                'limit',
                String(limit)
            );

            if (before !== null) {

                params.set(
                    'before',
                    String(before)
                );
            }

            const response =
                await fetch(
                    `/admin/chat/${conversationId}/messages?${params.toString()}`,
                    {
                        method: 'GET',

                        headers: {
                            'Accept':
                                'application/json'
                        },

                        credentials:
                            'same-origin',

                        signal
                    }
                );

            if (!response.ok) {

                throw new Error(
                    `Load messages failed: ${response.status}`
                );
            }
            console.log("LOAD MESSAGE");
            return await response.json();
        }

        function buildConversationQueryParams({
            limit = 10,
            beforeLastMessageAt = null,
            beforeId = null
        } = {}) {
            const params = new URLSearchParams();

            if (conversationState.filters.inboxId) {
                params.set(
                    'inboxId',
                    conversationState.filters.inboxId
                );
            }
             if (conversationState.filters.status !== 'all') {
            params.set(
                'status',
                conversationState.filters.status
            );
        }

        if (conversationState.filters.search) {
            params.set(
                'search',
                conversationState.filters.search
            );
        }
            
        if (conversationState.filters.assignedUserId) {
            params.set(
                'assignedUserId',
                conversationState.filters.assignedUserId
            );
        }

        if (conversationState.filters.labelId) {
            params.set(
                'labelId',
                conversationState.filters.labelId
            );
        }
            params.set('limit', String(limit));


            if (beforeLastMessageAt) {
                params.set(
                    'beforeLastMessageAt',
                    beforeLastMessageAt
                );
            }

            if (beforeId !== null) {
                params.set(
                    'beforeId',
                    beforeId
                );
            }

            return params;
        }
        /* =====================================================
           LOAD CONVERSATION LIST
        ===================================================== */
        async function loadInitialConversations() {
            if (conversationState.loading) {
                return;
            }

            conversationState.loading = true;
            const params = buildConversationQueryParams({
                limit: 10
            });
            console.log(
                '[CONVERSATION FILTERS]',
                conversationState.filters
            );

            console.log(
                '[CONVERSATION URL]',
                `/admin/chat/conversations?${params.toString()}`
            );
            try {
                const response = await fetch(
                    `/admin/chat/conversations?${params.toString()}`,
                    {
                        method: 'GET',
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    }
                );
                console.log("LOAD CONVERSATION");
                if (!response.ok) {
                    throw new Error('Không thể tải danh sách hội thoại.');
                }

                const result = await response.json();

                console.log(result);
                conversationState.items.clear();

                for (const item of result.items) {
                    conversationState.items.set(
                        normalizeId(item.id),
                        item
                    );
                }

                conversationState.hasMore = result.hasMore;
                conversationState.oldestLastMessageAt =
                    result.oldestLastMessageAt;

                conversationState.oldestId =
                    result.oldestId;

                renderConversationList();

            } catch (error) {
                console.error(
                    'loadInitialConversations:',
                    error
                );
            } finally {
                conversationState.loading = false;
            }
        }
        async function loadMoreConversations() {
            if (
                conversationState.loading ||
                !conversationState.hasMore
            ) {
                return;
            }

            if (
                !conversationState.oldestLastMessageAt ||
                !conversationState.oldestId
            ) {
                return;
            }

            conversationState.loading = true;

            try {
                const params =
                    buildConversationQueryParams({
                        limit: 30,
                        beforeLastMessageAt:
                            conversationState.oldestLastMessageAt,
                        beforeId:
                            conversationState.oldestId
                    });
                console.log(params);
                const response = await fetch(
                    `/admin/chat/conversations?${params}`,
                    {
                        method: 'GET',
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    }
                );

                if (!response.ok) {
                    throw new Error(
                        'Không thể tải thêm hội thoại.'
                    );
                }

                const result = await response.json();
                console.log("LOAD MORE CONVERSATION");
                console.log(result);
                for (const item of result.items) {
                    conversationState.items.set(
                        normalizeId(item.id),
                        item
                    );
                }

                conversationState.hasMore = result.hasMore;

                conversationState.oldestLastMessageAt =
                    result.oldestLastMessageAt;

                conversationState.oldestId =
                    result.oldestId;

                renderConversationList();

            } catch (error) {
                console.error(
                    'loadMoreConversations:',
                    error
                );
            } finally {
                conversationState.loading = false;
            }
        }
        function renderConversationList() {

            if (!DOM.conversationList) {
                return;
            }

            const conversations =
                getFilteredConversations();

            DOM.conversationList.innerHTML = '';

            if (conversations.length === 0) {

                renderConversationEmptyState();

            }
            else {

                for (const conversation of conversations) {

                    const element =
                        createConversationElement(
                            conversation
                        );

                    DOM.conversationList.appendChild(
                        element
                    );
                }
            }

            updateConversationTotal(
                conversations.length
            );

            ChatScroll.update(
                DOM.conversationList
            );
        }
        function renderConversationEmptyState() {
            const container =
                document.getElementById('conversationList');

            if (!container) {
                return;
            }

            container.innerHTML = `
        <div class="flex flex-col items-center justify-center py-12 text-center">
            <div class="text-sm font-medium text-gray-500">
                Không có cuộc trò chuyện
            </div>

            <div class="mt-1 text-xs text-gray-400">
                Không tìm thấy cuộc trò chuyện phù hợp.
            </div>
        </div>
    `;
        }
        function getFilteredConversations() {

            let items = Array.from(
                conversationState.items.values()
            );

            items.sort((a, b) => {

                const dateCompare =
                    new Date(b.lastMessageAt) -
                    new Date(a.lastMessageAt);

                if (dateCompare !== 0) {
                    return dateCompare;
                }

                return Number(b.id) - Number(a.id);
            });

            return items.filter(
                matchesConversation
            );
        }
        function matchesConversationFilters(
            conversation
        ) {
            const name =
                conversation.contactName
                    ?.toLowerCase() || '';

            const preview =
                (
                    conversation.lastMessage ||
                    conversation.subject ||
                    ''
                ).toLowerCase();

            const search =
                conversationState.filters.search
                    ?.trim()
                    .toLowerCase() || '';

            if (search) {

                const matched =
                    name.includes(search) ||
                    preview.includes(search);

                if (!matched) {
                    return false;
                }
            }

            if (
                conversationState.filters.assignedUserId
            ) {

                if (
                    normalizeId(
                        conversation.assignedUserId
                    ) !==
                    normalizeId(
                        conversationState.filters.assignedUserId
                    )
                ) {
                    return false;
                }
            }

            if (
                conversationState.filters.labelId
            ) {

                const hasLabel =
                    conversation.labels?.some(
                        label =>
                            normalizeId(label.id) ===
                            normalizeId(
                                conversationState.filters.labelId
                            )
                    );

                if (!hasLabel) {
                    return false;
                }
            }

            return true;
        }
        function matchesConversation(conversation) {

            const inboxFilter =
                conversationState.filters.inboxId;

            if (
                inboxFilter &&
                Number(conversation.inboxId) !== Number(inboxFilter)
            ) {
                return false;
            }

            const statusFilter =
                conversationState.filters.status;

            if (
                statusFilter &&
                statusFilter !== 'all' &&
                getConversationStatusKey(conversation.status) !== statusFilter
            ) {
                return false;
            }

            return matchesConversationFilters(conversation);
        }
        function createConversationElement(conversation) {
            const article = document.createElement('article');

            article.className = 'conversation-item';

            article.dataset.conversationId =
                conversation.id;

            article.dataset.status =
                conversation.status;

            article.dataset.inbox =
                conversation.inboxId;

            article.dataset.assignedUserId =
                conversation.assignedUserId ?? '';

            article.dataset.unreadCount =
                conversation.unreadCount ?? 0;

            const firstLetter =
                conversation.contactName
                    ?.trim()
                    ?.charAt(0)
                    ?.toUpperCase() || '?';

            const avatar = conversation.contactAvatarUrl
                ? `
            <img src="${escapeHtml(conversation.contactAvatarUrl)}"
                 alt="${escapeHtml(conversation.contactName ?? '')}"
                 class="rounded-circle" />
          `
                : `
            <span class="avatar-initial rounded-circle bg-label-primary">${escapeHtml(firstLetter)}</span>
          `;

            const preview =
                conversation.lastMessage?.trim()
                    ? conversation.lastMessage
                    : conversation.subject ?? '';

            article.innerHTML = `
        <div class="conversation-avatar">
            <div class="avatar avatar-sm avatar-online">
                ${avatar}
            </div>
        </div>

        <div class="conversation-content">
            <div class="conversation-top">
                <h6 class="conversation-name">${escapeHtml(conversation.contactName ?? '')}</h6>

                <time class="conversation-time">${formatConversationTime(conversation.lastMessageAt)}
                </time>
            </div>

            <div class="conversation-bottom">

                <p class="conversation-message">${escapeHtml(preview)}</p>

                ${conversation.unreadCount > 0
                    ? `
                            <span class="badge rounded-pill bg-primary">${conversation.unreadCount}</span>
                          `
                    : ''
                }

            </div>

            ${conversation.labels?.length
                    ? `
                        <div class="conversation-meta">
                            ${conversation.labels
                        .map(label => `
                                    <span class="chat-label badge bg-label-success">${escapeHtml(label.name)}</span>
                                `)
                        .join('')}

                            <span class="conversation-status">${getConversationStatusText(conversation.status)}
                            </span>
                        </div>
                      `
                    : ''
                }

        </div>
    `;

            return article;
        }
        function getConversationStatusKey(status) {
            if (status === null || status === undefined) {
                return '';
            }

            // Backend có thể trả enum dạng string:
            // "Open", "Pending", "Resolved", "Closed"
            if (typeof status === 'string') {
                const value = status.trim().toLowerCase();

                switch (value) {
                    case 'open':
                        return 'open';

                    case 'pending':
                        return 'pending';

                    case 'resolved':
                        return 'resolved';

                    case 'closed':
                        return 'closed';

                    default:
                        return '';
                }
            }

            // Backend/client có thể vẫn dùng enum number
            switch (Number(status)) {
                case 1:
                    return 'open';

                case 2:
                    return 'pending';

                case 3:
                    return 'resolved';

                case 4:
                    return 'closed';

                default:
                    return '';
            }
        }
            function getConversationStatusText(status) {
                switch (Number(status)) {
                    case 1:
                        return 'Đang mở';

                    case 2:
                        return 'Đang chờ';

                    case 3:
                        return 'Đã xử lý';

                    case 4:
                        return 'Đã đóng';

                    default:
                        return '';
                }
            }
        function setupConversationLazyLoading() {
            DOM.conversationList.addEventListener(
                'scroll',
                () => {
                    const element =
                        DOM.conversationList;

                    const distanceFromBottom =
                        element.scrollHeight -
                        element.scrollTop -
                        element.clientHeight;

                    if (distanceFromBottom < 300) {
                        loadMoreConversations();
                    }
                }
            );
        }

        function setupMessageLazyLoading() {

            if (!DOM.chatHistoryBody) {
                return;
            }

            DOM.chatHistoryBody.addEventListener(
                'scroll',
                function () {

                    /*
                     * Gần đầu conversation.
                     */
                    if (
                        this.scrollTop <= 100
                    ) {

                        loadOlderMessages();
                    }
                }
            );
        }
        /* =====================================================
           LOAD CONVERSATION MESSAGES
        ===================================================== */

        async function loadConversationMessages(
            conversationId
        ) {

            if (
                !conversationId ||
                !DOM.chatHistory
            ) {
                return;
            }

            /*
             * Cancel previous request.
             */
            if (
                state.messageRequestController
            ) {

                state.messageRequestController.abort();
            }

            const controller =
                new AbortController();

            state.messageRequestController =
                controller;

            state.messages.loading =
                true;

            renderLoadingMessages();

            try {

                const result =
                    await fetchConversationMessages(
                        conversationId,
                        {
                            limit: 30,
                            signal:
                                controller.signal
                        }
                    );

                /*
                 * User changed conversation
                 * while request was running.
                 */
                if (
                    !isCurrentConversation(
                        conversationId
                    )
                ) {
                    return;
                }

                const messages =
                    Array.isArray(result?.items)
                        ? result.items
                        : [];

                /*
                 * Initial render.
                 */
                renderMessages(
                    messages
                );

                /*
                 * Save lazy-loading cursor.
                 */
                state.messages.hasMore =
                    result?.hasMore === true;

                state.messages.oldestMessageId =
                    result?.oldestMessageId ?? null;

                state.messages.newestMessageId =
                    result?.newestMessageId ?? null;

                /*
                 * Initial load always scrolls
                 * to the newest message.
                 */
                scrollChatToBottom();

            }
            catch (error) {

                if (
                    error.name ===
                    'AbortError'
                ) {
                    return;
                }

                console.error(
                    'Load conversation failed:',
                    error
                );

                if (
                    !isCurrentConversation(
                        conversationId
                    )
                ) {
                    return;
                }

                renderMessageError(
                    conversationId
                );
            }
            finally {

                if (
                    state.messageRequestController ===
                    controller
                ) {

                    state.messageRequestController =
                        null;
                }

                state.messages.loading =
                    false;
            }
        }
        async function loadOlderMessages() {

            const conversationId =
                state.currentConversationId;

            if (
                !conversationId ||
                !DOM.chatHistory ||
                state.messages.loading ||
                !state.messages.hasMore ||
                !state.messages.oldestMessageId
            ) {
                return;
            }

            state.messages.loading =
                true;

            const oldestMessageId =
                state.messages.oldestMessageId;

            /*
             * Lưu vị trí scroll hiện tại.
             */
            const previousScrollHeight =
                DOM.chatHistoryBody.scrollHeight;

            const previousScrollTop =
                DOM.chatHistoryBody.scrollTop;

            try {

                const result =
                    await fetchConversationMessages(
                        conversationId,
                        {
                            limit: 30,
                            before:
                                oldestMessageId
                        }
                    );

                /*
                 * Conversation đã bị đổi
                 * trong lúc request.
                 */
                if (
                    !isCurrentConversation(
                        conversationId
                    )
                ) {
                    return;
                }

                const messages =
                    Array.isArray(result?.items)
                        ? result.items
                        : [];

                if (messages.length > 0) {

                    prependMessages(
                        messages
                    );
                }

                /*
                 * Update cursor.
                 */
                state.messages.hasMore =
                    result?.hasMore === true;

                state.messages.oldestMessageId =
                    result?.oldestMessageId ??
                    state.messages.oldestMessageId;

                /*
                 * Giữ nguyên vị trí user đang đọc.
                 */
                requestAnimationFrame(
                    () => {

                        const newScrollHeight =
                            DOM.chatHistoryBody.scrollHeight;

                        DOM.chatHistoryBody.scrollTop =
                            previousScrollTop +
                            (
                                newScrollHeight -
                                previousScrollHeight
                            );

                        ChatScroll.update(
                            DOM.chatHistoryBody
                        );
                    }
                );

            }
            catch (error) {

                console.error(
                    'Load older messages failed:',
                    error
                );
            }
            finally {

                state.messages.loading =
                    false;
            }
        }

        async function loadConversationCounts() {

            /*
             * Nếu đang có request:
             * không bỏ qua request mới.
             *
             * Đánh dấu cần refresh lại sau khi
             * request hiện tại hoàn tất.
             */
            if (conversationCountState.loading) {

                conversationCountsRefreshQueued = true;

                return;
            }

            conversationCountState.loading = true;

            try {

                const params =
                    new URLSearchParams();

                if (conversationState.filters.inboxId) {
                    params.set(
                        'inboxId',
                        conversationState.filters.inboxId
                    );
                }

                const search =
                    conversationState.filters.search?.trim();

                if (search) {

                    params.set(
                        'search',
                        search
                    );
                }

                if (
                    conversationState.filters.assignedUserId
                ) {

                    params.set(
                        'assignedUserId',
                        conversationState.filters.assignedUserId
                    );
                }

                if (
                    conversationState.filters.labelId
                ) {

                    params.set(
                        'labelId',
                        conversationState.filters.labelId
                    );
                }

                const response =
                    await fetch(
                        `/admin/chat/conversations/counts?${params.toString()}`,
                        {
                            method: 'GET',

                            headers: {
                                'X-Requested-With':
                                    'XMLHttpRequest'
                            }
                        }
                    );

                if (!response.ok) {

                    throw new Error(
                        `Failed to load conversation counts: ${response.status}`
                    );
                }

                const result =
                    await response.json();

                console.log(
                    '[COUNTS FROM DB]',
                    result
                );

                conversationCountState.all =
                    Number(result.all) || 0;

                conversationCountState.open =
                    Number(result.open) || 0;

                conversationCountState.pending =
                    Number(result.pending) || 0;

                conversationCountState.resolved =
                    Number(result.resolved) || 0;

                updateConversationFilterCounts();

            }
            catch (error) {

                console.error(
                    'Load conversation counts failed:',
                    error
                );

            }
            finally {

                conversationCountState.loading = false;

                /*
                 * Trong lúc request đang chạy có event
                 * hoặc filter mới yêu cầu refresh.
                 */
                if (conversationCountsRefreshQueued) {

                    conversationCountsRefreshQueued = false;

                    /*
                     * Không await ở đây để tránh
                     * chain recursion không cần thiết.
                     */
                    loadConversationCounts();
                }
            }
        }
        function updateConversationFilterCounts() {
            const counts = conversationCountState;

            const countElements = {
                all: document.querySelector(
                    '.chat-filter-list li[data-filter-value="all"] .chat-filter-count'
                ),

                open: document.querySelector(
                    '.chat-filter-list li[data-filter-value="open"] .chat-filter-count'
                ),

                pending: document.querySelector(
                    '.chat-filter-list li[data-filter-value="pending"] .chat-filter-count'
                ),

                resolved: document.querySelector(
                    '.chat-filter-list li[data-filter-value="resolved"] .chat-filter-count'
                )
            };

            if (countElements.all) {
                countElements.all.textContent = counts.all;
            }

            if (countElements.open) {
                countElements.open.textContent = counts.open;
            }

            if (countElements.pending) {
                countElements.pending.textContent = counts.pending;
            }

            if (countElements.resolved) {
                countElements.resolved.textContent = counts.resolved;
            }
        }
        /* =====================================================
           CONVERSATION OPEN
        ===================================================== */

        async function openConversation(
            conversationItem
        ) {

            if (!conversationItem) {
                return;
            }

            const conversationId =
                conversationItem.dataset
                    .conversationId;

            if (!conversationId) {
                return;
            }

            stopTyping();
            hideTyping();

            /*
             * Set current conversation
             * BEFORE loading messages.
             */
            state.currentConversationId =
                conversationId;

            /*
             * Reset message state.
             *
             * Không reset processedMessageIds
             * toàn bộ vì realtime deduplication
             * vẫn cần hoạt động.
             */
            resetMessageState();

            /*
             * Active item.
             */
            document
                .querySelectorAll(
                    '.conversation-item'
                )
                .forEach(
                    item => {
                        item.classList.remove(
                            'active'
                        );
                    }
                );

            conversationItem.classList.add(
                'active'
            );

            /*
             * Switch panels.
             */
            DOM.conversationPanel
                ?.classList.add(
                    'd-none'
                );

            DOM.chatDetailPanel
                ?.classList.remove(
                    'd-none'
                );

            closeContactDrawer();

            if (isMobile()) {
                closeFilterSidebar();
            }

            /*
             * Header.
             */
            updateChatHeader(
                conversationItem
            );

            /*
             * SignalR room.
             */
            await joinConversation(
                conversationId
            );

            /*
             * Load messages.
             */
            await loadConversationMessages(
                conversationId
            );

            /*
             * Mark read.
             */
            scheduleMarkConversationAsRead(
                conversationId
            );

            renderConversationList();

            ChatScroll.update(
                DOM.conversationList
            );
        }


        function resetMessageState() {

            state.messages.loading =
                false;

            state.messages.hasMore =
                true;

            state.messages.oldestMessageId =
                null;

            state.messages.newestMessageId =
                null;
        }


        /* =====================================================
           CONVERSATION CLOSE
        ===================================================== */

        async function closeConversation() {

            stopTyping();
            hideTyping();

            /*
             * Cancel message request.
             */
            if (
                state.messageRequestController
            ) {

                state.messageRequestController.abort();

                state.messageRequestController =
                    null;
            }

            await leaveCurrentConversation();

            state.currentConversationId =
                null;

            resetMessageState();

            closeContactDrawer();

            DOM.chatDetailPanel
                ?.classList.add(
                    'd-none'
                );

            DOM.conversationPanel
                ?.classList.remove(
                    'd-none'
                );

            renderConversationList();

            ChatScroll.update(
                DOM.conversationList
            );
        }


        async function leaveCurrentConversation() {

            if (
                !state.joinedConversationId ||
                !state.connection ||
                typeof signalR === 'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                state.joinedConversationId =
                    null;

                return;
            }

            try {

                await state.connection.invoke(
                    'LeaveAdminConversation',
                    Number(
                        state.joinedConversationId
                    )
                );

            }
            catch (error) {

                console.warn(
                    'LeaveAdminConversation failed:',
                    error
                );
            }

            state.joinedConversationId =
                null;
        }


        /* =====================================================
           SIGNALR
        ===================================================== */

        async function initChatSignalR() {

            if (
                typeof signalR ===
                'undefined'
            ) {

                console.error(
                    'SignalR is not loaded.'
                );

                return;
            }

            state.connection =
                new signalR.HubConnectionBuilder()
                    .withUrl(
                        '/hubs/chat?clientType=admin'
                    )
                    .withAutomaticReconnect()
                    .build();


            registerSignalREvents();


            try {

                await state.connection.start();

                console.log(
                    'Chat SignalR connected.'
                );

                await joinInbox();

            }
            catch (error) {

                console.error(
                    'Chat SignalR connection failed:',
                    error
                );
            }
        }


        function registerSignalREvents() {

            registerMessageReceived();

            registerTypingReceived();

            registerPresenceUpdated();

            registerConversationStatusUpdated();

            registerConnectionEvents();
        }


        /* =====================================================
           SIGNALR - MESSAGE RECEIVED
        ===================================================== */

        function registerMessageReceived() {

            state.connection.on(
                'chat.message.received',
                handleMessageReceived
            );
        }


        async function handleMessageReceived(
            message
        ) {

            if (!message?.id) {
                return;
            }

            if (
                isMessageProcessed(
                    message.id
                )
            ) {
                return;
            }

            const conversationId =
                message.conversationId;

            if (!conversationId) {
                return;
            }

            const current =
                isCurrentConversation(
                    conversationId
                );


            /*
             * Current conversation.
             */
            if (current) {

                appendChatMessage(
                    message
                );

                scrollChatToBottom();

                await acknowledgeMessageDelivered(
                    message
                );

                scheduleMarkConversationAsRead(
                    conversationId
                );
            }


            /*
             * Conversation list.
             */
            updateConversationList(
                message
            );
        }


        async function acknowledgeMessageDelivered(
            message
        ) {

            if (
                !state.connection ||
                typeof signalR ===
                'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            try {

                await state.connection.invoke(
                    'AdminMessageDelivered',
                    Number(message.id)
                );

            }
            catch (error) {

                console.error(
                    'Failed to acknowledge message delivered:',
                    error
                );
            }
        }


        /* =====================================================
           SIGNALR - CONNECTION
        ===================================================== */

        function registerConnectionEvents() {

            state.connection.onreconnecting(
                function (error) {

                    console.warn(
                        'Chat SignalR reconnecting...',
                        error
                    );

                    stopTyping();
                    hideTyping();
                }
            );


            state.connection.onreconnected(
                async function (connectionId) {

                    console.log(
                        'Chat SignalR reconnected:',
                        connectionId
                    );

                    await joinInbox();

                    if (
                        state.currentConversationId
                    ) {

                        /*
                         * Reset joined ID so
                         * joinConversation will
                         * execute again.
                         */
                        state.joinedConversationId =
                            null;

                        await joinConversation(
                            state.currentConversationId
                        );
                    }
                }
            );


            state.connection.onclose(
                function (error) {

                    console.warn(
                        'Chat SignalR connection closed.',
                        error
                    );
                }
            );
        }


        /* =====================================================
           SIGNALR - INBOX
        ===================================================== */

        async function joinInbox() {

            if (
                !state.connection ||
                typeof signalR ===
                'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            const inboxId =
                DOM.workspace.dataset.inboxId;

            if (!inboxId) {

                console.warn(
                    'InboxId is missing.'
                );

                return;
            }

            try {

                await state.connection.invoke(
                    'JoinInbox',
                    Number(inboxId)
                );

                console.log(
                    'Joined inbox:',
                    inboxId
                );
            }
            catch (error) {

                console.error(
                    'JoinInbox failed:',
                    error
                );
            }
        }


        /* =====================================================
           SIGNALR - CONVERSATION
        ===================================================== */

        async function joinConversation(
            conversationId
        ) {

            if (
                !conversationId ||
                !state.connection ||
                typeof signalR ===
                'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }


            if (
                isSameId(
                    state.joinedConversationId,
                    conversationId
                )
            ) {
                return;
            }


            /*
             * Leave previous conversation.
             */
            if (
                state.joinedConversationId
            ) {

                try {

                    await state.connection.invoke(
                        'LeaveAdminConversation',
                        Number(
                            state.joinedConversationId
                        )
                    );

                }
                catch (error) {

                    console.warn(
                        'LeaveAdminConversation failed:',
                        error
                    );
                }
            }


            try {

                await state.connection.invoke('JoinAdminConversation',  Number(conversationId));

                state.joinedConversationId = conversationId;

                await state.connection.invoke('OpenConversation', Number(conversationId));

                console.log(
                    'Joined conversation:',
                    conversationId
                );
            }
            catch (error) {

                console.error(
                    'JoinAdminConversation failed:',
                    error
                );

                state.joinedConversationId =
                    null;
            }
        }


        /* =====================================================
           SIGNALR - TYPING
        ===================================================== */

        function registerTypingReceived() {

            state.connection.on(
                'chat.typing',
                function (data) {

                    if (
                        !data ||
                        !isCurrentConversation(
                            data.conversationId
                        )
                    ) {
                        return;
                    }

                    /*
                     * Contact = 1
                     * Bot = 4
                     */
                    if (
                        Number(
                            data.senderType
                        ) !== 1 &&
                        Number(
                            data.senderType
                        ) !== 4
                    ) {
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
        }


        function showTyping() {

            if (!DOM.typing) {
                return;
            }

            DOM.typing.hidden =
                false;
        }


        function hideTyping() {

            if (!DOM.typing) {
                return;
            }

            DOM.typing.hidden =
                true;
        }


        async function sendTypingStatus(
            isTypingNow
        ) {

            if (
                !state.currentConversationId ||
                !state.connection ||
                typeof signalR ===
                'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            try {

                await state.connection.invoke(
                    'SendAdminTyping',
                    Number(
                        state.currentConversationId
                    ),
                    isTypingNow
                );

            }
            catch (error) {

                console.error(
                    'SEND ADMIN TYPING ERROR:',
                    error
                );
            }
        }


        function handleAdminTyping() {

            if (!DOM.messageInput) {
                return;
            }

            if (
                !DOM.messageInput.value.trim()
            ) {

                stopTyping();

                return;
            }

            if (
                !state.typing.isTyping
            ) {

                state.typing.isTyping =
                    true;

                sendTypingStatus(true);
            }

            clearTimeout(
                state.typing.timeout
            );

            state.typing.timeout =
                setTimeout(
                    stopTyping,
                    state.typing.timeoutDuration
                );
        }


        function stopTyping() {

            clearTimeout(
                state.typing.timeout
            );

            state.typing.timeout =
                null;

            if (
                !state.typing.isTyping
            ) {
                return;
            }

            state.typing.isTyping =
                false;

            sendTypingStatus(false);
        }


        /* =====================================================
           CONVERSATION READ
        ===================================================== */

        function scheduleMarkConversationAsRead(
            conversationId
        ) {

            if (!conversationId) {
                return;
            }

            clearTimeout(
                state.markRead.timer
            );

            state.markRead.timer =
                setTimeout(
                    async function () {

                        await markConversationAsRead(
                            conversationId
                        );

                    },
                    state.markRead.delay
                );
        }


        async function markConversationAsRead(
            conversationId
        ) {

            if (
                !conversationId ||
                !state.connection ||
                typeof signalR ===
                'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return false;
            }

            try {

                await state.connection.invoke(
                    'MarkConversationAsRead',
                    Number(conversationId)
                );


                const conversation =
                    conversationState.items.get(
                        normalizeId(conversationId)
                    );

                if (conversation) {
                    conversation.unreadCount = 0;
                }

                renderConversationList();

                return true;

            }
            catch (error) {

                console.error(
                    'MARK CONVERSATION READ ERROR:',
                    error
                );

                return false;
            }
        }


        /* =====================================================
           CONVERSATION DOM
        ===================================================== */

        function findConversationItem(
            conversationId
        ) {

            if (!DOM.conversationList) {
                return null;
            }

            return DOM.conversationList.querySelector(
                `.conversation-item[data-conversation-id="${CSS.escape(
                    String(conversationId)
                )}"]`
            );
        }


        function updateConversationList(message) {

            console.log('[MESSAGE RECEIVED]', message);

            if (!message?.conversationId) {
                return;
            }

            const conversationId =
                normalizeId(message.conversationId);

            const conversation =
                conversationState.items.get(
                    conversationId
                );

            if (!conversation) {
                return;
            }

            const isContact =
                Number(message.senderType) === 1;

            const isCurrent =
                isCurrentConversation(
                    conversationId
                );

            /*
             * Message content
             */
            conversation.lastMessage =
                message.content || '';

            /*
             * Message time
             */
            if (message.createdAt) {
                conversation.lastMessageAt =
                    message.createdAt;
            }

            /*
             * Unread
             */
            if (isContact) {

                if (isCurrent) {

                    conversation.unreadCount = 0;

                }
                else {

                    conversation.unreadCount =
                        (
                            Number(
                                conversation.unreadCount
                            ) || 0
                        ) + 1;
                }
            }

            /*
             * IMPORTANT:
             *
             * Không đổi conversation.status ở đây.
             *
             * Không update Open/Pending count ở đây.
             *
             * Backend sẽ quyết định status và
             * broadcast chat.conversation.status.updated.
             */

            renderConversationList();
        }

        /* =====================================================
           CONVERSATION STATUS
        ===================================================== */

        function registerConversationStatusUpdated() {

            state.connection.on(
                'chat.conversation.status.updated',
                async function (data) {

                    console.log(
                        '[SIGNALR STATUS EVENT]',
                        JSON.stringify(data)
                    );

                    if (!data?.conversationId) {
                        return;
                    }

                    updateConversationStatus(
                        data.conversationId,
                        data.status
                    );

                    /*
                     * Count luôn lấy từ DB.
                     */
                    await loadConversationCounts();
                }
            );
        }


        function updateConversationStatus(
            conversationId,
            status
        ) {

            const conversation =
                conversationState.items.get(
                    normalizeId(conversationId)
                );

            if (!conversation) {
                return;
            }

            const oldStatus =
                getConversationStatusKey(
                    conversation.status
                );

            const newStatus =
                getConversationStatusKey(
                    status
                );

            if (!newStatus) {
                return;
            }

            if (oldStatus === newStatus) {
                return;
            }

            console.log('[UPDATE STATUS]', {
                conversationId,
                oldStatus,
                newStatus
            });

            /*
             * Backend là source of truth.
             *
             * Chỉ cập nhật state local để UI phản ánh
             * status backend vừa broadcast.
             */
            conversation.status =
                getConversationStatusValue(
                    newStatus
                );

            renderConversationList();

            if (isCurrentConversation(conversationId)) {

                updateChatHeader(
                    findConversationItem(
                        conversationId
                    )
                );
            }
        }
        function getConversationStatusValue(status) {

            switch (status) {

                case 'open':
                    return 1;

                case 'pending':
                    return 2;

                case 'resolved':
                    return 3;

                case 'closed':
                    return 4;

                default:
                    return null;
            }
        }

        /* =====================================================
           ADMIN PRESENCE
        ===================================================== */

        function registerPresenceUpdated() {

            state.connection.on(
                'chat.admin.presence.updated',
                function (data) {

                    if (!data?.userId) {
                        return;
                    }

                    updateAdminPresence(
                        String(data.userId),
                        data.isOnline === true
                    );
                }
            );
        }

        function updateAdminPresence(
            userId,
            isOnline
        ) {

            const items =
                document.querySelectorAll(
                    `.conversation-item[data-assigned-user-id="${CSS.escape(
                        userId
                    )}"]`
                );

            items.forEach(
                item => {

                    const status =
                        item.querySelector(
                            '.admin-status'
                        );

                    if (!status) {
                        return;
                    }

                    status.textContent =
                        isOnline
                            ? 'Đang trực tuyến'
                            : 'Ngoại tuyến';

                    status.classList.toggle(
                        'online',
                        isOnline
                    );

                    status.classList.toggle(
                        'offline',
                        !isOnline
                    );
                }
            );
        }


        /* =====================================================
           FILTER
        ===================================================== */

        function updateStatusFilterCounts(
            counts
        ) {

            Object.entries(
                counts
            ).forEach(
                ([status, count]) => {

                    const badge =
                        document.querySelector(
                            `[data-status-count="${status}"]`
                        );

                    if (badge) {
                        badge.textContent =
                            count;
                    }
                }
            );
        }


        function updateUnreadFilterCount(
            count
        ) {

            const badge =
                document.querySelector(
                    '[data-filter-count="unread"]'
                );

            if (badge) {
                badge.textContent =
                    count;
            }
        }


        function updateConversationTotal(
            count
        ) {

            if (!DOM.conversationTotal) {
                return;
            }

            DOM.conversationTotal.textContent =
                `${count} conversations`;
        }


        /* =====================================================
           FILTER EVENTS
        ===================================================== */

        function initFilterEvents() {

            const filterMap = {
                inbox: 'inboxId',
                status: 'status',
                label: 'labelId',
                assigned: 'assignedUserId'
            };

            const filterItems =
                document.querySelectorAll(
                    '.chat-filter-list li[data-filter-type]'
                );

            filterItems.forEach(filterItem => {
                filterItem.addEventListener('click', async function () {

                    const type =
                        this.dataset.filterType;

                    const value =
                        this.dataset.filterValue;

                    const filterKey =
                        filterMap[type];

                    if (!filterKey) return;

                    // Chỉ một item active trong cùng filter type
                    document
                        .querySelectorAll(
                            `.chat-filter-list li[data-filter-type="${CSS.escape(type)}"]`
                        )
                        .forEach(item => {
                            item.classList.remove('active');
                        });

                    this.classList.add('active');

                    if (type === 'inbox') {

                        conversationState.filters.inboxId =
                            value === 'all'
                                ? null
                                : value;

                    } else if (type === 'status') {

                        conversationState.filters.status =
                            value || 'all';

                    } else {

                        conversationState.filters[filterKey] =
                            value || null;
                    }

                    await reloadConversationsWithFilters();

                    if (isMobile()) {
                        closeFilterSidebar();
                    }
                });
            });
        }
        async function reloadConversationsWithFilters() {
            conversationState.items.clear();
            conversationState.oldestLastMessageAt = null;
            conversationState.oldestId = null;
            conversationState.hasMore = true;

            await Promise.all([
                loadInitialConversations(),
                loadConversationCounts()
            ]);

            renderConversationList();
        }
        /* =====================================================
           SEARCH
        ===================================================== */

        function initSearch() {

            if (!DOM.conversationSearch) {
                return;
            }

            DOM.conversationSearch.addEventListener(
                'input',
                () => {
                    conversationState.filters.search =
                        DOM.conversationSearch.value
                            .trim()
                            .toLowerCase();

                    renderConversationList();

                    clearTimeout(
                        conversationSearchTimer
                    );

                    conversationSearchTimer = setTimeout(
                        () => {
                            loadConversationCounts();
                        },
                        300
                    );
                }
            );
        }


        /* =====================================================
           CONVERSATION EVENTS
        ===================================================== */

        function initConversationEvents() {

            DOM.conversationList?.addEventListener(
                'click',
                function (event) {

                    const item =
                        event.target.closest(
                            '.conversation-item'
                        );

                    if (!item) {
                        return;
                    }

                    openConversation(
                        item
                    );
                }
            );


            DOM.backButton?.addEventListener(
                'click',
                function () {

                    closeConversation();
                }
            );
        }


        /* =====================================================
           CONTACT DRAWER
        ===================================================== */

        function initContactEvents() {

            DOM.chatContactButton?.addEventListener(
                'click',
                function () {

                    if (
                        !state.currentConversationId
                    ) {
                        return;
                    }

                    DOM.contactDrawer
                        ?.classList.add(
                            'show'
                        );

                    updateOverlay();
                }
            );


            DOM.closeContactDrawerButton?.addEventListener(
                'click',
                closeContactDrawer
            );
        }


        /* =====================================================
           MOBILE FILTER
        ===================================================== */

        function initMobileEvents() {

            DOM.openChatFiltersButton?.addEventListener(
                'click',
                openFilterSidebar
            );


            DOM.filterClose?.addEventListener(
                'click',
                closeFilterSidebar
            );


            DOM.overlay?.addEventListener(
                'click',
                function () {

                    closeFilterSidebar();

                    closeContactDrawer();
                }
            );
        }


        /* =====================================================
           SEND MESSAGE
        ===================================================== */

        async function sendAdminMessage(
            content
        ) {

            if (
                !content ||
                !state.currentConversationId
            ) {
                return false;
            }

            if (
                !state.connection ||
                typeof signalR ===
                'undefined' ||
                state.connection.state !==
                signalR.HubConnectionState.Connected
            ) {

                console.warn(
                    'Chat SignalR is not connected.'
                );

                return false;
            }

            try {

                await state.connection.invoke(
                    'SendAdminMessage',
                    Number(
                        state.currentConversationId
                    ),
                    content
                );

                return true;

            }
            catch (error) {

                console.error(
                    'Send admin message failed:',
                    error
                );

                return false;
            }
        }


        function initSendMessage() {

            if (
                !DOM.sendMessageForm ||
                !DOM.messageInput
            ) {
                return;
            }


            DOM.messageInput.addEventListener(
                'input',
                handleAdminTyping
            );


            DOM.sendMessageForm.addEventListener(
                'submit',
                async function (event) {

                    event.preventDefault();

                    const content =
                        DOM.messageInput.value.trim();

                    if (!content) {
                        return;
                    }

                    stopTyping();

                    if (
                        !state.currentConversationId
                    ) {
                        return;
                    }


                    const sendButton =
                        DOM.sendMessageForm
                            .querySelector(
                                '.send-msg-btn'
                            );

                    if (sendButton) {
                        sendButton.disabled =
                            true;
                    }


                    try {

                        const success =
                            await sendAdminMessage(
                                content
                            );

                        if (success) {

                            DOM.messageInput.value =
                                '';

                            /*
                             * Message will be appended
                             * through SignalR.
                             */
                        }

                    }
                    finally {

                        if (sendButton) {
                            sendButton.disabled =
                                false;
                        }

                        DOM.messageInput.focus();
                    }
                }
            );
        }


        /* =====================================================
           REFRESH
        ===================================================== */

        function initRefresh() {

            DOM.refreshConversations?.addEventListener(
                'click',
                function () {

                    this.classList.add(
                        'ti-spin'
                    );

                    /*
                     * Chưa reload API ở đây.
                     *
                     * Sau khi lazy loading conversation
                     * hoàn chỉnh, refresh sẽ gọi:
                     *
                     * conversationStore.refresh()
                     */
                    setTimeout(
                        () => {

                            this.classList.remove(
                                'ti-spin'
                            );

                        },
                        500
                    );
                }
            );
        }


        /* =====================================================
           RETRY MESSAGE LOAD
        ===================================================== */

        DOM.chatHistory?.addEventListener(
            'click',
            function (event) {

                const retryButton =
                    event.target.closest(
                        '.chat-retry'
                    );

                if (!retryButton) {
                    return;
                }

                const conversationId =
                    retryButton.dataset
                        .conversationId;

                if (
                    !conversationId ||
                    !isCurrentConversation(
                        conversationId
                    )
                ) {
                    return;
                }

                loadConversationMessages(
                    conversationId
                );
            }
        );


        /* =====================================================
           INITIALIZATION
        ===================================================== */

        async function initialize() {
            conversationState.filters.inboxId =
                DOM.workspace?.dataset.inboxId || null;

            initFilterEvents();

            initSearch();

            initConversationEvents();

            initContactEvents();

            initMobileEvents();

            initSendMessage();

            initRefresh();

            setupConversationLazyLoading();

            setupMessageLazyLoading();

            scrollChatToBottom();

            await Promise.all([
                loadInitialConversations(),
                loadConversationCounts()
            ]);

            initChatSignalR();
        }


        initialize();
    }
);