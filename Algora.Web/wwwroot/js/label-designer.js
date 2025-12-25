/**
 * Label Designer - Professional Drag and Drop Implementation
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
        this.isLoading = false;

        this.init();
    }

    init() {
        this.canvas = document.getElementById('labelCanvas');
        this.canvasContainer = document.getElementById('canvasContainer');

        this.initializeEventListeners();
        this.initializeDragAndDrop();
        this.updateCanvasSize();
        this.updateSizePresets();
    }

    // Show loading state
    showLoading(message = 'Loading...') {
        this.isLoading = true;
        // Could add a loading overlay here if needed
    }

    hideLoading() {
        this.isLoading = false;
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
            document.getElementById('customSizeInputs').classList.add('grid');
        } else {
            widthInches = parseFloat(selected.dataset.width) || 2.625;
            heightInches = parseFloat(selected.dataset.height) || 1;
            document.getElementById('customSizeInputs').classList.add('hidden');
            document.getElementById('customSizeInputs').classList.remove('grid');
        }

        // Scale to fit container (96 DPI base, scaled for visibility)
        const scale = 96;
        this.canvasWidth = widthInches * scale;
        this.canvasHeight = heightInches * scale;

        this.canvas.style.width = `${this.canvasWidth}px`;
        this.canvas.style.height = `${this.canvasHeight}px`;

        document.getElementById('canvasSize').textContent = `${widthInches}" × ${heightInches}"`;

        // Update size presets active state
        this.updateSizePresets();

        // Re-render fields with new canvas size
        this.renderAllFields();
    }

    updateSizePresets() {
        const currentType = document.getElementById('labelType').value;
        document.querySelectorAll('.size-preset').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.type === currentType);
        });
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
                    event.target.classList.add('dragging');
                },
                move(event) {
                    // Visual feedback while dragging
                },
                end(event) {
                    event.target.classList.remove('dragging');

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
                self.canvas.classList.add('drop-active');
            },
            ondragleave(event) {
                self.canvas.classList.remove('drop-active');
            },
            ondrop(event) {
                self.canvas.classList.remove('drop-active');
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
            width: fieldType === 'Barcode' ? 40 : 30,
            height: fieldType === 'Barcode' ? 35 : 20,
            fontFamily: 'Arial',
            fontSize: fieldType === 'ProductTitle' ? 12 : 10,
            isBold: fieldType === 'ProductTitle' || fieldType === 'Price',
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
        element.className = 'canvas-field absolute cursor-move rounded-md overflow-hidden flex items-center touch-none';

        // Apply field-specific styling
        const fieldStyles = this.getFieldStyles(field.fieldType);
        element.style.cssText = `
            border: 2px solid ${fieldStyles.borderColor};
            background: ${fieldStyles.bgColor};
        `;

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
        element.style.padding = '4px';
        element.style.justifyContent = field.textAlign === 'center' ? 'center' : (field.textAlign === 'right' ? 'flex-end' : 'flex-start');

        element.innerHTML = this.getFieldPreviewContent(field);

        // Add resize handle
        const resizeHandle = document.createElement('div');
        resizeHandle.className = 'resize-handle';
        element.appendChild(resizeHandle);

        element.addEventListener('click', (e) => {
            e.stopPropagation();
            this.selectField(field.id);
        });

        this.canvas.appendChild(element);
    }

    getFieldStyles(fieldType) {
        const styles = {
            'ProductTitle': { borderColor: '#3b82f6', bgColor: 'rgba(59, 130, 246, 0.08)' },
            'SKU': { borderColor: '#22c55e', bgColor: 'rgba(34, 197, 94, 0.08)' },
            'Barcode': { borderColor: '#a855f7', bgColor: 'rgba(168, 85, 247, 0.08)' },
            'Price': { borderColor: '#10b981', bgColor: 'rgba(16, 185, 129, 0.08)' },
            'CompareAtPrice': { borderColor: '#f97316', bgColor: 'rgba(249, 115, 22, 0.08)' },
            'VariantTitle': { borderColor: '#06b6d4', bgColor: 'rgba(6, 182, 212, 0.08)' },
            'VariantOption1': { borderColor: '#6366f1', bgColor: 'rgba(99, 102, 241, 0.08)' },
            'VariantOption2': { borderColor: '#6366f1', bgColor: 'rgba(99, 102, 241, 0.08)' },
            'VariantOption3': { borderColor: '#6366f1', bgColor: 'rgba(99, 102, 241, 0.08)' },
            'Vendor': { borderColor: '#64748b', bgColor: 'rgba(100, 116, 139, 0.08)' },
            'ProductType': { borderColor: '#f59e0b', bgColor: 'rgba(245, 158, 11, 0.08)' },
            'Weight': { borderColor: '#f43f5e', bgColor: 'rgba(244, 63, 94, 0.08)' },
            'InventoryQuantity': { borderColor: '#14b8a6', bgColor: 'rgba(20, 184, 166, 0.08)' },
            'CustomText': { borderColor: '#ec4899', bgColor: 'rgba(236, 72, 153, 0.08)' }
        };
        return styles[fieldType] || { borderColor: '#94a3b8', bgColor: 'rgba(148, 163, 184, 0.08)' };
    }

    getFieldPreviewContent(field) {
        const preview = this.previewData || {};

        const fieldLabels = {
            'ProductTitle': preview.productTitle || 'Product Title',
            'SKU': preview.sku || 'SKU-12345',
            'Barcode': '<i class="fas fa-barcode text-lg text-slate-600"></i>',
            'Price': preview.price ? `$${parseFloat(preview.price).toFixed(2)}` : '$29.99',
            'CompareAtPrice': preview.compareAtPrice ? `$${parseFloat(preview.compareAtPrice).toFixed(2)}` : '$39.99',
            'VariantTitle': preview.variantTitle || 'Large / Blue',
            'VariantOption1': preview.option1 || 'Size: Large',
            'VariantOption2': preview.option2 || 'Color: Blue',
            'VariantOption3': preview.option3 || 'Option 3',
            'Vendor': preview.vendor || 'Brand Name',
            'ProductType': preview.productType || 'Category',
            'Weight': preview.weight ? `${preview.weight} ${preview.weightUnit || ''}`.trim() : '1.5 kg',
            'InventoryQuantity': preview.inventoryQuantity?.toString() || '42',
            'CustomText': field.customText || 'Custom Text'
        };

        return `<span class="truncate block w-full">${fieldLabels[field.fieldType] || field.fieldType}</span>`;
    }

    selectField(fieldId) {
        // Deselect previous
        document.querySelectorAll('.canvas-field').forEach(el => {
            el.classList.remove('selected');
        });

        const element = document.getElementById(fieldId);
        if (element) {
            element.classList.add('selected');
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
            'Barcode': 'Barcode',
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

        boldBtn.classList.toggle('active', this.selectedField?.isBold);
        italicBtn.classList.toggle('active', this.selectedField?.isItalic);

        alignBtns.forEach(id => {
            const btn = document.getElementById(id);
            const align = id.replace('align', '').toLowerCase();
            btn.classList.toggle('active', this.selectedField?.textAlign === align);
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
                element.style.justifyContent = value === 'center' ? 'center' : (value === 'right' ? 'flex-end' : 'flex-start');
                break;
            case 'textColor':
                element.style.color = value;
                break;
            case 'customText':
                element.querySelector('span').textContent = value || 'Custom Text';
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
            this.showLoading('Loading template...');
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
            this.showNotification('Failed to load template', 'error');
        } finally {
            this.hideLoading();
        }
    }

    async saveTemplate() {
        const name = document.getElementById('templateName').value.trim();
        if (!name) {
            this.showNotification('Please enter a template name', 'warning');
            document.getElementById('templateName').focus();
            return;
        }

        if (this.fields.length === 0) {
            this.showNotification('Please add at least one field to the label', 'warning');
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
            this.showLoading('Saving template...');
            let url;
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
            this.showNotification('Template saved successfully!', 'success');

            // Reload page to refresh template list
            setTimeout(() => location.reload(), 1000);

        } catch (error) {
            console.error('Error saving template:', error);
            this.showNotification('Failed to save template', 'error');
        } finally {
            this.hideLoading();
        }
    }

    async deleteTemplate() {
        if (!this.templateId) {
            this.showNotification('No template selected', 'warning');
            return;
        }

        if (!confirm('Are you sure you want to delete this template? This action cannot be undone.')) return;

        try {
            this.showLoading('Deleting template...');
            const response = await fetch(`?handler=DeleteTemplate&id=${this.templateId}`, {
                method: 'POST'
            });

            if (!response.ok) throw new Error('Failed to delete template');

            this.showNotification('Template deleted', 'success');
            setTimeout(() => location.reload(), 1000);

        } catch (error) {
            console.error('Error deleting template:', error);
            this.showNotification('Failed to delete template', 'error');
        } finally {
            this.hideLoading();
        }
    }

    clearCanvas() {
        this.fields = [];
        const existingFields = this.canvas.querySelectorAll('.canvas-field');
        existingFields.forEach(f => f.remove());
        this.showPlaceholder();
        this.hideFieldProperties();
        this.selectedField = null;
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

    // Notification helper
    showNotification(message, type = 'info') {
        // Simple alert for now - could be replaced with toast notifications
        if (type === 'error' || type === 'warning') {
            alert(message);
        } else {
            // For success, just log it
            console.log(message);
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

        // Clear canvas
        const clearBtn = document.getElementById('clearCanvasBtn');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => {
                if (this.fields.length === 0 || confirm('Clear all fields from the canvas?')) {
                    this.clearCanvas();
                    document.getElementById('templateName').value = '';
                    this.templateId = null;
                }
            });
        }

        // Size preset buttons
        document.querySelectorAll('.size-preset').forEach(btn => {
            btn.addEventListener('click', () => {
                const labelType = document.getElementById('labelType');
                labelType.value = btn.dataset.type;
                this.updateCanvasSize();
            });
        });

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
                    el.classList.remove('selected');
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

        // Close modal on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                document.getElementById('printModal').classList.add('hidden');
            }
        });

        // Close modal on backdrop click
        document.getElementById('printModal').addEventListener('click', (e) => {
            if (e.target.id === 'printModal') {
                document.getElementById('printModal').classList.add('hidden');
            }
        });

        // Select all products
        document.getElementById('selectAllProducts').addEventListener('change', (e) => {
            document.querySelectorAll('.product-checkbox').forEach(cb => {
                cb.checked = e.target.checked;
            });
        });

        // Generate PDF
        document.getElementById('generatePdfBtn').addEventListener('click', () => this.generatePdf());

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            // Delete selected field with Delete or Backspace (when not in input)
            if ((e.key === 'Delete' || e.key === 'Backspace') && this.selectedField) {
                const activeElement = document.activeElement;
                if (activeElement.tagName !== 'INPUT' && activeElement.tagName !== 'TEXTAREA' && activeElement.tagName !== 'SELECT') {
                    e.preventDefault();
                    this.removeSelectedField();
                }
            }
        });
    }

    async generatePdf() {
        const templateId = document.getElementById('printTemplateSelector').value;
        if (!templateId) {
            this.showNotification('Please select a template', 'warning');
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
            this.showNotification('Please select at least one product', 'warning');
            return;
        }

        try {
            this.showLoading('Generating PDF...');
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
            this.showNotification(error.message || 'Failed to generate PDF', 'error');
        } finally {
            this.hideLoading();
            const btn = document.getElementById('generatePdfBtn');
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-file-pdf"></i> Generate PDF';
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.labelDesigner = new LabelDesigner();
});
