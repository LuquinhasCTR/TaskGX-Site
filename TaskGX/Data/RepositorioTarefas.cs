using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace TaskGX.Data
{
    public class RepositorioTarefas
    {
        private readonly string _connectionString;
        private const string ListaPadraoNome = "Sem Lista";

        public RepositorioTarefas(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public async Task<int> CriarListaAsync(int usuarioId, string nome, string? cor, bool favoritaExists)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            var colunasFavorita = favoritaExists ? ", Favorita" : string.Empty;
            var valoresFavorita = favoritaExists ? ", @Favorita" : string.Empty;

            var sql = $@"
                INSERT INTO Listas (Usuario_id, Nome, Cor, DataCriacao{colunasFavorita})
                VALUES (@UsuarioId, @Nome, @Cor, @DataCriacao{valoresFavorita});";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
            comando.Parameters.AddWithValue("@Nome", nome);
            comando.Parameters.AddWithValue("@Cor", string.IsNullOrWhiteSpace(cor) ? DBNull.Value : cor);
            comando.Parameters.AddWithValue("@DataCriacao", DateTime.Now);
            if (favoritaExists)
            {
                comando.Parameters.AddWithValue("@Favorita", 0);
            }

            await comando.ExecuteNonQueryAsync();

            return comando.LastInsertedId > 0 ? Convert.ToInt32(comando.LastInsertedId) : 0;
        }

        public async Task<int> ObterOuCriarListaPadraoAsync(int usuarioId, bool favoritaExists)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                SELECT ID
                FROM Listas
                WHERE Usuario_id = @UsuarioId AND Nome = @Nome
                LIMIT 1;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
            comando.Parameters.AddWithValue("@Nome", ListaPadraoNome);

            var resultado = await comando.ExecuteScalarAsync();
            if (resultado != null && resultado != DBNull.Value)
            {
                return Convert.ToInt32(resultado);
            }

            return await CriarListaAsync(usuarioId, ListaPadraoNome, null, favoritaExists);
        }

        public async Task<int> CriarTarefaAsync(int? listaId, string titulo, string? descricao, int? prioridadeId, DateTime? dataVencimento, string? tags)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                INSERT INTO Tarefas (Lista_id, Titulo, Descricao, Prioridade_id, DataVencimento, Tags, Concluida, DataCriacao)
                VALUES (@ListaId, @Titulo, @Descricao, @PrioridadeId, @DataVencimento, @Tags, @Concluida, @DataCriacao);";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ListaId", listaId.HasValue && listaId > 0 ? listaId.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@Titulo", titulo);
            comando.Parameters.AddWithValue("@Descricao", string.IsNullOrWhiteSpace(descricao) ? DBNull.Value : descricao);
            comando.Parameters.AddWithValue("@PrioridadeId", prioridadeId.HasValue ? prioridadeId.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@DataVencimento", dataVencimento.HasValue ? dataVencimento.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@Tags", string.IsNullOrWhiteSpace(tags) ? DBNull.Value : tags);
            comando.Parameters.AddWithValue("@Concluida", 0);
            comando.Parameters.AddWithValue("@DataCriacao", DateTime.Now);

            await comando.ExecuteNonQueryAsync();

            return comando.LastInsertedId > 0 ? Convert.ToInt32(comando.LastInsertedId) : 0;
        }

        public async Task<bool> AtualizarTarefaAsync(int tarefaId, int usuarioId, string titulo, string? descricao, int? prioridadeId, DateTime? dataVencimento, string? tags)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                UPDATE Tarefas t
                INNER JOIN Listas l ON t.Lista_id = l.ID
                SET t.Titulo = @Titulo,
                    t.Descricao = @Descricao,
                    t.Prioridade_id = @PrioridadeId,
                    t.DataVencimento = @DataVencimento,
                    t.Tags = @Tags
                WHERE t.ID = @TarefaId AND l.Usuario_id = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Titulo", titulo);
            comando.Parameters.AddWithValue("@Descricao", string.IsNullOrWhiteSpace(descricao) ? DBNull.Value : descricao);
            comando.Parameters.AddWithValue("@PrioridadeId", prioridadeId.HasValue ? prioridadeId.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@DataVencimento", dataVencimento.HasValue ? dataVencimento.Value : DBNull.Value);
            comando.Parameters.AddWithValue("@Tags", string.IsNullOrWhiteSpace(tags) ? DBNull.Value : tags);
            comando.Parameters.AddWithValue("@TarefaId", tarefaId);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            return await comando.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> AtualizarConcluidaAsync(int tarefaId, int usuarioId, bool concluida)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                UPDATE Tarefas t
                INNER JOIN Listas l ON t.Lista_id = l.ID
                SET t.Concluida = @Concluida
                WHERE t.ID = @TarefaId AND l.Usuario_id = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Concluida", concluida ? 1 : 0);
            comando.Parameters.AddWithValue("@TarefaId", tarefaId);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            return await comando.ExecuteNonQueryAsync() > 0;
        }


        public async Task<bool> ExcluirTarefaAsync(int tarefaId, int usuarioId)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                DELETE t
                FROM Tarefas t
                INNER JOIN Listas l ON t.Lista_id = l.ID
                WHERE t.ID = @TarefaId AND l.Usuario_id = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@TarefaId", tarefaId);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            return await comando.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int> DuplicarTarefaAsync(int tarefaId, int usuarioId)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                INSERT INTO Tarefas (Lista_id, Titulo, Descricao, Prioridade_id, DataVencimento, Tags, Concluida, DataCriacao)
                SELECT t.Lista_id,
                       CONCAT(t.Titulo, ' (Cópia)'),
                       t.Descricao,
                       t.Prioridade_id,
                       t.DataVencimento,
                       t.Tags,
                       0,
                       @DataCriacao
                FROM Tarefas t
                INNER JOIN Listas l ON t.Lista_id = l.ID
                WHERE t.ID = @TarefaId AND l.Usuario_id = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@TarefaId", tarefaId);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
            comando.Parameters.AddWithValue("@DataCriacao", DateTime.Now);

            await comando.ExecuteNonQueryAsync();

            return comando.LastInsertedId > 0 ? Convert.ToInt32(comando.LastInsertedId) : 0;
        }

    }
}
