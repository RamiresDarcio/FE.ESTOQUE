requireAuth();
setupLogout();
const clientMessage = (text, type = 'success') => { const element = document.querySelector('[data-message]'); element.textContent = text; element.className = `message ${type}`; };
const clientEscape = value => String(value || '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[character]));
async function loadClients() {
    const list = document.querySelector('[data-clients]');
    const search = document.querySelector('[data-search]');
    const refresh = async () => {
        try {
            const clients = await apiRequest(`/clientes${search.value.trim() ? `?busca=${encodeURIComponent(search.value.trim())}` : ''}`);
            list.innerHTML = clients.length ? clients.map(client => `<tr><td>${clientEscape(client.nome)}</td><td>${clientEscape(client.cpf) || '-'}</td><td>${clientEscape(client.email) || '-'}</td><td>${clientEscape(client.telefone) || '-'}</td><td><a class="button secondary" href="cliente.html?id=${client.id}">Editar</a> <button class="button danger" data-delete="${client.id}">Excluir</button></td></tr>`).join('') : '<tr><td colspan="5">Nenhum cliente encontrado.</td></tr>';
            list.querySelectorAll('[data-delete]').forEach(button => button.addEventListener('click', async () => { if (!confirm('Excluir este cliente?')) return; try { await apiRequest(`/clientes/${button.dataset.delete}`, { method: 'DELETE' }); clientMessage('Cliente excluído com sucesso.'); await refresh(); } catch (error) { clientMessage(error.message, 'error'); } }));
        } catch (error) { clientMessage(error.message, 'error'); }
    };
    search.addEventListener('input', refresh); await refresh();
}
loadClients();
