window.handleValidationErrors = function(xhr, form) {
    const response = xhr.responseJSON;

    if (!response?.errors)
    {
        Toast.error(
            response?.detail ||
            response?.title ||
            'An error occurred.'
        );
        return;
    }

    // Xóa validation cũ
    form.querySelectorAll('.is-invalid').forEach(el => {
        el.classList.remove('is-invalid');
    });

    form.querySelectorAll('.invalid-feedback').forEach(el => {
        el.textContent = '';
    });

    // Xóa summary cũ
    const summary = form.querySelector(
        '[data-validation-summary]'
    );

    if (summary)
    {
        summary.innerHTML = '';
        summary.classList.add('d-none');
    }

    const summaryMessages = [];

    Object.entries(response.errors).forEach(([field, messages]) => {

    messages.forEach(message => {
        summaryMessages.push(message);
    });

    // Model-level error
    if (!field)
    {
        return;
    }
        const filePond = form.querySelector(
            `.filepond--root[data-filepond-field="${field}"]`
        );
        console.log('field:', field);
        console.log('filePond:', filePond);
        if (filePond) {
            filePond.classList.add('is-invalid');
            return;
        }
    // Tìm input/select/textarea theo name
    const input = form.querySelector(
            `[name = "${field}"]`
        );

    if (input)
    {
        input.classList.add('is-invalid');

        // Tìm validation message tương ứng
        const validationMessage = form.querySelector(
            `[data-valmsg-for= "${field}"]`
        );

        if (validationMessage) {
            validationMessage.textContent = messages.join(' ');
            validationMessage.classList.remove(
                'field-validation-valid'
            );
            validationMessage.classList.add(
                'field-validation-error'
            );
        }
        return;
    }


        // ==========================================
        // REPEATER FIELD
        // ==========================================

        showRepeaterValidationError(
            form,
            field,
            messages
        );
});

// Validation summary
if (summary && summaryMessages.length > 0)
{

    const ul = document.createElement('ul');

    summaryMessages.forEach(message => {
        const li = document.createElement('li');
        li.textContent = message;
        ul.appendChild(li);
    });

    summary.appendChild(ul);
    summary.classList.remove('d-none');
}
};

function clearValidationErrors(form) {
    form.querySelectorAll('.is-invalid')
        .forEach(element => {
            element.classList.remove('is-invalid');
        });

    form.querySelectorAll('[data-valmsg-for]')
        .forEach(element => {
            element.textContent = '';
            element.classList.remove('field-validation-error');
            element.classList.add('field-validation-valid');
        });
}

function showRepeaterValidationError(form, field, messages) {
    console.log('repeate error');
    // Ví dụ:
    // Options[0].Name
    // Attributes[2].Value
    // ProductOptions[1].Name

    const match = field.match(
        /^(.+)\[(\d+)\]\.([^.]+)$/
    );

    if (!match) {
        return false;
    }

    const repeaterName = match[1];
    const index = parseInt(match[2]);
    const property = match[3];

    // Tìm đúng repeater theo data-repeater-list
    const repeater = form.querySelector(
        `[data-repeater-list="${repeaterName}"]`
    );

    if (!repeater) {
        return false;
    }

    // Lấy item theo index
    const items = repeater.querySelectorAll(
        ':scope > [data-repeater-item]'
    );

    const item = items[index];

    if (!item) {
        return false;
    }

    // Tìm field
    const input = item.querySelector(
        `[name="${property}"]`
    );
    if (input) {
        input.classList.add('is-invalid');
    }

    // Tìm validation message
    const validationMessage = item.querySelector(
        `[data-valmsg-for="${property}"]`
    );

    if (validationMessage) {
        validationMessage.textContent =
            messages.join(' ');

        validationMessage.classList.remove(
            'field-validation-valid'
        );

        validationMessage.classList.add(
            'field-validation-error'
        );
    }

    return true;
}