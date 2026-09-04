/**
 * ============================================================
 * PRODUCT EDITOR - SHARED PRODUCT UI
 * ============================================================
 *
 * Dùng chung cho:
 *
 * - create-product.js
 * - update-product.js
 *
 * Không chứa:
 *
 * - Create submit
 * - Update submit
 * - AJAX submit
 * - Redirect
 * - Product API loading
 *
 * Chứa:
 *
 * - Product Images FilePond
 * - Product Images Cropper
 * - Product Tags Tagify
 * - Product Categories
 * - Product Attributes
 * - Option Repeater
 * - Option Tagify
 * - Tagify Sortable
 * - Variant generation
 * - Variant DataTable
 * - Variant Image Groups
 * - Variant FilePond
 *
 * ============================================================
 */

(function (window, $) {
    "use strict";

    // ============================================================
    // PRODUCT EDITOR
    // ============================================================

    const ProductEditor = (function () {
        // ========================================================
        // PRIVATE STATE
        // ========================================================

        let initialized = false;

        let mode = "create";

        let elements = {};

        // ========================================================
        // PRODUCT IMAGE
        // ========================================================

        let productImagePond = null;

        let productImages = [];

        // ========================================================
        // PRODUCT IMAGE CROP
        // ========================================================

        let currentFile = null;

        let cropper = null;

        let objectUrl = null;

        let isSavingCrop = false;

        // ========================================================
        // PRODUCT TAGS
        // ========================================================

        let productTags = null;

        // ========================================================
        // QUILL
        // ========================================================

        let quill = null;
        let attributesCache = [];
        let attributesLoaded = false;
        // ========================================================
        // VARIANT DATATABLE
        // ========================================================

        let variantDataTable = null;

        // ========================================================
        // VARIANT FILEPOND
        // ========================================================

        /*
         * Key:
         *
         * Color:red
         * Color:blue
         *
         * KHÔNG dùng:
         *
         * Color:red|Size:M
         */

        const variantFilePonds = new Map();

        // ========================================================
        // VARIANT IMAGE GROUPS
        // ========================================================

        /*
         * Mỗi image group chỉ có 1 image.
         *
         * Color:red
         *     -> red.jpg
         *
         * Color:blue
         *     -> blue.jpg
         */

        const variantImageGroups = new Map();

        // ========================================================
        // PRODUCT VARIANT STATE
        // ========================================================

        const productVariantState = {
            options: [],
            variants: [],
        };

        // ========================================================
        // INIT
        // ========================================================

        async function init({
            mode = "create",
            form,
            productId = null,
            product = null,
        }) {
            if (initialized) {
                destroy();
            }
            //mode = config.mode || "create";

            elements = normalizeElements(config.elements || {});

            const data = normalizeData(config.data || {});

            registerFilePondPlugins();

            initializeProductTags(product?.tags ?? []);

            await loadProductCategories(product?.categoryId ?? null);
            await loadAttributes();
            initializeQuill();
            console.log("init");
            console.log(product?.images);
            await initProductImageFilePond({
                mode,
                images: product?.images ?? [],
            });

            initializeCropperEvents();

            initializeOptionRepeater();

            initializeGlobalEvents();

            /*
             * Restore product data.
             *
             * Create:
             *
             *     images   = []
             *     options  = []
             *     variants = []
             *
             * Update:
             *
             *     restore existing product
             */

            await restoreProductData(mode, product);

            initialized = true;

            return api;
        }

        // ========================================================
        // NORMALIZE ELEMENTS
        // ========================================================

        function normalizeElements(source) {
            return {
                form:
                    resolveElement(source.form, "#createProduct") ||
                    resolveElement(source.form, "#updateProduct"),

                productImages: resolveElement(source.productImages, "#productImages"),

                tags: resolveElement(source.tags, "#Tags"),

                category: resolveElement(source.category, "#CategoryId"),

                optionsRepeater: resolveElement(
                    source.optionsRepeater,
                    ".form-repeater",
                ),

                variantsTable: resolveElement(
                    source.variantsTable,
                    "#product-variants-table",
                ),

                cropModal: resolveElement(source.cropModal, "#cropModal"),

                cropImage: resolveElement(source.cropImage, "#cropImage"),

                saveCrop: resolveElement(source.saveCrop, "#saveCrop"),

                cropTitle: resolveElement(source.cropTitle, "#cropTitle"),

                commentEditor: resolveElement(source.commentEditor, ".comment-editor"),

                btnApplyAll: resolveElement(source.btnApplyAll, "#btn-apply-all"),

                variantDefaultPrice: resolveElement(
                    source.variantDefaultPrice,
                    "#variant-default-price",
                ),

                variantDefaultStock: resolveElement(
                    source.variantDefaultStock,
                    "#variant-default-stock",
                ),
            };
        }

        // ========================================================
        // RESOLVE ELEMENT
        // ========================================================

        function resolveElement(element, fallback) {
            if (element instanceof HTMLElement) {
                return element;
            }

            if (element && typeof element === "string") {
                return document.querySelector(element);
            }

            if (fallback) {
                return document.querySelector(fallback);
            }

            return null;
        }

        // ========================================================
        // NORMALIZE DATA
        // ========================================================

        function normalizeData(data) {
            return {
                categoryId: data.categoryId ?? data.CategoryId ?? null,

                tags: Array.isArray(data.tags)
                    ? data.tags
                    : Array.isArray(data.Tags)
                        ? data.Tags
                        : [],

                images: Array.isArray(data.images)
                    ? data.images
                    : Array.isArray(data.Images)
                        ? data.Images
                        : [],

                options: Array.isArray(data.options)
                    ? data.options
                    : Array.isArray(data.Options)
                        ? data.Options
                        : [],

                variants: Array.isArray(data.variants)
                    ? data.variants
                    : Array.isArray(data.Variants)
                        ? data.Variants
                        : [],

                description: data.description ?? data.Description ?? "",

                variantImages: Array.isArray(data.variantImages)
                    ? data.variantImages
                    : Array.isArray(data.VariantImages)
                        ? data.VariantImages
                        : [],
            };
        }

        // ========================================================
        // REGISTER FILEPOND PLUGINS
        // ========================================================

        function registerFilePondPlugins() {
            if (
                typeof FilePond === "undefined" ||
                typeof FilePond.registerPlugin !== "function"
            ) {
                console.error("FilePond is not loaded.");

                return;
            }

            const plugins = [];

            if (typeof FilePondPluginFileValidateType !== "undefined") {
                plugins.push(FilePondPluginFileValidateType);
            }

            if (typeof FilePondPluginFileValidateSize !== "undefined") {
                plugins.push(FilePondPluginFileValidateSize);
            }

            if (typeof FilePondPluginImagePreview !== "undefined") {
                plugins.push(FilePondPluginImagePreview);
            }

            if (typeof FilePondPluginImageResize !== "undefined") {
                plugins.push(FilePondPluginImageResize);
            }

            if (typeof FilePondPluginImageTransform !== "undefined") {
                plugins.push(FilePondPluginImageTransform);
            }

            if (plugins.length) {
                FilePond.registerPlugin(...plugins);
            }
        }

        // ========================================================
        // PRODUCT TAGS
        // ========================================================
        function getProductTagsValue() {
            if (!productTags?.value) {
                return [];
            }

            return productTags.value
                .map(function (tag) {
                    return String(tag.value).trim();
                })
                .filter(Boolean);
        }
        function initializeProductTags(values = []) {
            if (!elements.tags) {
                return;
            }

            /*
             * Nếu đã có instance từ ngoài,
             * không tạo thêm.
             */

            if (elements.tags._productTagify) {
                try {
                    elements.tags._productTagify.destroy();
                } catch (error) {
                    console.warn("Destroy product Tagify error:", error);
                }

                elements.tags._productTagify = null;
            }

            productTags = new Tagify(elements.tags, {
                duplicates: false,

                editTags: true,

                dropdown: {
                    enabled: 0,
                },
            });

            elements.tags._productTagify = productTags;

            const normalizedValues = normalizeTagValues(values);

            if (normalizedValues.length) {
                productTags.addTags(normalizedValues);
            }

            syncProductTags();
        }

        // ========================================================
        // NORMALIZE TAG VALUES
        // ========================================================

        function normalizeTagValues(values) {
            if (!Array.isArray(values)) {
                return [];
            }

            return values
                .map(function (item) {
                    if (
                        typeof item === "string" ||
                        typeof item === "number"
                    ) {
                        return String(item);
                    }

                    return (
                        item?.value ??
                        item?.Value ??
                        item?.name ??
                        item?.Name ??
                        ""
                    );
                })
                .map(function (value) {
                    return String(value).trim();
                })
                .filter(Boolean);
        }

        // ========================================================
        // SYNC PRODUCT TAGS
        // ========================================================

        function syncProductTags() {
            if (!productTags.value) {
                productTags.value = [];
                return;
            }
            return productTags.value
                .map(function (tag) {
                    return {
                        value: String(tag.value ?? "").trim(),
                    };
                })
                .filter(function (tag) {
                    return tag.value;
                });
        }

        // ========================================================
        // PRODUCT CATEGORIES
        // ========================================================

        async function loadProductCategories(selectedId = null) {
            const $select = elements.category
                ? $(elements.category)
                : $("#CategoryId");
            if (!$select.length) {
                return;
            }

            try {
                $select.prop("disabled", true);

                const response = await fetch(
                    "/admin/product/get-create-product-categories",
                );

                const result = await response.json();
                if (!response.ok || !result.success) {
                    Toast.error(result.message || "Unable to load categories.");

                    return;
                }

                $select.empty();

                $select.append(new Option("Select Category", "0"));

                if (Array.isArray(result.data)) {
                    result.data.forEach(function (category) {
                        $select.append(new Option(category.text, category.value));
                    });
                }

                if (
                    selectedId !== null &&
                    selectedId !== undefined &&
                    String(selectedId) !== ""
                ) {
                    $select.val(String(selectedId));
                } else {
                    $select.val("0");
                }

                $select.trigger("change");
            } catch (error) {
                console.error("Load product categories error:", error);

                Toast.error("Unable to load categories.");
            } finally {
                $select.prop("disabled", false);
            }
        }

        // ========================================================
        // ATTRIBUTES
        // ========================================================

        async function loadAttributes() {
            if (attributesLoaded) {
                return attributesCache;
            }

            try {
                const response = await fetch(
                    "/admin/product/get-create-attributes",
                );

                const result =
                    await response.json();

                console.log('get create product attribute');
                console.log(result);
                if (
                    !response.ok ||
                    !result.success
                ) {
                    throw new Error(
                        result.message ||
                        "Unable to load attributes.",
                    );
                }

                attributesCache =
                    Array.isArray(result.data)
                        ? result.data
                        : [];

                attributesLoaded = true;

                return attributesCache;
            } catch (error) {
                console.error(
                    "Load attributes error:",
                    error,
                );

                Toast.error(
                    "Unable to load attributes.",
                );

                return [];
            }
        }
        function populateAttributeSelect(
            $select,
            selectedCode = "",
        ) {
            if (
                !$select ||
                !$select.length
            ) {
                return;
            }

            $select.empty();

            $select.append(
                new Option(
                    "Chọn phân loại",
                    "",
                ),
            );

            attributesCache.forEach(
                function (attribute) {
                    $select.append(
                        new Option(
                            attribute.text,
                            attribute.value,
                        ),
                    );
                },
            );

            if (selectedCode) {
                $select.val(
                    String(selectedCode),
                );
            } else {
                $select.val("");
            }

            $select.trigger("change");
        }
        // ========================================================
        // QUILL
        // ========================================================

        function initializeQuill() {
            if (!elements.commentEditor) {
                return;
            }

            if (typeof Quill === "undefined") {
                return;
            }

            quill = new Quill(elements.commentEditor, {
                modules: {
                    toolbar: ".comment-toolbar",
                },

                placeholder: "Product Description",

                theme: "snow",
            });
        }

        // ========================================================
        // PRODUCT IMAGE FILEPOND
        // ========================================================
        function initProductImageFilePond({ images = [], mode = "create" }) {
            try {
                const existingImages = mode === "update" && Array.isArray(images) ? images : [];
                productImagePond = FilePond.create(elements.productImages, {
                    allowMultiple: true,

                    files: existingImages.map(function (image) {
                        return {
                            source: image.url,

                            options: {
                                type: "local",

                                metadata: {
                                    mediaId: image.id,
                                    displayOrder: image.displayOrder ?? 0,
                                    isPrimary: Boolean(image.isPrimary),
                                    isExisting: true,
                                },
                            },
                        };
                    }),
                    server: {
                        load: function (source, load, error, progress, abort) {
                            const url = new URL(`/uploads${source}`, window.location.origin)
                                .href;

                            fetch(url)
                                .then(function (response) {
                                    if (!response.ok) {
                                        throw new Error(`HTTP ${response.status}`);
                                    }

                                    return response.blob();
                                })
                                .then(function (blob) {
                                    load(blob);
                                })
                                .catch(function (err) {
                                    error(err.message);
                                });
                        },
                    },

                    maxFiles: 10,

                    labelMaxFilesExceeded: "Bạn chỉ có thể tải lên tối đa {maxFiles} ảnh",

                    labelMaxFiles: "Tối đa {maxFiles} ảnh",

                    allowReorder: true,

                    acceptedFileTypes: ["image/jpeg", "image/png", "image/webp"],

                    styleItemPanelAspectRatio: 1,

                    maxFileSize: "5MB",

                    allowImagePreview: true,

                    imagePreviewHeight: 60,

                    allowImageResize: false,

                    imageResizeTargetWidth: 300,

                    imageResizeTargetHeight: 300,

                    imageResizeMode: "contain",

                    allowImageTransform: true,

                    imageTransformOutputQuality: 90,

                    itemInsertLocation: "after",

                    itemInsertLocationFreedom: false,

                    labelIdle: 'Kéo & thả ảnh vào đây hoặc <span class="filepond--label-action">Chọn ảnh</span>',

                    styleButtonRemoveItemPosition: "left",

                    styleLoadIndicatorPosition: "center",
                });

                const root = productImagePond.element.closest(".filepond--root");

                if (root) {
                    root.dataset.filepondField = elements.productImages.name;
                }

                bindProductImageEvents();
            } catch (error) {
                console.error("FilePond CREATE ERROR:", error);
            }
        }

        // ========================================================
        // PRODUCT IMAGE EVENTS
        // ========================================================

        function bindProductImageEvents() {
            if (!productImagePond) {
                return;
            }

            // ====================================================
            // ADD
            // ====================================================

            productImagePond.on("addfile", function (error, file) {
                if (error) {
                    handleProductImageError(error, file);

                    return;
                }

                if (!file) {
                    return;
                }

                const exists = productImages.some(function (image) {
                    return image.id === file.id;
                });
                const metadata = file.getMetadata();
                if (!exists) {
                    productImages.push({
                        id: metadata.mediaId,

                        file: file.file,

                        displayOrder: productImages.length,

                        isPrimary: productImages.length === 0,

                        /*
                         * Update có thể giữ ID DB.
                         */

                        serverId: file.serverId ?? null,
                    });
                }

                reindexProductImages();

                renderProductImageEditButtons();
            });

            // ====================================================
            // REMOVE
            // ====================================================

            productImagePond.on("removefile", function (error, file) {
                if (error) {
                    Toast.error("Không thể xóa ảnh.");

                    return;
                }

                if (!file) {
                    return;
                }

                /*
                 * Crop đang replace file.
                 *
                 * Không xử lý state ở đây.
                 */

                if (currentFile && file.id === currentFile.id) {
                    return;
                }
                console.log('SAU REMOVE: ');
                const productImageId = file.getMetadata("mediaId"); 
                console.log(productImageId);
                productImages = productImages.filter(function (image) {
                    return String(image.id) !== String(productImageId);
                });
                reindexProductImages();

                renderProductImageEditButtons();
                console.log(productImages);
                Toast.success(`Đã xóa ${file.filename}`);
            });

            // ====================================================
            // UPDATE FILES
            // ====================================================

            productImagePond.on("updatefiles", function () {
                setTimeout(renderProductImageEditButtons, 100);
            });

            // ====================================================
            // REORDER
            // ====================================================

            productImagePond.on("reorderfiles", function () {
                const files = productImagePond.getFiles();

                files.forEach(function (file, index) {
                    const image = productImages.find(function (item) {
                        return item.id === file.id;
                    });

                    if (image) {
                        image.displayOrder = index;
                    }
                });

                productImages.sort(function (a, b) {
                    return a.displayOrder - b.displayOrder;
                });

                reindexProductImages();

                renderProductImageEditButtons();
            });

            // ====================================================
            // WARNING
            // ====================================================

            productImagePond.on("warning", function (error) {
                if (error && error.body && error.body.includes("Max files")) {
                    Toast.warning("Bạn chỉ được tải lên tối đa 10 ảnh.");
                }
            });
        }

        // ========================================================
        // PRODUCT IMAGE ERROR
        // ========================================================

        function handleProductImageError(error, file) {
            console.error("FilePond add error:", error);

            const filename = file?.filename || "Ảnh";

            if (error.code === "file-too-large") {
                Toast.error(`${filename}: Ảnh vượt quá 5MB.`);

                return;
            }

            if (error.code === "file-invalid-type") {
                Toast.error(`${filename}: Chỉ chấp nhận JPG, PNG hoặc WEBP.`);

                return;
            }

            Toast.error(`${filename}: Không thể thêm ảnh.`);
        }

        // ========================================================
        // REINDEX PRODUCT IMAGES
        // ========================================================

        function reindexProductImages() {
            productImages.forEach(function (image, index) {
                image.displayOrder = index;

                image.isPrimary = index === 0;
            });
        }

        // ========================================================
        // PRODUCT IMAGE EDIT BUTTON
        // ========================================================

        function renderProductImageEditButtons() {
            if (!productImagePond) {
                return;
            }

            const files = productImagePond.getFiles();

            const items =
                productImagePond.element.querySelectorAll(".filepond--item");

            items.forEach(function (item, index) {
                if (item.querySelector(".fp-edit-button")) {
                    return;
                }

                const file = files[index];

                if (!file) {
                    return;
                }

                const button = document.createElement("button");

                button.type = "button";

                button.className = "fp-edit-button";

                button.innerHTML = "✎";

                button.title = "Chỉnh sửa ảnh";

                button.addEventListener("click", function (event) {
                    event.preventDefault();

                    event.stopPropagation();

                    openCropper(file);
                });

                item.appendChild(button);
            });
        }

        // ========================================================
        // CROP EVENTS
        // ========================================================

        // ========================================================
        // RESTORE PRODUCT IMAGES
        // ========================================================

        async function restoreProductImages(images) {
            if (!productImagePond || !Array.isArray(images) || images.length === 0) {
                return;
            }

            const sortedImages = [...images].sort(function (a, b) {
                return (
                    Number(a.displayOrder ?? a.DisplayOrder ?? 0) -
                    Number(b.displayOrder ?? b.DisplayOrder ?? 0)
                );
            });

            for (const image of sortedImages) {
                const source =
                    image.url ??
                    image.Url ??
                    image.file ??
                    image.File ??
                    image.path ??
                    image.Path;

                if (!source) {
                    continue;
                }

                try {
                    const fileItem = await productImagePond.addFile(source);

                    const serverId = image.id ?? image.Id ?? null;

                    const existing = productImages.find(function (item) {
                        return item.id === fileItem.id;
                    });

                    if (existing) {
                        existing.serverId = serverId;
                    } else {
                        productImages.push({
                            id: fileItem.id,

                            file: fileItem.file,

                            displayOrder: Number(
                                image.displayOrder ??
                                image.DisplayOrder ??
                                productImages.length,
                            ),

                            isPrimary: Boolean(image.isPrimary ?? image.IsPrimary ?? false),

                            serverId: serverId,
                        });
                    }
                } catch (error) {
                    console.warn("Cannot restore product image:", source, error);
                }
            }

            reindexProductImages();

            setTimeout(renderProductImageEditButtons, 100);
        }
        function initializeCropperEvents() {
            document
                .querySelector("#rotateLeft")
                ?.addEventListener("click", function () {
                    if (cropper && !isSavingCrop) {
                        cropper.rotate(-90);
                    }
                });

            document
                .querySelector("#rotateRight")
                ?.addEventListener("click", function () {
                    if (cropper && !isSavingCrop) {
                        cropper.rotate(90);
                    }
                });

            document.querySelector("#zoomIn")?.addEventListener("click", function () {
                if (cropper && !isSavingCrop) {
                    cropper.zoom(0.1);
                }
            });

            document
                .querySelector("#zoomOut")
                ?.addEventListener("click", function () {
                    if (cropper && !isSavingCrop) {
                        cropper.zoom(-0.1);
                    }
                });

            document
                .querySelector("#resetCrop")
                ?.addEventListener("click", function () {
                    if (cropper && !isSavingCrop) {
                        cropper.reset();
                    }
                });

            elements.saveCrop?.addEventListener("click", saveCrop);

            document
                .querySelector("#cancelCrop")
                ?.addEventListener("click", function () {
                    if (!isSavingCrop) {
                        closeCropper();
                    }
                });

            document
                .querySelector("#closeCrop")
                ?.addEventListener("click", function () {
                    if (!isSavingCrop) {
                        closeCropper();
                    }
                });
        }

        // ========================================================
        // OPEN CROPPER
        // ========================================================

        function openCropper(file) {
            if (!file || !file.file || !elements.cropModal || !elements.cropImage) {
                return;
            }

            if (elements.cropTitle) {
                elements.cropTitle.innerHTML = `Chỉnh sửa ảnh: <span>${escapeHtml(
                    file.filename,
                )}</span>`;
            }

            if (cropper) {
                cropper.destroy();

                cropper = null;
            }

            currentFile = file;

            objectUrl = URL.createObjectURL(file.file);

            elements.cropImage.src = objectUrl;

            elements.cropModal.classList.add("show");

            elements.cropImage.onload = function () {
                cropper = new Cropper(elements.cropImage, {
                    viewMode: 1,

                    dragMode: "move",

                    autoCropArea: 1,

                    responsive: true,

                    background: false,

                    movable: true,

                    zoomable: true,

                    rotatable: true,

                    scalable: true,

                    aspectRatio: NaN,
                });
            };
        }

        // ========================================================
        // SAVE CROP
        // ========================================================

        async function saveCrop() {
            if (isSavingCrop) {
                return;
            }

            if (!cropper || !currentFile || !productImagePond) {
                return;
            }

            isSavingCrop = true;

            if (elements.saveCrop) {
                elements.saveCrop.disabled = true;

                elements.saveCrop.textContent = "Đang lưu...";
            }

            try {
                const imageIndex = productImages.findIndex(function (image) {
                    return image.id === currentFile.id;
                });

                if (imageIndex === -1) {
                    throw new Error("Không tìm thấy ảnh.");
                }

                const oldImage = productImages[imageIndex];

                const canvas = cropper.getCroppedCanvas({
                    imageSmoothingEnabled: true,

                    imageSmoothingQuality: "high",
                });

                if (!canvas) {
                    throw new Error("Không thể tạo canvas.");
                }

                const blob = await new Promise(function (resolve, reject) {
                    canvas.toBlob(
                        function (result) {
                            if (!result) {
                                reject(new Error("Không thể tạo ảnh."));

                                return;
                            }

                            resolve(result);
                        },
                        "image/jpeg",
                        0.92,
                    );
                });

                const newFile = new File([blob], currentFile.filename, {
                    type: "image/jpeg",

                    lastModified: Date.now(),
                });

                const oldFileId = currentFile.id;

                const oldIndex = productImagePond.getFiles().findIndex(function (item) {
                    return item.id === oldFileId;
                });

                if (oldIndex === -1) {
                    throw new Error("Không tìm thấy vị trí file cũ.");
                }

                await productImagePond.removeFile(oldFileId);

                const addedFile = await productImagePond.addFile(newFile);

                const newFiles = productImagePond.getFiles();

                const currentIndex = newFiles.findIndex(function (item) {
                    return item.id === addedFile.id;
                });

                if (currentIndex !== -1 && currentIndex !== oldIndex) {
                    await productImagePond.moveFile(currentIndex, oldIndex);
                }

                productImages[imageIndex] = {
                    ...oldImage,

                    id: addedFile.id,

                    file: addedFile.file,
                };

                productImages[imageIndex].displayOrder = oldImage.displayOrder;

                productImages[imageIndex].isPrimary = oldImage.isPrimary;

                closeCropper();

                setTimeout(renderProductImageEditButtons, 100);
            } catch (error) {
                console.error("Crop error:", error);

                Toast.error("Không thể lưu ảnh. Vui lòng thử lại.");
            } finally {
                isSavingCrop = false;

                if (elements.saveCrop) {
                    elements.saveCrop.disabled = false;

                    elements.saveCrop.textContent = "Lưu thay đổi";
                }
            }
        }

        // ========================================================
        // CLOSE CROPPER
        // ========================================================

        function closeCropper() {
            elements.cropModal?.classList.remove("show");

            if (cropper) {
                cropper.destroy();

                cropper = null;
            }

            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);

                objectUrl = null;
            }

            currentFile = null;
        }

        // ========================================================
        // OPTION REPEATER
        // ========================================================

        function initializeOptionRepeater() {
            const $repeater = elements.optionsRepeater
                ? $(elements.optionsRepeater)
                : $(".form-repeater");

            if (!$repeater.length) {
                return;
            }

            let row = 2;

            let col = 1;

            $repeater.repeater({
                initEmpty: true,

                show: function () {
                    const $item = $(this);

                    const $controls = $item.find(".form-control, .form-select");

                    const $labels = $item.find(".form-label");

                    $controls.each(function (i) {
                        const id = "form-repeater-" + row + "-" + col;

                        $(this).attr("id", id);

                        if ($labels[i]) {
                            $($labels[i]).attr("for", id);
                        }

                        col++;
                    });

                    row++;

                    $item.slideDown();

                    initializeOptionItem($item);
                    populateAttributeSelect(
                        $item.find(".option-name"),
                    );

                    updateOptionNames();

                    updateApplyButton();
                },
                hide: function (deleteElement) {
                    const $item = $(this);

                    destroyOptionItem($item);

                    $item.slideUp(function () {
                        deleteElement();

                        updateOptionNames();
                        updateApplyButton();

                        requestAnimationFrame(function () {
                            generateVariants();
                        });
                    });
                },
                //hide: function (
                //    deleteElement,
                //) {
                //    const $item =
                //        $(this);

                //    destroyOptionItem(
                //        $item,
                //    );

                //    $item.slideUp(
                //        function () {
                //            deleteElement();

                //            generateVariants();

                //            updateOptionNames();

                //            updateApplyButton();
                //        },
                //    );
                //},
            });
        }

        // ========================================================
        // INITIALIZE OPTION ITEM
        // ========================================================

        function initializeOptionItem($item) {
            initializeOptionTagify($item);
        }

        // ========================================================
        // DESTROY OPTION ITEM
        // ========================================================

        function destroyOptionItem($item) {
            const sortable = $item.data("sortable-instance");

            if (sortable) {
                try {
                    sortable.destroy();
                } catch (error) {
                    console.warn("Destroy Tagify Sortable error:", error);
                }

                $item.removeData("sortable-instance");
            }

            const tagify = $item.data("tagify-instance");

            if (tagify) {
                try {
                    tagify.destroy();
                } catch (error) {
                    console.warn("Destroy Tagify error:", error);
                }

                $item.removeData("tagify-instance");
            }
        }

        // ========================================================
        // INITIALIZE OPTION TAGIFY
        // ========================================================

        function initializeOptionTagify($item) {
            const input = $item.find(".option-values")[0];

            if (!input) {
                return;
            }

            if ($item.data("tagify-instance")) {
                const tagify = $item.data("tagify-instance");

                initializeTagifySortable($item, tagify);

                return;
            }

            const tagify = new Tagify(input, {
                duplicates: false,

                editTags: true,

                dropdown: {
                    enabled: 0,
                },
            });

            $item.data("tagify-instance", tagify);

            tagify.on("add", function () {
                setTimeout(generateVariants, 0);
            });

            tagify.on("remove", function () {
                setTimeout(generateVariants, 0);
            });

            tagify.on("edit:updated", function () {
                setTimeout(generateVariants, 0);
            });

            initializeTagifySortable($item, tagify);
        }

        // ========================================================
        // TAGIFY SORTABLE
        // ========================================================

        function initializeTagifySortable($item, tagify) {
            const tagContainer = $item.find(".tagify")[0];

            if (!tagContainer) {
                setTimeout(function () {
                    initializeTagifySortable($item, tagify);
                }, 0);

                return;
            }

            if ($(tagContainer).data("sortable-instance")) {
                return;
            }

            if (typeof Sortable === "undefined") {
                console.warn("SortableJS is not loaded.");

                return;
            }

            const sortable = Sortable.create(tagContainer, {
                animation: 150,

                draggable: ".tagify__tag",

                handle: ".tagify__tag",

                filter: "input",

                onStart: function () {
                    $(tagContainer).addClass("tagify-sortable-active");
                },

                onEnd: function () {
                    $(tagContainer).removeClass("tagify-sortable-active");

                    syncTagifyOrder(tagify, tagContainer);
                },
            });

            $item.data("sortable-instance", sortable);
        }

        // ========================================================
        // SYNC TAGIFY ORDER
        // ========================================================

        function syncTagifyOrder(tagify, tagContainer) {
            const orderedValues = [];

            $(tagContainer)
                .children(".tagify__tag")
                .each(function () {
                    const value = $(this).attr("value");

                    if (value !== undefined && value !== null) {
                        orderedValues.push(String(value));
                    }
                });

            if (orderedValues.length === 0) {
                return;
            }

            const currentTags = tagify.value;

            const tagMap = new Map();

            currentTags.forEach(function (tag) {
                tagMap.set(String(tag.value), tag);
            });

            const reorderedTags = [];

            orderedValues.forEach(function (value) {
                const tag = tagMap.get(value);

                if (tag) {
                    reorderedTags.push(tag);
                }
            });

            const changed = reorderedTags.some(function (tag, index) {
                const oldTag = currentTags[index];

                return !oldTag || String(oldTag.value) !== String(tag.value);
            });

            if (!changed) {
                return;
            }

            /*
             * Không remove/add.
             *
             * Không trigger Tagify event.
             */

            tagify.value = reorderedTags;

            /*
             * Chỉ generate lại variant
             * theo thứ tự mới.
             */

            generateVariants();
        }

        // ========================================================
        // RESTORE OPTIONS
        // ========================================================

        async function restoreOptions(options) {
            if (!Array.isArray(options) || options.length === 0) {
                return;
            }

            const $repeater = elements.optionsRepeater
                ? $(elements.optionsRepeater)
                : $(".form-repeater");

            if (!$repeater.length) {
                return;
            }

            const $list = $repeater.find('[data-repeater-list="Options"]');

            if (!$list.length) {
                return;
            }

            /*
             * initEmpty: true
             *
             * Vì vậy cần add từng item.
             */

            for (const option of options) {
                const repeaterApi = $repeater.repeater;

                /*
                 * jquery-repeater không expose
                 * add() ở mọi version.
                 *
                 * Trigger click vào button
                 * data-repeater-create là ổn định nhất.
                 */

                const createButton = $repeater.find("[data-repeater-create]");

                if (!createButton.length) {
                    console.warn("data-repeater-create not found.");

                    break;
                }

                createButton.trigger("click");

                /*
                 * Chờ repeater clone item.
                 */

                await nextTick();

                const $items = $list.children("[data-repeater-item]");

                const $item = $items.last();

                if (!$item.length) {
                    continue;
                }

                const name =
                    option.name ??
                    option.Name ??
                    "";

                const values =
                    normalizeTagValues(
                        option.values ??
                        option.Values ??
                        [],
                    );

                const $select =
                    $item.find(".option-name");

                if (!$select.length) {
                    continue;
                }

                /*
                 * Option name
                 */
                console.log(option);
                const attributeCode =
                    option.name ??
                    option.code ??
                    findAttributeCodeByName(name);

                populateAttributeSelect(
                    $select,
                    attributeCode,
                );
                /*
                 * Tagify
                 */

                const tagify = $item.data("tagify-instance");

                if (tagify && values.length) {
                    tagify.removeAllTags();

                    tagify.addTags(values);
                }
            }

            updateOptionNames();

            updateApplyButton();

            syncOptionsFromForm();
        }

        // ========================================================
        // SYNC OPTIONS
        // ========================================================

        function syncOptionsFromForm() {
            const options = [];

            $('[data-repeater-list="Options"]')
                .children("[data-repeater-item]")
                .each(function () {
                    const $item = $(this);

                    const name = $item.find(".option-name").val();

                    if (!name) {
                        return;
                    }

                    const tagify = $item.data("tagify-instance");

                    if (!tagify) {
                        console.warn("Tagify instance not found:", name);

                        return;
                    }

                    const values = tagify.value
                        .map(function (tag) {
                            return String(tag.value).trim();
                        })
                        .filter(function (value) {
                            return value !== "";
                        });

                    options.push({
                        name: name,

                        values: values,
                    });
                });

            productVariantState.options = options;

            return options;
        }

        // ========================================================
        // OPTION NAME
        // ========================================================

        function updateOptionNames() {
            const selectedNames = [];

            $('[data-repeater-list="Options"] .option-name').each(function () {
                const value = $(this).val();

                if (value) {
                    selectedNames.push(value);
                }
            });

            $('[data-repeater-list="Options"] .option-name').each(function () {
                const currentValue = $(this).val();

                $(this)
                    .find("option")
                    .each(function () {
                        const optionValue = $(this).val();

                        if (!optionValue) {
                            return;
                        }

                        const disabled =
                            selectedNames.includes(optionValue) &&
                            optionValue !== currentValue;

                        $(this).prop("disabled", disabled);
                    });
            });
        }

        // ========================================================
        // APPLY BUTTON
        // ========================================================

        function updateApplyButton() {
            const itemCount = $('[data-repeater-list="Options"]').children(
                "[data-repeater-item]",
            ).length;

            const $button = elements.btnApplyAll
                ? $(elements.btnApplyAll)
                : $("#btn-apply-all");

            if (!$button.length) {
                return;
            }

            if (itemCount > 0) {
                $button.removeClass("d-none");
            } else {
                $button.addClass("d-none");
            }
        }

        // ========================================================
        // GENERATE COMBINATIONS
        // ========================================================

        function generateCombinations(options) {
            let combinations = [{}];

            options.forEach(function (option) {
                const next = [];

                combinations.forEach(function (combination) {
                    option.values.forEach(function (value) {
                        next.push({
                            ...combination,

                            [option.name]: value,
                        });
                    });
                });

                combinations = next;
            });

            return combinations;
        }

        // ========================================================
        // VARIANT KEY
        // ========================================================

        function getVariantKey(options) {
            return Object.keys(options)
                .sort()
                .map(function (key) {
                    return `${key}:${options[key]}`;
                })
                .join("|");
        }

        // ========================================================
        // IMAGE GROUP KEY
        // ========================================================

        function getImageKey(options) {
            const firstOption = productVariantState.options[0];

            if (!firstOption) {
                return "";
            }

            const value = options[firstOption.name];

            if (value === undefined || value === null || value === "") {
                return "";
            }

            return `${firstOption.name}:${value}`;
        }

        // ========================================================
        // GENERATE VARIANTS
        // ========================================================

        function generateVariants() {
            const options = syncOptionsFromForm();

            // ====================================================
            // NO OPTIONS
            // ====================================================

            if (options.length === 0) {
                productVariantState.variants = [];

                cleanupRemovedImageGroups([]);

                renderVariantTable([]);

                return;
            }

            // ====================================================
            // INVALID OPTION
            // ====================================================

            const invalidOption = options.find(function (option) {
                return option.values.length === 0;
            });

            if (invalidOption) {
                return;
            }

            // ====================================================
            // COMBINATIONS
            // ====================================================

            const combinations = generateCombinations(options);

            const oldVariants = productVariantState.variants;

            // ====================================================
            // CREATE
            // ====================================================

            console.log('CREATE VARIANTS');
            const variants = combinations.map(function (combination) {
                const key = getVariantKey(combination);

                const imageKey = getImageKey(combination);

                const oldVariant = oldVariants.find(function (variant) {
                    return variant.key === key;
                });

                // ========================================
                // PRESERVE OLD
                // ========================================

                if (oldVariant) {
                    oldVariant.options = combination;

                    oldVariant.imageKey = imageKey;

                    oldVariant.image = getImageGroupImage(imageKey);
                    console.log('variant old');
                    return oldVariant;
                }
                // ========================================
                // NEW
                // ========================================

                return {
                    key: key,

                    options: combination,

                    imageKey: imageKey,

                    price: "",

                    stock: "",

                    sku: "",

                    image: getImageGroupImage(imageKey),
                };
            });
            console.log('varaint data');
            console.log(variants);
            productVariantState.variants = variants;

            cleanupRemovedImageGroups(variants);

            renderVariantTable(variants);
        }

        // ========================================================
        // IMAGE GROUP IMAGE
        // ========================================================

        function getImageGroupImage(imageKey) {
            if (!imageKey) {
                return null;
            }

            const group = variantImageGroups.get(imageKey);

            return group?.image || null;
        }

        // ========================================================
        // ENSURE IMAGE GROUP
        // ========================================================

        function ensureImageGroup(imageKey) {
            if (!imageKey) {
                return null;
            }

            let group = variantImageGroups.get(imageKey);

            if (!group) {
                group = {
                    image: null,
                };

                variantImageGroups.set(imageKey, group);
            }

            return group;
        }

        // ========================================================
        // CLEANUP IMAGE GROUPS
        // ========================================================
        // =========================================================
        // CLEANUP REMOVED IMAGE GROUPS
        // =========================================================

        function cleanupRemovedImageGroups(variants) {
            const currentImageKeys = new Set();

            variants.forEach(function (variant) {
                if (variant.imageKey) {
                    currentImageKeys.add(variant.imageKey);
                }
            });

            const removedKeys = [];

            variantImageGroups.forEach(function (group, imageKey) {
                if (!currentImageKeys.has(imageKey)) {
                    removedKeys.push(imageKey);
                }
            });

            removedKeys.forEach(function (imageKey) {
                destroyVariantFilePond(imageKey);

                variantImageGroups.delete(imageKey);
            });
        }

        // =========================================================
        // DESTROY ONE VARIANT FILEPOND
        // =========================================================

        function destroyVariantFilePond(imageKey) {
            if (!imageKey) {
                return;
            }

            const variantPond = variantFilePonds.get(imageKey);

            if (!variantPond) {
                return;
            }

            try {
                variantPond.destroy();
            } catch (error) {
                console.warn("Destroy variant FilePond error:", error);
            }

            variantFilePonds.delete(imageKey);
        }

        // ========================================================
        // RENDER VARIANT TABLE
        // ========================================================

        function renderVariantTable(variants) {
            const $table = elements.variantsTable
                ? $(elements.variantsTable)
                : $("#product-variants-table");

            if (!$table.length) {
                return;
            }

            cleanupRemovedImageGroups(variants);

            // ====================================================
            // DESTROY DATATABLE
            // ====================================================

            if ($.fn.DataTable.isDataTable($table[0])) {
                $table.DataTable().clear().destroy();
            }

            variantDataTable = null;

            // ====================================================
            // CLEAR
            // ====================================================

            $table.find("thead").empty();

            $table.find("tbody").empty();

            // ====================================================
            // EMPTY
            // ====================================================

            if (!variants || variants.length === 0) {
                return;
            }

            // ====================================================
            // COLUMNS
            // ====================================================

            const columns = [];

            // ====================================================
            // OPTION COLUMNS
            // ====================================================
            console.log("productVariantState.options");
            console.log(productVariantState.options);
            productVariantState.options.forEach(function (option, optionIndex) {
                columns.push({
                    title: option.name,

                    data: null,

                    orderable: false,

                    searchable: false,

                    className: `variant-option-cell variant-option-${optionIndex}`,

                    render: function (data, type, row, meta) {
                        const value = row.options[option.name] ?? "";

                        if (optionIndex === 0) {
                            const isFirstRow = isFirstRowOfOptionGroupByIndex(
                                meta.row,
                                variants,
                            );

                            if (isFirstRow) {
                                const imageKey = row.imageKey;

                                const encodedImageKey = encodeURIComponent(imageKey);

                                return `
                                            <div class="variant-option-wrapper">

                                                <div class="variant-option-value">
                                                    ${escapeHtml(value)}
                                                </div>

                                                <div
                                                    class="variant-option-image variant-filepond-container"
                                                    data-image-key="${escapeAttribute(
                                    encodedImageKey,
                                )}">
                                                </div>

                                            </div>
                                        `;
                            }

                            return escapeHtml(value);
                        }

                        return `
                                    <div class="variant-option-value-only">
                                        ${escapeHtml(value)}
                                    </div>
                                `;
                    },
                });
            });

            // ====================================================
            // PRICE
            // ====================================================

            columns.push({
                title: "Price",

                data: null,

                orderable: false,

                searchable: false,

                className: "variant-price-cell",

                render: function (data, type, row) {
                    return `
                            <input
                                type="number"
                                class="form-control variant-price"
                                data-variant-key="${escapeAttribute(row.key)}"
                                value="${escapeAttribute(row.price)}"
                                min="0"
                                step="1000"
                                placeholder="Price">
                        `;
                },
            });

            // ====================================================
            // STOCK
            // ====================================================

            columns.push({
                title: "STOCK",

                data: null,

                orderable: false,

                searchable: false,

                className: "variant-stock-cell",

                render: function (data, type, row) {
                    return `
                            <input
                                type="number"
                                class="form-control variant-stock"
                                data-variant-key="${escapeAttribute(row.key)}"
                                value="${escapeAttribute(row.stock)}"
                                min="0"
                                step="1"
                                placeholder="Stock">
                        `;
                },
            });

            // ====================================================
            // SKU
            // ====================================================

            columns.push({
                title: "SKU",

                data: null,

                orderable: false,

                searchable: false,

                className: "variant-sku-cell",

                render: function (data, type, row) {
                    return `
                            <input
                                type="text"
                                class="form-control variant-sku"
                                data-variant-key="${escapeAttribute(row.key)}"
                                value="${escapeAttribute(row.sku)}"
                                placeholder="SKU">
                        `;
                },
            });

            // ====================================================
            // DATATABLE
            // ====================================================

            variantDataTable = $table.DataTable({
                data: variants,

                columns: columns,

                paging: false,

                searching: false,

                ordering: false,

                info: false,

                lengthChange: false,

                autoWidth: false,

                responsive: false,

                scrollX: false,

                destroy: true,

                drawCallback: function () {
                    const api = this.api();

                    /*
                     * Không destroy FilePond.
                     *
                     * Chỉ move DOM.
                     */

                    initializeVariantFilePonds();

                    mergeVariantOptionCells(api);
                },
            });
        }

        // ========================================================
        // MERGE OPTION CELLS
        // ========================================================

        function mergeVariantOptionCells(api) {
            const rows = api
                .rows({
                    page: "current",
                })
                .nodes();

            if (!rows || rows.length === 0) {
                return;
            }

            const optionCount = productVariantState.options.length;

            for (let optionIndex = optionCount - 1; optionIndex >= 0; optionIndex--) {
                mergeOptionColumn(api, rows, optionIndex);
            }
        }

        // ========================================================
        // MERGE ONE OPTION COLUMN
        // ========================================================

        function mergeOptionColumn(api, rows, optionIndex) {
            const cellSelector = `.variant-option-${optionIndex}`;

            let groupStart = 0;

            let previousKey = null;

            for (let rowIndex = 0; rowIndex < rows.length; rowIndex++) {
                const rowData = api.row(rows[rowIndex]).data();

                if (!rowData) {
                    continue;
                }

                const groupValues = [];

                for (let i = 0; i <= optionIndex; i++) {
                    const option = productVariantState.options[i];

                    groupValues.push(rowData.options[option.name] ?? "");
                }

                const currentKey = JSON.stringify(groupValues);

                if (previousKey !== null && currentKey !== previousKey) {
                    finalizeRowSpanBySelector(
                        rows,
                        cellSelector,
                        groupStart,
                        rowIndex - 1,
                    );

                    groupStart = rowIndex;
                }

                previousKey = currentKey;
            }

            finalizeRowSpanBySelector(
                rows,
                cellSelector,
                groupStart,
                rows.length - 1,
            );
        }

        // ========================================================
        // FIRST ROW OF OPTION GROUP
        // ========================================================

        function isFirstRowOfOptionGroupByIndex(rowIndex, variants) {
            const firstOption = productVariantState.options[0];

            if (!firstOption) {
                return false;
            }

            if (rowIndex === 0) {
                return true;
            }

            const currentVariant = variants[rowIndex];

            const previousVariant = variants[rowIndex - 1];

            if (!currentVariant || !previousVariant) {
                return false;
            }

            const currentValue = currentVariant.options[firstOption.name] ?? "";

            const previousValue = previousVariant.options[firstOption.name] ?? "";

            return currentValue !== previousValue;
        }

        // ========================================================
        // FINALIZE ROWSPAN
        // ========================================================

        function finalizeRowSpanBySelector(rows, selector, startIndex, endIndex) {
            const rowspan = endIndex - startIndex + 1;

            if (rowspan <= 1) {
                return;
            }

            const firstCell = $(rows[startIndex]).find(selector);

            if (!firstCell.length) {
                return;
            }

            firstCell.attr("rowspan", rowspan);

            for (let i = startIndex + 1; i <= endIndex; i++) {
                $(rows[i]).find(selector).remove();
            }
        }

        // ========================================================
        // VARIANT FILEPOND
        // ========================================================

        function initializeVariantFilePonds() {
            const $table = elements.variantsTable
                ? $(elements.variantsTable)
                : $("#product-variants-table");

            if (!$table.length) {
                return;
            }

            $table.find(".variant-filepond-container").each(function () {
                const container = this;

                const encodedImageKey = $(container).attr("data-image-key");

                if (!encodedImageKey) {
                    return;
                }

                const imageKey = decodeURIComponent(encodedImageKey);

                if (!imageKey) {
                    return;
                }

                const variant = productVariantState.variants.find(function (item) {
                    return item.imageKey === imageKey;
                });

                if (!variant) {
                    return;
                }

                const existingPond = variantFilePonds.get(imageKey);

                if (existingPond) {
                    const pondElement = existingPond.element;

                    if (pondElement && pondElement.parentNode !== container) {
                        container.appendChild(pondElement);
                    }

                    return;
                }

                createVariantFilePond(container, imageKey, variant);
            });
        }

        // ========================================================
        // CREATE VARIANT FILEPOND
        // ========================================================

        function createVariantFilePond(container, imageKey, variant) {
            if (!container) {
                return;
            }

            if (!imageKey) {
                return;
            }

            if (variantFilePonds.has(imageKey)) {
                return;
            }

            const input = document.createElement("input");

            input.type = "file";

            input.accept = "image/png,image/jpeg,image/webp";

            //container.innerHTML = "";

            container.appendChild(input);

            const image = variant?.image ?? null;

            const files = image?.url
                ? [
                    {
                        source: image.url,

                        options: {
                            type: "local",

                            metadata: {
                                mediaId: image.id,

                                isExisting: true,
                            },
                        },
                    },
                ]
                : [];
            const variantPond = FilePond.create(input, {
                allowMultiple: false,

                files: files,

                maxFiles: 1,

                allowReplaceItem: true,

                acceptedFileTypes: ["image/png", "image/jpeg", "image/webp"],

                labelIdle: 'Kéo ảnh vào hoặc <span class="filepond--label-action">Chọn ảnh</span>',

                allowProcess: false,

                instantUpload: false,

                allowImagePreview: true,

                imagePreviewHeight: 70,

                imagePreviewMinHeight: 70,

                imagePreviewMaxHeight: 70,

                stylePanelLayout: "compact",

                styleButtonRemoveItemPosition: "left",

                styleLoadIndicatorPosition: "center",

                credits: false,

                fileInfo: false,

                server: {
                    load: function (source, load, error) {
                        console.log("========== VARIANT IMAGE LOAD ==========");

                        console.log("imageKey:", imageKey);

                        console.log("source:", source);

                        /*
                         * Backend trả:
                         *
                         * /products/2026/08/xxx.png
                         *
                         * FilePond cần load:
                         *
                         * /uploads/products/2026/08/xxx.png
                         */

                        const normalizedSource = String(source).startsWith("/")
                            ? source
                            : `/${source}`;

                        const url = new URL(
                            `/uploads${normalizedSource}`,
                            window.location.origin,
                        ).href;

                        console.log("VARIANT IMAGE URL:", url);

                        fetch(url)
                            .then(function (response) {
                                console.log("HTTP:", response.status, response.statusText);

                                if (!response.ok) {
                                    throw new Error(`HTTP ${response.status}`);
                                }

                                return response.blob();
                            })
                            .then(function (blob) {
                                load(blob);
                            })
                            .catch(function (err) {
                                console.error("VARIANT IMAGE LOAD ERROR:", err);

                                error(err.message);
                            });
                    },
                },
            });
            variantFilePonds.set(imageKey, variantPond);

            // ========================================
            // ADD
            // ========================================

            variantPond.on("addfile", function (error, fileItem) {
                if (error || !fileItem?.file) {
                    return;
                }

                const group = ensureImageGroup(imageKey);

                if (!group) {
                    return;
                }
                const imageData = {
                    id: image.id,
                    url: null,
                    file: fileItem.file,
                    name:
                        fileItem.filename ??
                        fileItem.file.name ??
                        "",
                    size:
                        fileItem.fileSize ??
                        fileItem.file.size ??
                        0,
                    type:
                        fileItem.fileType ??
                        fileItem.file.type ??
                        "image/*",
                    serverId: null,
                };


                group.image = imageData;

                syncVariantImagesFromImageGroup(
                    imageKey,
                );
            });

            // ========================================
            // REMOVE
            // ========================================

            variantPond.on("removefile", function (error, fileItem) {
                if (error || !fileItem) {
                    return;
                }

                const group =
                    variantImageGroups.get(
                        imageKey,
                    );

                if (group) {
                    group.image = null;
                }

                const currentVariant =
                    productVariantState.variants.find(
                        function (item) {
                            return item.imageKey === imageKey;
                        },
                    );

                if (currentVariant) {
                    currentVariant.image = null;
                }
            });
        }

        // ========================================================
        // RESTORE VARIANT IMAGES
        // ========================================================

        async function restoreVariantImages(variantImages = []) {
            if (!Array.isArray(variantImages)) {
                return;
            }

            for (const item of variantImages) {

                const imageKey = item.key ?? item.Key ?? "";

                if (!imageKey) {
                    continue;
                }

                const url = item.url ?? item.Url ?? null;

                if (!url) {
                    continue;
                }

                const image = {
                    id: item.id ?? item.Id ?? null,

                    url: url,

                    file: null,

                    name: item.name ?? item.Name ?? "",

                    size: item.size ?? item.Size ?? 0,

                    type:
                        item.type ??
                        item.Type ??
                        item.contentType ??
                        item.ContentType ??
                        "image/*",

                    serverId: item.id ?? item.Id ?? null,
                };

                /*
                 * Lưu ảnh vào group theo imageKey.
                 */
                const group = ensureImageGroup(imageKey);

                if (!group) {
                    continue;
                }

                group.image = image;

                /*
                 * Quan trọng:
                 * Gắn image vào đúng variant
                 * có cùng imageKey.
                 */
                syncVariantImagesFromImageGroup(imageKey);
            }
        }

        // ========================================================
        // SYNC VARIANT IMAGES
        // ========================================================

        function syncVariantImagesFromImageGroup(imageKey) {
            const group = variantImageGroups.get(imageKey);

            const image = group?.image ?? null;

            productVariantState.variants.forEach(function (variant) {
                if (variant.imageKey === imageKey) {
                    variant.image = image;
                }
            });
        }

        // ========================================================
        // RESTORE VARIANTS
        // ========================================================

        function restoreVariants(variants) {
            if (!Array.isArray(variants)) {
                return;
            }

            productVariantState.variants = variants.map(function (variant) {
                const options = variant.options ?? variant.Options ?? {};

                const imageKey = getImageKey(options);

                return {
                    key: variant.key ?? variant.Key ?? getVariantKey(options),

                    id: variant.id ?? variant.Id ?? null,

                    options: options,

                    imageKey: imageKey,

                    price: variant.price ?? variant.Price ?? "",

                    stock:
                        variant.stock ??
                        variant.Stock ??
                        variant.availableQuantity ??
                        variant.AvailableQuantity ??
                        "",

                    sku: variant.sku ?? variant.SKU ?? "",

                    barcode: variant.barcode ?? variant.Barcode ?? "",

                    image: getImageGroupImage(imageKey),
                };
            });
            console.log('restoreVariants');
            console.log(productVariantState.variants);

        }

        // ========================================================
        // RESTORE PRODUCT DATA
        // ========================================================

        async function restoreProductData(mode, data) {
            /*
             * CREATE
             *
             * Không cần restore gì.
             */
            if (mode === "create") {

                productImages = [];

                productVariantState.options = [];

                productVariantState.variants = [];

                variantImageGroups.clear();

                return;
            }

            /*
             * UPDATE
             */

            // await restoreProductImages(data.images);

            await restoreOptions(data.options);

            /*
             * Options đã được restore.
             * Bây giờ sync lại options.
             */

            syncOptionsFromForm();

            /*
             * Restore image groups
             * trước khi render variant.
             */

            /*
             * 2. Sau khi variants tồn tại
             *    mới gắn variant images
             */
            await restoreVariantImages(data.variantImages);
            restoreVariants(data.variants);

            /*
             * Nếu DB có variants,
             * ưu tiên thứ tự theo options hiện tại.
             */

            if (productVariantState.options.length) {
                regenerateUsingExistingVariants();
            } else {
                renderVariantTable(productVariantState.variants);
            }

            /*
             * Description
             */

            if (quill && data.description) {
                quill.root.innerHTML = data.description;
            }

            /*
             * Update image groups
             * sau khi table đã render.
             */

            setTimeout(initializeVariantFilePonds(), 100);
        }

        // ========================================================
        // REGENERATE WITH EXISTING VARIANTS
        // ========================================================

        function regenerateUsingExistingVariants() {
            const options = syncOptionsFromForm();

            if (options.length === 0) {
                renderVariantTable(productVariantState.variants);

                return;
            }

            const invalidOption = options.find(function (option) {
                return option.values.length === 0;
            });

            if (invalidOption) {
                return;
            }

            const combinations = generateCombinations(options);

            const oldVariants = productVariantState.variants;

            const variants = combinations.map(function (combination) {
                const key = getVariantKey(combination);

                const imageKey = getImageKey(combination);

                const oldVariant = oldVariants.find(function (item) {
                    return item.key === key;
                });

                if (oldVariant) {
                    return {
                        ...oldVariant,

                        key: key,

                        options: combination,

                        imageKey: imageKey,

                        image: getImageGroupImage(imageKey),
                    };
                }

                return {
                    key: key,

                    options: combination,

                    imageKey: imageKey,

                    price: "",

                    stock: "",

                    sku: "",

                    image: getImageGroupImage(imageKey),
                };
            });

            productVariantState.variants = variants;

            cleanupRemovedImageGroups(variants);

            renderVariantTable(variants);
        }

        // ========================================================
        // GLOBAL EVENTS
        // ========================================================

        function initializeGlobalEvents() {
            // ====================================================
            // OPTION NAME CHANGE
            // ====================================================

            $(document)
                .off("change.productEditor", ".option-name")
                .on("change.productEditor", ".option-name", function () {
                    updateOptionNames();

                    updateApplyButton();

                    setTimeout(generateVariants, 0);
                });

            // ====================================================
            // PRICE
            // ====================================================

            $(document)
                .off("input.productEditor", ".variant-price")
                .on("input.productEditor", ".variant-price", function () {
                    const key = $(this).attr("data-variant-key");

                    const variant = productVariantState.variants.find(function (item) {
                        return item.key === key;
                    });

                    if (!variant) {
                        return;
                    }

                    variant.price = $(this).val();
                });

            // ====================================================
            // STOCK
            // ====================================================

            $(document)
                .off("input.productEditor", ".variant-stock")
                .on("input.productEditor", ".variant-stock", function () {
                    const key = $(this).attr("data-variant-key");

                    const variant = productVariantState.variants.find(function (item) {
                        return item.key === key;
                    });

                    if (!variant) {
                        return;
                    }

                    variant.stock = $(this).val();
                });

            // ====================================================
            // SKU
            // ====================================================

            $(document)
                .off("input.productEditor", ".variant-sku")
                .on("input.productEditor", ".variant-sku", function () {
                    const key = $(this).attr("data-variant-key");

                    const variant = productVariantState.variants.find(function (item) {
                        return item.key === key;
                    });

                    if (!variant) {
                        return;
                    }

                    variant.sku = $(this).val();
                });

            // ====================================================
            // APPLY ALL
            // ====================================================

            $(document)
                .off("click.productEditor", "#btn-apply-all")
                .on("click.productEditor", "#btn-apply-all", function () {
                    const price = elements.variantDefaultPrice
                        ? $(elements.variantDefaultPrice).val()
                        : $("#Price").val();

                    const stock = elements.variantDefaultStock
                        ? $(elements.variantDefaultStock).val()
                        : $("#Stock").val();

                    productVariantState.variants.forEach(function (variant) {
                        variant.price = price;

                        variant.stock = stock;
                    });

                    $(".variant-price").each(function () {
                        const key = $(this).attr("data-variant-key");

                        const variant = productVariantState.variants.find(function (item) {
                            return item.key === key;
                        });

                        if (variant) {
                            $(this).val(variant.price);
                        }
                    });

                    $(".variant-stock").each(function () {
                        const key = $(this).attr("data-variant-key");

                        const variant = productVariantState.variants.find(function (item) {
                            return item.key === key;
                        });

                        if (variant) {
                            $(this).val(variant.stock);
                        }
                    });
                });
        }

        // ========================================================
        // GET STATE
        // ========================================================

        function getState() {
            const tags = syncProductTags();

            syncOptionsFromForm();

            return {
                mode: mode,

                productImages: productImages,

                tags: tags,

                options: productVariantState.options,

                variants: productVariantState.variants,

                variantImageGroups: variantImageGroups,

                variantFilePonds: variantFilePonds,

                description: quill ? quill.root.innerHTML : "",
            };
        }

        // ========================================================
        // GET PRODUCT IMAGES
        // ========================================================

        function getProductImages() {
            return productImages;
        }

        // ========================================================
        // GET TAGS
        // ========================================================

        function getTags() {
            syncProductTags();

            return productTags;
        }

        // ========================================================
        // GET OPTIONS
        // ========================================================

        function getOptions() {
            syncOptionsFromForm();

            return productVariantState.options;
        }

        // ========================================================
        // GET VARIANTS
        // ========================================================

        function getVariants() {
            return productVariantState.variants;
        }

        // ========================================================
        // GET VARIANT IMAGE GROUPS
        // ========================================================

        function getVariantImageGroups() {
            return variantImageGroups;
        }

        // ========================================================
        // GET PRODUCT IMAGE POND
        // ========================================================

        function getProductImagePond() {
            return productImagePond;
        }

        // ========================================================
        // GET VARIANT FILE PONDS
        // ========================================================

        function getVariantFilePonds() {
            return variantFilePonds;
        }

        // ========================================================
        // GET QUILL
        // ========================================================

        function getQuill() {
            return quill;
        }

        // ========================================================
        // DESTROY
        // ========================================================

        function destroy() {
            /*
             * Cropper
             */

            closeCropper();

            /*
             * Product Image FilePond
             */

            if (productImagePond) {
                try {
                    productImagePond.destroy();
                } catch (error) {
                    console.warn("Destroy product FilePond error:", error);
                }

                productImagePond = null;
            }

            /*
             * Variant FilePond
             */

            destroyAllVariantFilePonds();

            /*
             * Option repeater items
             */

            $('[data-repeater-list="Options"]')
                .children("[data-repeater-item]")
                .each(function () {
                    destroyOptionItem($(this));
                });

            /*
             * Product Tagify
             */

            if (productTags) {
                try {
                    /*
                     * productTags đôi khi là array
                     * sau sync.
                     *
                     * Instance thực tế lưu trên input.
                     */
                } catch (error) {
                    console.warn("Destroy product Tagify error:", error);
                }
            }

            if (elements.tags && elements.tags._productTagify) {
                try {
                    elements.tags._productTagify.destroy();
                } catch (error) {
                    console.warn("Destroy product Tagify error:", error);
                }

                elements.tags._productTagify = null;
            }

            /*
             * DataTable
             */

            if (
                elements.variantsTable &&
                $.fn.DataTable.isDataTable(elements.variantsTable)
            ) {
                $(elements.variantsTable).DataTable().clear().destroy();
            }

            variantDataTable = null;

            /*
             * State
             */

            productImages = [];

            productVariantState.options = [];

            productVariantState.variants = [];

            variantImageGroups.clear();

            variantFilePonds.clear();

            productTags = null;

            quill = null;

            initialized = false;
        }

        // ========================================================
        // NEXT TICK
        // ========================================================

        function nextTick() {
            return new Promise(function (resolve) {
                setTimeout(resolve, 0);
            });
        }

        // ========================================================
        // ESCAPE HTML
        // ========================================================

        function escapeHtml(value) {
            const div = document.createElement("div");

            div.textContent = value ?? "";

            return div.innerHTML;
        }

        // ========================================================
        // ESCAPE ATTRIBUTE
        // ========================================================

        function escapeAttribute(value) {
            return escapeHtml(value);
        }

        // ========================================================
        // PUBLIC API
        // ========================================================

        const api = {
            init,

            destroy,

            getState,

            getProductImages,

            getProductImagePond,

            getTags,

            getOptions,

            getVariants,

            getVariantImageGroups,

            getVariantFilePonds,

            getQuill,

            generateVariants,

            syncOptionsFromForm,

            updateOptionNames,

            updateApplyButton,

            loadProductCategories,

            loadAttributes,
        };

        return api;
    })();

    // ============================================================
    // EXPORT
    // ============================================================

    window.ProductEditor = ProductEditor;
})(window, jQuery);
