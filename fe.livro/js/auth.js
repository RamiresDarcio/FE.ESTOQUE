function requireAuth() {
    if (!localStorage.getItem('feestoque_token')) location.href = 'idex.html';
}

function logout() {
    localStorage.removeItem('feestoque_token');
    localStorage.removeItem('feestoque_user');
    location.href = 'idex.html';
}

function setupLogout() {
    document.querySelectorAll('[data-logout]').forEach(button => button.addEventListener('click', logout));
}
