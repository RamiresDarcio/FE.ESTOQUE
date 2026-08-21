const API_URL = 'http://localhost:5050/api';

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
        try { message = (await response.json()).mensagem || message; } catch { /* resposta sem JSON */ }
        throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
}
