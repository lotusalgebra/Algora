/**
 * Label Designer - Drag and Drop Implementation
 * Uses interact.js for drag, drop, and resize functionality
 */

class LabelDesigner {
    constructor() {
        this.fields = [];
        this.selectedField = null;
        this.templateId = null;
        this.previewData = null;
        this.canvasWidth = 252;
        this.canvasHeight = 96;

        this.init();
    }

    init() {
        this.canvas = document.getElementById('labelCanvas');
        this.canvasContainer = document.getElementById('canvasContainer');

        this.initializeEventListeners();
        this.initializeDragAndDrop();
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

        // Scale to fit container (96 DPI base, scaled for visibility)
        const scale = 96;
        this.canvasWidth = widthInches * scale;
        this.canvasHeight = heightInches * scale;

        this.canvas.style.width = `${this.canvasWidth}px`;
        this.canvas.style.height = `${this.canvasHeight}px`;

        document.getElementById('canvasSize').textContent = `${widthInches}" x ${heightInches}"`;

        // Re-render fields with new canvas size
        this.renderAllFields();
    }

    // Drag and Drop setup
    initializeDragAndDrop() {
        const self = this;

        // Palette items - drag to clone onto canvas
        interact('.draggable-source').draggable({
            inertia: false,
            autoScroll: true,
            listeners: {
                start(event) {
                    event.target.classList.add('opacity-50');
                },
                move(event) {
                    // Visual feedback while dragging
                },
                end(event) {
                    event.target.classList.remove('opacity-50');

                    // Check if dropped on canvas
                    const canvasRect = self.canvas.getBoundingClientRect();
                    const dropX = event.clientX;
                    const dropY = event.clientY;

                    if (dropX >= canvasRect.left && dropX <= canvasRect.right &&
                        dropY >= canvasRect.top && dropY <= canvasRect.bottom) {

                        const fieldType = event.target.dataset.fieldType;
                        const x = ((dropX - canvasRect.left) / self.canvasWidth) * 100;
                        const y = ((dropY - canvasRect.top) / self.canvasHeight) * 100;

                        self.addFieldToCanvas(fieldType, x, y);
                    }
                }
            }
        });

        // Make canvas a dropzone
        interact('#labelCanvas').dropzone({
            accept: '.draggable-source',
            overlap: 0.25,
            ondragenter(event) {
                event.target.classList.add('ring-2', 'ring-fuchsia-400');
            },
            ondragleave(event) {
                event.target.classList.remove('ring-2', 'ring-fuchsia-400');
            },
            ondrop(event) {
                event.target.classList.remove('ring-2', 'ring-fuchsia-400');
            }
        });

        this.setupCanvasFieldInteractions();
    }

    setupCanvasFieldInteractions() {
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
                edges: { right: true, bottom: true },
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

    // Field management
    addFieldToCanvas(fieldType, x = 5, y = 5) {
        const fieldId = `field_${Date.now()}`;
        const field = {
            id: fieldId,
            fieldType: fieldType,
            x: Math.max(0, Math.min(x, 70)),
            y: Math.max(0, Math.min(y, 70)),
            width: 30,
            height: 20,
            fontFamily: 'Arial',
            fontSize: 10,
            isBold: false,
            isItalic: false,
            textAlign: 'left',
            textColor: '#000000',
            customText: fieldType === 'CustomText' ? 'Custom Text' : null,
            barcodeFormat: 'Code128',
            pricePrefix: '$',
            showCurrency: true
        };

        this.fields.push(field);
        this.renderField(field);
        this.selectField(fieldId);
        this.hidePlaceholder();
    }

    renderAllFields() {
        // Clear existing rendered fields
        const existingFields = this.canvas.querySelectorAll('.canvas-field');
        existingFields.forEach(f => f.remove());

        // Re-render all fields
        this.fields.forEach(field => this.renderField(field));

        // Re-setup interactions
        this.setupCanvasFieldInteractions();
    }

    renderField(field) {
        const element = document.createElement('div');
        element.id = field.id;
        element.dataset.fieldId = field.id;
        element.className = 'canvas-field absolute border border-blue-300 bg-blue-50/70 cursor-move rounded p-1 text-xs overflow-hidden flex items-center justify-center touch-none';

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
        element.style.textAlign = field.textAlign;
        element.style.color = field.textColor;

        element.innerHTML = this.getFieldPreviewContent(field);

        element.addEventListener('click', (e) => {
            e.stopPropagation();
            this.selectField(field.id);
        });

        this.canvas.appendChild(element);
    }

    getFieldPreviewContent(field) {
        const preview = this.previewData || {};

        const fieldLabels = {
            'ProductTitle': preview.productTitle || '[Product Title]',
            'SKU': preview.sku || '[SKU]',
            'Barcode': '<i class="fas fa-barcode text-lg text-gray-600"></i>',
            'Price': preview.price ? `$${parseFloat(preview.price).toFixed(2)}` : '[$0.00]',
            'CompareAtPrice': preview.compareAtPrice ? `$${parseFloat(preview.compareAtPrice).toFixed(2)}` : '[$0.00]',
            'VariantTitle': preview.variantTitle || '[Variant]',
            'VariantOption1': preview.option1 || '[Option 1]',
            'VariantOption2': preview.option2 || '[Option 2]',
            'VariantOption3': preview.option3 || '[Option 3]',
            'Vendor': preview.vendor || '[Vendor]',
            'ProductType': preview.productType || '[Type]',
            'Weight': preview.weight ? `${preview.weight} ${preview.weightUnit || ''}`.trim() : '[Weight]',
            'InventoryQuantity': preview.inventoryQuantity?.toString() || '[Qty]',
            'CustomText': field.customText || '[Custom Text]'
        };

        return `<span class="truncate">${fieldLabels[field.fieldType] || `[${field.fieldType}]`}</span>`;
    }

    selectField(fieldId) {
        // Deselect previous
        document.querySelectorAll('.canvas-field').forEach(el => {
            el.classList.remove('ring-2', 'ring-fuchsia-500', 'border-fuchsia-500');
            el.classList.add('border-blue-300');
        });

        const element = document.getElementById(fieldId);
        if (element) {
            element.classList.remove('border-blue-300');
            element.classList.add('ring-2', 'ring-fuchsia-500', 'border-fuchsia-500');
            this.selectedField = this.fields.find(f => f.id === fieldId);
            this.showFieldProperties();
        }
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

    removeSelectedField() {
        if (!this.selectedField) return;

        const element = document.getElementById(this.selectedField.id);
        if (element) element.remove();

        this.fields = this.fields.filter(f => f.id !== this.selectedField.id);
        this.selectedField = null;
        this.hideFieldProperties();

        if (this.fields.length === 0) {
            this.showPlaceholder();
        }
    }

    hidePlaceholder() {
        const placeholder = document.getElementById('canvasPlaceholder');
        if (placeholder) placeholder.classList.add('hidden');
    }

    showPlaceholder() {
        const placeholder = document.getElementById('canvasPlaceholder');
        if (placeholder) placeholder.classList.remove('hidden');
    }

    // Properties panel
    showFieldProperties() {
        document.getElementById('noFieldSelected').classList.add('hidden');
        document.getElementById('fieldPropertiesForm').classList.remove('hidden');

        if (!this.selectedField) return;

        const fieldTypeNames = {
            'ProductTitle': 'Product Title',
            'SKU': 'SKU',
            'Barcode': 'Barcode Image',
            'Price': 'Price',
            'CompareAtPrice': 'Compare Price',
            'VariantTitle': 'Variant Title',
            'VariantOption1': 'Option 1',
            'VariantOption2': 'Option 2',
            'VariantOption3': 'Option 3',
            'Vendor': 'Vendor',
            'ProductType': 'Product Type',
            'Weight': 'Weight',
            'InventoryQuantity': 'Inventory',
            'CustomText': 'Custom Text'
        };

        document.getElementById('selectedFieldType').textContent = fieldTypeNames[this.selectedField.fieldType] || this.selectedField.fieldType;
        document.getElementById('fontFamily').value = this.selectedField.fontFamily;
        document.getElementById('fontSize').value = this.selectedField.fontSize;
        document.getElementById('textColor').value = this.selectedField.textColor;

        // Update style buttons
        this.updateStyleButtons();

        // Show/hide field-specific options
        document.getElementById('customTextContainer').classList.toggle('hidden', this.selectedField.fieldType !== 'CustomText');
        document.getElementById('barcodeFormatContainer').classList.toggle('hidden', this.selectedField.fieldType !== 'Barcode');

        if (this.selectedField.fieldType === 'CustomText') {
            document.getElementById('customText').value = this.selectedField.customText || '';
        }
        if (this.selectedField.fieldType === 'Barcode') {
            document.getElementById('barcodeFormat').value = this.selectedField.barcodeFormat;
        }
    }

    hideFieldProperties() {
        document.getElementById('noFieldSelected').classList.remove('hidden');
        document.getElementById('fieldPropertiesForm').classList.add('hidden');
    }

    updateStyleButtons() {
        const boldBtn = document.getElementById('toggleBold');
        const italicBtn = document.getElementById('toggleItalic');
        const alignBtns = ['alignLeft', 'alignCenter', 'alignRight'];

        boldBtn.classList.toggle('bg-fuchsia-100', this.selectedField?.isBold);
        italicBtn.classList.toggle('bg-fuchsia-100', this.selectedField?.isItalic);

        alignBtns.forEach(id => {
            const btn = document.getElementById(id);
            const align = id.replace('align', '').toLowerCase();
            btn.classList.toggle('bg-fuchsia-100', this.selectedField?.textAlign === align);
        });
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
            case 'textAlign':
                element.style.textAlign = value;
                break;
            case 'textColor':
                element.style.color = value;
                break;
            case 'customText':
                element.innerHTML = `<span class="truncate">${value || '[Custom Text]'}</span>`;
                break;
        }

        this.updateStyleButtons();
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

            if (this.fields.length > 0) {
                this.hidePlaceholder();
            } else {
                this.showPlaceholder();
            }

            this.selectedField = null;
            this.hideFieldProperties();

        } catch (error) {
            console.error('Error loading template:', error);
            alert('Failed to load template');
        }
    }

    async saveTemplate() {
        const name = document.getElementById('templateName').value.trim();
        if (!name) {
            alert('Please enter a template name');
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
            let url, method;
            if (this.templateId) {
                dto.id = this.templateId;
                url = '?handler=UpdateTemplate';
            } else {
                url = '?handler=SaveTemplate';
            }

            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });

            if (!response.ok) throw new Error('Failed to save template');

            const result = await response.json();
            this.templateId = result.id;
            alert('Template saved successfully');
            location.reload();

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

        if (!confirm('Are you sure you want to delete this template?')) return;

        try {
            const response = await fetch(`?handler=DeleteTemplate&id=${this.templateId}`, {
                method: 'POST'
            });

            if (!response.ok) throw new Error('Failed to delete template');

            alert('Template deleted');
            location.reload();

        } catch (error) {
            console.error('Error deleting template:', error);
            alert('Failed to delete template');
        }
    }

    clearCanvas() {
        this.fields = [];
        const existingFields = this.canvas.querySelectorAll('.canvas-field');
        existingFields.forEach(f => f.remove());
        this.showPlaceholder();
        this.hideFieldProperties();
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

    // Event listeners
    initializeEventListeners() {
        const self = this;

        // Template selector
        document.getElementById('templateSelector').addEventListener('change', (e) => {
            this.loadTemplate(e.target.value);
        });

        // Save template
        document.getElementById('saveTemplateBtn').addEventListener('click', () => this.saveTemplate());

        // Delete template
        document.getElementById('deleteTemplateBtn').addEventListener('click', () => this.deleteTemplate());

        // Label type change
        document.getElementById('labelType').addEventListener('change', () => this.updateCanvasSize());

        // Custom size inputs
        ['customWidth', 'customHeight'].forEach(id => {
            document.getElementById(id).addEventListener('change', () => this.updateCanvasSize());
        });

        // Property changes
        document.getElementById('fontFamily').addEventListener('change', (e) => {
            this.updateSelectedFieldStyle('fontFamily', e.target.value);
        });

        document.getElementById('fontSize').addEventListener('change', (e) => {
            this.updateSelectedFieldStyle('fontSize', parseInt(e.target.value));
        });

        document.getElementById('textColor').addEventListener('input', (e) => {
            this.updateSelectedFieldStyle('textColor', e.target.value);
        });

        document.getElementById('toggleBold').addEventListener('click', () => {
            if (this.selectedField) {
                this.updateSelectedFieldStyle('isBold', !this.selectedField.isBold);
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

        document.getElementById('customText').addEventListener('input', (e) => {
            this.updateSelectedFieldStyle('customText', e.target.value);
        });

        document.getElementById('barcodeFormat').addEventListener('change', (e) => {
            if (this.selectedField) {
                this.selectedField.barcodeFormat = e.target.value;
            }
        });

        document.getElementById('removeField').addEventListener('click', () => this.removeSelectedField());

        // Preview product
        document.getElementById('previewProduct').addEventListener('change', (e) => {
            const [productId, variantId] = e.target.value.split('-');
            if (productId && variantId) {
                this.loadPreviewData(productId, variantId);
            }
        });

        document.getElementById('refreshPreview').addEventListener('click', () => {
            const select = document.getElementById('previewProduct');
            if (select.value) {
                const [productId, variantId] = select.value.split('-');
                this.loadPreviewData(productId, variantId);
            }
        });

        // Click outside to deselect
        this.canvas.addEventListener('click', (e) => {
            if (e.target === this.canvas || e.target.id === 'canvasPlaceholder') {
                this.selectedField = null;
                document.querySelectorAll('.canvas-field').forEach(el => {
                    el.classList.remove('ring-2', 'ring-fuchsia-500', 'border-fuchsia-500');
                    el.classList.add('border-blue-300');
                });
                this.hideFieldProperties();
            }
        });

        // Print modal
        document.getElementById('printLabelsBtn').addEventListener('click', () => {
            document.getElementById('printModal').classList.remove('hidden');
        });

        document.getElementById('closePrintModal').addEventListener('click', () => {
            document.getElementById('printModal').classList.add('hidden');
        });

        document.getElementById('cancelPrint').addEventListener('click', () => {
            document.getElementById('printModal').classList.add('hidden');
        });

        // Select all products
        document.getElementById('selectAllProducts').addEventListener('change', (e) => {
            document.querySelectorAll('.product-checkbox').forEach(cb => {
                cb.checked = e.target.checked;
            });
        });

        // Generate PDF
        document.getElementById('generatePdfBtn').addEventListener('click', () => this.generatePdf());
    }

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
            a.download = `labels-${Date.now()}.pdf`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);

            document.getElementById('printModal').classList.add('hidden');

        } catch (error) {
            console.error('Error generating PDF:', error);
            alert(error.message || 'Failed to generate PDF');
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.labelDesigner = new LabelDesigner();
});
