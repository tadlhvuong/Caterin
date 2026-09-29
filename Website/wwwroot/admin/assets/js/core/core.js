'use strict';

$(function () {
  const adminChatConnection =
        new signalR.HubConnectionBuilder()
            .withUrl("/hubs/chat")
            .withAutomaticReconnect()
          .build();

    adminChatConnection.on(
        'chat.conversation.status.updated',
        function (data) {

            updateChatPendingCount(
                data.pendingCount
            );

            // xử lý conversation status hiện tại
        }
    );
    function updateChatPendingCount(count) {
        const badge =
            document.getElementById(
                'chat-pending-count'
            );

        if (!badge) {
            return;
        }

        badge.textContent =
            Number(count) > 0
                ? count
                : '0';
    }
    function updateChatPendingCount(count) {
        const badge =
            document.getElementById(
                'chat-pending-count'
            );

        if (!badge) {
            return;
        }

        badge.textContent =
            Number(count) > 0
                ? count
                : '0';
    }
});