window.AppValidation = (() => {

    function create(form, options = {}) {

        const fields = options.fields || {};

        const validator = {

            form,

            fields,

            options,

            init() {

                Object.keys(fields).forEach(name => {

                    const input = this.getField(name);

                    if (!input) return;

                    input.addEventListener('blur', () => {
                        this.validateField(name);
                    });

                    input.addEventListener('input', () => {

                        if (input.classList.contains('is-invalid')) {
                            this.validateField(name);
                        }

                    });

                    input.addEventListener('change', () => {
                        this.validateField(name);
                    });

                });

                form.addEventListener('submit', async e => {

                    const valid = await this.validate();

                    if (!valid) {
                        e.preventDefault();
                        return;
                    }

                    if (options.onSuccess) {
                        e.preventDefault();
                        options.onSuccess(this);
                    }

                });

            },

            getField(name) {

                return form.querySelector(`[name="${name}"]`);

            },

            async validate() {

                let valid = true;

                for (const name of Object.keys(fields)) {

                    const result = await this.validateField(name);

                    if (!result) {
                        valid = false;
                    }

                }

                return valid;

            },

            async validateField(name) {

                const input = this.getField(name);

                if (!input)
                    return true;

                const config = fields[name];

                const validators = config.validators || {};

                for (const key of Object.keys(validators)) {

                    const rule = validators[key];

                    const fn = AppValidators[key];

                    if (!fn)
                        continue;

                    const result = await fn(input.value, rule, input, form);

                    if (!result.valid) {

                        this.showError(input, result.message);

                        options.onInvalid?.(name, result.message);

                        return false;

                    }

                }

                this.clearError(input);

                options.onValid?.(name);

                return true;

            },

            showError(input, message) {

                input.classList.remove('is-valid');
                input.classList.add('is-invalid');

                let feedback =
                    input.parentElement.querySelector('.invalid-feedback');

                if (!feedback) {

                    feedback = document.createElement('div');

                    feedback.className = 'invalid-feedback';

                    input.parentElement.appendChild(feedback);

                }

                feedback.textContent = message;

            },

            clearError(input) {

                input.classList.remove('is-invalid');
                input.classList.add('is-valid');

                const feedback =
                    input.parentElement.querySelector('.invalid-feedback');

                if (feedback) {

                    feedback.textContent = '';

                }

            },

            reset() {

                Object.keys(fields).forEach(name => {

                    const input = this.getField(name);

                    if (!input)
                        return;

                    input.classList.remove('is-valid');
                    input.classList.remove('is-invalid');

                    const feedback =
                        input.parentElement.querySelector('.invalid-feedback');

                    if (feedback) {

                        feedback.textContent = '';

                    }

                });

            },

            destroy() {

                this.reset();

            }

        };

        validator.init();

        return validator;

    }

    return {

        create

    };

})();