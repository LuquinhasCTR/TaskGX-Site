using System;
using System.Threading.Tasks;
using MySqlConnector;
using TaskGX.Models;
using Microsoft.Extensions.Configuration;

namespace TaskGX.Data
{
    public class RepositorioUsuario
    {
        private readonly string _connectionString;

        public RepositorioUsuario(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public async Task<Usuarios> ObterPorEmailAsync(string email)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            string sql = @"
                SELECT 
                    ID,
                    Nome,
                    Email,
                    Senha,
                    Avatar,
                    Ativo,
                    EmailVerificado,
                    CodigoVerificacao,
                    CodigoVerificacaoExpiracao,
                    Criado_em,
                    DataAtualizacao
                FROM Usuarios
                WHERE Email = @Email
                LIMIT 1;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Email", email);

            using var leitor = await comando.ExecuteReaderAsync();
            if (!await leitor.ReadAsync())
                return null;

            return new Usuarios
            {
                ID = leitor["ID"] != DBNull.Value ? Convert.ToInt32(leitor["ID"]) : 0,
                Nome = leitor["Nome"] as string,
                Email = leitor["Email"] as string,
                Senha = leitor["Senha"] as string,
                Avatar = leitor["Avatar"] != DBNull.Value ? leitor["Avatar"].ToString() : null,
                Ativo = leitor["Ativo"] != DBNull.Value ? Convert.ToBoolean(leitor["Ativo"]) : true,
                EmailVerificado = leitor["EmailVerificado"] != DBNull.Value ? Convert.ToBoolean(leitor["EmailVerificado"]) : false,
                CodigoVerificacao = leitor["CodigoVerificacao"] != DBNull.Value ? leitor["CodigoVerificacao"].ToString() : null,
                CodigoVerificacaoExpiracao = leitor["CodigoVerificacaoExpiracao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["CodigoVerificacaoExpiracao"]) : null,
                CriadoEm = leitor["Criado_em"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["Criado_em"]) : null,
                DataAtualizacao = leitor["DataAtualizacao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["DataAtualizacao"]) : null
            };
        }

        public async Task<Usuarios> ObterPorIdAsync(int usuarioId)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                SELECT 
                    ID,
                    Nome,
                    Email,
                    Senha,
                    Avatar,
                    Ativo,
                    EmailVerificado,
                    CodigoVerificacao,
                    CodigoVerificacaoExpiracao,
                    Criado_em,
                    DataAtualizacao
                FROM Usuarios
                WHERE ID = @UsuarioId
                LIMIT 1;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            using var leitor = await comando.ExecuteReaderAsync();
            if (!await leitor.ReadAsync())
            {
                return null;
            }

            return new Usuarios
            {
                ID = leitor["ID"] != DBNull.Value ? Convert.ToInt32(leitor["ID"]) : 0,
                Nome = leitor["Nome"] as string,
                Email = leitor["Email"] as string,
                Senha = leitor["Senha"] as string,
                Avatar = leitor["Avatar"] != DBNull.Value ? leitor["Avatar"].ToString() : null,
                Ativo = leitor["Ativo"] != DBNull.Value ? Convert.ToBoolean(leitor["Ativo"]) : true,
                EmailVerificado = leitor["EmailVerificado"] != DBNull.Value ? Convert.ToBoolean(leitor["EmailVerificado"]) : false,
                CodigoVerificacao = leitor["CodigoVerificacao"] != DBNull.Value ? leitor["CodigoVerificacao"].ToString() : null,
                CodigoVerificacaoExpiracao = leitor["CodigoVerificacaoExpiracao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["CodigoVerificacaoExpiracao"]) : null,
                CriadoEm = leitor["Criado_em"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["Criado_em"]) : null,
                DataAtualizacao = leitor["DataAtualizacao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["DataAtualizacao"]) : null
            };
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            string sql = "SELECT COUNT(1) FROM Usuarios WHERE Email = @Email;";

            using var cmd = new MySqlCommand(sql, conexao);
            cmd.Parameters.AddWithValue("@Email", email);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        }

        public async Task<bool> ExisteEmailOutroAsync(int usuarioId, string email)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = "SELECT COUNT(1) FROM Usuarios WHERE Email = @Email AND ID <> @UsuarioId;";

            using var cmd = new MySqlCommand(sql, conexao);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        }

        public async Task AtualizarDadosAsync(int usuarioId, string nome, string email)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                UPDATE Usuarios
                SET Nome = @Nome,
                    Email = @Email,
                    DataAtualizacao = @DataAtualizacao
                WHERE ID = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Nome", nome);
            comando.Parameters.AddWithValue("@Email", email);
            comando.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            await comando.ExecuteNonQueryAsync();
        }

        public async Task AtualizarSenhaAsync(int usuarioId, string senhaHash)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                UPDATE Usuarios
                SET Senha = @Senha,
                    DataAtualizacao = @DataAtualizacao
                WHERE ID = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Senha", senhaHash);
            comando.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            await comando.ExecuteNonQueryAsync();
        }

        public async Task InserirAsync(Usuarios usuario)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            string sql = @"
                INSERT INTO Usuarios
                (Nome, Email, Senha, Avatar, Ativo, EmailVerificado, CodigoVerificacao, CodigoVerificacaoExpiracao, Criado_em, DataAtualizacao)
                VALUES
                (@Nome, @Email, @Senha, @Avatar, @Ativo, @EmailVerificado, @CodigoVerificacao, @CodigoVerificacaoExpiracao, @Criado_em, @DataAtualizacao);";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Nome", usuario.Nome);
            comando.Parameters.AddWithValue("@Email", usuario.Email);
            comando.Parameters.AddWithValue("@Senha", usuario.Senha);
            comando.Parameters.AddWithValue("@Avatar", (object)usuario.Avatar ?? DBNull.Value);
            comando.Parameters.AddWithValue("@Ativo", usuario.Ativo);
            comando.Parameters.AddWithValue("@EmailVerificado", usuario.EmailVerificado);
            comando.Parameters.AddWithValue("@CodigoVerificacao", (object)usuario.CodigoVerificacao ?? DBNull.Value);
            comando.Parameters.AddWithValue("@CodigoVerificacaoExpiracao", (object)usuario.CodigoVerificacaoExpiracao ?? DBNull.Value);
            comando.Parameters.AddWithValue("@Criado_em", (object)usuario.CriadoEm ?? DateTime.Now);
            comando.Parameters.AddWithValue("@DataAtualizacao", (object)usuario.DataAtualizacao ?? DateTime.Now);

            await comando.ExecuteNonQueryAsync();

            try
            {
                var last = comando.LastInsertedId;
                if (last > 0)
                    usuario.ID = Convert.ToInt32(last);
            }
            catch
            {
                // Não crítico
            }
        }

        public async Task AtualizarVerificacaoEmailAsync(int usuarioId, bool emailVerificado, bool ativo, string codigoVerificacao, DateTime? expiracao)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                UPDATE Usuarios
                SET EmailVerificado = @EmailVerificado,
                    Ativo = @Ativo,
                    CodigoVerificacao = @CodigoVerificacao,
                    CodigoVerificacaoExpiracao = @CodigoVerificacaoExpiracao,
                    DataAtualizacao = @DataAtualizacao
                WHERE ID = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@EmailVerificado", emailVerificado);
            comando.Parameters.AddWithValue("@Ativo", ativo);
            comando.Parameters.AddWithValue("@CodigoVerificacao", (object)codigoVerificacao ?? DBNull.Value);
            comando.Parameters.AddWithValue("@CodigoVerificacaoExpiracao", (object)expiracao ?? DBNull.Value);
            comando.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            await comando.ExecuteNonQueryAsync();
        }
    }
}
