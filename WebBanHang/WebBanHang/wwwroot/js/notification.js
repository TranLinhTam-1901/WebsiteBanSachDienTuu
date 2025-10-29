// Professional Notification System
window.NotificationSystem = {
    container: null,
    notifications: [],

    init() {
        this.container = document.getElementById('notificationContainer');
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'notification-container';
            this.container.id = 'notificationContainer';
            document.body.appendChild(this.container);
        }
    },

    show(message, type = 'success', title = 'Thông báo', duration = 5000) {
        this.init();

        const notification = document.createElement('div');
        notification.className = `notification-toast ${type}`;

        // Set icon based on type
        let icon = 'bi-check-circle-fill';
        switch (type) {
            case 'success':
                icon = 'bi-check-circle-fill';
                break;
            case 'error':
                icon = 'bi-x-circle-fill';
                break;
            case 'warning':
                icon = 'bi-exclamation-triangle-fill';
                break;
            case 'info':
                icon = 'bi-info-circle-fill';
                break;
        }

        notification.innerHTML = `
            <div class="notification-icon">
                <i class="bi ${icon}"></i>
            </div>
            <div class="notification-content">
                <div class="notification-title">${title}</div>
                <div class="notification-message">${message}</div>
            </div>
            <div class="notification-close">
                <i class="bi bi-x-lg"></i>
            </div>
        `;

        this.container.appendChild(notification);
        this.notifications.push(notification);

        // Auto remove after duration
        const timeoutId = setTimeout(() => {
            this.remove(notification);
        }, duration);

        // Close button event
        const closeBtn = notification.querySelector('.notification-close');
        closeBtn.addEventListener('click', () => {
            clearTimeout(timeoutId);
            this.remove(notification);
        });

        return notification;
    },

    remove(notification) {
        if (!notification || !notification.parentNode) return;

        notification.classList.add('fade-out');
        
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
            this.notifications = this.notifications.filter(n => n !== notification);
        }, 300);
    },

    // Convenience methods
    success(message, title = 'Thành công', duration = 5000) {
        return this.show(message, 'success', title, duration);
    },

    error(message, title = 'Lỗi', duration = 5000) {
        return this.show(message, 'error', title, duration);
    },

    warning(message, title = 'Cảnh báo', duration = 5000) {
        return this.show(message, 'warning', title, duration);
    },

    info(message, title = 'Thông tin', duration = 5000) {
        return this.show(message, 'info', title, duration);
    }
};

// Make it globally available
window.showNotification = (message, type, title) => {
    return NotificationSystem.show(message, type, title);
};

// For compatibility with existing code
window.showSuccessNotification = (message) => {
    return NotificationSystem.success(message);
};

window.showErrorNotification = (message) => {
    return NotificationSystem.error(message);
};

window.showWarningNotification = (message) => {
    return NotificationSystem.warning(message);
};

window.showInfoNotification = (message) => {
    return NotificationSystem.info(message);
};

