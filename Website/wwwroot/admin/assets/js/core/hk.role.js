document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    const modalElement = document.getElementById('addRoleModal');
    const form = document.getElementById('addRoleForm');

    if (!modalElement || !form) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

    const title = modalElement.querySelector('.role-title');
    const roleIdInput = document.getElementById('modalRoleId');
    const roleNameInput = document.getElementById('modalRoleName')
    const roleLevelGroup = document.getElementById('roleLevelGroup');
    const roleLevelInput = document.getElementById('modalRoleLevel');
    const roleDescriptionInput = document.getElementById('modalRoleDescription');
    const saveButton = document.getElementById('saveRoleBtn');

    // URL theo Route của RolePermissionController.
    const baseUrl = '/admin/role-perms';

    const container = document.getElementById('roleCardsContainer');
    const currentUserIsRoot = container?.dataset.isRoot === 'true';
    const template = document.getElementById('roleCardTemplate');
    const permissionsModalElement = document.getElementById('rolePermissionsModal');

    const permissionsModal = permissionsModalElement
        ? bootstrap.Modal.getOrCreateInstance(permissionsModalElement)
        : null;

    const permissionsTitle =
        document.getElementById('rolePermissionsTitle');

    const permissionsSubtitle =
        document.getElementById('rolePermissionsSubtitle');

    const permissionsLoading =
        document.getElementById('rolePermissionsLoading');

    const permissionsError =
        document.getElementById('rolePermissionsError');

    const permissionsHead =
        document.getElementById('rolePermissionsHead');

    const permissionsBody =
        document.getElementById('rolePermissionsBody');
    const saveRolePermissionsBtn =
        document.getElementById('saveRolePermissionsBtn');

    let currentPermissionRoleId = null;
    // MEMBERS
    const membersModalElement =
        document.getElementById('roleMembersModal');

    const membersModal = membersModalElement
        ? bootstrap.Modal.getOrCreateInstance(membersModalElement)
        : null;

    const membersTitle = document.getElementById('roleMembersTitle');
    const membersSubtitle = document.getElementById('roleMembersSubtitle');
    const membersCount = document.getElementById('roleMembersCount');
    const membersLoading = document.getElementById('roleMembersLoading');
    const membersError = document.getElementById('roleMembersError');
    const membersEmpty = document.getElementById('roleMembersEmpty');
    const membersBody = document.getElementById('roleMembersBody');

    // ASSIGN ROLE MEMBERS

    const assignMembersModalElement =
        document.getElementById('assignRoleMembersModal');

    const assignMembersModal = assignMembersModalElement
        ? bootstrap.Modal.getOrCreateInstance(assignMembersModalElement)
        : null;

    const openAssignMembersBtn =
        document.getElementById('openAssignRoleMembersBtn');

    const assignMembersSearch =
        document.getElementById('assignRoleMembersSearch');

    const selectAllAvailableMembers =
        document.getElementById('selectAllAvailableMembers');

    const assignMembersBody =
        document.getElementById('assignRoleMembersBody');

    const assignMembersLoading =
        document.getElementById('assignRoleMembersLoading');

    const assignMembersError =
        document.getElementById('assignRoleMembersError');

    const assignMembersEmpty =
        document.getElementById('assignRoleMembersEmpty');

    const assignMembersSelectedCount =
        document.getElementById('assignRoleMembersSelectedCount');

    const confirmAssignMembersBtn =
        document.getElementById('confirmAssignRoleMembersBtn');

    const assignMembersSubtitle =
        document.getElementById('assignRoleMembersSubtitle');

    const assignMembersLimitNotice =
        document.getElementById('assignRoleMembersLimitNotice');

    let assigningRoleId = null;
    let assigningRoleName = '';
    let availableMembers = [];
    let selectedMemberIds = new Set();
    let assignSearchTimer = null;
    let assignRequestVersion = 0;
    let returnToMembersAfterAssign = false;
    let reloadMembersAfterAssign = false;

    if (!container || !template) return;

    const rolesUrl = container.dataset.rolesUrl;
    function setText(element, value) {
        element.textContent = value ?? '';
    }

    function createRoleCard(role) {
        const fragment = template.content.cloneNode(true);
        const card = fragment.querySelector('.role-card-item');
        card.dataset.roleId = role.id;

        const managementActions = card.querySelector('.role-management-actions');

        const isSystem = role.isSystem === true;
        const isSystemRole = role.name === 'System';

        setText(card.querySelector('.role-name'), role.name);
        setText(card.querySelector('.role-level'), `Level ${role.level}`);

        const typeBadge = card.querySelector('.role-type');
        typeBadge.textContent = isSystem ? 'System' : 'Custom';
        typeBadge.classList.add(
            isSystem ? 'bg-label-warning' : 'bg-label-secondary'
        );

        setText(
            card.querySelector('.role-description'),
            role.description || 'Chưa có mô tả cho vai trò này.'
        );

        setText(card.querySelector('.role-member-count'), role.memberCount);
        setText(card.querySelector('.role-permission-count'), role.permissionCount);

        const editButton = card.querySelector('.role-edit-modal');
        editButton.dataset.roleId = role.id;
        editButton.dataset.roleName = role.name ?? '';
        editButton.dataset.roleLevel = role.level ?? 0;
        editButton.dataset.roleDescription = role.description ?? '';

        const permissionsButton = card.querySelector('.role-permissions-btn');
        permissionsButton.dataset.roleId = role.id;

        const membersButton = card.querySelector('.role-members-btn');
        membersButton.dataset.roleId = role.id;

        const deleteButton = card.querySelector('.role-delete-btn');

        if (deleteButton) {
            deleteButton.dataset.roleId = role.id;
            deleteButton.dataset.roleName = role.name ?? '';
        }

        if (role.isSystem === true && managementActions) {
            managementActions.hidden = true;
        }
        // Chỉ Root được quản lý thành viên của role System.
        if (isSystemRole && !currentUserIsRoot) {
            membersButton.title =
                'Chỉ Root mới có thể quản lý thành viên của role System';
        }

        return card;
    }

    async function loadRoles() {
        try {
            console.log(rolesUrl);
            const response = await fetch(rolesUrl, {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error('Không thể tải danh sách.');
            }

            const roles = await response.json();
            if (!Array.isArray(roles)) {
                throw new Error('Dữ liệu danh sách role không hợp lệ.');
            }
            // Giữ lại card Add New Role.
            const addCard = document.getElementById('addRoleCard');

            // Xóa card role cũ.
            container.querySelectorAll('.role-card-item')
                .forEach(card => card.remove());

            // Tạo lại card theo dữ liệu mới nhất.
            const fragment = document.createDocumentFragment();
            roles.forEach(role => {
                fragment.appendChild(createRoleCard(role));
            });

            if (addCard) {
                container.insertBefore(fragment, addCard);
            } else {
                container.appendChild(fragment);
            }
        } catch (error) {
            console.error(error);
            Toast.error(error)
            // Không xóa dữ liệu đang hiển thị khi request thất bại.
        }
    }

    // Cho các chức năng tạo/sửa/gán quyền/gán user gọi lại.
    window.roleCards = {
        reload: loadRoles
    };

    // Tải dữ liệu ban đầu.
    loadRoles();

    function resetForm() {
        form.reset();
        roleIdInput.value = '';
        roleLevelInput.value = '';
        form.dataset.mode = 'create';
        title.textContent = 'Thêm vai trò mới';
        saveButton.textContent = 'Tạo mới';
    }

    // Mở modal tạo role mới.
    document.addEventListener('click', function (event) {
        const addButton = event.target.closest('.add-new-role');

        if (!addButton) return;
        resetForm();
        roleLevelGroup.classList.add('d-none');
        roleLevelInput.disabled = true;
        modal.show();
    });

    // Mở modal sửa role.
    document.addEventListener('click', function (event) {
        const editButton = event.target.closest('.role-edit-modal');

        if (!editButton) return;

        event.preventDefault();
        const roleId = editButton.dataset.roleId;
        const roleName = editButton.dataset.roleName;
        const roleLevel = editButton.dataset.roleLevel;
        const roleDescription = editButton.dataset.roleDescription || '';

        if (!roleId) {
            console.error('Missing role ID on edit button.');

            Toast.error('Không tìm thấy vai trò hợp lệ')
            return;
        }

        resetForm();

        roleLevelGroup.classList.remove('d-none');
        roleLevelInput.disabled = false;

        form.dataset.mode = 'edit';
        roleIdInput.value = roleId;
        roleNameInput.value = roleName || '';
        roleLevelInput.value = roleLevel ?? 0;
        roleDescriptionInput.value = roleDescription;

        title.textContent = 'Chỉnh sửa';
        saveButton.textContent = 'Xác nhận';

        modal.show();
    });

    // Submit form.
    form.addEventListener('submit', async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) return;

        const isEdit = form.dataset.mode === 'edit';

        const payload = {
            name: roleNameInput.value.trim(),
            description: roleDescriptionInput.value.trim() || null
        };
        if (!payload.name) {
            roleNameInput.focus();
            return;
        }
        console.log(isEdit);
        if (isEdit) {
            const levelValue = roleLevelInput.value.trim();

            if (
                levelValue === '' ||
                !Number.isInteger(Number(levelValue)) ||
                Number(levelValue) < 0
            ) {
                roleLevelInput.focus();
                return;
            }

            payload.id = roleIdInput.value;
            payload.level = Number(levelValue);
        } 

        const url = isEdit ? `${baseUrl}/update/${encodeURIComponent(roleIdInput.value)}` : `${baseUrl}/create`;

        saveButton.disabled = true;
        try {
            const response = await fetch(url, {
                method: isEdit ? 'PUT' : 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                const message = result?.message || result?.title || 'Unable to save role.';

                throw new Error(message);
            }

            if (result?.success === false) {
                throw new Error(
                    result.message || 'Unable to save role.'
                );
            }
            modal.hide();
            Toast.success(
                isEdit ? 'Cập nhật vai trò thành công.' : 'Tạo vai trò thành công.'
            );
            await window.roleCards.reload();
        } catch (error) {
            console.error('Save role error:', error);

            Toast.error('Lưu vai trò không thành công')
        } finally {
            saveButton.disabled = false;
        }
    });

    // Chuẩn bị trạng thái mặc định khi mở trang.
    resetForm();

    container?.addEventListener('click', async function (event) {
        const button = event.target.closest('.role-delete-btn');
        if (!button || button.hidden) return;

        const roleId = button.dataset.roleId;
        const roleName = button.dataset.roleName || 'role';

        if (!roleId) return;

        const result = await Swal.fire({
            icon: 'warning',
            title: 'Xác nhận xóa',
            text: `Bạn có chắc muốn xóa role "${roleName}"?`,
            showCancelButton: true,
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#d33',
            reverseButtons: true
        });

        if (!result.isConfirmed) {
            return;
        }

        button.disabled = true;

        try {
            const response = await fetch(
                `${baseUrl}/delete/${encodeURIComponent(roleId)}`,
                {
                    method: 'DELETE',
                    headers: { Accept: 'application/json' },
                    credentials: 'same-origin'
                }
            );

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                throw new Error(result?.message || 'Không thể xóa.');
            }

            // Thay bằng tên hàm reload role cards đang có trong dự án.
            await loadRoles();

        } catch (error) {
            console.error('Delete role error:', error);
            Toast.error(error.message || 'Đã xảy ra lỗi khi xóa role.');
        } finally {
            button.disabled = false;
        }
    });

    document.addEventListener('click', function (event) {
        const button = event.target.closest('.role-permissions-btn');

        if (!button) return;

        const roleId = button.dataset.roleId;
        const roleCard = button.closest('.role-card-item');
        const roleName = roleCard?.querySelector('.role-name')?.textContent?.trim() || 'Role';
        if (!roleId) {
            console.error('Missing role ID for permissions.');
            Toast.error('Quyền hạn không hợp lệ')
            return;
        }

        openRolePermissions(roleId, roleName);
    });



    async function openRolePermissions(roleId, roleName) {
        if (!permissionsModal) return;

        currentPermissionRoleId = roleId;

        permissionsTitle.textContent = `Permissions - ${roleName}`;
        permissionsSubtitle.textContent = `Role ID: ${roleId}`;

        permissionsHead.replaceChildren();
        permissionsBody.replaceChildren();

        permissionsError.textContent = '';
        permissionsError.classList.add('d-none');
        permissionsLoading.classList.remove('d-none');

        if (saveRolePermissionsBtn) {
            saveRolePermissionsBtn.disabled = true;
        }

        permissionsModal.show();

        try {
            const response = await fetch(
                `${baseUrl}/permission-options?roleId=${encodeURIComponent(roleId)}`,
                {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' },
                    credentials: 'same-origin'
                }
            );

            if (!response.ok) {
                throw new Error(`Không thể tải danh sách quyền.`);
            }

            const data = await response.json();
            renderRolePermissionOptions(data);
        } catch (error) {
            console.error('Load role permissions error:', error);

            Toast.error('Tải danh sách quyền hạn thất bại')
            permissionsError.textContent =
                error.message || 'Không thể tải danh sách quyền.';

            permissionsError.classList.remove('d-none');
        } finally {
            permissionsLoading.classList.add('d-none');

            if (saveRolePermissionsBtn) {
                saveRolePermissionsBtn.disabled = false;
            }
        }
    }


    function renderRolePermissionOptions(data) {
        const modules = [...(data.modules || [])].sort((a, b) => {
            const aIsSystem = String(a.code || '').toLowerCase() === 'system';
            const bIsSystem = String(b.code || '').toLowerCase() === 'system';

            if (aIsSystem && !bIsSystem) return -1;
            if (!aIsSystem && bIsSystem) return 1;

            return 0;
        });
        const actions = data.actions || [];
        const specialPermission = data.specialPermission;

        const assigned = new Set(
            (data.assigned || []).map(item =>
                `${item.moduleId}:${String(item.action).toLowerCase()}`
            )
        );

        // Header giữ nguyên như bảng hiện tại.
        const headerRow = document.createElement('tr');

        const moduleHeader = document.createElement('th');
        moduleHeader.textContent = 'Module';
        headerRow.appendChild(moduleHeader);

        const allHeader = document.createElement('th');
        allHeader.className = 'text-center';
        allHeader.textContent = 'All';
        headerRow.appendChild(allHeader);

        actions.forEach(action => {
            const th = document.createElement('th');
            th.className = 'text-center';
            th.textContent = action.name;
            headerRow.appendChild(th);
        });

        permissionsHead.replaceChildren(headerRow);

        const fragment = document.createDocumentFragment();

        modules.forEach(module => {
            const row = document.createElement('tr');

            // Xác định hàng System có quyền truy cập Admin.
            const isSystemRow =
                (specialPermission &&
                    Number(module.id) === Number(specialPermission.moduleId)) ||
                String(module.code || '').toLowerCase() === 'system';

            const moduleCell = document.createElement('td');

            const moduleName = document.createElement('div');
            moduleName.className = 'fw-semibold';
            moduleName.textContent = module.name;
            moduleCell.appendChild(moduleName);

            if (module.code) {
                const moduleCode = document.createElement('small');
                moduleCode.className = 'text-body-secondary d-block';
                moduleCode.textContent = module.code;
                moduleCell.appendChild(moduleCode);
            }

            row.appendChild(moduleCell);

            // Hàng System: gộp các cột thành vùng hiển thị quyền riêng.
            if (isSystemRow) {
                const specialCell = document.createElement('td');
                specialCell.colSpan = actions.length + 1;
                specialCell.className = 'p-3';

                const permissionPanel = document.createElement('div');
                permissionPanel.className =
                    'd-flex align-items-center justify-content-between gap-3 ' +
                    'border rounded-3 p-3';

                const permissionInfo = document.createElement('div');
                permissionInfo.className =
                    'd-flex align-items-center gap-3 min-w-0';

                const iconWrapper = document.createElement('div');
                iconWrapper.className =
                    'd-flex align-items-center justify-content-center ' +
                    'rounded-3 bg-label-primary flex-shrink-0';
                iconWrapper.style.width = '44px';
                iconWrapper.style.height = '44px';

                const icon = document.createElement('i');
                icon.className = 'ti ti-shield-lock fs-3';
                iconWrapper.appendChild(icon);

                const textWrapper = document.createElement('div');
                textWrapper.className = 'min-w-0';

                const permissionName = document.createElement('div');
                permissionName.className = 'fw-semibold';
                permissionName.textContent =
                    specialPermission?.name ||
                    'Truy cập khu vực quản trị';

                const permissionDescription = document.createElement('small');
                permissionDescription.className =
                    'text-body-secondary d-block text-break';
                permissionDescription.textContent =
                    specialPermission?.code || 'system.admin.access';

                textWrapper.append(permissionName, permissionDescription);
                permissionInfo.append(iconWrapper, textWrapper);

                permissionPanel.appendChild(permissionInfo);

                if (specialPermission) {
                    const checkboxWrapper = document.createElement('div');
                    checkboxWrapper.className =
                        'form-check form-switch mb-0 flex-shrink-0';

                    const checkbox = document.createElement('input');
                    checkbox.type = 'checkbox';
                    checkbox.className =
                        'form-check-input role-permission-checkbox';
                    checkbox.dataset.moduleId = specialPermission.moduleId;
                    checkbox.dataset.action = specialPermission.action;
                    checkbox.dataset.permissionCode = specialPermission.code;

                    checkbox.checked = assigned.has(
                        `${specialPermission.moduleId}:` +
                        String(specialPermission.action).toLowerCase()
                    );

                    checkbox.setAttribute(
                        'aria-label',
                        specialPermission.name ||
                        'Truy cập khu vực quản trị'
                    );

                    checkboxWrapper.appendChild(checkbox);
                    permissionPanel.appendChild(checkboxWrapper);
                } else {
                    const warning = document.createElement('small');
                    warning.className = 'text-warning text-end';
                    warning.textContent =
                        'Chưa cấu hình quyền system.admin.access';
                    permissionPanel.appendChild(warning);
                }

                specialCell.appendChild(permissionPanel);
                row.appendChild(specialCell);
                fragment.appendChild(row);
                return;
            }

            // Các module bình thường: giữ nguyên All và các action.
            const allCell = document.createElement('td');
            allCell.className = 'text-center';

            const allCheckbox = document.createElement('input');
            allCheckbox.type = 'checkbox';
            allCheckbox.className =
                'form-check-input role-permission-all';
            allCheckbox.setAttribute(
                'aria-label',
                `Tất cả quyền - ${module.name}`
            );

            allCell.appendChild(allCheckbox);
            row.appendChild(allCell);

            actions.forEach(action => {
                const cell = document.createElement('td');
                cell.className = 'text-center';

                const checkbox = document.createElement('input');
                checkbox.type = 'checkbox';
                checkbox.className =
                    'form-check-input role-permission-checkbox';

                checkbox.dataset.moduleId = module.id;
                checkbox.dataset.action = action.name;

                checkbox.checked = assigned.has(
                    `${module.id}:${action.name.toLowerCase()}`
                );

                checkbox.setAttribute(
                    'aria-label',
                    `${module.name} - ${action.name}`
                );

                cell.appendChild(checkbox);
                row.appendChild(cell);
            });

            fragment.appendChild(row);
        });

        if (modules.length === 0) {
            const row = document.createElement('tr');
            const cell = document.createElement('td');

            cell.colSpan = actions.length + 2;
            cell.className = 'text-center text-body-secondary py-4';
            cell.textContent = 'Không có module đang hoạt động.';

            row.appendChild(cell);
            fragment.appendChild(row);
        }

        permissionsBody.replaceChildren(fragment);

        // Chỉ đồng bộ All ở những hàng có checkbox All.
        permissionsBody.querySelectorAll('tr').forEach(row => {
            if (row.querySelector('.role-permission-all')) {
                syncPermissionAllCheckbox(row);
            }
        });
    }


    if (saveRolePermissionsBtn) {
        saveRolePermissionsBtn.addEventListener('click', async function () {
            const roleId = currentPermissionRoleId;

            if (!roleId) return;

            const selections = Array.from(
                permissionsBody.querySelectorAll(
                    '.role-permission-checkbox:checked'
                )
            ).map(checkbox => ({
                moduleId: Number(checkbox.dataset.moduleId),
                action: checkbox.dataset.action
            }));

            saveRolePermissionsBtn.disabled = true;

            permissionsError.textContent = '';
            permissionsError.classList.add('d-none');

            try {
                const response = await fetch(
                    `${baseUrl}/permissions/assign`,
                    {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        credentials: 'same-origin',
                        body: JSON.stringify({
                            roleId: currentPermissionRoleId,
                            selections
                        })
                    }
                );

                const result = await response.json().catch(() => null);

                if (!response.ok) {
                    throw new Error(
                        result?.message || 'Không thể lưu quyền.'
                    );
                }
                Toast.success(result?.message ?? "Lưu quyền hạn thành công")
                // Tải lại dữ liệu từ server để xác nhận trạng thái đã lưu.
                const roleName = permissionsTitle.textContent
                    .replace(/^Permissions - /, '');

                // Đếm số quyền đang được chọn của role vừa chỉnh sửa
                const permissionCount = result.permissionCount;

                // Đóng modal sau khi server xác nhận lưu thành công
                permissionsModal.hide();

                // Cập nhật số lượng quyền trên đúng card role
                const roleCard = container.querySelector(
                    `.role-card-item[data-role-id="${CSS.escape(roleId)}"]`
                );

                if (roleCard) {
                    const countElement = roleCard.querySelector('.role-permission-count');

                    if (countElement) {
                        countElement.textContent = permissionCount;
                    }
                }
            } catch (error) {
                console.error('Save role permissions error:', error);

                Toast.error('Lưu quyền hạn thất bại')
                permissionsError.textContent =
                    error.message || 'Đã xảy ra lỗi khi lưu quyền.';

                permissionsError.classList.remove('d-none');
            } finally {
                saveRolePermissionsBtn.disabled = false;
            }
        });
    }
    function syncPermissionAllCheckbox(row) {
        const allCheckbox = row.querySelector('.role-permission-all');

        const actionCheckboxes = Array.from(
            row.querySelectorAll('.role-permission-checkbox')
        );

        if (!allCheckbox || actionCheckboxes.length === 0) {
            if (allCheckbox) {
                allCheckbox.checked = false;
                allCheckbox.indeterminate = false;
            }
            return;
        }

        const checkedCount = actionCheckboxes.filter(
            checkbox => checkbox.checked
        ).length;

        allCheckbox.checked = checkedCount === actionCheckboxes.length;
        allCheckbox.indeterminate =
            checkedCount > 0 && checkedCount < actionCheckboxes.length;
    }

    permissionsBody.addEventListener('change', function (event) {
        const target = event.target;

        // Tích/bỏ All sẽ cập nhật toàn bộ action trong cùng hàng
        if (target.matches('.role-permission-all')) {
            const row = target.closest('tr');

            row.querySelectorAll('.role-permission-checkbox')
                .forEach(checkbox => {
                    checkbox.checked = target.checked;
                });

            target.indeterminate = false;
            return;
        }

        // Khi thay đổi action riêng lẻ, cập nhật trạng thái All
        if (target.matches('.role-permission-checkbox')) {
            syncPermissionAllCheckbox(target.closest('tr'));
        }
    });


    async function openRoleMembers(roleId, roleName) {
        if (!membersModal) return;

        membersTitle.textContent = `Thành viên - ${roleName}`;
        membersSubtitle.textContent = `Role ID: ${roleId}`;

        membersCount.textContent = '0';
        membersBody.replaceChildren();

        membersError.textContent = '';
        membersError.classList.add('d-none');
        membersEmpty.classList.add('d-none');
        membersLoading.classList.remove('d-none');

        membersModal.show();

        try {
            const response = await fetch(
                `${baseUrl}/roles/${encodeURIComponent(roleId)}/members`,
                {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    },
                    credentials: 'same-origin'
                }
            );

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                throw new Error(
                    result?.message || 'Không thể tải danh sách thành viên.'
                );
            }

            membersCount.textContent = result.total ?? result.members.length;

            const fragment = document.createDocumentFragment();

            result.members.forEach(member => {
                const row = document.createElement('tr');

                // Tài khoản
                const accountCell = document.createElement('td');
                const accountWrapper = document.createElement('div');
                accountWrapper.className = 'd-flex align-items-center gap-3';

                const avatar = document.createElement('img');
                avatar.className = 'rounded-circle';
                avatar.width = 40;
                avatar.height = 40;
                avatar.alt = '';
                avatar.src = member.avatar || '/images/Default/default-user.png';

                avatar.onerror = function () {
                    this.onerror = null;
                    this.src = '/images/Default/default-user.png';
                };

                const accountInfo = document.createElement('div');

                const userName = document.createElement('div');
                userName.className = 'fw-semibold';
                userName.textContent = member.userName || '(Chưa có username)';

                const email = document.createElement('small');
                email.className = 'text-body-secondary';
                email.textContent = member.email || 'Chưa có email';

                accountInfo.append(userName, email);
                accountWrapper.append(avatar, accountInfo);
                accountCell.appendChild(accountWrapper);

                // Trạng thái
                const statusCell = document.createElement('td');
                const statusBadge = document.createElement('span');

                if (member.isLocked) {
                    statusBadge.className = 'badge bg-label-danger';
                    statusBadge.textContent = 'Đã khóa';
                } else if (member.isActive) {
                    statusBadge.className = 'badge bg-label-success';
                    statusBadge.textContent = 'Hoạt động';
                } else {
                    statusBadge.className = 'badge bg-label-secondary';
                    statusBadge.textContent = 'Ngừng hoạt động';
                }

                statusCell.appendChild(statusBadge);

                // Đăng nhập gần nhất
                const lastLoginCell = document.createElement('td');
                lastLoginCell.textContent = member.lastLogin
                    ? new Date(member.lastLogin).toLocaleString('vi-VN')
                    : 'Chưa đăng nhập';

                // Thao tác
                const actionCell = document.createElement('td');
                actionCell.className = 'text-end';

                const removeBtn = document.createElement('button');
                removeBtn.type = 'button';
                removeBtn.className = 'btn btn-sm btn-outline-danger btn-remove-role-member';
                removeBtn.innerHTML = '<i class="ti ti-user-minus me-1"></i> Remove';
                removeBtn.dataset.userId = member.id;
                removeBtn.dataset.userName = member.userName || member.email || '';
                removeBtn.setAttribute('aria-label', `Gỡ ${member.userName || member.email || 'thành viên'} khỏi role`);

                actionCell.appendChild(removeBtn);

                row.append(accountCell, statusCell, lastLoginCell, actionCell);
                fragment.appendChild(row);
            });

            membersBody.replaceChildren(fragment);

            membersEmpty.classList.toggle(
                'd-none',
                result.members.length > 0
            );
        } catch (error) {
            console.error('Load role members error:', error);

            Toast.error('Tải danh sách thành viên không thành công')
            membersError.textContent = error.message || 'Đã xảy ra lỗi khi tải thành viên.';

            membersError.classList.remove('d-none');
        } finally {
            membersLoading.classList.add('d-none');
        }
    }

    document.addEventListener('click', function (event) {
        const button = event.target.closest('.role-members-btn');

        if (!button) return;

        const roleId = button.dataset.roleId;
        const roleCard = button.closest('.role-card-item');
        const roleName = roleCard?.querySelector('.role-name')?.textContent?.trim() || 'Role';

        if (!roleId) {
            console.error('Missing role ID for members.');
            Toast.error('Thành viên không hợp lệ')
            return;
        }
        assigningRoleId = roleId;
        assigningRoleName = roleName;

        openRoleMembers(roleId, roleName);
    });


    //ASSIGN ROLE MEMBER
    function updateAssignMembersSelection() {
        if (assignMembersSelectedCount) {
            assignMembersSelectedCount.textContent =
                selectedMemberIds.size;
        }

        if (confirmAssignMembersBtn) {
            confirmAssignMembersBtn.disabled =
                selectedMemberIds.size === 0;
        }

        if (selectAllAvailableMembers) {
            const visibleIds = availableMembers.map(x => x.id);
            const selectedVisibleCount = visibleIds.filter(
                id => selectedMemberIds.has(id)
            ).length;

            selectAllAvailableMembers.checked =
                visibleIds.length > 0 &&
                selectedVisibleCount === visibleIds.length;

            selectAllAvailableMembers.indeterminate =
                selectedVisibleCount > 0 &&
                selectedVisibleCount < visibleIds.length;

            selectAllAvailableMembers.disabled =
                visibleIds.length === 0;
        }
    }

    function showAssignMembersError(message) {
        if (!assignMembersError) return;

        assignMembersError.textContent =
            message || 'Không thể thực hiện thao tác. Vui lòng thử lại.';

        assignMembersError.classList.remove('d-none');
    }

    function renderAvailableMembers() {
        if (!assignMembersBody) return;

        assignMembersBody.replaceChildren();

        availableMembers.forEach(user => {
            const tr = document.createElement('tr');

            const checkboxCell = document.createElement('td');
            const checkbox = document.createElement('input');

            checkbox.type = 'checkbox';
            checkbox.className = 'form-check-input assign-member-checkbox';
            checkbox.value = user.id;
            checkbox.checked = selectedMemberIds.has(user.id);
            checkbox.setAttribute(
                'aria-label',
                `Chọn tài khoản ${user.userName || user.email || ''}`
            );

            checkboxCell.appendChild(checkbox);

            const accountCell = document.createElement('td');
            const accountWrapper = document.createElement('div');
            accountWrapper.className = 'd-flex align-items-center gap-3';

            const avatar = document.createElement('img');
            avatar.src = user.avatar || '/images/Default/default-user.png';
            avatar.alt = '';
            avatar.width = 38;
            avatar.height = 38;
            avatar.className = 'rounded-circle object-fit-cover';
            avatar.addEventListener('error', function () {
                this.onerror = null;
                this.src = '/images/Default/default-user.png';
            });

            const accountInfo = document.createElement('div');
            accountInfo.className = 'd-flex flex-column';

            const userName = document.createElement('span');
            userName.className = 'fw-medium';
            userName.textContent = user.userName || '(Chưa có username)';

            const email = document.createElement('small');
            email.className = 'text-body-secondary';
            email.textContent = user.email || 'Chưa có email';

            accountInfo.append(userName, email);
            accountWrapper.append(avatar, accountInfo);
            accountCell.appendChild(accountWrapper);

            const statusCell = document.createElement('td');
            const statusBadge = document.createElement('span');

            statusBadge.className = user.isLocked
                ? 'badge bg-label-danger'
                : user.isActive
                    ? 'badge bg-label-success'
                    : 'badge bg-label-secondary';

            statusBadge.textContent = user.isLocked
                ? 'Đã khóa'
                : user.isActive
                    ? 'Hoạt động'
                    : 'Không hoạt động';

            statusCell.appendChild(statusBadge);
            tr.append(checkboxCell, accountCell, statusCell);
            assignMembersBody.appendChild(tr);
        });

        if (assignMembersEmpty) {
            assignMembersEmpty.classList.toggle(
                'd-none',
                availableMembers.length > 0
            );
        }

        if (assignMembersLimitNotice) {
            assignMembersLimitNotice.classList.toggle(
                'd-none',
                availableMembers.length < 50
            );
        }

        updateAssignMembersSelection();
    }

    async function loadAvailableMembers(keyword = '') {
        if (!assigningRoleId || !assignMembersBody) return;

        const requestVersion = ++assignRequestVersion;

        assignMembersError?.classList.add('d-none');
        assignMembersEmpty?.classList.add('d-none');
        assignMembersLoading?.classList.remove('d-none');

        if (assignMembersLimitNotice) {
            assignMembersLimitNotice.classList.add('d-none');
        }

        assignMembersBody.replaceChildren();

        if (selectAllAvailableMembers) {
            selectAllAvailableMembers.checked = false;
            selectAllAvailableMembers.indeterminate = false;
            selectAllAvailableMembers.disabled = true;
        }

        try {
            const params = new URLSearchParams();

            if (keyword.trim()) {
                params.set('keyword', keyword.trim());
            }

            const response = await fetch(
                `${baseUrl}/roles/${encodeURIComponent(assigningRoleId)}/available-members?${params}`,
                {
                    method: 'GET',
                    headers: {
                        Accept: 'application/json'
                    },
                    credentials: 'same-origin'
                }
            );

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                throw new Error(
                    result?.message || 'Không tải được danh sách tài khoản.'
                );
            }

            // Bỏ qua kết quả của lần tìm kiếm cũ.
            if (requestVersion !== assignRequestVersion) return;

            availableMembers = Array.isArray(result) ? result : [];
            renderAvailableMembers();
        } catch (error) {
            if (requestVersion !== assignRequestVersion) return;

            showAssignMembersError(error.message);
        } finally {
            if (requestVersion === assignRequestVersion) {
                assignMembersLoading?.classList.add('d-none');
            }
        }
    }

    function openAssignRoleMembers() {
        if (!assignMembersModal || !assigningRoleId) return;

        selectedMemberIds.clear();
        availableMembers = [];

        if (assignMembersSearch) {
            assignMembersSearch.value = '';
        }

        if (assignMembersSubtitle) {
            assignMembersSubtitle.textContent = `Role: ${assigningRoleName}`;
        }

        assignMembersError?.classList.add('d-none');

        updateAssignMembersSelection();

        // Đóng modal thành viên trước để tránh xung đột backdrop.
        returnToMembersAfterAssign = true;
        reloadMembersAfterAssign = false;

        if (membersModalElement?.classList.contains('show')) {
            membersModalElement.addEventListener('hidden.bs.modal',
                function handleMembersHidden() {
                    membersModalElement.removeEventListener(
                        'hidden.bs.modal',
                        handleMembersHidden
                    );

                    assignMembersModal.show();
                    loadAvailableMembers();
                }
            );

            membersModal.hide();
        } else {
            assignMembersModal.show();
            loadAvailableMembers();
        }
    }

    // Mở modal gán từ modal thành viên.
    openAssignMembersBtn?.addEventListener('click', function () {
        openAssignRoleMembers();
    });

    // Tìm kiếm có debounce 300ms.
    assignMembersSearch?.addEventListener('input', function () {
        clearTimeout(assignSearchTimer);

        assignSearchTimer = setTimeout(() => {
            loadAvailableMembers(assignMembersSearch.value);
        }, 300);
    });

    // Chọn / bỏ chọn một tài khoản.
    assignMembersBody?.addEventListener('change', function (event) {
        const checkbox = event.target.closest('.assign-member-checkbox');

        if (!checkbox) return;

        if (checkbox.checked) {
            selectedMemberIds.add(checkbox.value);
        } else {
            selectedMemberIds.delete(checkbox.value);
        }

        updateAssignMembersSelection();
    });

    // Chọn tất cả kết quả đang hiển thị.
    selectAllAvailableMembers?.addEventListener('change', function () {
        availableMembers.forEach(user => {
            if (this.checked) {
                selectedMemberIds.add(user.id);
            } else {
                selectedMemberIds.delete(user.id);
            }
        });

        renderAvailableMembers();
    });

    // Xác nhận gán tài khoản.
    confirmAssignMembersBtn?.addEventListener('click', async function () {
        if (!assigningRoleId || selectedMemberIds.size === 0) return;

        const button = this;
        const originalHtml = button.innerHTML;

        button.disabled = true;
        button.innerHTML =
            '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang gán...';

        assignMembersError?.classList.add('d-none');

        try {
            const response = await fetch(
                `${baseUrl}/roles/assign-members`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        Accept: 'application/json'
                    },
                    credentials: 'same-origin',
                    body: JSON.stringify({
                        roleId: assigningRoleId,
                        userIds: [...selectedMemberIds]
                    })
                }
            );

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                console.log(result);
                throw new Error(result?.message || 'Không thể gán tài khoản.');
            }

            reloadMembersAfterAssign = true;
            selectedMemberIds.clear();

            assignMembersModal.hide();

            // Cập nhật số thành viên trên role card.
            const roleCard = container?.querySelector(
                `.role-card-item[data-role-id="${CSS.escape(assigningRoleId)}"]`
            );

            const countElement =
                roleCard?.querySelector('.role-member-count');

            if (countElement) {
                const currentCount = Number(countElement.textContent) || 0;
                countElement.textContent =
                    currentCount + (Number(result?.assignedCount) || 0);
            }
        } catch (error) {
            showAssignMembersError(error.message);
        } finally {
            button.innerHTML = originalHtml;
            updateAssignMembersSelection();
        }
    });

    // Trở lại modal thành viên sau khi đóng modal gán.
    assignMembersModalElement?.addEventListener(
        'hidden.bs.modal',
        function () {
            if (!returnToMembersAfterAssign) return;

            returnToMembersAfterAssign = false;

            if (membersModal) {
                membersModal.show();
            }

            if (reloadMembersAfterAssign) {
                reloadMembersAfterAssign = false;

                if (assigningRoleId) {
                    openRoleMembers(assigningRoleId, assigningRoleName);
                }
            }
        }
    );

    membersBody?.addEventListener('click', async function (event) {
        const button = event.target.closest('.btn-remove-role-member');

        if (!button || !assigningRoleId || button.disabled) return;

        const roleId = assigningRoleId;
        const userId = button.dataset.userId;
        const userName = button.dataset.userName || 'thành viên';
        const roleName = assigningRoleName;

        if (!userId) return;

        const result = await Swal.fire({
            icon: 'warning',
            title: 'Xác nhận gỡ',
            text: `Bạn có chắc muốn gỡ "${userName}" khỏi role "${roleName}"?`,
            showCancelButton: true,
            confirmButtonText: 'Gỡ',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#d33',
            reverseButtons: true
        });

        if (!result.isConfirmed) {
            return;
        }

        const originalHtml = button.innerHTML;
        button.disabled = true;
        button.innerHTML =
            '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';

        try {
            const response = await fetch(
                `${baseUrl}/roles/${encodeURIComponent(roleId)}/members/${encodeURIComponent(userId)}`,
                {
                    method: 'DELETE',
                    headers: {
                        Accept: 'application/json'
                    },
                    credentials: 'same-origin'
                }
            );

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                let message = result?.message;

                if (!message && response.status === 403) {
                    message = 'Bạn không có quyền gỡ thành viên này.';
                } else if (!message && response.status === 404) {
                    message = 'Role hoặc thành viên không tồn tại.';
                } else if (!message && response.status === 400) {
                    message = 'Không thể gỡ thành viên này khỏi role.';
                }

                throw new Error(message || 'Không thể gỡ thành viên.');
            }

            Toast.success(result?.message || 'Gỡ thành viên thành công.');

            // Tải lại danh sách thành viên.
            await openRoleMembers(roleId, roleName);

            // Tải lại số lượng thành viên từ server, tránh lệch số liệu.
            await window.roleCards.reload();

        } catch (error) {
            console.error('Remove role member error:', error);
            Toast.error(error.message || 'Đã xảy ra lỗi khi gỡ thành viên.');
        } finally {
            // Nếu dòng đã bị thay thế sau khi tải lại, việc khôi phục
            // nút cũ không ảnh hưởng đến giao diện.
            button.disabled = false;
            button.innerHTML = originalHtml;
        }
    });

});