# Compte rendu – TP Boutique Diayma (.NET)

> **Étudiant :** Mohamed Wade – Master GLSI, semestre 3
> **Dépôt GitHub :** <https://github.com/Mohadevsn/C-AndDotNetTD>
> **Exécutable Windows :** <https://drive.google.com/drive/folders/1R70JO5vJwHykK-S2Ba-I349QwZ9nSBBd?usp=drive_link>

---

## 1. Récupération et exécution du code

```bash
git clone https://github.com/devehod/BoutiqueDiayma2025.git
cd BoutiqueDiayma2025
dotnet run --project P2FixAnAppDotNetCode/Diayma.csproj
```

À l'ouverture dans Visual Studio, une fenêtre d'**avertissements** s'affiche. Elle signale :

- `NETSDK1138` : le framework cible `netcoreapp2.0` n'est plus pris en charge ;
- `NU1902` / `NU1903` : les paquets `Microsoft.AspNetCore.All 2.0.6` et `Microsoft.NETCore.App 2.0.0` ont des failles de sécurité connues.

Ce ne sont que des avertissements : la compilation réussit (0 erreur), on peut fermer la fenêtre.

**Problème rencontré sur Mac (Apple Silicon) :** le code d'origine compile mais ne démarre pas.

1. `You must install or update .NET to run this application` : le runtime .NET Core 2.0 est absent, seul .NET 10 est installé.
2. En forçant l'exécution sur .NET 10 (`DOTNET_ROLL_FORWARD=Major`), le serveur Kestrel 2.0 plante. Sa bibliothèque native `libuv 1.10.0` n'existe qu'en x86/x64 (`missing compatible architecture (have 'i386,x86_64', need 'arm64')`).

**Solution retenue : migrer le projet vers .NET 10** (voir §11, commit 1).

---

## 2. Projets de la solution

La solution `Diayma.sln` contient **un seul projet** :

| Projet | Fichier | Type |
|---|---|---|
| **Diayma** | `P2FixAnAppDotNetCode/Diayma.csproj` | Application web **ASP.NET Core MVC** (SDK `Microsoft.NET.Sdk.Web`) |

Organisation du projet :

| Dossier | Contenu |
|---|---|
| `Controllers/` | `ProductController`, `CartController`, `OrderController`, `LanguageController` |
| `Components/` | View Components `CartSummaryViewComponent`, `LanguageSelectorViewComponent` |
| `Models/` | `Product`, `Cart`/`CartLine`, `Order`, interfaces |
| `Models/Repositories/` | `ProductRepository`, `OrderRepository` (données en mémoire) |
| `Models/Services/` | `ProductService`, `OrderService`, `LanguageService` |
| `Views/` | Vues Razor `.cshtml` |
| `Resources/` | Fichiers de traduction `.resx` (en, fr, et maintenant es, wo) |
| `wwwroot/` | Fichiers statiques (Bootstrap 4 alpha, Font Awesome, jQuery) |

---

## 3. Version du SDK .NET utilisée

- `<TargetFramework>netcoreapp2.0</TargetFramework>`, donc **.NET Core 2.0**.
- Paquet principal : `Microsoft.AspNetCore.All` **2.0.6**.
- Il n'y a pas de `global.json` : n'importe quel SDK capable de cibler `netcoreapp2.0` convient. Le SDK correspondant est le **.NET Core SDK 2.1.x** (par ex. 2.1.202, livré avec le runtime 2.0.9).

---

## 4. Installation du SDK

- SDK installé sur la machine : **.NET SDK 10.0.403** (`dotnet --list-sdks`).
- Le SDK/runtime 2.0 d'origine est téléchargeable sur <https://dotnet.microsoft.com/download/dotnet/2.0>. Il n'existe cependant pas de version ARM64 pour macOS : il faudrait la version x64 sous Rosetta.
- **Choix fait :** garder le SDK .NET 10 (version LTS actuelle) et **migrer le projet** vers `net10.0`.

---

## 5. Dépôt GitHub personnel

Le dépôt d'origine (`devehod/BoutiqueDiayma2025`) appartient à l'enseignant. Le code est publié sur mon dépôt personnel : **<https://github.com/Mohadevsn/C-AndDotNetTD>**.

```bash
# 1. Dépôt vide créé sur https://github.com/new : C-AndDotNetTD
# 2. origin pointe vers mon dépôt (au lieu de celui de l'enseignant)
git remote set-url origin https://github.com/Mohadevsn/C-AndDotNetTD.git
# 3. Branche principale renommée en main, puis publication
git branch -M main
git push -u origin main
```

---

## 6. Bugs trouvés en explorant l'application

J'ai reproduit chaque bug avec un scénario de test : ajout au panier, commande, changement de langue. Tous ont été **corrigés** (commit 2 et commit 3).

### Bug 1 – Le total et la moyenne du panier ignorent la quantité

- **Reproduction :** ajouter 2 × « XIAOMI Redmi 13C » (88 000) et 1 × « Clé USB » (1 000).
- **Attendu :** total = 177 000, moyenne par article = 59 000.
- **Obtenu :** total = **89 000**, moyenne = 44 500. Le sous-total de la ligne (176 000) est pourtant juste.
- **Cause :** `Cart.GetTotalValue()` fait `Sum(x => x.Product.Price)` sans multiplier par `x.Quantity`. `GetAverageValue()` fait la moyenne par ligne.
- **Correction :** `Sum(x => x.Product.Price * x.Quantity)`, et moyenne = total / nombre d'articles.

### Bug 2 – Le stock n'est jamais décrémenté après une commande

- **Reproduction :** commander 2 XIAOMI (stock 10), revenir à l'accueil.
- **Attendu :** stock = 8. **Obtenu :** stock = **10**.
- **Cause :** `ProductRepository` est enregistré en `Transient` : il est recréé à chaque injection. Son constructeur réinitialise la liste **statique** `_products` (`_products = new List<Product>(); GenerateProductData();`). La mise à jour du stock est donc effacée à la requête suivante.
- **Correction :** n'initialiser la liste qu'une seule fois (`if (_products == null)` sous `lock`).

### Autres bugs relevés (et corrigés)

| # | Bug | Cause | Correction |
|---|---|---|---|
| 3 | **Panier partagé entre tous les visiteurs.** Un 2ᵉ navigateur voit le panier du 1ᵉʳ. | `services.AddSingleton<ICart, Cart>()` | Un panier par session (`Models/CartProvider.cs`, `AddScoped`) |
| 4 | Le formulaire de commande vide affiche **`ErrorMissingName`, `ErrorMissingAddress`…** au lieu de messages traduits. | Les `.resx` de `Order` étaient dans `Resources/Models/ViewModels/` alors que la classe est `Models.Order` | Fichiers déplacés dans `Resources/Models/` |
| 5 | Choisir **« Espagnol »** laisse l'interface en anglais. | La culture `es` n'est pas déclarée et aucun `.es.resx` n'existe | Ajout de `es`/`es-ES` et des traductions |
| 6 | Les prix s'affichent en **`¤`/`$`** (`¤88,000.00`) alors qu'ils sont en francs CFA. | `ToString("c")` utilise la devise de la culture courante | Méthode `ToFcfa()`, ex. « 88 000 FCFA » |
| 7 | `Cart.FindProductInCartLines()` lève une `NullReferenceException` si le produit n'est pas dans le panier. | `.FirstOrDefault().Product` | `?.Product` |

---

## 7. Points d'arrêt

| Repère | Fichier : ligne | Instruction | Quand est-il atteint ? |
|---|---|---|---|
| a) | `Components/CartSummaryViewComponent.cs:12` | `_cart = cart as Cart;` | À **chaque page** (le composant est dans `_Layout`) |
| b) | `Controllers/ProductController.cs:15` | `_productService = productService;` | À l'affichage de l'**accueil** (liste des produits) |
| c) | `Controllers/OrderController.cs:17` | `_cart = pCart;` | Seulement en allant sur **Commander** (`/Order`) |
| d) | `Controllers/CartController.cs:15` | `_cart = pCart;` | Seulement à l'**ajout au panier** ou sur la page **Panier** |
| e) | `Startup.cs:20` | `Configuration = configuration;` | **Une seule fois**, au démarrage, avant toute requête |

> Les numéros de ligne sont identiques avant et après la migration : ces lignes n'ont pas bougé.
> Points c) et d) : ils ne sont **pas** atteints avant l'affichage de l'écran d'accueil. Ils ne se déclenchent qu'après une action de l'utilisateur.

---

## 8. Parcours du débogueur jusqu'à l'affichage des produits

Raccourcis des trois modes :

| Mode | Visual Studio / VS Code | Rider | Rôle |
|---|---|---|---|
| **Pas à pas détaillé** (Step Into) | `F11` | `F7` | Entrer dans la méthode appelée |
| **Pas à pas principal** (Step Over) | `F10` | `F8` | Exécuter la ligne sans entrer dans les appels |
| **Pas à pas sortant** (Step Out) | `Maj+F11` | `Maj+F8` | Finir la méthode courante et revenir à l'appelant |

On lance en mode Debug (`F5`) avec l'option **« Uniquement mon code »** (*Just My Code*) activée. Le code du framework `Microsoft.AspNetCore.*` est alors traversé sans s'y arrêter.

### Phase A – Démarrage de l'application (avant toute requête)

| # | Namespace | Classe | Méthode | Mode utilisé | Explication |
|---|---|---|---|---|---|
| 1 | `P2FixAnAppDotNetCode` | `Program` | `Main(string[])` | point d'entrée | Démarrage du processus. |
| 2 | `P2FixAnAppDotNetCode` | `Program` | `BuildWebHost(string[])` | **Pas à pas détaillé** | On entre dans la construction de l'hôte. |
| 3 | *(framework)* `Microsoft.Extensions.Hosting` | `Host` | `CreateDefaultBuilder`, `ConfigureWebHostDefaults`, `Build` | **Pas à pas principal** | Code Microsoft : inutile d'y entrer. |
| 4 | `P2FixAnAppDotNetCode` | `Startup` | **constructeur** `Startup(IConfiguration)` | **point d'arrêt e)** ligne 20 | Appelé pendant `Build()` : réception de la configuration (`appsettings.json`). |
| 5 | `P2FixAnAppDotNetCode` | `Startup` | `ConfigureServices(IServiceCollection)` | **Pas à pas principal** (ligne par ligne) | Inscription des services : localisation, `ICart`, `ILanguageService`, `IProductService`, `IProductRepository`, `IOrderService`, `IOrderRepository`, session, MVC, cultures (en, fr, es, wo). |
| 6 | `P2FixAnAppDotNetCode` | `Startup` | — | **Pas à pas sortant** | Retour dans `Program.Main`. |
| 7 | `P2FixAnAppDotNetCode` | `Program` | `Main` → `.Run()` | **Pas à pas principal** | Démarre le serveur Kestrel. |
| 8 | `P2FixAnAppDotNetCode` | `Startup` | `Configure(IApplicationBuilder, IWebHostEnvironment)` | (appelée par le framework pendant `Run()`) | Construction du pipeline HTTP : `UseStaticFiles` → `UseRequestLocalization` → `UseSession` → `UseRouting` → `UseEndpoints`. Route par défaut `{controller=Product}/{action=Index}/{id?}`. |

> **Différence avec le code d'origine (ASP.NET Core 2.0) :** `WebHost.CreateDefaultBuilder(...).Build()` appelait le constructeur de `Startup`, puis `ConfigureServices` **et** `Configure` pendant `Build()`. Avec l'hôte générique de .NET 10, `Configure` n'est exécutée qu'au moment de `Run()`.

Le navigateur s'ouvre sur `http://localhost:62929/`. La route par défaut envoie vers **`ProductController.Index`**.

### Phase B – Requête `GET /` : construction du contrôleur

Avant de créer `ProductController`, le conteneur d'injection de dépendances construit ses dépendances. Avec un point d'arrêt (ou `F9`) dans chacun des constructeurs ci-dessous, on observe l'ordre suivant :

| # | Namespace | Classe | Méthode | Mode utilisé | Explication |
|---|---|---|---|---|---|
| 9 | `P2FixAnAppDotNetCode.Models.Repositories` | `ProductRepository` | constructeur `ProductRepository()` | **Pas à pas détaillé** | Dépendance de `ProductService` (Transient). |
| 10 | `P2FixAnAppDotNetCode.Models.Repositories` | `ProductRepository` | `GenerateProductData()` | **Pas à pas détaillé**, puis **sortant** | Crée les 9 produits (1ʳᵉ requête seulement depuis la correction du bug 2). |
| 11 | `P2FixAnAppDotNetCode.Models.Repositories` | `OrderRepository` | constructeur `OrderRepository()` | **Pas à pas sortant** | 2ᵉ dépendance de `ProductService`. |
| 12 | `P2FixAnAppDotNetCode.Models.Services` | `ProductService` | constructeur `ProductService(IProductRepository, IOrderRepository)` | **Pas à pas sortant** | Reçoit les deux repositories. |
| 13 | `P2FixAnAppDotNetCode.Models.Services` | `LanguageService` | constructeur (implicite) | — | Singleton créé à la 1ʳᵉ utilisation. |
| 14 | `P2FixAnAppDotNetCode.Controllers` | `ProductController` | **constructeur** `ProductController(IProductService, ILanguageService)` | **point d'arrêt b)** ligne 15 | Stocke les services injectés. |

### Phase C – Exécution de l'action et récupération des produits

| # | Namespace | Classe | Méthode | Mode utilisé | Explication |
|---|---|---|---|---|---|
| 15 | `P2FixAnAppDotNetCode.Controllers` | `ProductController` | `Index()` | **Pas à pas principal** jusqu'à la ligne 21 | Action appelée par le routage. |
| 16 | `P2FixAnAppDotNetCode.Models.Services` | `ProductService` | `GetAllProducts()` | **Pas à pas détaillé** sur la ligne 21 | Convertit le tableau en `List<Product>`. |
| 17 | `P2FixAnAppDotNetCode.Models.Repositories` | `ProductRepository` | `GetAllProducts()` | **Pas à pas détaillé** | Filtre `Stock > 0` et trie par nom (LINQ). |
| 18 | — | — | — | **Pas à pas sortant** ×2 | Retour dans `ProductController.Index` avec la liste. |
| 19 | `P2FixAnAppDotNetCode.Controllers` | `ProductController` | `Index()` → `return View(products)` | **Pas à pas principal** | Renvoie un `ViewResult` : le rendu Razor commence. |

### Phase D – Rendu de la page (vue + layout + View Components)

La vue s'exécute d'abord, puis le layout qui l'entoure. C'est le layout qui appelle les View Components.

| # | Namespace | Classe | Méthode | Mode utilisé | Explication |
|---|---|---|---|---|---|
| 20 | `AspNetCoreGeneratedDocument` | `Views_Product_Index` | `ExecuteAsync()` | (point d'arrêt possible dans le `.cshtml`) | Boucle `foreach` sur les produits, appel de `PriceExtensions.ToFcfa()` (namespace `P2FixAnAppDotNetCode.Models`) pour chaque prix. |
| 21 | `AspNetCoreGeneratedDocument` | `Views_Shared__Layout` | `ExecuteAsync()` | — | Layout commun ; ligne 22 : `Component.InvokeAsync("CartSummary")`. |
| 22 | `P2FixAnAppDotNetCode.Models` | `CartProvider` | `GetCart(IServiceProvider)` | **Pas à pas détaillé** | Fabrique du panier `Scoped` : lit ou crée l'identifiant de panier dans la session. |
| 23 | `P2FixAnAppDotNetCode.Components` | `CartSummaryViewComponent` | **constructeur** `CartSummaryViewComponent(ICart)` | **point d'arrêt a)** ligne 12 | Reçoit le panier du visiteur. |
| 24 | `P2FixAnAppDotNetCode.Components` | `CartSummaryViewComponent` | `Invoke()` | **Pas à pas principal** | Renvoie la vue `Shared/Components/CartSummary/Default`. Elle n'appelle `Cart.GetTotalValue()` que si le panier n'est pas vide. |
| 25 | `P2FixAnAppDotNetCode.Components` | `LanguageSelectorViewComponent` | `Invoke(ILanguageService)` | **Pas à pas principal** | Layout ligne 33 : affiche la liste déroulante des langues. |
| 26 | — | — | — | `F5` (Continuer) | La réponse HTML est envoyée : **les produits s'affichent dans le navigateur**. |

> Avec le code d'origine (2.0), les vues Razor étaient compilées dans le namespace `AspNetCore` (classes `_Views_Product_Index_cshtml`…). Depuis .NET 6, c'est `AspNetCoreGeneratedDocument`.

### Récapitulatif : namespaces et classes visités

| Namespace | Classes (méthodes) |
|---|---|
| `P2FixAnAppDotNetCode` | `Program` (`Main`, `BuildWebHost`), `Startup` (`Startup()`, `ConfigureServices`, `Configure`) |
| `P2FixAnAppDotNetCode.Models.Repositories` | `ProductRepository` (`ProductRepository()`, `GenerateProductData`, `GetAllProducts`), `OrderRepository` (`OrderRepository()`) |
| `P2FixAnAppDotNetCode.Models.Services` | `ProductService` (`ProductService()`, `GetAllProducts`), `LanguageService` (constructeur) |
| `P2FixAnAppDotNetCode.Controllers` | `ProductController` (`ProductController()`, `Index`) |
| `P2FixAnAppDotNetCode.Models` | `CartProvider` (`GetCart`), `Cart` (`Lines`), `PriceExtensions` (`ToFcfa`) |
| `P2FixAnAppDotNetCode.Components` | `CartSummaryViewComponent` (`CartSummaryViewComponent()`, `Invoke`), `LanguageSelectorViewComponent` (`Invoke`) |
| `AspNetCoreGeneratedDocument` | `Views__ViewStart` (définit le layout), `Views_Product_Index`, `Views_Shared__Layout`, `Views_Shared_Components_CartSummary_Default`, `Views_Shared_Components_LanguageSelector_Default` |

**Non visités** avant l'affichage de l'accueil : `CartController`, `OrderController`, `OrderService`, `LanguageController`. C'est pourquoi les points d'arrêt **c)** et **d)** ne sont pas atteints.

---

## 9. Déploiement sous forme d'exécutable Windows

Publication **autonome** (*self-contained*) : le runtime .NET est inclus, rien à installer sur le PC Windows.

```bash
cd P2FixAnAppDotNetCode
dotnet publish -c Release -r win-x64 --self-contained true -o bin/Release/publish/win-x64
```

Le profil Visual Studio `Properties/PublishProfiles/FolderProfile.pubxml` fait la même chose (clic droit sur le projet → **Publier**).

- Résultat : dossier `win-x64/` (119 Mo) contenant **`Diayma.exe`** (PE32+ x86-64), `wwwroot/` et les dossiers de langues `fr/`, `es/`, `wo/`.
- Archive : `P2FixAnAppDotNetCode/bin/Release/Diayma-win-x64.zip` (51 Mo).

**Lancer l'application sous Windows :**

1. Décompresser `Diayma-win-x64.zip`.
2. Double-cliquer sur `Diayma-win-x64\Diayma.exe` (une console s'ouvre).
3. Ouvrir <http://localhost:5000> dans le navigateur.

> L'exécutable a été généré depuis macOS (compilation croisée) et n'a pas été testé sur une machine Windows.

## 10. Lien de téléchargement de l'exécutable

<https://drive.google.com/drive/folders/1R70JO5vJwHykK-S2Ba-I349QwZ9nSBBd?usp=drive_link>

---

## 11. Partie optionnelle

### a) Ajout de la langue Wolof

- `Startup.cs` : ajout des cultures `wo-SN` et `wo`. Les cultures françaises `fr-FR` et `fr` sont **conservées**.
- `Models/Services/LanguageService.cs` : `"Wolof"` → culture `"wo"`.
- `Views/Shared/Components/LanguageSelector/Default.cshtml` : nouvelle option **Wolof**.
- 9 fichiers `*.wo.resx` (accueil, panier, commande, confirmation, messages d'erreur, sélecteur de langue).

> Ces traductions sont à faire relire par un locuteur natif.

### b) Commits significatifs

| Commit | Contenu |
|---|---|
| `Migration de .NET Core 2.0 vers .NET 10` | `csproj` → `net10.0`, hôte générique, routage par endpoints, `IWebHostEnvironment`, attribut `RootNamespace` pour la localisation |
| `Correction de bugs du panier, du stock, des messages et de la devise` | Bugs 1, 2, 3, 4, 6 et 7 (§6) |
| `Ajout de la langue Wolof et correction de l'espagnol` | Culture `wo`, 9 `.wo.resx`, culture `es` + 9 `.es.resx` (bug 5) |
| `Compte rendu du TP et README` | Ce document |
| `Update footer copyright year to 2026` | Personnalisation du pied de page : « © 2026 - ESP Mohamed Wade 2026 » |

### c) Dépôt sur Google Classroom

Lien déposé sur Google Classroom : <https://github.com/Mohadevsn/C-AndDotNetTD>

---

## Annexe – Lancer le projet migré

```bash
dotnet run --project P2FixAnAppDotNetCode/Diayma.csproj
# puis ouvrir http://localhost:62929
```

Prérequis : **.NET SDK 10** (`dotnet --version` ≥ 10.0).
