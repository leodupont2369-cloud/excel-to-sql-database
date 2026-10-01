using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;

/// <summary>
/// Classe représentant un objet avec des propriétés génériques et dynamiques
/// </summary>
public class ObjetGenerique
{
    private Dictionary<string, object> _proprietes = new();

    /// <summary>
    /// Ajouter une propriété
    /// </summary>
    public void AjouterPropriete(string nom, object valeur = null)
    {
        _proprietes[nom] = valeur;
    }

    /// <summary>
    /// Ajouter plusieurs propriétés à la fois
    /// </summary>
    public void AjouterProprietes(params string[] noms)
    {
        foreach (var nom in noms)
        {
            _proprietes[nom] = null;
        }
    }

    /// <summary>
    /// Définir la valeur d'une propriété
    /// </summary>
    public void DefinirValeur(string nom, object valeur)
    {
        if (!_proprietes.ContainsKey(nom))
            throw new KeyNotFoundException($"La propriété '{nom}' n'existe pas.");
        
        _proprietes[nom] = valeur;
    }

    /// <summary>
    /// Récupérer la valeur d'une propriété
    /// </summary>
    public object ObtenirValeur(string nom)
    {
        return _proprietes.ContainsKey(nom) ? _proprietes[nom] : null;
    }

    /// <summary>
    /// Récupérer la valeur avec conversion de type
    /// </summary>
    public T ObtenirValeur<T>(string nom)
    {
        if (!_proprietes.ContainsKey(nom))
            return default;
        
        var valeur = _proprietes[nom];
        if (valeur == null)
            return default;
        
        return (T)Convert.ChangeType(valeur, typeof(T));
    }

    /// <summary>
    /// Accès par indexeur (syntaxe obj["Nom"])
    /// </summary>
    public object this[string nom]
    {
        get => _proprietes.ContainsKey(nom) ? _proprietes[nom] : null;
        set => _proprietes[nom] = value;
    }

    /// <summary>
    /// Récupérer tous les noms de propriétés
    /// </summary>
    public List<string> ObtenirNomsProprietes()
    {
        return _proprietes.Keys.ToList();
    }

    /// <summary>
    /// Récupérer toutes les propriétés
    /// </summary>
    public Dictionary<string, object> ObtenirToutesProprietes()
    {
        return new Dictionary<string, object>(_proprietes);
    }

    /// <summary>
    /// Vérifier si une propriété existe
    /// </summary>
    public bool ProprieteExiste(string nom)
    {
        return _proprietes.ContainsKey(nom);
    }

    /// <summary>
    /// Afficher toutes les propriétés et leurs valeurs
    /// </summary>
    public void Afficher()
    {
        Console.WriteLine("--- Propriétés de l'objet ---");
        foreach (var prop in _proprietes)
        {
            var valeur = prop.Value ?? "null";
            Console.WriteLine($"{prop.Key}: {valeur}");
        }
    }

    /// <summary>
    /// Convertir l'objet en string formaté
    /// </summary>
    public override string ToString()
    {
        var lignes = _proprietes.Select(p => $"{p.Key}: {p.Value ?? "null"}");
        return string.Join(", ", lignes);
    }
}

/// <summary>
/// Classe pour gérer un tableau d'objets génériques
/// </summary>
public class TableauObjets
{
    private List<ObjetGenerique> _objets = new();
    private List<string> _noms_proprietes = new();

    /// <summary>
    /// Initialiser avec les noms des propriétés
    /// </summary>
    public TableauObjets(params string[] nomsProprietes)
    {
        _noms_proprietes = nomsProprietes.ToList();
    }

    /// <summary>
    /// Ajouter un objet au tableau
    /// </summary>
    public void AjouterObjet(ObjetGenerique obj)
    {
        _objets.Add(obj);
    }

    /// <summary>
    /// Créer et ajouter un nouvel objet avec les valeurs
    /// </summary>
    public void AjouterObjet(params object[] valeurs)
    {
        if (valeurs.Length != _noms_proprietes.Count)
            throw new ArgumentException($"Attendu {_noms_proprietes.Count} valeurs, trouvé {valeurs.Length}");

        var obj = new ObjetGenerique();
        for (int i = 0; i < _noms_proprietes.Count; i++)
        {
            obj.AjouterPropriete(_noms_proprietes[i], valeurs[i]);
        }
        _objets.Add(obj);
    }

    /// <summary>
    /// Récupérer un objet à un index
    /// </summary>
    public ObjetGenerique ObtenirObjet(int index)
    {
        if (index < 0 || index >= _objets.Count)
            throw new IndexOutOfRangeException($"Index {index} hors limites");
        
        return _objets[index];
    }

    /// <summary>
    /// Récupérer tous les objets
    /// </summary>
    public List<ObjetGenerique> ObtenirTousLesObjets()
    {
        return new List<ObjetGenerique>(_objets);
    }

    /// <summary>
    /// Nombre d'objets dans le tableau
    /// </summary>
    public int Nombre => _objets.Count;

    /// <summary>
    /// Afficher tous les objets et leurs propriétés
    /// </summary>
    public void Afficher()
    {
        Console.WriteLine($"\n========== TABLEAU D'OBJETS ({_objets.Count} objets) ==========");
        Console.WriteLine($"Propriétés: {string.Join(", ", _noms_proprietes)}\n");

        for (int i = 0; i < _objets.Count; i++)
        {
            Console.WriteLine($"--- Objet {i + 1} ---");
            _objets[i].Afficher();
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Afficher dans un tableau formaté
    /// </summary>
    public void AfficherTableau()
    {
        if (_objets.Count == 0)
        {
            Console.WriteLine("Le tableau est vide.");
            return;
        }

        // Calculer la largeur des colonnes
        var largeurs = new Dictionary<string, int>();
        foreach (var prop in _noms_proprietes)
        {
            largeurs[prop] = Math.Max(prop.Length, 10);
            foreach (var obj in _objets)
            {
                var val = obj[prop]?.ToString() ?? "null";
                largeurs[prop] = Math.Max(largeurs[prop], val.Length);
            }
        }

        // En-tête
        Console.WriteLine("\n" + new string('=', largeurs.Values.Sum() + (_noms_proprietes.Count * 3) + 2));
        foreach (var prop in _noms_proprietes)
        {
            Console.Write(prop.PadRight(largeurs[prop]) + " | ");
        }
        Console.WriteLine();
        Console.WriteLine(new string('=', largeurs.Values.Sum() + (_noms_proprietes.Count * 3) + 2));

        // Lignes
        foreach (var obj in _objets)
        {
            foreach (var prop in _noms_proprietes)
            {
                var val = obj[prop]?.ToString() ?? "null";
                Console.Write(val.PadRight(largeurs[prop]) + " | ");
            }
            Console.WriteLine();
        }
        Console.WriteLine(new string('=', largeurs.Values.Sum() + (_noms_proprietes.Count * 3) + 2) + "\n");
    }

    /// <summary>
    /// Filtrer les objets selon une condition
    /// </summary>
    public List<ObjetGenerique> Filtrer(Func<ObjetGenerique, bool> condition)
    {
        return _objets.Where(condition).ToList();
    }

    /// <summary>
    /// Récupérer une colonne complète
    /// </summary>
    public List<object> ObtenirColonne(string nomPropriete)
    {
        if (!_noms_proprietes.Contains(nomPropriete))
            throw new KeyNotFoundException($"Propriété '{nomPropriete}' introuvable");

        return _objets.Select(obj => obj[nomPropriete]).ToList();
    }

    /// <summary>
    /// Convertir en List<Dictionary<string, object>>
    /// </summary>
    public List<Dictionary<string, object>> ConvertirEnDictionnaire()
    {
        return _objets.Select(obj => obj.ObtenirToutesProprietes()).ToList();
    }
}
