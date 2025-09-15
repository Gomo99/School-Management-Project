function updateUnreadCount() {
    $.get('@Url.Action("GetUnreadCount", "Message")', function (count) {
        $('#unreadCount').text(count);
        $('#navUnreadCount').text(count > 0 ? count : '');
    });
}

// Update every 30 seconds
setInterval(updateUnreadCount, 30000);
$(document).ready(updateUnreadCount);






// wwwroot/js/messaging.js
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/messageHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Connection management
connection.start().catch(err => console.error('SignalR Connection Error: ', err));

connection.onclose(async () => {
    await startConnection();
});

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        
        // Join user's personal group
        const userId = document.getElementById('userId').value;
        await connection.invoke("JoinUserGroup", parseInt(userId));
    } catch (err) {
        console.log(err);
        setTimeout(startConnection, 5000);
    }
}

// Real-time event handlers
connection.on("ReceiveMessage", (message) => {
    showNewMessageNotification(message);
    updateUnreadCount();
});

connection.on("MessageRead", (messageId, readerId) => {
    updateMessageReadStatus(messageId);
});

connection.on("UpdateUnreadCount", (count) => {
    updateUnreadBadge(count);
});

connection.on("UserTyping", (userId, isTyping) => {
    showTypingIndicator(userId, isTyping);
});

connection.on("UserOnlineStatusChanged", (userId, isOnline) => {
    updateUserOnlineStatus(userId, isOnline);
});

// Helper functions
function showNewMessageNotification(message) {
    const notification = document.createElement('div');
    notification.className = 'message-notification';
    notification.innerHTML = `
        <strong>New message from ${message.senderName}</strong>
        <p>${message.content.substring(0, 100)}...</p>
        <small>${new Date(message.sentAt).toLocaleTimeString()}</small>
    `;
    notification.onclick = () => window.location.href = `/Message/ViewMessage/${message.messageId}`;
    
    document.body.appendChild(notification);
    setTimeout(() => notification.remove(), 5000);
}

function updateUnreadBadge(count) {
    const badge = document.getElementById('unread-badge');
    if (badge) {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'inline' : 'none';
    }
}

function showTypingIndicator(userId, isTyping) {
    const indicator = document.getElementById(`typing-${userId}`);
    if (indicator) {
        indicator.style.display = isTyping ? 'block' : 'none';
    }
}

// Typing detection for compose page
let typingTimer;
const typingTimeout = 1000;

document.getElementById('messageContent')?.addEventListener('input', async function() {
    const conversationId = this.dataset.conversationId;
    if (conversationId) {
        await connection.invoke("StartTyping", parseInt(conversationId), parseInt(userId));
        
        clearTimeout(typingTimer);
        typingTimer = setTimeout(async () => {
            await connection.invoke("StopTyping", parseInt(conversationId), parseInt(userId));
        }, typingTimeout);
    }
});