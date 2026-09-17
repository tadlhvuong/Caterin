'use strict';

// Select2 (jquery)
$(function () {
    const processedMessageIds = new Set();
    const MAX_PROCESSED_MESSAGE_IDS = 500;

  const adminChatConnection =
        new signalR.HubConnectionBuilder()
            .withUrl("/hubs/chat")
            .withAutomaticReconnect()
          .build();

    await adminChatConnection.invoke(
        "JoinInbox",
        inboxId
    );
    await adminChatConnection.invoke(
        "JoinAdminConversation",
        conversationId
    );
    adminChatConnection.on(
        "chat.message.received",
        function (message) {

            console.log("Admin received:", message);

            if (isDuplicateMessage(message?.id)) {
                return;
            }

            handleIncomingMessage(message);
        }
    );
    function handleIncomingMessage(message) {
        updateConversationList(message);

        if (currentConversationId === message.conversationId) {
            appendMessage(message);
            scrollChatToBottom();
        }
    }
    function isDuplicateMessage(messageId) {
        if (!messageId) {
            return true;
        }

        if (processedMessageIds.has(messageId)) {
            return true;
        }

        processedMessageIds.add(messageId);

        if (processedMessageIds.size > MAX_PROCESSED_MESSAGE_IDS) {
            const firstId = processedMessageIds.values().next().value;

            if (firstId !== undefined) {
                processedMessageIds.delete(firstId);
            }
        }

        return false;
    }
    function renderMessages(messages) {
        messages.forEach(message => {
            processedMessageIds.add(message.id);
            appendMessage(message);
        });
    }
});