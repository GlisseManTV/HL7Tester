# HL7 Inspector — Plan d'implémentation MAUI
> Contexte : Ajout d'un HL7 Inspector dans une app .NET MAUI, 100% local, multi-version (HL7 v2.1 → v2.8), avec base de connaissances extraite de NHapi via réflexion.

---

## 🎯 Objectif

Construire un composant MAUI permettant de :
1. **Coller** un message HL7 brut dans un `Editor`
2. **Parser** le message et afficher une arborescence `Segment > Field > Component > SubComponent`
3. **Résoudre** chaque nœud avec son nom, description, dataType et valeurs de table — **100% offline**
4. **Supporter** plusieurs versions HL7 (2.1, 2.2, 2.3, 2.3.1, 2.4, 2.5, 2.5.1, 2.6, 2.7, 2.7.1, 2.8) en détectant la version dans MSH-12

---

## 📦 Packages NuGet à ajouter

```xml
<!-- Parser HL7 -->
<PackageReference Include="NHapi.Base"         Version="3.1.0" />

<!-- Modèles par version — inclure selon les versions supportées -->
<PackageReference Include="NHapi.Model.V21"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V22"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V23"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V231"   Version="3.1.0" />
<PackageReference Include="NHapi.Model.V24"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V25"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V251"   Version="3.1.0" />
<PackageReference Include="NHapi.Model.V26"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V27"    Version="3.1.0" />
<PackageReference Include="NHapi.Model.V271"   Version="3.1.0" />
<PackageReference Include="NHapi.Model.V28"    Version="3.1.0" />
```

Chaque package `NHapi.Model.VXX` contient en C# typé : tous les segments, tous les fields, tous les dataTypes et toutes les tables HL7 pour cette version.

---

## 🗂️ Architecture des fichiers à créer

```
/Services/
    HL7ParserService.cs          ← Parse le message brut → arbre d'objets
    HL7KnowledgeBaseService.cs   ← Extrait la KB depuis NHapi par réflexion
    HL7VersionDetector.cs        ← Lit MSH-12 pour détecter la version

/Models/
    ParsedHL7Message.cs          ← Arbre complet du message parsé
    HL7Segment.cs
    HL7Field.cs
    HL7Component.cs
    HL7SubComponent.cs
    SegmentInfo.cs               ← Info KB : nom, description, fields
    FieldInfo.cs                 ← Info KB : nom, dataType, required, table
    ComponentInfo.cs             ← Info KB : nom, description
    DataTypeInfo.cs              ← Info KB : nom, composants

/Pages/
    HL7InspectorPage.xaml        ← UI : Editor + arbre + panneau détail
    HL7InspectorPage.xaml.cs

/ViewModels/
    HL7InspectorViewModel.cs     ← MVVM binding
```

---

## 🔧 Étape 1 — Détection de version (HL7VersionDetector.cs)

La version est dans **MSH-12** (12ème champ du segment MSH).

```csharp
public static class HL7VersionDetector
{
    // Mapping version string → nom d'assembly NHapi
    private static readonly Dictionary<string, string> VersionMap = new()
    {
        { "2.1",   "NHapi.Model.V21"  },
        { "2.2",   "NHapi.Model.V22"  },
        { "2.3",   "NHapi.Model.V23"  },
        { "2.3.1", "NHapi.Model.V231" },
        { "2.4",   "NHapi.Model.V24"  },
        { "2.5",   "NHapi.Model.V25"  },
        { "2.5.1", "NHapi.Model.V251" },
        { "2.6",   "NHapi.Model.V26"  },
        { "2.7",   "NHapi.Model.V27"  },
        { "2.7.1", "NHapi.Model.V271" },
        { "2.8",   "NHapi.Model.V28"  },
    };

    public static string DetectVersion(string rawMessage)
    {
        // MSH est toujours la 1ère ligne ; MSH-12 est le 12ème champ (index 11)
        var mshLine = rawMessage
            .Replace("\r\n", "\r").Replace("\n", "\r")
            .Split('\r')[0];

        var fields = mshLine.Split('|');
        var version = fields.Length > 11 ? fields[11].Trim() : "2.5";

        return VersionMap.ContainsKey(version) ? version : "2.5"; // fallback
    }

    public static string GetAssemblyName(string version)
        => VersionMap.TryGetValue(version, out var asm) ? asm : "NHapi.Model.V25";
}
```

---

## 🔧 Étape 2 — Extraction de la KB depuis NHapi par réflexion (HL7KnowledgeBaseService.cs)

NHapi encode toutes les métadonnées dans des attributs C# sur chaque classe de segment. On les extrait **une seule fois au démarrage** et on les met en cache.

```csharp
using System.Reflection;
using NHapi.Base.Model;
using NHapi.Base.Parser;

public class HL7KnowledgeBaseService
{
    // Cache par version HL7
    private readonly Dictionary<string, VersionKB> _cache = new();

    public VersionKB GetKB(string version)
    {
        if (_cache.TryGetValue(version, out var cached)) return cached;

        var assemblyName = HL7VersionDetector.GetAssemblyName(version);
        var assembly = Assembly.Load(assemblyName);
        var kb = BuildKB(assembly, version);

        _cache[version] = kb;
        return kb;
    }

    private VersionKB BuildKB(Assembly assembly, string version)
    {
        var kb = new VersionKB { Version = version };

        // Tous les types de segments dans cet assembly
        var segmentTypes = assembly.GetTypes()
            .Where(t => typeof(ISegment).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract);

        foreach (var segType in segmentTypes)
        {
            try
            {
                var segInfo = ExtractSegmentInfo(segType);
                kb.Segments[segType.Name.ToUpper()] = segInfo;
            }
            catch { /* ignorer les segments non instanciables */ }
        }

        return kb;
    }

    private SegmentInfo ExtractSegmentInfo(Type segType)
    {
        // Instancier avec les paramètres minimaux NHapi
        var instance = (ISegment)Activator.CreateInstance(
            segType,
            new DefaultModelClassFactory(),
            null /* message parent */
        );

        var info = new SegmentInfo
        {
            Id = segType.Name.ToUpper(),
            // NHapi expose la description via attribut ou nom de type
            Description = GetSegmentDescription(segType),
            Fields = new Dictionary<int, FieldInfo>()
        };

        for (int i = 1; i <= instance.NumFields(); i++)
        {
            try
            {
                var fieldType = instance.GetField(i, 0).GetType();
                info.Fields[i] = new FieldInfo
                {
                    Index      = i,
                    Name       = GetFieldName(segType, i),
                    DataType   = fieldType.Name,
                    Required   = instance.IsRequired(i),
                    MaxReps    = instance.GetMaxCardinality(i),
                    Components = ExtractComponents(fieldType)
                };
            }
            catch { }
        }

        return info;
    }

    private Dictionary<int, ComponentInfo> ExtractComponents(Type fieldType)
    {
        var components = new Dictionary<int, ComponentInfo>();
        var props = fieldType.GetProperties()
            .Where(p => p.Name.StartsWith("Component"))
            .OrderBy(p => p.Name);

        int idx = 1;
        foreach (var prop in props)
        {
            components[idx++] = new ComponentInfo
            {
                Index = idx - 1,
                Name  = FormatPropertyName(prop.Name),
                DataType = prop.PropertyType.Name
            };
        }
        return components;
    }

    // Convertit "Component1_FamilyName" → "Family Name"
    private string FormatPropertyName(string propName)
    {
        var withoutPrefix = System.Text.RegularExpressions.Regex
            .Replace(propName, @"^Component\d+_?", "");
        return System.Text.RegularExpressions.Regex
            .Replace(withoutPrefix, "([A-Z])", " $1").Trim();
    }

    private string GetSegmentDescription(Type segType)
    {
        // NHapi met parfois la description dans un attribut Description
        var attr = segType.GetCustomAttributes(false)
            .FirstOrDefault(a => a.GetType().Name.Contains("Description"));
        return attr?.ToString() ?? segType.Name;
    }

    private string GetFieldName(Type segType, int fieldIndex)
    {
        // Les propriétés NHapi sont nommées : GetFIELDNAME() ou FIELDNAME
        // On extrait via les méthodes publiques nommées avec le numéro de field
        var method = segType.GetMethods()
            .FirstOrDefault(m => m.Name.Contains(fieldIndex.ToString("D2")));
        return method != null
            ? FormatPropertyName(method.Name)
            : $"Field {fieldIndex}";
    }
}

public class VersionKB
{
    public string Version { get; set; }
    public Dictionary<string, SegmentInfo> Segments { get; set; } = new();
}
```

---

## 🔧 Étape 3 — Parser HL7 (HL7ParserService.cs)

```csharp
using NHapi.Base.Parser;

public class HL7ParserService
{
    private readonly HL7KnowledgeBaseService _kb;

    public HL7ParserService(HL7KnowledgeBaseService kb) => _kb = kb;

    public ParsedHL7Message Parse(string rawMessage)
    {
        // Normaliser séparateurs de ligne
        rawMessage = rawMessage
            .Replace("\r\n", "\r")
            .Replace("\n", "\r")
            .Trim();

        var version = HL7VersionDetector.DetectVersion(rawMessage);
        var kb      = _kb.GetKB(version);

        var result = new ParsedHL7Message
        {
            RawMessage = rawMessage,
            Version    = version,
            Segments   = new List<HL7SegmentNode>()
        };

        var lines = rawMessage.Split('\r', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var segId = line.Length >= 3 ? line[..3] : line;
            kb.Segments.TryGetValue(segId.ToUpper(), out var segInfo);

            // Le séparateur de field est '|' sauf pour MSH où MSH-1 = '|' lui-même
            var fields = line.Split('|');

            var segNode = new HL7SegmentNode
            {
                Id          = segId,
                RawValue    = line,
                Name        = segInfo?.Description ?? segId,
                Fields      = new List<HL7FieldNode>()
            };

            // Pour MSH : field 1 = '|', les autres décalent d'un index
            int startIndex = segId == "MSH" ? 0 : 1;

            for (int fi = startIndex; fi < fields.Length; fi++)
            {
                int fieldIndex = segId == "MSH" ? fi : fi;
                var rawField   = fields[fi];
                if (string.IsNullOrEmpty(rawField)) continue;

                segInfo?.Fields.TryGetValue(fieldIndex, out var fieldInfo);

                var fieldNode = new HL7FieldNode
                {
                    Index      = fieldIndex,
                    RawValue   = rawField,
                    Name       = fieldInfo?.Name ?? $"Field {fieldIndex}",
                    DataType   = fieldInfo?.DataType,
                    Required   = fieldInfo?.Required ?? false,
                    Components = ParseComponents(rawField, fieldInfo)
                };

                segNode.Fields.Add(fieldNode);
            }

            result.Segments.Add(segNode);
        }

        return result;
    }

    private List<HL7ComponentNode> ParseComponents(string rawField, FieldInfo fieldInfo)
    {
        var components = new List<HL7ComponentNode>();
        var parts = rawField.Split('^');

        for (int ci = 0; ci < parts.Length; ci++)
        {
            if (string.IsNullOrEmpty(parts[ci])) continue;

            fieldInfo?.Components.TryGetValue(ci + 1, out var compInfo);

            components.Add(new HL7ComponentNode
            {
                Index         = ci + 1,
                Value         = parts[ci],
                Name          = compInfo?.Name ?? $"Component {ci + 1}",
                SubComponents = parts[ci].Split('&')
                    .Select((s, si) => new HL7SubComponentNode
                    {
                        Index = si + 1,
                        Value = s
                    }).ToList()
            });
        }

        return components;
    }
}
```

---

## 🔧 Étape 4 — Modèles (Models/)

```csharp
// ParsedHL7Message.cs
public class ParsedHL7Message
{
    public string RawMessage { get; set; }
    public string Version    { get; set; }
    public List<HL7SegmentNode> Segments { get; set; }
}

// HL7SegmentNode.cs
public class HL7SegmentNode
{
    public string Id       { get; set; }   // "PV1"
    public string Name     { get; set; }   // "Patient Visit"
    public string RawValue { get; set; }
    public List<HL7FieldNode> Fields { get; set; }
}

// HL7FieldNode.cs
public class HL7FieldNode
{
    public int    Index    { get; set; }   // 3
    public string Name     { get; set; }   // "Assigned Patient Location"
    public string RawValue { get; set; }
    public string DataType { get; set; }   // "PL"
    public bool   Required { get; set; }
    public List<HL7ComponentNode> Components { get; set; }

    // Notation courte ex: "PV1-3"
    public string Notation(string segId) => $"{segId}-{Index}";
}

// HL7ComponentNode.cs
public class HL7ComponentNode
{
    public int    Index { get; set; }      // 8
    public string Name  { get; set; }      // "Floor"
    public string Value { get; set; }
    public List<HL7SubComponentNode> SubComponents { get; set; }
}

// HL7SubComponentNode.cs
public class HL7SubComponentNode
{
    public int    Index { get; set; }
    public string Value { get; set; }
}

// KB Models
public class SegmentInfo
{
    public string Id          { get; set; }
    public string Description { get; set; }
    public Dictionary<int, FieldInfo> Fields { get; set; } = new();
}

public class FieldInfo
{
    public int    Index    { get; set; }
    public string Name     { get; set; }
    public string DataType { get; set; }
    public bool   Required { get; set; }
    public int    MaxReps  { get; set; }
    public Dictionary<int, ComponentInfo> Components { get; set; } = new();
}

public class ComponentInfo
{
    public int    Index    { get; set; }
    public string Name     { get; set; }
    public string DataType { get; set; }
}
```

---

## 🔧 Étape 5 — Résolution d'une notation pointée (ex: PV1.3.8)

```csharp
public class HL7Resolver
{
    private readonly HL7KnowledgeBaseService _kb;

    public ResolvedNode Resolve(string notation, string version)
    {
        // Accepte : "PV1.3.8" ou "PV1-3.8" ou "PV1-3-8"
        var clean = notation.Replace("-", ".").Replace("_", ".");
        var parts = clean.Split('.');

        if (parts.Length == 0) return null;

        var kb         = _kb.GetKB(version);
        var segId      = parts[0].ToUpper();
        var fieldIndex = parts.Length > 1 ? int.Parse(parts[1]) : (int?)null;
        var compIndex  = parts.Length > 2 ? int.Parse(parts[2]) : (int?)null;
        var subIndex   = parts.Length > 3 ? int.Parse(parts[3]) : (int?)null;

        kb.Segments.TryGetValue(segId, out var seg);
        FieldInfo field = null;
        if (fieldIndex.HasValue) seg?.Fields.TryGetValue(fieldIndex.Value, out field);
        ComponentInfo comp = null;
        if (compIndex.HasValue) field?.Components.TryGetValue(compIndex.Value, out comp);

        return new ResolvedNode
        {
            Notation     = notation,
            SegmentId    = segId,
            SegmentName  = seg?.Description,
            FieldIndex   = fieldIndex,
            FieldName    = field?.Name,
            FieldDataType= field?.DataType,
            ComponentIndex = compIndex,
            ComponentName  = comp?.Name,
            SubComponentIndex = subIndex,
            // Description synthétique
            Summary = BuildSummary(segId, seg, fieldIndex, field, compIndex, comp, subIndex)
        };
    }

    private string BuildSummary(string segId, SegmentInfo seg,
        int? fi, FieldInfo field, int? ci, ComponentInfo comp, int? si)
    {
        if (comp != null)
            return $"{segId}-{fi}.{ci} → {field?.Name} > Component {ci}: {comp.Name} ({comp.DataType})";
        if (field != null)
            return $"{segId}-{fi} → {field.Name} (dataType: {field.DataType}, required: {field.Required})";
        return $"{segId} → {seg?.Description ?? "Segment inconnu"}";
    }
}

public class ResolvedNode
{
    public string Notation          { get; set; }
    public string SegmentId         { get; set; }
    public string SegmentName       { get; set; }
    public int?   FieldIndex        { get; set; }
    public string FieldName         { get; set; }
    public string FieldDataType     { get; set; }
    public int?   ComponentIndex    { get; set; }
    public string ComponentName     { get; set; }
    public int?   SubComponentIndex { get; set; }
    public string Summary           { get; set; }
}
```

**Exemple :**
```
Resolve("PV1.3.8", "2.5")
→ SegmentName  : "Patient Visit"
→ FieldName    : "Assigned Patient Location"
→ FieldDataType: "PL"
→ ComponentName: "Floor"
→ Summary      : "PV1-3.8 → Assigned Patient Location > Component 8: Floor (IS)"
```

---

## 🎨 Étape 6 — UI MAUI (HL7InspectorPage.xaml)

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             x:Class="YourApp.Pages.HL7InspectorPage">
  <Grid RowDefinitions="200,Auto,*" ColumnDefinitions="*,*" Padding="12">

    <!-- Zone de saisie -->
    <Editor Grid.Row="0" Grid.ColumnSpan="2"
            x:Name="RawInput"
            Placeholder="Coller le message HL7 ici (MSH|^~\&amp;|...)"
            FontFamily="Courier New"
            FontSize="12"
            AutoSize="TextChanges"/>

    <!-- Bouton parse + sélecteur version -->
    <HorizontalStackLayout Grid.Row="1" Grid.ColumnSpan="2" Spacing="8" Padding="0,8">
      <Picker x:Name="VersionPicker" Title="Version" WidthRequest="120">
        <Picker.Items>
          <x:String>2.1</x:String><x:String>2.2</x:String>
          <x:String>2.3</x:String><x:String>2.3.1</x:String>
          <x:String>2.4</x:String><x:String>2.5</x:String>
          <x:String>2.5.1</x:String><x:String>2.6</x:String>
          <x:String>2.7</x:String><x:String>2.8</x:String>
        </Picker.Items>
      </Picker>
      <Label x:Name="DetectedVersion" Text="Détecté: —" VerticalOptions="Center" TextColor="Gray"/>
      <Button Text="🔍 Analyser" Clicked="OnParseClicked" HorizontalOptions="EndAndExpand"/>
    </HorizontalStackLayout>

    <!-- Arbre segments (gauche) -->
    <CollectionView Grid.Row="2" Grid.Column="0"
                    x:Name="SegmentTree"
                    SelectionMode="Single"
                    SelectionChanged="OnSegmentSelected">
      <CollectionView.ItemTemplate>
        <DataTemplate>
          <Grid Padding="8,4" ColumnDefinitions="40,*">
            <Label Grid.Column="0"
                   Text="{Binding Id}"
                   FontFamily="Courier New"
                   FontAttributes="Bold"
                   TextColor="#0078D4"/>
            <Label Grid.Column="1"
                   Text="{Binding Name}"
                   FontSize="12"
                   TextColor="#555"
                   VerticalOptions="Center"/>
          </Grid>
        </DataTemplate>
      </CollectionView.ItemTemplate>
    </CollectionView>

    <!-- Panneau détail (droite) -->
    <ScrollView Grid.Row="2" Grid.Column="1">
      <VerticalStackLayout x:Name="DetailPanel" Padding="8" Spacing="6"/>
    </ScrollView>

  </Grid>
</ContentPage>
```

---

## 🔧 Étape 7 — Enregistrement DI (MauiProgram.cs)

```csharp
builder.Services.AddSingleton<HL7KnowledgeBaseService>();
builder.Services.AddSingleton<HL7ParserService>();
builder.Services.AddSingleton<HL7Resolver>();
builder.Services.AddTransient<HL7InspectorPage>();
builder.Services.AddTransient<HL7InspectorViewModel>();
```

---

## ✅ Résumé du flux complet

```
User colle message HL7
        ↓
HL7VersionDetector.DetectVersion()   ← lit MSH-12
        ↓
HL7KnowledgeBaseService.GetKB(v)     ← charge NHapi.Model.VXX par réflexion (1x, mis en cache)
        ↓
HL7ParserService.Parse()             ← découpe Segments > Fields > Components > SubComponents
        ↓                               enrichit chaque nœud avec nom/dataType depuis KB
UI affiche l'arbre
        ↓
User clique sur PV1-3
        ↓
HL7Resolver.Resolve("PV1.3.8", v)   ← résolution 4 niveaux depuis KB
        ↓
Panneau détail affiche :
  Segment : Patient Visit (PV1)
  Field 3 : Assigned Patient Location [PL]
  Component 8 : Floor [IS]
```

---

## ⚠️ Points d'attention pour l'implémentation

- **NHapi instanciation** : certains segments ont besoin d'un `IModelClassFactory` et d'un `IMessage` parent. Wrapper dans un `try/catch` pour chaque segment.
- **Cache KB obligatoire** : l'extraction par réflexion prend ~200ms par version. Charger au 1er accès et garder en mémoire.
- **MSH est un cas spécial** : MSH-1 = `|` (le séparateur lui-même), donc l'indexation des fields décale d'un cran. Gérer séparément.
- **Répétitions** : un field peut se répéter (`~`). Splitter sur `~` avant de splitter sur `^`.
- **Escape characters** : HL7 utilise `\F\`, `\S\`, `\R\`, `\E\`, `\T\` pour échapper les séparateurs. À décoder pour l'affichage.
