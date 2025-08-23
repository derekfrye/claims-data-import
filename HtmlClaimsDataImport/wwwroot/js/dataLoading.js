// Data loading functionality
function loadPreviewData() {
    const tmpdir = document.getElementById('tmpdir').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    fetch('/ClaimsDataImporter?handler=Preview', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `tmpdir=${encodeURIComponent(tmpdir)}&mappingStep=0&selectedColumn=&__RequestVerificationToken=${encodeURIComponent(token)}`
    })
    .then(response => response.text())
    .then(html => {
        const container = document.getElementById('preview-container-outer');
        container.innerHTML = html;
        if (window.htmx && typeof window.htmx.process === 'function') {
            // Ensure htmx scans the newly injected preview content (for hx-post, etc.)
            window.htmx.process(container);
        }
    })
    .catch(error => {
        console.error('Error loading preview:', error);
        document.getElementById('preview-container-outer').innerHTML = '<div class="alert alert-danger">Error loading preview data</div>';
    });
}

function loadData() {
    const loadBtn = document.getElementById('loadBtn');
    const loadWarning = document.getElementById('load-warning');
    
    if (loadBtn.classList.contains('disabled')) {
        // Check conditions and show appropriate warning
        const jsonMode = document.getElementById('jsonMode').value;
        const jsonFile = document.getElementById('jsonFile').value;
        const jsonValid = jsonMode === 'default' || (jsonMode === 'upload' && jsonFile.trim() !== '');
        
        if (!jsonValid) {
            loadWarning.textContent = 'no json supplied';
            return;
        }
        
        const fileName = document.getElementById('fileName').value;
        const fileValid = fileName.trim() !== '';
        
        if (!fileValid) {
            loadWarning.textContent = 'no file supplied';
            return;
        }
        
        const databaseMode = document.getElementById('databaseMode').value;
        const database = document.getElementById('database').value;
        const databaseValid = databaseMode === 'default' || (databaseMode === 'upload' && database.trim() !== '');
        
        if (!databaseValid) {
            loadWarning.textContent = 'no database supplied';
            return;
        }
    }
    
    // All conditions met - proceed with load
    loadWarning.textContent = 'Loading...';
    
    // Prepare data for server
    const formData = new FormData();
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    formData.append('__RequestVerificationToken', token);
    formData.append('tmpdir', document.getElementById('tmpdir').value);
    formData.append('fileName', document.getElementById('fileName').value);
    
    // Add JSON path
    const jsonMode = document.getElementById('jsonMode').value;
    if (jsonMode === 'default') {
        formData.append('jsonPath', 'default');
    } else {
        formData.append('jsonPath', document.getElementById('jsonFile').value);
    }
    
    // Add database path
    const databaseMode = document.getElementById('databaseMode').value;
    if (databaseMode === 'default') {
        formData.append('databasePath', 'default');
    } else {
        formData.append('databasePath', document.getElementById('database').value);
    }
    
    // Use fetch to call the load handler
    fetch('/ClaimsDataImporter?handler=LoadData', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(result => {
        loadWarning.textContent = result.statusMessage || (result.success ? 'Load completed' : 'Load failed');
        if (result.success) {
            // Optionally, refresh preview after successful load
            // loadPreviewData();
        }
    })
    .catch(error => {
        console.error('Load error:', error);
        loadWarning.textContent = 'Load failed';
    });
}
