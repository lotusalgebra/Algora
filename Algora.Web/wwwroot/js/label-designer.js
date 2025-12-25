/**
 * Label Designer - Toolbar-based Implementation
 * Clean toolbar interface with Add Element dropdown
 */

class LabelDesigner {
    constructor() {
        this.fields = [];
        this.selectedField = null;
        this.templateId = null;
        this.previewData = null;
        this.canvasWidth = 252;
        this.canvasHeight = 96;
        this.pendingImageData = null;

        this.init();
    }

    init() {
        this.canvas = document.getElementById('labelCanvas');
        this.initializeEventListeners();
        this.initializeCanvasInteractions();
        this.updateCanvasSize();
    }

    // Canvas size management
    updateCanvasSize() {
        const labelType = document.getElementById('labelType');
        const selected = labelType.options[labelType.selectedIndex];

        let widthInches, heightInches;

        if (labelType.value === 'Custom') {
            widthInches = parseFloat(document.getElementById('customWidth').value) || 2;
            heightInches = parseFloat(document.getElementById('customHeight').value) || 1;
            document.getElementById('customSizeInputs').classList.remove('hidden');
        } else {
            widthInches = parseFloat(selected.dataset.width) || 2.625;
            heightInches = parseFloat(selected.dataset.height) || 1;
            document.getElementById('customSizeInputs').classList.add('hidden');
        }

        const scale = 96;
        this.canvasWidth = widthInches * scale;
        this.canvasHeight = heightInches * scale;

        this.canvas.style.width = `${this.canvasWidth}px`;
        this.canvas.style.height = `${this.canvasHeight}px`;

        document.getElementById('canvasSize').textContent = `${widthInches}" × ${heightInches}"`;
        this.renderAllFields();
    }

    // Canvas field interactions (drag and resize)
    initializeCanvasInteractions() {
        const self = this;

        interact('.canvas-field')
            .draggable({
                inertia: false,
                modifiers: [
                    interact.modifiers.restrictRect({
                        restriction: 'parent',
                        endOnly: true
                    })
                ],
                listeners: {
                    move(event) {
                        const target = event.target;
                        const x = (parseFloat(target.dataset.x) || 0) + event.dx;
                        const y = (parseFloat(target.dataset.y) || 0) + event.dy;

                        target.style.transform = `translate(${x}px, ${y}px)`;
                        target.dataset.x = x;
                        target.dataset.y = y;
                    },
                    end(event) {
                        self.updateFieldPosition(event.target.dataset.fieldId);
                    }
                }
            })
            .resizable({
                edges: { left: true, right: true, bottom: true, top: true },
                modifiers: [
                    interact.modifiers.restrictSize({
                        min: { width: 20, height: 15 }
                    }),
                    interact.modifiers.restrictEdges({
                        outer: 'parent'
                    })
                ],
                listeners: {
                    move(event) {
                        const target = event.target;

                        target.style.width = `${event.rect.width}px`;
                        target.style.height = `${event.rect.height}px`;

                        let x = parseFloat(target.dataset.x) || 0;
                        let y = parseFloat(target.dataset.y) || 0;

                        x += event.deltaRect.left;
                        y += event.deltaRect.top;

                        target.style.transform = `translate(${x}px, ${y}px)`;
                        target.dataset.x = x;
                        target.dataset.y = y;
                    },
                    end(event) {
                        self.updateFieldSize(event.target.dataset.fieldId, event.rect.width, event.rect.height);
                    }
                }
            });
    }

    // Add element from dropdown
    addElement(elementType) {
        if (elementType === 'image') {
            this.showImageModal();
            return;
        }

        const dataBinding = document.getElementById('dataBinding').value;
        const fieldType = elementType === 'barcode' ? 'Barcode' :
                          (dataBinding === 'custom' ? 'CustomText' : dataBinding);

        this.addFieldToCanvas(fieldType);
    }

    // Field management
    addFieldToCanvas(fieldType, options = {}) {
        const fieldId = `field_${Date.now()}`;
        const field = {
            id: fieldId,
            fieldType: fieldType,
            x: options.x ?? 10,
            y: options.y ?? 10,
            width: fieldType === 'Barcode' ? 40 : (fieldType === 'Image' ? 25 : 30),
            height: fieldType === 'Barcode' ? 30 : (fieldType === 'Image' ? 25 : 18),
            fontFamily: 'Arial',
            fontSize: fieldType === 'ProductTitle' ? 14 : 12,
            isBold: fieldType === 'ProductTitle' || fieldType === 'Price',
            isItalic: false,
            isUnderline: false,
            textAlign: 'left',
            textColor: '#000000',
            textTransform: 'normal',
            customText: fieldType === 'CustomText' ? document.getElementById('elementText').value || 'Custom Text' : null,
            barcodeFormat: 'Code128',
            imageData: options.imageData || null
        };

        this.fields.push(field);
        this.renderField(field);
        this.selectField(fieldId);
    }

    renderAllFields() {
        const existingFields = this.canvas.querySelectorAll('.canvas-field');
        existingFields.forEach(f => f.remove());

        this.fields.forEach(field => this.renderField(field));
        this.initializeCanvasInteractions();
    }

    renderField(field) {
        const element = document.createElement('div');
        element.id = field.id;
        element.dataset.fieldId = field.id;
        element.className = 'canvas-field';

        const x = (field.x / 100) * this.canvasWidth;
        const y = (field.y / 100) * this.canvasHeight;
        const width = (field.width / 100) * this.canvasWidth;
        const height = (field.height / 100) * this.canvasHeight;

        element.style.transform = `translate(${x}px, ${y}px)`;
        element.style.width = `${width}px`;
        element.style.height = `${height}px`;
        element.dataset.x = x;
        element.dataset.y = y;

        // Apply text styles
        element.style.fontFamily = field.fontFamily;
        element.style.fontSize = `${field.fontSize}px`;
        element.style.fontWeight = field.isBold ? 'bold' : 'normal';
        element.style.fontStyle = field.isItalic ? 'italic' : 'normal';
        element.style.textDecoration = field.isUnderline ? 'underline' : 'none';
        element.style.textAlign = field.textAlign;
        element.style.color = field.textColor;
        element.style.textTransform = field.textTransform || 'none';
        element.style.display = 'flex';
        element.style.alignItems = 'center';
        element.style.justifyContent = field.textAlign === 'center' ? 'center' : (field.textAlign === 'right' ? 'flex-end' : 'flex-start');
        element.style.overflow = 'hidden';

        element.innerHTML = this.getFieldContent(field);

        // Add 8-point resize handles
        const handles = ['nw', 'n', 'ne', 'w', 'e', 'sw', 's', 'se'];
        handles.forEach(pos => {
            const handle = document.createElement('div');
            handle.className = `resize-handle ${pos}`;
            element.appendChild(handle);
        });

        element.addEventListener('click', (e) => {
            e.stopPropagation();
            this.selectField(field.id);
        });

        this.canvas.appendChild(element);
    }

    getFieldContent(field) {
        if (field.fieldType === 'Image' && field.imageData) {
            return `<img src="${field.imageData}" style="max-width:100%;max-height:100%;object-fit:contain;" />`;
        }

        if (field.fieldType === 'Barcode') {
            return '<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;width:100%;height:100%;">' +
                   '<i class="fas fa-barcode" style="font-size:1.5em;color:#666;"></i>' +
                   '<span style="font-size:9px;color:#888;margin-top:2px;">Barcode</span></div>';
        }

        const preview = this.previewData || {};
        const labels = {
            'ProductTitle': preview.productTitle || 'Product Title',
            'SKU': preview.sku || 'SKU-12345',
            'Price': preview.price ? `$${parseFloat(preview.price).toFixed(2)}` : '$29.99',
            'CompareAtPrice': preview.compareAtPrice ? `$${parseFloat(preview.compareAtPrice).toFixed(2)}` : '$39.99',
            'VariantTitle': preview.variantTitle || 'Variant',
            'VariantOption1': preview.option1 || 'Option 1',
            'VariantOption2': preview.option2 || 'Option 2',
            'VariantOption3': preview.option3 || 'Option 3',
            'Vendor': preview.vendor || 'Vendor',
            'ProductType': preview.productType || 'Type',
            'Weight': preview.weight ? `${preview.weight}` : '1.5 kg',
            'InventoryQuantity': preview.inventoryQuantity?.toString() || '42',
            'CustomText': field.customText || 'Custom Text'
        };

        return `<span style="width:100%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">${labels[field.fieldType] || field.fieldType}</span>`;
    }

    selectField(fieldId) {
        // Deselect all
        document.querySelectorAll('.canvas-field').forEach(el => el.classList.remove('selected'));

        const element = document.getElementById(fieldId);
        if (element) {
            element.classList.add('selected');
            this.selectedField = this.fields.find(f => f.id === fieldId);
            this.showFormatToolbar();
            this.syncToolbarWithField();
        }
    }

    deselectField() {
        document.querySelectorAll('.canvas-field').forEach(el => el.classList.remove('selected'));
        this.selectedField = null;
        this.hideFormatToolbar();
    }

    syncToolbarWithField() {
        if (!this.selectedField) return;

        const field = this.selectedField;

        // Sync data binding dropdown
        const dataBinding = document.getElementById('dataBinding');
        if (field.fieldType === 'CustomText') {
            dataBinding.value = 'custom';
        } else if (field.fieldType !== 'Barcode' && field.fieldType !== 'Image') {
            dataBinding.value = field.fieldType;
        }

        // Sync text input
        if (field.fieldType === 'CustomText') {
            document.getElementById('elementText').value = field.customText || '';
        }

        // Sync color
        document.getElementById('textColor').value = field.textColor;
        document.getElementById('colorSwatch').style.background = field.textColor;

        // Sync text transform
        document.getElementById('textFormat').value = field.textTransform || 'normal';

        // Sync format toolbar
        document.getElementById('fontFamily').value = field.fontFamily;
        document.getElementById('fontSize').value = field.fontSize;

        // Style buttons
        document.getElementById('toggleBold').classList.toggle('active', field.isBold);
        document.getElementById('toggleUnderline').classList.toggle('active', field.isUnderline);
        document.getElementById('toggleItalic').classList.toggle('active', field.isItalic);

        // Alignment
        ['alignLeft', 'alignCenter', 'alignRight'].forEach(id => {
            const align = id.replace('align', '').toLowerCase();
            document.getElementById(id).classList.toggle('active', field.textAlign === align);
        });
    }

    showFormatToolbar() {
        document.getElementById('formatToolbar').classList.remove('hidden');
    }

    hideFormatToolbar() {
        document.getElementById('formatToolbar').classList.add('hidden');
    }

    updateFieldPosition(fieldId) {
        const element = document.getElementById(fieldId);
        const field = this.fields.find(f => f.id === fieldId);
        if (!element || !field) return;

        const x = parseFloat(element.dataset.x) || 0;
        const y = parseFloat(element.dataset.y) || 0;

        field.x = (x / this.canvasWidth) * 100;
        field.y = (y / this.canvasHeight) * 100;
    }

    updateFieldSize(fieldId, width, height) {
        const field = this.fields.find(f => f.id === fieldId);
        if (!field) return;

        field.width = (width / this.canvasWidth) * 100;
        field.height = (height / this.canvasHeight) * 100;
        this.updateFieldPosition(fieldId);
    }

    updateSelectedFieldStyle(property, value) {
        if (!this.selectedField) return;

        this.selectedField[property] = value;
        const element = document.getElementById(this.selectedField.id);
        if (!element) return;

        switch (property) {
            case 'fontFamily':
                element.style.fontFamily = value;
                break;
            case 'fontSize':
                element.style.fontSize = `${value}px`;
                break;
            case 'isBold':
                element.style.fontWeight = value ? 'bold' : 'normal';
                break;
            case 'isItalic':
                element.style.fontStyle = value ? 'italic' : 'normal';
                break;
            case 'isUnderline':
                element.style.textDecoration = value ? 'underline' : 'none';
                break;
            case 'textAlign':
                element.style.textAlign = value;
                element.style.justifyContent = value === 'center' ? 'center' : (value === 'right' ? 'flex-end' : 'flex-start');
                break;
            case 'textColor':
                element.style.color = value;
                document.getElementById('colorSwatch').style.background = value;
                break;
            case 'textTransform':
                element.style.textTransform = value;
                break;
            case 'customText':
                const span = element.querySelector('span');
                if (span) span.textContent = value || 'Custom Text';
                break;
        }

        this.syncToolbarWithField();
    }

    removeSelectedField() {
        if (!this.selectedField) return;

        const element = document.getElementById(this.selectedField.id);
        if (element) element.remove();

        this.fields = this.fields.filter(f => f.id !== this.selectedField.id);
        this.selectedField = null;
        this.hideFormatToolbar();
    }

    clearCanvas() {
        this.fields = [];
        const existingFields = this.canvas.querySelectorAll('.canvas-field');
        existingFields.forEach(f => f.remove());
        this.selectedField = null;
        this.hideFormatToolbar();
    }

    // Image handling
    showImageModal() {
        document.getElementById('imageModal').classList.remove('hidden');
        document.getElementById('imagePreview').classList.add('hidden');
        document.getElementById('previewImg').src = '';
        this.pendingImageData = null;
    }

    hideImageModal() {
        document.getElementById('imageModal').classList.add('hidden');
    }

    handleImageSelect(file) {
        if (!file || !file.type.startsWith('image/')) return;

        const reader = new FileReader();
        reader.onload = (e) => {
            this.pendingImageData = e.target.result;
            document.getElementById('previewImg').src = this.pendingImageData;
            document.getElementById('imagePreview').classList.remove('hidden');
        };
        reader.readAsDataURL(file);
    }

    addImageToCanvas() {
        if (!this.pendingImageData) return;

        this.addFieldToCanvas('Image', { imageData: this.pendingImageData });
        this.hideImageModal();
        this.pendingImageData = null;
    }

    // Template management
    async loadTemplate(templateId) {
        if (!templateId) {
            this.clearCanvas();
            document.getElementById('templateName').value = '';
            this.templateId = null;
            return;
        }

        try {
            const response = await fetch(`?handler=Template&id=${templateId}`);
            if (!response.ok) throw new Error('Failed to load template');

            const template = await response.json();
            this.templateId = template.id;
            document.getElementById('templateName').value = template.name;
            document.getElementById('labelType').value = template.labelType;

            if (template.labelType === 'Custom') {
                document.getElementById('customWidth').value = template.customWidthInches || 2;
                document.getElementById('customHeight').value = template.customHeightInches || 1;
            }

            this.updateCanvasSize();
            this.fields = template.fields || [];
            this.renderAllFields();

            this.selectedField = null;
            this.hideFormatToolbar();

        } catch (error) {
            console.error('Error loading template:', error);
            alert('Failed to load template');
        }
    }

    async saveTemplate() {
        const name = document.getElementById('templateName').value.trim();
        if (!name) {
            alert('Please enter a template name');
            document.getElementById('templateName').focus();
            return;
        }

        if (this.fields.length === 0) {
            alert('Please add at least one element to the label');
            return;
        }

        const labelType = document.getElementById('labelType').value;
        const dto = {
            name: name,
            labelType: labelType,
            customWidthInches: labelType === 'Custom' ? parseFloat(document.getElementById('customWidth').value) : null,
            customHeightInches: labelType === 'Custom' ? parseFloat(document.getElementById('customHeight').value) : null,
            fields: this.fields,
            isDefault: false
        };

        try {
            let url = this.templateId ? '?handler=UpdateTemplate' : '?handler=SaveTemplate';
            if (this.templateId) dto.id = this.templateId;

            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });

            if (!response.ok) throw new Error('Failed to save template');

            const result = await response.json();
            this.templateId = result.id;
            alert('Template saved!');
            setTimeout(() => location.reload(), 500);

        } catch (error) {
            console.error('Error saving template:', error);
            alert('Failed to save template');
        }
    }

    async deleteTemplate() {
        if (!this.templateId) {
            alert('No template selected');
            return;
        }

        if (!confirm('Delete this template?')) return;

        try {
            const response = await fetch(`?handler=DeleteTemplate&id=${this.templateId}`, { method: 'POST' });
            if (!response.ok) throw new Error('Failed to delete template');

            alert('Template deleted');
            setTimeout(() => location.reload(), 500);

        } catch (error) {
            console.error('Error deleting template:', error);
            alert('Failed to delete template');
        }
    }

    // Preview data
    async loadPreviewData(productId, variantId) {
        try {
            const response = await fetch(`?handler=PreviewData&productId=${productId}&variantId=${variantId}`);
            if (!response.ok) throw new Error('Failed to load preview data');

            this.previewData = await response.json();
            this.renderAllFields();

        } catch (error) {
            console.error('Error loading preview data:', error);
        }
    }

    // PDF Generation
    async generatePdf() {
        const templateId = document.getElementById('printTemplateSelector').value;
        if (!templateId) {
            alert('Please select a template');
            return;
        }

        const products = [];
        document.querySelectorAll('.product-checkbox:checked').forEach(cb => {
            const row = cb.closest('tr');
            const copies = parseInt(row.querySelector('.copies-input').value) || 1;
            products.push({
                productId: parseInt(cb.dataset.productId),
                variantId: parseInt(cb.dataset.variantId),
                copies: copies
            });
        });

        if (products.length === 0) {
            alert('Please select at least one product');
            return;
        }

        try {
            const btn = document.getElementById('generatePdfBtn');
            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i> Generating...';

            const response = await fetch('?handler=GeneratePdf', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    templateId: parseInt(templateId),
                    products: products,
                    includeVariants: true
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || 'Failed to generate PDF');
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `labels-${new Date().toISOString().slice(0,10)}.pdf`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);

            document.getElementById('printModal').classList.add('hidden');

        } catch (error) {
            console.error('Error generating PDF:', error);
            alert(error.message || 'Failed to generate PDF');
        } finally {
            const btn = document.getElementById('generatePdfBtn');
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-file-pdf"></i> Generate PDF';
        }
    }

    // Event Listeners
    initializeEventListeners() {
        const self = this;

        // Add Element dropdown
        const addBtn = document.getElementById('addElementBtn');
        const addMenu = document.getElementById('addElementMenu');

        addBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            addMenu.classList.toggle('show');
        });

        document.querySelectorAll('.add-element-item').forEach(item => {
            item.addEventListener('click', () => {
                self.addElement(item.dataset.element);
                addMenu.classList.remove('show');
            });
        });

        // Close menu on outside click
        document.addEventListener('click', () => {
            addMenu.classList.remove('show');
        });

        // Data binding change
        document.getElementById('dataBinding').addEventListener('change', (e) => {
            if (this.selectedField && this.selectedField.fieldType !== 'Barcode' && this.selectedField.fieldType !== 'Image') {
                // Update selected field type
                const newType = e.target.value === 'custom' ? 'CustomText' : e.target.value;
                this.selectedField.fieldType = newType;

                const element = document.getElementById(this.selectedField.id);
                if (element) {
                    element.innerHTML = this.getFieldContent(this.selectedField);
                    // Re-add handles
                    const handles = ['nw', 'n', 'ne', 'w', 'e', 'sw', 's', 'se'];
                    handles.forEach(pos => {
                        const handle = document.createElement('div');
                        handle.className = `resize-handle ${pos}`;
                        element.appendChild(handle);
                    });
                }
            }
        });

        // Text input for custom text
        document.getElementById('elementText').addEventListener('input', (e) => {
            if (this.selectedField && this.selectedField.fieldType === 'CustomText') {
                this.updateSelectedFieldStyle('customText', e.target.value);
            }
        });

        // Color picker
        document.getElementById('textColor').addEventListener('input', (e) => {
            this.updateSelectedFieldStyle('textColor', e.target.value);
        });

        // Text format (transform)
        document.getElementById('textFormat').addEventListener('change', (e) => {
            this.updateSelectedFieldStyle('textTransform', e.target.value);
        });

        // Delete element
        document.getElementById('deleteElement').addEventListener('click', () => {
            this.removeSelectedField();
        });

        // Format toolbar
        document.getElementById('fontFamily').addEventListener('change', (e) => {
            this.updateSelectedFieldStyle('fontFamily', e.target.value);
        });

        document.getElementById('fontSize').addEventListener('change', (e) => {
            this.updateSelectedFieldStyle('fontSize', parseInt(e.target.value));
        });

        document.getElementById('toggleBold').addEventListener('click', () => {
            if (this.selectedField) {
                this.updateSelectedFieldStyle('isBold', !this.selectedField.isBold);
            }
        });

        document.getElementById('toggleUnderline').addEventListener('click', () => {
            if (this.selectedField) {
                this.updateSelectedFieldStyle('isUnderline', !this.selectedField.isUnderline);
            }
        });

        document.getElementById('toggleItalic').addEventListener('click', () => {
            if (this.selectedField) {
                this.updateSelectedFieldStyle('isItalic', !this.selectedField.isItalic);
            }
        });

        ['alignLeft', 'alignCenter', 'alignRight'].forEach(id => {
            document.getElementById(id).addEventListener('click', () => {
                const align = id.replace('align', '').toLowerCase();
                this.updateSelectedFieldStyle('textAlign', align);
            });
        });

        // Canvas click to deselect
        this.canvas.addEventListener('click', (e) => {
            if (e.target === this.canvas) {
                this.deselectField();
            }
        });

        // Template selector
        document.getElementById('templateSelector').addEventListener('change', (e) => {
            this.loadTemplate(e.target.value);
        });

        // Save/Delete template
        document.getElementById('saveTemplateBtn').addEventListener('click', () => this.saveTemplate());
        document.getElementById('deleteTemplateBtn').addEventListener('click', () => this.deleteTemplate());

        // Label type change
        document.getElementById('labelType').addEventListener('change', () => this.updateCanvasSize());

        // Custom size inputs
        ['customWidth', 'customHeight'].forEach(id => {
            document.getElementById(id).addEventListener('change', () => this.updateCanvasSize());
        });

        // Preview product
        document.getElementById('previewProduct').addEventListener('change', (e) => {
            const [productId, variantId] = e.target.value.split('-');
            if (productId && variantId) {
                this.loadPreviewData(productId, variantId);
            }
        });

        // Print modal
        document.getElementById('testPrintBtn').addEventListener('click', () => {
            document.getElementById('printModal').classList.remove('hidden');
        });

        const closeModal = () => document.getElementById('printModal').classList.add('hidden');
        document.getElementById('closePrintModal')?.addEventListener('click', closeModal);
        document.getElementById('cancelPrint')?.addEventListener('click', closeModal);

        document.getElementById('printModal').addEventListener('click', (e) => {
            if (e.target.id === 'printModal') closeModal();
        });

        // Select all products
        document.getElementById('selectAllProducts')?.addEventListener('change', (e) => {
            document.querySelectorAll('.product-checkbox').forEach(cb => cb.checked = e.target.checked);
        });

        // Generate PDF
        document.getElementById('generatePdfBtn')?.addEventListener('click', () => this.generatePdf());

        // Image modal
        const imageDropZone = document.getElementById('imageDropZone');
        const imageInput = document.getElementById('imageInput');

        imageDropZone?.addEventListener('click', () => imageInput.click());

        imageDropZone?.addEventListener('dragover', (e) => {
            e.preventDefault();
            imageDropZone.style.borderColor = '#6366f1';
        });

        imageDropZone?.addEventListener('dragleave', () => {
            imageDropZone.style.borderColor = '#cbd5e1';
        });

        imageDropZone?.addEventListener('drop', (e) => {
            e.preventDefault();
            imageDropZone.style.borderColor = '#cbd5e1';
            const file = e.dataTransfer.files[0];
            this.handleImageSelect(file);
        });

        imageInput?.addEventListener('change', (e) => {
            const file = e.target.files[0];
            this.handleImageSelect(file);
        });

        document.getElementById('closeImageModal')?.addEventListener('click', () => this.hideImageModal());
        document.getElementById('cancelImage')?.addEventListener('click', () => this.hideImageModal());
        document.getElementById('addImageBtn')?.addEventListener('click', () => this.addImageToCanvas());

        document.getElementById('imageModal')?.addEventListener('click', (e) => {
            if (e.target.id === 'imageModal') this.hideImageModal();
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                document.getElementById('printModal').classList.add('hidden');
                this.hideImageModal();
            }

            if ((e.key === 'Delete' || e.key === 'Backspace') && this.selectedField) {
                const active = document.activeElement;
                if (active.tagName !== 'INPUT' && active.tagName !== 'TEXTAREA' && active.tagName !== 'SELECT') {
                    e.preventDefault();
                    this.removeSelectedField();
                }
            }
        });
    }
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    window.labelDesigner = new LabelDesigner();
});
