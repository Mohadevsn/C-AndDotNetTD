# Boutique Diayma

Boutique en ligne de démonstration, **ASP.NET Core MVC**, migrée de .NET Core 2.0 vers **.NET 10**.
Langues de l'interface : anglais, français, espagnol et **wolof**.

## Lancer le projet

Prérequis : [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project P2FixAnAppDotNetCode/Diayma.csproj
```

Puis ouvrir <http://localhost:62929>.

## Générer l'exécutable Windows

```bash
cd P2FixAnAppDotNetCode
dotnet publish -c Release -r win-x64 --self-contained true -o bin/Release/publish/win-x64
```

## Compte rendu du TP

Toutes les réponses du TP sont dans **[COMPTE_RENDU.md](COMPTE_RENDU.md)** :

- projets de la solution et version du SDK ;
- [bugs trouvés et corrigés](COMPTE_RENDU.md#6-bugs-trouvés-en-explorant-lapplication) ;
- [points d'arrêt](COMPTE_RENDU.md#7-points-darrêt) ;
- [namespaces, classes et méthodes visités avant l'affichage des produits](COMPTE_RENDU.md#8-parcours-du-débogueur-jusquà-laffichage-des-produits), avec le mode pas à pas (détaillé, principal ou sortant) choisi à chaque étape ;
- déploiement Windows et ajout de la langue Wolof.
