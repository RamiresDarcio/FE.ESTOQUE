const money = value => Number(value).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const imageOrPlaceholder = image => image || 'https://images.unsplash.com/photo-1544947950-fa07a98d237f?auto=format&fit=crop&w=600&q=80';

function showMessage(text, type = 'success') {
    const element = document.querySelector('[data-dashboard-view] [data-message]') || document.querySelector('[data-message]');
    if (!element) return;
    element.textContent = text;
    element.className = `message ${type}`;
    window.setTimeout(() => { element.textContent = ''; element.className = 'message'; }, 4000);
}

async function loadDashboard() {
    requireAuth(); setupLogout();
    try {
        const data = await apiRequest('/dashboard');
        document.querySelector('[data-total-livros]').textContent = data.totalLivros;
        document.querySelector('[data-total-estoque]').textContent = data.totalEstoque;
        document.querySelector('[data-valor-estoque]').textContent = money(data.valorEstoque);
        document.querySelector('[data-estoque-baixo]').textContent = data.estoqueBaixo;
        document.querySelector('[data-vendas-hoje]')?.replaceChildren(String(data.vendasHoje));
        document.querySelector('[data-faturamento-hoje]')?.replaceChildren(money(data.faturamentoHoje));
        document.querySelector('[data-faturamento-mes]')?.replaceChildren(money(data.faturamentoMes));
        document.querySelector('[data-ticket-medio]')?.replaceChildren(money(data.ticketMedio));
    } catch (error) { showMessage(error.message, 'error'); }
}

async function loadBooks() {
    requireAuth(); setupLogout();
    const list = document.querySelector('[data-books]');
    const search = document.querySelector('[data-search]');
    const render = books => {
        list.innerHTML = books.length ? books.map(book => `<article class="book-card"><img src="${imageOrPlaceholder(book.imagem)}" alt="Capa de ${escapeHtml(book.titulo)}"><div class="book-copy"><span class="eyebrow">${escapeHtml(book.genero)}</span><h2>${escapeHtml(book.titulo)}</h2><p>${escapeHtml(book.autor)}</p><div class="book-meta"><strong>${money(book.preco)}</strong><span>${book.quantidade} em estoque</span></div><div class="card-actions"><a class="button secondary" href="produto.html?id=${book.id}">Visualizar</a><a class="button secondary" href="gerenciar_de_produto.html?id=${book.id}">Editar</a><button class="button danger" data-delete="${book.id}">Excluir</button></div></div></article>`).join('') : '<p class="empty">Nenhum livro encontrado.</p>';
        list.querySelectorAll('[data-delete]').forEach(button => button.addEventListener('click', async () => { if (!confirm('Tem certeza que deseja excluir este livro?')) return; try { await apiRequest(`/livros/${button.dataset.delete}`, { method: 'DELETE' }); showMessage('Livro excluído com sucesso.'); await refresh(); } catch (error) { showMessage(error.message, 'error'); } }));
    };
    const refresh = async () => { try { render(await apiRequest(`/livros${search.value.trim() ? `?busca=${encodeURIComponent(search.value.trim())}` : ''}`)); } catch (error) { showMessage(error.message, 'error'); } };
    search.addEventListener('input', refresh); await refresh();
}

async function loadBookDetail() {
    requireAuth(); setupLogout();
    const id = new URLSearchParams(location.search).get('id');
    try {
        const book = await apiRequest(`/livros/${id}`);
        document.querySelector('[data-detail]').innerHTML = `<img class="detail-image" src="${imageOrPlaceholder(book.imagem)}" alt="Capa de ${escapeHtml(book.titulo)}"><div><span class="eyebrow">${escapeHtml(book.genero)}</span><h1>${escapeHtml(book.titulo)}</h1><p class="lead">${escapeHtml(book.resumo)}</p><dl class="details"><div><dt>Autor</dt><dd>${escapeHtml(book.autor)}</dd></div><div><dt>Editora</dt><dd>${escapeHtml(book.editora)}</dd></div><div><dt>Ano</dt><dd>${book.anoPublicacao}</dd></div><div><dt>Estoque</dt><dd>${book.quantidade} unidades</dd></div><div><dt>Preço</dt><dd>${money(book.preco)}</dd></div></dl><div class="card-actions"><a class="button primary" href="gerenciar_de_produto.html?id=${book.id}">Editar livro</a><button class="button danger" data-detail-delete>Excluir</button></div></div>`;
        document.querySelector('[data-detail-delete]').addEventListener('click', async () => { if (!confirm('Tem certeza que deseja excluir este livro?')) return; await apiRequest(`/livros/${id}`, { method: 'DELETE' }); location.href = 'pesquisar.html'; });
    } catch (error) { showMessage(error.message, 'error'); }
}

async function loadBookForm() {
    requireAuth(); setupLogout();
    const form = document.querySelector('[data-book-form]');
    const id = new URLSearchParams(location.search).get('id');
    if (id) { document.querySelector('[data-form-title]').textContent = 'Editar livro'; try { const book = await apiRequest(`/livros/${id}`); Object.entries(book).forEach(([key, value]) => { const input = form.elements[key]; if (input) input.value = value ?? ''; }); } catch (error) { showMessage(error.message, 'error'); } }
    form.addEventListener('submit', async event => { event.preventDefault(); const data = Object.fromEntries(new FormData(form)); data.anoPublicacao = Number(data.anoPublicacao); data.preco = Number(data.preco); data.quantidade = Number(data.quantidade); try { await apiRequest(id ? `/livros/${id}` : '/livros', { method: id ? 'PUT' : 'POST', body: JSON.stringify(data) }); location.href = 'pesquisar.html'; } catch (error) { showMessage(error.message, 'error'); } });
}

function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[character])); }
