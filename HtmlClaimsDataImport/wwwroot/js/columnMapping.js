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
    
    // Move to next mapping step
    fetch('/ClaimsDataImporter?handler=Preview', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: `tmpdir=${encodeURIComponent(tmpdir)}&mappingStep=${currentStep + 1}&selectedColumn=&__RequestVerificationToken=${encodeURIComponent(document.querySelector('input[name="__RequestVerificationToken"]').value)}`
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
