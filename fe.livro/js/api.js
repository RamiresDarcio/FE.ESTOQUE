const API_URL = 'http://localhost:5050/api';

const savedTheme = localStorage.getItem('feestoque_theme') || 'dark';
document.documentElement.dataset.theme = savedTheme;

function setupTheme() {
    const target = document.querySelector('.nav') || document.querySelector('[data-login-view] .login-box');
    if (!target || document.querySelector('[data-theme-toggle]')) return;
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'theme-toggle';
    button.dataset.themeToggle = '';
    const updateLabel = () => {
        const dark = document.documentElement.dataset.theme === 'dark';
        button.textContent = dark ? 'Tema claro' : 'Tema escuro';
        button.setAttribute('aria-label', dark ? 'Ativar tema claro' : 'Ativar tema escuro');
    };
    button.addEventListener('click', () => {
        const theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
        document.documentElement.dataset.theme = theme;
        localStorage.setItem('feestoque_theme', theme);
        updateLabel();
    });
    target.append(button);
    updateLabel();
}

setupTheme();

async function apiRequest(path, options = {}) {
    const token = localStorage.getItem('feestoque_token');
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    if (token) headers.Authorization = `Bearer ${token}`;

    let response;
    try {
        response = await fetch(`${API_URL}${path}`, { ...options, headers });
    } catch {
        throw new Error('Não foi possível conectar à API. Inicie o backend na porta 5050.');
    }
    if (response.status === 401) {
        localStorage.removeItem('feestoque_token');
        localStorage.removeItem('feestoque_user');
        if (!location.pathname.endsWith('idex.html')) location.href = 'idex.html';
    }
    if (!response.ok) {
        let message = 'Não foi possível concluir a operação.';
        try {
            const body = await response.json();
            const validation = body.errors ? Object.values(body.errors).flat().join(' ') : '';
            message = body.mensagem || body.detail || body.title || validation || message;
        } catch { /* resposta sem JSON */ }
        console.error(`API ${response.status} em ${path}: ${message}`);
        throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
}
