document.addEventListener("DOMContentLoaded", async function () {
    "use strict";

    // =========================================================
    // CREATE PRODUCT
    // =========================================================

    const productForm = document.querySelector("#productForm");

    if (!productForm) {
        return;
    }

    // =========================================================
    // INITIALIZE PRODUCT EDITOR
    // =========================================================

    /*
     * ProductEditor chịu trách nhiệm:
     *
     * - Product Images FilePond
     * - Product Tags Tagify
     * - Category
     * - Attributes
     * - Cropper
     * - Product Options
     * - Variant generation
     * - Variant FilePond
     * - Variant Image Groups
     *
     * Create mode:
     *
     * - Không có product data
     * - Editor khởi tạo state rỗng
     */

    let editor;

    try {
        editor = await ProductEditor.init({
            mode: "create",
            form: productForm,
        });
    } catch (error) {
        console.error("ProductEditor initialization error:", error);

        Toast.error("Không thể khởi tạo trình chỉnh sửa sản phẩm.");

        return;
    }

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
    // ELEMENTS
    // =========================================================

    const nameInput = $("#Name");

    const slugInput = $("#Slug");

    const skuInput = $("#SKU");


    // =========================================================
    // SUBMIT
    // =========================================================

    productForm.addEventListener("submit", async function (event) {
        event.preventDefault();

        const submitter = event.submitter;

        const submitAction =
            submitter?.dataset.action || "draft";

        await submitCreate(submitAction);
    });

    // =========================================================
    // SUBMIT CREATE
    // =========================================================

    async function submitCreate(submitAction) {
        clearValidationErrors(productForm);

        // =====================================================
        // GET EDITOR STATE
        // =====================================================

        const state = editor.getState();

        const productImages = state.productImages || [];

        const productTags = state.tags || [];

        const options = state.options || [];

        const variants = state.variants || [];


        const variantImageGroups = state.variantImageGroups || new Map();

        // =====================================================
        // BASIC DATA
        // =====================================================

        const name = nameInput.val()?.trim();

        const slug = slugInput.val()?.trim();

        const sku = skuInput.val()?.trim();

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
        // VALIDATE VARIANTS
        // =====================================================

        //const invalidVariant = variants.find(function (variant) {
        //    if (!variant) {
        //        return true;
        //    }

        //    if (!variant.sku?.trim()) {
        //        return true;
        //    }

        //    if (
        //        variant.price === "" ||
        //        variant.price === null ||
        //        variant.price === undefined
        //    ) {
        //        return true;
        //    }

        //    return false;
        //});
        // =====================================================
        // VALIDATE CATEGORY
        // =====================================================

        const categoryId = $("#CategoryId").val();

        if (!categoryId || categoryId === "0" || categoryId === 0) {
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
        //if (invalidVariant) {
        //    Toast.warning(
        //        "Vui lòng nhập đầy đủ SKU và giá cho các variant.",
        //    );

        //    return;
        //}

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
                if (!image?.file) {
                    Toast.warning(
                        `Variant #${variantNumber}: Vui lòng tải lên ít nhất 1 ảnh.`,
                    );

                    focusVariantImage(
                        row,
                    );

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

                    focusVariantInput(
                        row,
                        ".variant-price",
                    );

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
            "========== CREATE PRODUCT SUBMIT ==========",
        );

        for (const [key, value] of formData.entries()) {
            console.log(key, value);
        }

        // =====================================================
        // SUBMIT
        // =====================================================

        await sendCreateRequest(formData);
    }

    // =========================================================
    // BUILD FORM DATA
    // =========================================================
    function getProductTagsValue(tags = []) {
        if (!Array.isArray(tags)) {
            return [];
        }

        return tags
            .map(function (tag) {
                return String(
                    tag?.value ??
                    tag?.Value ??
                    tag
                ).trim();
            })
            .filter(Boolean);
    }
    function buildFormData({
        options,
        variants,
        productImages,
        productTags,
        variantImageGroups,
        submitAction,
    }) {
        /*
         * Bắt đầu từ form hiện tại.
         *
         * Không cần tự append:
         *
         * Name
         * SKU
         * Slug
         * CategoryId
         * ...
         *
         * vì FormData(form) đã lấy chúng.
         */

        const formData = new FormData(productForm);
        // =====================================================
        // PRODUCT IMAGES
        // =====================================================

        /*
         * Xóa Images/ProductImages cũ nếu HTML form
         * có input file liên quan.
         */

        removeFormDataKeys(
            formData,
            [
                "Images",
                "ProductImages",
            ],
        );


        productImages.forEach(function (image, index) {
            if (!image?.file) {
                return;
            }

            formData.append(
                `ProductImages[${index}].File`,
                image.file,
                image.file.name,
            );

            formData.append(
                `ProductImages[${index}].DisplayOrder`,
                String(image.displayOrder ?? index),
            );

            formData.append(
                `ProductImages[${index}].IsPrimary`,
                String(
                    image.isPrimary ??
                    index === 0,
                ),
            );
        });

        // =====================================================
        // Add Tags
        // =====================================================
        const tagValues = (productTags || [])
            .map(tag => typeof tag === "string" ? tag : tag.value)
            .map(tag => String(tag).trim())
            .filter(Boolean);
        formData.set(
            "Tags",
            JSON.stringify(tagValues),
        );

        // =====================================================
        // REMOVE ASP.NET REPEATER OPTIONS
        // =====================================================

        removeFormDataKeysByPrefix(
            formData,
            "Options[",
        );

        // =====================================================
        // OPTIONS
        // =====================================================

        formData.set(
            "Options",
            JSON.stringify(options),
        );

        // =====================================================
        // VARIANTS
        // =====================================================

        formData.set(
            "Variants",
            JSON.stringify(
                variants.map(function (variant) {
                    return {
                        options:
                            variant.options || {},

                        price:
                            variant.price !== "" &&
                                variant.price !== null &&
                                variant.price !== undefined
                                ? Number(variant.price)
                                : null,

                        stock:
                            variant.stock !== "" &&
                                variant.stock !== null &&
                                variant.stock !== undefined
                                ? Number(variant.stock)
                                : 0,

                        sku:
                            variant.sku?.trim() ||
                            null,
                    };
                }),
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
        console.log(
            "===== SUBMIT VARIANT IMAGES =====",
        );

        console.log(
            "variantImageGroups:",
            variantImageGroups,
        );

        console.log(
            "variantImageGroups.size:",
            variantImageGroups.size,
        );
        variantImageGroups.forEach(function (group, key) {
            console.log(
                "GROUP:",
                key,
                group,
            );
            const image = group?.image;
            console.log(
                "IMAGE:",
                image,
            );

            console.log(
                "IMAGE FILE:",
                image?.file,
            );

            if (!image?.file) {
                console.warn(
                    "SKIP VARIANT IMAGE:",
                    key,
                );

                return;
            }

            formData.append(
                `VariantImages[${imageIndex}].Key`,
                key,
            );

            formData.append(
                `VariantImages[${imageIndex}].File`,
                image.file,
                image.name ||
                image.file.name,
            );

            imageIndex++;
        });
        console.log(
            "Variant image count:",
            imageIndex,
        );
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
    // REMOVE FORM DATA KEYS
    // =========================================================

    function removeFormDataKeys(formData, keys) {
        for (const key of [...formData.keys()]) {
            if (keys.includes(key)) {
                formData.delete(key);
            }
        }
    }

    // =========================================================
    // REMOVE FORM DATA PREFIX
    // =========================================================

    function removeFormDataKeysByPrefix(
        formData,
        prefix,
    ) {
        for (const key of [...formData.keys()]) {
            if (key.startsWith(prefix)) {
                formData.delete(key);
            }
        }
    }

    // =========================================================
    // SEND CREATE REQUEST
    // =========================================================

    async function sendCreateRequest(formData) {
        const url = productForm.action;

        try {
            const response = await $.ajax({
                url: url,

                type: "POST",

                data: formData,

                processData: false,

                contentType: false,

                beforeSend: function () {
                    setSubmitButtonsDisabled(
                        true,
                    );
                },
            });

            if (response.success) {
                /*
                 * Giữ behavior hiện tại:
                 * redirect trước, Toast sau.
                 */

                window.location.href =
                    response.redirectUrl;

                setTimeout(function () {
                    Toast.success(
                        response.message,
                    );
                }, 500);

                return;
            }

            Toast.error(
                response.message ||
                "Không thể tạo sản phẩm.",
            );
        } catch (xhr) {
            console.error(
                "Create product error:",
                xhr,
            );
            Toast.error(
               
                "Không thể tạo sản phẩm: " + xhr,
            );
            /*
             * Validation error từ server.
             */

            if (
                xhr?.responseJSON?.errors
            ) {
                handleValidationErrors(
                    xhr,
                    productForm,
                );
            } else {
                Toast.error(
                    "Không thể tạo sản phẩm.",
                );
            }
        } finally {
            setSubmitButtonsDisabled(
                false,
            );
        }
    }

    // =========================================================
    // DISABLE SUBMIT BUTTONS
    // =========================================================

    function setSubmitButtonsDisabled(disabled) {
        const buttons = [
            "#btnPublish",
            "#btnSaveDraft",
            "#btnDiscard",
        ];

        buttons.forEach(function (selector) {
            const button =
                productForm.querySelector(
                    selector,
                );

            if (!button) {
                return;
            }

            button.disabled = disabled;
        });
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

                nameInput.trigger("focus");

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
    // DISCARD
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
                            "Hủy tạo sản phẩm?",

                        text:
                            "Các thông tin bạn đã nhập sẽ bị mất.",

                        icon: "warning",

                        showCancelButton: true,

                        confirmButtonText:
                            "Discard",

                        cancelButtonText:
                            "Hủy",

                        reverseButtons: true,
                    });

                if (!result.isConfirmed) {
                    return;
                }

                window.location.href = "/admin/product";
            },
        );
});