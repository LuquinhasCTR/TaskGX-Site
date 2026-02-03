using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using TaskGX.Models;

namespace TaskGX.Data
{
    public class RepositorioDashboard
    {
        private readonly string _connectionString;

        public RepositorioDashboard(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public async Task<bool> ColunaExisteAsync(string tabela, string coluna)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            var sql = $"SHOW COLUMNS FROM {tabela} LIKE @Coluna;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@Coluna", coluna);

            using var leitor = await comando.ExecuteReaderAsync();
            return await leitor.ReadAsync();
        }

        public async Task<List<Lista>> ObterListasAsync(int usuarioId, bool favoritaExists)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            var orderBy = favoritaExists ? "Favorita DESC, DataCriacao DESC" : "DataCriacao DESC";
            var selectFavorita = favoritaExists ? "Favorita" : "NULL AS Favorita";

            var sql = $@"
                SELECT ID, Usuario_id, Nome, Cor, {selectFavorita}, DataCriacao
                FROM Listas
                WHERE Usuario_id = @UsuarioId
                ORDER BY {orderBy};";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            var listas = new List<Lista>();
            using var leitor = await comando.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                listas.Add(new Lista
                {
                    ID = leitor["ID"] != DBNull.Value ? Convert.ToInt32(leitor["ID"]) : 0,
                    UsuarioId = leitor["Usuario_id"] != DBNull.Value ? Convert.ToInt32(leitor["Usuario_id"]) : 0,
                    Nome = leitor["Nome"]?.ToString() ?? string.Empty,
                    Cor = leitor["Cor"] != DBNull.Value ? leitor["Cor"].ToString() : null,
                    Favorita = leitor["Favorita"] != DBNull.Value ? Convert.ToBoolean(leitor["Favorita"]) : null,
                    DataCriacao = leitor["DataCriacao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["DataCriacao"]) : null
                });
            }

            return listas;
        }

        public async Task<List<Prioridade>> ObterPrioridadesAsync()
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = "SELECT ID, Nome FROM Prioridades ORDER BY ID;";

            using var comando = new MySqlCommand(sql, conexao);
            var prioridades = new List<Prioridade>();

            using var leitor = await comando.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                prioridades.Add(new Prioridade
                {
                    ID = leitor["ID"] != DBNull.Value ? Convert.ToInt32(leitor["ID"]) : 0,
                    Nome = leitor["Nome"]?.ToString() ?? string.Empty
                });
            }

            return prioridades;
        }

        public async Task<List<Tarefa>> ObterTarefasUsuarioAsync(int usuarioId)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                SELECT t.*
                FROM Tarefas t
                INNER JOIN Listas l ON t.Lista_id = l.ID
                WHERE l.Usuario_id = @UsuarioId;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            var tarefas = new List<Tarefa>();
            using var leitor = await comando.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                tarefas.Add(MapearTarefa(leitor, false));
            }

            return tarefas;
        }

        public async Task<(int TotalListas, int TotalTarefas, int TarefasConcluidas)> ObterEstatisticasContaAsync(int usuarioId)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string listasSql = "SELECT COUNT(*) FROM Listas WHERE Usuario_id = @UsuarioId;";
            using var listasCmd = new MySqlCommand(listasSql, conexao);
            listasCmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
            var totalListas = Convert.ToInt32(await listasCmd.ExecuteScalarAsync());

            const string tarefasSql = @"
                SELECT
                    COUNT(*) AS Total,
                    SUM(CASE WHEN Concluida = 1 THEN 1 ELSE 0 END) AS Concluidas
                FROM Tarefas t
                INNER JOIN Listas l ON t.Lista_id = l.ID
                WHERE l.Usuario_id = @UsuarioId;";

            using var tarefasCmd = new MySqlCommand(tarefasSql, conexao);
            tarefasCmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
            using var leitor = await tarefasCmd.ExecuteReaderAsync();
            if (await leitor.ReadAsync())
            {
                var totalTarefas = leitor["Total"] != DBNull.Value ? Convert.ToInt32(leitor["Total"]) : 0;
                var concluidas = leitor["Concluidas"] != DBNull.Value ? Convert.ToInt32(leitor["Concluidas"]) : 0;
                return (totalListas, totalTarefas, concluidas);
            }

            return (totalListas, 0, 0);
        }

        public async Task<Lista?> ObterListaAsync(int listaId, int usuarioId, bool favoritaExists)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            var selectFavorita = favoritaExists ? "Favorita" : "NULL AS Favorita";

            var sql = $@"
                SELECT ID, Usuario_id, Nome, Cor, {selectFavorita}, DataCriacao
                FROM Listas
                WHERE ID = @ListaId AND Usuario_id = @UsuarioId
                LIMIT 1;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ListaId", listaId);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);

            using var leitor = await comando.ExecuteReaderAsync();
            if (!await leitor.ReadAsync())
            {
                return null;
            }

            return new Lista
            {
                ID = leitor["ID"] != DBNull.Value ? Convert.ToInt32(leitor["ID"]) : 0,
                UsuarioId = leitor["Usuario_id"] != DBNull.Value ? Convert.ToInt32(leitor["Usuario_id"]) : 0,
                Nome = leitor["Nome"]?.ToString() ?? string.Empty,
                Cor = leitor["Cor"] != DBNull.Value ? leitor["Cor"].ToString() : null,
                Favorita = leitor["Favorita"] != DBNull.Value ? Convert.ToBoolean(leitor["Favorita"]) : null,
                DataCriacao = leitor["DataCriacao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["DataCriacao"]) : null
            };
        }

        public async Task<List<Tarefa>> ObterTarefasPorListaAsync(int listaId)
        {
            using var conexao = new MySqlConnection(_connectionString);
            await conexao.OpenAsync();

            const string sql = @"
                SELECT t.*, p.Nome AS PrioridadeNome
                FROM Tarefas t
                LEFT JOIN Prioridades p ON t.Prioridade_id = p.ID
                WHERE t.Lista_id = @ListaId
                ORDER BY t.Concluida ASC, t.DataVencimento ASC, t.DataCriacao DESC;";

            using var comando = new MySqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@ListaId", listaId);

            var tarefas = new List<Tarefa>();
            using var leitor = await comando.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                tarefas.Add(MapearTarefa(leitor, true));
            }

            return tarefas;
        }

        private static Tarefa MapearTarefa(MySqlDataReader leitor, bool incluirPrioridade)
        {
            var temPrioridadeNome = incluirPrioridade && TemColuna(leitor, "PrioridadeNome");
            var temArquivada = TemColuna(leitor, "Arquivada");

            return new Tarefa
            {
                ID = leitor["ID"] != DBNull.Value ? Convert.ToInt32(leitor["ID"]) : 0,
                ListaId = leitor["Lista_id"] != DBNull.Value ? Convert.ToInt32(leitor["Lista_id"]) : (int?)null,
                Titulo = leitor["Titulo"]?.ToString() ?? string.Empty,
                Descricao = leitor["Descricao"] != DBNull.Value ? leitor["Descricao"].ToString() : null,
                PrioridadeId = leitor["Prioridade_id"] != DBNull.Value ? Convert.ToInt32(leitor["Prioridade_id"]) : (int?)null,
                PrioridadeNome = temPrioridadeNome && leitor["PrioridadeNome"] != DBNull.Value ? leitor["PrioridadeNome"].ToString() : null,
                Tags = leitor["Tags"] != DBNull.Value ? leitor["Tags"].ToString() : null,
                Concluida = leitor["Concluida"] != DBNull.Value && Convert.ToBoolean(leitor["Concluida"]),
                Arquivada = temArquivada && leitor["Arquivada"] != DBNull.Value ? Convert.ToBoolean(leitor["Arquivada"]) : (bool?)null,
                DataVencimento = leitor["DataVencimento"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(leitor["DataVencimento"]) : null,
                DataCriacao = leitor["DataCriacao"] != DBNull.Value ? Convert.ToDateTime(leitor["DataCriacao"]) : DateTime.MinValue
            };
        }

        private static bool TemColuna(MySqlDataReader leitor, string coluna)
        {
            for (var i = 0; i < leitor.FieldCount; i++)
            {
                if (string.Equals(leitor.GetName(i), coluna, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
