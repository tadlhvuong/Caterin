document.addEventListener("DOMContentLoaded", async function () {
    "use strict";

    // =========================================================
    // UPDATE PRODUCT
    // =========================================================

    const productForm = document.querySelector("#productForm");

    if (!productForm) {
        return;
    }

    // =========================================================
    // PRODUCT ID
    // =========================================================

    /*
     * Ưu tiên lấy từ:
     *
     * <form id="updateProduct" data-product-id="123">
     *
     * hoặc:
     *
     * <input type="hidden" id="ProductId" ...>
     *
     * hoặc:
     *
     * URL /admin/product/edit/123
     */

    const productId =
        productForm.dataset.productId ||
        document.querySelector("#Id")?.value ||
        getProductIdFromUrl();

    if (!productId) {
        Toast.error("Không xác định được sản phẩm cần cập nhật.");

        return;
    }
    // =========================================================
    // ELEMENTS
    // =========================================================

    const nameInput = $("#Name");

    const slugInput = $("#Slug");

    const skuInput = $("#SKU");


    // =========================================================
    // QUILL
    // =========================================================

    const commentEditor = document.querySelector(".comment-editor");

    let quill = null;

    if (commentEditor && typeof Quill !== "undefined") {
        quill = new Quill(".comment-editor", {
            modules: {
                toolbar: ".comment-toolbar",
            },

            placeholder: "Product Description",

            theme: "snow",
        });
    }
    // =========================================================
    // LOAD PRODUCT
    // =========================================================

    let product = null;

    try {
        product = await loadProduct(productId);
        console.log("PRODUCT:", product);
        console.log("PRODUCT TAGS:", product.tags);
    } catch (error) {
        console.error("Load product error:", error);

        Toast.error(
            error.message ||
            "Không thể tải thông tin sản phẩm.",
        );

        return;
    }

    let editor;
    try {
        console.log('update: ' + productId);
        console.log(product);
        editor = await ProductEditor.init({
            mode: "update",
            form: productForm,
            productId,
            product,
        });

        // =========================================================
        // SET BASIC PRODUCT DATA
        // =========================================================

        setBasicProductData(product);

        // =========================================================
        // SET DESCRIPTION
        // =========================================================

        setDescription(product);

        // =========================================================
        // INITIALIZE PRODUCT EDITOR
        // =========================================================

    } catch (error) {
        console.error(
            "ProductEditor initialization error:",
            error,
        );

        Toast.error(
            "Không thể khởi tạo trình chỉnh sửa sản phẩm.",
        );

        return;
    }

    // =========================================================
    // SUBMIT
    // =========================================================

    productForm.addEventListener(
        "submit",
        async function (event) {
            event.preventDefault();

            const submitter = event.submitter;

            const submitAction =
                submitter?.dataset.action ||
                "draft";

            await submitUpdate(submitAction);
        },
    );

    // =========================================================
    // SUBMIT UPDATE
    // =========================================================

    async function submitUpdate(submitAction) {
        clearValidationErrors(productForm);

        // =====================================================
        // GET EDITOR STATE
        // =====================================================

        const state = editor.getState();
        console.log("=== UPDATE STATE ===");
        console.log("variantImageGroups:", state.variantImageGroups);
        console.log("variantFilePonds:", state.variantFilePonds);
        console.log("variants:", state.variants);
        console.log("tags:", state.tags);
        const productImages = state.productImages || [];
        const productTags = state.tags || [];
        const options = state.options || [];
        const variants = state.variants || [];

        const variantImageGroups = state.variantImageGroups || new Map();

        // =====================================================
        // BASIC DATA
        // =====================================================

        const name =
            nameInput.val()?.trim();

        const slug =
            slugInput.val()?.trim();

        const sku =
            skuInput.val()?.trim();


        // =====================================================
        // VALIDATE PRODUCT IMAGES
        // =====================================================

        if (productImages.length === 0) {
            Toast.warning("Vui lòng chọn ảnh.");

            handleValidationErrors(
                {
                    responseJSON: {
                        errors: {
                            Images: [
                                "Thêm ít nhất 1 ảnh sản phẩm.",
                            ],
                        },
                    },
                },
                productForm,
            );

            return;
        }

        // =====================================================
        // VALIDATE NAME
        // =====================================================

        if (!name) {
            Toast.warning("Tên không được để trống.");

            handleValidationErrors(
                {
                    responseJSON: {
                        errors: {
                            Name: [
                                "Tên không được để trống.",
                            ],
                        },
                    },
                },
                productForm,
            );

            $("#Name").trigger("focus");
            return;
        }

        // =====================================================
        // VALIDATE SKU
        // =====================================================

        if (!sku) {
            Toast.warning("SKU không được để trống.");

            handleValidationErrors(
                {
                    responseJSON: {
                        errors: {
                            SKU: [
                                "SKU không được để trống.",
                            ],
                        },
                    },
                },
                productForm,
            );

            $("#SKU").trigger("focus");
            return;
        }

        // =====================================================
        // VALIDATE SLUG
        // =====================================================

        if (!slug) {
            Toast.warning(
                "Đường dẫn không được để trống.",
            );

            handleValidationErrors(
                {
                    responseJSON: {
                        errors: {
                            Slug: [
                                "Đường dẫn không được để trống.",
                            ],
                        },
                    },
                },
                productForm,
            );

            $("#Slug").trigger("focus");
            return;
        }

        // =====================================================
        // VALIDATE CATEGORY
        // =====================================================

        const categoryId = $("#CategoryId").val();

        if (
            !categoryId ||
            categoryId === "0" ||
            categoryId === 0
        ) {
            Toast.warning(
                "Vui lòng chọn danh mục sản phẩm.",
            );

            handleValidationErrors(
                {
                    responseJSON: {
                        errors: {
                            CategoryId: [
                                "Vui lòng chọn danh mục sản phẩm.",
                            ],
                        },
                    },
                },
                productForm,
            );
            $("#CategoryId").trigger("focus");
            return false;
        }

        // =====================================================
        // VALIDATE PRICE / STOCK / VARIANTS
        // =====================================================

        if (variants.length === 0) {
            // =================================================
            // PRODUCT KHÔNG CÓ VARIANT
            // =================================================

            const price = $("#Price").val();
            const stock = $("#Stock").val();

            // =================================================
            // PRICE
            // =================================================

            if (
                price === null ||
                price === undefined ||
                price.toString().trim() === ""
            ) {
                Toast.warning(
                    "Giá sản phẩm không được để trống.",
                );

                handleValidationErrors(
                    {
                        responseJSON: {
                            errors: {
                                Price: [
                                    "Giá sản phẩm không được để trống.",
                                ],
                            },
                        },
                    },
                    productForm,
                );

                $("#Price").trigger("focus");

                return;
            }

            // =================================================
            // STOCK
            // =================================================

            if (
                stock === null ||
                stock === undefined ||
                stock.toString().trim() === ""
            ) {
                Toast.warning(
                    "Số lượng tồn kho không được để trống.",
                );

                handleValidationErrors(
                    {
                        responseJSON: {
                            errors: {
                                Stock: [
                                    "Số lượng tồn kho không được để trống.",
                                ],
                            },
                        },
                    },
                    productForm,
                );

                $("#Stock").trigger("focus");

                return;
            }
        } else {
            // =================================================
            // PRODUCT CÓ VARIANT
            // =================================================

            const invalidVariant = validateVariants(
                variants,
                productForm,
            );

            if (invalidVariant) {
                Toast.warning(
                    "Vui lòng nhập đầy đủ SKU và giá cho các variant.",
                );
                return;
            }
        }
        // =========================================================
        // VALIDATE VARIANTS
        // =========================================================

        function validateVariants(
            variants,
            form,
        ) {
            for (
                let index = 0;
                index < variants.length;
                index++
            ) {
                const variant = variants[index];

                if (!variant) {
                    continue;
                }

                const variantNumber = index + 1;

                // =====================================================
                // FIND VARIANT ROW
                // =====================================================

                const row = findVariantTableRow(
                    variant.key,
                );

                // =====================================================
                // IMAGE
                // =====================================================

                const image = variant.image;

                const hasExistingImage = Boolean(
                    image?.id || image?.serverId,
                );
                const hasNewImage = Boolean(
                    image?.file,
                );
                if (!hasExistingImage && !hasNewImage) {
                    Toast.warning(
                        `Variant #${variantNumber}: Vui lòng tải lên ít nhất 1 ảnh.`,
                    );

                    focusVariantImage(row);

                    return true;
                }

                // =====================================================
                // PRICE
                // =====================================================

                if (
                    variant.price === null ||
                    variant.price === undefined ||
                    String(variant.price).trim() === ""
                ) {
                    Toast.warning(
                        `Variant #${variantNumber}: Giá không được để trống.`,
                    );

                    handleValidationErrors(
                        {
                            responseJSON: {
                                errors: {
                                    [`Variants[${index}].Price`]: [
                                        "Giá variant không được để trống.",
                                    ],
                                },
                            },
                        },
                        form,
                    );

                    focusVariantInput(row, ".variant-price");

                    return true;
                }

                // =====================================================
                // STOCK
                // =====================================================

                if (
                    variant.stock === null ||
                    variant.stock === undefined ||
                    String(variant.stock).trim() === ""
                ) {
                    Toast.warning(
                        `Variant #${variantNumber}: Số lượng tồn kho không được để trống.`,
                    );

                    handleValidationErrors(
                        {
                            responseJSON: {
                                errors: {
                                    [`Variants[${index}].Stock`]: [
                                        "Số lượng tồn kho không được để trống.",
                                    ],
                                },
                            },
                        },
                        form,
                    );

                    focusVariantInput(
                        row,
                        ".variant-stock",
                    );

                    return true;
                }

                // =====================================================
                // SKU
                // =====================================================

                if (
                    !variant.sku ||
                    variant.sku.trim() === ""
                ) {
                    Toast.warning(
                        `Variant #${variantNumber}: SKU không được để trống.`,
                    );

                    handleValidationErrors(
                        {
                            responseJSON: {
                                errors: {
                                    [`Variants[${index}].SKU`]: [
                                        "SKU variant không được để trống.",
                                    ],
                                },
                            },
                        },
                        form,
                    );

                    focusVariantInput(
                        row,
                        ".variant-sku",
                    );

                    return true;
                }
            }

            return false;
        }
        // =========================================================
        // FIND VARIANT TABLE ROW
        // =========================================================

        function findVariantTableRow(
            variantKey,
        ) {
            if (!variantKey) {
                return null;
            }

            const escapedKey =
                typeof CSS !== "undefined" &&
                    CSS.escape
                    ? CSS.escape(variantKey)
                    : variantKey.replace(
                        /"/g,
                        '\\"',
                    );

            const input = document.querySelector(
                `.variant-price[data-variant-key="${escapedKey}"]`,
            );

            return input?.closest("tr") || null;
        }
        // =========================================================
        // FIND VARIANT TABLE ROW
        // =========================================================

        function findVariantTableRow(
            variantKey,
        ) {
            if (!variantKey) {
                return null;
            }

            const inputs = document.querySelectorAll(
                "[data-variant-key]",
            );

            for (const input of inputs) {
                if (
                    input.getAttribute(
                        "data-variant-key",
                    ) === variantKey
                ) {
                    return input.closest("tr");
                }
            }

            return null;
        }
        // =========================================================
        // FOCUS VARIANT INPUT
        // =========================================================

        function focusVariantInput(
            row,
            selector,
        ) {
            if (!row) {
                return;
            }

            const input =
                row.querySelector(selector);

            if (!input) {
                return;
            }

            input.classList.add(
                "is-invalid",
            );

            input.focus();

            input.scrollIntoView({
                behavior: "smooth",
                block: "center",
            });

            setTimeout(function () {
                input.classList.remove(
                    "is-invalid",
                );
            }, 3000);
        }
        // =========================================================
        // FOCUS VARIANT IMAGE
        // =========================================================

        function focusVariantImage(row) {
            if (!row) {
                return;
            }

            const container =
                row.querySelector(
                    ".variant-filepond-container",
                );

            if (!container) {
                return;
            }

            container.classList.add(
                "border",
                "border-danger",
            );

            container.scrollIntoView({
                behavior: "smooth",
                block: "center",
            });

            setTimeout(function () {
                container.classList.remove(
                    "border",
                    "border-danger",
                );
            }, 3000);
        }
        // =====================================================
        // BUILD FORM DATA
        // =====================================================
        console.log(productImages);
        const formData = buildFormData({
            options,
            variants,
            productImages,
            productTags,
            variantImageGroups,
            submitAction,
        });

        // =====================================================
        // DEBUG
        // =====================================================

        console.log(
            "========== UPDATE PRODUCT SUBMIT ==========",
        );

        for (const [key, value] of formData.entries()) {
            console.log(key, value);
        }

        // =====================================================
        // SEND
        // =====================================================

        await sendUpdateRequest(formData);
    }

    // =========================================================
    // BUILD FORM DATA
    // =========================================================
    function buildFormData({
        options,
        variants,
        productImages,
        productTags,
        variantImageGroups,
        submitAction,
    }) {
        const formData =
            new FormData(productForm);

        // =====================================================
        // PRODUCT ID
        // =====================================================

        /*
         * Đảm bảo ProductId luôn được gửi.
         */

        if (!formData.get("Id")) {
            formData.set(
                "Id",
                String(productId),
            );
        }

        // =====================================================
        // PRODUCT IMAGES
        // =====================================================

        /*
         * ProductEditor phải trả về:
         *
         * {
         *     id,
         *     file,
         *     displayOrder,
         *     isPrimary,
         *     isExisting
         * }
         *
         * Existing image:
         *
         *     không append File nếu không có file mới.
         *
         * New image:
         *
         *     append File.
         *
         * Đây là điểm quan trọng của UPDATE.
         */

        removeFormDataKeys(
            formData,
            [
                "Images",
                "ProductImages",
            ],
        );

        productImages.forEach(function (image, index) {
            if (!image) {
                return;
            }

            console.log(
                `ProductImage[${index}]`,
                image,
            );

            // =================================================
            // EXISTING IMAGE
            // =================================================

            if (image.id != null) {
                formData.append(
                    `ProductImages[${index}].Id`,
                    String(image.id),
                );
            }

            // =================================================
            // NEW IMAGE
            // =================================================

            if (image.file instanceof File) {
                formData.append(
                    `ProductImages[${index}].File`,
                    image.file,
                    image.file.name,
                );
            }

            // =================================================
            // DISPLAY ORDER
            // =================================================

            formData.append(
                `ProductImages[${index}].DisplayOrder`,
                String(
                    image.displayOrder ?? index,
                ),
            );

            // =================================================
            // PRIMARY
            // =================================================

            formData.append(
                `ProductImages[${index}].IsPrimary`,
                String(
                    image.isPrimary ?? index === 0,
                ),
            );
        });

        
        // =====================================================
        // Update Tags
        // =====================================================
        console.log('updatetags');
        console.log(productTags);
        const tagValues = (productTags || [])
            .map(tag => typeof tag === "string" ? tag : tag.value)
            .map(tag => String(tag).trim())
            .filter(Boolean);
        formData.set(
            "Tags",
            JSON.stringify(tagValues),
        );

        // =====================================================
        // OPTIONS
        // =====================================================

        removeFormDataKeysByPrefix(
            formData,
            "Options[",
        );

        formData.set(
            "Options",
            JSON.stringify(options),
        );

        // =====================================================
        // VARIANTS
        // =====================================================

        /*
         * Không gửi toàn bộ internal state.
         *
         * Chỉ gửi data mà backend cần.
         */

        formData.set(
            "Variants",
            JSON.stringify(
                variants.map(
                    function (variant) {
                        return {
                            id:
                                variant.id ??
                                null,

                            options:
                                variant.options ||
                                {},

                            price:
                                variant.price !==
                                    "" &&
                                    variant.price !==
                                    null &&
                                    variant.price !==
                                    undefined
                                    ? Number(
                                        variant.price,
                                    )
                                    : null,

                            stock:
                                variant.stock !==
                                    "" &&
                                    variant.stock !==
                                    null &&
                                    variant.stock !==
                                    undefined
                                    ? Number(
                                        variant.stock,
                                    )
                                    : 0,

                            sku:
                                variant.sku?.trim() ||
                                null,

                            isDefault:
                                Boolean(
                                    variant.isDefault,
                                ),

                            isActive:
                                variant.isActive !==
                                    undefined
                                    ? Boolean(
                                        variant.isActive,
                                    )
                                    : true,
                        };
                    },
                ),
            ),
        );

        // =====================================================
        // DESCRIPTION
        // =====================================================

        function getQuillHtml(quill) {
            if (!quill) {
                return '';
            }

            if (quill.getText().trim() === '') {
                return '';
            }

            return quill.root.innerHTML;
        }
        if (quill) {
            const description = getQuillHtml(quill);

            formData.set("Description", description);
        }

        // =====================================================
        // VARIANT IMAGES
        // =====================================================

        removeFormDataKeysByPrefix(
            formData,
            "VariantImages[",
        );

        let imageIndex = 0;
        console.log('VariantImages');
        console.log(variantImageGroups);
        variantImageGroups.forEach(function (group, key) {
            const image = group?.image;

            if (!image) {
                return;
            }

            // ID ảnh cũ
            if (image.id != null) {
                formData.append(
                    `VariantImages[${imageIndex}].Id`,
                    String(image.id),
                );
            }

            // Key của variant
            formData.append(
                `VariantImages[${imageIndex}].Key`,
                key,
            );

            // Chỉ upload nếu là ảnh mới
            if (image.file instanceof File) {
                formData.append(
                    `VariantImages[${imageIndex}].File`,
                    image.file,
                    image.file.name,
                );
            }

            imageIndex++;
        });

        // =====================================================
        // ACTION
        // =====================================================

        formData.set(
            "Action",
            submitAction,
        );

        return formData;
    }

    // =========================================================
    // SEND UPDATE REQUEST
    // =========================================================

    async function sendUpdateRequest(formData) {
        /*
         * Lấy endpoint trực tiếp từ:
         *
         * <form action="...">
         *
         * Như vậy JS không hard-code URL.
         */

        const url =
            productForm.action;
        console.log("FORM ACTION:", productForm.action);
        console.log("FORM METHOD:", productForm.method);
        console.log("REQUEST URL:", url);
        try {
            const response =
                await $.ajax({
                    url: url,

                    type: "POST",

                    data: formData,

                    processData: false,

                    contentType: false,

                    beforeSend:
                        function () {
                            setSubmitButtonsDisabled(
                                true,
                            );
                        },
                });

            // =================================================
            // SUCCESS
            // =================================================

            if (response.success) {
                /*
                 * Nếu backend trả redirectUrl
                 * thì chuyển trang.
                 */

                if (response.redirectUrl) {
                    window.location.href = response.redirectUrl;

                    return;
                }

                Toast.success(
                    response.message ||
                    "Cập nhật sản phẩm thành công.",
                );

                return;
            }
            // =================================================
            // BUSINESS ERROR
            // =================================================
            console.log( " BUSINESS ERROR"   );
            Toast.error(
                response.message ||
                "Không thể cập nhật sản phẩm.",
            );
        } catch (xhr) {
            console.error(
                "Update product error:",
                xhr,
            );

            // =================================================
            // VALIDATION
            // =================================================

            if (xhr?.responseJSON?.errors) {
                window.handleValidationErrors(xhr, productForm);
                return;
            }

            // =================================================
            // GENERAL ERROR
            // =================================================

            Toast.error(xhr?.responseJSON?.message || "Không thể cập nhật sản phẩm.");
        } finally {
            setSubmitButtonsDisabled(false);
        }
    }

    // =========================================================
    // SET BASIC PRODUCT DATA
    // =========================================================

    function setBasicProductData(product) {
        if (!product) {
            return;
        }

        /*
         * Chỉ set nếu backend trả property.
         */

        if (
            product.name !==
            undefined
        ) {
            nameInput.val(
                product.name,
            );
        }

        if (
            product.slug !==
            undefined
        ) {
            slugInput.val(
                product.slug,
            );
        }

        if (
            product.sku !==
            undefined
        ) {
            skuInput.val(
                product.sku,
            );
        }

        if (
            product.shortDescription !==
            undefined
        ) {
            $("#ShortDescription").val(
                product.shortDescription,
            );
        }

        if (
            product.categoryId !==
            undefined &&
            product.categoryId !==
            null
        ) {
            console.log(product.categoryId);
            $("#CategoryId")
                .val(String(product.categoryId))
                .trigger("change");
        }

        /*
         * Các field khác của Product
         * có thể để ProductEditor hoặc
         * HTML form tự xử lý.
         */
    }

    // =========================================================
    // SET DESCRIPTION
    // =========================================================

    function setDescription(product) {
        if (!product) {
            return;
        }

        if (
            !product.description
        ) {
            return;
        }

        /*
         * Quill chưa khởi tạo thì
         * set trực tiếp HTML.
         */

        if (quill) {
            quill.root.innerHTML =
                product.description;

            return;
        }

        if (commentEditor) {
            commentEditor.innerHTML =
                product.description;
        }
    }

    // =========================================================
    // LOAD PRODUCT
    // =========================================================

    async function loadProduct(id) {
        /*
         * Endpoint nên trả toàn bộ data cần cho editor:
         *
         * {
         *     id,
         *     name,
         *     sku,
         *     slug,
         *     description,
         *     shortDescription,
         *     categoryId,
         *
         *     tags,
         *
         *     images,
         *
         *     options,
         *
         *     variants,
         *
         *     variantImages
         * }
         */

        const response =
            await fetch(
                `/admin/product/get-update-product?id=${encodeURIComponent(
                    id,
                )}`,
                {
                    method: "GET",

                    headers: {
                        Accept:
                            "application/json",
                    },
                },
            );

        let result = null;

        try {
            result = await response.json();
            console.log(result);
        } catch {
            throw new Error(
                "Server trả về dữ liệu không hợp lệ.",
            );
        }

        if (
            !response.ok ||
            !result.success
        ) {
            throw new Error(
                result.message ||
                "Không thể tải sản phẩm.",
            );
        }

        if (!result.data) {
            throw new Error(
                "Không tìm thấy dữ liệu sản phẩm.",
            );
        }

        return normalizeProductData(
            result.data,
        );
    }

    // =========================================================
    // NORMALIZE PRODUCT DATA
    // =========================================================

    function normalizeProductData(
        data,
    ) {
        /*
         * Không mutate object backend.
         *
         * Tạo object mới để ProductEditor
         * nhận format ổn định.
         */
        console.log('normalizeProductData');
        console.log(data);
        console.log(data.variants);
        return {
            ...data,

            id:
                data.id ??
                data.productId ??
                productId,

            name:
                data.name ?? "",

            slug:
                data.slug ?? "",

            sku:
                data.sku ?? "",

            shortDescription:
                data.shortDescription ??
                "",

            description:
                data.description ??
                "",

            categoryId:
                data.categoryId ??
                null,
            tags:
                Array.isArray(
                    data.tags,
                )
                    ? data.tags
                    : [],

            images:
                Array.isArray(
                    data.images,
                )
                    ? data.images
                    : [],

            options:
                Array.isArray(
                    data.options,
                )
                    ? data.options
                    : [],

            variants:
                Array.isArray(
                    data.variants,
                )
                    ? data.variants
                    : [],

            variantImages:
                Array.isArray(
                    data.variantImages,
                )
                    ? data.variantImages
                    : [],
        };
    }

    // =========================================================
    // GENERATE SLUG
    // =========================================================

    const btnGenerateSlug =
        document.querySelector(
            "#btn-generate-slug",
        );

    btnGenerateSlug?.addEventListener(
        "click",
        async function (event) {
            event.preventDefault();

            const name =
                nameInput.val()?.trim();

            if (!name) {
                Toast.warning(
                    "Hãy nhập tên sản phẩm trước.",
                );

                nameInput.trigger(
                    "focus",
                );

                return;
            }

            try {
                btnGenerateSlug.classList.add(
                    "disabled",
                );

                btnGenerateSlug.disabled = true;

                btnGenerateSlug.innerHTML =
                    "Generating...";

                const response =
                    await fetch(
                        `/admin/product/generate-slug-product?name=${encodeURIComponent(
                            name,
                        )}&id=${encodeURIComponent(
                            productId,
                        )}`,
                    );

                const result =
                    await response.json();

                if (
                    !response.ok ||
                    !result.success
                ) {
                    Toast.error(
                        result.message ||
                        "Unable to generate slug.",
                    );

                    return;
                }

                slugInput.val(
                    result.slug,
                );

                clearValidationErrors(
                    productForm,
                );
            } catch (error) {
                console.error(
                    "Generate slug error:",
                    error,
                );

                Toast.error(
                    "Unable to generate slug.",
                );
            } finally {
                btnGenerateSlug.classList.remove(
                    "disabled",
                );

                btnGenerateSlug.disabled = false;

                btnGenerateSlug.innerHTML =
                    '<i class="ti ti-wand me-1"></i> Generate Slug';
            }
        },
    );

    // =========================================================
    // CREATE CATEGORY
    // =========================================================

    $(document).on(
        "click",
        "#btn-add-category",
        function () {
            const element =
                document.getElementById(
                    "offcanvasCreateCategory",
                );

            if (!element) {
                console.error(
                    "#offcanvasCreateCategory not found",
                );

                return;
            }

            const offcanvas =
                bootstrap.Offcanvas.getOrCreateInstance(
                    element,
                );

            offcanvas.show();
        },
    );

    // =========================================================
    // DISCARD / CANCEL UPDATE
    // =========================================================

    document
        .getElementById("btnDiscard")
        ?.addEventListener(
            "click",
            async function (event) {
                event.preventDefault();

                const result =
                    await Swal.fire({
                        title:
                            "Hủy chỉnh sửa sản phẩm?",

                        text:
                            "Các thay đổi bạn chưa lưu sẽ bị mất.",

                        icon: "warning",

                        showCancelButton: true,

                        confirmButtonText:
                            "Xác nhận",

                        cancelButtonText:
                            "Hủy",

                        reverseButtons: true,
                    });

                if (!result.isConfirmed) {
                    return;
                }

                window.location.href =
                    "/admin/product";
            },
        );

    // =========================================================
    // ENABLE / DISABLE SUBMIT BUTTONS
    // =========================================================

    function setSubmitButtonsDisabled(
        disabled,
    ) {
        const buttons = [
            "#btnPublish",
            "#btnSaveDraft",
            "#btnDiscard",
        ];

        buttons.forEach(
            function (selector) {
                const button =
                    productForm.querySelector(
                        selector,
                    );

                if (!button) {
                    return;
                }

                button.disabled =
                    disabled;
            },
        );
    }

    // =========================================================
    // FORM DATA HELPERS
    // =========================================================

    function removeFormDataKeys(
        formData,
        keys,
    ) {
        for (const key of [
            ...formData.keys(),
        ]) {
            if (keys.includes(key)) {
                formData.delete(key);
            }
        }
    }

    function removeFormDataKeysByPrefix(
        formData,
        prefix,
    ) {
        for (const key of [
            ...formData.keys(),
        ]) {
            if (key.startsWith(prefix)) {
                formData.delete(key);
            }
        }
    }

    // =========================================================
    // GET PRODUCT ID FROM URL
    // =========================================================

    function getProductIdFromUrl() {
        const path =
            window.location.pathname;

        const parts =
            path.split("/").filter(Boolean);

        if (parts.length === 0) {
            return null;
        }

        /*
         * Ví dụ:
         *
         * /admin/product/update/123
         *
         * => 123
         */

        const lastPart =
            parts[parts.length - 1];

        if (
            /^\d+$/.test(lastPart)
        ) {
            return lastPart;
        }

        return null;
    }
});