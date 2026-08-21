const clientMessage = (text, type = 'success') => { const element = document.querySelector('[data-message]'); element.textContent = text; element.className = `message ${type}`; };
(async function setupClientForm() {
	requireAuth();
	setupLogout();
	const form = document.querySelector('[data-client-form]');
	const clientId = new URLSearchParams(location.search).get('id');
	if (clientId) {
		document.querySelector('[data-form-title]').textContent = 'Editar cliente';
		try { const client = await apiRequest(`/clientes/${clientId}`); Object.entries(client).forEach(([key, value]) => { const input = form.elements[key]; if (input) input.value = value ?? ''; }); } catch (error) { clientMessage(error.message, 'error'); }
	}
	form.addEventListener('submit', async event => { event.preventDefault(); const data = Object.fromEntries(new FormData(event.currentTarget)); try { await apiRequest(clientId ? `/clientes/${clientId}` : '/clientes', { method: clientId ? 'PUT' : 'POST', body: JSON.stringify(data) }); clientMessage(clientId ? 'Cliente atualizado com sucesso.' : 'Cliente cadastrado com sucesso.'); if (!clientId) event.currentTarget.reset(); } catch (error) { clientMessage(error.message, 'error'); } });
})();
