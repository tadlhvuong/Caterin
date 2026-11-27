(function ($) {
    'use strict';

    const form = document.getElementById('bannerForm');

    if (!form) return;

    const bannerId = Number(form.dataset.bannerId || 0);
    const isEdit = bannerId > 0;

    const backgroundContainer = document.getElementById('backgroundContainer');
    const overlayContainer = document.getElementById('overlayContainer');
    const emptyOverlay = document.getElementById('emptyOverlay');

    const desktopPreview = document.getElementById('desktopPreview');
    const mobilePreview = document.getElementById('mobilePreview');

    const antiForgeryToken = form.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    const urls = {
        positions: '/admin/banner/positions',
        linkTypes: '/admin/banner/link-types',
        getUpdate: '/admin/banner/get-update-banner',
        create: '/admin/banner/create',
        edit: '/admin/banner/edit',
        index: '/admin/banner'
    };

    let mediaIndex = 0;

    // -------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------

    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        })[char]);
    }

    function showError(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: 'Không thành công',
                text: message
            });
        } else {
            alert(message);
        }
    }

    function showSuccess(message) {
        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: 'success',
                title: 'Thành công',
                text: message
            });
        }

        alert(message);
        return Promise.resolve();
    }

    function getErrorMessage(result) {
        if (result?.errors?.length) {
            return result.errors
                .map(x => x.message)
                .filter(Boolean)
                .join('\n');
        }

        return result?.message || 'Đã xảy ra lỗi. Vui lòng thử lại.';
    }

    async function getJson(url) {
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error('Không thể tải dữ liệu.');
        }

        return response.json();
    }

    function normalizeOptions(result) {
        const items = Array.isArray(result)
            ? result
            : result?.data ?? result?.items ?? result?.result ?? [];

        return items.map(item => ({
            value: item.value ?? item.id ?? item.key,
            text: item.text ?? item.name ?? item.label ?? item.displayName
        })).filter(x => x.value !== undefined);
    }

    async function loadSelectOptions(url, select, selectedValue) {
        try {
            const result = await getJson(url);
            const options = normalizeOptions(result);

            select.innerHTML = '<option value="">-- Chọn --</option>';

            options.forEach(item => {
                const option = document.createElement('option');
                option.value = item.value;
                option.textContent = item.text ?? item.value;

                select.appendChild(option);
            });

            if (selectedValue !== undefined && selectedValue !== null) {
                select.value = String(selectedValue);
            }
        } catch (error) {
            console.error(error);
            showError('Không thể tải danh sách tùy chọn Banner.');
        }
    }

    function getToken() {
        return antiForgeryToken;
    }

    function getMediaCards() {
        return [
            ...backgroundContainer.querySelectorAll('.banner-media-card'),
            ...overlayContainer.querySelectorAll('.banner-media-card')
        ];
    }

    // -------------------------------------------------------
    // MEDIA CARD
    // -------------------------------------------------------

    function createMediaCard(data = {}, isBackground = false) {
        const card = document.createElement('div');

        card.className = `banner-media-card ${isBackground ? 'is-background' : 'is-overlay'
            }`;

        card.dataset.mediaType = isBackground ? '1' : '2';

        const id = Number(data.id || 0);

        card.innerHTML = `
            <input type="hidden" class="media-id" value="${id}">
            <input type="hidden" class="media-type" value="${isBackground ? 1 : 2}">

            <div class="d-flex justify-content-between align-items-center mb-3">
                <div class="d-flex align-items-center gap-2">
                    ${isBackground
                ? '<i class="ti ti-photo text-success"></i><strong>Background</strong>'
                : '<i class="ti ti-grip-vertical banner-sort-handle"></i><strong class="overlay-title">Overlay</strong>'}
                </div>

                ${isBackground
                ? ''
                : `<button type="button"
                               class="btn btn-sm btn-label-danger btn-remove-overlay">
                            <i class="ti ti-trash me-1"></i>
                            Xóa
                       </button>`}
            </div>

            <div class="row g-3">

                <div class="col-md-6">
                    ${createDeviceEditor('desktop', data)}
                </div>

                <div class="col-md-6">
                    ${createDeviceEditor('mobile', data)}
                </div>

                <div class="col-12">
                    <label class="form-label">Alt text</label>
                    <input type="text"
                           class="form-control media-alt"
                           maxlength="255"
                           value="${escapeHtml(data.altText || '')}"
                           placeholder="Mô tả hình ảnh">
                </div>

                <div class="col-12">
                    <div class="row g-3">
                        <div class="col-4">
                            <label class="form-label">Thứ tự</label>
                            <input type="number"
                                   class="form-control media-sort"
                                   min="0"
                                   value="${Number(data.sortOrder || 0)} readonly">
                        </div>
                    </div>
                </div>

            </div>
        `;
        if (!isBackground) {
            card.draggable = true;
        }
        return card;
    }

    function createDeviceEditor(device, data) {
        const isDesktop = device === 'desktop';

        const file = isDesktop
            ? data.desktopUrl
            : data.mobileUrl;

        const x = isDesktop
            ? data.desktopX
            : data.mobileX;

        const y = isDesktop
            ? data.desktopY
            : data.mobileY;

        const width = isDesktop
            ? data.desktopWidth
            : data.mobileWidth;

        const title = isDesktop ? 'Desktop' : 'Mobile';

        return `
            <div class="border rounded p-3 h-100 device-editor"
                 data-device="${device}">

                <div class="fw-semibold mb-2">${title}</div>

                <div class="banner-image-preview mb-2">
                    ${file
                ? `<img src="${escapeHtml(file)}" alt="">`
                : '<span>Chưa có ảnh</span>'}
                </div>

                <input type="file"
                       class="form-control banner-upload-input mb-2 device-file"
                       accept="image/jpeg,image/png,image/webp">

                <input type="hidden"
                       class="device-remove"
                       value="false">

                <input type="hidden"
                       class="device-original-url"
                       value="${escapeHtml(file || '')}">

                <button type="button"
                        class="btn btn-sm btn-label-danger btn-remove-image mb-3"
                        ${file ? '' : 'disabled'}>
                    <i class="ti ti-trash me-1"></i>
                    Bỏ ảnh ${title}
                </button>

                <div class="row g-2">
                    <div class="col-4">
                        <label class="form-label small">X (%)</label>
                        <input type="number"
                               class="form-control form-control-sm device-x"
                               min="0" max="100" step="0.01"
                               value="${Number(x ?? 0)}">
                    </div>

                    <div class="col-4">
                        <label class="form-label small">Y (%)</label>
                        <input type="number"
                               class="form-control form-control-sm device-y"
                               min="0" max="100" step="0.01"
                               value="${Number(y ?? 0)}">
                    </div>

                    <div class="col-4">
                        <label class="form-label small">Width (%)</label>
                        <input type="number"
                               class="form-control form-control-sm device-width"
                               min="0" max="100" step="0.01"
                               value="${Number(width ?? 100)}">
                    </div>
                </div>
            </div>
        `;
    }

    function addBackground(data = {}) {
        backgroundContainer.innerHTML = '';

        const card = createMediaCard(data, true);
        backgroundContainer.appendChild(card);

        reindexMedia();
        updatePreview();
    }

    function addOverlay(data = {}) {
        const card = createMediaCard(data, false);

        overlayContainer.appendChild(card);

        updateOverlayState();
        reindexMedia();
        updatePreview();
    }

    function updateOverlayState() {
        const overlays = overlayContainer.querySelectorAll(
            '.banner-media-card'
        );

        emptyOverlay.style.display = overlays.length ? 'none' : 'block';

        overlays.forEach((card, index) => {
            const title = card.querySelector('.overlay-title');

            if (title) {
                title.textContent = `Overlay ${index + 1}`;
            }
        });
    }

    // -------------------------------------------------------
    // REINDEX FORM DATA
    // -------------------------------------------------------

    function reindexMedia() {
        const cards = getMediaCards();

        cards.forEach((card, index) => {
            const isBackground = card.dataset.mediaType === '1';

            card.querySelector('.media-sort').value =
                isBackground
                    ? 0
                    : [...overlayContainer.querySelectorAll('.banner-media-card')]
                        .indexOf(card) + 1;

            const fields = {
                Id: card.querySelector('.media-id').value,
                MediaType: card.querySelector('.media-type').value,
                AltText: card.querySelector('.media-alt').value,
                SortOrder: card.querySelector('.media-sort').value
            };

            const devices = {
                desktop: card.querySelector('[data-device="desktop"]'),
                mobile: card.querySelector('[data-device="mobile"]')
            };

            card.dataset.index = index;

            card.querySelector('.media-id').name =
                `Medias[${index}].Id`;

            card.querySelector('.media-type').name =
                `Medias[${index}].MediaType`;

            card.querySelector('.media-alt').name =
                `Medias[${index}].AltText`;

            card.querySelector('.media-sort').name =
                `Medias[${index}].SortOrder`;

            Object.entries(devices).forEach(([device, editor]) => {
                const prefix = device === 'desktop'
                    ? 'Desktop'
                    : 'Mobile';

                editor.querySelector('.device-file').name =
                    `Medias[${index}].${prefix}File`;

                editor.querySelector('.device-remove').name =
                    `Medias[${index}].Remove${prefix}Image`;

                editor.querySelector('.device-x').name =
                    `Medias[${index}].${prefix}X`;

                editor.querySelector('.device-y').name =
                    `Medias[${index}].${prefix}Y`;

                editor.querySelector('.device-width').name =
                    `Medias[${index}].${prefix}Width`;
            });
        });

        mediaIndex = cards.length;
        updateOverlayState();
    }

    // -------------------------------------------------------
    // IMAGE EVENTS
    // -------------------------------------------------------

    function updateDevicePreview(editor, src) {
        const preview = editor.querySelector('.banner-image-preview');

        preview.innerHTML = src
            ? `<img src="${escapeHtml(src)}" alt="">`
            : '<span>Chưa có ảnh</span>';

        editor.querySelector('.btn-remove-image').disabled = !src;
    }

    function handleFileChange(input) {
        const editor = input.closest('.device-editor');
        const file = input.files?.[0];

        if (!file) return;

        const allowedTypes = [
            'image/jpeg',
            'image/png',
            'image/webp'
        ];

        if (!allowedTypes.includes(file.type)) {
            input.value = '';
            showError('Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.');
            return;
        }

        if (file.size > 5 * 1024 * 1024) {
            input.value = '';
            showError('Dung lượng ảnh không được vượt quá 5MB.');
            return;
        }

        editor.querySelector('.device-remove').value = 'false';

        const objectUrl = URL.createObjectURL(file);

        updateDevicePreview(editor, objectUrl);
        updatePreview();
    }

    function removeDeviceImage(button) {
        const editor = button.closest('.device-editor');

        const fileInput = editor.querySelector('.device-file');
        const removeInput = editor.querySelector('.device-remove');
        const originalUrl = editor.querySelector('.device-original-url');

        fileInput.value = '';
        removeInput.value = 'true';

        originalUrl.value = '';

        updateDevicePreview(editor, null);
        updatePreview();
    }

    // -------------------------------------------------------
    // LIVE PREVIEW
    // -------------------------------------------------------

    function getDeviceImage(editor) {
        const file = editor.querySelector('.device-file').files?.[0];

        if (file) {
            return URL.createObjectURL(file);
        }

        if (editor.querySelector('.device-remove').value === 'true') {
            return null;
        }

        return editor.querySelector('.device-original-url').value || null;
    }

    function drawPreview(container, device) {
        container.innerHTML = '';

        const cards = getMediaCards();

        const background = cards.find(
            x => x.dataset.mediaType === '1'
        );

        if (!background) {
            container.innerHTML =
                '<div class="banner-preview-placeholder">Chưa có hình ảnh</div>';
            return;
        }

        const bgEditor = background.querySelector(
            `[data-device="${device}"]`
        );

        const bgSrc = getDeviceImage(bgEditor);

        if (!bgSrc) {
            container.innerHTML =
                '<div class="banner-preview-placeholder">Chưa có Background</div>';
            return;
        }

        const bgImage = document.createElement('img');
        bgImage.src = bgSrc;
        bgImage.className = 'preview-background';

        container.appendChild(bgImage);

        cards.filter(x => x.dataset.mediaType === '2')
            .forEach(card => {
                const editor = card.querySelector(
                    `[data-device="${device}"]`
                );

                const src = getDeviceImage(editor);

                if (!src) return;

                const image = document.createElement('img');

                image.src = src;
                image.className = 'preview-overlay';

                image.style.left =
                    `${editor.querySelector('.device-x').value || 0}%`;

                image.style.top =
                    `${editor.querySelector('.device-y').value || 0}%`;

                image.style.width =
                    `${editor.querySelector('.device-width').value || 100}%`;

                container.appendChild(image);
            });
    }

    function updatePreview() {
        drawPreview(desktopPreview, 'desktop');
        drawPreview(mobilePreview, 'mobile');
    }

    // -------------------------------------------------------
    // LOAD EXISTING BANNER
    // -------------------------------------------------------

    function setDateTimeInput(selector, value) {
        const input = document.querySelector(selector);

        if (!value) {
            input.value = '';
            return;
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            input.value = '';
            return;
        }

        const localDate = new Date(
            date.getTime() - date.getTimezoneOffset() * 60000
        );

        input.value = localDate.toISOString().slice(0, 16);
    }

    async function loadBanner() {
        const result = await getJson(
            `${urls.getUpdate}?id=${bannerId}`
        );

        if (!result.success || !result.data) {
            throw new Error(result.message || 'Không tải được Banner.');
        }

        const data = result.data;

        document.getElementById('Title').value = data.title || '';
        document.getElementById('Position').value = String(data.position);
        document.getElementById('LinkType').value = String(data.linkType);
        document.getElementById('LinkValue').value = data.linkValue || '';
        document.getElementById('SortOrder').value = data.sortOrder ?? 0;
        document.getElementById('IsPublished').checked = !!data.isPublished;

        setDateTimeInput('#StartAt', data.startAt);
        setDateTimeInput('#EndAt', data.endAt);

        const medias = data.medias || [];

        const background = medias.find(x => Number(x.mediaType) === 1);

        addBackground(background || {});

        medias.filter(x => Number(x.mediaType) === 2)
            .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
            .forEach(item => addOverlay(item));

        reindexMedia();
        updatePreview();
    }

    // -------------------------------------------------------
    // SUBMIT
    // -------------------------------------------------------

    function appendUtcDate(formData, inputId, fieldName) {
        const value = document.getElementById(inputId).value.trim();

        // Xóa giá trị cũ trong FormData.
        formData.delete(fieldName);

        // Không nhập thì không gửi field.
        // DateTime? ở server sẽ nhận null.
        if (!value) {
            return;
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            throw new Error('Thời gian không hợp lệ.');
        }

        formData.append(fieldName, date.toISOString());
    }

    async function submitForm(event) {
        event.preventDefault();

        reindexMedia();

        const title = document.getElementById('Title').value.trim();

        if (!title) {
            showError('Vui lòng nhập tên Banner.');
            return;
        }

        const background = backgroundContainer.querySelector(
            '.banner-media-card'
        );

        if (!background) {
            showError('Banner cần có Background.');
            return;
        }

        if (document.getElementById('IsPublished').checked) {
            const desktop = getDeviceImage(
                background.querySelector('[data-device="desktop"]')
            );

            const mobile = getDeviceImage(
                background.querySelector('[data-device="mobile"]')
            );

            if (!desktop && !mobile) {
                showError(
                    'Banner cần có ít nhất một ảnh Background Desktop hoặc Mobile trước khi xuất bản.'
                );
                return;
            }
        }

        const startAt = document.getElementById('StartAt').value.trim();
        const endAt = document.getElementById('EndAt').value.trim();

        if (startAt && endAt) {
            const startDate = new Date(startAt);
            const endDate = new Date(endAt);

            if (
                Number.isNaN(startDate.getTime()) ||
                Number.isNaN(endDate.getTime())
            ) {
                showError('Thời gian không hợp lệ.');
                return;
            }

            if (endDate <= startDate) {
                showError('Thời gian kết thúc phải lớn hơn thời gian bắt đầu.');
                return;
            }
        }

        const button = document.getElementById('btnSaveBanner');

        button.disabled = true;
        button.innerHTML =
            '<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...';

        try {
            const formData = new FormData(form);
            formData.set('Id', String(isEdit ? bannerId : 0));
            // DateTime được gửi dưới dạng UTC ISO 8601.
            appendUtcDate(formData, 'StartAt', 'StartAt');
            appendUtcDate(formData, 'EndAt', 'EndAt');

            // Checkbox unchecked không được browser gửi lên.
            formData.set(
                'IsPublished',
                document.getElementById('IsPublished').checked ? 'true' : 'false'
            );

            const response = await fetch(
                isEdit ? urls.edit : urls.create,
                {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': getToken(),
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: formData
                }
            );

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(getErrorMessage(result));
            }

            await showSuccess(
                result.message || 'Lưu Banner thành công.'
            );

            window.location.href = result.redirectUrl || urls.index;
        } catch (error) {
            console.error(error);
            showError(error.message);
        } finally {
            button.disabled = false;
            button.innerHTML = isEdit
                ? '<i class="ti ti-device-floppy me-1"></i> Lưu thay đổi'
                : '<i class="ti ti-device-floppy me-1"></i> Tạo Banner';
        }
    }

    // -------------------------------------------------------
    // EVENTS
    // -------------------------------------------------------

    document.getElementById('btnAddOverlay')
        .addEventListener('click', () => addOverlay());

    overlayContainer.addEventListener('click', event => {
        const button = event.target.closest('.btn-remove-overlay');

        if (!button) return;

        button.closest('.banner-media-card').remove();

        reindexMedia();
        updatePreview();
    });

    form.addEventListener('change', event => {
        if (event.target.matches('.device-file')) {
            handleFileChange(event.target);
        }
    });

    form.addEventListener('click', event => {
        const button = event.target.closest('.btn-remove-image');

        if (button) {
            removeDeviceImage(button);
        }
    });

    form.addEventListener('input', event => {
        if (
            event.target.matches('.device-x') ||
            event.target.matches('.device-y') ||
            event.target.matches('.device-width')
        ) {
            updatePreview();
        }
    });

    form.addEventListener('submit', submitForm);

    let draggedOverlay = null;

    overlayContainer.addEventListener('dragstart', event => {
        const card = event.target.closest('.banner-media-card');

        if (!card || !event.target.closest('.banner-sort-handle')) {
            event.preventDefault();
            return;
        }

        draggedOverlay = card;
        card.classList.add('opacity-50');

        event.dataTransfer.effectAllowed = 'move';
    });

    overlayContainer.addEventListener('dragover', event => {
        event.preventDefault();

        const target = event.target.closest('.banner-media-card');

        if (!draggedOverlay || !target || target === draggedOverlay) {
            return;
        }

        const rect = target.getBoundingClientRect();
        const insertAfter = event.clientY > rect.top + rect.height / 2;

        overlayContainer.insertBefore(
            draggedOverlay,
            insertAfter ? target.nextSibling : target
        );
    });

    overlayContainer.addEventListener('dragend', () => {
        if (draggedOverlay) {
            draggedOverlay.classList.remove('opacity-50');
            draggedOverlay = null;
        }

        reindexMedia();
        updatePreview();
    });

    // -------------------------------------------------------
    // INIT
    // -------------------------------------------------------

    async function init() {
        addBackground();

        await Promise.all([
            loadSelectOptions(
                urls.positions,
                document.getElementById('Position')
            ),
            loadSelectOptions(
                urls.linkTypes,
                document.getElementById('LinkType')
            )
        ]);

        if (isEdit) {
            await loadBanner();
        }
    }

    init().catch(error => {
        console.error(error);
        showError(error.message || 'Không thể khởi tạo form Banner.');
    });

})(jQuery);