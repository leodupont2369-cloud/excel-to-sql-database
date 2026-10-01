using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

/// <summary>
/// Classe pour gérer une base de données SQL Server
/// Permet de créer, modifier, ajouter des tables et colonnes
/// </summary>
public class GestionnaireBD
{
    private string _connectionString;
    private string _nomBD;

    public GestionnaireBD(string server, string baseDeDonnees, bool authentificationWindows = true, 
        string utilisateur = "", string motDePasse = "")
    {
        _nomBD = baseDeDonnees;

        if (authentificationWindows)
        {
            _connectionString = $"Server={server};Database={baseDeDonnees};Trusted_Connection=True;TrustServerCertificate=True;";
        }
        else
        {
            _connectionString = $"Server={server};Database={baseDeDonnees};User Id={utilisateur};Password={motDePasse};TrustServerCertificate=True;";
        }
    }

    /// <summary>
    /// Vérifier la connexion à la base de données
    /// </summary>
    public bool VerifierConnexion()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur de connexion: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Vérifier si une table existe
    /// </summary>
    public bool TableExiste(string nomTable)
    {
        string query = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{nomTable}'";
        
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(query, connection);
        
        int count = (int)command.ExecuteScalar();
        return count > 0;
    }

    /// <summary>
    /// Vérifier si une colonne existe dans une table
    /// </summary>
    public bool ColonneExiste(string nomTable, string nomColonne)
    {
        string query = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{nomTable}' AND COLUMN_NAME = '{nomColonne}'";
        
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(query, connection);
        
        int count = (int)command.ExecuteScalar();
        return count > 0;
    }

    /// <summary>
    /// Obtenir la liste de toutes les tables
    /// </summary>
    public List<string> ObtenirTables()
    {
        string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
        var tables = new List<string>();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            tables.Add(reader["TABLE_NAME"].ToString());
        }

        return tables;
    }

    /// <summary>
    /// Obtenir les colonnes d'une table avec leurs types
    /// </summary>
    public List<(string NomColonne, string Type, bool NullAutorise)> ObtenirColonnes(string nomTable)
    {
        string query = $@"
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{nomTable}'
            ORDER BY ORDINAL_POSITION";

        var colonnes = new List<(string, string, bool)>();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            string nomColonne = reader["COLUMN_NAME"].ToString();
            string type = reader["DATA_TYPE"].ToString();
            bool nullable = reader["IS_NULLABLE"].ToString() == "YES";

            colonnes.Add((nomColonne, type, nullable));
        }

        return colonnes;
    }

    /// <summary>
    /// Créer une nouvelle table
    /// </summary>
    public void CreerTable(TableDefinition table)
    {
        if (TableExiste(table.NomTable))
        {
            Console.WriteLine($"⚠️ La table '{table.NomTable}' existe déjà !");
            return;
        }

        string script = GenerateCreateTableScript(table);
        ExecuterScript(script);
        Console.WriteLine($"✅ Table '{table.NomTable}' créée avec succès !");
    }

    /// <summary>
    /// Ajouter une colonne à une table existante
    /// </summary>
    public void AjouterColonne(string nomTable, string nomColonne, string type, int longueur = 0, bool nullAutorise = true)
    {
        if (!TableExiste(nomTable))
        {
            Console.WriteLine($"❌ La table '{nomTable}' n'existe pas !");
            return;
        }

        if (ColonneExiste(nomTable, nomColonne))
        {
            Console.WriteLine($"⚠️ La colonne '{nomColonne}' existe déjà dans '{nomTable}' !");
            return;
        }

        string typeSQL = ConvertirTypeSql(type, longueur);
        string nullable = nullAutorise ? "NULL" : "NOT NULL";

        string script = $"ALTER TABLE [{nomTable}] ADD [{nomColonne}] {typeSQL} {nullable}";
        ExecuterScript(script);
        Console.WriteLine($"✅ Colonne '{nomColonne}' ajoutée à '{nomTable}' !");
    }

    /// <summary>
    /// Modifier une colonne existante
    /// </summary>
    public void ModifierColonne(string nomTable, string nomColonne, string nouveauType, int longueur = 0, bool nullAutorise = true)
    {
        if (!TableExiste(nomTable))
        {
            Console.WriteLine($"❌ La table '{nomTable}' n'existe pas !");
            return;
        }

        if (!ColonneExiste(nomTable, nomColonne))
        {
            Console.WriteLine($"❌ La colonne '{nomColonne}' n'existe pas dans '{nomTable}' !");
            return;
        }

        string typeSQL = ConvertirTypeSql(nouveauType, longueur);
        string nullable = nullAutorise ? "NULL" : "NOT NULL";

        string script = $"ALTER TABLE [{nomTable}] ALTER COLUMN [{nomColonne}] {typeSQL} {nullable}";
        ExecuterScript(script);
        Console.WriteLine($"✅ Colonne '{nomColonne}' modifiée dans '{nomTable}' !");
    }

    /// <summary>
    /// Supprimer une colonne
    /// </summary>
    public void SupprimerColonne(string nomTable, string nomColonne)
    {
        if (!ColonneExiste(nomTable, nomColonne))
        {
            Console.WriteLine($"❌ La colonne '{nomColonne}' n'existe pas !");
            return;
        }

        string script = $"ALTER TABLE [{nomTable}] DROP COLUMN [{nomColonne}]";
        ExecuterScript(script);
        Console.WriteLine($"✅ Colonne '{nomColonne}' supprimée de '{nomTable}' !");
    }

    /// <summary>
    /// Renommer une colonne
    /// </summary>
    public void RenommerColonne(string nomTable, string ancienNom, string nouveauNom)
    {
        if (!ColonneExiste(nomTable, ancienNom))
        {
            Console.WriteLine($"❌ La colonne '{ancienNom}' n'existe pas !");
            return;
        }

        string script = $"EXEC sp_rename '@objname = [{nomTable}].[{ancienNom}], @newname = [{nouveauNom}], @objtype = COLUMN'";
        ExecuterScript(script);
        Console.WriteLine($"✅ Colonne renommée: '{ancienNom}' → '{nouveauNom}' !");
    }

    /// <summary>
    /// Renommer une table
    /// </summary>
    public void RenommerTable(string ancienNom, string nouveauNom)
    {
        if (!TableExiste(ancienNom))
        {
            Console.WriteLine($"❌ La table '{ancienNom}' n'existe pas !");
            return;
        }

        string script = $"EXEC sp_rename '{ancienNom}', '{nouveauNom}'";
        ExecuterScript(script);
        Console.WriteLine($"✅ Table renommée: '{ancienNom}' → '{nouveauNom}' !");
    }

    /// <summary>
    /// Supprimer une table
    /// </summary>
    public void SupprimerTable(string nomTable)
    {
        if (!TableExiste(nomTable))
        {
            Console.WriteLine($"❌ La table '{nomTable}' n'existe pas !");
            return;
        }

        string script = $"DROP TABLE [{nomTable}]";
        ExecuterScript(script);
        Console.WriteLine($"✅ Table '{nomTable}' supprimée !");
    }

    /// <summary>
    /// Ajouter une clé primaire
    /// </summary>
    public void AjouterClePrimaire(string nomTable, params string[] colonnes)
    {
        string colonnesStr = string.Join("], [", colonnes);
        string script = $"ALTER TABLE [{nomTable}] ADD PRIMARY KEY ([{colonnesStr}])";
        ExecuterScript(script);
        Console.WriteLine($"✅ Clé primaire ajoutée à '{nomTable}' !");
    }

    /// <summary>
    /// Ajouter un index
    /// </summary>
    public void AjouterIndex(string nomTable, string nomIndex, params string[] colonnes)
    {
        string colonnesStr = string.Join("], [", colonnes);
        string script = $"CREATE INDEX [{nomIndex}] ON [{nomTable}] ([{colonnesStr}])";
        ExecuterScript(script);
        Console.WriteLine($"✅ Index '{nomIndex}' créé sur '{nomTable}' !");
    }

    /// <summary>
    /// Exécuter un script SQL brut
    /// </summary>
    public void ExecuterScript(string script)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = new SqlCommand(script, connection);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur lors de l'exécution: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Générer le script CREATE TABLE
    /// </summary>
    private string GenerateCreateTableScript(TableDefinition table)
    {
        var lignes = new List<string>();

        foreach (var champ in table.Champs)
        {
            var type = ConvertirTypeSql(champ.TypeSql, champ.Longueur);
            var nullable = champ.NullAutorise ? "NULL" : "NOT NULL";

            lignes.Add($"    [{champ.NomChamp}] {type} {nullable}");
        }

        return $"CREATE TABLE [{table.NomTable}] (\n{string.Join(",\n", lignes)}\n);";
    }

    /// <summary>
    /// Convertir le type Excel en type SQL
    /// </summary>
    private string ConvertirTypeSql(string typeExcel, int longueur = 0)
    {
        var typeLower = typeExcel?.Trim().ToLower() ?? "";
        var result = typeLower switch
        {
            "int" or "integer" => "INT",
            "bigint" => "BIGINT",
            "smallint" => "SMALLINT",
            "bit" or "bool" => "BIT",
            "varchar" or "nvarchar" or "string" => longueur > 0 ? $"NVARCHAR({longueur})" : "NVARCHAR(255)",
            "char" or "nchar" => longueur > 0 ? $"NCHAR({longueur})" : "NCHAR(1)",
            "date" => "DATE",
            "datetime" or "datetime2" => "DATETIME2",
            "decimal" => "DECIMAL(18, 2)",
            "float" or "double" => "FLOAT",
            "money" => "MONEY",
            "text" => "NVARCHAR(MAX)",
            _ => "NVARCHAR(255)"
        };
        return result;
    }

    /// <summary>
    /// Afficher toutes les tables et colonnes
    /// </summary>
    public void AfficherStructure()
    {
        Console.WriteLine($"\n{'='} Structure de la base: {_nomBD} {'='}\n");

        var tables = ObtenirTables();

        if (tables.Count == 0)
        {
            Console.WriteLine("❌ Aucune table trouvée !");
            return;
        }

        foreach (var nomTable in tables)
        {
            Console.WriteLine($"\n📋 TABLE: [{nomTable}]");
            Console.WriteLine("─────────────────────────────────────");

            var colonnes = ObtenirColonnes(nomTable);
            foreach (var (colonne, type, nullable) in colonnes)
            {
                var nullStr = nullable ? "NULL" : "NOT NULL";
                Console.WriteLine($"  • {colonne.PadRight(25)} | {type.PadRight(20)} | {nullStr}");
            }
        }

        Console.WriteLine($"\n{'='}\n");
    }
}

/// <summary>
/// Classe représentant une table avec ses colonnes
/// </summary>
public class TableDefinition
{
    public string NomTable { get; set; }
    public List<ChampDefinition> Champs { get; set; } = new();
}

/// <summary>
/// Classe représentant une colonne/champ
/// </summary>
public class ChampDefinition
{
    public string NomChamp { get; set; }
    public string TypeSql { get; set; }
    public int Longueur { get; set; }
    public bool NullAutorise { get; set; }
    public bool ClePrimaire { get; set; }
}
