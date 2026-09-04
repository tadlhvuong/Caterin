window.AppValidators = (() => {

    const EMAIL_REGEX =
        /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    const URL_REGEX =
        /^(https?:\/\/)?([\w-]+\.)+[\w-]{2,}(\/.*)?$/i;

    const PHONE_REGEX =
        /^[0-9()+\-\s]{8,20}$/;

    async function required(value, options) {

        const valid = value.trim().length > 0;

        return {
            valid,
            message: options.message
        };

    }

    async function email(value, options) {

        if (!value)
            return { valid: true };

        return {
            valid: EMAIL_REGEX.test(value),
            message: options.message
        };

    }

    async function minLength(value, options) {

        return {
            valid: value.length >= options.value,
            message: options.message
        };

    }

    async function maxLength(value, options) {

        return {
            valid: value.length <= options.value,
            message: options.message
        };

    }

    async function betweenLength(value, options) {

        return {

            valid:
                value.length >= options.min &&
                value.length <= options.max,

            message: options.message

        };

    }

    async function regex(value, options) {

        if (!value)
            return { valid: true };

        return {

            valid: options.pattern.test(value),

            message: options.message

        };

    }

    async function equalTo(value, options, input, form) {

        let compare = "";

        if (typeof options.value === "function") {

            compare = options.value(input, form);

        }
        else {

            compare = options.value;

        }

        return {

            valid: value === compare,

            message: options.message

        };

    }

    async function number(value, options) {

        if (!value)
            return { valid: true };

        return {

            valid: !isNaN(value),

            message: options.message

        };

    }

    async function integer(value, options) {

        if (!value)
            return { valid: true };

        return {

            valid: Number.isInteger(Number(value)),

            message: options.message

        };

    }

    async function min(value, options) {

        return {

            valid: Number(value) >= options.value,

            message: options.message

        };

    }

    async function max(value, options) {

        return {

            valid: Number(value) <= options.value,

            message: options.message

        };

    }

    async function between(value, options) {

        const number = Number(value);

        return {

            valid:
                number >= options.min &&
                number <= options.max,

            message: options.message

        };

    }

    async function phone(value, options) {

        if (!value)
            return { valid: true };

        return {

            valid: PHONE_REGEX.test(value),

            message: options.message

        };

    }

    async function url(value, options) {

        if (!value)
            return { valid: true };

        return {

            valid: URL_REGEX.test(value),

            message: options.message

        };

    }

    async function callback(value, options, input, form) {

        const result = await options.callback(value, input, form);

        if (typeof result === "boolean") {

            return {

                valid: result,

                message: options.message

            };

        }

        return result;

    }

    async function remote(value, options) {

        const response = await fetch(options.url, {

            method: options.method ?? "POST",

            headers: {
                "Content-Type": "application/json"
            },

            body: JSON.stringify({

                value

            })

        });

        return await response.json();

    }

    async function password(value, options) {

        if (!value)
            return { valid: true };

        const valid =
            value.length >= 8 &&
            /[A-Z]/.test(value) &&
            /[a-z]/.test(value) &&
            /\d/.test(value);

        return {

            valid,

            message: options.message

        };

    }

    return {

        required,

        email,

        minLength,

        maxLength,

        betweenLength,

        regex,

        equalTo,

        number,

        integer,

        min,

        max,

        between,

        phone,

        url,

        callback,

        remote,

        password

    };

})();