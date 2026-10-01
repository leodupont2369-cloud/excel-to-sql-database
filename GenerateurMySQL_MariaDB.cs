using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Limites des colonnes et taille des lignes pour différentes bases de données
/// </summary>
public class LimitesBD
{
    public static void AfficherLimites()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("LIMITES DES COLONNES PAR BASE DE DONNÉES");
        Console.WriteLine(new string('=', 80));

        Console.WriteLine("\n📊 SQL SERVER");
        Console.WriteLine("  • Colonnes par table: 1 024");
        Console.WriteLine("  • Largeur de ligne: 8 060 octets");
        Console.WriteLine("  • Colonnes par index: 16");
        Console.WriteLine("  • Clés primaires: 16 colonnes");

        Console.WriteLine("\n🐘 PostgreSQL");
        Console.WriteLine("  • Colonnes par table: 1 600");
        Console.WriteLine("  • Largeur de ligne: illimitée (stockage hébergé)");
        Console.WriteLine("  • Colonnes par index: 32");
        Console.WriteLine("  • Clés primaires: illimitées");

        Console.WriteLine("\n🐬 MySQL");
        Console.WriteLine("  • Colonnes par table: 4 096");
        Console.WriteLine("  • Largeur de ligne: 65 535 octets (limite physique)");
        Console.WriteLine("  • Colonnes par index: 16 (par défaut, peut être augmenté)");
        Console.WriteLine("  • Clés primaires: 1 seule par table");
        Console.WriteLine("  • Note: VARCHAR(255)*255 = 65 025 octets, laisse peu de marge");

        Console.WriteLine("\n🌊 MariaDB");
        Console.WriteLine("  • Colonnes par table: 4 096");
        Console.WriteLine("  • Largeur de ligne: 65 535 octets (limite physique)");
        Console.WriteLine("  • Colonnes par index: 16 (peut être augmenté à 32)");
        Console.WriteLine("  • Clés primaires: 1 seule par table");
        Console.WriteLine("  • JSON support: LONGTEXT, JSON");
        Console.WriteLine("  • Note: Compatible MySQL, limites identiques");

        Console.WriteLine("\n📦 SQLite");
        Console.WriteLine("  • Colonnes par table: 2 000 (configurable à la compilation)");
        Console.WriteLine("  • Largeur de ligne: illimitée (pas de limite fixe)");
        Console.WriteLine("  • Colonnes par index: illimitées");
        Console.WriteLine("  • Note: Pas de limite stricte, dépend de la RAM disponible");

        Console.WriteLine("\n" + new string('=', 80) + "\n");
    }
}

/// <summary>
/// Validateur pour les limites MySQL/MariaDB
/// </summary>
public class ValidateurMySQL
{
    private const int MAX_COLONNES_MYSQL = 4096;
    private const int MAX_LARGEUR_LIGNE = 65535; // octets
    private const int MARGE_SECURITE = 1000; // octets de marge

    /// <summary>
    /// Vérifier si la structure est valide pour MySQL/MariaDB
    /// </summary>
    public static bool ValiderStructure(string nomTable, List<ChampMySQL> champs)
    {
        Console.WriteLine($"\n--- Validation MySQL/MariaDB pour '{nomTable}' ---");

        // Vérifier le nombre de colonnes
        if (champs.Count > MAX_COLONNES_MYSQL)
        {
            Console.WriteLine($"❌ ERREUR: {champs.Count} colonnes dépasse la limite de {MAX_COLONNES_MYSQL}");
            return false;
        }
        Console.WriteLine($"✅ Nombre de colonnes OK: {champs.Count}/{MAX_COLONNES_MYSQL}");

        // Vérifier la largeur de la ligne
        int largeurTotale = CalculerLargeurLigne(champs);
        int largeurMax = MAX_LARGEUR_LIGNE - MARGE_SECURITE;

        Console.WriteLine($"\nLargeur de ligne: {largeurTotale} octets");
        Console.WriteLine($"Limite: {MAX_LARGEUR_LIGNE} octets (avec marge: {largeurMax} octets)");

        foreach (var champ in champs)
        {
            int octets = CalculerOctetsChamp(champ);
            Console.WriteLine($"  • {champ.Nom.PadRight(30)} {champ.Type.PadRight(20)} = {octets} octets");
        }

        if (largeurTotale > largeurMax)
        {
            Console.WriteLine($"\n❌ ERREUR: Largeur totale ({largeurTotale}) dépasse la limite ({largeurMax})");
            Console.WriteLine($"   Dépassement: {largeurTotale - largeurMax} octets");
            return false;
        }

        Console.WriteLine($"\n✅ Largeur de ligne OK");
        return true;
    }

    /// <summary>
    /// Calculer la largeur totale d'une ligne
    /// </summary>
    public static int CalculerLargeurLigne(List<ChampMySQL> champs)
    {
        return champs.Sum(c => CalculerOctetsChamp(c));
    }

    /// <summary>
    /// Calculer les octets d'un champ
    /// </summary>
    private static int CalculerOctetsChamp(ChampMySQL champ)
    {
        switch (champ.Type.ToLower())
        {
            case "tinyint":
                return 1;
            case "smallint":
            case "year":
                return 2;
            case "mediumint":
                return 3;
            case "int":
            case "integer":
            case "float":
                return 4;
            case "bigint":
            case "double":
            case "datetime":
            case "timestamp":
                return 8;
            case "date":
                return 3;
            case "time":
                return 3;
            case "decimal":
            case "numeric":
                return 17; // Pour DECIMAL(18,2)
            case "char":
                // CHAR est de longueur fixe
                return champ.Longueur * 3; // UTF-8: 3 octets max par caractère
            case "varchar":
                // VARCHAR utilise 1 ou 2 octets pour la longueur + données
                return (champ.Longueur * 3) + 2; // +2 pour la métadonnée de longueur
            case "text":
            case "tinytext":
                return 2 + 256; // Minimum pour TEXT
            case "mediumtext":
            case "longtext":
                // Ces types ne comptent pas vraiment dans la limite
                // mais ils comptent un peu pour la structure
                return 4;
            case "blob":
            case "tinyblob":
                return 2 + 256;
            case "mediumblob":
            case "longblob":
                return 4;
            case "json":
                return 4; // JSON est LONGTEXT, compte peu dans la limite
            case "enum":
                return 2; // ENUM peut avoir jusqu'à 65 535 valeurs
            case "set":
                return 8; // SET peut avoir jusqu'à 64 valeurs
            default:
                return 255; // Par défaut VARCHAR
        }
    }

    /// <summary>
    /// Afficher des recommandations pour optimiser
    /// </summary>
    public static void AfficherRecommandations(List<ChampMySQL> champs)
    {
        Console.WriteLine("\n💡 RECOMMANDATIONS:");
        
        var champsGrands = champs
            .Where(c => CalculerOctetsChamp(c) > 1000)
            .ToList();

        if (champsGrands.Count > 0)
        {
            Console.WriteLine("\n  ⚠️ Colonnes très larges détectées:");
            foreach (var champ in champsGrands)
            {
                int octets = CalculerOctetsChamp(champ);
                Console.WriteLine($"     • {champ.Nom}: {octets} octets - Considérez MEDIUMTEXT/LONGTEXT");
            }
        }

        var champsVarchar = champs
            .Where(c => c.Type.ToLower() == "varchar" && c.Longueur > 500)
            .ToList();

        if (champsVarchar.Count > 0)
        {
            Console.WriteLine("\n  ⚠️ Colonnes VARCHAR très longues:");
            foreach (var champ in champsVarchar)
            {
                Console.WriteLine($"     • {champ.Nom}({champ.Longueur}) - Considérez TEXT pour alléger");
            }
        }
    }
}

/// <summary>
/// Classe représentant un champ MySQL/MariaDB
/// </summary>
public class ChampMySQL
{
    public string Nom { get; set; }
    public string Type { get; set; } // int, varchar, text, json, etc.
    public int Longueur { get; set; } = 255; // Pour VARCHAR, CHAR, etc.
    public bool Nullable { get; set; } = true;
    public bool ClePrimaire { get; set; } = false;
    public bool AutoIncrement { get; set; } = false;
    public string ValeurParDefaut { get; set; } = null;
}

/// <summary>
/// Générateur SQL pour MySQL/MariaDB
/// </summary>
public class GenerateurMySQL
{
    /// <summary>
    /// Générer le script CREATE TABLE pour MySQL/MariaDB
    /// </summary>
    public static string GenererCreateTable(string nomTable, List<ChampMySQL> champs)
    {
        if (!ValidateurMySQL.ValiderStructure(nomTable, champs))
        {
            throw new Exception("Structure invalide pour MySQL/MariaDB");
        }

        var lignes = new List<string>();

        foreach (var champ in champs)
        {
            string type = ConvertirType(champ.Type, champ.Longueur);
            string nullable = champ.Nullable ? "NULL" : "NOT NULL";
            string autoIncr = champ.AutoIncrement ? "AUTO_INCREMENT" : "";
            string defaut = !string.IsNullOrEmpty(champ.ValeurParDefaut) ? $"DEFAULT {champ.ValeurParDefaut}" : "";
            string pk = champ.ClePrimaire ? "PRIMARY KEY" : "";

            string definition = $"  `{champ.Nom}` {type} {nullable}";
            if (!string.IsNullOrWhiteSpace(pk))
                definition += $" {pk}";
            if (!string.IsNullOrWhiteSpace(autoIncr))
                definition += $" {autoIncr}";
            if (!string.IsNullOrWhiteSpace(defaut))
                definition += $" {defaut}";

            lignes.Add(definition);
        }

        return $"CREATE TABLE `{nomTable}` (\n" +
               string.Join(",\n", lignes) +
               "\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";
    }

    /// <summary>
    /// Convertir les types de données en types MySQL
    /// </summary>
    private static string ConvertirType(string type, int longueur)
    {
        return type.ToLower() switch
        {
            "int" or "integer" => "INT",
            "bigint" => "BIGINT",
            "smallint" => "SMALLINT",
            "tinyint" => "TINYINT",
            "bit" or "bool" => "BOOLEAN",
            "varchar" or "string" => longueur > 0 ? $"VARCHAR({longueur})" : "VARCHAR(255)",
            "char" => longueur > 0 ? $"CHAR({longueur})" : "CHAR(1)",
            "date" => "DATE",
            "datetime" or "datetime2" => "DATETIME",
            "timestamp" => "TIMESTAMP",
            "time" => "TIME",
            "decimal" or "numeric" => "DECIMAL(18,2)",
            "double" or "float" => "DOUBLE",
            "money" => "DECIMAL(10,2)",
            "text" => "LONGTEXT",
            "json" => "JSON",
            "blob" => "LONGBLOB",
            "uuid" => "CHAR(36)",
            _ => "VARCHAR(255)"
        };
    }
}

/// <summary>
/// Classe représentant une table MySQL/MariaDB
/// </summary>
public class TableMySQL
{
    public string Nom { get; set; }
    public List<ChampMySQL> Champs { get; set; } = new();
}
