// Collapsible + editable model prompt controls
function expandModelPrompt() {
    const expanded = document.getElementById('modelPromptExpanded');
    const collapsed = document.getElementById('modelPromptCollapsed');
    if (expanded && collapsed) {
        expanded.classList.remove('hidden');
        collapsed.classList.add('hidden');
    }
}

function collapseModelPrompt() {
    const expanded = document.getElementById('modelPromptExpanded');
    const collapsed = document.getElementById('modelPromptCollapsed');
    if (expanded && collapsed) {
        expanded.classList.add('hidden');
        collapsed.classList.remove('hidden');
    }
    return false; // prevent anchor navigation
}

function editModelPrompt() {
    const editor = document.getElementById('modelPromptEditor');
    const readonly = document.getElementById('modelPromptReadonly');
    const editBtn = document.getElementById('editModelPromptBtn');
    const saveBtn = document.getElementById('saveModelPromptBtn');
    const cancelBtn = document.getElementById('cancelModelPromptBtn');
    if (editor && readonly && editBtn && saveBtn && cancelBtn) {
        readonly.classList.add('hidden');
        editor.classList.remove('hidden');
        editBtn.classList.add('hidden');
        saveBtn.classList.remove('hidden');
        cancelBtn.classList.remove('hidden');
        // Sync textarea with current readonly text
        const pre = readonly.querySelector('.model-prompt-pre');
        const textarea = document.getElementById('modelPromptTextarea');
        if (pre && textarea) {
            textarea.value = pre.textContent || '';
        }
    }
}

function saveModelPrompt() {
    const editor = document.getElementById('modelPromptEditor');
    const readonly = document.getElementById('modelPromptReadonly');
    const editBtn = document.getElementById('editModelPromptBtn');
    const saveBtn = document.getElementById('saveModelPromptBtn');
    const cancelBtn = document.getElementById('cancelModelPromptBtn');
    const textarea = document.getElementById('modelPromptTextarea');
    if (editor && readonly && editBtn && saveBtn && cancelBtn && textarea) {
        // Update readonly text
        const pre = readonly.querySelector('.model-prompt-pre');
        if (pre) {
            pre.textContent = textarea.value;
        }
        // Toggle back to readonly view
        editor.classList.add('hidden');
        readonly.classList.remove('hidden');
        editBtn.classList.remove('hidden');
        saveBtn.classList.add('hidden');
        cancelBtn.classList.add('hidden');
    }
}

function cancelEditModelPrompt() {
    const editor = document.getElementById('modelPromptEditor');
    const readonly = document.getElementById('modelPromptReadonly');
    const editBtn = document.getElementById('editModelPromptBtn');
    const saveBtn = document.getElementById('saveModelPromptBtn');
    const cancelBtn = document.getElementById('cancelModelPromptBtn');
    if (editor && readonly && editBtn && saveBtn && cancelBtn) {
        editor.classList.add('hidden');
        readonly.classList.remove('hidden');
        editBtn.classList.remove('hidden');
        saveBtn.classList.add('hidden');
        cancelBtn.classList.add('hidden');
    }
}

