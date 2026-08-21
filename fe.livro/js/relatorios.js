requireAuth();
setupLogout();
const reportMoney = value => Number(value).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const reportEscape = value => String(value || '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[character]));
const reportMessage = (text, type = 'success') => { const element = document.querySelector('[data-message]'); element.textContent = text; element.className = `message ${type}`; };
async function loadReports() {
    const start = document.querySelector('[data-start]').value;
    const end = document.querySelector('[data-end]').value;
    const query = new URLSearchParams(); if (start) query.set('dataInicial', start); if (end) query.set('dataFinal', end);
    try {
        const [summary, products] = await Promise.all([apiRequest(`/vendas/relatorios?${query}`), apiRequest('/vendas/relatorios/produtos-mais-vendidos')]);
        document.querySelector('[data-sales-count]').textContent = summary.quantidadeVendas;
        document.querySelector('[data-revenue]').textContent = reportMoney(summary.faturamento);
        document.querySelector('[data-discounts]').textContent = reportMoney(summary.descontos);
        document.querySelector('[data-products-count]').textContent = summary.produtosVendidos;
        document.querySelector('[data-top-products]').innerHTML = products.length ? products.map(product => `<tr><td>${reportEscape(product.produto)}</td><td>${product.quantidade}</td><td>${reportMoney(product.faturamento)}</td></tr>`).join('') : '<tr><td colspan="3">Nenhuma venda paga encontrada.</td></tr>';
    } catch (error) { reportMessage(error.message, 'error'); }
}
document.querySelector('[data-load]').addEventListener('click', loadReports);
loadReports();
