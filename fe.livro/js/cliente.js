requireAuth();
setupLogout();
const clientMessage = (text, type = 'success') => { const element = document.querySelector('[data-message]'); element.textContent = text; element.className = `message ${type}`; };
document.querySelector('[data-client-form]').addEventListener('submit', async event => { event.preventDefault(); const data = Object.fromEntries(new FormData(event.currentTarget)); try { await apiRequest('/clientes', { method: 'POST', body: JSON.stringify(data) }); clientMessage('Cliente cadastrado com sucesso.'); event.currentTarget.reset(); } catch (error) { clientMessage(error.message, 'error'); } });
