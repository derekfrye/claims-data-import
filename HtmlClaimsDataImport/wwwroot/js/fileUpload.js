// File upload and mode toggle functionality
function toggleJsonMode() {
    const mode = document.getElementById('jsonMode').value;
    const defaultPreview = document.getElementById('json-default-preview');
    const uploadSection = document.getElementById('json-upload-section');
    const statusElement = document.getElementById('json-status');
    
    if (mode === 'default') {
        defaultPreview.classList.remove('hidden');
        uploadSection.classList.add('hidden');
        // Clear upload field and show default status
        document.getElementById('jsonFile').value = '';
        statusElement.textContent = 'Using default configuration';
    } else {
        defaultPreview.classList.add('hidden');
        uploadSection.classList.remove('hidden');
        statusElement.textContent = 'Please select a file';
    }
    validateLoadButton();
}

function toggleDatabaseMode() {
    const mode = document.getElementById('databaseMode').value;
    const defaultPreview = document.getElementById('database-default-preview');
    const uploadSection = document.getElementById('database-upload-section');
    const statusElement = document.getElementById('database-status');
    
    if (mode === 'default') {
        defaultPreview.classList.remove('hidden');
        uploadSection.classList.add('hidden');
        // Clear upload field and show default status
        document.getElementById('database').value = '';
        statusElement.textContent = 'Using default database';
    } else {
        defaultPreview.classList.add('hidden');
        uploadSection.classList.remove('hidden');
        statusElement.textContent = 'Please select a file';
    }
    validateLoadButton();
}

function validateLoadButton() {
    const loadBtn = document.getElementById('loadBtn');
    const loadWarning = document.getElementById('load-warning');
    
    // Check JSON condition
    const jsonMode = document.getElementById('jsonMode').value;
    const jsonFile = document.getElementById('jsonFile').value;
    const jsonValid = jsonMode === 'default' || (jsonMode === 'upload' && jsonFile.trim() !== '');
    
    if (!jsonValid) {
        loadBtn.classList.add('disabled');
        loadWarning.textContent = '';
        return;
    }
    
    // Check filename condition
    const fileName = document.getElementById('fileName').value;
    const fileValid = fileName.trim() !== '';
    
    if (!fileValid) {
        loadBtn.classList.add('disabled');
        loadWarning.textContent = '';
        return;
    }
    
    // Check database condition
    const databaseMode = document.getElementById('databaseMode').value;
    const database = document.getElementById('database').value;
    const databaseValid = databaseMode === 'default' || (databaseMode === 'upload' && database.trim() !== '');
    
    if (!databaseValid) {
        loadBtn.classList.add('disabled');
        loadWarning.textContent = '';
        return;
    }
    
    // All conditions met - enable button
    loadBtn.classList.remove('disabled');
    loadWarning.textContent = '';
}

function uploadFile(fileType, inputElement) {
    let textElement, statusTarget;
    
    switch(fileType) {
        case 'json':
            textElement = document.getElementById('jsonFile');
            statusTarget = '#json-status';
            break;
        case 'filename':
            textElement = document.getElementById('fileName');
            statusTarget = '#filename-status';
            break;
        case 'database':
            textElement = document.getElementById('database');
            statusTarget = '#database-status';
            break;
    }
    
    if (inputElement.files.length > 0) {
        const selectedFile = inputElement.files[0];
        textElement.value = selectedFile.name;
        
        // Create FormData to upload the file
        const formData = new FormData();
        formData.append('fileType', fileType);
        formData.append('uploadedFile', selectedFile);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        formData.append('__RequestVerificationToken', token);
        
        // Include temp directory to maintain session consistency
        const tmpdir = document.querySelector('input[name="tmpdir"]').value;
        if (tmpdir) {
            formData.append('tmpdir', tmpdir);
        }
        
        // Use fetch to upload file (HTMX doesn't handle FormData well in htmx.ajax)
        fetch('/ClaimsDataImporter?handler=FileUpload', {
            method: 'POST',
            body: formData
        })
        .then(response => response.text())
        .then(html => {
            // Parse the response which contains multiple HTML fragments
            console.log('Response HTML:', html);
            
            // Create a temporary container to parse the response
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = html;
            
            // Update status span using data attributes
            const statusSpan = tempDiv.querySelector('span[data-status]');
            if (statusSpan) {
                const targetId = statusSpan.id;
                const statusText = statusSpan.dataset.status;
                const statusElement = document.getElementById(targetId);
                if (statusElement) {
                    statusElement.textContent = statusText;
                }
            }
            
            // Update input field using data attributes
            const inputElement = tempDiv.querySelector('input[data-file-path]');
            if (inputElement) {
                const targetId = inputElement.id;
                const filePath = inputElement.dataset.filePath;
                const targetInput = document.getElementById(targetId);
                if (targetInput) {
                    targetInput.value = filePath;
                }
            }
            
            // Update upload log using data attributes
            const logDiv = tempDiv.querySelector('div[data-log-entry]');
            if (logDiv) {
                const logEntry = logDiv.dataset.logEntry;
                const uploadLog = document.getElementById('upload-log');
                uploadLog.innerHTML = logEntry + '<br/>' + uploadLog.innerHTML;
            }
            
            // Validate load button after upload
            validateLoadButton();
        })
        .catch(error => {
            console.error('Upload error:', error);
            document.querySelector(statusTarget).textContent = 'Upload failed';
        });
    } else {
        // User cancelled - clear the field and update status
        textElement.value = '';
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        htmx.ajax('POST', '/ClaimsDataImporter?handler=FileSelected', {
            target: statusTarget,
            values: {
                fileType: fileType,
                fileName: '',
                action: 'cancel',
                __RequestVerificationToken: token
            }
        });
    }
}