- Các cấu hình như đăng nhập fb, gg, sql: được lưu theo key user secrets.
- Time life jwt token: access token: 15', refresh token: 1d, refresh token have remember: 30d. setup in file appsetting.json
- Time life token: confirm email: 24h; còn lại như change pass, reset pass, confim p
- sau delop cầm thêm các biến even như sau trong service (os: centos):
[Unit]
Description=HKMain

[Service]
WorkingDirectory=/var/www/hkmain
ExecStart=/usr/bin/dotnet /var/www/hkmain/HKMain.dll

Environment="Jwt__Secret=xxxxxxxx"
Environment="Email__Password=xxxxxxxx"
Environment="Sms__ApiKey=xxxxxxxx"

Restart=always

[Install]
WantedBy=multi-user.target

- data-asset-path: path resource
- data-app-style-img (style: light/dark): change image follow style web

getMessageRowClass
getMessageClass

function clearMessages() {
    DOM.chatBody?.replaceChildren();

    clearQuickReplyTimer();

    state.processedMessageIds.clear();

    state.quickReplies.lastMessageAt = null;

    resetConversationHistoryState();
}

function clearMessages() {
    DOM.chatBody?.replaceChildren();
    state.processedMessageIds.clear();
}

function resetMessagePaging() {
    state.messages.oldestMessageId = null;
    state.messages.hasMore = true;
    state.messages.loading = false;
}
function resetConversationHistoryState() {
    state.history.previousConversationId = null;
    state.history.hasPrevious = false;
    state.history.loading = false;
    state.history.oldestLoadedConversationId = null;
    state.history.userHasScrolledUp = false;
    state.history.conversations.clear();

    hidePreviousConversationButton();
}
function clearConversationIdentity() {
    state.currentConversationId = null;
    state.currentConversationStatus = null;

    localStorage.removeItem(CONFIG.storage.conversationId);
}


 function resetToNewChatState() {
     clearMessages();
     resetMessagePaging();
     resetConversationHistoryState();
     /*
      * Chỉ reset conversation.
      *
      * Không xóa Contact.
      * Không xóa guestToken.
      */
     state.currentConversationId = null;
     state.currentConversationStatus = null;
     state.chatSession.hasActiveHistory = false;

     clearConversationIdentity();
     setUnreadMessageCount(0);
     showQuickReplies();
 }