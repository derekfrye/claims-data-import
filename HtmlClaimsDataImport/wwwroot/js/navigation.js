// Tab navigation functionality
function showHome() {
    document.querySelector('.nav-menu li.active').classList.remove('active');
    event.target.classList.add('active');
    
    document.getElementById('home-sidebar').classList.remove('hidden');
    document.getElementById('preview-sidebar').classList.add('hidden');
    document.getElementById('developer-sidebar').classList.add('hidden');
    
    document.getElementById('config-content').classList.remove('hidden');
    document.getElementById('preview-content').classList.add('hidden');
    document.getElementById('developer-content').classList.add('hidden');
}

function showPreview() {
    document.querySelector('.nav-menu li.active').classList.remove('active');
    event.target.classList.add('active');
    
    document.getElementById('home-sidebar').classList.add('hidden');
    document.getElementById('preview-sidebar').classList.remove('hidden');
    document.getElementById('developer-sidebar').classList.add('hidden');
    
    document.getElementById('config-content').classList.add('hidden');
    document.getElementById('preview-content').classList.remove('hidden');
    document.getElementById('developer-content').classList.add('hidden');
    
    // Load preview data automatically when preview tab is shown
    loadPreviewData();
}

function showDeveloper() {
    document.querySelector('.nav-menu li.active').classList.remove('active');
    event.target.classList.add('active');
    
    document.getElementById('home-sidebar').classList.add('hidden');
    document.getElementById('preview-sidebar').classList.add('hidden');
    document.getElementById('developer-sidebar').classList.remove('hidden');
    
    document.getElementById('config-content').classList.add('hidden');
    document.getElementById('preview-content').classList.add('hidden');
    document.getElementById('developer-content').classList.remove('hidden');
}

function showConfig() {
    // Config is the only option in Home sidebar for now
    document.querySelector('#home-sidebar li.active').classList.remove('active');
    event.target.classList.add('active');
}

function showDeveloperInfo() {
    // Info is the only option in Developer sidebar for now
    document.querySelector('#developer-sidebar li.active').classList.remove('active');
    event.target.classList.add('active');
}