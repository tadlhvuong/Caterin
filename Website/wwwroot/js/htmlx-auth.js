document.body.addEventListener('htmx:responseError', async function (evt) {

    if (evt.detail.xhr.status === 401) {

        const refreshResponse = await fetch('/admin/auth/refresh-token', {
            method: 'POST',
            credentials: 'include'
        });

        if (refreshResponse.ok) {

            htmx.ajax(
                evt.detail.requestConfig.verb,
                evt.detail.requestConfig.path,
                evt.detail.requestConfig
            );
        }
        else {
            window.location.href = '/login';
        }
    }
});