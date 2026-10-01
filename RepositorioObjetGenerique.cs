using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;

/// <summary>
/// Gestionnaire de persistance pour les objets génériques
/// Permet d'importer, créer, mettre à jour, et supprimer des objets en base de données
/// </summary>
public class RepositorioObjetGenerique
{
    private string _connectionString;
    private string _typeBaseDonnees; // "SqlServer", "MySQL", "MariaDB", "PostgreSQL"

    public RepositorioObjetGenerique(string connectionString, string typeBaseDonnees = "SqlServer")
    {
        _connectionString = connectionString;
        _typeBaseDonnees = typeBaseDonnees;
    }

    /// <summary>
    /// Créer une table en base de données pour stocker les objets génériques
    /// </summary>
    public void CreerTable(string nomTable, List<string> nomsProprietes)
    {
        if (TableExiste(nomTable))
        {
            Console.WriteLine($"⚠️ La table '{nomTable}' existe déjà.");
            return;
        }

        var colonnes = new List<string> { "Id INT PRIMARY KEY AUTO_INCREMENT" };
        
        foreach (var prop in nomsProprietes)
        {
            colonnes.Add($"`{prop}` LONGTEXT NULL");
        }

        colonnes.Add("`CreatedAt` DATETIME DEFAULT CURRENT_TIMESTAMP");
        colonnes.Add("`UpdatedAt` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

        string sql = $"CREATE TABLE `{nomTable}` (\n  " +
                     string.Join(",\n  ", colonnes) +
                     "\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        ExecuterSQL(sql);
        Console.WriteLine($"✅ Table '{nomTable}' créée avec succès.");
    }

    /// <summary>
    /// Vérifier si une table existe
    /// </summary>
    public bool TableExiste(string nomTable)
    {
        string sql = _typeBaseDonnees switch
        {
            "SqlServer" => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{nomTable}'",
            "MySQL" or "MariaDB" => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{nomTable}'",
            "PostgreSQL" => $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{nomTable.ToLower()}'",
            _ => throw new NotSupportedException($"Type de base de données non supporté: {_typeBaseDonnees}")
        };

        try
        {
            var resultat = ExecuterScalar(sql);
            return Convert.ToInt32(resultat) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Insérer un nouvel objet générique en base de données
    /// </summary>
    public int InsererObjet(string nomTable, ObjetGenerique objet)
    {
        var proprietes = objet.ObtenirToutesProprietes();
        var colonnes = proprietes.Keys.ToList();
        var valeurs = proprietes.Values.ToList();

        var nomsColonnes = string.Join(", ", colonnes.Select(c => $"`{c}`"));
        var placeholders = string.Join(", ", Enumerable.Range(0, colonnes.Count).Select(i => "@p" + i));

        string sql = $"INSERT INTO `{nomTable}` ({nomsColonnes}) VALUES ({placeholders})";

        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);

        for (int i = 0; i < colonnes.Count; i++)
        {
            command.Parameters.AddWithValue("@p" + i, valeurs[i] ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
        Console.WriteLine($"✅ Objet inséré dans '{nomTable}'");

        // Récupérer l'ID de l'objet inséré
        string sqlId = _typeBaseDonnees switch
        {
            "SqlServer" => "SELECT CAST(SCOPE_IDENTITY() AS INT)",
            "MySQL" or "MariaDB" => "SELECT LAST_INSERT_ID()",
            "PostgreSQL" => "SELECT lastval()",
            _ => throw new NotSupportedException(_typeBaseDonnees)
        };

        using var commandId = CreerCommande(sqlId, connection);
        return Convert.ToInt32(commandId.ExecuteScalar());
    }

    /// <summary>
    /// Mettre à jour un objet existant
    /// </summary>
    public void MettreAJourObjet(string nomTable, int id, ObjetGenerique objet)
    {
        var proprietes = objet.ObtenirToutesProprietes();
        var updates = new List<string>();
        var parametres = new List<(string, object)>();

        int index = 0;
        foreach (var prop in proprietes)
        {
            updates.Add($"`{prop.Key}` = @p{index}");
            parametres.Add(($"@p{index}", prop.Value ?? DBNull.Value));
            index++;
        }

        string sql = $"UPDATE `{nomTable}` SET {string.Join(", ", updates)}, UpdatedAt = NOW() WHERE Id = @id";

        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);

        foreach (var param in parametres)
        {
            command.Parameters.AddWithValue(param.Item1, param.Item2);
        }
        command.Parameters.AddWithValue("@id", id);

        command.ExecuteNonQuery();
        Console.WriteLine($"✅ Objet ID {id} mis à jour dans '{nomTable}'");
    }

    /// <summary>
    /// Récupérer un objet par son ID
    /// </summary>
    public ObjetGenerique ObtenirObjet(string nomTable, int id)
    {
        string sql = $"SELECT * FROM `{nomTable}` WHERE Id = @id";

        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var objet = new ObjetGenerique();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string nom = reader.GetName(i);
                object valeur = reader.GetValue(i);

                if (nom != "Id" && nom != "CreatedAt" && nom != "UpdatedAt")
                {
                    objet.AjouterPropriete(nom, valeur == DBNull.Value ? null : valeur);
                }
            }
            return objet;
        }

        return null;
    }

    /// <summary>
    /// Récupérer tous les objets d'une table
    /// </summary>
    public List<(int Id, ObjetGenerique Objet)> ObtenirTousLesObjets(string nomTable)
    {
        var resultat = new List<(int, ObjetGenerique)>();
        string sql = $"SELECT * FROM `{nomTable}` ORDER BY Id";

        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            int id = Convert.ToInt32(reader["Id"]);
            var objet = new ObjetGenerique();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string nom = reader.GetName(i);
                object valeur = reader.GetValue(i);

                if (nom != "Id" && nom != "CreatedAt" && nom != "UpdatedAt")
                {
                    objet.AjouterPropriete(nom, valeur == DBNull.Value ? null : valeur);
                }
            }

            resultat.Add((id, objet));
        }

        return resultat;
    }

    /// <summary>
    /// Supprimer un objet
    /// </summary>
    public void SupprimerObjet(string nomTable, int id)
    {
        string sql = $"DELETE FROM `{nomTable}` WHERE Id = @id";

        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        command.ExecuteNonQuery();
        Console.WriteLine($"✅ Objet ID {id} supprimé de '{nomTable}'");
    }

    /// <summary>
    /// Importer un tableau d'objets génériques en base de données
    /// </summary>
    public List<int> ImporterObjets(string nomTable, TableauObjets tableau)
    {
        var ids = new List<int>();

        Console.WriteLine($"\n📥 Importation de {tableau.Nombre} objets dans '{nomTable}'...");

        foreach (var objet in tableau.ObtenirTousLesObjets())
        {
            int id = InsererObjet(nomTable, objet);
            ids.Add(id);
        }

        Console.WriteLine($"✅ {ids.Count} objets importés avec succès!");
        return ids;
    }

    /// <summary>
    /// Actualiser les objets : importer les nouveaux, mettre à jour les existants
    /// </summary>
    public void ActualiserObjets(string nomTable, List<(int Id, ObjetGenerique Objet)> objets)
    {
        Console.WriteLine($"\n🔄 Actualisation des objets dans '{nomTable}'...");

        int inseres = 0, miseAJour = 0;

        foreach (var (id, objet) in objets)
        {
            if (id <= 0)
            {
                InsererObjet(nomTable, objet);
                inseres++;
            }
            else
            {
                MettreAJourObjet(nomTable, id, objet);
                miseAJour++;
            }
        }

        Console.WriteLine($"✅ {inseres} objets insérés, {miseAJour} objets mis à jour");
    }

    /// <summary>
    /// Exporter les données en JSON
    /// </summary>
    public string ExporterEnJSON(string nomTable)
    {
        var objets = ObtenirTousLesObjets(nomTable);
        var json = new StringBuilder("[");

        for (int i = 0; i < objets.Count; i++)
        {
            var (id, objet) = objets[i];
            json.Append("{\"Id\":" + id + ",");

            var proprietes = objet.ObtenirToutesProprietes();
            var props = proprietes.Select(p => $"\"{ p.Key}\":\"{EchapperJSON(p.Value?.ToString() ?? "")}\"");
            json.Append(string.Join(",", props));

            json.Append("}");
            if (i < objets.Count - 1) json.Append(",");
        }

        json.Append("]");
        return json.ToString();
    }

    /// <summary>
    /// Compter les objets dans une table
    /// </summary>
    public int CompterObjets(string nomTable)
    {
        string sql = $"SELECT COUNT(*) FROM `{nomTable}`";
        var resultat = ExecuterScalar(sql);
        return Convert.ToInt32(resultat);
    }

    /// <summary>
    /// Afficher les objets de manière formatée
    /// </summary>
    public void AfficherObjets(string nomTable)
    {
        var objets = ObtenirTousLesObjets(nomTable);

        Console.WriteLine($"\n{'='} Table: {nomTable} ({objets.Count} objets) {'='}\n");

        if (objets.Count == 0)
        {
            Console.WriteLine("Aucun objet trouvé.");
            return;
        }

        // En-tête
        Console.Write("ID".PadRight(10));
        var premiereProps = objets[0].Objet.ObtenirNomsProprietes();
        foreach (var prop in premiereProps)
        {
            Console.Write(prop.PadRight(30));
        }
        Console.WriteLine();
        Console.WriteLine(new string('─', 150));

        // Données
        foreach (var (id, objet) in objets)
        {
            Console.Write(id.ToString().PadRight(10));
            var proprietes = objet.ObtenirToutesProprietes();

            foreach (var prop in premiereProps)
            {
                var valeur = proprietes.ContainsKey(prop) ? proprietes[prop]?.ToString() ?? "null" : "null";
                Console.Write(valeur.PadRight(30));
            }
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Ajouter une colonne à une table existante
    /// </summary>
    public void AjouterColonne(string nomTable, string nomColonne)
    {
        if (ColonneExiste(nomTable, nomColonne))
        {
            Console.WriteLine($"⚠️ La colonne '{nomColonne}' existe déjà.");
            return;
        }

        string sql = $"ALTER TABLE `{nomTable}` ADD COLUMN `{nomColonne}` LONGTEXT NULL";
        ExecuterSQL(sql);
        Console.WriteLine($"✅ Colonne '{nomColonne}' ajoutée à '{nomTable}'");
    }

    /// <summary>
    /// Vérifier si une colonne existe
    /// </summary>
    public bool ColonneExiste(string nomTable, string nomColonne)
    {
        string sql = _typeBaseDonnees switch
        {
            "SqlServer" => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{nomTable}' AND COLUMN_NAME = '{nomColonne}'",
            "MySQL" or "MariaDB" => $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{nomTable}' AND COLUMN_NAME = '{nomColonne}'",
            "PostgreSQL" => $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{nomTable.ToLower()}' AND column_name = '{nomColonne.ToLower()}'",
            _ => throw new NotSupportedException(_typeBaseDonnees)
        };

        try
        {
            return Convert.ToInt32(ExecuterScalar(sql)) > 0;
        }
        catch
        {
            return false;
        }
    }

    // ========== Méthodes privées ==========

    private object CreerConnexion()
    {
        return _typeBaseDonnees switch
        {
            "SqlServer" => new SqlConnection(_connectionString),
            "MySQL" or "MariaDB" => new MySqlConnection(_connectionString),
            _ => throw new NotSupportedException(_typeBaseDonnees)
        };
    }

    private dynamic CreerCommande(string sql, object connection)
    {
        return _typeBaseDonnees switch
        {
            "SqlServer" => new SqlCommand(sql, (SqlConnection)connection),
            "MySQL" or "MariaDB" => new MySqlCommand(sql, (MySqlConnection)connection),
            _ => throw new NotSupportedException(_typeBaseDonnees)
        };
    }

    private void ExecuterSQL(string sql)
    {
        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);
        command.ExecuteNonQuery();
    }

    private object ExecuterScalar(string sql)
    {
        using var connection = CreerConnexion();
        connection.Open();
        using var command = CreerCommande(sql, connection);
        return command.ExecuteScalar();
    }

    private string EchapperJSON(string texte)
    {
        return texte
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

/// <summary>
/// Extension pour faciliter l'utilisation
/// </summary>
public static class ExtensionsObjet
{
    public static void SauvegarderEnBD(this ObjetGenerique objet, RepositorioObjetGenerique repo, string table)
    {
        repo.InsererObjet(table, objet);
    }

    public static void SauvegarderEnBD(this TableauObjets tableau, RepositorioObjetGenerique repo, string table)
    {
        repo.ImporterObjets(table, tableau);
    }
}
