# 📚 FE.ESTOQUE

Sistema web para gerenciamento de estoque de livros.

O FE.ESTOQUE tem como objetivo facilitar o cadastro, consulta,
alteração e controle de produtos de uma loja ou biblioteca.

---

## 🎯 Objetivo

Desenvolver um sistema de gerenciamento de estoque que permita:

- Cadastrar livros
- Consultar livros
- Visualizar detalhes dos produtos
- Alterar produtos
- Excluir produtos
- Controlar quantidade em estoque
- Controlar preços
- Pesquisar produtos
- Gerenciar informações dos livros

---

## 🚀 Tecnologias

### Front-end

- HTML5
- CSS3
- JavaScript

### Back-end

- .NET 10
- C#

### Banco de dados

- A definir

### Design

- Figma

### Desenvolvimento

- Visual Studio Code
- Git
- GitHub

---

## 🖥️ Funcionalidades

### 🔐 Login

O usuário poderá acessar o sistema através de:

- Login
- Senha

### 📦 Gerenciamento de produtos

Será possível cadastrar:

- Título
- Autor
- Editora
- Gênero
- Ano de publicação
- Preço
- Quantidade
- Resumo
- Imagem

### 🔎 Pesquisa

O usuário poderá pesquisar produtos cadastrados.

### 📖 Detalhes do produto

Será possível visualizar todas as informações
de um determinado livro.

### ✏️ Edição

O usuário poderá alterar as informações de um produto.

### 🗑️ Exclusão

O sistema solicitará uma confirmação antes de excluir
um produto.

---

## 🏗️ Arquitetura

O projeto será dividido em:

Frontend
↓
JavaScript
↓
API .NET 10
↓
Banco de dados

---

## ▶️ Executar localmente

1. Inicie a API pelo PowerShell:

	```powershell
	.\iniciar-api.ps1
	```

	Ou execute diretamente: `dotnet run --project .\FeEstoque.Api\FeEstoque.Api.csproj --launch-profile http`

2. Com a API em execução, abra `fe.livro/html/idex.html`. Live Server ou a abertura direta no navegador são aceitos no desenvolvimento.

3. Use um dos perfis iniciais:
	- Administrador: `admin` / `admin123`
	- Gerente de estoque: `gerente` / `gerente123`

	O SQLite é criado automaticamente em `FeEstoque.Api/feestoque.db`.

A API fica disponível em `http://localhost:5050`. Os endpoints protegidos exigem o token JWT retornado pelo login.

## 📌 Status do projeto

✅ Fluxo principal implementado

### Etapas

- [x] Protótipo inicial no Figma
- [x] Estrutura inicial do HTML
- [x] Desenvolvimento do CSS
- [x] JavaScript
- [x] API .NET 10
- [x] Banco de dados SQLite com Entity Framework Core
- [x] Integração Front-end + Back-end
- [x] Sistema de login com JWT e hash de senha
- [x] CRUD, pesquisa, dashboard e controle de estoque
- [x] Documentação de execução
- [ ] Versão final

---

## 👨‍💻 Desenvolvedor

Projeto desenvolvido como estudo e prática
de desenvolvimento de sistemas web.

---

## 📄 Licença

Este projeto está sob a licença MIT.
