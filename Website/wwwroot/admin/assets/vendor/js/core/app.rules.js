window.AppRules = (() => {

    function required(message) {
        return {
            validators: {
                required: {
                    message
                }
            }
        };
    }

    function email(message) {
        return {
            validators: {
                required: {
                    message: message?.required
                },
                email: {
                    message: message?.email
                }
            }
        };
    }

    function password(message) {
        return {
            validators: {
                required: {
                    message: message?.required
                },
                password: {
                    message: message?.password
                }
            }
        };
    }

    function confirmPassword(form, fieldName = "Password", message) {
        return {
            validators: {
                required: {
                    message: message?.required
                },
                equalTo: {
                    value: () => form.elements[fieldName].value,
                    message: message?.equalTo
                }
            }
        };
    }

    function minLength(length, message) {
        return {
            validators: {
                minLength: {
                    value: length,
                    message
                }
            }
        };
    }

    function maxLength(length, message) {
        return {
            validators: {
                value: length,
                message
            }
        };
    }

    function betweenLength(min, max, message) {
        return {
            validators: {
                betweenLength: {
                    min,
                    max,
                    message
                }
            }
        };
    }

    function regex(pattern, message) {
        return {
            validators: {
                regex: {
                    pattern,
                    message
                }
            }
        };
    }

    function phone(message) {
        return {
            validators: {
                phone: {
                    message
                }
            }
        };
    }

    function url(message) {
        return {
            validators: {
                url: {
                    message
                }
            }
        };
    }

    function number(message) {
        return {
            validators: {
                number: {
                    message
                }
            }
        };
    }

    function integer(message) {
        return {
            validators: {
                integer: {
                    message
                }
            }
        };
    }

    function min(value, message) {
        return {
            validators: {
                min: {
                    value,
                    message
                }
            }
        };
    }

    function max(value, message) {
        return {
            validators: {
                max: {
                    value,
                    message
                }
            }
        };
    }

    function between(min, max, message) {
        return {
            validators: {
                between: {
                    min,
                    max,
                    message
                }
            }
        };
    }

    function callback(callback, message) {
        return {
            validators: {
                callback: {
                    callback,
                    message
                }
            }
        };
    }

    function remote(url, options = {}) {
        return {
            validators: {
                remote: {
                    url,
                    method: options.method ?? "POST",
                    data: options.data,
                    message: options.message
                }
            }
        };
    }

    function combine(...rules) {

        const validators = {};

        rules.forEach(rule => {
            Object.assign(validators, rule.validators);
        });

        return {
            validators
        };
    }

    return {

        required,

        email,

        password,

        confirmPassword,

        minLength,

        maxLength,

        betweenLength,

        regex,

        phone,

        url,

        number,

        integer,

        min,

        max,

        between,

        callback,

        remote,

        combine

    };

})();