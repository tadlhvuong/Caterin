/// Address custom input - API v2

(function () {
    'use strict';

    const input = document.getElementById('Address');
    const dropdown = document.getElementById('addressDropdown');
    const optionsContainer = document.getElementById('addressOptions');
    const header = dropdown?.querySelector('.address-dropdown-header');
    document.getElementById('Address').value = document.getElementById('AddressDetail').value ?? '';
    let isSelectingFromDropdown = false;

    if (!input || !dropdown || !optionsContainer || !header) return;

    const API_PROVINCES = 'https://provinces.open-api.vn/api/v2/p/';
    const API_WARDS = 'https://provinces.open-api.vn/api/v2/p/';

    let provinces = [];
    const wardsCache = new Map();

    let selectedProvince = null;
    let selectedWard = null;

    // Phần địa chỉ cụ thể, không bắt buộc.
    let houseAddress = '';

    let currentStep = 'province';
    let typingTimer = null;
    let requestVersion = 0;
    let provincesPromise = null;

    function normalize(value = '') {
        return String(value)
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();
    }

    function getParts() {
        // Giữ nguyên logic của file cũ.
        return input.value.split(',').map(part => part.trim());
    }

    function composeAddress() {
        const specificAddress = houseAddress.trim().replace(/,\s*$/, '');

        input.value = [
            specificAddress,
            selectedWard?.name,
            selectedProvince?.name
        ].filter(Boolean).join(', ');
    }

    function openDropdown() {
        dropdown.classList.add('show');
        input.setAttribute('aria-expanded', 'true');
    }

    function closeDropdown() {
        dropdown.classList.remove('show');
        input.setAttribute('aria-expanded', 'false');
    }

    function renderOptions(items, label, onSelect) {
        header.textContent = label;
        optionsContainer.replaceChildren();

        if (!items.length) {
            const empty = document.createElement('div');
            empty.className = 'address-empty';
            empty.textContent = 'Không tìm thấy kết quả';
            optionsContainer.appendChild(empty);
            return;
        }

        items.forEach(item => {
            const button = document.createElement('button');

            button.type = 'button';
            button.className = 'address-option';
            button.setAttribute('role', 'option');
            button.textContent = item.name;

            button.addEventListener('mousedown', event => {
                event.preventDefault();
                isSelectingFromDropdown = true;

                try {
                    onSelect(item);
                    document.getElementById('ProvinceCode').value = selectedProvince?.code ?? '';
                    document.getElementById('WardCode').value = selectedWard?.code ?? '';
                } finally {
                    isSelectingFromDropdown = false;
                }
            });

            optionsContainer.appendChild(button);
        });
    }

    async function fetchJson(url) {
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                Accept: 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`API trả về HTTP ${ response.status } `);
        }

        return response.json();
    }

    // API trả danh sách tỉnh/thành.
    async function loadProvinces(forceReload = false) {
        if (provinces.length && !forceReload) {
            return provinces;
        }

        if (provincesPromise && !forceReload) {
            return provincesPromise;
        }

        provincesPromise = (async () => {
            const data = await fetchJson(API_PROVINCES);

            if (!Array.isArray(data)) {
                throw new Error('Dữ liệu tỉnh/thành không hợp lệ');
            }

            provinces = data
                .filter(item => item.code != null && item.name)
                .map(item => ({
                    code: String(item.code),
                    name: item.name
                }))
                .sort((a, b) => a.name.localeCompare(b.name, 'vi'));

            return provinces;
        })();

        try {
            return await provincesPromise;
        } catch (error) {
            provincesPromise = null;
            throw error;
        }
    }

    // API trả đối tượng tỉnh có thuộc tính wards.
    async function loadWardsByProvince(provinceCode, forceReload = false) {
        const code = String(provinceCode);

        if (wardsCache.has(code) && !forceReload) {
            return wardsCache.get(code);
        }
        console.log(`${API_WARDS}${encodeURIComponent(code)}?depth=2`);
        const result = await fetchJson(
            `${ API_WARDS }${ encodeURIComponent(code)}?depth=2`
        );

        const data = Array.isArray(result)
            ? result
            : result?.wards;

        if (!Array.isArray(data)) {
            throw new Error('Dữ liệu phường/xã không hợp lệ');
        }

        const wards = data
            .filter(item => item.code != null && item.name)
            .map(item => ({
                code: String(item.code),
                name: item.name
            }))
            .sort((a, b) => a.name.localeCompare(b.name, 'vi'));

        wardsCache.set(code, wards);

        return wards;
    }

    function showMessage(message, retryCallback = null) {
        optionsContainer.replaceChildren();

        const item = document.createElement('div');
        item.className = 'address-empty';
        item.textContent = message;
        optionsContainer.appendChild(item);

        if (retryCallback) {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'address-option text-primary';
            button.textContent = 'Thử tải lại';
            button.addEventListener('click', retryCallback);
            optionsContainer.appendChild(button);
        }
    }

    async function showProvinces(keyword = '', forceReload = false) {
        currentStep = 'province';

        const version = ++requestVersion;

        header.textContent = 'Đang tải tỉnh/thành...';
        optionsContainer.replaceChildren();
        openDropdown();

        try {
            const data = await loadProvinces(forceReload);

            if (version !== requestVersion) return;

            const query = normalize(keyword);

            const filtered = data.filter(province =>
                normalize(province.name).includes(query)
            );

            renderOptions(filtered, 'Chọn tỉnh/thành', province => {
                selectedProvince = province;
                selectedWard = null;

                // Giữ nguyên cách format của file cũ.
                composeAddress();

                currentStep = 'ward';
                showWards();
            });
        } catch (error) {
            console.error('Không thể tải tỉnh/thành:', error);

            showMessage(
                'Tải tỉnh/thành thất bại. Vui lòng thử lại.',
                () => showProvinces(keyword, true)
            );
        }
    }

    async function showWards(keyword = '', forceReload = false) {
        if (!selectedProvince) {
            showProvinces();
            return;
        }

        const province = selectedProvince;
        const version = ++requestVersion;

        currentStep = 'ward';

        header.textContent = `Đang tải phường / xã — ${ province.name } `;
        optionsContainer.replaceChildren();
        openDropdown();

        try {
            const wards = await loadWardsByProvince(
                province.code,
                forceReload
            );

            if (
                version !== requestVersion ||
                selectedProvince?.code !== province.code
            ) {
                return;
            }

            const query = normalize(keyword);

            const filtered = wards.filter(ward =>
                normalize(ward.name).includes(query)
            );

            renderOptions(
                filtered,
                `Chọn phường / xã — ${ province.name } `,
                ward => {
                    // Giữ nguyên logic chọn phường của file cũ.
                    const parts = getParts();

                    // Bỏ tỉnh ở cuối vì composeAddress() sẽ ghép lại.
                    if (
                        parts.length > 0 &&
                        selectedProvince &&
                        normalize(parts[parts.length - 1]) ===
                            normalize(selectedProvince.name)
                    ) {
                        parts.pop();
                    }

                    // Thay phần cuối bằng phường vừa chọn.
                    if (parts.length > 0) {
                        parts[parts.length - 1] = ward.name;
                    } else {
                        parts.push(ward.name);
                    }

                    selectedWard = ward;
                    houseAddress = parts.slice(0, -1).join(', ').trim();

                    composeAddress();
                    closeDropdown();
                }
            );
        } catch (error) {
            console.error('Không thể tải phường/xã:', error);

            if (
                version !== requestVersion ||
                selectedProvince?.code !== province.code
            ) {
                return;
            }

            showMessage(
                'Tải phường/xã thất bại. Vui lòng thử lại.',
                () => showWards(keyword, true)
            );
        }
    }

    function findProvince(name) {
        return provinces.find(province =>
            normalize(province.name) === normalize(name)
        ) || null;
    }

    async function findWard(province, name) {
        if (!province) return null;

        const wards = await loadWardsByProvince(province.code);

        return wards.find(ward =>
            normalize(ward.name) === normalize(name)
        ) || null;
    }

    // Đồng bộ trạng thái khi người dùng tự nhập hoặc sửa địa chỉ.
    async function syncSelectionFromText() {
        const parts = getParts();
        const previousWard = selectedWard;

        selectedProvince = null;
        selectedWard = null;
        houseAddress = '';

        if (!input.value.trim()) return;

        try {
            await loadProvinces();
        } catch {
            houseAddress = input.value.trim();
            return;
        }

        const province = findProvince(parts[parts.length - 1]);

        if (province) {
            selectedProvince = province;
            if (parts.length === 1) return;

            const ward = await findWard(
                province,
                parts[parts.length - 2]
            ).catch(() => null);

            if (ward) {
                selectedWard = ward;
                houseAddress = parts.slice(0, -2).join(', ').trim();
            } else {
                houseAddress = parts.slice(0, -1).join(', ').trim();
            }
            return;
        }

        // Giữ nguyên cách xử lý của file cũ khi tỉnh bị xóa.
        if (
            previousWard &&
            parts.some(part =>
                normalize(part) === normalize(previousWard.name)
            )
        ) {
            houseAddress = parts
                .filter(part =>
                    normalize(part) !== normalize(previousWard.name)
                )
                .join(', ')
                .trim();

            return;
        }

        houseAddress = input.value.trim();
    }

    function validateAddress(openIfNeeded = true) {
        if (!openIfNeeded) return;

        syncSelectionFromText().then(() => {
            if (selectedProvince && selectedWard) {
                closeDropdown();
                return;
            }

            if (selectedProvince) {
                showWards();
            } else {
                showProvinces();
            }
        });
    }

    input.addEventListener('focus', async () => {
        if (isSelectingFromDropdown) return;

        await syncSelectionFromText();

        if (selectedProvince && selectedWard) {
            closeDropdown();
            return;
        }

        if (selectedProvince) {
            showWards();
        } else {
            showProvinces();
        }
    });

    input.addEventListener('blur', () => {
        setTimeout(() => {
            if (isSelectingFromDropdown) return;
            closeDropdown();
        }, 150);
    });

    input.addEventListener('input', () => {
        if (isSelectingFromDropdown) return;

        clearTimeout(typingTimer);

        typingTimer = setTimeout(() => {
            validateAddress(true);
        }, 400);
    });
})();