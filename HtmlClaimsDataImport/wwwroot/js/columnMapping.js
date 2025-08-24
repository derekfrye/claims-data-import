// Column mapping and preview functionality
function selectColumn(columnName) {
    // Update selected column via AJAX
    const tmpdir = document.getElementById('tmpdir').value;
    
    // Get current mapping step from the UI
    const mappingProgressElement = document.querySelector('.mapping-progress');
    let currentStep = 0;
    if (mappingProgressElement) {
        const stepText = mappingProgressElement.textContent;
        const stepMatch = stepText.match(/Step (\d+)/);
        if (stepMatch) {
            currentStep = parseInt(stepMatch[1]) - 1; // Convert to 0-based
        }
    }
    
    fetch('/ClaimsDataImporter?handler=Preview', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: `tmpdir=${encodeURIComponent(tmpdir)}&mappingStep=${currentStep}&selectedColumn=${encodeURIComponent(columnName)}&__RequestVerificationToken=${encodeURIComponent(document.querySelector('input[name="__RequestVerificationToken"]').value)}`
    })
    .then(response => response.text())
    .then(html => {
        const container = document.getElementById('preview-container-outer');
        container.innerHTML = html;
        if (window.htmx && typeof window.htmx.process === 'function') {
            window.htmx.process(container);
        }
    })
    .catch(error => {
        console.error('Error selecting column:', error);
    });
}

function nextMapping() {
    const tmpdir = document.getElementById('tmpdir').value;
    
    // Get current mapping step and selected column from the UI
    const mappingProgressElement = document.querySelector('.mapping-progress');
    let currentStep = 0;
    if (mappingProgressElement) {
        const stepText = mappingProgressElement.textContent;
        const stepMatch = stepText.match(/Step (\d+)/);
        if (stepMatch) {
            currentStep = parseInt(stepMatch[1]) - 1; // Convert to 0-based
        }
    }
    
    const selectedColumnElement = document.querySelector('.selected-column');
    const selectedColumn = selectedColumnElement ? selectedColumnElement.textContent.trim() : '';
    
    if (!selectedColumn) {
        alert('Please select a column first.');
        return;
    }

    // Determine the current destination (claims) column name from the sidebar
    const activeClaim = document.querySelector('#claims-columns-list li.active');
    const outputColumn = activeClaim ? (activeClaim.getAttribute('data-column') || activeClaim.textContent.trim()) : '';
    if (!outputColumn) {
        console.error('Unable to resolve destination column for mapping.');
        return;
    }
    
    // Persist mapping, then move to the next mapping step
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    fetch('/ClaimsDataImporter?handler=SaveMapping', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `tmpdir=${encodeURIComponent(tmpdir)}&outputColumn=${encodeURIComponent(outputColumn)}&importColumn=${encodeURIComponent(selectedColumn)}&__RequestVerificationToken=${encodeURIComponent(token)}`
    })
    .then(response => response.ok ? response.text() : Promise.reject('Failed to save mapping'))
    .then(() => {
        return fetch('/ClaimsDataImporter?handler=Preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `tmpdir=${encodeURIComponent(tmpdir)}&mappingStep=${currentStep + 1}&selectedColumn=&__RequestVerificationToken=${encodeURIComponent(token)}`
        });
    })
    .then(response => response.text())
    .then(html => {
        const container = document.getElementById('preview-container-outer');
        container.innerHTML = html;
        if (window.htmx && typeof window.htmx.process === 'function') {
            window.htmx.process(container);
        }
    })
    .catch(error => {
        console.error('Error moving to next mapping:', error);
    });
}

function previousMapping() {
    const tmpdir = document.getElementById('tmpdir').value;
    
    // Get current mapping step from the UI
    const mappingProgressElement = document.querySelector('.mapping-progress');
    let currentStep = 0;
    if (mappingProgressElement) {
        const stepText = mappingProgressElement.textContent;
        const stepMatch = stepText.match(/Step (\d+)/);
        if (stepMatch) {
            currentStep = parseInt(stepMatch[1]) - 1; // Convert to 0-based
        }
    }
    
    if (currentStep <= 0) {
        return;
    }
    
    // Move to previous mapping step
    fetch('/ClaimsDataImporter?handler=Preview', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: `tmpdir=${encodeURIComponent(tmpdir)}&mappingStep=${currentStep - 1}&selectedColumn=&__RequestVerificationToken=${encodeURIComponent(document.querySelector('input[name="__RequestVerificationToken"]').value)}`
    })
    .then(response => response.text())
    .then(html => {
        const container = document.getElementById('preview-container-outer');
        container.innerHTML = html;
        if (window.htmx && typeof window.htmx.process === 'function') {
            window.htmx.process(container);
        }
    })
    .catch(error => {
        console.error('Error moving to previous mapping:', error);
    });
}

// Render the claims columns into the preview sidebar with mapping status and step highlighting
function updatePreviewSidebar(claimsColumns, columnMappings, currentStep) {
    try {
        const ul = document.getElementById('claims-columns-list');
        if (!ul) return;

        // Normalize mapping dict keys: claims (dest) -> import (src)
        const mappedSet = new Set(Object.keys(columnMappings || {}));
        ul.innerHTML = '';

        claimsColumns.forEach((col, idx) => {
            const li = document.createElement('li');
            li.textContent = col;
            li.setAttribute('data-column', col);
            if (mappedSet.has(col)) li.classList.add('mapped');
            if (idx === currentStep) li.classList.add('active');
            li.onclick = () => goToMappingStep(idx);
            ul.appendChild(li);
        });
    } catch (e) {
        console.error('updatePreviewSidebar error:', e);
    }
}

// Jump to a particular mapping step (claims column)
function goToMappingStep(stepIndex) {
    const tmpdir = document.getElementById('tmpdir').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    fetch('/ClaimsDataImporter?handler=Preview', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `tmpdir=${encodeURIComponent(tmpdir)}&mappingStep=${stepIndex}&selectedColumn=&__RequestVerificationToken=${encodeURIComponent(token)}`
    })
    .then(r => r.text())
    .then(html => {
        const container = document.getElementById('preview-container-outer');
        container.innerHTML = html;
        if (window.htmx && typeof window.htmx.process === 'function') {
            window.htmx.process(container);
        }
    })
    .catch(e => console.error('goToMappingStep error:', e));
}

// Expose functions to global scope for inline scripts
window.updatePreviewSidebar = updatePreviewSidebar;
window.goToMappingStep = goToMappingStep;
