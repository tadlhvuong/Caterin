window.AppMessages = (() => {

    const messages = {

        vi: {

            required: "Trường này là bắt buộc.",

            email: "Địa chỉ email không hợp lệ.",

            password: "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và số.",

            minLength: ({ value }) =>
                `Vui lòng nhập ít nhất ${value} ký tự.`,

            maxLength: ({ value }) =>
                `Vui lòng nhập không quá ${value} ký tự.`,

            betweenLength: ({ min, max }) =>
                `Vui lòng nhập từ ${min} đến ${max} ký tự.`,

            regex: "Giá trị không đúng định dạng.",

            equalTo: "Giá trị không khớp.",

            number: "Vui lòng nhập số.",

            integer: "Vui lòng nhập số nguyên.",

            min: ({ value }) =>
                `Giá trị phải lớn hơn hoặc bằng ${value}.`,

            max: ({ value }) =>
                `Giá trị phải nhỏ hơn hoặc bằng ${value}.`,

            between: ({ min, max }) =>
                `Giá trị phải nằm trong khoảng từ ${min} đến ${max}.`,

            phone: "Số điện thoại không hợp lệ.",

            url: "Địa chỉ URL không hợp lệ.",

            remote: "Dữ liệu không hợp lệ.",

            callback: "Dữ liệu không hợp lệ."

        },

        en: {

            required: "This field is required.",

            email: "Please enter a valid email address.",

            password: "Password must contain at least 8 characters, including uppercase, lowercase and a number.",

            minLength: ({ value }) =>
                `Please enter at least ${value} characters.`,

            maxLength: ({ value }) =>
                `Please enter no more than ${value} characters.`,

            betweenLength: ({ min, max }) =>
                `Please enter between ${min} and ${max} characters.`,

            regex: "Invalid format.",

            equalTo: "The values do not match.",

            number: "Please enter a valid number.",

            integer: "Please enter a valid integer.",

            min: ({ value }) =>
                `Please enter a value greater than or equal to ${value}.`,

            max: ({ value }) =>
                `Please enter a value less than or equal to ${value}.`,

            between: ({ min, max }) =>
                `Please enter a value between ${min} and ${max}.`,

            phone: "Please enter a valid phone number.",

            url: "Please enter a valid URL.",

            remote: "The value is invalid.",

            callback: "The value is invalid."

        }

    };

    let culture = "vi";

    function setCulture(name) {

        if (messages[name]) {
            culture = name;
        }

    }

    function get(validator, options = {}) {

        const message = messages[culture]?.[validator];

        if (!message) {
            return "Invalid value.";
        }

        return typeof message === "function"
            ? message(options)
            : message;

    }

    return {

        get,

        setCulture

    };

})();