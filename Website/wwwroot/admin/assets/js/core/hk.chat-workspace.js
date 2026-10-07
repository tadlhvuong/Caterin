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

document.addEventListener('DOMContentLoaded', function () {

        const workspace =
            document.getElementById('chatWorkspace');

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
            contactAvatar:
                document.getElementById(
                    'contactAvatar'
                ),

            contactName:
                document.getElementById(
                    'contactName'
                ),

            contactType:
                document.getElementById(
                    'contactType'
                ),

            contactEmail:
                document.getElementById(
                    'contactEmail'
                ),

            contactPhone:
                document.getElementById(
                    'contactPhone'
                ),

            contactCreatedAt:
                document.getElementById(
                    'contactCreatedAt'
                ),

            contactLoading:
                document.getElementById(
                    'contactLoading'
                ),

            resolveConversationButton: document.getElementById(
                "resolveConversationButton"
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
                ),
            markAllAsRead:
                document.getElementById(
                    'markAllAsRead'
                ),

            conversationLabels: document.getElementById('conversationLabels'),
            addConversationLabelButton: document.getElementById('addConversationLabelButton'),
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
            currentConversationStatus: null,
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
            },

            contactConversationId: null,
            contactData: null,

            labels: [],

        };
        const conversationState = {
            items: new Map(),

            loading: false,
            hasMore: true,

            oldestLastMessageAt: null,
            oldestId: null,

            requestVersion: 0,
            requestController: null,

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
            website: 0,
            facebook: 0,
            open: 0,
            pending: 0,
            resolved: 0,
            loading: false
        };
        const limitState = {
            list: 10, //limit lazy loading conversation list
            message: 30 //limit lazy loading conversation message
        }
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

        function isCurrentConversation(conversationId) {
            return isSameId(conversationId, state.currentConversationId);
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
                        data-conversation-id="${escapeHtml(conversationId)}"> Thử lại
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
            return await response.json();
        }

        function buildConversationQueryParams({
            limit = 10,
            beforeLastMessageAt = null,
            beforeId = null
        } = {}) {

            const params =
                new URLSearchParams();


            if (conversationState.filters.inboxId) {

                params.set(
                    'inboxId',
                    conversationState.filters.inboxId
                );
            }


            if (conversationState.filters.status) {
                params.set(
                    'status',
                    conversationState.filters.status
                );
            }

            if (
                conversationState.filters.search
            ) {

                params.set(
                    'search',
                    conversationState.filters.search
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


            params.set(
                'limit',
                String(limit)
            );


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
        const requestVersion =
            ++conversationState.requestVersion;

        // Hủy request list trước đó
        if (conversationState.requestController) {
            conversationState.requestController.abort();
        }

        const controller = new AbortController();

        conversationState.requestController = controller;
        conversationState.loading = true;

        const params = buildConversationQueryParams({
            limit: limitState.list
        });

        const search =
            conversationState.filters.search?.trim();

        if (search) {
            params.set('search', search);
        }

        try {
            const response = await fetch(
                `/admin/chat/conversations?${params.toString()}`,
                {
                    method: 'GET',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    signal: controller.signal
                }
            );

            if (!response.ok) {
                throw new Error(
                    'Không thể tải danh sách hội thoại.'
                );
            }

            const result = await response.json();

            // Request này đã cũ.
            // Không được phép cập nhật state.
            if (
                requestVersion !==
                conversationState.requestVersion
            ) {
                return;
            }

            conversationState.items.clear();

            for (const item of result.items) {
                conversationState.items.set(
                    normalizeId(item.id),
                    item
                );
            }

            conversationState.hasMore =
                result.hasMore;

            conversationState.oldestLastMessageAt =
                result.oldestLastMessageAt;

            conversationState.oldestId =
                result.oldestId;

            renderConversationList();

        } catch (error) {

            // Abort là hành vi bình thường khi filter thay đổi.
            if (error.name === 'AbortError') {
                return;
            }

            // Request cũ thì bỏ qua lỗi luôn.
            if (
                requestVersion !==
                conversationState.requestVersion
            ) {
                return;
            }

            console.log(
                'loadInitialConversations:',
                error
            );

        } finally {

            if (
                requestVersion ===
                conversationState.requestVersion
            ) {
                conversationState.loading = false;

                if (
                    conversationState.requestController ===
                    controller
                ) {
                    conversationState.requestController = null;
                }
            }
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
                        limit: limitState.list,
                        beforeLastMessageAt:
                            conversationState.oldestLastMessageAt,
                        beforeId:
                            conversationState.oldestId
                    });
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
                for (const item of result.items) {
                    conversationState.items.set(
                        normalizeId(item.id),
                        item
                    );
                }

                conversationState.hasMore = result.hasMore;

                conversationState.oldestLastMessageAt = result.oldestLastMessageAt;

                conversationState.oldestId = result.oldestId;

                renderConversationList();

            } catch (error) {
                console.log('loadMoreConversations:', error
                );
            } finally {
                conversationState.loading = false;
            }
        }
        function renderConversationList() {
            if (!DOM.conversationList) {
                return;
            }
            const conversations = getFilteredConversations();

            DOM.conversationList.innerHTML = '';

            if (conversations.length === 0) {
                renderConversationEmptyState();
            }
            else {
                for (const conversation of conversations) {
                    const element = createConversationElement(conversation);
                    if (element) {
                        if (conversation.status != 3) {
                            element.classList.remove('is-resolved');
                            if (conversation.unreadCount === 0) {
                                element.classList.add('is-read');
                            }
                            else {
                                element.classList.remove('is-read');
                            }

                        } else {
                            element.classList.add('is-resolved');
                        }
                    }

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

            let items = Array.from(conversationState.items.values());
            items.sort((a, b) => {

                const dateCompare = new Date(b.lastMessageAt) - new Date(a.lastMessageAt);

                if (dateCompare !== 0) {
                    return dateCompare;
                }

                return Number(b.id) - Number(a.id);
            });

            return items.filter(matchesConversation);
        }
        function matchesConversationFilters(
            conversation
        ) {
            const name = conversation.contactName?.toLowerCase() || '';

            const preview =
                (
                    conversation.lastMessage ||
                    conversation.subject ||
                    ''
                ).toLowerCase();

            const search = conversationState.filters.search?.trim().toLowerCase() || '';
            if (search) {

                const matched = name.includes(search) || preview.includes(search);

                if (!matched) {
                    return false;
                }
            }

            if (conversationState.filters.assignedUserId) {

                if (normalizeId(conversation.assignedUserId) !== normalizeId(conversationState.filters.assignedUserId)
                ) {
                    return false;
                }
            }

            if (conversationState.filters.labelId) {

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

            const inboxFilter = conversationState.filters.inboxId;
            if (inboxFilter &&
                inboxFilter !== 'all' &&
                Number(conversation.inboxId) !==
                Number(inboxFilter)) {
                return false;
            }

            const statusFilter = conversationState.filters.status;
            if (statusFilter && statusFilter !== 'all' && getConversationStatusKey(conversation.status) !== statusFilter) {
                return false;
            }
            return matchesConversationFilters(conversation);
        }
        function createConversationElement(conversation) {
            const article = document.createElement('article');

            article.className = 'conversation-item';

            article.dataset.conversationId =
                conversation.id;
            const statusKey =
                getConversationStatusKey(
                    conversation.status
                );

            article.dataset.status = statusKey;

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
            <div class="avatar avatar-sm">
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
                        .map(label => `<span class="chat-label badge bg-label-success">${escapeHtml(label.name)}</span>`)
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

        switch (
        getConversationStatusKey(status)
        ) {
            case 'open':
                return 'Đang mở';

            case 'pending':
                return 'Đang chờ';

            case 'resolved':
                return 'Đã xử lý';

            case 'closed':
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
                            limit: limitState.message,
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

                console.log(
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
                            limit: limitState.message,
                            before: oldestMessageId
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

                console.log(
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
            console.log("loadConversationCounts");
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

                const params = new URLSearchParams();

                if (conversationState.filters.inboxId) {
                    params.set(
                        'inboxId',
                        conversationState.filters.inboxId
                    );
                }

                const search = conversationState.filters.search?.trim();

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

                conversationCountState.all =
                    Number(result.all) || 0;

                conversationCountState.website =
                    Number(result.website) || 0;

                conversationCountState.facebook =
                    Number(result.facebook) || 0;

                conversationCountState.open =
                    Number(result.open) || 0;

                conversationCountState.pending =
                    Number(result.pending) || 0;

                conversationCountState.resolved =
                    Number(result.resolved) || 0;
                updateConversationFilterCounts();

            }
            catch (error) {

                console.log(
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
                website: document.querySelector(
                    '.chat-filter-list li[data-filter-value="1"] .chat-filter-count'
                ),
                facebook: document.querySelector(
                    '.chat-filter-list li[data-filter-value="2"] .chat-filter-count'
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
            if (countElements.website) {
                countElements.website.textContent = counts.website;
            }
            if (countElements.facebook) {
                countElements.facebook.textContent = counts.facebook;
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
            state.currentConversationId = conversationId;
            state.currentConversationStatus =
                getConversationStatusKey(
                    conversationItem.dataset.status
                );
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

            renderConversationLabels(conversationItem.labels);
            /*
            * Resolved.
            */
            const statusKey =
                getConversationStatusKey(
                    conversationItem.dataset.status
                );

            state.currentConversationStatus = statusKey;
            updateConversationReadOnlyUI(
                conversationItem.dataset.status
            );
            /*
             * SignalR room.
             */
            await joinConversation(
                conversationId,
                conversationItem.dataset.status
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

            state.contactConversationId = null;
            state.contactData = null;
            resetContact();
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
            if (state.messageRequestController) {

                state.messageRequestController.abort();

                state.messageRequestController =
                    null;
            }

            await leaveCurrentConversation();

            state.currentConversationId = null;
            state.currentConversationStatus = null;

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

                console.log(
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

                console.log(
                    'Chat SignalR connection failed:',
                    error
                );
            }
        }


        function registerSignalREvents() {

            registerMessageReceived();

            registerConversationCreated();

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
                'chat.message.received', handleMessageReceived
            );
        }


    async function handleMessageReceived(message) {

        if (!message?.id) {
            return;
        }

        if (
            isMessageProcessed(message.id)
        ) {
            return;
        }

        const conversationId =
            normalizeId(message.conversationId);

        if (!conversationId) {
            return;
        }

        const current =
            isCurrentConversation(
                conversationId
            );

        /*
         * Message từ customer/contact.
         *
         * Nếu đang mở conversation hiện tại,
         * mark delivered + append detail.
         */
        await acknowledgeMessageDelivered(message);

        if (current) {

            appendChatMessage(message);

            scrollChatToBottom();

            scheduleMarkConversationAsRead(
                conversationId
            );
        }

        /*
         * Conversation chưa có trong Map.
         *
         * Đây là trường hợp customer gửi message đầu tiên
         * và server vừa tạo Conversation mới.
         *
         * MessageReceived đã được broadcast vào Inbox group,
         * nhưng Admin chưa có conversation trong state.
         *
         * Không thể tự dựng đầy đủ conversation item từ MessageDto
         * vì còn thiếu contactName / avatar / labels...
         *
         * Vì vậy chỉ trong trường hợp conversation mới,
         * load lại batch đầu tiên + counts.
         */
        const conversation =
            conversationState.items.get(
                conversationId
            );

        if (!conversation) {

            await reloadConversationsWithFilters();

            return;
        }

        /*
         * Conversation đã tồn tại:
         * chỉ update item hiện tại,
         * không reload toàn bộ list.
         */
        updateConversationList(
            message
        );
    }
        function registerConversationCreated() {

            state.connection.on(
                'chat.conversation.created',
                handleConversationCreated
            );
        }

        async function handleConversationCreated(
            conversation
        ) {

            if (!conversation?.id) {
                return;
            }

            const conversationId =
                normalizeId(conversation.id);

            /*
             * Conversation mới.
             */
            conversationState.items.set(
                conversationId,
                conversation
            );


            /*
             * Render lại list.
             *
             * renderConversationList()
             * sẽ tự:
             * - lấy Map
             * - sort theo lastMessageAt
             * - apply filter
             * - createConversationElement()
             */
            renderConversationList();
            await loadConversationCounts();
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
                await state.connection.invoke('AdminMessageDelivered', Number(message.id)
                );

            }
            catch (error) {

                console.log(
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

                    if (state.currentConversationId) {

                        state.joinedConversationId = null;

                        await joinConversation(
                            state.currentConversationId,
                            state.currentConversationStatus
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

                console.log(
                    'JoinInbox failed:',
                    error
                );
            }
        }


        /* =====================================================
           SIGNALR - CONVERSATION
        ===================================================== */

    async function joinConversation(
        conversationId,
        status = null
    ) {
        if (
            !conversationId ||
            !state.connection ||
            typeof signalR === 'undefined' ||
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

        if (state.joinedConversationId) {

            await state.connection.invoke(
                'LeaveAdminConversation',
                Number(state.joinedConversationId)
            );

            state.joinedConversationId = null;
        }

        try {

            const normalizedConversationId =
                normalizeId(conversationId);

            const statusKey =
                getConversationStatusKey(status);

            console.log(
                '[JOIN]',
                {
                    conversationId: normalizedConversationId,
                    status,
                    statusKey
                }
            );

            await state.connection.invoke(
                'JoinAdminConversation',
                Number(normalizedConversationId)
            );

            state.joinedConversationId =
                normalizedConversationId;

            /*
             * Resolved / Closed chỉ được xem.
             * Không được gọi OpenConversation.
             */
            if (
                statusKey === 'open' ||
                statusKey === 'pending'
            ) {
                await state.connection.invoke(
                    'OpenConversation',
                    Number(normalizedConversationId)
                );
            }

            console.log(
                'Joined conversation:',
                normalizedConversationId,
                statusKey
            );

        }
        catch (error) {

            console.error(
                'Join conversation failed:',
                error
            );

            state.joinedConversationId = null;

            throw error;
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

                console.log(
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
            if (isCurrentConversationReadOnly()) {
                stopTyping();
                hideTyping();
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
    function isCurrentConversationReadOnly() {

        return (
            state.currentConversationStatus === 'resolved' ||
            state.currentConversationStatus === 'closed'
        );
    }
    function isCurrentConversationResolved() {

        if (!state.currentConversationId) {
            return false;
        }

        if (
            state.currentConversationStatus === 'resolved'
        ) {
            return true;
        }

        const conversation =
            conversationState.items.get(
                normalizeId(
                    state.currentConversationId
                )
            );

        if (!conversation) {
            return false;
        }

        return (
            getConversationStatusKey(
                conversation.status
            ) === 'resolved'
        );
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


                await reloadConversationsWithFilters();

                return true;

            }
            catch (error) {

                console.log(
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

                await reloadConversationsWithFilters();
            }
        );
    }


    function updateConversationStatus(
        conversationId,
        status
    ) {
        const normalizedId =
            normalizeId(conversationId);

        const newStatus =
            getConversationStatusKey(status);

        if (!newStatus) {
            return;
        }

        const conversation =
            conversationState.items.get(
                normalizedId
            );

        /*
         * Current conversation vẫn phải update UI
         * kể cả khi item không còn nằm trong Map.
         */
        if (
            isCurrentConversation(
                normalizedId
            )
        ) {

            state.currentConversationStatus = newStatus;
            updateConversationReadOnlyUI(newStatus);

            if (DOM.chatContactStatus) {
                const statusText = {
                    open: 'Open',
                    pending: 'Pending',
                    resolved: 'Resolved'
                };

                DOM.chatContactStatus.textContent =
                    statusText[newStatus] ||
                    newStatus;
            }
        }

        if (!conversation) {
            return;
        }

        const oldStatus =
            getConversationStatusKey(
                conversation.status
            );

        if (oldStatus === newStatus) {
            return;
        }

        conversation.status =
            getConversationStatusValue(
                newStatus
            );

        renderConversationList();

        if (
            isCurrentConversation(
                normalizedId
            )
        ) {
            updateChatHeader(
                findConversationItem(
                    normalizedId
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

    function updateConversationReadOnlyUI(newStatus) {
        const statusKey = getConversationStatusKey(newStatus);
        const isReadOnly =
            statusKey === 'resolved' ||
            statusKey === 'closed';

        if (DOM.sendMessageForm) {
            DOM.sendMessageForm.classList.toggle(
                'd-none',
                isReadOnly
            );
        }

        if (DOM.messageInput) {
            DOM.messageInput.disabled = isReadOnly;

            if (isReadOnly) {
                DOM.messageInput.value = '';
            }
        }

        if (DOM.typing) {
            DOM.typing.hidden = true;
        }

        if (isReadOnly) {
            stopTyping();
            hideTyping();
        }

        if (DOM.resolveConversationButton) {
            DOM.resolveConversationButton.disabled = isReadOnly;
        }
    }
        /* =====================================================
           FILTER EVENTS
        ===================================================== */

    function initFilterEvents() {

        const filterContainer =
            document.querySelector(
                '.chat-filter-sidebar'
            );

        if (!filterContainer) {
            return;
        }

        filterContainer.addEventListener(
            'click',
            async function (event) {

                const filterItem =
                    event.target.closest(
                        'li[data-filter-type]'
                    );

                if (!filterItem ||
                    !filterContainer.contains(filterItem)) {
                    return;
                }

                const filterMap = {
                    inbox: 'inboxId',
                    status: 'status',
                    label: 'labelId',
                    assigned: 'assignedUserId'
                };

                const type =
                    filterItem.dataset.filterType;

                const value =
                    filterItem.dataset.filterValue;

                const filterKey =
                    filterMap[type];

                if (!filterKey) {
                    return;
                }

                if (
                    filterItem.classList.contains('active') &&
                    type !== 'inbox'
                ) {

                    filterItem.classList.remove('active');

                    conversationState.filters[
                        filterKey
                    ] = null;

                }
                else {

                    filterContainer
                        .querySelectorAll(
                            `li[data-filter-type="${CSS.escape(type)}"]`
                        )
                        .forEach(item => {
                            item.classList.remove('active');
                        });

                    filterItem.classList.add('active');

                    conversationState.filters[
                        filterKey
                    ] = value;
                }

                await reloadConversationsWithFilters();

                if (isMobile()) {
                    closeFilterSidebar();
                }
            }
        );
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

    async function initContactEvents() {

            DOM.chatContactButton?.addEventListener(
                'click',
                function () {
                    const conversationId = state.currentConversationId;
                    if (!conversationId) {
                        return;
                    }

                    DOM.contactDrawer?.classList.add('show');

                    updateOverlay();
                    loadContact(
                        conversationId
                    );
                }
            );


            DOM.closeContactDrawerButton?.addEventListener(
                'click',
                closeContactDrawer
            );
        }

    async function loadContact(conversationId) {

        // Đã load contact của conversation này
        if (
            state.contactConversationId ===
            conversationId &&
            state.contactData
        ) {
            renderContact(
                state.contactData
            );

            return;
        }

        setContactLoading(true);

        try {

            const response = await fetch(
                `/admin/chat/${conversationId}/contact`,
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
                    'Failed to load contact.'
                );
            }

            const contact =
                await response.json();
            console.log(contact);
            // Trong lúc request đang chạy,
            // user có thể đã click conversation khác
            if (
                state.currentConversationId !==
                conversationId
            ) {
                return;
            }

            state.contactConversationId =
                conversationId;

            state.contactData =
                contact;

            renderContact(contact);

        } catch (error) {

            console.log(
                'Load contact failed:',
                error
            );

            renderContactError();

        } finally {

            setContactLoading(false);
        }
    }
    function renderContact(contact) {

        if (!contact) {
            return;
        }

        if (DOM.contactName) {
            DOM.contactName.textContent =
                contact.name || 'Unknown';
        }

        if (DOM.contactType) {
            DOM.contactType.textContent =
                contact.type || 'Customer';
        }

        if (DOM.contactEmail) {
            DOM.contactEmail.textContent =
                contact.email || '-';
        }

        if (DOM.contactPhone) {
            DOM.contactPhone.textContent =
                contact.phone || '-';
        }

        if (DOM.contactCreatedAt) {
            DOM.contactCreatedAt.textContent = formatDate(contact.createdAt);
        }

        if (DOM.contactAvatar) {
            DOM.contactAvatar.src =
                contact.avatarUrl ||
                '/admin/assets/img/avatars/4.png';
        }
    }
    function resetContact() {

        if (DOM.contactName) {
            DOM.contactName.textContent = '-';
        }

        if (DOM.contactType) {
            DOM.contactType.textContent = 'Customer';
        }

        if (DOM.contactEmail) {
            DOM.contactEmail.textContent = '-';
        }

        if (DOM.contactPhone) {
            DOM.contactPhone.textContent = '-';
        }

        if (DOM.contactCreatedAt) {
            DOM.contactCreatedAt.textContent = '-';
        }

        if (DOM.contactAvatar) {
            DOM.contactAvatar.src =
                '/admin/assets/img/avatars/4.png';
        }
    }

    function setContactLoading(isLoading) {

        if (!DOM.contactLoading ||
            !DOM.contactContent) {
            return;
        }

        DOM.contactLoading.classList.toggle(
            'd-none',
            !isLoading
        );

        DOM.contactContent.classList.toggle(
            'd-none',
            isLoading
        );
    }

    //Fix A
    function registerConversationActions() {

        DOM.resolveConversationButton?.addEventListener(
            "click",
            async function () {

                const conversationId =
                    state.currentConversationId;

                if (!conversationId) {
                    return;
                }

                if (isCurrentConversationReadOnly()) {
                    return;
                }

                if (
                    !state.connection ||
                    typeof signalR === 'undefined' ||
                    state.connection.state !==
                    signalR.HubConnectionState.Connected
                ) {
                    console.warn(
                        "Chat SignalR is not connected."
                    );

                    return;
                }

                try {

                    this.disabled = true;

                    console.log(
                        "[RESOLVE]",
                        state.currentConversationId,
                        state.currentConversationStatus,
                        state.connection?.state
                    );

                    await state.connection.invoke(
                        "ResolveConversation",
                        Number(conversationId)
                    );

                    /*
                     * ==========================================
                     * UI RESOLVED NGAY LẬP TỨC
                     * ==========================================
                     */

                    state.currentConversationStatus =
                        'resolved';

                    updateConversationReadOnlyUI(
                        'resolved'
                    );

                }
                catch (error) {

                    console.error(
                        "Failed to resolve conversation:",
                        error
                    );

                }
                finally {

                    /*
                     * Nếu conversation đã resolved,
                     * button vẫn phải readonly.
                     */
                    this.disabled =
                        isCurrentConversationReadOnly();
                }
            }
        );
    }

    function formatDate(value) {
        if (!value) {
            return '-';
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return '-';
        }

        return new Intl.DateTimeFormat(
            'vi-VN',
            {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric'
            }
        ).format(date);
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

            //Chặn gửi message ở frontend
            if (
                !content ||
                !state.currentConversationId
            ) {
                return false;
            }

            if (isCurrentConversationReadOnly()) {
                return false;
            }
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

                console.log(
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

        async function initRefresh() {

            DOM.refreshConversations?.addEventListener(
                'click',
                async function () {

                    this.classList.add(
                        'ti-spin'
                    );

                    try {

                        /*
                         * Refresh luôn reset:
                         *
                         * - Inbox -> All
                         * - Status -> All
                         * - Label -> inactive
                         */
                        resetConversationFiltersForRefresh();


                        /*
                         * Load lại:
                         *
                         * - conversation list
                         * - conversation counts
                         */
                        await reloadConversationsWithFilters();

                    }
                    catch (error) {

                        console.log(
                            'REFRESH CONVERSATIONS ERROR:',
                            error
                        );

                    }
                    finally {

                        this.classList.remove(
                            'ti-spin'
                        );
                    }
                }
            );
        }
        function resetConversationFiltersForRefresh() {

            /*
             * Inbox
             * => trở về All
             */
            conversationState.filters.inboxId = null;

            document
                .querySelectorAll(
                    '.chat-filter-list li[data-filter-type="inbox"]'
                )
                .forEach(item => {
                    item.classList.remove('active');

                    if (
                        item.dataset.filterValue === 'all'
                    ) {
                        item.classList.add('active');
                    }
                });


            /*
             * Status
             * => bỏ filter
             */
            conversationState.filters.status = 'all';

            document
                .querySelectorAll(
                    '.chat-filter-list li[data-filter-type="status"]'
                )
                .forEach(item => {
                    item.classList.remove('active');
                });


            /*
             * Label
             * => bỏ filter
             */
            conversationState.filters.labelId = null;

            document
                .querySelectorAll(
                    '.chat-filter-list li[data-filter-type="label"]'
                )
                .forEach(item => {
                    item.classList.remove('active');
                });
        }

        /* =====================================================
           MARK ALL AS READ
        ===================================================== */
        function initMarkAllAsRead() {
            DOM.markAllAsRead?.addEventListener(
                'click',
                async function () {

                    if (
                        !state.connection ||
                        typeof signalR === 'undefined' ||
                        state.connection.state !==
                        signalR.HubConnectionState.Connected
                    ) {
                        return;
                    }

                    try {
                        this.disabled = true;

                        await state.connection.invoke(
                            'MarkAllConversationsAsRead'
                        );

                        /*
                         * Backend đã xử lý mark all read.
                         *
                         * Không tự sửa count ở client.
                         * Không tự đổi status ở client.
                         *
                         * Reload lại DB:
                         * - conversation list
                         * - filter counts
                         */
                        await reloadConversationsWithFilters();

                    }
                    catch (error) {
                        console.log(
                            'MARK ALL AS READ ERROR:',
                            error
                        );
                    }
                    finally {
                        this.disabled = false;
                    }
                }
            );
        }

    async function loadChatLabels() {

        try {

            const response =
                await fetch('/admin/chat/labels', {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

            if (!response.ok) {
                throw new Error(
                    'Không thể tải danh sách label.'
                );
            }

            const labels =
                await response.json();

            state.labels =
                Array.isArray(labels)
                    ? labels
                    : [];

            renderChatLabels();

        }
        catch (error) {

            console.error(
                'loadChatLabels:',
                error
            );

            state.labels = [];
        }
    }
    function renderChatLabels() {

        const labelList =
            document.querySelector(
                '.chat-filter-section[data-filter="label"] .chat-filter-list'
            );

        if (!labelList) {
            return;
        }

        labelList.innerHTML = '';

        state.labels
            .filter(label => label.isActive)
            .forEach(label => {

                const li =
                    document.createElement('li');

                li.classList.add('chat-label-item');

                li.dataset.filterType = 'label';
                li.dataset.filterValue = label.id;

                const a =
                    document.createElement('a');

                a.href = 'javascript:void(0);';

                const content =
                    document.createElement('span');

                content.className =
                    'd-flex align-items-center flex-grow-1';

                const dot =
                    document.createElement('span');

                dot.className =
                    'chat-label-dot';

                if (label.color) {
                    dot.style.backgroundColor =
                        label.color;
                }

                const name =
                    document.createElement('span');

                name.textContent =
                    label.name;

                content.appendChild(dot);
                content.appendChild(name);

                a.appendChild(content);


                const deleteButton =
                    document.createElement('button');

                deleteButton.type = 'button';

                deleteButton.className =
                    'btn btn-sm btn-icon text-danger chat-label-delete';

                deleteButton.dataset.labelId =
                    label.id;

                deleteButton.title =
                    'Xóa label';

                deleteButton.innerHTML =
                    '<i class="ti ti-trash"></i>';


                li.appendChild(a);
                li.appendChild(deleteButton);

                labelList.appendChild(li);
            });
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

    //LABELS

    const createLabelButton =
        document.getElementById('btn-create-chat-label');

    const chatLabelOffcanvasElement =
        document.getElementById('chatLabelOffcanvas');

    const createChatLabelForm =
        document.getElementById('form-create-chat-label');

    const chatLabelNameInput =
        document.getElementById('chat-label-name');

    const chatLabelColorInput =
        document.getElementById('chat-label-color');

    const chatLabelColorValueInput =
        document.getElementById('chat-label-color-value');

    const chatLabelIsActiveInput =
        document.getElementById('chat-label-is-active');

    const submitChatLabelButton =
        document.getElementById('btn-submit-chat-label');


    /* Open offcanvas */

    createLabelButton?.addEventListener('click', () => {

        const offcanvas =
            bootstrap.Offcanvas.getOrCreateInstance(
                chatLabelOffcanvasElement
            );

        offcanvas.show();
    });


    /* Color picker -> text */

    chatLabelColorInput?.addEventListener(
        'input',
        () => {

            chatLabelColorValueInput.value =
                chatLabelColorInput.value;
        }
    );


    /* Text -> color picker */

    chatLabelColorValueInput?.addEventListener(
        'input',
        () => {

            const value =
                chatLabelColorValueInput.value.trim();

            if (/^#[0-9A-Fa-f]{6}$/.test(value)) {

                chatLabelColorInput.value =
                    value;
            }
        }
    );


    /* Submit */

    createChatLabelForm?.addEventListener(
        'submit',
        async function (event) {

            event.preventDefault();


            const name =
                chatLabelNameInput.value.trim();

            const color =
                chatLabelColorValueInput.value.trim();

            const isActive =
                chatLabelIsActiveInput.checked;


            if (!name) {

                chatLabelNameInput.focus();

                return;
            }


            const token =
                createChatLabelForm.querySelector(
                    'input[name="__RequestVerificationToken"]'
                )?.value;


            try {

                submitChatLabelButton.disabled = true;


                const response =
                    await fetch(
                        '/admin/chat/labels',
                        {
                            method: 'POST',

                            headers: {
                                'Content-Type':
                                    'application/json',

                                'Accept':
                                    'application/json',

                                'RequestVerificationToken':
                                    token || ''
                            },

                            body: JSON.stringify({
                                name: name,
                                color: color || null,
                                isActive: isActive
                            })
                        }
                    );


                const result =
                    await response.json();


                if (!response.ok || !result.success) {

                    throw new Error(
                        result.message ||
                        'Không thể tạo label.'
                    );
                }


                /*
                 * API:
                 *
                 * {
                 *     success: true,
                 *     data: label
                 * }
                 */

                const label =
                    result.data;


                if (label) {

                    state.labels.push(label);

                    renderChatLabels();
                }


                /*
                 * Reset form
                 */

                createChatLabelForm.reset();

                chatLabelColorInput.value =
                    '#7367F0';

                chatLabelColorValueInput.value =
                    '#7367F0';

                chatLabelIsActiveInput.checked =
                    true;


                /*
                 * Close offcanvas
                 */

                const offcanvas =
                    bootstrap.Offcanvas.getOrCreateInstance(
                        chatLabelOffcanvasElement
                    );

                offcanvas.hide();


                /*
                 * Success toast
                 */

                if (typeof Toastify !== 'undefined') {

                    Toastify({
                        text: 'Đã thêm label.',
                        duration: 2500,
                        gravity: 'top',
                        position: 'right'
                    }).showToast();
                }


            } catch (error) {

                console.error(
                    'createChatLabel:',
                    error
                );


                if (typeof Toastify !== 'undefined') {

                    Toastify({
                        text:
                            error.message ||
                            'Không thể tạo label.',
                        duration: 3000,
                        gravity: 'top',
                        position: 'right'
                    }).showToast();

                } else {

                    alert(
                        error.message ||
                        'Không thể tạo label.'
                    );
                }


            } finally {

                submitChatLabelButton.disabled = false;
            }
        }
    );

    document.addEventListener(
        'click',
        async function (event) {

            const deleteButton =
                event.target.closest(
                    '.chat-label-delete'
                );

            if (!deleteButton) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();

            const labelId =
                deleteButton.dataset.labelId;

            if (!labelId) {
                return;
            }

            try {

                deleteButton.disabled = true;

                const response =
                    await fetch(
                        `/admin/chat/labels/${labelId}`,
                        {
                            method: 'DELETE',
                            headers: {
                                'Accept':
                                    'application/json',
                                'X-Requested-With':
                                    'XMLHttpRequest'
                            }
                        }
                    );

                const result =
                    await response.json();

                if (!response.ok || !result.success) {
                    throw new Error(
                        result.message ||
                        'Không thể xóa label.'
                    );
                }


                // Soft delete khỏi state
                state.labels =
                    state.labels.filter(
                        label =>
                            String(label.id) !==
                            String(labelId)
                    );

                renderChatLabels();


                if (typeof Toastify !== 'undefined') {

                    Toastify({
                        text: 'Đã xóa label.',
                        duration: 2500,
                        gravity: 'top',
                        position: 'right'
                    }).showToast();
                }

            } catch (error) {

                console.error(
                    'deleteChatLabel:',
                    error
                );

                if (typeof Toastify !== 'undefined') {

                    Toastify({
                        text:
                            error.message ||
                            'Không thể xóa label.',
                        duration: 3000,
                        gravity: 'top',
                        position: 'right'
                    }).showToast();
                }

            } finally {

                deleteButton.disabled = false;
            }
        }
    );

    function getCurrentConversationItem() {
        if (!state.currentConversationId) {
            return null;
        }

        return conversationState.items.get(
            normalizeId(state.currentConversationId)
        ) || null;
    }

    function renderConversationLabels(labels = null) {
        if (!DOM.conversationLabels) {
            return;
        }

        DOM.conversationLabels.innerHTML = '';

        const conversation =
            getCurrentConversationItem();

        const currentLabels = Array.isArray(labels)
            ? labels
            : conversation?.labels;

        if (!Array.isArray(currentLabels) || currentLabels.length === 0) {
            return;
        }

        currentLabels.forEach(label => {
            const labelElement = document.createElement('span');

            labelElement.className = 'chat-conversation-label';

            if (label.color) {
                labelElement.style.backgroundColor =
                    `${label.color}18`;

                labelElement.style.color =
                    label.color;
            }

            labelElement.innerHTML = `
            <span
                class="chat-conversation-label-dot"
                ${label.color
                    ? `style="background-color:${escapeHtml(label.color)}"`
                    : ''}
            ></span>

            <span>
                ${escapeHtml(label.name)}
            </span>

            <button
                type="button"
                class="chat-conversation-label-remove"
                data-label-id="${escapeHtml(label.id)}"
                title="Xóa label">
                <i class="ti ti-x"></i>
            </button>
        `;

            DOM.conversationLabels.appendChild(labelElement);
        });
    }

    function renderConversationLabels(labels = null) {
        if (!DOM.conversationLabels) {
            return;
        }

        DOM.conversationLabels.innerHTML = '';

        const conversation = getCurrentConversationItem();

        const currentLabels = Array.isArray(labels)
            ? labels
            : conversation?.labels;

        if (!Array.isArray(currentLabels)) {
            return;
        }

        currentLabels.forEach(label => {
            const wrapper = document.createElement('span');
            wrapper.className = 'chat-conversation-label';

            const color = typeof label.color === 'string'
                ? label.color.trim()
                : '';

            if (color) {
                wrapper.style.backgroundColor = `${color}18`;
                wrapper.style.color = color;
            }

            const dot = document.createElement('span');
            dot.className = 'chat-conversation-label-dot';

            if (color) {
                dot.style.backgroundColor = color;
            }

            const name = document.createElement('span');
            name.textContent = label.name || '';

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'chat-conversation-label-remove';
            removeButton.dataset.labelId = label.id;
            removeButton.title = 'Xóa label';
            removeButton.innerHTML = '<i class="ti ti-x"></i>';

            wrapper.appendChild(dot);
            wrapper.appendChild(name);
            wrapper.appendChild(removeButton);

            DOM.conversationLabels.appendChild(wrapper);
        });
    }

    function closeConversationLabelPicker() {
        document
            .querySelector('.chat-conversation-label-picker')
            ?.remove();
    }

    function openConversationLabelPicker() {
        closeConversationLabelPicker();

        const conversation = getCurrentConversationItem();

        if (!conversation || !DOM.addConversationLabelButton) {
            return;
        }

        const assignedIds = new Set(
            (conversation.labels || [])
                .map(label => normalizeId(label.id))
        );

        const availableLabels = (state.labels || [])
            .filter(label =>
                label.isActive !== false &&
                !assignedIds.has(normalizeId(label.id))
            );

        const picker = document.createElement('div');

        picker.className =
            'chat-conversation-label-picker dropdown-menu show';

        if (availableLabels.length === 0) {
            picker.innerHTML = `
            <span class="dropdown-item-text text-muted">
                Không còn label để gán
            </span>
        `;
        } else {
            availableLabels.forEach(label => {
                const button = document.createElement('button');

                button.type = 'button';
                button.className = 'dropdown-item d-flex align-items-center gap-2';
                button.dataset.labelId = label.id;

                const dot = document.createElement('span');

                dot.className = 'chat-conversation-label-dot';

                if (label.color) {
                    dot.style.backgroundColor = label.color;
                }

                const name = document.createElement('span');
                name.textContent = label.name;

                button.appendChild(dot);
                button.appendChild(name);

                picker.appendChild(button);
            });
        }

        document.body.appendChild(picker);

        const buttonRect =
            DOM.addConversationLabelButton.getBoundingClientRect();

        picker.style.position = 'fixed';
        picker.style.left = `${buttonRect.left}px`;
        picker.style.top = `${buttonRect.bottom + 4}px`;
        picker.style.zIndex = '1080';
    }

    async function assignConversationLabel(labelId) {
        const conversationId = state.currentConversationId;

        if (!conversationId || !labelId) {
            return;
        }

        try {
            const response = await fetch(
                `/admin/chat/${conversationId}/labels/${labelId}`,
                {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json'
                    }
                }
            );

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(
                    result.message || 'Không thể gán label.'
                );
            }

            const conversation =
                getCurrentConversationItem();

            if (conversation) {
                if (!Array.isArray(conversation.labels)) {
                    conversation.labels = [];
                }

                const exists = conversation.labels.some(
                    label =>
                        isSameId(label.id, result.data.id)
                );

                if (!exists) {
                    conversation.labels.push(result.data);
                }

                renderConversationLabels(conversation.labels);
            }

            closeConversationLabelPicker();

            renderConversationList();
            ChatScroll.update(DOM.conversationList);

            Toastify({
                text: 'Đã gán label.',
                duration: 2000,
                gravity: 'top',
                position: 'right'
            }).showToast();
        }
        catch (error) {
            console.error(
                'assignConversationLabel error:',
                error
            );

            Toastify({
                text: error.message || 'Không thể gán label.',
                duration: 3000,
                gravity: 'top',
                position: 'right'
            }).showToast();
        }
    }

    async function removeConversationLabel(labelId) {
        const conversationId = state.currentConversationId;

        if (!conversationId || !labelId) {
            return;
        }

        try {
            const response = await fetch(
                `/admin/chat/${conversationId}/labels/${labelId}`,
                {
                    method: 'DELETE',
                    headers: {
                        'Accept': 'application/json'
                    }
                }
            );

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(
                    result.message || 'Không thể xóa label.'
                );
            }

            const conversation =
                getCurrentConversationItem();

            if (conversation && Array.isArray(conversation.labels)) {
                conversation.labels =
                    conversation.labels.filter(
                        label =>
                            !isSameId(label.id, labelId)
                    );
            }

            renderConversationLabels(
                conversation?.labels || []
            );

            renderConversationList();
            ChatScroll.update(DOM.conversationList);

            Toastify({
                text: 'Đã xóa label.',
                duration: 2000,
                gravity: 'top',
                position: 'right'
            }).showToast();
        }
        catch (error) {
            console.error(
                'removeConversationLabel error:',
                error
            );

            Toastify({
                text: error.message || 'Không thể xóa label.',
                duration: 3000,
                gravity: 'top',
                position: 'right'
            }).showToast();
        }
    }

    DOM.addConversationLabelButton?.addEventListener(
        'click',
        function (event) {
            event.stopPropagation();
            openConversationLabelPicker();
        }
    );
    document.addEventListener('click', function (event) {
        const labelButton =
            event.target.closest(
                '.chat-conversation-label-picker [data-label-id]'
            );

        if (labelButton) {
            event.preventDefault();

            assignConversationLabel(
                labelButton.dataset.labelId
            );

            return;
        }

        const removeButton =
            event.target.closest(
                '.chat-conversation-label-remove'
            );

        if (removeButton) {
            event.preventDefault();
            event.stopPropagation();

            removeConversationLabel(
                removeButton.dataset.labelId
            );

            return;
        }

        if (
            !event.target.closest('.chat-conversation-label-picker') &&
            !event.target.closest('#addConversationLabelButton')
        ) {
            closeConversationLabelPicker();
        }
    });
        /* =====================================================
           INITIALIZATION
        ===================================================== */

        async function initialize() {
            //conversationState.filters.inboxId = DOM.workspace?.dataset.inboxId || null;
            conversationState.filters.inboxId = null;

            initFilterEvents();

            initSearch();

            registerConversationActions();

            initConversationEvents();

            initContactEvents();

            initMobileEvents();

            initSendMessage();

            initRefresh();

            initMarkAllAsRead();

            setupConversationLazyLoading();

            setupMessageLazyLoading();

            scrollChatToBottom();

            await Promise.all([
                loadInitialConversations(),
                loadConversationCounts(),
                loadChatLabels()
            ]);

            initChatSignalR();
        }


    initialize();

    }
);