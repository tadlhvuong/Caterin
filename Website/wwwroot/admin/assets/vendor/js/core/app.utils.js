window.AppUtils = (() => {

    function getField(form, name) {
        return form.elements[name] ??
            form.querySelector(`[name="${name}"]`);
    }

    function getValue(element) {

        if (!element)
            return "";

        switch (element.type) {

            case "checkbox":
                return element.checked;

            case "radio": {

                const checked = element.form.querySelector(
                    `input[name="${element.name}"]:checked`
                );

                return checked ? checked.value : "";

            }

            default:
                return element.value.trim();

        }

    }

    function addClass(element, className) {
        element?.classList.add(className);
    }

    function removeClass(element, className) {
        element?.classList.remove(className);
    }

    function removeClasses(element, ...classes) {
        element?.classList.remove(...classes);
    }

    function getRow(element, selector = ".mb-3") {
        return element.closest(selector);
    }

    function getFeedback(element) {

        let feedback = element.parentElement.querySelector(".invalid-feedback");

        if (!feedback) {

            feedback = document.createElement("div");

            feedback.className = "invalid-feedback";

            element.parentElement.appendChild(feedback);

        }

        return feedback;

    }

    function showError(element, message) {

        removeClasses(element, "is-valid");

        addClass(element, "is-invalid");

        const feedback = getFeedback(element);

        feedback.textContent = message;

    }

    function showSuccess(element) {

        removeClasses(element, "is-invalid");

        addClass(element, "is-valid");

        const feedback = element.parentElement.querySelector(".invalid-feedback");

        if (feedback) {
            feedback.textContent = "";
        }

    }

    function clear(element) {

        removeClasses(
            element,
            "is-valid",
            "is-invalid"
        );

        const feedback = element.parentElement.querySelector(".invalid-feedback");

        if (feedback) {
            feedback.textContent = "";
        }

    }

    function focusFirstInvalid(form) {

        const input = form.querySelector(".is-invalid");

        input?.focus();

    }

    function isEmpty(value) {

        if (value === null || value === undefined)
            return true;

        if (typeof value === "string")
            return value.trim() === "";

        if (Array.isArray(value))
            return value.length === 0;

        return false;

    }

    return {

        getField,

        getValue,

        addClass,

        removeClass,

        removeClasses,

        getRow,

        getFeedback,

        showError,

        showSuccess,

        clear,

        focusFirstInvalid,

        isEmpty

    };

})();