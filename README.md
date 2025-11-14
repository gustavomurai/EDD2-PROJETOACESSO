# Projeto ACESSO (C#)

Sistema de controle de acessos a ambientes de uma empresa, com registro de todas as tentativas (autorizadas ou negadas) em um log por ambiente.

> Trabalho acadêmico desenvolvido em **C#** como aplicação de console no **Visual Studio**.

---

## 👥 Integrantes

- **Gustavo Cerqueira Murai**
- **Igor Cerqueira Murai**

---

## 🎯 Objetivo do Sistema

Uma empresa possui diversos ambientes (salas, laboratórios, etc.) e deseja:

- Controlar quais **usuários** têm permissão de acesso a cada **ambiente**.
- Registrar **toda ação de acesso**:
  - Acessos **autorizados**
  - Tentativas de acesso **negadas**
- Manter, para cada ambiente, uma fila de **no máximo 100 logs**, descartando sempre o log mais antigo quando o limite é atingido.
- Permitir **cadastro, consulta, exclusão e permissões** de usuários e ambientes por meio de um menu interativo em console.

---

## 🧱 Modelagem de Classes (C#)

### Classe `Usuario`

Representa uma pessoa que pode tentar acessar os ambientes.

- **Atributos**:
  - `int Id`
  - `string Nome`
  - `List<Ambiente> Ambientes`  
    (lista de ambientes para os quais o usuário tem permissão)

- **Métodos principais**:
  - `bool ConcederPermissao(Ambiente ambiente)`
    - Adiciona o ambiente na lista de permissões do usuário.
    - Retorna `false` se o usuário já tiver permissão para aquele ambiente.
  - `bool RevogarPermissao(Ambiente ambiente)`
    - Remove a permissão para o ambiente.
    - Retorna `false` se o usuário não tiver permissão para aquele ambiente.

- **Regra**:
  - Cada usuário só pode ter **uma permissão por ambiente** (não pode duplicar o mesmo ambiente na lista).

---

### Classe `Ambiente`

Representa cada sala/setor da empresa.

- **Atributos**:
  - `int Id`
  - `string Nome`
  - `Queue<Log> Logs`  
    (fila de logs com tamanho máximo de 100 registros)

- **Métodos**:
  - `void RegistrarLog(Log log)`
    - Se a fila tiver 100 itens, remove o **mais antigo** com `Dequeue()`.
    - Adiciona o novo log com `Enqueue()`.

- **Regra**:
  - Cada ambiente pode armazenar **no máximo 100 logs**. Se chegar nesse limite, sempre o log mais antigo é descartado.

---

### Classe `Log`

Representa uma tentativa de acesso (autorizada ou não).

- **Atributos**:
  - `DateTime DtAcesso` – data/hora da tentativa
  - `Usuario Usuario` – usuário que tentou acessar
  - `bool TipoAcesso` – `true` para **autorizado** / `false` para **negado**

Cada log é gerado quando a opção **“Registrar acesso”** é utilizada no menu.

---

### Classe `Cadastro`

Responsável por guardar e gerenciar todos os dados do sistema.

- **Atributos**:
  - `List<Usuario> Usuarios`
  - `List<Ambiente> Ambientes`

- **Métodos para usuários**:
  - `void AdicionarUsuario(Usuario usuario)`
  - `bool RemoverUsuario(Usuario usuario)`
    - Só remove se o usuário não tiver nenhuma permissão.
  - `Usuario PesquisarUsuarioPorId(int id)`

- **Métodos para ambientes**:
  - `void AdicionarAmbiente(Ambiente ambiente)`
  - `bool RemoverAmbiente(Ambiente ambiente)`
    - Remove o ambiente da lista.
    - Remove também as permissões desse ambiente dos usuários.
  - `Ambiente PesquisarAmbientePorId(int id)`

- **Persistência (arquivos)**:
  - `void Upload()`
    - Salva usuários, ambientes e logs em arquivos texto:
      - `ambientes.txt`
      - `usuarios.txt`
      - `logs.txt`
  - `void Download()`
    - Lê os arquivos ao iniciar o programa,
    - Recria a lista de ambientes, a lista de usuários, as permissões e os logs.

---

## 📋 Menu de Opções (Console)

O programa exibe um menu no `Main` com as seguintes opções:

0. **Sair**  
   - Encerra o programa.  
   - Antes de sair, chama `Upload()` para salvar os dados em arquivo.

1. **Cadastrar ambiente**  
   - Solicita `ID` e `Nome` do ambiente.  
   - Cria um novo objeto `Ambiente` e chama `AdicionarAmbiente()`.

2. **Consultar ambiente**  
   - Solicita o `ID` do ambiente.  
   - Exibe:
     - Dados básicos do ambiente (`Id` e `Nome`)
     - Quantidade de logs já registrados.

3. **Excluir ambiente**  
   - Solicita `ID` do ambiente.  
   - Procura o ambiente e chama `RemoverAmbiente()`.  
   - Remove também as permissões desse ambiente dos usuários.

4. **Cadastrar usuário**  
   - Solicita `ID` e `Nome` do usuário.  
   - Cria um `Usuario` e chama `AdicionarUsuario()`.

5. **Consultar usuário**  
   - Solicita `ID` do usuário.  
   - Exibe:
     - Dados do usuário
     - Lista de ambientes para os quais ele possui permissão.

6. **Excluir usuário**  
   - Solicita `ID` do usuário.  
   - Só consegue excluir se o usuário não tiver nenhuma permissão.  
   - Usa `RemoverUsuario()` e informa se a remoção foi bem-sucedida.

7. **Conceder permissão de acesso ao usuário**  
   - Solicita:
     - `ID do usuário`
     - `ID do ambiente`
   - Verifica se existem usuário e ambiente.  
   - Chama `ConcederPermissao(ambiente)` no usuário.  
   - Se a permissão já existir, retorna `false` e informa que o usuário já tinha permissão.

8. **Revogar permissão de acesso ao usuário**  
   - Solicita:
     - `ID do usuário`
     - `ID do ambiente`
   - Chama `RevogarPermissao(ambiente)` e indica se a revogação funcionou.

9. **Registrar acesso**  
   - Solicita:
     - `ID do usuário`
     - `ID do ambiente`
   - Verifica se o usuário tem permissão para o ambiente:
     - Se tiver: registra um log com `TipoAcesso = true` (AUTORIZADO).
     - Se não tiver: registra um log com `TipoAcesso = false` (NEGADO).
   - Chama `RegistrarLog(log)` no ambie
