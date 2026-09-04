window.Toast = {
    show: function (message, type = "success") {

        const colors = {
            success: "linear-gradient(to right, #1F9345, #28a745)",
            error: "linear-gradient(to right, #dc3545, #c82333)",
            warning: "linear-gradient(to right, #ffc107, #e0a800)",
            info: "linear-gradient(to right, #0d6efd, #0b5ed7)"
        };

        Toastify({
            text: message,
            duration: 3500,
            close: true,
            gravity: "top",
            position: "right",
            stopOnFocus: true,
            style: {
                background: colors[type] || colors.info,
                borderRadius: "8px",
                boxShadow: "0 4px 15px rgba(0,0,0,.15)",
                fontSize: "14px",
                fontWeight: "500"
            }
        }).showToast();
    },

    success: function (message) {
        this.show(message, "success");
    },

    error: function (message) {
        this.show(message, "error");
    },

    warning: function (message) {
        this.show(message, "warning");
    },

    info: function (message) {
        this.show(message, "info");
    }
};