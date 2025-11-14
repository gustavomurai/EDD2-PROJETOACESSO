using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjetoAcessoCS
{
    // ------------------------------
    // CLASSE LOG
    // ------------------------------
    // Representa uma tentativa de acesso (autorizada ou negada)
    class Log
    {
        public DateTime DtAcesso { get; set; }   // Data e hora do acesso
        public Usuario Usuario { get; set; }     // Usuário que tentou acessar
        public bool TipoAcesso { get; set; }     // true = Autorizado, false = Negado

        public Log(DateTime dtAcesso, Usuario usuario, bool tipoAcesso)
        {
            DtAcesso = dtAcesso;
            Usuario = usuario;
            TipoAcesso = tipoAcesso;
        }
    }

    // ------------------------------
    // CLASSE AMBIENTE
    // ------------------------------
    // Cada sala/ambiente da empresa
    class Ambiente
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        // Fila de logs com no máximo 100 ocorrências
        public Queue<Log> Logs { get; private set; }

        public Ambiente(int id, string nome)
        {
            Id = id;
            Nome = nome;
            Logs = new Queue<Log>();
        }

        // Registra um log, garantindo o máximo de 100
        public void RegistrarLog(Log log)
        {
            if (Logs.Count == 100)
            {
                // Remove o mais antigo
                Logs.Dequeue();
            }
            Logs.Enqueue(log);
        }

        public override string ToString()
        {
            return $"[{Id}] {Nome}";
        }
    }

    // ------------------------------
    // CLASSE USUARIO
    // ------------------------------
    // Representa um usuário da empresa
    class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; }

        // Ambientes para os quais ele tem permissão
        public List<Ambiente> Ambientes { get; private set; }

        public Usuario(int id, string nome)
        {
            Id = id;
            Nome = nome;
            Ambientes = new List<Ambiente>();
        }

        // Conceder permissão para um ambiente
        // Só pode ter uma permissão por ambiente
        public bool ConcederPermissao(Ambiente ambiente)
        {
            // Verifica se já existe esse ambiente na lista
            if (Ambientes.Any(a => a.Id == ambiente.Id))
            {
                return false; // já tinha permissão
            }

            Ambientes.Add(ambiente);
            return true;
        }

        // Revogar permissão de um ambiente
        public bool RevogarPermissao(Ambiente ambiente)
        {
            var existente = Ambientes.FirstOrDefault(a => a.Id == ambiente.Id);
            if (existente == null)
            {
                return false; // não tinha permissão
            }

            Ambientes.Remove(existente);
            return true;
        }

        public override string ToString()
        {
            return $"[{Id}] {Nome}";
        }
    }

    // ------------------------------
    // CLASSE CADASTRO
    // ------------------------------
    // Guarda todos os usuários e ambientes
    // e faz upload/download (persistência em arquivo)
    class Cadastro
    {
        public List<Usuario> Usuarios { get; private set; }
        public List<Ambiente> Ambientes { get; private set; }

        private const string ArquivoAmbientes = "ambientes.txt";
        private const string ArquivoUsuarios = "usuarios.txt";
        private const string ArquivoLogs = "logs.txt";

        public Cadastro()
        {
            Usuarios = new List<Usuario>();
            Ambientes = new List<Ambiente>();
        }

        // ------------------------------
        // MÉTODOS DE USUÁRIO
        // ------------------------------
        public void AdicionarUsuario(Usuario usuario)
        {
            // Evita IDs repetidos
            if (Usuarios.Any(u => u.Id == usuario.Id))
            {
                throw new Exception("Já existe um usuário com esse ID.");
            }
            Usuarios.Add(usuario);
        }

        public bool RemoverUsuario(Usuario usuario)
        {
            // Só pode remover se não tiver nenhuma permissão
            if (usuario.Ambientes.Count > 0)
            {
                return false;
            }

            return Usuarios.Remove(usuario);
        }

        public Usuario PesquisarUsuarioPorId(int id)
        {
            return Usuarios.FirstOrDefault(u => u.Id == id);
        }

        // ------------------------------
        // MÉTODOS DE AMBIENTE
        // ------------------------------
        public void AdicionarAmbiente(Ambiente ambiente)
        {
            if (Ambientes.Any(a => a.Id == ambiente.Id))
            {
                throw new Exception("Já existe um ambiente com esse ID.");
            }
            Ambientes.Add(ambiente);
        }

        public bool RemoverAmbiente(Ambiente ambiente)
        {
            // Se remover ambiente, idealmente também remover
            // permissões dos usuários (senão ficam apontando pra nada)
            foreach (var usuario in Usuarios)
            {
                var amb = usuario.Ambientes.FirstOrDefault(a => a.Id == ambiente.Id);
                if (amb != null)
                {
                    usuario.Ambientes.Remove(amb);
                }
            }

            return Ambientes.Remove(ambiente);
        }

        public Ambiente PesquisarAmbientePorId(int id)
        {
            return Ambientes.FirstOrDefault(a => a.Id == id);
        }

        // ------------------------------
        // PERSISTÊNCIA EM ARQUIVOS
        // ------------------------------
        // Formatos simples (texto):
        // ambientes.txt:  id;nome
        // usuarios.txt :  id;nome;idAmb1,idAmb2,...
        // logs.txt     :  dataIso;idUsuario;idAmbiente;tipo(1/0)

        public void Upload()
        {
            // Salva ambientes
            using (var writer = new StreamWriter(ArquivoAmbientes, false))
            {
                foreach (var a in Ambientes)
                {
                    writer.WriteLine($"{a.Id};{a.Nome}");
                }
            }

            // Salva usuários + permissões
            using (var writer = new StreamWriter(ArquivoUsuarios, false))
            {
                foreach (var u in Usuarios)
                {
                    string idsAmbientes = string.Join(",", u.Ambientes.Select(a => a.Id));
                    writer.WriteLine($"{u.Id};{u.Nome};{idsAmbientes}");
                }
            }

            // Salva logs (percorrendo ambientes)
            using (var writer = new StreamWriter(ArquivoLogs, false))
            {
                foreach (var a in Ambientes)
                {
                    foreach (var log in a.Logs)
                    {
                        // Formato de data ISO (que é fácil de ler depois)
                        string data = log.DtAcesso.ToString("o");
                        int idUsuario = log.Usuario.Id;
                        int idAmbiente = a.Id;
                        int tipo = log.TipoAcesso ? 1 : 0;

                        writer.WriteLine($"{data};{idUsuario};{idAmbiente};{tipo}");
                    }
                }
            }
        }

        public void Download()
        {
            Usuarios.Clear();
            Ambientes.Clear();

            // --------------------------
            // Carrega ambientes
            // --------------------------
            if (File.Exists(ArquivoAmbientes))
            {
                var linhasAmbientes = File.ReadAllLines(ArquivoAmbientes);
                foreach (var linha in linhasAmbientes)
                {
                    if (string.IsNullOrWhiteSpace(linha)) continue;
                    var partes = linha.Split(';');
                    int id = int.Parse(partes[0]);
                    string nome = partes[1];
                    Ambientes.Add(new Ambiente(id, nome));
                }
            }

            // --------------------------
            // Carrega usuários
            // --------------------------
            if (File.Exists(ArquivoUsuarios))
            {
                var linhasUsuarios = File.ReadAllLines(ArquivoUsuarios);
                foreach (var linha in linhasUsuarios)
                {
                    if (string.IsNullOrWhiteSpace(linha)) continue;
                    var partes = linha.Split(';');
                    int id = int.Parse(partes[0]);
                    string nome = partes[1];
                    string idsAmbientes = partes.Length > 2 ? partes[2] : "";

                    var usuario = new Usuario(id, nome);
                    Usuarios.Add(usuario);

                    if (!string.IsNullOrWhiteSpace(idsAmbientes))
                    {
                        var ids = idsAmbientes.Split(',');
                        foreach (var idStr in ids)
                        {
                            if (int.TryParse(idStr, out int idAmb))
                            {
                                var ambiente = PesquisarAmbientePorId(idAmb);
                                if (ambiente != null)
                                {
                                    usuario.ConcederPermissao(ambiente);
                                }
                            }
                        }
                    }
                }
            }

            // --------------------------
            // Carrega logs
            // --------------------------
            if (File.Exists(ArquivoLogs))
            {
                var linhasLogs = File.ReadAllLines(ArquivoLogs);
                foreach (var linha in linhasLogs)
                {
                    if (string.IsNullOrWhiteSpace(linha)) continue;

                    var partes = linha.Split(';');
                    string dataIso = partes[0];
                    int idUsuario = int.Parse(partes[1]);
                    int idAmbiente = int.Parse(partes[2]);
                    int tipoInt = int.Parse(partes[3]);

                    DateTime data = DateTime.Parse(dataIso);
                    bool tipoAcesso = tipoInt == 1;

                    var usuario = PesquisarUsuarioPorId(idUsuario);
                    var ambiente = PesquisarAmbientePorId(idAmbiente);

                    if (usuario != null && ambiente != null)
                    {
                        var log = new Log(data, usuario, tipoAcesso);
                        ambiente.RegistrarLog(log);
                    }
                }
            }
        }
    }

    // ------------------------------
    // PROGRAMA PRINCIPAL (MENU)
    // ------------------------------
    class Program
    {
        static void Main(string[] args)
        {
            Cadastro cadastro = new Cadastro();

            // Carrega dados do disco (download)
            cadastro.Download();

            int opcao = -1;
            while (opcao != 0)
            {
                Console.Clear();
                Console.WriteLine("==== PROJETO ACESSO - C# ====");
                Console.WriteLine("0. Sair");
                Console.WriteLine("1. Cadastrar ambiente");
                Console.WriteLine("2. Consultar ambiente");
                Console.WriteLine("3. Excluir ambiente");
                Console.WriteLine("4. Cadastrar usuario");
                Console.WriteLine("5. Consultar usuario");
                Console.WriteLine("6. Excluir usuario");
                Console.WriteLine("7. Conceder permissão de acesso ao usuario");
                Console.WriteLine("8. Revogar permissão de acesso ao usuario");
                Console.WriteLine("9. Registrar acesso");
                Console.WriteLine("10. Consultar logs de acesso");
                Console.Write("Escolha uma opcao: ");

                int.TryParse(Console.ReadLine(), out opcao);
                Console.WriteLine();

                try
                {
                    switch (opcao)
                    {
                        case 0:
                            // Antes de sair, salva (upload)
                            cadastro.Upload();
                            Console.WriteLine("Salvando dados e saindo...");
                            break;

                        case 1:
                            CadastrarAmbiente(cadastro);
                            break;

                        case 2:
                            ConsultarAmbiente(cadastro);
                            break;

                        case 3:
                            ExcluirAmbiente(cadastro);
                            break;

                        case 4:
                            CadastrarUsuario(cadastro);
                            break;

                        case 5:
                            ConsultarUsuario(cadastro);
                            break;

                        case 6:
                            ExcluirUsuario(cadastro);
                            break;

                        case 7:
                            ConcederPermissao(cadastro);
                            break;

                        case 8:
                            RevogarPermissao(cadastro);
                            break;

                        case 9:
                            RegistrarAcesso(cadastro);
                            break;

                        case 10:
                            ConsultarLogs(cadastro);
                            break;

                        default:
                            Console.WriteLine("Opcao invalida.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERRO: " + ex.Message);
                }

                if (opcao != 0)
                {
                    Console.WriteLine("\nPressione ENTER para continuar...");
                    Console.ReadLine();
                }
            }
        }

        // 1. Cadastrar ambiente
        static void CadastrarAmbiente(Cadastro cadastro)
        {
            Console.Write("ID do ambiente: ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            Console.Write("Nome do ambiente: ");
            string nome = Console.ReadLine();

            var ambiente = new Ambiente(id, nome);
            cadastro.AdicionarAmbiente(ambiente);

            Console.WriteLine("Ambiente cadastrado com sucesso.");
        }

        // 2. Consultar ambiente
        static void ConsultarAmbiente(Cadastro cadastro)
        {
            Console.Write("Informe o ID do ambiente: ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            var ambiente = cadastro.PesquisarAmbientePorId(id);
            if (ambiente == null)
            {
                Console.WriteLine("Ambiente nao encontrado.");
                return;
            }

            Console.WriteLine("Ambiente encontrado: " + ambiente);
            Console.WriteLine("Quantidade de logs: " + ambiente.Logs.Count);
        }

        // 3. Excluir ambiente
        static void ExcluirAmbiente(Cadastro cadastro)
        {
            Console.Write("Informe o ID do ambiente a excluir: ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            var ambiente = cadastro.PesquisarAmbientePorId(id);
            if (ambiente == null)
            {
                Console.WriteLine("Ambiente nao encontrado.");
                return;
            }

            bool ok = cadastro.RemoverAmbiente(ambiente);
            Console.WriteLine(ok ? "Ambiente removido." : "Nao foi possivel remover o ambiente.");
        }

        // 4. Cadastrar usuario
        static void CadastrarUsuario(Cadastro cadastro)
        {
            Console.Write("ID do usuario: ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            Console.Write("Nome do usuario: ");
            string nome = Console.ReadLine();

            var usuario = new Usuario(id, nome);
            cadastro.AdicionarUsuario(usuario);

            Console.WriteLine("Usuario cadastrado com sucesso.");
        }

        // 5. Consultar usuario
        static void ConsultarUsuario(Cadastro cadastro)
        {
            Console.Write("Informe o ID do usuario: ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            var usuario = cadastro.PesquisarUsuarioPorId(id);
            if (usuario == null)
            {
                Console.WriteLine("Usuario nao encontrado.");
                return;
            }

            Console.WriteLine("Usuario encontrado: " + usuario);
            Console.WriteLine("Permissoes (ambientes):");
            foreach (var amb in usuario.Ambientes)
            {
                Console.WriteLine(" - " + amb);
            }
        }

        // 6. Excluir usuario
        static void ExcluirUsuario(Cadastro cadastro)
        {
            Console.Write("Informe o ID do usuario a excluir: ");
            int id = int.Parse(Console.ReadLine() ?? "0");

            var usuario = cadastro.PesquisarUsuarioPorId(id);
            if (usuario == null)
            {
                Console.WriteLine("Usuario nao encontrado.");
                return;
            }

            bool ok = cadastro.RemoverUsuario(usuario);
            if (!ok)
            {
                Console.WriteLine("Nao foi possivel remover. O usuario ainda possui permissoes em ambientes.");
            }
            else
            {
                Console.WriteLine("Usuario removido.");
            }
        }

        // 7. Conceder permissão
        static void ConcederPermissao(Cadastro cadastro)
        {
            Console.Write("ID do usuario: ");
            int idUsuario = int.Parse(Console.ReadLine() ?? "0");
            var usuario = cadastro.PesquisarUsuarioPorId(idUsuario);
            if (usuario == null)
            {
                Console.WriteLine("Usuario nao encontrado.");
                return;
            }

            Console.Write("ID do ambiente: ");
            int idAmbiente = int.Parse(Console.ReadLine() ?? "0");
            var ambiente = cadastro.PesquisarAmbientePorId(idAmbiente);
            if (ambiente == null)
            {
                Console.WriteLine("Ambiente nao encontrado.");
                return;
            }

            bool ok = usuario.ConcederPermissao(ambiente);
            Console.WriteLine(ok ? "Permissao concedida." : "Usuario ja possuia permissao para esse ambiente.");
        }

        // 8. Revogar permissão
        static void RevogarPermissao(Cadastro cadastro)
        {
            Console.Write("ID do usuario: ");
            int idUsuario = int.Parse(Console.ReadLine() ?? "0");
            var usuario = cadastro.PesquisarUsuarioPorId(idUsuario);
            if (usuario == null)
            {
                Console.WriteLine("Usuario nao encontrado.");
                return;
            }

            Console.Write("ID do ambiente: ");
            int idAmbiente = int.Parse(Console.ReadLine() ?? "0");
            var ambiente = cadastro.PesquisarAmbientePorId(idAmbiente);
            if (ambiente == null)
            {
                Console.WriteLine("Ambiente nao encontrado.");
                return;
            }

            bool ok = usuario.RevogarPermissao(ambiente);
            Console.WriteLine(ok ? "Permissao revogada." : "Usuario nao possuia permissao para esse ambiente.");
        }

        // 9. Registrar acesso
        static void RegistrarAcesso(Cadastro cadastro)
        {
            Console.Write("ID do usuario: ");
            int idUsuario = int.Parse(Console.ReadLine() ?? "0");
            var usuario = cadastro.PesquisarUsuarioPorId(idUsuario);
            if (usuario == null)
            {
                Console.WriteLine("Usuario nao encontrado.");
                return;
            }

            Console.Write("ID do ambiente: ");
            int idAmbiente = int.Parse(Console.ReadLine() ?? "0");
            var ambiente = cadastro.PesquisarAmbientePorId(idAmbiente);
            if (ambiente == null)
            {
                Console.WriteLine("Ambiente nao encontrado.");
                return;
            }

            // Verifica se o usuário tem permissão para o ambiente
            bool autorizado = usuario.Ambientes.Any(a => a.Id == ambiente.Id);

            var log = new Log(DateTime.Now, usuario, autorizado);
            ambiente.RegistrarLog(log);

            Console.WriteLine(autorizado ? "Acesso AUTORIZADO." : "Acesso NEGADO.");
        }

        // 10. Consultar logs
        static void ConsultarLogs(Cadastro cadastro)
        {
            Console.Write("ID do ambiente: ");
            int idAmbiente = int.Parse(Console.ReadLine() ?? "0");
            var ambiente = cadastro.PesquisarAmbientePorId(idAmbiente);
            if (ambiente == null)
            {
                Console.WriteLine("Ambiente nao encontrado.");
                return;
            }

            Console.WriteLine("Filtrar logs:");
            Console.WriteLine("1 - Apenas AUTORIZADOS");
            Console.WriteLine("2 - Apenas NEGADOS");
            Console.WriteLine("3 - TODOS");
            Console.Write("Opcao: ");
            int filtro = int.Parse(Console.ReadLine() ?? "3");

            Console.WriteLine($"\nLogs do ambiente {ambiente.Nome}:");
            foreach (var log in ambiente.Logs)
            {
                if (filtro == 1 && !log.TipoAcesso) continue;
                if (filtro == 2 && log.TipoAcesso) continue;

                string tipo = log.TipoAcesso ? "AUTORIZADO" : "NEGADO";
                Console.WriteLine($"{log.DtAcesso:dd/MM/yyyy HH:mm:ss} - Usuario: {log.Usuario.Nome} - {tipo}");
            }
        }
    }
}
