function updateUnreadCount() {
    $.get('@Url.Action("GetUnreadCount", "Message")', function (count) {
        $('#unreadCount').text(count);
        $('#navUnreadCount').text(count > 0 ? count : '');
    });
}

// Update every 30 seconds
setInterval(updateUnreadCount, 30000);
$(document).ready(updateUnreadCount);